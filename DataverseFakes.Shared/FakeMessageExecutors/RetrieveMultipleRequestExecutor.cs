using FakeXrmEasy.Extensions;
using FakeXrmEasy.Extensions.FetchXml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Handles the execution of <see cref="RetrieveMultipleRequest"/> messages in the faked CRM context.
    /// This executor simulates retrieving multiple entity records from Dynamics 365/CRM using various query types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The executor supports three query types:
    /// <list type="bullet">
    /// <item><description><see cref="QueryExpression"/> - Standard SDK query with filters, links, and ordering</description></item>
    /// <item><description><see cref="FetchExpression"/> - FetchXML-based queries including aggregate functions</description></item>
    /// <item><description><see cref="QueryByAttribute"/> - Simple attribute-based filtering</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The executor also handles paging, distinct results, formatted values population,
    /// and EntityReference Name property resolution.
    /// </para>
    /// </remarks>
    public class RetrieveMultipleRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The <see cref="OrganizationRequest"/> to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="RetrieveMultipleRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveMultipleRequest;
        }

        /// <summary>
        /// Executes the retrieve multiple operation, fetching entity records from the faked CRM context.
        /// </summary>
        /// <param name="req">The <see cref="RetrieveMultipleRequest"/> containing the query to execute.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that provides the in-memory CRM simulation.</param>
        /// <returns>
        /// A <see cref="RetrieveMultipleResponse"/> containing an <see cref="EntityCollection"/> with the query results,
        /// including paging information such as MoreRecords, PagingCookie, and TotalRecordCount.
        /// </returns>
        /// <exception cref="PullRequestException">
        /// Thrown when the query type is not supported (not QueryExpression, FetchExpression, or QueryByAttribute).
        /// </exception>
        /// <remarks>
        /// <para>
        /// For <see cref="QueryExpression"/> queries, the executor translates the query to LINQ and executes it
        /// against the in-memory data store.
        /// </para>
        /// <para>
        /// For <see cref="FetchExpression"/> queries, the FetchXML is parsed and converted to a QueryExpression
        /// before execution. Aggregate FetchXML queries are processed separately to handle grouping and aggregation.
        /// </para>
        /// <para>
        /// For <see cref="QueryByAttribute"/> queries, the executor builds an equivalent QueryExpression
        /// with conditions for each attribute-value pair.
        /// </para>
        /// <para>
        /// Paging is controlled by the PageInfo property of the query. If not specified, the context's
        /// MaxRetrieveCount is used as the page size. The response includes MoreRecords and PagingCookie
        /// properties to support result set paging.
        /// </para>
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest req, XrmFakedContext ctx)
        {
            var request = req as RetrieveMultipleRequest;
            List<Entity> list = null;
            PagingInfo pageInfo = null;
            QueryExpression qe;

            string entityName = null;

            if (request.Query is QueryExpression)
            {
                qe = (request.Query as QueryExpression).Clone();
                entityName = qe.EntityName;

                var linqQuery = XrmFakedContext.TranslateQueryExpressionToLinq(ctx, qe);
                list = linqQuery.ToList();
            }
            else if (request.Query is FetchExpression)
            {
                var fetchXml = (request.Query as FetchExpression).Query;
                var xmlDoc = XrmFakedContext.ParseFetchXml(fetchXml);
                qe = XrmFakedContext.TranslateFetchXmlDocumentToQueryExpression(ctx, xmlDoc);
                entityName = qe.EntityName;

                var linqQuery = XrmFakedContext.TranslateQueryExpressionToLinq(ctx, qe);
                list = linqQuery.ToList();

                if (xmlDoc.IsAggregateFetchXml())
                {
                    list = XrmFakedContext.ProcessAggregateFetchXml(ctx, xmlDoc, list);
                }
            }
            else if (request.Query is QueryByAttribute)
            {
                // We instantiate a QueryExpression to be executed as we have the implementation done already
                var query = request.Query as QueryByAttribute;
                qe = new QueryExpression(query.EntityName);
                entityName = qe.EntityName;

                qe.ColumnSet = query.ColumnSet;
                qe.Criteria = new FilterExpression();
                for (var i = 0; i < query.Attributes.Count; i++)
                {
                    qe.Criteria.AddCondition(new ConditionExpression(query.Attributes[i], ConditionOperator.Equal, query.Values[i]));
                }

                foreach (var order in query.Orders)
                {
                    qe.AddOrder(order.AttributeName, order.OrderType);
                }

                qe.PageInfo = query.PageInfo;
                qe.TopCount = query.TopCount;

                // QueryExpression now done... execute it!
                var linqQuery = XrmFakedContext.TranslateQueryExpressionToLinq(ctx, qe);
                list = linqQuery.ToList();
            }
            else
            {
                throw PullRequestException.NotImplementedOrganizationRequest(request.Query.GetType());
            }

            if (qe.Distinct)
            {
                list = GetDistinctEntities(list);
            }

            // Handle the top count before taking paging into account
            if (qe.TopCount != null && qe.TopCount.Value < list.Count)
            {
                list = list.Take(qe.TopCount.Value).ToList();
            }

            // Handle TotalRecordCount here?
            int totalRecordCount = -1;
            if (qe?.PageInfo?.ReturnTotalRecordCount == true)
            {
                totalRecordCount = list.Count;
            }

            // Handle paging
            var pageSize = ctx.MaxRetrieveCount;
            pageInfo = qe.PageInfo;
            int pageNumber = 1;
            if (pageInfo != null && pageInfo.PageNumber > 0)
            {
                pageNumber = pageInfo.PageNumber;
                pageSize = pageInfo.Count == 0 ? ctx.MaxRetrieveCount : pageInfo.Count;
            }

            // Figure out where in the list we need to start and how many items we need to grab
            int numberToGet = pageSize;
            int startPosition = 0;

            if (pageNumber != 1)
            {
                startPosition = (pageNumber - 1) * pageSize;
            }

            if (list.Count < pageSize)
            {
                numberToGet = list.Count;
            }
            else if (list.Count - pageSize * (pageNumber - 1) < pageSize)
            {
                numberToGet = list.Count - (pageSize * (pageNumber - 1));
            }

            var recordsToReturn = startPosition + numberToGet > list.Count ? new List<Entity>() : list.GetRange(startPosition, numberToGet);

            recordsToReturn.ForEach(e => e.ApplyDateBehaviour(ctx));
            recordsToReturn.ForEach(e => PopulateFormattedValues(e, ctx));
            recordsToReturn.ForEach(e => PopulateEntityReferenceNames(e, ctx));

            var response = new RetrieveMultipleResponse
            {
                Results = new ParameterCollection
                                 {
                                    { "EntityCollection", new EntityCollection(recordsToReturn) }
                                 }
            };
            response.EntityCollection.EntityName = entityName;
            response.EntityCollection.MoreRecords = (list.Count - pageSize * pageNumber) > 0;
            response.EntityCollection.TotalRecordCount = totalRecordCount;

            if (response.EntityCollection.MoreRecords)
            {
                var first = response.EntityCollection.Entities.First();
                var last = response.EntityCollection.Entities.Last();
                response.EntityCollection.PagingCookie = $"<cookie page=\"{pageNumber}\"><{first.LogicalName}id last=\"{last.Id.ToString("B").ToUpper()}\" first=\"{first.Id.ToString("B").ToUpper()}\" /></cookie>";
            }

            return response;
        }

        /// <summary>
        /// Populates the FormattedValues property of an entity based on attribute types.
        /// </summary>
        /// <param name="e">The entity whose FormattedValues collection should be populated.</param>
        /// <param name="ctx">The XrmFakedContext containing entity metadata for OptionSet label lookups.</param>
        /// <remarks>
        /// This method iterates through all attributes of the entity and generates formatted string
        /// representations for certain types (such as Enum values, OptionSetValue, StateCode, StatusCode,
        /// and boolean values). The formatted values are added to the entity's FormattedValues collection
        /// if not already present.
        ///
        /// For OptionSetValue fields, the method will first check for PicklistAttributeMetadata,
        /// StateAttributeMetadata, or StatusAttributeMetadata to get the proper label. If no metadata
        /// is available, it falls back to the numeric value as a string.
        /// </remarks>
        protected void PopulateFormattedValues(Entity e, XrmFakedContext ctx)
        {
            // Iterate through attributes and retrieve formatted values based on type
            foreach (var attKey in e.Attributes.Keys)
            {
                var value = e[attKey];
                string formattedValue = "";
                if (!e.FormattedValues.ContainsKey(attKey) && (value != null))
                {
                    bool bShouldAdd;
                    formattedValue = this.GetFormattedValueForValue(value, e.LogicalName, attKey, ctx, out bShouldAdd);
                    if (bShouldAdd)
                    {
                        e.FormattedValues.Add(attKey, formattedValue);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the formatted string representation of an attribute value.
        /// </summary>
        /// <param name="value">The attribute value to format.</param>
        /// <param name="entityLogicalName">The logical name of the entity containing the attribute.</param>
        /// <param name="attributeName">The logical name of the attribute.</param>
        /// <param name="ctx">The XrmFakedContext containing entity metadata for OptionSet label lookups.</param>
        /// <param name="bShouldAddFormattedValue">
        /// When this method returns, contains <c>true</c> if the formatted value should be added
        /// to the FormattedValues collection; otherwise, <c>false</c>.
        /// </param>
        /// <returns>
        /// The formatted string representation of the value, or an empty string if no formatting is applicable.
        /// </returns>
        /// <remarks>
        /// Supports formatting for:
        /// <list type="bullet">
        /// <item><description>Enum values - Returns the enum member name</description></item>
        /// <item><description>OptionSetValue - Returns the option label from metadata, or the numeric value as string if no metadata</description></item>
        /// <item><description>Boolean - Returns "Yes" or "No" from metadata, or the bool value as string if no metadata</description></item>
        /// <item><description>AliasedValue - Recursively formats the underlying value</description></item>
        /// </list>
        /// </remarks>
        protected string GetFormattedValueForValue(object value, string entityLogicalName, string attributeName, XrmFakedContext ctx, out bool bShouldAddFormattedValue)
        {
            bShouldAddFormattedValue = false;
            var sFormattedValue = string.Empty;

            if (value is Enum)
            {
                // Retrieve the enum type
                sFormattedValue = Enum.GetName(value.GetType(), value);
                bShouldAddFormattedValue = true;
            }
            else if (value is OptionSetValue osv)
            {
                // Try to get the label from metadata
                sFormattedValue = GetOptionSetLabel(entityLogicalName, attributeName, osv.Value, ctx);
                bShouldAddFormattedValue = true;
            }
            else if (value is bool boolValue)
            {
                // Try to get the label from metadata - only add if metadata provides a label
                sFormattedValue = GetBooleanLabel(entityLogicalName, attributeName, boolValue, ctx, out bShouldAddFormattedValue);
            }
#if FAKE_XRM_EASY_9
            else if (value is OptionSetValueCollection osvCollection)
            {
                // For MultiOptionSetValue (OptionSetValueCollection), get comma-separated labels for all selected options
                if (osvCollection.Count > 0)
                {
                    var labels = new List<string>();
                    foreach (var optionSetValue in osvCollection)
                    {
                        labels.Add(GetOptionSetLabel(entityLogicalName, attributeName, optionSetValue.Value, ctx));
                    }
                    sFormattedValue = string.Join("; ", labels);
                    bShouldAddFormattedValue = true;
                }
            }
#endif
            else if (value is AliasedValue aliasedValue)
            {
                // For aliased values, extract the entity name and attribute name from the alias
                var aliasedEntityName = aliasedValue.EntityLogicalName;
                var aliasedAttributeName = aliasedValue.AttributeLogicalName;
                return this.GetFormattedValueForValue(aliasedValue.Value, aliasedEntityName, aliasedAttributeName, ctx, out bShouldAddFormattedValue);
            }

            return sFormattedValue;
        }

        /// <summary>
        /// Gets the label for an OptionSet value from entity metadata.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity containing the attribute.</param>
        /// <param name="attributeName">The logical name of the attribute.</param>
        /// <param name="optionValue">The numeric value of the option.</param>
        /// <param name="ctx">The XrmFakedContext containing entity metadata.</param>
        /// <returns>
        /// The label for the option value if found in metadata; otherwise, the numeric value as a string.
        /// </returns>
        protected string GetOptionSetLabel(string entityLogicalName, string attributeName, int optionValue, XrmFakedContext ctx)
        {
            if (string.IsNullOrEmpty(entityLogicalName) || string.IsNullOrEmpty(attributeName) || ctx == null)
            {
                return optionValue.ToString();
            }

            // Check if we have entity metadata
            if (ctx.EntityMetadata.ContainsKey(entityLogicalName))
            {
                var entityMetadata = ctx.EntityMetadata[entityLogicalName];
                if (entityMetadata.Attributes != null)
                {
                    var attributeMetadata = entityMetadata.Attributes
                        .FirstOrDefault(a => a.LogicalName == attributeName);

                    if (attributeMetadata != null)
                    {
                        OptionMetadata[] options = null;

                        // Handle different types of OptionSet attributes
                        if (attributeMetadata is PicklistAttributeMetadata picklistAttr)
                        {
                            options = picklistAttr.OptionSet?.Options?.ToArray();
                        }
                        else if (attributeMetadata is StateAttributeMetadata stateAttr)
                        {
                            options = stateAttr.OptionSet?.Options?.ToArray();
                        }
                        else if (attributeMetadata is StatusAttributeMetadata statusAttr)
                        {
                            options = statusAttr.OptionSet?.Options?.ToArray();
                        }
#if FAKE_XRM_EASY_9
                        else if (attributeMetadata is MultiSelectPicklistAttributeMetadata multiSelectAttr)
                        {
                            options = multiSelectAttr.OptionSet?.Options?.ToArray();
                        }
#endif

                        if (options != null)
                        {
                            var option = options.FirstOrDefault(o => o.Value == optionValue);
                            if (option?.Label != null)
                            {
                                // Try UserLocalizedLabel first
                                if (option.Label.UserLocalizedLabel?.Label != null)
                                {
                                    return option.Label.UserLocalizedLabel.Label;
                                }
                                // Fall back to first LocalizedLabel if available
                                if (option.Label.LocalizedLabels?.Count > 0)
                                {
                                    return option.Label.LocalizedLabels[0].Label;
                                }
                            }
                        }
                    }
                }
            }

            // Fallback to numeric value as string
            return optionValue.ToString();
        }

        /// <summary>
        /// Gets the label for a boolean value from entity metadata.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity containing the attribute.</param>
        /// <param name="attributeName">The logical name of the attribute.</param>
        /// <param name="boolValue">The boolean value.</param>
        /// <param name="ctx">The XrmFakedContext containing entity metadata.</param>
        /// <param name="bShouldAddFormattedValue">Output parameter indicating whether a formatted value should be added.</param>
        /// <returns>
        /// The label for the boolean value if found in metadata (TrueOption/FalseOption labels);
        /// otherwise, an empty string if no metadata is available.
        /// </returns>
        protected string GetBooleanLabel(string entityLogicalName, string attributeName, bool boolValue, XrmFakedContext ctx, out bool bShouldAddFormattedValue)
        {
            bShouldAddFormattedValue = true;

            if (string.IsNullOrEmpty(entityLogicalName) || string.IsNullOrEmpty(attributeName) || ctx == null)
            {
                return boolValue ? "Yes" : "No";
            }

            // Check if we have entity metadata
            if (ctx.EntityMetadata.ContainsKey(entityLogicalName))
            {
                var entityMetadata = ctx.EntityMetadata[entityLogicalName];
                if (entityMetadata.Attributes != null)
                {
                    var attributeMetadata = entityMetadata.Attributes
                        .FirstOrDefault(a => a.LogicalName == attributeName);

                    if (attributeMetadata is BooleanAttributeMetadata boolAttr)
                    {
                        var optionSet = boolAttr.OptionSet;
                        if (optionSet != null)
                        {
                            var option = boolValue ? optionSet.TrueOption : optionSet.FalseOption;
                            if (option?.Label != null)
                            {
                                // Try UserLocalizedLabel first
                                if (option.Label.UserLocalizedLabel?.Label != null)
                                {
                                    return option.Label.UserLocalizedLabel.Label;
                                }
                                // Fall back to first LocalizedLabel if available
                                if (option.Label.LocalizedLabels?.Count > 0)
                                {
                                    return option.Label.LocalizedLabels[0].Label;
                                }
                            }
                        }
                    }
                }
            }

            // No metadata found - return default Yes/No
            return boolValue ? "Yes" : "No";
        }

        /// <summary>
        /// Gets the type of organization request that this executor handles.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="RetrieveMultipleRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveMultipleRequest);
        }

        /// <summary>
        /// Populates the Name property of EntityReference attributes by looking up the referenced entity's primary name attribute.
        /// </summary>
        /// <param name="entity">The entity whose EntityReference attributes should have their Name property populated.</param>
        /// <param name="context">The <see cref="XrmFakedContext"/> containing entity metadata and data.</param>
        /// <remarks>
        /// This method resolves upstream issue #555 by ensuring that EntityReference attributes
        /// have their Name property set based on the referenced entity's primary name attribute.
        /// The Name is only populated if it is not already set and the referenced entity exists
        /// in the context with valid metadata.
        /// </remarks>
        private void PopulateEntityReferenceNames(Entity entity, XrmFakedContext context)
        {
            if (entity == null || context == null)
                return;

            foreach (var attribute in entity.Attributes.ToList())
            {
                if (attribute.Value is EntityReference entityRef)
                {
                    // Only populate if Name is not already set
                    if (string.IsNullOrEmpty(entityRef.Name) &&
                        !string.IsNullOrEmpty(entityRef.LogicalName) &&
                        entityRef.Id != Guid.Empty)
                    {
                        // Check if metadata exists for this entity
                        if (context.EntityMetadata.ContainsKey(entityRef.LogicalName) &&
                            !string.IsNullOrEmpty(context.EntityMetadata[entityRef.LogicalName].PrimaryNameAttribute))
                        {
                            var primaryNameAttribute = context.EntityMetadata[entityRef.LogicalName].PrimaryNameAttribute;

                            // Check if the referenced entity exists in the context
                            ConcurrentDictionary<Guid, Entity> refEntityDict;
                            Entity referencedEntity;
                            if (context.Data.TryGetValue(entityRef.LogicalName, out refEntityDict) &&
                                refEntityDict.TryGetValue(entityRef.Id, out referencedEntity))
                            {
                                if (referencedEntity.Contains(primaryNameAttribute))
                                {
                                    entityRef.Name = referencedEntity.GetAttributeValue<string>(primaryNameAttribute);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Filters a list of entities to return only distinct records based on logical name and attribute values.
        /// </summary>
        /// <param name="input">The collection of entities to filter.</param>
        /// <returns>
        /// A list containing only unique entities. Two entities are considered equal if they have
        /// the same LogicalName and identical attribute key-value pairs.
        /// </returns>
        /// <remarks>
        /// This method is used to handle the Distinct property of QueryExpression queries.
        /// Entity comparison is based on both the logical name and all attribute values.
        /// </remarks>
        private static List<Entity> GetDistinctEntities(IEnumerable<Entity> input)
        {
            var output = new List<Entity>();

            foreach (var entity in input)
            {
                if (!output.Any(i => i.LogicalName == entity.LogicalName && i.Attributes.SequenceEqual(entity.Attributes)))
                {
                    output.Add(entity);
                }
            }

            return output;
        }
    }
}

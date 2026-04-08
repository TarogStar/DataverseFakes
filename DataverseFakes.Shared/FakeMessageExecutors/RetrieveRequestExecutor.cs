using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Handles the execution of <see cref="RetrieveRequest"/> messages in the faked CRM context.
    /// This executor simulates retrieving a single entity record from Dynamics 365/CRM by its unique identifier.
    /// </summary>
    /// <remarks>
    /// The executor retrieves an entity from the in-memory context, applies column projections based on
    /// the specified <see cref="ColumnSet"/>, and optionally retrieves related entities based on
    /// the <see cref="RetrieveRequest.RelatedEntitiesQuery"/> property. It also populates
    /// EntityReference Name properties using the referenced entity's primary name attribute.
    /// </remarks>
    public class RetrieveRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The <see cref="OrganizationRequest"/> to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request type matches <see cref="RetrieveRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request.GetType().Equals(GetResponsibleRequestType());
        }

        /// <summary>
        /// Executes the retrieve operation, fetching a single entity record from the faked CRM context.
        /// </summary>
        /// <param name="req">The <see cref="RetrieveRequest"/> containing the target entity reference and column set.</param>
        /// <param name="context">The <see cref="XrmFakedContext"/> that provides the in-memory CRM simulation.</param>
        /// <returns>
        /// A <see cref="RetrieveResponse"/> containing the retrieved entity in the Results collection under the "Entity" key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the Target property is null.
        /// </exception>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the ColumnSet is missing or when the requested entity does not exist in the context.
        /// The error code 0x80040217 indicates "entity does not exist".
        /// </exception>
        /// <remarks>
        /// <para>
        /// The Target property must contain a valid <see cref="EntityReference"/> identifying the entity to retrieve.
        /// The ColumnSet property specifies which attributes to return. If ColumnSet.AllColumns is false,
        /// only the specified columns are included in the result.
        /// </para>
        /// <para>
        /// If RelatedEntitiesQuery is specified, related entities are retrieved according to the defined
        /// relationships and included in the result entity's RelatedEntities collection.
        /// </para>
        /// <para>
        /// EntityReference attributes in the result have their Name property populated automatically
        /// using the referenced entity's primary name attribute from metadata.
        /// </para>
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest req, XrmFakedContext context)
        {
            var request = req as RetrieveRequest;

            if (request.Target == null)
            {
                throw new ArgumentNullException("Target", "RetrieveRequest without Target is invalid.");
            }

            var entityName = request.Target.LogicalName;

            // Dataverse special-case behavior: retrieving a calendar record automatically returns
            // the calendarrules attribute (EntityCollection) even when not explicitly requested.
            // Also, calendarrules can be requested in the ColumnSet without triggering
            // "attribute does not exist" validation.
            var isCalendar = string.Equals(entityName, "calendar", StringComparison.OrdinalIgnoreCase);

            // Dataverse behavior: activityparty is not a retrievable entity.
            // Parties are only accessible through activity party-list attributes (e.g. to/from) on activity entities.
            if (string.Equals(entityName, "activityparty", StringComparison.OrdinalIgnoreCase))
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "The 'Retrieve' method does not support entities of type 'activityparty'.");
            }

            var columnSet = request.ColumnSet;
            if (columnSet == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Required field 'ColumnSet' is missing");
            }

            var id = context.GetRecordUniqueId(request.Target);

            //Entity logical name exists, so , check if the requested entity exists
            ConcurrentDictionary<Guid, Entity> entityDict;
            Entity foundEntity;
            if (context.Data.TryGetValue(entityName, out entityDict) && entityDict != null
                && entityDict.TryGetValue(id, out foundEntity))
            {
                //Return the subset of columns requested only
                var reflectedType = context.FindReflectedType(entityName);

                //Entity found => return only the subset of columns specified or all of them
                var resultEntity = foundEntity.Clone(reflectedType, context);
                if (!columnSet.AllColumns)
                {
                    resultEntity = resultEntity.ProjectAttributes(columnSet, context);
                }

                if (isCalendar)
                {
                    // Always include calendarrules when present on the stored calendar entity.
                    // (Some environments also expose it via FetchXML even when not selected.)
                    if (foundEntity.Attributes.ContainsKey("calendarrules") && foundEntity["calendarrules"] != null)
                    {
                        resultEntity["calendarrules"] = foundEntity["calendarrules"];
                    }
                }
                resultEntity.ApplyDateBehaviour(context);
                PopulateFormattedValues(resultEntity, context);
                PopulateEntityReferenceNames(resultEntity, context);

                if (request.RelatedEntitiesQuery != null && request.RelatedEntitiesQuery.Count > 0)
                {
                    foreach (var relatedEntitiesQuery in request.RelatedEntitiesQuery)
                    {
                        if (relatedEntitiesQuery.Value == null)
                        {
                            throw new ArgumentNullException("relateEntitiesQuery.Value",
                                string.Format("RelatedEntitiesQuery for \"{0}\" does not contain a Query Expression.",
                                    relatedEntitiesQuery.Key.SchemaName));
                        }

                        var fakeRelationship = context.GetRelationship(relatedEntitiesQuery.Key.SchemaName);
                        if (fakeRelationship == null)
                        {
                            throw new Exception(string.Format("Relationship \"{0}\" does not exist in the metadata cache.",
                                relatedEntitiesQuery.Key.SchemaName));
                        }

                        var relatedEntitiesQueryValue = (QueryExpression)relatedEntitiesQuery.Value;
                        QueryExpression retrieveRelatedEntitiesQuery = relatedEntitiesQueryValue.Clone();

                        if (fakeRelationship.RelationshipType == XrmFakedRelationship.enmFakeRelationshipType.OneToMany)
                        {
                            var isFrom1to2 = relatedEntitiesQueryValue.EntityName == fakeRelationship.Entity1LogicalName
                                || request.Target.LogicalName != fakeRelationship.Entity1LogicalName
                                || string.IsNullOrWhiteSpace(relatedEntitiesQueryValue.EntityName);

                            if (isFrom1to2)
                            {
                                var fromAttribute = isFrom1to2 ? fakeRelationship.Entity1Attribute : fakeRelationship.Entity2Attribute;
                                var toAttribute = isFrom1to2 ? fakeRelationship.Entity2Attribute : fakeRelationship.Entity1Attribute;

                                var linkEntity = new LinkEntity
                                {
                                    Columns = new ColumnSet(false),
                                    LinkFromAttributeName = fromAttribute,
                                    LinkFromEntityName = retrieveRelatedEntitiesQuery.EntityName,
                                    LinkToAttributeName = toAttribute,
                                    LinkToEntityName = resultEntity.LogicalName
                                };

                                if (retrieveRelatedEntitiesQuery.Criteria == null)
                                {
                                    retrieveRelatedEntitiesQuery.Criteria = new FilterExpression();
                                }

                                retrieveRelatedEntitiesQuery.Criteria
                                    .AddFilter(LogicalOperator.And)
                                    .AddCondition(linkEntity.LinkFromAttributeName, ConditionOperator.Equal, resultEntity.Id);
                            }
                            else
                            {
                                var link = retrieveRelatedEntitiesQuery.AddLink(fakeRelationship.Entity1LogicalName, fakeRelationship.Entity2Attribute, fakeRelationship.Entity1Attribute);
                                link.LinkCriteria.AddCondition(resultEntity.LogicalName + "id", ConditionOperator.Equal, resultEntity.Id);
                            }
                        }
                        else
                        {
                            var isFrom1 = fakeRelationship.Entity1LogicalName == retrieveRelatedEntitiesQuery.EntityName;
                            var linkAttributeName = isFrom1 ? fakeRelationship.Entity1Attribute : fakeRelationship.Entity2Attribute;
                            var conditionAttributeName = isFrom1 ? fakeRelationship.Entity2Attribute : fakeRelationship.Entity1Attribute;

                            var linkEntity = new LinkEntity
                            {
                                Columns = new ColumnSet(false),
                                LinkFromAttributeName = linkAttributeName,
                                LinkFromEntityName = retrieveRelatedEntitiesQuery.EntityName,
                                LinkToAttributeName = linkAttributeName,
                                LinkToEntityName = fakeRelationship.IntersectEntity,
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                {
                                    new ConditionExpression(conditionAttributeName , ConditionOperator.Equal, resultEntity.Id)
                                }
                                }
                            };
                            retrieveRelatedEntitiesQuery.LinkEntities.Add(linkEntity);
                        }

                        var retrieveRelatedEntitiesRequest = new RetrieveMultipleRequest
                        {
                            Query = retrieveRelatedEntitiesQuery
                        };

                        //use of an executor directly; if to use service.RetrieveMultiple then the result will be
                        //limited to the number of records per page (somewhere in future release).
                        //ALL RECORDS are needed here.
                        var executor = new RetrieveMultipleRequestExecutor();
                        var retrieveRelatedEntitiesResponse = executor
                            .Execute(retrieveRelatedEntitiesRequest, context) as RetrieveMultipleResponse;

                        if (retrieveRelatedEntitiesResponse.EntityCollection.Entities.Count == 0)
                            continue;

                        resultEntity.RelatedEntities
                            .Add(relatedEntitiesQuery.Key, retrieveRelatedEntitiesResponse.EntityCollection);
                    }
                }

                return new RetrieveResponse
                {
                    Results = new ParameterCollection { { "Entity", resultEntity } }
                };
            }
            else
            {
                // Entity not found in the context => FaultException
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault() { ErrorCode = unchecked((int)0x80040217) }, $"{entityName} With Id = {id:D} Does Not Exist");
            }
        }

        /// <summary>
        /// Gets the type of organization request that this executor handles.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="RetrieveRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveRequest);
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
        /// Populates the FormattedValues property of an entity based on attribute types.
        /// </summary>
        /// <param name="entity">The entity whose FormattedValues collection should be populated.</param>
        /// <param name="context">The XrmFakedContext containing entity metadata for OptionSet label lookups.</param>
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
        private void PopulateFormattedValues(Entity entity, XrmFakedContext context)
        {
            if (entity == null || context == null)
                return;

            foreach (var attKey in entity.Attributes.Keys)
            {
                var value = entity[attKey];
                if (!entity.FormattedValues.ContainsKey(attKey) && value != null)
                {
                    string formattedValue = GetFormattedValueForValue(value, entity.LogicalName, attKey, context, out bool shouldAdd);
                    if (shouldAdd)
                    {
                        entity.FormattedValues.Add(attKey, formattedValue);
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
        /// <param name="context">The XrmFakedContext containing entity metadata for OptionSet label lookups.</param>
        /// <param name="shouldAddFormattedValue">
        /// When this method returns, contains <c>true</c> if the formatted value should be added
        /// to the FormattedValues collection; otherwise, <c>false</c>.
        /// </param>
        /// <returns>
        /// The formatted string representation of the value, or an empty string if no formatting is applicable.
        /// </returns>
        private string GetFormattedValueForValue(object value, string entityLogicalName, string attributeName, XrmFakedContext context, out bool shouldAddFormattedValue)
        {
            shouldAddFormattedValue = false;
            var formattedValue = string.Empty;

            if (value is Enum)
            {
                formattedValue = Enum.GetName(value.GetType(), value);
                shouldAddFormattedValue = true;
            }
            else if (value is OptionSetValue osv)
            {
                formattedValue = GetOptionSetLabel(entityLogicalName, attributeName, osv.Value, context);
                shouldAddFormattedValue = true;
            }
            else if (value is bool boolValue)
            {
                formattedValue = GetBooleanLabel(entityLogicalName, attributeName, boolValue, context);
                shouldAddFormattedValue = true;
            }
#if FAKE_XRM_EASY_9
            else if (value is OptionSetValueCollection osvCollection)
            {
                if (osvCollection.Count > 0)
                {
                    var labels = new System.Collections.Generic.List<string>();
                    foreach (var item in osvCollection)
                    {
                        labels.Add(GetOptionSetLabel(entityLogicalName, attributeName, item.Value, context));
                    }
                    formattedValue = string.Join("; ", labels);
                    shouldAddFormattedValue = true;
                }
            }
#endif
            else if (value is AliasedValue aliasedValue)
            {
                return GetFormattedValueForValue(aliasedValue.Value, aliasedValue.EntityLogicalName, aliasedValue.AttributeLogicalName, context, out shouldAddFormattedValue);
            }

            return formattedValue;
        }

        /// <summary>
        /// Gets the label for an OptionSet value from entity metadata.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity containing the attribute.</param>
        /// <param name="attributeName">The logical name of the attribute.</param>
        /// <param name="optionValue">The numeric value of the option.</param>
        /// <param name="context">The XrmFakedContext containing entity metadata.</param>
        /// <returns>
        /// The label for the option value if found in metadata; otherwise, the numeric value as a string.
        /// </returns>
        private string GetOptionSetLabel(string entityLogicalName, string attributeName, int optionValue, XrmFakedContext context)
        {
            if (string.IsNullOrEmpty(entityLogicalName) || string.IsNullOrEmpty(attributeName) || context == null)
            {
                return optionValue.ToString();
            }

            if (context.EntityMetadata.ContainsKey(entityLogicalName))
            {
                var entityMetadata = context.EntityMetadata[entityLogicalName];
                if (entityMetadata.Attributes != null)
                {
                    var attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == attributeName);
                    if (attributeMetadata != null)
                    {
                        OptionMetadata[] options = null;

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

            return optionValue.ToString();
        }

        /// <summary>
        /// Gets the label for a boolean value from entity metadata.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity containing the attribute.</param>
        /// <param name="attributeName">The logical name of the attribute.</param>
        /// <param name="boolValue">The boolean value.</param>
        /// <param name="context">The XrmFakedContext containing entity metadata.</param>
        /// <returns>
        /// The label for the boolean value if found in metadata (TrueOption/FalseOption labels);
        /// otherwise, "Yes" for true or "No" for false.
        /// </returns>
        private string GetBooleanLabel(string entityLogicalName, string attributeName, bool boolValue, XrmFakedContext context)
        {
            if (string.IsNullOrEmpty(entityLogicalName) || string.IsNullOrEmpty(attributeName) || context == null)
            {
                return boolValue ? "Yes" : "No";
            }

            if (context.EntityMetadata.ContainsKey(entityLogicalName))
            {
                var entityMetadata = context.EntityMetadata[entityLogicalName];
                if (entityMetadata.Attributes != null)
                {
                    var attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == attributeName);
                    if (attributeMetadata is BooleanAttributeMetadata boolAttr)
                    {
                        var optionSet = boolAttr.OptionSet;
                        if (optionSet != null)
                        {
                            var option = boolValue ? optionSet.TrueOption : optionSet.FalseOption;
                            if (option?.Label?.UserLocalizedLabel?.Label != null)
                            {
                                return option.Label.UserLocalizedLabel.Label;
                            }
                        }
                    }
                }
            }

            return boolValue ? "Yes" : "No";
        }
    }
}

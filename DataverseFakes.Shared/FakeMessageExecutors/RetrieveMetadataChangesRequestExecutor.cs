using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for RetrieveMetadataChangesRequest.
    /// Enables testing metadata change tracking and filtering.
    /// Addresses upstream PR #538 from MarkMpn.
    /// </summary>
    public class RetrieveMetadataChangesRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is RetrieveMetadataChangesRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveMetadataChangesRequest;
        }

        /// <summary>
        /// Executes the RetrieveMetadataChangesRequest by filtering and projecting metadata
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>RetrieveMetadataChangesResponse with filtered entity metadata</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as RetrieveMetadataChangesRequest;

            var response = new RetrieveMetadataChangesResponse()
            {
                Results = new ParameterCollection
                {
                    ["EntityMetadata"] = ApplyFilter(req.Query, ctx.EntityMetadata.Values)
                }
            };

            return response;
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of RetrieveMetadataChangesRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveMetadataChangesRequest);
        }

        private EntityMetadataCollection ApplyFilter(EntityQueryExpression qry,
            IEnumerable<EntityMetadata> metadata)
        {
            var results = new EntityMetadataCollection();

            if (qry == null)
            {
                results.AddRange(metadata);
                return results;
            }

            results.AddRange(metadata
                .Where(e => IsMatch(e, qry.Criteria))
                .Select(e => Project(e, qry, qry.Properties)));

            return results;
        }

        private bool IsMatch(object obj, MetadataFilterExpression criteria)
        {
            if (criteria == null)
                return true;

            if ((criteria.Conditions == null || criteria.Conditions.Count == 0) &&
                (criteria.Filters == null || criteria.Filters.Count == 0))
                return true;

            if (criteria.Conditions != null)
            {
                foreach (var condition in criteria.Conditions)
                {
                    var conditionMatch = IsMatch(obj, condition);

                    if (criteria.FilterOperator == LogicalOperator.And && !conditionMatch)
                        return false;
                    else if (criteria.FilterOperator == LogicalOperator.Or && conditionMatch)
                        return true;
                }
            }

            if (criteria.Filters != null)
            {
                foreach (var filter in criteria.Filters)
                {
                    var filterMatch = IsMatch(obj, filter);

                    if (criteria.FilterOperator == LogicalOperator.And && !filterMatch)
                        return false;
                    else if (criteria.FilterOperator == LogicalOperator.Or && filterMatch)
                        return true;
                }
            }

            if (criteria.FilterOperator == LogicalOperator.And)
                return true;

            return false;
        }

        private bool IsMatch(object obj, MetadataConditionExpression condition)
        {
            if (obj == null)
                return false;

            var prop = obj.GetType().GetProperty(condition.PropertyName);

            if (prop == null)
                throw new InvalidOperationException($"Unknown property {condition.PropertyName} on type {obj.GetType().Name}");

            var value = prop.GetValue(obj, null);

            switch (condition.ConditionOperator)
            {
                case MetadataConditionOperator.Equals:
                    return value == condition.Value || (value != null && condition.Value != null &&
                        value.Equals(condition.Value));

                case MetadataConditionOperator.GreaterThan:
                    return value != null && condition.Value != null &&
                        ((IComparable)value).CompareTo(condition.Value) > 0;

                case MetadataConditionOperator.In:
                    if (condition.Value == null)
                        return false;
                    foreach (var v in (Array)condition.Value)
                    {
                        if (value == v || (value != null && v != null && value.Equals(v)))
                            return true;
                    }
                    return false;

                case MetadataConditionOperator.LessThan:
                    return value != null && condition.Value != null &&
                        ((IComparable)value).CompareTo(condition.Value) < 0;

                case MetadataConditionOperator.NotEquals:
                    return (value == null ^ condition.Value == null) || (value != null &&
                        condition.Value != null && !value.Equals(condition.Value));

                case MetadataConditionOperator.NotIn:
                    if (condition.Value == null)
                        return true;
                    foreach (var v in (Array)condition.Value)
                    {
                        if ((value == null && v == null) || (value != null && v != null &&
                            value.Equals(v)))
                            return false;
                    }
                    return true;

                default:
                    throw new InvalidOperationException($"Unknown condition operator {condition.ConditionOperator}");
            }
        }

        private T Project<T>(T obj, EntityQueryExpression qry, MetadataPropertiesExpression properties)
        {
            if (obj == null)
                return default(T);

            var props = obj.GetType().GetProperties();

            if (properties != null && !properties.AllProperties && properties.PropertyNames != null)
            {
                props = props
                    .Where(p => properties.PropertyNames.Contains(p.Name))
                    .ToArray();
            }

            var result = Activator.CreateInstance(obj.GetType());

            foreach (var prop in props)
            {
                if (!prop.CanWrite)
                    continue;

                var value = prop.GetValue(obj, null);

                if (value == null)
                    continue;

                if (prop.PropertyType == typeof(AttributeMetadata[]) && qry?.AttributeQuery != null)
                {
                    var attrs = (AttributeMetadata[])value;

                    value = attrs
                        .Where(a => IsMatch(a, qry.AttributeQuery.Criteria))
                        .Select(a => Project(a, qry, qry.AttributeQuery.Properties))
                        .ToArray();
                }
                else if (prop.PropertyType == typeof(OneToManyRelationshipMetadata[]) &&
                    qry?.RelationshipQuery != null)
                {
                    var rels = (OneToManyRelationshipMetadata[])value;

                    value = rels
                        .Where(r => IsMatch(r, qry.RelationshipQuery.Criteria))
                        .Select(r => Project(r, qry, qry.RelationshipQuery.Properties))
                        .ToArray();
                }
                else if (prop.PropertyType == typeof(ManyToManyRelationshipMetadata[]) &&
                    qry?.RelationshipQuery != null)
                {
                    var rels = (ManyToManyRelationshipMetadata[])value;

                    value = rels
                        .Where(r => IsMatch(r, qry.RelationshipQuery.Criteria))
                        .Select(r => Project(r, qry, qry.RelationshipQuery.Properties))
                        .ToArray();
                }
                else if (prop.PropertyType == typeof(Label) && qry?.LabelQuery != null)
                {
                    var label = (Label)value;

                    if (label.LocalizedLabels != null && qry.LabelQuery.FilterLanguages != null)
                    {
                        var locLabels = label.LocalizedLabels.ToArray();
                        label.LocalizedLabels.Clear();
                        label.LocalizedLabels.AddRange(locLabels.Where(l =>
                            qry.LabelQuery.FilterLanguages.Contains(l.LanguageCode)));
                    }
                }

                try
                {
                    prop.SetValue(result, value, null);
                }
                catch
                {
                    // Some properties may be read-only at runtime, skip them
                }
            }

            return (T)result;
        }
    }
}

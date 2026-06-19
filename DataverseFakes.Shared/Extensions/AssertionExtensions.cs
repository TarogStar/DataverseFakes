using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DataverseFakes
{
    /// <summary>
    /// Intent-revealing assertion helpers over the in-memory data of an <see cref="XrmFakedContext"/>.
    /// Each method throws <see cref="XrmFakedAssertException"/> with a descriptive message when the
    /// expectation is not met, so they work with any test runner without a framework dependency.
    /// </summary>
    public static class AssertionExtensions
    {
        // ---- Existence -----------------------------------------------------------------------

        /// <summary>Asserts that a record with the given logical name and id exists in the context.</summary>
        public static void AssertExists(this XrmFakedContext context, string entityName, Guid id)
        {
            if (!TryGetRecord(context, entityName, id, out _))
            {
                throw new XrmFakedAssertException($"Expected record {entityName}({id}) to exist, but it was not found.");
            }
        }

        /// <summary>Asserts that no record with the given logical name and id exists in the context.</summary>
        public static void AssertDoesNotExist(this XrmFakedContext context, string entityName, Guid id)
        {
            if (TryGetRecord(context, entityName, id, out _))
            {
                throw new XrmFakedAssertException($"Expected record {entityName}({id}) to not exist, but it was found.");
            }
        }

        // ---- Attribute -----------------------------------------------------------------------

        /// <summary>
        /// Asserts that the record's attribute equals <paramref name="expected"/>. The expected value
        /// may be the raw underlying value or the SDK wrapper: <see cref="OptionSetValue"/>,
        /// <see cref="Money"/>, and <see cref="EntityReference"/> are normalized before comparison.
        /// </summary>
        public static void AssertAttributeValue(this XrmFakedContext context, string entityName, Guid id, string attribute, object expected)
        {
            var record = GetRecordOrThrow(context, entityName, id);
            record.Attributes.TryGetValue(attribute, out var actual);
            if (!ValuesEqual(actual, expected))
            {
                throw new XrmFakedAssertException(
                    $"Expected {entityName}({id}).{attribute} == {Format(expected)} but was {Format(actual)}.");
            }
        }

        /// <summary>Asserts that the record has the attribute present with a non-null value.</summary>
        public static void AssertHasAttribute(this XrmFakedContext context, string entityName, Guid id, string attribute)
        {
            var record = GetRecordOrThrow(context, entityName, id);
            if (!record.Attributes.TryGetValue(attribute, out var value) || value == null)
            {
                throw new XrmFakedAssertException(
                    $"Expected {entityName}({id}) to have a non-null '{attribute}' attribute, but it was {(record.Attributes.ContainsKey(attribute) ? "null" : "absent")}.");
            }
        }

        /// <summary>Asserts that the record's attribute is absent or null.</summary>
        public static void AssertAttributeNull(this XrmFakedContext context, string entityName, Guid id, string attribute)
        {
            var record = GetRecordOrThrow(context, entityName, id);
            if (record.Attributes.TryGetValue(attribute, out var value) && value != null)
            {
                throw new XrmFakedAssertException(
                    $"Expected {entityName}({id}).{attribute} to be null/absent, but was {Format(value)}.");
            }
        }

        // ---- Association (N:N) ---------------------------------------------------------------

        /// <summary>Asserts that the two records are associated via the named relationship.</summary>
        public static void AssertAssociated(this XrmFakedContext context, string entity1, Guid id1, string entity2, Guid id2, string relationshipName)
        {
            if (!IsAssociated(context, id1, id2, relationshipName))
            {
                throw new XrmFakedAssertException(
                    $"Expected {entity1}({id1}) and {entity2}({id2}) to be associated via '{relationshipName}', but no intersect row was found.");
            }
        }

        /// <summary>Asserts that the two records are NOT associated via the named relationship.</summary>
        public static void AssertNotAssociated(this XrmFakedContext context, string entity1, Guid id1, string entity2, Guid id2, string relationshipName)
        {
            if (IsAssociated(context, id1, id2, relationshipName))
            {
                throw new XrmFakedAssertException(
                    $"Expected {entity1}({id1}) and {entity2}({id2}) to not be associated via '{relationshipName}', but an intersect row was found.");
            }
        }

        // ---- Record count --------------------------------------------------------------------

        /// <summary>Asserts the number of records stored for a logical name.</summary>
        public static void AssertRecordCount(this XrmFakedContext context, string entityName, int expectedCount)
        {
            var actual = context.Data.TryGetValue(entityName, out var table) ? table.Count : 0;
            if (actual != expectedCount)
            {
                throw new XrmFakedAssertException($"Expected {expectedCount} {entityName} record(s) but found {actual}.");
            }
        }

        /// <summary>Asserts the number of records returned by a query.</summary>
        public static void AssertRecordCount(this XrmFakedContext context, QueryExpression query, int expectedCount)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var actual = context.GetOrganizationService().RetrieveMultiple(query).Entities.Count;
            if (actual != expectedCount)
            {
                throw new XrmFakedAssertException($"Expected {expectedCount} {query.EntityName} record(s) from the query but found {actual}.");
            }
        }

        // ---- Plugin execution assertions -----------------------------------------------------

        /// <summary>
        /// Asserts that plugin <typeparamref name="T"/> was executed at least once in this context.
        /// </summary>
        public static void AssertPluginExecuted<T>(this XrmFakedContext context)
            where T : IPlugin
        {
            if (!context.PluginExecutions.Any(r => r.PluginType == typeof(T)))
            {
                throw new XrmFakedAssertException(
                    $"Expected plugin {typeof(T).Name} to have been executed, but it was not found in the execution trace.\n{context.GetPluginStepTrace()}");
            }
        }

        /// <summary>
        /// Asserts that plugin <typeparamref name="T"/> was executed at least once for the given message name.
        /// </summary>
        public static void AssertPluginExecuted<T>(this XrmFakedContext context, string messageName)
            where T : IPlugin
        {
            if (!context.PluginExecutions.Any(r => r.PluginType == typeof(T) && string.Equals(r.MessageName, messageName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new XrmFakedAssertException(
                    $"Expected plugin {typeof(T).Name} to have been executed for message '{messageName}', but it was not found in the execution trace.\n{context.GetPluginStepTrace()}");
            }
        }

        /// <summary>
        /// Asserts that plugin <typeparamref name="T"/> was executed exactly <paramref name="expectedCount"/> times.
        /// </summary>
        public static void AssertPluginExecutedTimes<T>(this XrmFakedContext context, int expectedCount)
            where T : IPlugin
        {
            var actual = context.PluginExecutions.Count(r => r.PluginType == typeof(T));
            if (actual != expectedCount)
            {
                throw new XrmFakedAssertException(
                    $"Expected plugin {typeof(T).Name} to have been executed {expectedCount} time(s), but it was executed {actual} time(s).\n{context.GetPluginStepTrace()}");
            }
        }

        /// <summary>
        /// Asserts that plugin <typeparamref name="T"/> was NOT executed at all in this context.
        /// </summary>
        public static void AssertPluginNotExecuted<T>(this XrmFakedContext context)
            where T : IPlugin
        {
            var actual = context.PluginExecutions.Count(r => r.PluginType == typeof(T));
            if (actual > 0)
            {
                throw new XrmFakedAssertException(
                    $"Expected plugin {typeof(T).Name} to NOT have been executed, but it was executed {actual} time(s).\n{context.GetPluginStepTrace()}");
            }
        }

        // ---- Helpers -------------------------------------------------------------------------

        private static bool TryGetRecord(XrmFakedContext context, string entityName, Guid id, out Entity record)
        {
            record = null;
            return context.Data.TryGetValue(entityName, out var table) && table.TryGetValue(id, out record);
        }

        private static Entity GetRecordOrThrow(XrmFakedContext context, string entityName, Guid id)
        {
            if (TryGetRecord(context, entityName, id, out var record))
            {
                return record;
            }
            throw new XrmFakedAssertException($"Expected record {entityName}({id}) to exist, but it was not found.");
        }

        private static bool IsAssociated(XrmFakedContext context, Guid id1, Guid id2, string relationshipName)
        {
            var relationship = context.GetRelationship(relationshipName);
            if (relationship == null)
            {
                throw new XrmFakedAssertException($"Relationship '{relationshipName}' was not found in the context.");
            }

            var rows = context.GetOrganizationService()
                .RetrieveMultiple(new QueryExpression(relationship.IntersectEntity) { ColumnSet = new ColumnSet(true) })
                .Entities;

            return rows.Any(r =>
            {
                var a = AsGuid(r.Attributes.TryGetValue(relationship.Entity1Attribute, out var v1) ? v1 : null);
                var b = AsGuid(r.Attributes.TryGetValue(relationship.Entity2Attribute, out var v2) ? v2 : null);
                // Association is symmetric; accept either ordering (covers self-referential N:N too).
                return (a == id1 && b == id2) || (a == id2 && b == id1);
            });
        }

        private static Guid AsGuid(object value)
        {
            switch (value)
            {
                case Guid g: return g;
                case EntityReference r: return r.Id;
                default: return Guid.Empty;
            }
        }

        private static bool ValuesEqual(object actual, object expected)
        {
            if (actual == null && expected == null) return true;
            if (actual == null || expected == null) return false;

            if (actual is EntityReference actualRef)
            {
                if (expected is EntityReference expectedRef)
                {
                    return actualRef.Id == expectedRef.Id
                        && string.Equals(actualRef.LogicalName, expectedRef.LogicalName, StringComparison.Ordinal);
                }
                if (expected is Guid expectedGuid)
                {
                    return actualRef.Id == expectedGuid;
                }
                return false;
            }
            if (actual is Guid actualGuid && expected is EntityReference expRef)
            {
                return actualGuid == expRef.Id;
            }

            return Equals(NormalizeScalar(actual), NormalizeScalar(expected));
        }

        private static object NormalizeScalar(object value)
        {
            switch (value)
            {
                case OptionSetValue osv: return osv.Value;
                case Money money: return money.Value;
                default: return value;
            }
        }

        private static string Format(object value)
        {
            switch (value)
            {
                case null: return "null";
                case string s: return $"\"{s}\"";
                case OptionSetValue osv: return $"OptionSetValue({osv.Value})";
                case Money money: return $"Money({money.Value})";
                case EntityReference r: return $"{r.LogicalName}({r.Id})";
                default: return value.ToString();
            }
        }
    }
}

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

namespace DataverseFakes
{
    /// <summary>
    /// Partial class containing elastic table support for the faked CRM context.
    /// Elastic tables are Cosmos DB-backed tables in Dataverse that have different
    /// behavioral characteristics from standard SQL-backed tables.
    /// </summary>
    /// <remarks>
    /// Key differences modeled here:
    /// - partitionid (string) and ttlinseconds (int) are supported as ordinary attributes.
    /// - ExecuteTransactionRequest is not allowed when any target entity is elastic.
    /// - AssociateRequest / DisassociateRequest are not allowed when either side is elastic.
    /// - Bulk messages (CreateMultiple/UpdateMultiple/UpsertMultiple/DeleteMultiple) ARE allowed.
    /// - TTL is stored but NOT auto-purged. Use RemoveExpiredElasticRecords for opt-in cleanup.
    /// </remarks>
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// Registry of elastic table logical names (lower-cased for case-insensitive lookup).
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _elasticTables =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Marks a table as elastic. Elastic tables are Cosmos DB-backed and have
        /// different behavioral constraints (no transactions, no N:N relationships).
        /// </summary>
        /// <param name="logicalName">The logical name of the entity to mark as elastic.</param>
        public void MarkAsElasticTable(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
                throw new ArgumentNullException(nameof(logicalName));

            _elasticTables[logicalName.ToLowerInvariant()] = true;
        }

        /// <summary>
        /// Removes the elastic table designation from a table, reverting it to standard (SQL-backed).
        /// </summary>
        /// <param name="logicalName">The logical name of the entity to unmark.</param>
        public void MarkAsStandardTable(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
                throw new ArgumentNullException(nameof(logicalName));

            _elasticTables.TryRemove(logicalName.ToLowerInvariant(), out _);
        }

        /// <summary>
        /// Returns whether a table is registered as elastic.
        /// </summary>
        /// <param name="logicalName">The logical name of the entity to check.</param>
        /// <returns>True if the table is elastic; otherwise false.</returns>
        public bool IsElasticTable(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
                return false;

            return _elasticTables.ContainsKey(logicalName.ToLowerInvariant());
        }

        /// <summary>
        /// Inspects an EntityMetadata instance via reflection for a "TableType" property.
        /// If present and equal to "Elastic" (case-insensitive), registers the entity as elastic.
        /// This guard keeps compilation safe on older SDK surfaces where TableType may not exist.
        /// </summary>
        /// <param name="entityMetadata">The entity metadata to inspect.</param>
        internal void AutoDetectElasticTableFromMetadata(EntityMetadata entityMetadata)
        {
            if (entityMetadata == null) return;
            if (string.IsNullOrWhiteSpace(entityMetadata.LogicalName)) return;

            try
            {
                var tableTypeProp = entityMetadata.GetType()
                    .GetProperty("TableType", BindingFlags.Public | BindingFlags.Instance);

                if (tableTypeProp == null) return;

                var tableTypeValue = tableTypeProp.GetValue(entityMetadata) as string;
                if (string.Equals(tableTypeValue, "Elastic", StringComparison.OrdinalIgnoreCase))
                {
                    MarkAsElasticTable(entityMetadata.LogicalName);
                }
            }
            catch
            {
                // Reflection failures are silently swallowed — we never want metadata
                // inspection to break an otherwise valid test setup.
            }
        }

        /// <summary>
        /// Removes elastic records whose TTL has expired relative to the supplied UTC timestamp.
        /// Expiry is determined by: createdon + ttlinseconds &lt; asOfUtc.
        /// Only affects tables registered as elastic. Never called automatically.
        /// </summary>
        /// <param name="logicalName">The logical name of the elastic table to clean up.</param>
        /// <param name="asOfUtc">The reference UTC timestamp to evaluate TTL expiry against.</param>
        /// <returns>The number of records deleted.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the specified table is not registered as elastic.
        /// </exception>
        public int RemoveExpiredElasticRecords(string logicalName, DateTime asOfUtc)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
                throw new ArgumentNullException(nameof(logicalName));

            if (!IsElasticTable(logicalName))
                throw new InvalidOperationException(
                    $"Cannot remove expired records from '{logicalName}': table is not registered as elastic.");

            if (!Data.ContainsKey(logicalName))
                return 0;

            var entityStore = Data[logicalName];
            var toDelete = new List<Guid>();

            foreach (var kvp in entityStore)
            {
                var entity = kvp.Value;

                // Must have both createdon and ttlinseconds
                if (!entity.Attributes.ContainsKey("createdon")) continue;
                if (!entity.Attributes.ContainsKey("ttlinseconds")) continue;

                var createdOn = entity.GetAttributeValue<DateTime>("createdon");
                var ttl = entity.GetAttributeValue<int>("ttlinseconds");

                if (ttl <= 0) continue;

                var expiresAt = createdOn.AddSeconds(ttl);
                if (expiresAt < asOfUtc)
                {
                    toDelete.Add(kvp.Key);
                }
            }

            foreach (var id in toDelete)
            {
                entityStore.TryRemove(id, out _);
            }

            return toDelete.Count;
        }
    }
}

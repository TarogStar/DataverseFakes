using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace DataverseFakes
{
    /// <summary>
    /// Relationship cascade behavior support (metadata-driven). Currently simulates
    /// Delete cascade (Cascade / RemoveLink / Restrict / no-op) for 1:N relationships and a
    /// simple Assign cascade. Share, Unshare, Reparent and Merge cascades are NOT yet simulated.
    /// </summary>
    public partial class XrmFakedContext
    {
        /// <summary>
        /// Internal record describing a 1:N relationship's cascade behavior.
        /// Populated by <see cref="AddCascadeDeleteRelationship"/> and from initialized EntityMetadata.
        /// </summary>
        internal class CascadeRule
        {
            public string SchemaName { get; set; }
            public string ReferencedEntity { get; set; }
            public string ReferencingEntity { get; set; }
            public string ReferencingAttribute { get; set; }
            public CascadeType DeleteBehavior { get; set; }
            public CascadeType AssignBehavior { get; set; }
        }

        private readonly Dictionary<string, CascadeRule> CascadeRules =
            new Dictionary<string, CascadeRule>(StringComparer.OrdinalIgnoreCase);

        // Shared across one cascade chain so recursive child deletes terminate cycles.
        [ThreadStatic]
        private static HashSet<Guid> _cascadeVisited;

        /// <summary>
        /// Convenience helper to register the Delete cascade behavior of a 1:N relationship
        /// without building full EntityMetadata. The DeleteEntity logic consults these rules.
        /// </summary>
        /// <param name="schemaName">Relationship schema name (the store key).</param>
        /// <param name="referencedEntity">Parent ("one" side) entity logical name.</param>
        /// <param name="referencingEntity">Child ("many" side) entity logical name.</param>
        /// <param name="referencingAttribute">Lookup attribute on the child pointing at the parent.</param>
        /// <param name="deleteBehavior">Cascade behavior to apply when the parent is deleted.</param>
        public void AddCascadeDeleteRelationship(string schemaName, string referencedEntity,
            string referencingEntity, string referencingAttribute, CascadeType deleteBehavior)
        {
            RegisterCascadeRule(schemaName, referencedEntity, referencingEntity,
                referencingAttribute, deleteBehavior, CascadeType.NoCascade);
        }

        /// <summary>
        /// Registers or updates a cascade rule keyed by relationship schema name.
        /// </summary>
        internal void RegisterCascadeRule(string schemaName, string referencedEntity,
            string referencingEntity, string referencingAttribute,
            CascadeType deleteBehavior, CascadeType assignBehavior)
        {
            if (string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(referencedEntity)
                || string.IsNullOrEmpty(referencingEntity) || string.IsNullOrEmpty(referencingAttribute))
            {
                return; // Incomplete rule; ignore.
            }

            CascadeRules[schemaName] = new CascadeRule
            {
                SchemaName = schemaName,
                ReferencedEntity = referencedEntity,
                ReferencingEntity = referencingEntity,
                ReferencingAttribute = referencingAttribute,
                DeleteBehavior = deleteBehavior,
                AssignBehavior = assignBehavior
            };
        }

        /// <summary>
        /// Returns the cascade rules whose parent (referenced) entity matches and whose
        /// Delete behavior is something other than NoCascade.
        /// </summary>
        internal IEnumerable<CascadeRule> GetDeleteCascadeRulesFor(string referencedEntity)
        {
            return CascadeRules.Values.Where(r =>
                string.Equals(r.ReferencedEntity, referencedEntity, StringComparison.OrdinalIgnoreCase)
                && r.DeleteBehavior != CascadeType.NoCascade);
        }

        /// <summary>
        /// Returns assign cascade rules whose parent (referenced) entity matches and whose
        /// Assign behavior is Cascade / Active / UserOwned.
        /// </summary>
        internal IEnumerable<CascadeRule> GetAssignCascadeRulesFor(string referencedEntity)
        {
            return CascadeRules.Values.Where(r =>
                string.Equals(r.ReferencedEntity, referencedEntity, StringComparison.OrdinalIgnoreCase)
                && (r.AssignBehavior == CascadeType.Cascade
                    || r.AssignBehavior == CascadeType.Active
                    || r.AssignBehavior == CascadeType.UserOwned));
        }

        /// <summary>
        /// Finds the ids of child records that reference the given parent through the rule's lookup.
        /// </summary>
        internal List<Guid> FindCascadeChildren(CascadeRule rule, Guid parentId)
        {
            var result = new List<Guid>();
            ConcurrentDictionary<Guid, Entity> childTable;
            if (!Data.TryGetValue(rule.ReferencingEntity, out childTable) || childTable == null)
            {
                return result;
            }

            foreach (var child in childTable.Values)
            {
                var lookup = child.GetAttributeValue<EntityReference>(rule.ReferencingAttribute);
                if (lookup != null && lookup.Id == parentId)
                {
                    result.Add(child.Id);
                }
            }
            return result;
        }

        /// <summary>
        /// Entry point invoked by DeleteEntity. Marks the record as visited, runs Delete cascades,
        /// and manages the chain-scoped visited set so self-referential / cyclic relationships terminate.
        /// Returns true if this call owns (created) the visited set and is responsible for clearing it.
        /// </summary>
        internal bool RunDeleteCascades(EntityReference parentRef)
        {
            bool ownsChain = _cascadeVisited == null;
            if (ownsChain)
            {
                _cascadeVisited = new HashSet<Guid>();
            }

            _cascadeVisited.Add(parentRef.Id);

            try
            {
                ApplyDeleteCascades(parentRef, _cascadeVisited);
            }
            catch
            {
                // On failure (e.g. Restrict), reset the chain so a subsequent delete starts clean.
                if (ownsChain)
                {
                    _cascadeVisited = null;
                }
                throw;
            }

            return ownsChain;
        }

        /// <summary>
        /// Clears the chain-scoped visited set. Call only from the outermost delete (ownsChain == true).
        /// </summary>
        internal void EndDeleteCascadeChain()
        {
            _cascadeVisited = null;
        }

        /// <summary>
        /// Applies Delete cascade rules for a parent record about to be deleted.
        /// Restrict is validated (and throws) BEFORE any mutation; Cascade recursively deletes
        /// children through the organization service so their own cascade + pipeline fire;
        /// RemoveLink nulls the child lookup. The visited set terminates self-referential cycles.
        /// </summary>
        internal void ApplyDeleteCascades(EntityReference parentRef, HashSet<Guid> visited)
        {
            var rules = GetDeleteCascadeRulesFor(parentRef.LogicalName).ToList();
            if (rules.Count == 0)
            {
                return; // Inert: no cascade rules => default behavior unchanged.
            }

            // Validate Restrict for ALL rules BEFORE mutating anything.
            foreach (var rule in rules.Where(r => r.DeleteBehavior == CascadeType.Restrict))
            {
                if (FindCascadeChildren(rule, parentRef.Id).Count > 0)
                {
                    throw new FaultException<OrganizationServiceFault>(
                        new OrganizationServiceFault(),
                        $"The {parentRef.LogicalName} record cannot be deleted because it is referenced by " +
                        $"one or more {rule.ReferencingEntity} records (relationship {rule.SchemaName}, Restrict).");
                }
            }

            var service = GetOrganizationService();

            foreach (var rule in rules)
            {
                var childIds = FindCascadeChildren(rule, parentRef.Id);
                foreach (var childId in childIds)
                {
                    if (rule.DeleteBehavior == CascadeType.Cascade)
                    {
                        // Cycle guard: skip records already in the active delete chain.
                        if (visited.Contains(childId))
                        {
                            continue;
                        }
                        visited.Add(childId);

                        // Child may already have been removed by another cascade branch.
                        ConcurrentDictionary<Guid, Entity> childTable;
                        if (Data.TryGetValue(rule.ReferencingEntity, out childTable)
                            && childTable.ContainsKey(childId))
                        {
                            service.Delete(rule.ReferencingEntity, childId);
                        }
                    }
                    else if (rule.DeleteBehavior == CascadeType.RemoveLink)
                    {
                        var update = new Entity(rule.ReferencingEntity) { Id = childId };
                        update[rule.ReferencingAttribute] = null;
                        service.Update(update);
                    }
                    // Active / UserOwned => no delete-time action.
                }
            }
        }

        /// <summary>
        /// Cascades a new owner to child records for 1:N relationships configured to cascade Assign.
        /// Updates each child through an AssignRequest (so deeper cascade fires).
        /// </summary>
        internal void ApplyAssignCascade(EntityReference parentRef, EntityReference newOwner)
        {
            if (newOwner == null)
            {
                return;
            }

            var rules = GetAssignCascadeRulesFor(parentRef.LogicalName).ToList();
            if (rules.Count == 0)
            {
                return; // Inert.
            }

            var service = GetOrganizationService();
            foreach (var rule in rules)
            {
                foreach (var childId in FindCascadeChildren(rule, parentRef.Id))
                {
                    service.Execute(new Microsoft.Crm.Sdk.Messages.AssignRequest
                    {
                        Target = new EntityReference(rule.ReferencingEntity, childId),
                        Assignee = newOwner
                    });
                }
            }
        }
    }
}

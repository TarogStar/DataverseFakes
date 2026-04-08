using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace FakeXrmEasy.Extensions
{
    /// <summary>
    /// Extension helpers that add convenience operations to <see cref="QueryExpression"/> instances.
    /// </summary>
    public static class QueryExpressionExtensions
    {
        /// <summary>
        /// Resolves the logical entity name associated with a FetchXML alias in a query.
        /// Searches recursively through all LinkEntities including nested ones.
        /// Also matches by LinkToEntityName (logical name) when no explicit alias is set.
        /// </summary>
        /// <param name="qe">The query expression to inspect.</param>
        /// <param name="sAlias">The alias to resolve. Can be an explicit alias, entity logical name, or prefixed attribute (e.g., "contact.fullname").</param>
        /// <returns>The linked entity logical name when found; otherwise the alias itself.</returns>
        public static string GetEntityNameFromAlias(this QueryExpression qe, string sAlias)
        {
            if (sAlias == null)
                return qe.EntityName;

            // Handle case where alias contains a dot (e.g., "contact.fullname") - extract the entity portion
            var aliasToSearch = sAlias;
            if (sAlias.Contains("."))
            {
                aliasToSearch = sAlias.Split('.')[0];
            }

            // Check if the alias matches the root entity name
            if (aliasToSearch.Equals(qe.EntityName))
            {
                return qe.EntityName;
            }

            // Search recursively through all LinkEntities
            var linkedEntity = FindLinkEntityByAliasRecursive(qe.LinkEntities, aliasToSearch);

            if (linkedEntity != null)
            {
                return linkedEntity.LinkToEntityName;
            }

            // If the alias wasn't found by explicit alias, try to match by entity logical name
            // This handles cases where FetchXML uses entityname attribute without an explicit alias
            linkedEntity = FindLinkEntityByLogicalNameRecursive(qe.LinkEntities, aliasToSearch);

            if (linkedEntity != null)
            {
                return linkedEntity.LinkToEntityName;
            }

            // If still not found, return the alias as-is (it could be an entity name reference)
            return sAlias;
        }

        /// <summary>
        /// Recursively searches for a LinkEntity by its EntityAlias.
        /// </summary>
        /// <param name="linkEntities">The collection of LinkEntities to search.</param>
        /// <param name="alias">The alias to find.</param>
        /// <returns>The matching LinkEntity or null if not found.</returns>
        private static LinkEntity FindLinkEntityByAliasRecursive(DataCollection<LinkEntity> linkEntities, string alias)
        {
            if (linkEntities == null)
                return null;

            foreach (var le in linkEntities)
            {
                // Check if this LinkEntity's alias matches
                if (le.EntityAlias != null && le.EntityAlias.Equals(alias))
                {
                    return le;
                }

                // Search nested LinkEntities recursively
                var nested = FindLinkEntityByAliasRecursive(le.LinkEntities, alias);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively searches for a LinkEntity by its LinkToEntityName (logical name).
        /// This is used when FetchXML uses the entityname attribute without an explicit alias.
        /// </summary>
        /// <param name="linkEntities">The collection of LinkEntities to search.</param>
        /// <param name="entityName">The entity logical name to find.</param>
        /// <returns>The first matching LinkEntity or null if not found.</returns>
        private static LinkEntity FindLinkEntityByLogicalNameRecursive(DataCollection<LinkEntity> linkEntities, string entityName)
        {
            if (linkEntities == null)
                return null;

            foreach (var le in linkEntities)
            {
                // Check if this LinkEntity's logical name matches (only if no explicit alias is set)
                if (string.IsNullOrEmpty(le.EntityAlias) && le.LinkToEntityName.Equals(entityName))
                {
                    return le;
                }

                // Also check if the LinkToEntityName matches the alias directly
                // This handles cases where the alias is the entity name itself
                if (le.LinkToEntityName.Equals(entityName))
                {
                    return le;
                }

                // Search nested LinkEntities recursively
                var nested = FindLinkEntityByLogicalNameRecursive(le.LinkEntities, entityName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a deep clone of the query expression so callers can mutate the copy safely.
        /// </summary>
        /// <param name="qe">The source query expression.</param>
        /// <returns>A new <see cref="QueryExpression"/> that contains the same structure and values.</returns>
        public static QueryExpression Clone(this QueryExpression qe)
        {
            return qe.Copy();
        }
    }
}
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FakeXrmEasy.Extensions;
using FakeXrmEasy.Extensions.FetchXml;
using FakeXrmEasy.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace FakeXrmEasy
{
    /// <summary>
    /// Partial class containing query translation functionality for the XrmFakedContext.
    /// This portion handles QueryExpression, FetchXML, and LINQ query translation to enable
    /// in-memory querying of CRM entities during unit testing.
    /// </summary>
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// Finds the early-bound type for a given entity logical name by searching all registered proxy type assemblies.
        /// </summary>
        /// <param name="logicalName">The logical name of the CRM entity to find the reflected type for.</param>
        /// <returns>The early-bound Type if found in exactly one assembly; null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the entity type is defined in multiple assemblies.</exception>
        protected internal Type FindReflectedType(string logicalName)
        {
            var types =
                ProxyTypesAssemblies.Select(a => FindReflectedType(logicalName, a))
                                    .Where(t => t != null);

            if (types.Count() > 1)
            {
                var errorMsg = $"Type { logicalName } is defined in multiple assemblies: ";
                foreach (var type in types)
                {
                    errorMsg += type.Assembly
                                    .GetName()
                                    .Name + "; ";
                }
                var lastIndex = errorMsg.LastIndexOf("; ");
                errorMsg = errorMsg.Substring(0, lastIndex) + ".";
                throw new InvalidOperationException(errorMsg);
            }

            return types.SingleOrDefault();
        }

        /// <summary>
        /// Finds reflected type for given entity from given assembly.
        /// </summary>
        /// <param name="logicalName">
        /// Entity logical name which type is searched from given
        /// <paramref name="assembly"/>.
        /// </param>
        /// <param name="assembly">
        /// Assembly where early-bound type is searched for given
        /// <paramref name="logicalName"/>.
        /// </param>
        /// <returns>
        /// Early-bound type of <paramref name="logicalName"/> if it's found
        /// from <paramref name="assembly"/>. Otherwise null is returned.
        /// </returns>
        private static Type FindReflectedType(string logicalName,
                                              Assembly assembly)
        {
            try
            {
                if (assembly == null)
                {
                    throw new ArgumentNullException(nameof(assembly));
                }

                /* This wasn't building within the CI FAKE build script...
                var subClassType = assembly.GetTypes()
                        .Where(t => typeof(Entity).IsAssignableFrom(t))
                        .Where(t => t.GetCustomAttributes<EntityLogicalNameAttribute>(true).Any())
                        .FirstOrDefault(t => t.GetCustomAttributes<EntityLogicalNameAttribute>(true).First().LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));

                */
                var subClassType = assembly.GetTypes()
                        .Where(t => typeof(Entity).IsAssignableFrom(t))
                        .Where(t => t.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true).Length > 0)
                        .Where(t => ((EntityLogicalNameAttribute)t.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true)[0]).LogicalName.Equals(logicalName.ToLower()))
                        .FirstOrDefault();

                return subClassType;
            }
            catch (ReflectionTypeLoadException exception)
            {
                // now look at ex.LoaderExceptions - this is an Exception[], so:
                var s = "";
                foreach (var innerException in exception.LoaderExceptions)
                {
                    // write details of "inner", in particular inner.Message
                    s += innerException.Message + " ";
                }

                throw new Exception("XrmFakedContext.FindReflectedType: " + s);
            }
        }

        /// <summary>
        /// Finds the attribute type from injected entity metadata for a given entity and attribute name.
        /// </summary>
        /// <param name="sEntityName">The logical name of the CRM entity.</param>
        /// <param name="sAttributeName">The logical name of the attribute to find.</param>
        /// <returns>The CLR Type corresponding to the attribute's CRM attribute type, or null if not found.</returns>
        /// <exception cref="Exception">Thrown for unsupported attribute types like CalendarRules or Virtual.</exception>
        protected internal Type FindAttributeTypeInInjectedMetadata(string sEntityName, string sAttributeName)
        {
            if (!EntityMetadata.ContainsKey(sEntityName))
                return null;

            if (EntityMetadata[sEntityName].Attributes == null)
                return null;

            var attribute = EntityMetadata[sEntityName].Attributes
                                .Where(a => a.LogicalName == sAttributeName)
                                .FirstOrDefault();

            if (attribute == null)
                return null;

            if (attribute.AttributeType == null)
                return null;

            switch (attribute.AttributeType.Value)
            {
                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.BigInt:
                    return typeof(long);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Integer:
                    return typeof(int);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Boolean:
                    return typeof(bool);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.CalendarRules:
                    throw new Exception("CalendarRules: Type not yet supported");

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Lookup:
                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Customer:
                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Owner:
                    return typeof(EntityReference);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.DateTime:
                    return typeof(DateTime);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Decimal:
                    return typeof(decimal);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Double:
                    return typeof(double);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.EntityName:
                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Memo:
                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.String:
                    return typeof(string);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Money:
                    return typeof(Money);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.PartyList:
                    return typeof(EntityReferenceCollection);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Picklist:
                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.State:
                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Status:
                    return typeof(OptionSetValue);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Uniqueidentifier:
                    return typeof(Guid);

                case Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode.Virtual:
#if FAKE_XRM_EASY_9
                    if (attribute.AttributeTypeName.Value == "MultiSelectPicklistType")
                    {
                        return typeof(OptionSetValueCollection);
                    }
#endif
                    throw new Exception("Virtual: Type not yet supported");

                default:
                    return typeof(string);

            }

        }
        /// <summary>
        /// Finds the CLR type of an attribute by reflecting on the early-bound entity type or falling back to injected metadata.
        /// Handles special cases like EntityReference name attributes and enum types.
        /// </summary>
        /// <param name="earlyBoundType">The early-bound CLR Type of the CRM entity.</param>
        /// <param name="sEntityName">The logical name of the CRM entity.</param>
        /// <param name="attributeName">The logical name of the attribute to find.</param>
        /// <returns>The CLR Type of the attribute.</returns>
        /// <exception cref="Exception">Thrown when the attribute cannot be found in the early-bound type or injected metadata.</exception>
        protected internal Type FindReflectedAttributeType(Type earlyBoundType, string sEntityName, string attributeName)
        {
            //Get that type properties
            var attributeInfo = GetEarlyBoundTypeAttribute(earlyBoundType, attributeName);
            if (attributeInfo == null && attributeName.EndsWith("name"))
            {
                // Special case for referencing the name of a EntityReference
                attributeName = attributeName.Substring(0, attributeName.Length - 4);
                attributeInfo = GetEarlyBoundTypeAttribute(earlyBoundType, attributeName);

                if (attributeInfo.PropertyType != typeof(EntityReference))
                {
                    // Don't mess up if other attributes follow this naming pattern
                    attributeInfo = null;
                }
            }

            if (attributeInfo == null || attributeInfo.PropertyType.FullName == null)
            {
                //Try with metadata
                var injectedType = FindAttributeTypeInInjectedMetadata(sEntityName, attributeName);

                if (injectedType == null)
                {
                    throw new Exception($"XrmFakedContext.FindReflectedAttributeType: Attribute {attributeName} not found for type {earlyBoundType}");
                }

                return injectedType;
            }

            if (attributeInfo.PropertyType.FullName.EndsWith("Enum") || attributeInfo.PropertyType.BaseType.FullName.EndsWith("Enum"))
            {
                return typeof(int);
            }

            if (!attributeInfo.PropertyType.FullName.StartsWith("System."))
            {
                try
                {
                    var instance = Activator.CreateInstance(attributeInfo.PropertyType);
                    if (instance is Entity)
                    {
                        return typeof(EntityReference);
                    }
                }
                catch
                {
                    // ignored
                }
            }
#if FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
            else if (attributeInfo.PropertyType.FullName.StartsWith("System.Nullable"))
            {
                return attributeInfo.PropertyType.GenericTypeArguments[0];
            }
#endif

            return attributeInfo.PropertyType;
        }

        private static PropertyInfo GetEarlyBoundTypeAttribute(Type earlyBoundType, string attributeName)
        {
            var attributeInfo = earlyBoundType.GetProperties()
                .Where(pi => pi.GetCustomAttributes(typeof(AttributeLogicalNameAttribute), true).Length > 0)
                .Where(pi => (pi.GetCustomAttributes(typeof(AttributeLogicalNameAttribute), true)[0] as AttributeLogicalNameAttribute).LogicalName.Equals(attributeName))
                .FirstOrDefault();

            return attributeInfo;
        }

        /// <summary>
        /// Creates a LINQ queryable for entities of the specified logical name using late-bound Entity type.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the CRM entity to query.</param>
        /// <returns>An IQueryable of Entity objects that can be used to build LINQ queries against the in-memory data store.</returns>
        public IQueryable<Entity> CreateQuery(string entityLogicalName)
        {
            return this.CreateQuery<Entity>(entityLogicalName);
        }

        /// <summary>
        /// Creates a LINQ queryable for entities of the specified early-bound type.
        /// Automatically infers the entity logical name from the EntityLogicalNameAttribute on the type.
        /// </summary>
        /// <typeparam name="T">The early-bound entity type that inherits from Entity and has an EntityLogicalNameAttribute.</typeparam>
        /// <returns>An IQueryable of the specified entity type that can be used to build LINQ queries against the in-memory data store.</returns>
        public IQueryable<T> CreateQuery<T>()
            where T : Entity
        {
            var typeParameter = typeof(T);

            if (ProxyTypesAssembly == null)
            {
                //Try to guess proxy types assembly
                var assembly = Assembly.GetAssembly(typeof(T));
                if (assembly != null)
                {
                    ProxyTypesAssembly = assembly;
                }
            }

            var logicalName = "";

            if (typeParameter.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true).Length > 0)
            {
                logicalName = (typeParameter.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true)[0] as EntityLogicalNameAttribute).LogicalName;
            }

            return this.CreateQuery<T>(logicalName);
        }

        /// <summary>
        /// Creates a LINQ queryable for entities of the specified type and logical name.
        /// Clones entities from the in-memory data store to the appropriate type before returning.
        /// </summary>
        /// <typeparam name="T">The entity type to return, either Entity or an early-bound type.</typeparam>
        /// <param name="entityLogicalName">The logical name of the CRM entity to query.</param>
        /// <returns>An IQueryable of the specified type containing cloned entities from the in-memory store.</returns>
        /// <exception cref="Exception">Thrown when the entity type cannot be found and T is not the base Entity type.</exception>
        protected IQueryable<T> CreateQuery<T>(string entityLogicalName)
            where T : Entity
        {
            var subClassType = FindReflectedType(entityLogicalName);
            if (subClassType == null && !(typeof(T) == typeof(Entity)) || (typeof(T) == typeof(Entity) && string.IsNullOrWhiteSpace(entityLogicalName)))
            {
                throw new Exception($"The type {entityLogicalName} was not found");
            }

            var lst = new List<T>();
            ConcurrentDictionary<Guid, Entity> entityDict;
            if (!Data.TryGetValue(entityLogicalName, out entityDict))
            {
                return lst.AsQueryable(); //Empty list
            }

            foreach (var e in entityDict.Values)
            {
                if (subClassType != null)
                {
                    var cloned = e.Clone(subClassType);
                    lst.Add((T)cloned);
                }
                else
                    lst.Add((T)e.Clone());
            }

            return lst.AsQueryable();
        }

        /// <summary>
        /// Creates a LINQ queryable directly from the internal data store without cloning.
        /// Unlike CreateQuery, this returns references to the actual stored entities.
        /// </summary>
        /// <param name="entityName">The logical name of the CRM entity to query.</param>
        /// <returns>An IQueryable of Entity objects directly from the in-memory data store.</returns>
        public IQueryable<Entity> CreateQueryFromEntityName(string entityName)
        {
            ConcurrentDictionary<Guid, Entity> entityDict;
            if (Data.TryGetValue(entityName, out entityDict))
            {
                return entityDict.Values.AsQueryable();
            }
            return Enumerable.Empty<Entity>().AsQueryable();
        }

        /// <summary>
        /// Translates a LinkEntity into a LINQ join operation on the query.
        /// Supports Inner and LeftOuter join operators and recursively processes nested LinkEntities.
        /// </summary>
        /// <param name="context">The XrmFakedContext containing metadata and proxy type information.</param>
        /// <param name="le">The LinkEntity defining the join relationship between entities.</param>
        /// <param name="query">The current LINQ query to add the join to.</param>
        /// <param name="previousColumnSet">The ColumnSet from the parent query or LinkEntity.</param>
        /// <param name="linkedEntities">Dictionary tracking linked entity aliases to ensure uniqueness.</param>
        /// <param name="linkFromAlias">Optional alias of the entity being joined from.</param>
        /// <param name="linkFromEntity">Optional logical name of the entity being joined from.</param>
        /// <returns>The query with the join operation applied.</returns>
        /// <exception cref="PullRequestException">Thrown when an unsupported JoinOperator is used.</exception>
        public static IQueryable<Entity> TranslateLinkedEntityToLinq(XrmFakedContext context, LinkEntity le, IQueryable<Entity> query, ColumnSet previousColumnSet, Dictionary<string, int> linkedEntities, string linkFromAlias = "", string linkFromEntity = "")
        {
            if (!string.IsNullOrEmpty(le.EntityAlias))
            {
                if (!Regex.IsMatch(le.EntityAlias, "^[A-Za-z_](\\w|\\.)*$", RegexOptions.ECMAScript))
                {
                FakeOrganizationServiceFault.Throw(ErrorCodes.QueryBuilderInvalid_Alias, $"Invalid character specified for alias: {le.EntityAlias}. Only characters within the ranges [A-Z], [a-z] or [0-9] or _ are allowed.  The first character may only be in the ranges [A-Z], [a-z] or _.");
                }
            }

            var leAlias = string.IsNullOrWhiteSpace(le.EntityAlias) ? le.LinkToEntityName : le.EntityAlias;
            context.EnsureEntityNameExistsInMetadata(le.LinkFromEntityName != linkFromAlias ? le.LinkFromEntityName : linkFromEntity);
            context.EnsureEntityNameExistsInMetadata(le.LinkToEntityName);

            if (!context.AttributeExistsInMetadata(le.LinkToEntityName, le.LinkToAttributeName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.QueryBuilderNoAttribute, string.Format("The attribute {0} does not exist on this entity.", le.LinkToAttributeName));
            }

            // Validate LinkFromAttributeName - determine the actual entity we're linking from
            var actualLinkFromEntity = le.LinkFromEntityName != linkFromAlias ? le.LinkFromEntityName : linkFromEntity;
            if (!string.IsNullOrWhiteSpace(actualLinkFromEntity) && !context.AttributeExistsInMetadata(actualLinkFromEntity, le.LinkFromAttributeName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.QueryBuilderNoAttribute, string.Format("The attribute {0} does not exist on this entity.", le.LinkFromAttributeName));
            }

            IQueryable<Entity> inner = null;
            if (le.JoinOperator == JoinOperator.LeftOuter)
            {
                //filters are applied in the inner query and then ignored during filter evaluation
                // NOTE: We only pre-apply LinkCriteria filters here, NOT nested LinkEntities
                // Nested entities are processed recursively later to maintain flat alias structure
                var outerQueryExpression = new QueryExpression()
                {
                    EntityName = le.LinkToEntityName,
                    Criteria = le.LinkCriteria,
                    ColumnSet = new ColumnSet(true)
                };

                var outerQuery = TranslateQueryExpressionToLinq(context, outerQueryExpression);
                inner = outerQuery;

            }
            else
            {
                //Filters are applied after joins
                inner = context.CreateQuery<Entity>(le.LinkToEntityName);
            }

            //if (!le.Columns.AllColumns && le.Columns.Columns.Count == 0)
            //{
            //    le.Columns.AllColumns = true;   //Add all columns in the joined entity, otherwise we can't filter by related attributes, then the Select will actually choose which ones we need
            //}

            if (string.IsNullOrWhiteSpace(linkFromAlias))
            {
                linkFromAlias = le.LinkFromAttributeName;
            }
            else
            {
                linkFromAlias += "." + le.LinkFromAttributeName;
            }

            switch (le.JoinOperator)
            {
                case JoinOperator.Inner:
                case JoinOperator.Natural:
                    query = query.Join(inner,
                                    outerKey => outerKey.KeySelector(linkFromAlias, context),
                                    innerKey => innerKey.KeySelector(le.LinkToAttributeName, context),
                                    (outerEl, innerEl) => outerEl.Clone(outerEl.GetType(), context).JoinAttributes(innerEl, new ColumnSet(true), leAlias, context));

                    break;
                case JoinOperator.LeftOuter:
                    query = query.GroupJoin(inner,
                                    outerKey => outerKey.KeySelector(linkFromAlias, context),
                                    innerKey => innerKey.KeySelector(le.LinkToAttributeName, context),
                                    (outerEl, innerElemsCol) => new { outerEl, innerElemsCol })
                                                .SelectMany(x => x.innerElemsCol.DefaultIfEmpty()
                                                            , (x, y) => x.outerEl
                                                                            .JoinAttributes(y, new ColumnSet(true), leAlias, context));


                    break;

                case JoinOperator.Any:
                case JoinOperator.NotAny:
                case JoinOperator.All:
                case JoinOperator.NotAll:
                    // Any/All operators use existence checks rather than actual joins
                    // They filter the parent entity based on existence/non-existence of related records
                    query = TranslateAnyAllLinkedEntityToLinq(context, le, query, linkFromAlias);
                    // Any/All link entities cannot have nested link-entities, so we return early
                    return query;

                default: //This shouldn't be reached as there are only 3 types of Join...
                    throw new PullRequestException(string.Format("The join operator {0} is currently not supported. Feel free to implement and send a PR.", le.JoinOperator));

            }

            // Process nested linked entities recursively
            // This maintains flat alias structure (e.g., pet1.childid, not child1.pet1.childid)
            foreach (var nestedLinkedEntity in le.LinkEntities)
            {
                if (string.IsNullOrWhiteSpace(le.EntityAlias))
                {
                    le.EntityAlias = le.LinkToEntityName;
                }

                if (string.IsNullOrWhiteSpace(nestedLinkedEntity.EntityAlias))
                {
                    nestedLinkedEntity.EntityAlias = EnsureUniqueLinkedEntityAlias(linkedEntities, nestedLinkedEntity.LinkToEntityName);
                }

                query = TranslateLinkedEntityToLinq(context, nestedLinkedEntity, query, le.Columns, linkedEntities, le.EntityAlias, le.LinkToEntityName);
            }

            return query;
        }

        /// <summary>
        /// Translates Any/All/NotAny/NotAll link entity operators to LINQ existence checks.
        /// These operators filter the parent entity based on existence/non-existence of related records
        /// that match the filter criteria in the link-entity.
        ///
        /// Constraints from real Dataverse:
        /// - Any/All LinkEntity CAN ONLY be a direct child of the root entity (top level)
        /// - Any/All LinkEntity CANNOT have link-entity as parent (no nesting)
        /// - Any/All LinkEntity CANNOT have nested child link-entities
        /// - No attributes/columns allowed on Any/All link-entities
        /// - Filters ARE supported inside Any/All link-entities
        ///
        /// Semantics:
        /// - Any: Records that have AT LEAST ONE matching related record
        /// - NotAny: Records that have NO matching related records
        /// - All: Records where ALL related records match the filter (or no related records exist)
        ///        Implemented as NOT EXISTS (related records that DON'T match)
        /// - NotAll: Records where AT LEAST ONE related record does NOT match the filter
        /// </summary>
        /// <param name="context">The XrmFakedContext containing metadata and in-memory data.</param>
        /// <param name="le">The LinkEntity with Any/All operator.</param>
        /// <param name="query">The current LINQ query to filter.</param>
        /// <param name="linkFromAlias">The attribute path for the join key on the parent entity.</param>
        /// <returns>The filtered query.</returns>
        private static IQueryable<Entity> TranslateAnyAllLinkedEntityToLinq(
            XrmFakedContext context,
            LinkEntity le,
            IQueryable<Entity> query,
            string linkFromAlias)
        {
            // Get the related entity collection from in-memory data
            IQueryable<Entity> relatedEntities = context.CreateQuery<Entity>(le.LinkToEntityName);

            // Apply the filter from the link-entity if present
            if (le.LinkCriteria != null && (le.LinkCriteria.Conditions.Count > 0 || le.LinkCriteria.Filters.Count > 0))
            {
        // Create a temporary QueryExpression to translate the filter
        var filterQe = new QueryExpression(le.LinkToEntityName)
        {
          Criteria = le.LinkCriteria
        };

        // Create parameter for related entity
        ParameterExpression relatedEntityParam = Expression.Parameter(typeof(Entity), "relatedEntity");

                // Translate the filter expression
                var filterExpression = TranslateFilterExpressionToExpression(
                    filterQe, context, le.LinkToEntityName, le.LinkCriteria, relatedEntityParam, false);

                // Create the filter lambda
                Expression<Func<Entity, bool>> filterLambda =
                    Expression.Lambda<Func<Entity, bool>>(filterExpression, relatedEntityParam);

                // Apply the filter to related entities
                relatedEntities = relatedEntities.Where(filterLambda);
            }

            // Get the link attribute names for the key comparison
            var parentKeyAttribute = linkFromAlias;
            var relatedKeyAttribute = le.LinkToAttributeName.ToLower();

            // Materialize the filtered related entities for efficient lookups
            // We need to do this because LINQ to Objects doesn't support complex nested queries on IQueryable
            var filteredRelatedList = relatedEntities.ToList();

            // Build a lookup of related entity keys for efficient matching
            var filteredRelatedKeys = new HashSet<object>(
                filteredRelatedList.Select(e => e.KeySelector(relatedKeyAttribute, context))
                                   .Where(k => k != null)
                                   .Select(k => NormalizeKey(k)));

            // Build the existence check based on the join operator
            switch (le.JoinOperator)
            {
                case JoinOperator.Any:
                    // Any: Records that have AT LEAST ONE matching related record
                    // Filter parent records where there exists a matching related record
                    query = query.Where(parentEntity =>
                        filteredRelatedKeys.Contains(NormalizeKey(parentEntity.KeySelector(parentKeyAttribute, context))));
                    break;

                case JoinOperator.NotAny:
                    // NotAny: Records that have NO matching related records
                    // Filter parent records where NO matching related record exists
                    query = query.Where(parentEntity =>
                        !filteredRelatedKeys.Contains(NormalizeKey(parentEntity.KeySelector(parentKeyAttribute, context))));
                    break;

                case JoinOperator.All:
                    // All: Records where ALL related records match the filter (or no related records exist)
                    // This is equivalent to: NOT EXISTS (related records that DON'T match the filter)
                    // Since we've already filtered relatedEntities to only matching records,
                    // we need to check that there are no UNfiltered related records that aren't in our filtered set

                    // Get all related entities (unfiltered) and build lookup
                    var allRelatedList = context.CreateQuery<Entity>(le.LinkToEntityName).ToList();

                    // Build a dictionary from parent key to count of all related records
                    var allRelatedCountByParentKey = allRelatedList
                        .GroupBy(e => NormalizeKey(e.KeySelector(relatedKeyAttribute, context)))
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Build a dictionary from parent key to count of filtered (matching) related records
                    var filteredCountByParentKey = filteredRelatedList
                        .GroupBy(e => NormalizeKey(e.KeySelector(relatedKeyAttribute, context)))
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Use AsEnumerable() to switch to in-memory LINQ for complex predicate
                    query = query.AsEnumerable().Where(parentEntity =>
                        EvaluateAllOperator(parentEntity, parentKeyAttribute, context,
                            allRelatedCountByParentKey, filteredCountByParentKey)).AsQueryable();
                    break;

                case JoinOperator.NotAll:
                    // NotAll: Records where AT LEAST ONE related record does NOT match the filter
                    // This is the inverse of All: EXISTS (related records that DON'T match the filter)

                    // Get all related entities (unfiltered) and build lookup
                    var allRelatedForNotAllList = context.CreateQuery<Entity>(le.LinkToEntityName).ToList();

                    // Build a dictionary from parent key to count of all related records
                    var allCountByKey = allRelatedForNotAllList
                        .GroupBy(e => NormalizeKey(e.KeySelector(relatedKeyAttribute, context)))
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Build a dictionary from parent key to count of filtered (matching) related records
                    var matchingCountByKey = filteredRelatedList
                        .GroupBy(e => NormalizeKey(e.KeySelector(relatedKeyAttribute, context)))
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Use AsEnumerable() to switch to in-memory LINQ for complex predicate
                    query = query.AsEnumerable().Where(parentEntity =>
                        EvaluateNotAllOperator(parentEntity, parentKeyAttribute, context,
                            allCountByKey, matchingCountByKey)).AsQueryable();
                    break;
            }

            return query;
        }

        /// <summary>
        /// Normalizes a key value for use in HashSet comparisons.
        /// Extracts the underlying comparable value from complex types like EntityReference.
        /// Used by Any/All operators for efficient key matching.
        /// </summary>
        /// <param name="key">The key value to normalize.</param>
        /// <returns>A normalized key value suitable for HashSet operations.</returns>
        private static object NormalizeKey(object key)
        {
            if (key == null)
                return null;

            // Extract Id from EntityReference for consistent comparison
            if (key is EntityReference er)
                return er.Id;

            return key;
        }

        /// <summary>
        /// Evaluates the "All" operator for a parent entity.
        /// Returns true if all related records match the filter (or no related records exist).
        /// </summary>
        private static bool EvaluateAllOperator(
            Entity parentEntity,
            string parentKeyAttribute,
            XrmFakedContext context,
            Dictionary<object, int> allRelatedCountByKey,
            Dictionary<object, int> filteredCountByKey)
        {
            var parentKey = NormalizeKey(parentEntity.KeySelector(parentKeyAttribute, context));

            // Get count of all related records for this parent
            var allCount = allRelatedCountByKey.ContainsKey(parentKey)
                ? allRelatedCountByKey[parentKey]
                : 0;

            // Get count of matching (filtered) related records
            var matchingCount = filteredCountByKey.ContainsKey(parentKey)
                ? filteredCountByKey[parentKey]
                : 0;

            // All: either no related records exist, or all related records match the filter
            return allCount == matchingCount;
        }

        /// <summary>
        /// Evaluates the "NotAll" operator for a parent entity.
        /// Returns true if at least one related record does not match the filter.
        /// </summary>
        private static bool EvaluateNotAllOperator(
            Entity parentEntity,
            string parentKeyAttribute,
            XrmFakedContext context,
            Dictionary<object, int> allCountByKey,
            Dictionary<object, int> matchingCountByKey)
        {
            var parentKey = NormalizeKey(parentEntity.KeySelector(parentKeyAttribute, context));

            // Get count of all related records for this parent
            var allCount = allCountByKey.ContainsKey(parentKey)
                ? allCountByKey[parentKey]
                : 0;

            // Get count of matching (filtered) related records
            var matchingCount = matchingCountByKey.ContainsKey(parentKey)
                ? matchingCountByKey[parentKey]
                : 0;

            // NotAll: at least one related record doesn't match
            // There must be related records AND count of all related != count of matching
            return allCount > 0 && allCount != matchingCount;
        }

        private static string EnsureUniqueLinkedEntityAlias(IDictionary<string, int> linkedEntities, string entityName)
        {
            if (linkedEntities.ContainsKey(entityName))
            {
                linkedEntities[entityName]++;
            }
            else
            {
                linkedEntities[entityName] = 1;
            }

            return $"{entityName}{linkedEntities[entityName]}";
        }


        /// <summary>
        /// Retrieves a specific node from a FetchXML document
        /// </summary>
        /// <param name="xlDoc">The FetchXML document</param>
        /// <param name="sName">The name of the node to retrieve</param>
        /// <returns>The requested XElement or null if not found</returns>
        protected static XElement RetrieveFetchXmlNode(XDocument xlDoc, string sName)
        {
            return xlDoc.Descendants().Where(e => e.Name.LocalName.Equals(sName)).FirstOrDefault();
        }

        /// <summary>
        /// Parses a FetchXML string into an XDocument for further processing.
        /// </summary>
        /// <param name="fetchXml">The FetchXML query string to parse.</param>
        /// <returns>An XDocument representation of the FetchXML.</returns>
        /// <exception cref="Exception">Thrown when the FetchXML is not valid XML.</exception>
        public static XDocument ParseFetchXml(string fetchXml)
        {
            try
            {
                return XDocument.Parse(fetchXml);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("FetchXml must be a valid XML document: {0}", ex.ToString()));
            }
        }

        /// <summary>
        /// Translates a FetchXML query string into a QueryExpression.
        /// </summary>
        /// <param name="context">The XrmFakedContext for metadata resolution.</param>
        /// <param name="fetchXml">The FetchXML query string to translate.</param>
        /// <returns>A QueryExpression equivalent to the FetchXML query.</returns>
        public static QueryExpression TranslateFetchXmlToQueryExpression(XrmFakedContext context, string fetchXml)
        {
            return TranslateFetchXmlDocumentToQueryExpression(context, ParseFetchXml(fetchXml));
        }

        /// <summary>
        /// Translates a parsed FetchXML XDocument into a QueryExpression.
        /// Handles columns, filters, ordering, paging, distinct, and linked entities.
        /// </summary>
        /// <param name="context">The XrmFakedContext for metadata resolution.</param>
        /// <param name="xlDoc">The parsed FetchXML document.</param>
        /// <returns>A QueryExpression equivalent to the FetchXML document.</returns>
        /// <exception cref="Exception">Thrown when the FetchXML contains invalid nodes or structure.</exception>
        public static QueryExpression TranslateFetchXmlDocumentToQueryExpression(XrmFakedContext context, XDocument xlDoc)
        {
            //Validate nodes
            if (!xlDoc.Descendants().All(el => el.IsFetchXmlNodeValid()))
                throw new Exception("At least some node is not valid");

            //Root node
            if (!xlDoc.Root.Name.LocalName.Equals("fetch"))
            {
                throw new Exception("Root node must be fetch");
            }

            var entityNode = RetrieveFetchXmlNode(xlDoc, "entity");
      var query = new QueryExpression(entityNode.GetAttribute("name").Value)
      {
        ColumnSet = xlDoc.ToColumnSet()
      };

      // Ordering is done after grouping/aggregation
      if (!xlDoc.IsAggregateFetchXml())
            {
                var orders = xlDoc.ToOrderExpressionList();
                foreach (var order in orders)
                {
                    query.AddOrder(order.AttributeName, order.OrderType);
                }
            }

            query.Distinct = xlDoc.IsDistincFetchXml();

            query.Criteria = xlDoc.ToCriteria(context);

            query.TopCount = xlDoc.ToTopCount();

            query.PageInfo.Count = xlDoc.ToCount() ?? 0;
            query.PageInfo.PageNumber = xlDoc.ToPageNumber() ?? 1;
            query.PageInfo.ReturnTotalRecordCount = xlDoc.ToReturnTotalRecordCount();

            var linkedEntities = xlDoc.ToLinkEntities(context);
            foreach (var le in linkedEntities)
            {
                query.LinkEntities.Add(le);
            }

            return query;
        }

        /// <summary>
        /// Translates a QueryExpression into a LINQ IQueryable for execution against the in-memory data store.
        /// Handles joins, filters, ordering, and column projection.
        /// </summary>
        /// <param name="context">The XrmFakedContext containing the in-memory data and metadata.</param>
        /// <param name="qe">The QueryExpression to translate into LINQ.</param>
        /// <returns>An IQueryable of Entity that represents the QueryExpression as a LINQ query; null if qe is null.</returns>
        public static IQueryable<Entity> TranslateQueryExpressionToLinq(XrmFakedContext context, QueryExpression qe)
        {
            if (qe == null) return null;

            //Start form the root entity and build a LINQ query to execute the query against the In-Memory context:
            context.EnsureEntityNameExistsInMetadata(qe.EntityName);

            IQueryable<Entity> query = null;

            query = context.CreateQuery<Entity>(qe.EntityName);

            var linkedEntities = new Dictionary<string, int>();

#if  !FAKE_XRM_EASY
            ValidateAliases(qe, context);
#endif

            // Add as many Joins as linked entities
            foreach (var le in qe.LinkEntities)
            {
                if (string.IsNullOrWhiteSpace(le.EntityAlias))
                {
                    le.EntityAlias = EnsureUniqueLinkedEntityAlias(linkedEntities, le.LinkToEntityName);
                }

                query = TranslateLinkedEntityToLinq(context, le, query, qe.ColumnSet, linkedEntities);
            }

            // Compose the expression tree that represents the parameter to the predicate.
            ParameterExpression entity = Expression.Parameter(typeof(Entity));
            var expTreeBody = TranslateQueryExpressionFiltersToExpression(context, qe, entity);
            Expression<Func<Entity, bool>> lambda = Expression.Lambda<Func<Entity, bool>>(expTreeBody, entity);
            query = query.Where(lambda);

            //Sort results
            if (qe.Orders != null)
            {
                if (qe.Orders.Count > 0)
                {
                    IOrderedQueryable<Entity> orderedQuery = null;

                    var order = qe.Orders[0];
                    if (order.OrderType == OrderType.Ascending)
                        orderedQuery = query.OrderBy(e => e.Attributes.ContainsKey(order.AttributeName) ? e[order.AttributeName] : null, new XrmOrderByAttributeComparer());
                    else
                        orderedQuery = query.OrderByDescending(e => e.Attributes.ContainsKey(order.AttributeName) ? e[order.AttributeName] : null, new XrmOrderByAttributeComparer());

                    //Subsequent orders should use ThenBy and ThenByDescending
                    for (var i = 1; i < qe.Orders.Count; i++)
                    {
                        var thenOrder = qe.Orders[i];
                        if (thenOrder.OrderType == OrderType.Ascending)
                            orderedQuery = orderedQuery.ThenBy(e => e.Attributes.ContainsKey(thenOrder.AttributeName) ? e[thenOrder.AttributeName] : null, new XrmOrderByAttributeComparer());
                        else
                            orderedQuery = orderedQuery.ThenByDescending(e => e[thenOrder.AttributeName], new XrmOrderByAttributeComparer());
                    }

                    query = orderedQuery;
                }
            }

            //Project the attributes in the root column set  (must be applied after the where and order clauses, not before!!)
            query = query.Select(x => x.Clone(x.GetType(), context).ProjectAttributes(qe, context));

            return query;
        }

#if !FAKE_XRM_EASY
        /// <summary>
        /// Validates aliases in a query expression
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        protected static void ValidateAliases(QueryExpression qe, XrmFakedContext context)
        {
            if (qe.Criteria != null)
                ValidateAliases(qe, context, qe.Criteria);
            if (qe.LinkEntities != null)
                foreach (var link in qe.LinkEntities)
                {
                    ValidateAliases(qe, context, link);
                }
        }

        /// <summary>
        /// Validates aliases in a linked entity
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="link">The linked entity</param>
        protected static void ValidateAliases(QueryExpression qe, XrmFakedContext context, LinkEntity link)
        {
            if (link.LinkCriteria != null)
                ValidateAliases(qe, context, link.LinkCriteria);
            if (link.LinkEntities != null)
                foreach (var innerLink in link.LinkEntities)
                {
                    ValidateAliases(qe, context, innerLink);
                }
        }

        /// <summary>
        /// Validates aliases in a filter expression
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="filter">The filter expression</param>
        protected static void ValidateAliases(QueryExpression qe, XrmFakedContext context, FilterExpression filter)
        {
            if (filter.Filters != null)
                foreach (var innerFilter in filter.Filters)
                {
                    ValidateAliases(qe, context, innerFilter);
                }

            if (filter.Conditions != null)
                foreach (var condition in filter.Conditions)
                {
                    if (!string.IsNullOrEmpty(condition.EntityName))
                    {
                        ValidateAliases(qe, context, condition);
                    }
                }
        }

        /// <summary>
        /// Validates aliases in a condition expression
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="condition">The condition expression</param>
        protected static void ValidateAliases(QueryExpression qe, XrmFakedContext context, ConditionExpression condition)
        {
            var matches = qe.LinkEntities != null ? MatchByAlias(qe, context, condition, qe.LinkEntities) : 0;
            if (matches > 1)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), $"Table {condition.EntityName} is not unique amongst all top-level table and join aliases");
            }
            else if (matches == 0)
            {
                if (qe.LinkEntities != null) matches = MatchByEntity(qe, context, condition, qe.LinkEntities);
                if (matches > 1)
                {
                    throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), $"There's more than one LinkEntity expressions with name={condition.EntityName}");
                }
                else if (matches == 0)
                {
                    if (condition.EntityName == qe.EntityName) return;
                    throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), $"LinkEntity with name or alias {condition.EntityName} is not found");
                }
                condition.EntityName += "1";
            }
        }

        /// <summary>
        /// Matches a condition expression by entity name in linked entities
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="condition">The condition expression</param>
        /// <param name="linkEntities">The collection of linked entities</param>
        /// <returns>The number of matches found</returns>
        protected static int MatchByEntity(QueryExpression qe, XrmFakedContext context, ConditionExpression condition, DataCollection<LinkEntity> linkEntities)
        {
            var matches = 0;
            foreach (var link in linkEntities)
            {
                if (string.IsNullOrEmpty(link.EntityAlias) && condition.EntityName == link.LinkToEntityName)
                {
                    matches += 1;
                }
                if (link.LinkEntities != null) matches += MatchByEntity(qe, context, condition, link.LinkEntities);
            }
            return matches;
        }

        /// <summary>
        /// Matches a condition expression by alias in linked entities
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="condition">The condition expression</param>
        /// <param name="linkEntities">The collection of linked entities</param>
        /// <returns>The number of matches found</returns>
        protected static int MatchByAlias(QueryExpression qe, XrmFakedContext context, ConditionExpression condition, DataCollection<LinkEntity> linkEntities)
        {
            var matches = 0;
            foreach (var link in linkEntities)
            {
                if (link.EntityAlias == condition.EntityName)
                {
                    matches += 1;
                }
                if (link.LinkEntities != null) matches += MatchByAlias(qe, context, condition, link.LinkEntities);
            }
            return matches;
        }
#endif


        /// <summary>
        /// Translates a condition expression to a LINQ expression
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="c">The typed condition expression</param>
        /// <param name="entity">The parameter expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpression(QueryExpression qe, XrmFakedContext context, TypedConditionExpression c, ParameterExpression entity)
        {
            Expression attributesProperty = Expression.Property(
                entity,
                "Attributes"
                );


            //If the attribute comes from a joined entity, then we need to access the attribute from the join
            //But the entity name attribute only exists >= 2013
#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
            string attributeName = "";

            //Do not prepend the entity name if the EntityLogicalName is the same as the QueryExpression main logical name

            if (!string.IsNullOrWhiteSpace(c.CondExpression.EntityName) && !c.CondExpression.EntityName.Equals(qe.EntityName))
            {
                attributeName = c.CondExpression.EntityName + "." + c.CondExpression.AttributeName;
            }
            else
                attributeName = c.CondExpression.AttributeName;

            Expression containsAttributeExpression = Expression.Call(
                attributesProperty,
                typeof(AttributeCollection).GetMethod("ContainsKey", new Type[] { typeof(string) }),
                Expression.Constant(attributeName)
                );

            Expression getAttributeValueExpr = Expression.Property(
                            attributesProperty, "Item",
                            Expression.Constant(attributeName, typeof(string))
                            );
#else
            Expression containsAttributeExpression = Expression.Call(
                attributesProperty,
                typeof(AttributeCollection).GetMethod("ContainsKey", new Type[] { typeof(string) }),
                Expression.Constant(c.CondExpression.AttributeName)
                );

            Expression getAttributeValueExpr = Expression.Property(
                            attributesProperty, "Item",
                            Expression.Constant(c.CondExpression.AttributeName, typeof(string))
                            );
#endif



            Expression getNonBasicValueExpr = getAttributeValueExpr;

            Expression operatorExpression = null;

            // Handle column-to-column comparison (FetchXML valueof attribute)
            // Addresses upstream issue #514
            if (c.IsColumnComparison)
            {
                operatorExpression = TranslateColumnComparisonExpression(c, entity, attributesProperty, containsAttributeExpression);
                if (operatorExpression != null)
                {
                    return operatorExpression;
                }
                // If the operator is not supported for column comparison, fall through to throw an error
            }

            switch (c.CondExpression.Operator)
            {
                case ConditionOperator.Equal:
                case ConditionOperator.Today:
                case ConditionOperator.Yesterday:
                case ConditionOperator.Tomorrow:
                case ConditionOperator.EqualUserId:
                    operatorExpression = TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NotEqualUserId:
                    operatorExpression = Expression.Not(TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.EqualBusinessId:
                    operatorExpression = TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NotEqualBusinessId:
                    operatorExpression = Expression.Not(TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.BeginsWith:
                case ConditionOperator.Like:
                    operatorExpression = TranslateConditionExpressionLike(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.EndsWith:
                    operatorExpression = TranslateConditionExpressionEndsWith(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.Contains:
                    operatorExpression = TranslateConditionExpressionContains(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NotEqual:
                    operatorExpression = Expression.Not(TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.DoesNotBeginWith:
                case ConditionOperator.DoesNotEndWith:
                case ConditionOperator.NotLike:
                case ConditionOperator.DoesNotContain:
                    operatorExpression = Expression.Not(TranslateConditionExpressionLike(c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.Null:
                    operatorExpression = TranslateConditionExpressionNull(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NotNull:
                    operatorExpression = Expression.Not(TranslateConditionExpressionNull(c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.GreaterThan:
                    operatorExpression = TranslateConditionExpressionGreaterThan(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.GreaterEqual:
                    operatorExpression = TranslateConditionExpressionGreaterThanOrEqual(context, c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.LessThan:
                    operatorExpression = TranslateConditionExpressionLessThan(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.LessEqual:
                    operatorExpression = TranslateConditionExpressionLessThanOrEqual(context, c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.In:
                    operatorExpression = TranslateConditionExpressionIn(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NotIn:
                    operatorExpression = Expression.Not(TranslateConditionExpressionIn(c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.On:
                    operatorExpression = TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NotOn:
                    operatorExpression = Expression.Not(TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.OnOrAfter:
                    operatorExpression = Expression.Or(
                               TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression),
                               TranslateConditionExpressionGreaterThan(c, getNonBasicValueExpr, containsAttributeExpression));
                    break;
                case ConditionOperator.LastXHours:
                case ConditionOperator.LastXDays:
                case ConditionOperator.Last7Days:
                case ConditionOperator.LastXWeeks:
                case ConditionOperator.LastXMonths:
                case ConditionOperator.LastXYears:
                    operatorExpression = TranslateConditionExpressionLast(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.OnOrBefore:
                    operatorExpression = Expression.Or(
                                TranslateConditionExpressionEqual(context, c, getNonBasicValueExpr, containsAttributeExpression),
                                TranslateConditionExpressionLessThan(c, getNonBasicValueExpr, containsAttributeExpression));
                    break;

                case ConditionOperator.Between:
                    if (c.CondExpression.Values.Count != 2)
                    {
                        throw new Exception("Between operator requires exactly 2 values.");
                    }
                    operatorExpression = TranslateConditionExpressionBetween(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NotBetween:
                    if (c.CondExpression.Values.Count != 2)
                    {
                        throw new Exception("Not-Between operator requires exactly 2 values.");
                    }
                    operatorExpression = Expression.Not(TranslateConditionExpressionBetween(c, getNonBasicValueExpr, containsAttributeExpression));
                    break;
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013
                case ConditionOperator.OlderThanXMinutes:
                case ConditionOperator.OlderThanXHours:
                case ConditionOperator.OlderThanXDays:
                case ConditionOperator.OlderThanXWeeks:
                case ConditionOperator.OlderThanXYears:                  
#endif
                case ConditionOperator.OlderThanXMonths:
                    operatorExpression = TranslateConditionExpressionOlderThan(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.NextXHours:               
                case ConditionOperator.NextXDays:                  
                case ConditionOperator.Next7Days:
                case ConditionOperator.NextXWeeks:                 
                case ConditionOperator.NextXMonths:                    
                case ConditionOperator.NextXYears:
                    operatorExpression = TranslateConditionExpressionNext(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;
                case ConditionOperator.ThisYear:
                case ConditionOperator.LastYear:
                case ConditionOperator.NextYear:
                case ConditionOperator.ThisMonth:
                case ConditionOperator.LastMonth:
                case ConditionOperator.NextMonth:
                case ConditionOperator.LastWeek:
                case ConditionOperator.ThisWeek:
                case ConditionOperator.NextWeek:
                case ConditionOperator.InFiscalYear:
                case ConditionOperator.InFiscalPeriod:
                case ConditionOperator.InFiscalPeriodAndYear:
                case ConditionOperator.ThisFiscalPeriod:
                case ConditionOperator.LastFiscalPeriod:
                case ConditionOperator.NextFiscalPeriod:
                    operatorExpression = TranslateConditionExpressionBetweenDates(c, getNonBasicValueExpr, containsAttributeExpression, context);
                    break;
#if FAKE_XRM_EASY_9
                case ConditionOperator.ContainValues:
                    operatorExpression = TranslateConditionExpressionContainValues(c, getNonBasicValueExpr, containsAttributeExpression);
                    break;

                case ConditionOperator.DoesNotContainValues:
                    operatorExpression = Expression.Not(TranslateConditionExpressionContainValues(c, getNonBasicValueExpr, containsAttributeExpression));
                    break;
#endif

                // Hierarchy operators (issue #287)
                case ConditionOperator.Above:
                    operatorExpression = TranslateConditionExpressionHierarchy(context, qe, c, entity, HierarchyOperatorType.Above);
                    break;

                case ConditionOperator.AboveOrEqual:
                    operatorExpression = TranslateConditionExpressionHierarchy(context, qe, c, entity, HierarchyOperatorType.AboveOrEqual);
                    break;

                case ConditionOperator.Under:
                    operatorExpression = TranslateConditionExpressionHierarchy(context, qe, c, entity, HierarchyOperatorType.Under);
                    break;

                case ConditionOperator.UnderOrEqual:
                    operatorExpression = TranslateConditionExpressionHierarchy(context, qe, c, entity, HierarchyOperatorType.UnderOrEqual);
                    break;

                case ConditionOperator.NotUnder:
                    operatorExpression = TranslateConditionExpressionHierarchy(context, qe, c, entity, HierarchyOperatorType.NotUnder);
                    break;

                default:
                    throw new PullRequestException(string.Format("Operator {0} not yet implemented for condition expression", c.CondExpression.Operator.ToString()));


            }

            if (c.IsOuter)
            {
                //If outer join, filter is optional, only if there was a value
                return Expression.Constant(true);
            }
            else
                return operatorExpression;

        }

        /// <summary>
        /// Validates that the condition value types are compatible with the attribute type.
        /// Real Dataverse throws an error when comparing incompatible types, for example:
        /// - Comparing a GUID value against an EntityReference field
        /// - Comparing an EntityReference value against a GUID field
        /// Addresses GitHub issue #258.
        /// </summary>
        /// <param name="typedExpression">The typed condition expression to validate.</param>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when types are incompatible.</exception>
        private static void ValidateConditionValueTypeCompatibility(TypedConditionExpression typedExpression)
        {
            // Skip validation for operators that don't use values
            var operatorsWithoutValues = new[]
            {
                ConditionOperator.Null,
                ConditionOperator.NotNull,
                ConditionOperator.Today,
                ConditionOperator.Yesterday,
                ConditionOperator.Tomorrow,
                ConditionOperator.EqualUserId,
                ConditionOperator.NotEqualUserId,
                ConditionOperator.EqualBusinessId,
                ConditionOperator.NotEqualBusinessId,
                ConditionOperator.ThisYear,
                ConditionOperator.LastYear,
                ConditionOperator.NextYear,
                ConditionOperator.ThisMonth,
                ConditionOperator.LastMonth,
                ConditionOperator.NextMonth,
                ConditionOperator.ThisWeek,
                ConditionOperator.LastWeek,
                ConditionOperator.NextWeek,
                ConditionOperator.Last7Days,
                ConditionOperator.Next7Days,
                ConditionOperator.ThisFiscalPeriod,
                ConditionOperator.LastFiscalPeriod,
                ConditionOperator.NextFiscalPeriod
            };

            if (operatorsWithoutValues.Contains(typedExpression.CondExpression.Operator))
            {
                return;
            }

            // Skip if no values to validate
            if (typedExpression.CondExpression.Values == null || typedExpression.CondExpression.Values.Count == 0)
            {
                return;
            }

            // Skip if attribute type is unknown (late-bound without metadata)
            if (typedExpression.AttributeType == null)
            {
                return;
            }

            // Skip column-to-column comparisons
            if (typedExpression.IsColumnComparison)
            {
                return;
            }

            // Get the underlying type if nullable
            var attributeType = typedExpression.AttributeType;
            if (Nullable.GetUnderlyingType(attributeType) != null)
            {
                attributeType = Nullable.GetUnderlyingType(attributeType);
            }

            // Validate each value in the condition
            foreach (var value in typedExpression.CondExpression.Values)
            {
                if (value == null)
                {
                    continue;
                }

                // Handle arrays (e.g., In operator with array of values)
                if (value is Array arrayValue)
                {
                    foreach (var item in arrayValue)
                    {
                        ValidateSingleValueTypeCompatibility(attributeType, item, typedExpression.CondExpression.AttributeName);
                    }
                }
                else
                {
                    ValidateSingleValueTypeCompatibility(attributeType, value, typedExpression.CondExpression.AttributeName);
                }
            }
        }

        /// <summary>
        /// Validates that a single condition value is type-compatible with the attribute type.
        /// Note: GUID values are allowed for EntityReference fields since that's the common pattern
        /// used by LINQ providers and FetchXML. The Id property of the EntityReference is compared.
        /// However, EntityReference values CANNOT be used for GUID fields (the reverse is not valid).
        /// </summary>
        /// <param name="attributeType">The type of the attribute being compared.</param>
        /// <param name="value">The condition value to validate.</param>
        /// <param name="attributeName">The name of the attribute (for error messages).</param>
        private static void ValidateSingleValueTypeCompatibility(Type attributeType, object value, string attributeName)
        {
            if (value == null)
            {
                return;
            }

            var valueType = value.GetType();

            // Check for type mismatches addressed by GitHub issue #258
            //
            // Note: GUID values ARE allowed for EntityReference fields. This is because:
            // 1. The SDK LINQ provider converts EntityReference comparisons to GUID comparisons internally
            // 2. FetchXML conditions typically use GUID values for lookup fields
            // 3. The internal storage in Dataverse is GUID-based
            //
            // However, EntityReference values CANNOT be used for GUID fields because:
            // - A GUID field (like ActivityId, or a primary key) is a simple identifier
            // - Passing an EntityReference where a GUID is expected is a type mismatch

            // Case: Attribute is GUID but value is EntityReference - this is NOT allowed
            if (attributeType == typeof(Guid) && valueType == typeof(EntityReference))
            {
                FakeOrganizationServiceFault.Throw(
                    ErrorCodes.ArgumentTypeMismatch,
                    $"Condition for attribute '{attributeName}': cannot compare Guid attribute to EntityReference value. Use a Guid value instead.");
            }
        }

        private static void ValidateSupportedTypedExpression(TypedConditionExpression typedExpression)
        {
            // Validate type compatibility between attribute type and condition values
            // Real Dataverse throws an error when comparing incompatible types (e.g., GUID vs EntityReference)
            // Addresses GitHub issue #258
            ValidateConditionValueTypeCompatibility(typedExpression);

            Expression validateOperatorTypeExpression = Expression.Empty();
            ConditionOperator[] supportedOperators = (ConditionOperator[])Enum.GetValues(typeof(ConditionOperator));

#if FAKE_XRM_EASY_9
            if (typedExpression.AttributeType == typeof(OptionSetValueCollection))
            {
                supportedOperators = new[]
                {
                    ConditionOperator.ContainValues,
                    ConditionOperator.DoesNotContainValues,
                    ConditionOperator.Equal,
                    ConditionOperator.NotEqual,
                    ConditionOperator.NotNull,
                    ConditionOperator.Null,
                    ConditionOperator.In,
                    ConditionOperator.NotIn,
                };
            }
#endif

            if (!supportedOperators.Contains(typedExpression.CondExpression.Operator))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidOperatorCode, "The operator is not valid or it is not supported.");
            }
        }

        /// <summary>
        /// Gets an appropriate typed value expression for comparison
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>An expression representing the typed value</returns>
        protected static Expression GetAppropiateTypedValue(object value)
        {
            //Basic types conversions
            //Special case => datetime is sent as a string
            if (value is string)
            {
                DateTime dtDateTimeConversion;
                if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dtDateTimeConversion))
                {
                    return Expression.Constant(dtDateTimeConversion, typeof(DateTime));
                }
                else
                {
                    return GetCaseInsensitiveExpression(Expression.Constant(value, typeof(string)));
                }
            }
            else if (value is EntityReference)
            {
                var cast = (value as EntityReference).Id;
                return Expression.Constant(cast);
            }
            else if (value is OptionSetValue)
            {
                var cast = (value as OptionSetValue).Value;
                return Expression.Constant(cast);
            }
            else if (value is Money)
            {
                var cast = (value as Money).Value;
                return Expression.Constant(cast);
            }
            return Expression.Constant(value);
        }

        /// <summary>
        /// Gets the appropriate type for a given value
        /// </summary>
        /// <param name="value">The value to get the type for</param>
        /// <returns>The type of the value</returns>
        protected static Type GetAppropiateTypeForValue(object value)
        {
            //Basic types conversions
            //Special case => datetime is sent as a string
            if (value is string)
            {
                DateTime dtDateTimeConversion;
                if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dtDateTimeConversion))
                {
                    return typeof(DateTime);
                }
                else
                {
                    return typeof(string);
                }
            }
            else
                return value.GetType();
        }

        /// <summary>
        /// Gets an appropriate typed value expression and type for comparison
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="attributeType">The attribute type</param>
        /// <returns>An expression representing the typed value</returns>
        protected static Expression GetAppropiateTypedValueAndType(object value, Type attributeType)
        {
            if (attributeType == null)
                return GetAppropiateTypedValue(value);

            if (Nullable.GetUnderlyingType(attributeType) != null)
            {
                attributeType = Nullable.GetUnderlyingType(attributeType);
            }

            //Basic types conversions
            //Special case => datetime is sent as a string
            if (value is string)
            {
                int iValue;

                DateTime dtDateTimeConversion;
                Guid id;
                if (attributeType.IsDateTime()  //Only convert to DateTime if the attribute's type was DateTime
                    && DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dtDateTimeConversion))
                {
                    return Expression.Constant(dtDateTimeConversion, typeof(DateTime));
                }
                else if (attributeType.IsOptionSet() && int.TryParse(value.ToString(), out iValue))
                {
                    return Expression.Constant(iValue, typeof(int));
                }
                else if ((attributeType == typeof(EntityReference) || attributeType == typeof(Guid)) && Guid.TryParse((string)value, out id))
                {
                    return Expression.Constant(id);
                }
                else
                {
                    return GetCaseInsensitiveExpression(Expression.Constant(value, typeof(string)));
                }
            }
            else if (value is EntityReference)
            {
                var cast = (value as EntityReference).Id;
                return Expression.Constant(cast);
            }
            else if (value is OptionSetValue)
            {
                var cast = (value as OptionSetValue).Value;
                return Expression.Constant(cast);
            }
            else if (value is Money)
            {
                var cast = (value as Money).Value;
                return Expression.Constant(cast);
            }
            return Expression.Constant(value);
        }

        /// <summary>
        /// Gets an appropriate cast expression based on type
        /// </summary>
        /// <param name="t">The target type</param>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnType(Type t, Expression input, object value)
        {
            var typedExpression = GetAppropiateCastExpressionBasedOnAttributeTypeOrValue(input, value, t);

            //Now, any value (entity reference, string, int, etc,... could be wrapped in an AliasedValue object
            //So let's add this
            var getValueFromAliasedValueExp = Expression.Call(Expression.Convert(input, typeof(AliasedValue)),
                                            typeof(AliasedValue).GetMethod("get_Value"));

            var exp = Expression.Condition(Expression.TypeIs(input, typeof(AliasedValue)),
                    GetAppropiateCastExpressionBasedOnAttributeTypeOrValue(getValueFromAliasedValueExp, value, t),
                    typedExpression //Not an aliased value
                );

            return exp;
        }

        //protected static Expression GetAppropiateCastExpressionBasedOnValue(XrmFakedContext context, Expression input, object value)
        //{
        //    var typedExpression = GetAppropiateCastExpressionBasedOnAttributeTypeOrValue(context, input, value, sEntityName, sAttributeName);

        //    //Now, any value (entity reference, string, int, etc,... could be wrapped in an AliasedValue object
        //    //So let's add this
        //    var getValueFromAliasedValueExp = Expression.Call(Expression.Convert(input, typeof(AliasedValue)),
        //                                    typeof(AliasedValue).GetMethod("get_Value"));

        //    var  exp = Expression.Condition(Expression.TypeIs(input, typeof(AliasedValue)),
        //            GetAppropiateCastExpressionBasedOnAttributeTypeOrValue(context, getValueFromAliasedValueExp, value, sEntityName, sAttributeName),
        //            typedExpression //Not an aliased value
        //        );

        //    return exp;
        //}

        /// <summary>
        /// Gets an appropriate cast expression based on value's inherent type
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnValueInherentType(Expression input, object value)
        {
            if (value is Guid || value is EntityReference)
                return GetAppropiateCastExpressionBasedGuid(input); //Could be compared against an EntityReference
            if (value is int || value is OptionSetValue)
                return GetAppropiateCastExpressionBasedOnInt(input); //Could be compared against an OptionSet
            if (value is decimal || value is Money)
                return GetAppropiateCastExpressionBasedOnDecimal(input); //Could be compared against a Money
            if (value is bool)
                return GetAppropiateCastExpressionBasedOnBoolean(input); //Could be a BooleanManagedProperty
            if (value is string)
            {
                return GetAppropiateCastExpressionBasedOnString(input, value);
            }
            return GetAppropiateCastExpressionDefault(input, value); //any other type
        }

        /// <summary>
        /// Gets an appropriate cast expression based on attribute type or value
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <param name="attributeType">The attribute type</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnAttributeTypeOrValue(Expression input, object value, Type attributeType)
        {
            if (attributeType != null)
            {
                if (Nullable.GetUnderlyingType(attributeType) != null)
                {
                    attributeType = Nullable.GetUnderlyingType(attributeType);
                }
#if FAKE_XRM_EASY || FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015
                if (attributeType == typeof(Microsoft.Xrm.Client.CrmEntityReference))
                    return GetAppropiateCastExpressionBasedGuid(input);
#endif
                if (attributeType == typeof(Guid))
                    return GetAppropiateCastExpressionBasedGuid(input);
                if (attributeType == typeof(EntityReference))
                    return GetAppropiateCastExpressionBasedOnEntityReference(input, value);
                if (attributeType == typeof(int) || attributeType == typeof(Nullable<int>) || attributeType.IsOptionSet())
                    return GetAppropiateCastExpressionBasedOnInt(input);
                if (attributeType == typeof(decimal) || attributeType == typeof(Money))
                    return GetAppropiateCastExpressionBasedOnDecimal(input);
                if (attributeType == typeof(bool) || attributeType == typeof(BooleanManagedProperty))
                    return GetAppropiateCastExpressionBasedOnBoolean(input);
                if (attributeType == typeof(string))
                    return GetAppropiateCastExpressionBasedOnStringAndType(input, value, attributeType);
                if (attributeType.IsDateTime())
                    return GetAppropiateCastExpressionBasedOnDateTime(input, value);
#if FAKE_XRM_EASY_9
                if (attributeType.IsOptionSetValueCollection())
                    return GetAppropiateCastExpressionBasedOnOptionSetValueCollection(input);
#endif

                return GetAppropiateCastExpressionDefault(input, value); //any other type
            }

            return GetAppropiateCastExpressionBasedOnValueInherentType(input, value); //Dynamic entities
        }

        /// <summary>
        /// Gets an appropriate cast expression based on string value
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnString(Expression input, object value)
        {
            var defaultStringExpression = GetCaseInsensitiveExpression(GetAppropiateCastExpressionDefault(input, value));

            // Issue #439: Use InvariantCulture for DateTime parsing since FetchXML values are in ISO format
            DateTime dtDateTimeConversion;
            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dtDateTimeConversion))
            {
                return Expression.Convert(input, typeof(DateTime));
            }

            int iValue;
            if (int.TryParse(value.ToString(), out iValue))
            {
                return Expression.Condition(Expression.TypeIs(input, typeof(OptionSetValue)),
                    GetToStringExpression<Int32>(GetAppropiateCastExpressionBasedOnInt(input)),
                    defaultStringExpression
                );
            }

            return defaultStringExpression;
        }

        /// <summary>
        /// Gets an appropriate cast expression based on string value and type
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <param name="attributeType">The attribute type</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnStringAndType(Expression input, object value, Type attributeType)
        {
            var defaultStringExpression = GetCaseInsensitiveExpression(GetAppropiateCastExpressionDefault(input, value));

            int iValue;
            if (attributeType.IsOptionSet() && int.TryParse(value.ToString(), out iValue))
            {
                return Expression.Condition(Expression.TypeIs(input, typeof(OptionSetValue)),
                    GetToStringExpression<Int32>(GetAppropiateCastExpressionBasedOnInt(input)),
                    defaultStringExpression
                );
            }

            return defaultStringExpression;
        }

        /// <summary>
        /// Gets an appropriate cast expression based on DateTime value
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnDateTime(Expression input, object value)
        {
            // Convert to DateTime if string
            // Issue #439: Use InvariantCulture for DateTime parsing since FetchXML values are in ISO format
            DateTime _;
            if (value is DateTime || value is string && DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return Expression.Convert(input, typeof(DateTime));
            }

            return input; // return directly
        }

        /// <summary>
        /// Gets the default cast expression
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionDefault(Expression input, object value)
        {
            return Expression.Convert(input, value.GetType());  //Default type conversion
        }

        /// <summary>
        /// Gets an appropriate cast expression based on Guid
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedGuid(Expression input)
        {
            var getIdFromEntityReferenceExpr = Expression.Call(Expression.TypeAs(input, typeof(EntityReference)),
                typeof(EntityReference).GetMethod("get_Id"));

            return Expression.Condition(
                Expression.TypeIs(input, typeof(EntityReference)),  //If input is an entity reference, compare the Guid against the Id property
                Expression.Convert(
                    getIdFromEntityReferenceExpr,
                    typeof(Guid)),
                Expression.Condition(Expression.TypeIs(input, typeof(Guid)),  //If any other case, then just compare it as a Guid directly
                    Expression.Convert(input, typeof(Guid)),
                    Expression.Constant(Guid.Empty, typeof(Guid))));
        }

        /// <summary>
        /// Gets an appropriate cast expression based on EntityReference
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <param name="value">The value to cast</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnEntityReference(Expression input, object value)
        {
            Guid guid;
            if (value is string && !Guid.TryParse((string)value, out guid))
            {
                var getNameFromEntityReferenceExpr = Expression.Call(Expression.TypeAs(input, typeof(EntityReference)),
                    typeof(EntityReference).GetMethod("get_Name"));

                return GetCaseInsensitiveExpression(Expression.Condition(Expression.TypeIs(input, typeof(EntityReference)),
                    Expression.Convert(getNameFromEntityReferenceExpr, typeof(string)),
                    Expression.Constant(string.Empty, typeof(string))));
            }

            var getIdFromEntityReferenceExpr = Expression.Call(Expression.TypeAs(input, typeof(EntityReference)),
                typeof(EntityReference).GetMethod("get_Id"));

            return Expression.Condition(
                Expression.TypeIs(input, typeof(EntityReference)),  //If input is an entity reference, compare the Guid against the Id property
                Expression.Convert(
                    getIdFromEntityReferenceExpr,
                    typeof(Guid)),
                Expression.Condition(Expression.TypeIs(input, typeof(Guid)),  //If any other case, then just compare it as a Guid directly
                    Expression.Convert(input, typeof(Guid)),
                    Expression.Constant(Guid.Empty, typeof(Guid))));

        }

        /// <summary>
        /// Gets an appropriate cast expression based on decimal
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnDecimal(Expression input)
        {
            // Handle Money -> decimal
            var moneyCondition = Expression.Condition(
                Expression.TypeIs(input, typeof(Money)),
                Expression.Convert(
                    Expression.Call(Expression.TypeAs(input, typeof(Money)),
                        typeof(Money).GetMethod("get_Value")),
                    typeof(decimal)),
                Expression.Constant(0.0M));

            // Handle OptionSetValue -> decimal (for cases where numeric value could be optionset or money)
            // This allows FetchXML numeric values without ProxyTypes to work with both Money and OptionSetValue
            var optionSetCondition = Expression.Condition(
                Expression.TypeIs(input, typeof(OptionSetValue)),
                Expression.Convert(
                    Expression.Call(Expression.TypeAs(input, typeof(OptionSetValue)),
                        typeof(OptionSetValue).GetMethod("get_Value")),
                    typeof(decimal)),
                moneyCondition);

            // Handle decimal or fallback
            return Expression.Condition(
                Expression.TypeIs(input, typeof(decimal)),
                Expression.Convert(input, typeof(decimal)),
                optionSetCondition);
        }

        /// <summary>
        /// Gets an appropriate cast expression based on boolean
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnBoolean(Expression input)
        {
            return Expression.Condition(
                        Expression.TypeIs(input, typeof(BooleanManagedProperty)),
                                Expression.Convert(
                                    Expression.Call(Expression.TypeAs(input, typeof(BooleanManagedProperty)),
                                            typeof(BooleanManagedProperty).GetMethod("get_Value")),
                                            typeof(bool)),
                           Expression.Condition(Expression.TypeIs(input, typeof(bool)),
                                        Expression.Convert(input, typeof(bool)),
                                        Expression.Constant(false)));

        }

        /// <summary>
        /// Gets an appropriate cast expression based on int
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnInt(Expression input)
        {
            // Issue #569: Use SafeConvertToInt to handle type mismatches gracefully
            // This handles cases where ObjectTypeCode is stored as string but queried as int
            return Expression.Call(typeof(XrmFakedContext).GetMethod(nameof(SafeConvertToInt)), input);
        }

        /// <summary>
        /// Safely converts an object to an integer for query comparisons.
        /// Handles OptionSetValue, int, and string inputs. Returns int.MinValue for incompatible types
        /// to ensure the comparison fails gracefully rather than throwing an exception.
        /// </summary>
        /// <param name="input">The input value to convert</param>
        /// <returns>The integer value, or int.MinValue if conversion is not possible</returns>
        /// <remarks>
        /// This method supports Issue #569 where ObjectTypeCode attributes may be stored as strings
        /// (entity logical names like "salesorder") but queried with integer values (ObjectTypeCode like 1088).
        /// Instead of throwing InvalidCastException, it returns a sentinel value that won't match.
        /// </remarks>
        public static int SafeConvertToInt(object input)
        {
            if (input == null)
                return int.MinValue;

            if (input is OptionSetValue osv)
                return osv.Value;

            if (input is int intValue)
                return intValue;

            if (input is string strValue)
            {
                if (int.TryParse(strValue, out int parsed))
                    return parsed;
                // String that can't be parsed to int - return sentinel (won't match any query value)
                return int.MinValue;
            }

            // Try Convert for other numeric types
            try
            {
                return Convert.ToInt32(input);
            }
            catch
            {
                // Type mismatch - return sentinel value that won't match
                return int.MinValue;
            }
        }

        /// <summary>
        /// Gets an appropriate cast expression based on OptionSetValueCollection
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <returns>The cast expression</returns>
        protected static Expression GetAppropiateCastExpressionBasedOnOptionSetValueCollection(Expression input)
        {
            return Expression.Call(typeof(XrmFakedContext).GetMethod("ConvertToHashSetOfInt"), input, Expression.Constant(true));
        }

#if FAKE_XRM_EASY_9
        /// <summary>
        /// Converts various input types to a HashSet of integers for multi-select option set comparisons.
        /// Supports int, string, int[], string[], DataCollection of objects, and OptionSetValueCollection.
        /// </summary>
        /// <param name="input">The input value to convert. Can be int, string, int[], string[], DataCollection, or OptionSetValueCollection.</param>
        /// <param name="isOptionSetValueCollectionAccepted">If true, OptionSetValueCollection inputs are accepted; otherwise they throw a FaultException.</param>
        /// <returns>A HashSet of integers representing the converted values.</returns>
        /// <exception cref="FaultException">Thrown when the input type is not supported or cannot be converted.</exception>
        public static HashSet<int> ConvertToHashSetOfInt(object input, bool isOptionSetValueCollectionAccepted)
        {
            var set = new HashSet<int>();

            var faultReason = $"The formatter threw an exception while trying to deserialize the message: There was an error while trying to deserialize parameter" +
                $" http://schemas.microsoft.com/xrm/2011/Contracts/Services:query. The InnerException message was 'Error in line 1 position 8295. Element " +
                $"'http://schemas.microsoft.com/2003/10/Serialization/Arrays:anyType' contains data from a type that maps to the name " +
                $"'http://schemas.microsoft.com/xrm/2011/Contracts:{input?.GetType()}'. The deserializer has no knowledge of any type that maps to this name. " +
                $"Consider changing the implementation of the ResolveName method on your DataContractResolver to return a non-null value for name " +
                $"'{input?.GetType()}' and namespace 'http://schemas.microsoft.com/xrm/2011/Contracts'.'.  Please see InnerException for more details.";

            if (input is int)
            {
                set.Add((int)input);
            }
            else if (input is string)
            {
                set.Add(int.Parse(input as string));
            }
            else if (input is int[])
            {
                set.UnionWith(input as int[]);
            }
            else if (input is string[])
            {
                set.UnionWith((input as string[]).Select(s => int.Parse(s)));
            }
            else if (input is DataCollection<object>)
            {
                var collection = input as DataCollection<object>;

                if (collection.All(o => o is int))
                {
                    set.UnionWith(collection.Cast<int>());
                }
                else if (collection.All(o => o is string))
                {
                    set.UnionWith(collection.Select(o => int.Parse(o as string)));
                }
                else if (collection.Count == 1 && collection[0] is int[])
                {
                    set.UnionWith(collection[0] as int[]);
                }
                else if (collection.Count == 1 && collection[0] is string[])
                {
                    set.UnionWith((collection[0] as string[]).Select(s => int.Parse(s)));
                }
                else
                {
                    throw new FaultException(new FaultReason(faultReason));
                }
            }
            else if (isOptionSetValueCollectionAccepted && input is OptionSetValueCollection)
            {
                set.UnionWith((input as OptionSetValueCollection).Select(osv => osv.Value));
            }
            else if (input is AliasedValue aliasedValue)
            {
                // For linked entity queries, the value may be wrapped in AliasedValue
                return ConvertToHashSetOfInt(aliasedValue.Value, isOptionSetValueCollectionAccepted);
            }
            else
            {
                throw new FaultException(new FaultReason(faultReason));
            }

            return set;
        }
#endif

        /// <summary>
        /// Transforms expression to get date only part
        /// </summary>
        /// <param name="input">The input expression</param>
        /// <returns>The transformed expression</returns>
        protected static Expression TransformExpressionGetDateOnlyPart(Expression input)
        {
            return Expression.Call(input, typeof(DateTime).GetMethod("get_Date"));
        }

        /// <summary>
        /// Transforms expression value based on operator
        /// </summary>
        /// <param name="op">The condition operator</param>
        /// <param name="input">The input expression</param>
        /// <returns>The transformed expression</returns>
        protected static Expression TransformExpressionValueBasedOnOperator(ConditionOperator op, Expression input)
        {
            switch (op)
            {
                case ConditionOperator.Today:
                case ConditionOperator.Yesterday:
                case ConditionOperator.Tomorrow:
                case ConditionOperator.On:
                case ConditionOperator.OnOrAfter:
                case ConditionOperator.OnOrBefore:
                    return TransformExpressionGetDateOnlyPart(input);

                default:
                    return input; //No transformation
            }
        }

        /// <summary>
        /// Translates condition expression for equal operator
        /// </summary>
        /// <param name="context">The faked context</param>
        /// <param name="c">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionEqual(XrmFakedContext context, TypedConditionExpression c, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {

            BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));

            object unaryOperatorValue = null;

            switch (c.CondExpression.Operator)
            {
                case ConditionOperator.Today:
                    unaryOperatorValue = DateTime.Today;
                    break;
                case ConditionOperator.Yesterday:
                    unaryOperatorValue = DateTime.Today.AddDays(-1);
                    break;
                case ConditionOperator.Tomorrow:
                    unaryOperatorValue = DateTime.Today.AddDays(1);
                    break;
                case ConditionOperator.EqualUserId:
                case ConditionOperator.NotEqualUserId:
                    unaryOperatorValue = context.CallerId.Id;
                    break;

                case ConditionOperator.EqualBusinessId:
                case ConditionOperator.NotEqualBusinessId:
                    unaryOperatorValue = context.BusinessUnitId.Id;
                    break;
            }

            if (unaryOperatorValue != null)
            {
                //c.Values empty in this case
                var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(c.AttributeType, getAttributeValueExpr, unaryOperatorValue);
                var transformedExpression = TransformExpressionValueBasedOnOperator(c.CondExpression.Operator, leftHandSideExpression);

                expOrValues = Expression.Equal(transformedExpression,
                                GetAppropiateTypedValueAndType(unaryOperatorValue, c.AttributeType));
            }
#if FAKE_XRM_EASY_9
            else if (c.AttributeType == typeof(OptionSetValueCollection))
            {
                var conditionValue = GetSingleConditionValue(c);

                var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(c.AttributeType, getAttributeValueExpr, conditionValue);
                var rightHandSideExpression = Expression.Constant(ConvertToHashSetOfInt(conditionValue, isOptionSetValueCollectionAccepted: false));

                expOrValues = Expression.Equal(
                    Expression.Call(leftHandSideExpression, typeof(HashSet<int>).GetMethod("SetEquals"), rightHandSideExpression),
                    Expression.Constant(true));
            }
#endif
            else
            {
                foreach (object value in c.CondExpression.Values)
                {
                    var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(c.AttributeType, getAttributeValueExpr, value);
                    var transformedExpression = TransformExpressionValueBasedOnOperator(c.CondExpression.Operator, leftHandSideExpression);

                    expOrValues = Expression.Or(expOrValues, Expression.Equal(
                                transformedExpression,
                                TransformExpressionValueBasedOnOperator(c.CondExpression.Operator, GetAppropiateTypedValueAndType(value, c.AttributeType))));


                }
            }

            return Expression.AndAlso(
                            containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                expOrValues));
        }

        private static object GetSingleConditionValue(TypedConditionExpression c)
        {
            if (c.CondExpression.Values.Count != 1)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, $"The {c.CondExpression.Operator} requires 1 value/s, not {c.CondExpression.Values.Count}.Parameter name: {c.CondExpression.AttributeName}");
            }

            var conditionValue = c.CondExpression.Values.Single();

            if (!(conditionValue is string) && conditionValue is IEnumerable)
            {
                var conditionValueEnumerable = conditionValue as IEnumerable;
                var count = 0;

                foreach (var obj in conditionValueEnumerable)
                {
                    count++;
                    conditionValue = obj;
                }

                if (count != 1)
                {
                    FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, $"The {c.CondExpression.Operator} requires 1 value/s, not {count}.Parameter name: {c.CondExpression.AttributeName}");
                }
            }

            return conditionValue;
        }

        /// <summary>
        /// Translates condition expression for in operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionIn(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            // Defensive null checks (resolves upstream issue #607)
            if (c.Values == null || c.Values.Count == 0)
            {
                return Expression.Constant(false);
            }

            BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));

#if FAKE_XRM_EASY_9
            if (tc.AttributeType == typeof(OptionSetValueCollection))
            {
                var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, null);
                var rightHandSideExpression = Expression.Constant(ConvertToHashSetOfInt(c.Values, isOptionSetValueCollectionAccepted: false));

                expOrValues = Expression.Equal(
                    Expression.Call(leftHandSideExpression, typeof(HashSet<int>).GetMethod("SetEquals"), rightHandSideExpression),
                    Expression.Constant(true));
            }
            else
#endif
            {
                foreach (object value in c.Values)
                {
                    if (value is Array)
                    {
                        foreach (var a in ((Array)value))
                        {
                            expOrValues = Expression.Or(expOrValues, Expression.Equal(
                                GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, a),
                                GetAppropiateTypedValueAndType(a, tc.AttributeType)));
                        }
                    }
                    else
                    {
                        expOrValues = Expression.Or(expOrValues, Expression.Equal(
                                    GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, value),
                                    GetAppropiateTypedValueAndType(value, tc.AttributeType)));
                    }
                }
            }

            return Expression.AndAlso(
                            containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                expOrValues));
        }

        //protected static Expression TranslateConditionExpressionOn(ConditionExpression c, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        //{
        //    BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));
        //    foreach (object value in c.Values)
        //    {

        //        expOrValues = Expression.Or(expOrValues, Expression.Equal(
        //                    GetAppropiateCastExpressionBasedOnValue(getAttributeValueExpr, value),
        //                    GetAppropiateTypedValue(value)));


        //    }
        //    return Expression.AndAlso(
        //                    containsAttributeExpr,
        //                    Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
        //                        expOrValues));
        //}

        /// <summary>
        /// Translates condition expression for greater than or equal operator
        /// </summary>
        /// <param name="context">The faked context</param>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionGreaterThanOrEqual(XrmFakedContext context, TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            //var c = tc.CondExpression;

            return Expression.Or(
                                TranslateConditionExpressionEqual(context, tc, getAttributeValueExpr, containsAttributeExpr),
                                TranslateConditionExpressionGreaterThan(tc, getAttributeValueExpr, containsAttributeExpr));

        }
        /// <summary>
        /// Translates condition expression for greater than operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionGreaterThan(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            if (c.Values.Count(v => v != null) != 1)
            {
                throw new FaultException(new FaultReason($"The ConditonOperator.{c.Operator} requires 1 value/s, not {c.Values.Count(v => v != null)}. Parameter Name: {c.AttributeName}"));
            }

            if (tc.AttributeType == typeof(string))
            {
                return TranslateConditionExpressionGreaterThanString(tc, getAttributeValueExpr, containsAttributeExpr);
            }
            else if (GetAppropiateTypeForValue(c.Values[0]) == typeof(string))
            {
                return TranslateConditionExpressionGreaterThanString(tc, getAttributeValueExpr, containsAttributeExpr);
            }
            else
            {
                BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));
                foreach (object value in c.Values)
                {
                    var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, value);
                    var transformedExpression = TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, leftHandSideExpression);

                    expOrValues = Expression.Or(expOrValues,
                            Expression.GreaterThan(
                                transformedExpression,
                                TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, GetAppropiateTypedValueAndType(value, tc.AttributeType))));
                }
                return Expression.AndAlso(
                                containsAttributeExpr,
                                Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                    expOrValues));
            }

        }

        /// <summary>
        /// Translates condition expression for greater than operator with string values
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionGreaterThanString(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));
            foreach (object value in c.Values)
            {
                var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, value);
                var transformedExpression = TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, leftHandSideExpression);

                var left = transformedExpression;
                var right = TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, GetAppropiateTypedValueAndType(value, tc.AttributeType));

                var methodCallExpr = GetCompareToExpression<string>(left, right);

                expOrValues = Expression.Or(expOrValues,
                        Expression.GreaterThan(
                            methodCallExpr,
                            Expression.Constant(0)));
            }
            return Expression.AndAlso(
                            containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                expOrValues));
        }

        /// <summary>
        /// Translates condition expression for less than or equal operator
        /// </summary>
        /// <param name="context">The faked context</param>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionLessThanOrEqual(XrmFakedContext context, TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            //var c = tc.CondExpression;

            return Expression.Or(
                                TranslateConditionExpressionEqual(context, tc, getAttributeValueExpr, containsAttributeExpr),
                                TranslateConditionExpressionLessThan(tc, getAttributeValueExpr, containsAttributeExpr));

        }

        /// <summary>
        /// Gets a CompareTo expression for the given type
        /// </summary>
        /// <typeparam name="T">The type to compare</typeparam>
        /// <param name="left">The left expression</param>
        /// <param name="right">The right expression</param>
        /// <returns>The CompareTo expression</returns>
        protected static Expression GetCompareToExpression<T>(Expression left, Expression right)
        {
            return Expression.Call(left, typeof(T).GetMethod("CompareTo", new Type[] { typeof(string) }), new[] { right });
        }

        /// <summary>
        /// Translates condition expression for less than operator with string values
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionLessThanString(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));
            foreach (object value in c.Values)
            {
                var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, value);
                var transformedLeftHandSideExpression = TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, leftHandSideExpression);

                var rightHandSideExpression = TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, GetAppropiateTypedValueAndType(value, tc.AttributeType));

                //var compareToMethodCall = Expression.Call(transformedLeftHandSideExpression, typeof(string).GetMethod("CompareTo", new Type[] { typeof(string) }), new[] { rightHandSideExpression });
                var compareToMethodCall = GetCompareToExpression<string>(transformedLeftHandSideExpression, rightHandSideExpression);

                expOrValues = Expression.Or(expOrValues,
                        Expression.LessThan(compareToMethodCall, Expression.Constant(0)));
            }
            return Expression.AndAlso(
                            containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                expOrValues));
        }

        /// <summary>
        /// Translates condition expression for less than operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionLessThan(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            if (c.Values.Count(v => v != null) != 1)
            {
                throw new FaultException(new FaultReason($"The ConditonOperator.{c.Operator} requires 1 value/s, not {c.Values.Count(v => v != null)}. Parameter Name: {c.AttributeName}"));
            }

            if (tc.AttributeType == typeof(string))
            {
                return TranslateConditionExpressionLessThanString(tc, getAttributeValueExpr, containsAttributeExpr);
            }
            else if (GetAppropiateTypeForValue(c.Values[0]) == typeof(string))
            {
                return TranslateConditionExpressionLessThanString(tc, getAttributeValueExpr, containsAttributeExpr);
            }
            else
            {
                BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));
                foreach (object value in c.Values)
                {
                    var leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, value);
                    var transformedExpression = TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, leftHandSideExpression);

                    expOrValues = Expression.Or(expOrValues,
                            Expression.LessThan(
                                transformedExpression,
                                TransformExpressionValueBasedOnOperator(tc.CondExpression.Operator, GetAppropiateTypedValueAndType(value, tc.AttributeType))));
                }
                return Expression.AndAlso(
                                containsAttributeExpr,
                                Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                    expOrValues));
            }

        }

        /// <summary>
        /// Translates condition expression for last operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionLast(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            var beforeDateTime = default(DateTime);
            var currentDateTime = DateTime.UtcNow;
            switch (c.Operator)
            {
                case ConditionOperator.LastXHours:
                    beforeDateTime = currentDateTime.AddHours(-(int)c.Values[0]);
                    break;
                case ConditionOperator.LastXDays:
                    beforeDateTime = currentDateTime.AddDays(-(int)c.Values[0]);
                    break;
                case ConditionOperator.Last7Days:
                    beforeDateTime = currentDateTime.AddDays(-7);
                    break;
                case ConditionOperator.LastXWeeks:
                    beforeDateTime = currentDateTime.AddDays(-7 * (int)c.Values[0]);
                    break;
                case ConditionOperator.LastXMonths:
                    beforeDateTime = currentDateTime.AddMonths(-(int)c.Values[0]);
                    break;
                case ConditionOperator.LastXYears:
                    beforeDateTime = currentDateTime.AddYears(-(int)c.Values[0]);
                    break;
            }

            c.Values.Clear();          
            c.Values.Add(beforeDateTime);
            c.Values.Add(currentDateTime);
            
            return TranslateConditionExpressionBetween(tc, getAttributeValueExpr, containsAttributeExpr);
        }

        /// <summary>
        /// Takes a condition expression which needs translating into a 'between two dates' expression and works out the relevant dates
        /// Respects the context's SystemTimeZone setting for timezone-aware date calculations
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <param name="context">The faked context</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionBetweenDates(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr, XrmFakedContext context)
        {
            var c = tc.CondExpression;

            DateTime? fromDate = null;
            DateTime? toDate = null;

            // Use the context's SystemTimeZone to get "today" in the correct timezone
            // This allows tests to specify timezone and get correct date ranges
            var today = context.SystemTimeZone != null
                ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, context.SystemTimeZone).Date
                : DateTime.Today;
            var thisYear = today.Year;
            var thisMonth = today.Month;


            switch (c.Operator)
            {
                case ConditionOperator.ThisYear: // From first day of this year to last day of this year
                    fromDate = new DateTime(thisYear, 1, 1);
                    toDate = new DateTime(thisYear, 12, 31, 23, 59, 59, 999);
                    break;
                case ConditionOperator.LastYear: // From first day of last year to last day of last year
                    fromDate = new DateTime(thisYear - 1, 1, 1);
                    toDate = new DateTime(thisYear - 1, 12, 31, 23, 59, 59, 999);
                    break;
                case ConditionOperator.NextYear: // From first day of next year to last day of next year
                    fromDate = new DateTime(thisYear + 1, 1, 1);
                    toDate = new DateTime(thisYear + 1, 12, 31, 23, 59, 59, 999);
                    break;
                case ConditionOperator.ThisMonth: // From first day of this month to last day of this month
                    fromDate = new DateTime(thisYear, thisMonth, 1);
                    // Last day of this month: Add one month to the first of this month, then remove one day, include full day
                    toDate = new DateTime(thisYear, thisMonth, 1).AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                    break;
                case ConditionOperator.LastMonth: // From first day of last month to last day of last month
                    fromDate = new DateTime(thisYear, thisMonth, 1).AddMonths(-1);
                    // Last day of last month: One day before the first of this month, include full day
                    toDate = new DateTime(thisYear, thisMonth, 1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                    break;
                case ConditionOperator.NextMonth: // From first day of next month to last day of next month
                    fromDate = new DateTime(thisYear, thisMonth, 1).AddMonths(1);
                    // Last day of Next Month: Add two months to the first of this month, then go back one day, include full day
                    toDate = new DateTime(thisYear, thisMonth, 1).AddMonths(2).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                    break;
                case ConditionOperator.ThisWeek:
                    fromDate = today.ToFirstDayOfDeltaWeek();
                    // Include full end day by going to end of the previous day, then adding full day
                    toDate = today.ToLastDayOfDeltaWeek().AddDays(1).AddMilliseconds(-1);
                    break;
                case ConditionOperator.LastWeek:
                    fromDate = today.ToFirstDayOfDeltaWeek(-1);
                    toDate = today.ToLastDayOfDeltaWeek(-1).AddDays(1).AddMilliseconds(-1);
                    break;
                case ConditionOperator.NextWeek:
                    fromDate = today.ToFirstDayOfDeltaWeek(1);
                    toDate = today.ToLastDayOfDeltaWeek(1).AddDays(1).AddMilliseconds(-1);
                    break;
                case ConditionOperator.InFiscalYear:
                    var fiscalYear = (int)c.Values[0];
                    c.Values.Clear();
                    var fiscalYearDate = context.FiscalYearSettings?.StartDate ?? new DateTime(fiscalYear, 4, 1);
                    fromDate = fiscalYearDate;
                    toDate = fiscalYearDate.AddYears(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                    break;
                case ConditionOperator.InFiscalPeriod:
                case ConditionOperator.InFiscalPeriodAndYear:
                    {
                        // InFiscalPeriod takes (period) or (year, period) as values
                        int periodYear;
                        int periodNumber;
                        if (c.Values.Count == 1)
                        {
                            // Just period number, use current fiscal year
                            periodNumber = Convert.ToInt32(c.Values[0]);
                            var fiscalStart = context.FiscalYearSettings?.StartDate ?? new DateTime(today.Year, 4, 1);
                            periodYear = today >= fiscalStart ? today.Year : today.Year - 1;
                        }
                        else
                        {
                            // Year and period
                            periodYear = Convert.ToInt32(c.Values[0]);
                            periodNumber = Convert.ToInt32(c.Values[1]);
                        }
                        c.Values.Clear();
                        var periodBounds = GetFiscalPeriodBounds(context, periodYear, periodNumber, today);
                        fromDate = periodBounds.start;
                        toDate = periodBounds.end;
                    }
                    break;
                case ConditionOperator.ThisFiscalPeriod:
                    {
                        c.Values.Clear();
                        var currentPeriod = GetCurrentFiscalPeriod(context, today);
                        var periodBounds = GetFiscalPeriodBounds(context, currentPeriod.year, currentPeriod.period, today);
                        fromDate = periodBounds.start;
                        toDate = periodBounds.end;
                    }
                    break;
                case ConditionOperator.LastFiscalPeriod:
                    {
                        c.Values.Clear();
                        var currentPeriod = GetCurrentFiscalPeriod(context, today);
                        var lastPeriod = GetOffsetFiscalPeriod(context, currentPeriod.year, currentPeriod.period, -1, today);
                        var periodBounds = GetFiscalPeriodBounds(context, lastPeriod.year, lastPeriod.period, today);
                        fromDate = periodBounds.start;
                        toDate = periodBounds.end;
                    }
                    break;
                case ConditionOperator.NextFiscalPeriod:
                    {
                        c.Values.Clear();
                        var currentPeriod = GetCurrentFiscalPeriod(context, today);
                        var nextPeriod = GetOffsetFiscalPeriod(context, currentPeriod.year, currentPeriod.period, 1, today);
                        var periodBounds = GetFiscalPeriodBounds(context, nextPeriod.year, nextPeriod.period, today);
                        fromDate = periodBounds.start;
                        toDate = periodBounds.end;
                    }
                    break;
            }

            c.Values.Add(fromDate);
            c.Values.Add(toDate);

            return TranslateConditionExpressionBetween(tc, getAttributeValueExpr, containsAttributeExpr);
        }

        /// <summary>
        /// Gets the number of periods in a fiscal year based on the fiscal period template.
        /// </summary>
        /// <param name="template">The fiscal period template.</param>
        /// <returns>The number of periods in the fiscal year.</returns>
        protected static int GetPeriodsPerYear(FiscalYearSettings.Template template)
        {
            switch (template)
            {
                case FiscalYearSettings.Template.Annually:
                    return 1;
                case FiscalYearSettings.Template.SemiAnnually:
                    return 2;
                case FiscalYearSettings.Template.Quarterly:
                    return 4;
                case FiscalYearSettings.Template.Monthly:
                    return 12;
                case FiscalYearSettings.Template.FourWeek:
                    return 13;
                default:
                    return 4; // Default to quarterly
            }
        }

        /// <summary>
        /// Calculates the start and end dates for a specific fiscal period.
        /// </summary>
        /// <param name="context">The XrmFakedContext containing FiscalYearSettings.</param>
        /// <param name="fiscalYear">The fiscal year number.</param>
        /// <param name="periodNumber">The 1-based period number within the fiscal year.</param>
        /// <param name="referenceDate">Reference date for calculating default fiscal year start.</param>
        /// <returns>A tuple containing the start and end DateTime for the fiscal period.</returns>
        protected static (DateTime start, DateTime end) GetFiscalPeriodBounds(XrmFakedContext context, int fiscalYear, int periodNumber, DateTime referenceDate)
        {
            // Get fiscal year settings or use defaults (April 1st start, Quarterly)
            var fiscalYearStart = context.FiscalYearSettings?.StartDate ?? new DateTime(fiscalYear, 4, 1);
            var template = context.FiscalYearSettings?.FiscalPeriodTemplate ?? FiscalYearSettings.Template.Quarterly;

            // Make sure fiscalYearStart uses the correct year
            fiscalYearStart = new DateTime(fiscalYear, fiscalYearStart.Month, fiscalYearStart.Day);

            var periodsPerYear = GetPeriodsPerYear(template);

            // Validate period number
            if (periodNumber < 1 || periodNumber > periodsPerYear)
            {
                throw new Exception($"Period number {periodNumber} is out of range for template {template}. Valid range is 1-{periodsPerYear}.");
            }

            DateTime periodStart;
            DateTime periodEnd;

            if (template == FiscalYearSettings.Template.FourWeek)
            {
                // Four-week periods (13 periods of 28 days each, plus extra days in last period)
                var periodLengthDays = 28;
                periodStart = fiscalYearStart.AddDays((periodNumber - 1) * periodLengthDays);

                if (periodNumber == periodsPerYear)
                {
                    // Last period goes to end of fiscal year
                    periodEnd = fiscalYearStart.AddYears(1).AddMilliseconds(-1);
                }
                else
                {
                    periodEnd = periodStart.AddDays(periodLengthDays).AddMilliseconds(-1);
                }
            }
            else
            {
                // Month-based periods (Annually, SemiAnnually, Quarterly, Monthly)
                var monthsPerPeriod = 12 / periodsPerYear;
                periodStart = fiscalYearStart.AddMonths((periodNumber - 1) * monthsPerPeriod);
                periodEnd = periodStart.AddMonths(monthsPerPeriod).AddMilliseconds(-1);
            }

            return (periodStart, periodEnd);
        }

        /// <summary>
        /// Determines the current fiscal period based on today's date.
        /// </summary>
        /// <param name="context">The XrmFakedContext containing FiscalYearSettings.</param>
        /// <param name="today">Today's date.</param>
        /// <returns>A tuple containing the fiscal year and 1-based period number.</returns>
        protected static (int year, int period) GetCurrentFiscalPeriod(XrmFakedContext context, DateTime today)
        {
            // Get fiscal year settings or use defaults
            var fiscalYearStartMonth = context.FiscalYearSettings?.StartDate.Month ?? 4;
            var fiscalYearStartDay = context.FiscalYearSettings?.StartDate.Day ?? 1;
            var template = context.FiscalYearSettings?.FiscalPeriodTemplate ?? FiscalYearSettings.Template.Quarterly;

            // Determine which fiscal year we're in
            var fiscalYearStartForThisYear = new DateTime(today.Year, fiscalYearStartMonth, fiscalYearStartDay);
            int fiscalYear;
            DateTime fiscalYearStart;

            if (today >= fiscalYearStartForThisYear)
            {
                fiscalYear = today.Year;
                fiscalYearStart = fiscalYearStartForThisYear;
            }
            else
            {
                fiscalYear = today.Year - 1;
                fiscalYearStart = new DateTime(fiscalYear, fiscalYearStartMonth, fiscalYearStartDay);
            }

            var periodsPerYear = GetPeriodsPerYear(template);
            var daysSinceFiscalYearStart = (today - fiscalYearStart).Days;

            int period;
            if (template == FiscalYearSettings.Template.FourWeek)
            {
                // Four-week periods (28 days each)
                period = Math.Min((daysSinceFiscalYearStart / 28) + 1, periodsPerYear);
            }
            else
            {
                // Month-based periods
                var monthsPerPeriod = 12 / periodsPerYear;
                var monthsSinceFiscalYearStart = ((today.Year - fiscalYearStart.Year) * 12 + today.Month - fiscalYearStart.Month);

                // Handle edge case when today is before the start day of the month
                if (today.Day < fiscalYearStartDay && monthsSinceFiscalYearStart > 0)
                {
                    monthsSinceFiscalYearStart--;
                }

                period = Math.Min((monthsSinceFiscalYearStart / monthsPerPeriod) + 1, periodsPerYear);
            }

            return (fiscalYear, period);
        }

        /// <summary>
        /// Calculates a fiscal period offset from a given period (for LastFiscalPeriod and NextFiscalPeriod).
        /// </summary>
        /// <param name="context">The XrmFakedContext containing FiscalYearSettings.</param>
        /// <param name="fiscalYear">The current fiscal year.</param>
        /// <param name="periodNumber">The current 1-based period number.</param>
        /// <param name="offset">The offset to apply (negative for previous, positive for next).</param>
        /// <param name="referenceDate">Reference date for calculations.</param>
        /// <returns>A tuple containing the new fiscal year and period number.</returns>
        protected static (int year, int period) GetOffsetFiscalPeriod(XrmFakedContext context, int fiscalYear, int periodNumber, int offset, DateTime referenceDate)
        {
            var template = context.FiscalYearSettings?.FiscalPeriodTemplate ?? FiscalYearSettings.Template.Quarterly;
            var periodsPerYear = GetPeriodsPerYear(template);

            var newPeriod = periodNumber + offset;
            var newYear = fiscalYear;

            while (newPeriod < 1)
            {
                newPeriod += periodsPerYear;
                newYear--;
            }

            while (newPeriod > periodsPerYear)
            {
                newPeriod -= periodsPerYear;
                newYear++;
            }

            return (newYear, newPeriod);
        }

        /// <summary>
        /// Translates condition expression for older than operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionOlderThan(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            var valueToAdd = 0;

            if (!int.TryParse(c.Values[0].ToString(), out valueToAdd))
            {
                throw new Exception(c.Operator + " requires an integer value in the ConditionExpression.");
            }

            if (valueToAdd <= 0)
            {
                throw new Exception(c.Operator + " requires a value greater than 0.");
            }

            DateTime toDate = default(DateTime);

            switch (c.Operator)
            {
                case ConditionOperator.OlderThanXMonths:
                    toDate = DateTime.UtcNow.AddMonths(-valueToAdd);
                    break;
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013
                case ConditionOperator.OlderThanXMinutes:      
                    toDate = DateTime.UtcNow.AddMinutes(-valueToAdd);
                    break;
                case ConditionOperator.OlderThanXHours: 
                    toDate = DateTime.UtcNow.AddHours(-valueToAdd);
                    break;
                case ConditionOperator.OlderThanXDays: 
                    toDate = DateTime.UtcNow.AddDays(-valueToAdd);
                    break;
                case ConditionOperator.OlderThanXWeeks:              
                    toDate = DateTime.UtcNow.AddDays(-7 * valueToAdd);
                    break;              
                case ConditionOperator.OlderThanXYears: 
                    toDate = DateTime.UtcNow.AddYears(-valueToAdd);
                    break;
#endif
            }
                        
            return TranslateConditionExpressionOlderThan(tc, getAttributeValueExpr, containsAttributeExpr, toDate);
        }
     

        /// <summary>
        /// Translates condition expression for between operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionBetween(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            object value1, value2;
            value1 = c.Values[0];
            value2 = c.Values[1];

            // For DateTime comparisons, if the end date is at midnight (00:00:00), extend it to the end of that day
            // This matches Dynamics 365 behavior where Between with dates includes the full end day
            if (value2 is DateTime endDateTime)
            {
                if (endDateTime.TimeOfDay == TimeSpan.Zero)
                {
                    // End date is at midnight, extend to 23:59:59.999
                    value2 = endDateTime.Date.AddDays(1).AddMilliseconds(-1);
                    c.Values[1] = value2; // Update the original collection as well
                }
            }

            //Between the range...
            var exp = Expression.And(
                Expression.GreaterThanOrEqual(
                            GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, value1),
                            GetAppropiateTypedValueAndType(value1, tc.AttributeType)),

                Expression.LessThanOrEqual(
                            GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, value2),
                            GetAppropiateTypedValueAndType(value2, tc.AttributeType)));


            //and... attribute exists too
            return Expression.AndAlso(
                            containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                exp));
        }

        /// <summary>
        /// Translates condition expression for null operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionNull(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            // For joined entities, the attribute value is wrapped in an AliasedValue object.
            // We need to check if AliasedValue.Value is null, not if the AliasedValue itself is null.
            // Fixes upstream issue #547 - ConditionOperator.Null broken in nested filters
            var getValueFromAliasedValueExpr = Expression.Call(
                Expression.Convert(getAttributeValueExpr, typeof(AliasedValue)),
                typeof(AliasedValue).GetMethod("get_Value"));

            // Check if the unwrapped value is null (for AliasedValue)
            var aliasedValueIsNull = Expression.Equal(getValueFromAliasedValueExpr, Expression.Constant(null));

            // Check if the direct value is null (for non-AliasedValue)
            var directValueIsNull = Expression.Equal(getAttributeValueExpr, Expression.Constant(null));

            // Use conditional: if it's an AliasedValue, check AliasedValue.Value; otherwise check the value directly
            var valueIsNullExpr = Expression.Condition(
                Expression.TypeIs(getAttributeValueExpr, typeof(AliasedValue)),
                aliasedValueIsNull,
                directValueIsNull);

            return Expression.Or(Expression.AndAlso(
                                    containsAttributeExpr,
                                    valueIsNullExpr),              //Attribute is null (handles both direct and AliasedValue)
                                 Expression.AndAlso(
                                    Expression.Not(containsAttributeExpr),
                                    Expression.Constant(true)));   //Or attribute is not defined (null)
        }

        /// <summary>
        /// Translates condition expression for older than operator with specific date
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <param name="olderThanDate">The date to compare against</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionOlderThan(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr, DateTime olderThanDate)
        {
            var lessThanExpression = Expression.LessThan(
                            GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, olderThanDate),
                            GetAppropiateTypedValueAndType(olderThanDate, tc.AttributeType));

            return Expression.AndAlso(containsAttributeExpr,
                            Expression.AndAlso(Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                                lessThanExpression));
        }

        /// <summary>
        /// Translates condition expression for ends with operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionEndsWith(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            // Defensive null checks (resolves upstream issue #607)
            if (c.Values == null || c.Values.Count == 0)
            {
                return Expression.Constant(false);
            }

            //Append a ´%´at the end of each condition value
            var computedCondition = new ConditionExpression(c.AttributeName, c.Operator,
                c.Values.Where(x => x != null).Select(x => "%" + x.ToString()).ToList());
      var typedComputedCondition = new TypedConditionExpression(computedCondition)
      {
        AttributeType = tc.AttributeType
      };

      return TranslateConditionExpressionLike(typedComputedCondition, getAttributeValueExpr, containsAttributeExpr);
        }

        /// <summary>
        /// Gets a ToString expression for the given type
        /// </summary>
        /// <typeparam name="T">The type to convert to string</typeparam>
        /// <param name="e">The expression to convert</param>
        /// <returns>The ToString expression</returns>
        protected static Expression GetToStringExpression<T>(Expression e)
        {
            return Expression.Call(e, typeof(T).GetMethod("ToString", new Type[] { }));
        }

        /// <summary>
        /// Gets a case insensitive expression by calling ToLowerInvariant
        /// </summary>
        /// <param name="e">The expression to make case insensitive</param>
        /// <returns>The case insensitive expression</returns>
        protected static Expression GetCaseInsensitiveExpression(Expression e)
        {
            return Expression.Call(e,
                                typeof(string).GetMethod("ToLowerInvariant", new Type[] { }));
        }

        /// <summary>
        /// Creates a null-safe case insensitive expression that returns null if the input is null,
        /// otherwise returns the lowercase version of the string.
        /// This prevents NullReferenceException when attribute values are null.
        /// </summary>
        protected static Expression GetNullSafeCaseInsensitiveExpression(Expression e)
        {
            // Create: e == null ? null : e.ToLowerInvariant()
            var nullCheck = Expression.Equal(e, Expression.Constant(null, e.Type));
            var toLowerCall = Expression.Call(e, typeof(string).GetMethod("ToLowerInvariant", new Type[] { }));

            return Expression.Condition(
                nullCheck,
                Expression.Constant(null, typeof(string)),
                toLowerCall
            );
        }

        /// <summary>
        /// Checks if a LIKE pattern contains advanced wildcards that require regex processing.
        /// Advanced wildcards include: _ (single character), [ (character sets/ranges).
        /// </summary>
        /// <param name="pattern">The LIKE pattern to check.</param>
        /// <returns>True if the pattern contains advanced wildcards requiring regex; false for simple % patterns only.</returns>
        protected static bool LikePatternRequiresRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            // Check for underscore or bracket characters that indicate advanced patterns
            return pattern.Contains("_") || pattern.Contains("[");
        }

        /// <summary>
        /// Converts a SQL LIKE pattern to a .NET Regex pattern.
        /// Handles: % (any characters), _ (single character), [A-Z] (character ranges),
        /// [ABC] (character sets), [^ABC] (negated sets).
        /// </summary>
        /// <param name="likePattern">The SQL LIKE pattern to convert.</param>
        /// <returns>A .NET Regex pattern string.</returns>
        public static string ConvertLikePatternToRegex(string likePattern)
        {
            if (string.IsNullOrEmpty(likePattern))
                return "^$"; // Match empty string only

            var result = new StringBuilder();
            result.Append("^"); // Anchor at start

            bool insideBracket = false;
            for (int i = 0; i < likePattern.Length; i++)
            {
                char c = likePattern[i];

                if (insideBracket)
                {
                    // Inside brackets, pass through characters as-is (they're already regex-compatible)
                    result.Append(c);
                    if (c == ']')
                    {
                        insideBracket = false;
                    }
                }
                else
                {
                    switch (c)
                    {
                        case '%':
                            // % matches zero or more characters -> .*
                            result.Append(".*");
                            break;
                        case '_':
                            // _ matches exactly one character -> .
                            result.Append(".");
                            break;
                        case '[':
                            // Start of character class - pass through to regex
                            result.Append('[');
                            insideBracket = true;
                            break;
                        // Escape regex special characters (outside brackets)
                        case '.':
                        case '^':
                        case '$':
                        case '*':
                        case '+':
                        case '?':
                        case '{':
                        case '}':
                        case '\\':
                        case '|':
                        case '(':
                        case ')':
                            result.Append('\\');
                            result.Append(c);
                            break;
                        default:
                            result.Append(c);
                            break;
                    }
                }
            }

            result.Append("$"); // Anchor at end
            return result.ToString();
        }

        /// <summary>
        /// Performs a LIKE pattern match using regex for advanced patterns.
        /// This method is called at runtime via Expression.Call.
        /// </summary>
        /// <param name="input">The string to match against.</param>
        /// <param name="regexPattern">The regex pattern converted from LIKE syntax.</param>
        /// <returns>True if the input matches the pattern; false otherwise.</returns>
        public static bool MatchLikePattern(string input, string regexPattern)
        {
            if (input == null)
                return false;
            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Translates condition expression for like operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionLike(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            // Defensive null checks (resolves upstream issue #608)
            if (c.Values == null || c.Values.Count == 0)
            {
                return Expression.Constant(false);
            }

            BinaryExpression expOrValues = Expression.Or(Expression.Constant(false), Expression.Constant(false));

            // Check if the RAW attribute value is null BEFORE doing any conversions
            // In Dataverse, empty strings are converted to null, so we treat null as empty string for comparisons
            var rawValueIsNull = Expression.Equal(getAttributeValueExpr, Expression.Constant(null));

            // Get the appropriately cast attribute value expression
            Expression attributeValueExpr = GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, c.Values[0]);

            // Convert to string
            Expression convertedValueToStr = Expression.Convert(attributeValueExpr, typeof(string));

            // Create null-safe string: if raw value is null, use empty string, otherwise use the converted value
            var safeString = Expression.Condition(
                rawValueIsNull,
                Expression.Constant("", typeof(string)),
                convertedValueToStr
            );

            Expression convertedValueToStrAndToLower = Expression.Call(
                safeString,
                typeof(string).GetMethod("ToLowerInvariant", new Type[] { })
            );

            string sLikeOperator = "%";
            foreach (object value in c.Values)
            {
                // Skip null values to prevent NullReferenceException
                if (value == null)
                    continue;

                var strValue = value.ToString();

                // Check if this pattern requires regex (contains _, [, or ])
                if (LikePatternRequiresRegex(strValue))
                {
                    // Use regex for advanced pattern matching
                    var regexPattern = ConvertLikePatternToRegex(strValue);

                    // Call MatchLikePattern(safeString, regexPattern)
                    // Note: Using safeString (not lowercased) because regex uses IgnoreCase
                    var matchMethod = typeof(XrmFakedContext).GetMethod("MatchLikePattern", new Type[] { typeof(string), typeof(string) });
                    expOrValues = Expression.Or(expOrValues, Expression.Call(
                        matchMethod,
                        safeString,
                        Expression.Constant(regexPattern)
                    ));
                }
                else
                {
                    // Simple % pattern - use existing optimized StartsWith/EndsWith/Contains logic
                    string sMethod = "";

                    if (strValue.EndsWith(sLikeOperator) && strValue.StartsWith(sLikeOperator))
                        sMethod = "Contains";
                    else if (strValue.StartsWith(sLikeOperator))
                        sMethod = "EndsWith";
                    else
                        sMethod = "StartsWith";

                    expOrValues = Expression.Or(expOrValues, Expression.Call(
                        convertedValueToStrAndToLower,
                        typeof(string).GetMethod(sMethod, new Type[] { typeof(string) }),
                        Expression.Constant(strValue.ToLowerInvariant().Replace("%", ""))
                    ));
                }
            }

            // Attribute must exist for the comparison to proceed
            return Expression.AndAlso(containsAttributeExpr, expOrValues);
        }

        /// <summary>
        /// Translates condition expression for contains operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionContains(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            // Defensive null checks (resolves upstream issue #607)
            if (c.Values == null || c.Values.Count == 0)
            {
                return Expression.Constant(false);
            }

            //Append a ´%´at the end of each condition value
            var computedCondition = new ConditionExpression(c.AttributeName, c.Operator,
                c.Values.Where(x => x != null).Select(x => "%" + x.ToString() + "%").ToList());
      var computedTypedCondition = new TypedConditionExpression(computedCondition)
      {
        AttributeType = tc.AttributeType
      };

      return TranslateConditionExpressionLike(computedTypedCondition, getAttributeValueExpr, containsAttributeExpr);

        }

        /// <summary>
        /// Translates multiple condition expressions with a logical operator
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="sEntityName">The entity name</param>
        /// <param name="conditions">The list of condition expressions</param>
        /// <param name="op">The logical operator</param>
        /// <param name="entity">The entity parameter expression</param>
        /// <param name="bIsOuter">Whether this is an outer join</param>
        /// <returns>The translated binary expression</returns>
        protected static BinaryExpression TranslateMultipleConditionExpressions(QueryExpression qe, XrmFakedContext context, string sEntityName, List<ConditionExpression> conditions, LogicalOperator op, ParameterExpression entity, bool bIsOuter)
        {
            BinaryExpression binaryExpression = null;  //Default initialisation depending on logical operator
            if (op == LogicalOperator.And)
                binaryExpression = Expression.And(Expression.Constant(true), Expression.Constant(true));
            else
                binaryExpression = Expression.Or(Expression.Constant(false), Expression.Constant(false));

            foreach (var c in conditions)
            {
                var cEntityName = sEntityName;
        //Create a new typed expression
        var typedExpression = new TypedConditionExpression(c)
        {
          IsOuter = bIsOuter
        };

        // Check for column-to-column comparison
        // Method 1: FetchXML valueof attribute - ColumnComparisonValue marker is stored in the Values array
        if (c.Values != null && c.Values.Count == 1 && c.Values[0] is ColumnComparisonValue)
                {
                    typedExpression.ValueOfAttribute = ((ColumnComparisonValue)c.Values[0]).ColumnName;
                }
#if FAKE_XRM_EASY_9
                // Method 2: SDK QueryExpression CompareColumns property (D365 v9.x+)
                // When CompareColumns is true, the first value in Values is the column name to compare against
                else if (c.CompareColumns && c.Values != null && c.Values.Count == 1 && c.Values[0] is string)
                {
                    typedExpression.ValueOfAttribute = (string)c.Values[0];
                }
#endif

                string sAttributeName = c.AttributeName;

                //Find the attribute type if using early bound entities
                if (context.ProxyTypesAssembly != null)
                {

#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
                    if (c.EntityName != null)
                        cEntityName = qe.GetEntityNameFromAlias(c.EntityName);
                    else
                    {
                        if (c.AttributeName.IndexOf(".") >= 0)
                        {
                            var alias = c.AttributeName.Split('.')[0];
                            cEntityName = qe.GetEntityNameFromAlias(alias);
                            sAttributeName = c.AttributeName.Split('.')[1];
                        }
                    }

#else
                    //CRM 2011
                    if (c.AttributeName.IndexOf(".") >= 0) {
                        var alias = c.AttributeName.Split('.')[0];
                        cEntityName = qe.GetEntityNameFromAlias(alias);
                        sAttributeName = c.AttributeName.Split('.')[1];
                    }
#endif

                    var earlyBoundType = context.FindReflectedType(cEntityName);
                    if (earlyBoundType != null)
                    {
                        typedExpression.AttributeType = context.FindReflectedAttributeType(earlyBoundType, cEntityName, sAttributeName);

                        // Special case when filtering on the name of a Lookup
                        if (typedExpression.AttributeType == typeof(EntityReference) && sAttributeName.EndsWith("name"))
                        {
                            var realAttributeName = c.AttributeName.Substring(0, c.AttributeName.Length - 4);

                            if (GetEarlyBoundTypeAttribute(earlyBoundType, sAttributeName) == null && GetEarlyBoundTypeAttribute(earlyBoundType, realAttributeName) != null && GetEarlyBoundTypeAttribute(earlyBoundType, realAttributeName).PropertyType == typeof(EntityReference))
                            {
                                // Need to make Lookups work against the real attribute, not the "name" suffixed attribute that doesn't exist
                                c.AttributeName = realAttributeName;
                            }
                        }
                    }
                }

                ValidateSupportedTypedExpression(typedExpression);

                //Build a binary expression  
                if (op == LogicalOperator.And)
                {
                    binaryExpression = Expression.And(binaryExpression, TranslateConditionExpression(qe, context, typedExpression, entity));
                }
                else
                    binaryExpression = Expression.Or(binaryExpression, TranslateConditionExpression(qe, context, typedExpression, entity));
            }

            return binaryExpression;
        }

        /// <summary>
        /// Translates multiple filter expressions with a logical operator
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="sEntityName">The entity name</param>
        /// <param name="filters">The list of filter expressions</param>
        /// <param name="op">The logical operator</param>
        /// <param name="entity">The entity parameter expression</param>
        /// <param name="bIsOuter">Whether this is an outer join</param>
        /// <returns>The translated binary expression</returns>
        protected static BinaryExpression TranslateMultipleFilterExpressions(QueryExpression qe, XrmFakedContext context, string sEntityName, List<FilterExpression> filters, LogicalOperator op, ParameterExpression entity, bool bIsOuter)
        {
            BinaryExpression binaryExpression = null;
            if (op == LogicalOperator.And)
                binaryExpression = Expression.And(Expression.Constant(true), Expression.Constant(true));
            else
                binaryExpression = Expression.Or(Expression.Constant(false), Expression.Constant(false));

            foreach (var f in filters)
            {
                var thisFilterLambda = TranslateFilterExpressionToExpression(qe, context, sEntityName, f, entity, bIsOuter);

                //Build a binary expression  
                if (op == LogicalOperator.And)
                {
                    binaryExpression = Expression.And(binaryExpression, thisFilterLambda);
                }
                else
                    binaryExpression = Expression.Or(binaryExpression, thisFilterLambda);
            }

            return binaryExpression;
        }

        /// <summary>
        /// Translates linked entity filter expression to expression
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="le">The link entity</param>
        /// <param name="entity">The entity parameter expression</param>
        /// <returns>The list of translated expressions</returns>
        protected static List<Expression> TranslateLinkedEntityFilterExpressionToExpression(QueryExpression qe, XrmFakedContext context, LinkEntity le, ParameterExpression entity)
        {
            //In CRM 2011, condition expressions are at the LinkEntity level without an entity name
            //From CRM 2013, condition expressions were moved to outside the LinkEntity object at the QueryExpression level,
            //with an EntityName alias attribute

            //If we reach this point, it means we are translating filters at the Link Entity level (2011),
            //Therefore we need to prepend the alias attribute because the code to generate attributes for Joins (JoinAttribute extension) is common across versions
            var linkedEntitiesQueryExpressions = new List<Expression>();

            if (le.LinkCriteria != null)
            {
                var earlyBoundType = context.FindReflectedType(le.LinkToEntityName);
                ConcurrentDictionary<string, string> attributeMetadata;
                context.AttributeMetadataNames.TryGetValue(le.LinkToEntityName, out attributeMetadata);

                foreach (var ce in le.LinkCriteria.Conditions)
                {
                    if (earlyBoundType != null)
                    {
                        var attributeInfo = GetEarlyBoundTypeAttribute(earlyBoundType, ce.AttributeName);
                        if (attributeInfo == null && ce.AttributeName.EndsWith("name"))
                        {
                            // Special case for referencing the name of a EntityReference
                            var sAttributeName = ce.AttributeName.Substring(0, ce.AttributeName.Length - 4);
                            attributeInfo = GetEarlyBoundTypeAttribute(earlyBoundType, sAttributeName);

                            if (attributeInfo.PropertyType == typeof(EntityReference))
                            {
                                // Don't mess up if other attributes follow this naming pattern
                                ce.AttributeName = sAttributeName;
                            }
                        }
                    }
                    else if (attributeMetadata != null && !attributeMetadata.ContainsKey(ce.AttributeName) && ce.AttributeName.EndsWith("name"))
                    {
                        // Special case for referencing the name of a EntityReference
                        var sAttributeName = ce.AttributeName.Substring(0, ce.AttributeName.Length - 4);
                        if (attributeMetadata.ContainsKey(sAttributeName))
                        {
                            ce.AttributeName = sAttributeName;
                        }
                    }

                    var entityAlias = !string.IsNullOrEmpty(le.EntityAlias) ? le.EntityAlias : le.LinkToEntityName;
                    ce.AttributeName = entityAlias + "." + ce.AttributeName;
                }

                foreach (var fe in le.LinkCriteria.Filters)
                {
                    foreach (var ce in fe.Conditions)
                    {
                        var entityAlias = !string.IsNullOrEmpty(le.EntityAlias) ? le.EntityAlias : le.LinkToEntityName;
                        ce.AttributeName = entityAlias + "." + ce.AttributeName;
                    }
                }
            }

            //Translate this specific Link Criteria
            linkedEntitiesQueryExpressions.Add(TranslateFilterExpressionToExpression(qe, context, le.LinkToEntityName, le.LinkCriteria, entity, le.JoinOperator == JoinOperator.LeftOuter));

            //Processed nested linked entities
            foreach (var nestedLinkedEntity in le.LinkEntities)
            {
                var listOfExpressions = TranslateLinkedEntityFilterExpressionToExpression(qe, context, nestedLinkedEntity, entity);
                linkedEntitiesQueryExpressions.AddRange(listOfExpressions);
            }

            return linkedEntitiesQueryExpressions;
        }

        /// <summary>
        /// Translates query expression filters to expression
        /// </summary>
        /// <param name="context">The faked context</param>
        /// <param name="qe">The query expression</param>
        /// <param name="entity">The entity parameter expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateQueryExpressionFiltersToExpression(XrmFakedContext context, QueryExpression qe, ParameterExpression entity)
        {
            var linkedEntitiesQueryExpressions = new List<Expression>();
            foreach (var le in qe.LinkEntities)
            {
                // Skip Any/All/NotAny/NotAll link entities - their filters are applied during the existence check,
                // not as WHERE clauses on the parent entity
                if (le.JoinOperator == JoinOperator.Any ||
                    le.JoinOperator == JoinOperator.NotAny ||
                    le.JoinOperator == JoinOperator.All ||
                    le.JoinOperator == JoinOperator.NotAll)
                {
                    continue;
                }

                var listOfExpressions = TranslateLinkedEntityFilterExpressionToExpression(qe, context, le, entity);
                linkedEntitiesQueryExpressions.AddRange(listOfExpressions);
            }

            if (linkedEntitiesQueryExpressions.Count > 0 && qe.Criteria != null)
            {
                //Return the and of the two
                Expression andExpression = Expression.Constant(true);
                foreach (var e in linkedEntitiesQueryExpressions)
                {
                    andExpression = Expression.And(e, andExpression);

                }
                var feExpression = TranslateFilterExpressionToExpression(qe, context, qe.EntityName, qe.Criteria, entity, false);
                return Expression.And(andExpression, feExpression);
            }
            else if (linkedEntitiesQueryExpressions.Count > 0)
            {
                //Linked entity expressions only
                Expression andExpression = Expression.Constant(true);
                foreach (var e in linkedEntitiesQueryExpressions)
                {
                    andExpression = Expression.And(e, andExpression);

                }
                return andExpression;
            }
            else
            {
                //Criteria only
                return TranslateFilterExpressionToExpression(qe, context, qe.EntityName, qe.Criteria, entity, false);
            }
        }
        /// <summary>
        /// Translates filter expression to expression
        /// </summary>
        /// <param name="qe">The query expression</param>
        /// <param name="context">The faked context</param>
        /// <param name="sEntityName">The entity name</param>
        /// <param name="fe">The filter expression</param>
        /// <param name="entity">The entity parameter expression</param>
        /// <param name="bIsOuter">Whether this is an outer join</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateFilterExpressionToExpression(QueryExpression qe, XrmFakedContext context, string sEntityName, FilterExpression fe, ParameterExpression entity, bool bIsOuter)
        {
            if (fe == null) return Expression.Constant(true);

            BinaryExpression conditionsLambda = null;
            BinaryExpression filtersLambda = null;
            if (fe.Conditions != null && fe.Conditions.Count > 0)
            {
                conditionsLambda = TranslateMultipleConditionExpressions(qe, context, sEntityName, fe.Conditions.ToList(), fe.FilterOperator, entity, bIsOuter);
            }

            //Process nested filters recursively
            if (fe.Filters != null && fe.Filters.Count > 0)
            {
                filtersLambda = TranslateMultipleFilterExpressions(qe, context, sEntityName, fe.Filters.ToList(), fe.FilterOperator, entity, bIsOuter);
            }

            if (conditionsLambda != null && filtersLambda != null)
            {
                //Satisfy both
                if (fe.FilterOperator == LogicalOperator.And)
                {
                    return Expression.And(conditionsLambda, filtersLambda);
                }
                else
                {
                    return Expression.Or(conditionsLambda, filtersLambda);
                }
            }
            else if (conditionsLambda != null)
                return conditionsLambda;
            else if (filtersLambda != null)
                return filtersLambda;

            return Expression.Constant(true); //Satisfy filter if there are no conditions nor filters
        }
        /// <summary>
        /// Translates condition expression for next operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionNext(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var c = tc.CondExpression;

            var nextDateTime = default(DateTime);
            var currentDateTime = DateTime.UtcNow;
            switch (c.Operator)
            {
                case ConditionOperator.NextXHours:
                    nextDateTime = currentDateTime.AddHours((int)c.Values[0]);
                    break;
                case ConditionOperator.NextXDays:
                    nextDateTime = currentDateTime.AddDays((int)c.Values[0]);
                    break;
                case ConditionOperator.Next7Days:
                    nextDateTime = currentDateTime.AddDays(7);
                    break;
                case ConditionOperator.NextXWeeks:                  
                    nextDateTime = currentDateTime.AddDays(7 * (int)c.Values[0]);
                    break;              
                case ConditionOperator.NextXMonths:
                    nextDateTime = currentDateTime.AddMonths((int)c.Values[0]);
                    break;
                case ConditionOperator.NextXYears:
                    nextDateTime = currentDateTime.AddYears((int)c.Values[0]);
                    break;
            }

            c.Values.Clear();
            c.Values.Add(currentDateTime);
            c.Values.Add(nextDateTime);


            return TranslateConditionExpressionBetween(tc, getAttributeValueExpr, containsAttributeExpr);
        }

#if FAKE_XRM_EASY_9
        /// <summary>
        /// Translates condition expression for contain values operator
        /// </summary>
        /// <param name="tc">The typed condition expression</param>
        /// <param name="getAttributeValueExpr">The attribute value expression</param>
        /// <param name="containsAttributeExpr">The contains attribute expression</param>
        /// <returns>The translated expression</returns>
        protected static Expression TranslateConditionExpressionContainValues(TypedConditionExpression tc, Expression getAttributeValueExpr, Expression containsAttributeExpr)
        {
            var rightHandSideExpression = Expression.Constant(ConvertToHashSetOfInt(tc.CondExpression.Values, isOptionSetValueCollectionAccepted: false));

            // For late-bound entities (tc.AttributeType is null), we need to handle the type check at runtime
            // The leftHandSideExpression needs to convert the attribute value to a HashSet<int> via ConvertToHashSetOfInt
            Expression leftHandSideExpression;
            if (tc.AttributeType != null)
            {
                leftHandSideExpression = GetAppropiateCastExpressionBasedOnType(tc.AttributeType, getAttributeValueExpr, null);
            }
            else
            {
                // For late-bound: dynamically call ConvertToHashSetOfInt on the attribute value at runtime
                // This handles OptionSetValueCollection when the type is not known at compile time
                leftHandSideExpression = GetAppropiateCastExpressionBasedOnOptionSetValueCollection(getAttributeValueExpr);
            }

            return Expression.AndAlso(
                       containsAttributeExpr,
                       Expression.AndAlso(
                           Expression.NotEqual(getAttributeValueExpr, Expression.Constant(null)),
                           Expression.Equal(
                               Expression.Call(leftHandSideExpression, typeof(HashSet<int>).GetMethod("Overlaps"), rightHandSideExpression),
                               Expression.Constant(true))));
        }
#endif

        /// <summary>
        /// Translates a column-to-column comparison expression (FetchXML valueof attribute).
        /// Compares one column's value against another column's value in the same entity row.
        /// Addresses upstream issue #514.
        /// </summary>
        /// <param name="c">The typed condition expression with ValueOfAttribute set.</param>
        /// <param name="entity">The entity parameter expression.</param>
        /// <param name="attributesProperty">Expression to access the entity's Attributes collection.</param>
        /// <param name="containsLeftAttributeExpr">Expression to check if left attribute exists.</param>
        /// <returns>The translated expression, or null if the operator is not supported for column comparison.</returns>
        protected static Expression TranslateColumnComparisonExpression(TypedConditionExpression c, ParameterExpression entity, Expression attributesProperty, Expression containsLeftAttributeExpr)
        {
            // Get the comparison column name from ValueOfAttribute
            var valueOfColumnName = c.ValueOfAttribute;

            // Supported operators for column comparison
            var supportedOperators = new[]
            {
                ConditionOperator.Equal,
                ConditionOperator.NotEqual,
                ConditionOperator.GreaterThan,
                ConditionOperator.GreaterEqual,
                ConditionOperator.LessThan,
                ConditionOperator.LessEqual
            };

            if (!supportedOperators.Contains(c.CondExpression.Operator))
            {
                return null; // Operator not supported for column comparison
            }

            // Build expression to check if the valueof column exists
            Expression containsRightAttributeExpr = Expression.Call(
                attributesProperty,
                typeof(AttributeCollection).GetMethod("ContainsKey", new Type[] { typeof(string) }),
                Expression.Constant(valueOfColumnName)
            );

            // Get the left attribute value (the condition attribute)
            Expression getLeftAttributeValueExpr = Expression.Property(
                attributesProperty, "Item",
                Expression.Constant(c.CondExpression.AttributeName, typeof(string))
            );

            // Get the right attribute value (the valueof column)
            Expression getRightAttributeValueExpr = Expression.Property(
                attributesProperty, "Item",
                Expression.Constant(valueOfColumnName, typeof(string))
            );

            // Unwrap AliasedValue for both sides if present
            Expression unwrapLeftExpr = UnwrapAliasedValue(getLeftAttributeValueExpr);
            Expression unwrapRightExpr = UnwrapAliasedValue(getRightAttributeValueExpr);

            // Get comparable values for both sides (handles EntityReference, Money, OptionSetValue, etc.)
            Expression comparableLeftExpr = GetComparableValueExpression(unwrapLeftExpr);
            Expression comparableRightExpr = GetComparableValueExpression(unwrapRightExpr);

            // Build the comparison expression based on operator
            Expression comparisonExpr;
            switch (c.CondExpression.Operator)
            {
                case ConditionOperator.Equal:
                    comparisonExpr = Expression.Call(
                        typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) }),
                        comparableLeftExpr,
                        comparableRightExpr
                    );
                    break;
                case ConditionOperator.NotEqual:
                    comparisonExpr = Expression.Not(
                        Expression.Call(
                            typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) }),
                            comparableLeftExpr,
                            comparableRightExpr
                        )
                    );
                    break;
                case ConditionOperator.GreaterThan:
                    comparisonExpr = BuildComparableComparisonExpression(comparableLeftExpr, comparableRightExpr, ExpressionType.GreaterThan);
                    break;
                case ConditionOperator.GreaterEqual:
                    comparisonExpr = BuildComparableComparisonExpression(comparableLeftExpr, comparableRightExpr, ExpressionType.GreaterThanOrEqual);
                    break;
                case ConditionOperator.LessThan:
                    comparisonExpr = BuildComparableComparisonExpression(comparableLeftExpr, comparableRightExpr, ExpressionType.LessThan);
                    break;
                case ConditionOperator.LessEqual:
                    comparisonExpr = BuildComparableComparisonExpression(comparableLeftExpr, comparableRightExpr, ExpressionType.LessThanOrEqual);
                    break;
                default:
                    return null;
            }

            // Both attributes must exist and be non-null for comparison to succeed
            return Expression.AndAlso(
                containsLeftAttributeExpr,
                Expression.AndAlso(
                    containsRightAttributeExpr,
                    Expression.AndAlso(
                        Expression.NotEqual(getLeftAttributeValueExpr, Expression.Constant(null)),
                        Expression.AndAlso(
                            Expression.NotEqual(getRightAttributeValueExpr, Expression.Constant(null)),
                            comparisonExpr
                        )
                    )
                )
            );
        }

        /// <summary>
        /// Unwraps an AliasedValue to get the underlying value.
        /// Returns the original expression if not an AliasedValue.
        /// </summary>
        private static Expression UnwrapAliasedValue(Expression input)
        {
            var getValueFromAliasedValueExpr = Expression.Call(
                Expression.Convert(input, typeof(AliasedValue)),
                typeof(AliasedValue).GetMethod("get_Value")
            );

            return Expression.Condition(
                Expression.TypeIs(input, typeof(AliasedValue)),
                getValueFromAliasedValueExpr,
                input
            );
        }

        /// <summary>
        /// Gets a comparable value expression that extracts the underlying value from
        /// EntityReference (Id), Money (Value), OptionSetValue (Value), etc.
        /// </summary>
        private static Expression GetComparableValueExpression(Expression input)
        {
            // Handle EntityReference -> Guid
            var entityRefExpr = Expression.Condition(
                Expression.TypeIs(input, typeof(EntityReference)),
                Expression.Convert(
                    Expression.Call(Expression.TypeAs(input, typeof(EntityReference)),
                        typeof(EntityReference).GetMethod("get_Id")),
                    typeof(object)
                ),
                input
            );

            // Handle Money -> decimal
            var moneyExpr = Expression.Condition(
                Expression.TypeIs(entityRefExpr, typeof(Money)),
                Expression.Convert(
                    Expression.Call(Expression.TypeAs(entityRefExpr, typeof(Money)),
                        typeof(Money).GetMethod("get_Value")),
                    typeof(object)
                ),
                entityRefExpr
            );

            // Handle OptionSetValue -> int
            var optionSetExpr = Expression.Condition(
                Expression.TypeIs(moneyExpr, typeof(OptionSetValue)),
                Expression.Convert(
                    Expression.Call(Expression.TypeAs(moneyExpr, typeof(OptionSetValue)),
                        typeof(OptionSetValue).GetMethod("get_Value")),
                    typeof(object)
                ),
                moneyExpr
            );

            return optionSetExpr;
        }

        /// <summary>
        /// Builds a comparison expression for IComparable types (gt, ge, lt, le).
        /// Uses IComparable.CompareTo for runtime comparison.
        /// </summary>
        private static Expression BuildComparableComparisonExpression(Expression left, Expression right, ExpressionType comparisonType)
        {
            // Call IComparable.CompareTo on the left value
            var compareToMethod = typeof(IComparable).GetMethod("CompareTo", new[] { typeof(object) });

            // Cast left to IComparable and call CompareTo
            var compareToExpr = Expression.Call(
                Expression.Convert(left, typeof(IComparable)),
                compareToMethod,
                right
            );

            // Build comparison: CompareTo(right) > 0, >= 0, < 0, or <= 0
            int threshold = 0;
            switch (comparisonType)
            {
                case ExpressionType.GreaterThan:
                    return Expression.GreaterThan(compareToExpr, Expression.Constant(threshold));
                case ExpressionType.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(compareToExpr, Expression.Constant(threshold));
                case ExpressionType.LessThan:
                    return Expression.LessThan(compareToExpr, Expression.Constant(threshold));
                case ExpressionType.LessThanOrEqual:
                    return Expression.LessThanOrEqual(compareToExpr, Expression.Constant(threshold));
                default:
                    throw new NotSupportedException($"Comparison type {comparisonType} is not supported");
            }
        }

        /// <summary>
        /// Enum representing the type of hierarchy operator being used.
        /// </summary>
        protected enum HierarchyOperatorType
        {
            /// <summary>
            /// Above: Returns records that are ancestors of the specified record (excluding the record itself).
            /// </summary>
            Above,

            /// <summary>
            /// AboveOrEqual (eq-or-above): Returns records that are ancestors of the specified record OR the record itself.
            /// </summary>
            AboveOrEqual,

            /// <summary>
            /// Under: Returns records that are descendants of the specified record (excluding the record itself).
            /// </summary>
            Under,

            /// <summary>
            /// UnderOrEqual (eq-or-under): Returns records that are descendants of the specified record OR the record itself.
            /// </summary>
            UnderOrEqual,

            /// <summary>
            /// NotUnder: Returns records that are NOT descendants of the specified record.
            /// </summary>
            NotUnder
        }

        /// <summary>
        /// Translates hierarchy operators (Above, AboveOrEqual, Under, UnderOrEqual, NotUnder) to LINQ expressions.
        /// These operators require a hierarchical relationship to be defined on the entity.
        ///
        /// Hierarchy operators work on entities with self-referential parent-child relationships:
        /// - Above: All ancestors in the hierarchy (parent, grandparent, etc.)
        /// - AboveOrEqual: The record itself plus all ancestors
        /// - Under: All descendants in the hierarchy (children, grandchildren, etc.)
        /// - UnderOrEqual: The record itself plus all descendants
        /// - NotUnder: All records that are NOT descendants (inverse of Under)
        /// </summary>
        /// <param name="context">The XrmFakedContext containing hierarchy relationship definitions.</param>
        /// <param name="qe">The query expression being translated.</param>
        /// <param name="c">The typed condition expression containing the operator and value.</param>
        /// <param name="entity">The entity parameter expression.</param>
        /// <param name="operatorType">The type of hierarchy operation to perform.</param>
        /// <returns>An expression that evaluates whether the entity matches the hierarchy condition.</returns>
        protected static Expression TranslateConditionExpressionHierarchy(
            XrmFakedContext context,
            QueryExpression qe,
            TypedConditionExpression c,
            ParameterExpression entity,
            HierarchyOperatorType operatorType)
        {
            // Get the entity name for the hierarchy lookup
            string entityName = qe.EntityName;

            // Check if we're querying from a linked entity
            if (!string.IsNullOrWhiteSpace(c.CondExpression.EntityName) && c.CondExpression.EntityName != qe.EntityName)
            {
                entityName = qe.GetEntityNameFromAlias(c.CondExpression.EntityName);
            }

            // Validate that a hierarchical relationship is defined for this entity
            if (!context.HierarchicalRelationships.ContainsKey(entityName))
            {
                throw new Exception($"Hierarchy operator '{operatorType}' requires a hierarchical relationship to be defined for entity '{entityName}'. " +
                    $"Use context.HierarchicalRelationships[\"{entityName}\"] = \"parentattributename\" to define the parent attribute.");
            }

            // Get the condition value (the GUID of the reference record)
            if (c.CondExpression.Values == null || c.CondExpression.Values.Count != 1)
            {
                throw new Exception($"Hierarchy operator '{operatorType}' requires exactly one value.");
            }

            var conditionValue = c.CondExpression.Values[0];
            Guid referenceRecordId;

            if (conditionValue is Guid guid)
            {
                referenceRecordId = guid;
            }
            else if (conditionValue is EntityReference entityRef)
            {
                referenceRecordId = entityRef.Id;
            }
            else if (conditionValue is string strValue && Guid.TryParse(strValue, out Guid parsedGuid))
            {
                referenceRecordId = parsedGuid;
            }
            else
            {
                throw new Exception($"Hierarchy operator '{operatorType}' value must be a GUID, EntityReference, or parseable GUID string.");
            }

            // Build the set of matching IDs based on the operator type
            var matchingIds = GetHierarchyMatchingIds(context, entityName, referenceRecordId, operatorType);

            // Build expression to check if the current entity's ID is in the matching set
            // entity.Id is the primary key of the record
            var entityIdProperty = Expression.Property(entity, "Id");

            // Use a runtime method call to check if the entity ID is in the matching set
            var matchMethod = typeof(XrmFakedContext).GetMethod("IsIdInHierarchySet", BindingFlags.Public | BindingFlags.Static);
            var matchExpression = Expression.Call(
                matchMethod,
                entityIdProperty,
                Expression.Constant(matchingIds)
            );

            return matchExpression;
        }

        /// <summary>
        /// Checks if a GUID is contained in a HashSet of GUIDs.
        /// This method is called at runtime via Expression.Call for hierarchy operator evaluation.
        /// </summary>
        /// <param name="id">The GUID to check.</param>
        /// <param name="matchingIds">The set of GUIDs that match the hierarchy condition.</param>
        /// <returns>True if the ID is in the set; false otherwise.</returns>
        public static bool IsIdInHierarchySet(Guid id, HashSet<Guid> matchingIds)
        {
            return matchingIds.Contains(id);
        }

        /// <summary>
        /// Builds a set of entity IDs that match the specified hierarchy operator.
        /// </summary>
        /// <param name="context">The XrmFakedContext containing entity data and hierarchy definitions.</param>
        /// <param name="entityName">The logical name of the entity.</param>
        /// <param name="referenceRecordId">The ID of the reference record for the hierarchy operation.</param>
        /// <param name="operatorType">The type of hierarchy operation.</param>
        /// <returns>A HashSet of GUIDs that match the hierarchy condition.</returns>
        protected static HashSet<Guid> GetHierarchyMatchingIds(
            XrmFakedContext context,
            string entityName,
            Guid referenceRecordId,
            HierarchyOperatorType operatorType)
        {
            var matchingIds = new HashSet<Guid>();
            var parentAttributeName = context.HierarchicalRelationships[entityName];

            // Get all entities of this type from the context
            if (!context.Data.ContainsKey(entityName))
            {
                return matchingIds; // No data, return empty set
            }

            var allEntities = context.Data[entityName];

            switch (operatorType)
            {
                case HierarchyOperatorType.Above:
                    // Get all ancestors (NOT including the record itself)
                    GetAncestors(allEntities, referenceRecordId, parentAttributeName, matchingIds);
                    break;

                case HierarchyOperatorType.AboveOrEqual:
                    // Get the record itself plus all ancestors
                    if (allEntities.ContainsKey(referenceRecordId))
                    {
                        matchingIds.Add(referenceRecordId);
                    }
                    GetAncestors(allEntities, referenceRecordId, parentAttributeName, matchingIds);
                    break;

                case HierarchyOperatorType.Under:
                    // Get all descendants (NOT including the record itself)
                    GetDescendants(allEntities, referenceRecordId, parentAttributeName, matchingIds);
                    break;

                case HierarchyOperatorType.UnderOrEqual:
                    // Get the record itself plus all descendants
                    if (allEntities.ContainsKey(referenceRecordId))
                    {
                        matchingIds.Add(referenceRecordId);
                    }
                    GetDescendants(allEntities, referenceRecordId, parentAttributeName, matchingIds);
                    break;

                case HierarchyOperatorType.NotUnder:
                    // Get all records that are NOT descendants
                    // First, get all descendants
                    var descendants = new HashSet<Guid>();
                    GetDescendants(allEntities, referenceRecordId, parentAttributeName, descendants);
                    // Then, add all entities NOT in the descendants set
                    foreach (var kvp in allEntities)
                    {
                        if (!descendants.Contains(kvp.Key))
                        {
                            matchingIds.Add(kvp.Key);
                        }
                    }
                    break;
            }

            return matchingIds;
        }

        /// <summary>
        /// Recursively finds all ancestor records (parent, grandparent, etc.) of a given record.
        /// </summary>
        /// <param name="allEntities">Dictionary of all entities of this type.</param>
        /// <param name="recordId">The ID of the record to find ancestors for.</param>
        /// <param name="parentAttributeName">The name of the attribute that stores the parent reference.</param>
        /// <param name="ancestors">The set to populate with ancestor IDs.</param>
        protected static void GetAncestors(
            ConcurrentDictionary<Guid, Entity> allEntities,
            Guid recordId,
            string parentAttributeName,
            HashSet<Guid> ancestors)
        {
            // Find the record
            if (!allEntities.TryGetValue(recordId, out Entity record))
            {
                return; // Record not found
            }

            // Get the parent reference
            if (!record.Contains(parentAttributeName))
            {
                return; // No parent attribute set
            }

            var parentValue = record[parentAttributeName];
            Guid? parentId = null;

            if (parentValue is EntityReference parentRef)
            {
                parentId = parentRef.Id;
            }
            else if (parentValue is Guid parentGuid)
            {
                parentId = parentGuid;
            }

            if (parentId == null || parentId.Value == Guid.Empty)
            {
                return; // No parent or empty parent
            }

            // Add the parent to ancestors (if not already added - prevents infinite loops)
            if (ancestors.Add(parentId.Value))
            {
                // Recursively get grandparents, etc.
                GetAncestors(allEntities, parentId.Value, parentAttributeName, ancestors);
            }
        }

        /// <summary>
        /// Finds all descendant records (children, grandchildren, etc.) of a given record.
        /// </summary>
        /// <param name="allEntities">Dictionary of all entities of this type.</param>
        /// <param name="recordId">The ID of the record to find descendants for.</param>
        /// <param name="parentAttributeName">The name of the attribute that stores the parent reference.</param>
        /// <param name="descendants">The set to populate with descendant IDs.</param>
        protected static void GetDescendants(
            ConcurrentDictionary<Guid, Entity> allEntities,
            Guid recordId,
            string parentAttributeName,
            HashSet<Guid> descendants)
        {
            // Find all records where parentAttributeName points to recordId
            foreach (var kvp in allEntities)
            {
                var entity = kvp.Value;
                if (!entity.Contains(parentAttributeName))
                {
                    continue; // No parent attribute set
                }

                var parentValue = entity[parentAttributeName];
                Guid? parentId = null;

                if (parentValue is EntityReference parentRef)
                {
                    parentId = parentRef.Id;
                }
                else if (parentValue is Guid parentGuid)
                {
                    parentId = parentGuid;
                }

                if (parentId.HasValue && parentId.Value == recordId)
                {
                    // This entity is a child of recordId
                    if (descendants.Add(entity.Id))
                    {
                        // Recursively get grandchildren, etc.
                        GetDescendants(allEntities, entity.Id, parentAttributeName, descendants);
                    }
                }
            }
        }
    }
}
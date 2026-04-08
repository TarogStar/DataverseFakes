using FakeXrmEasy.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FakeXrmEasy.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="Entity"/> objects to facilitate cloning,
    /// attribute manipulation, projection, and join operations within the FakeXrmEasy framework.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Adds an attribute to the entity and returns the entity instance for fluent chaining.
        /// </summary>
        /// <param name="e">The entity to which the attribute will be added.</param>
        /// <param name="key">The logical name of the attribute.</param>
        /// <param name="value">The value to assign to the attribute.</param>
        /// <returns>The same entity instance, allowing method chaining.</returns>
        public static Entity AddAttribute(this Entity e, string key, object value)
        {
            e.Attributes.Add(key, value);
            return e;
        }

        /// <summary>
        /// Projects the attributes of an entity so that only the attributes specified in the column set are returned.
        /// </summary>
        /// <param name="e">The source entity containing the attributes to project.</param>
        /// <param name="columnSet">The column set specifying which attributes to include in the projection.</param>
        /// <param name="context">The fake context used to resolve metadata and proxy types.</param>
        /// <returns>A new entity containing only the projected attributes.</returns>
        public static Entity ProjectAttributes(this Entity e, ColumnSet columnSet, XrmFakedContext context)
        {
            return ProjectAttributes(e, new QueryExpression() { ColumnSet = columnSet }, context, stripImageColumns: false);
        }

        /// <summary>
        /// Applies date behavior rules to DateTime attributes on the entity based on the context's configured date behaviors.
        /// For DateOnly behavior, the time portion is stripped and the value is normalized to midnight UTC.
        /// </summary>
        /// <param name="e">The entity whose DateTime attributes should be adjusted.</param>
        /// <param name="context">The fake context containing the date behavior configuration.</param>
        public static void ApplyDateBehaviour(this Entity e, XrmFakedContext context)
        {
#if FAKE_XRM_EASY || FAKE_XRM_EASY_2013
            return; //Do nothing... DateBehavior wasn't available for versions <= 2013
#else

            if (context.DateBehaviour.Count == 0 || e.LogicalName == null || !context.DateBehaviour.ContainsKey(e.LogicalName))
            {
                return;
            }

            var entityDateBehaviours = context.DateBehaviour[e.LogicalName];
            foreach (var attribute in entityDateBehaviours.Keys)
            {
                if (!e.Attributes.ContainsKey(attribute))
                {
                    continue;
                }

                switch (entityDateBehaviours[attribute])
                {
                    case DateTimeAttributeBehavior.DateOnly:
                        var currentValue = (DateTime)e[attribute];
                        // Use DateTimeKind.Unspecified to match Dataverse behavior for DateOnly fields
                        e[attribute] = new DateTime(currentValue.Year, currentValue.Month, currentValue.Day, 0, 0, 0, DateTimeKind.Unspecified);
                        break;

                    case DateTimeAttributeBehavior.TimeZoneIndependent:
                        var tziValue = (DateTime)e[attribute];
                        // TimeZoneIndependent fields return Unspecified Kind
                        e[attribute] = DateTime.SpecifyKind(tziValue, DateTimeKind.Unspecified);
                        break;

                    default:
                        break;
                }
            }
#endif
        }

        /// <summary>
        /// Projects attributes from a source entity into a target entity based on a link entity definition.
        /// Handles both all-columns and specific column projections, including nested link entities.
        /// </summary>
        /// <param name="e">The source entity containing the attributes to project.</param>
        /// <param name="projected">The target entity where projected attributes will be stored.</param>
        /// <param name="le">The link entity definition specifying which columns to project and the alias to use.</param>
        /// <param name="context">The fake context used for metadata resolution.</param>
        public static void ProjectAttributes(Entity e, Entity projected, LinkEntity le, XrmFakedContext context)
        {
            var sAlias = string.IsNullOrWhiteSpace(le.EntityAlias) ? le.LinkToEntityName : le.EntityAlias;

            if (le.Columns.AllColumns)
            {
                foreach (var attKey in e.Attributes.Keys)
                {
                    if (attKey.StartsWith(sAlias + "."))
                    {
                        projected[attKey] = e[attKey];
                    }
                }

                foreach (var attKey in e.FormattedValues.Keys)
                {
                    if (attKey.StartsWith(sAlias + "."))
                    {
                        projected.FormattedValues[attKey] = e.FormattedValues[attKey];
                    }
                }
            }
            else
            {
                foreach (var attKey in le.Columns.Columns)
                {
                    var linkedAttKey = sAlias + "." + attKey;
                    if (e.Attributes.ContainsKey(linkedAttKey))
                        projected[linkedAttKey] = e[linkedAttKey];

                    if (e.FormattedValues.ContainsKey(linkedAttKey))
                        projected.FormattedValues[linkedAttKey] = e.FormattedValues[linkedAttKey];
                }

            }

            foreach (var nestedLinkedEntity in le.LinkEntities)
            {
                ProjectAttributes(e, projected, nestedLinkedEntity, context);
            }
        }

        /// <summary>
        /// Projects entity attributes based on a QueryExpression, including linked entity columns.
        /// Creates a new entity with only the requested columns, validating attribute existence against metadata.
        /// </summary>
        /// <param name="e">The source entity containing the attributes to project.</param>
        /// <param name="qe">The query expression defining which columns to include.</param>
        /// <param name="context">The fake context used for metadata validation and proxy type resolution.</param>
        /// <param name="stripImageColumns">Whether to strip image columns from the projected attributes.</param>
        /// <returns>A new entity containing only the projected attributes, with null attributes removed.</returns>
        public static Entity ProjectAttributes(this Entity e, QueryExpression qe, XrmFakedContext context, bool stripImageColumns = true)
        {
            if (qe.ColumnSet == null || qe.ColumnSet.AllColumns)
            {
                return RemoveNullAttributes(e); //return all the original attributes
            }
            else
            {
                //Return selected list of attributes in a projected entity
                Entity projected = null;

                //However, if we are using proxy types, we must create a instance of the appropiate class
                if (context.ProxyTypesAssembly != null)
                {
                    var subClassType = context.FindReflectedType(e.LogicalName);
                    if (subClassType != null)
                    {
                        var instance = Activator.CreateInstance(subClassType);
                        projected = (Entity)instance;
                        projected.Id = e.Id;
                    }
                    else
                        projected = new Entity(e.LogicalName) { Id = e.Id }; //fallback to generic type if type not found
                }
                else
                    projected = new Entity(e.LogicalName) { Id = e.Id };


                foreach (var attKey in qe.ColumnSet.Columns)
                {
                    //Check if attribute really exists in metadata
                    if (!context.AttributeExistsInMetadata(e.LogicalName, attKey))
                    {
                        // Dataverse special-case: calendar can return calendarrules even though it is not present
                        // as a normal attribute in early-bound metadata.
                        if (!(string.Equals(e.LogicalName, "calendar", StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(attKey, "calendarrules", StringComparison.OrdinalIgnoreCase)))
                        {
                            FakeOrganizationServiceFault.Throw(ErrorCodes.QueryBuilderNoAttribute, string.Format("The attribute {0} does not exist on this entity.", attKey));
                        }
                    }

                    if (e.Attributes.ContainsKey(attKey) && e.Attributes[attKey] != null)
                    {
                        // Dataverse silently omits image (byte[]) columns from RetrieveMultiple results.
                        // They are only returned via individual Retrieve calls.
                        if (stripImageColumns && e.Attributes[attKey] is byte[])
                            continue;

                        projected[attKey] = CloneAttribute(e[attKey], context);

                        string formattedValue = "";

                        if (e.FormattedValues.TryGetValue(attKey, out formattedValue))
                        {
                            projected.FormattedValues[attKey] = formattedValue;
                        }
                    }
                }


                //Plus attributes from joins
                foreach (var le in qe.LinkEntities)
                {
                    ProjectAttributes(RemoveNullAttributes(e), projected, le, context);
                }
                return RemoveNullAttributes(projected);
            }
        }

        /// <summary>
        /// Removes all attributes with null values from the entity, including aliased values that contain null.
        /// </summary>
        /// <param name="entity">The entity from which null attributes will be removed.</param>
        /// <returns>The same entity instance with null attributes removed.</returns>
        public static Entity RemoveNullAttributes(Entity entity)
        {
            IList<string> nullAttributes = entity.Attributes
                .Where(attribute => attribute.Value == null ||
                                  (attribute.Value is AliasedValue && (attribute.Value as AliasedValue).Value == null))
                .Select(attribute => attribute.Key).ToList();
            foreach (var nullAttribute in nullAttributes)
            {
                entity.Attributes.Remove(nullAttribute);
            }
            return entity;
        }

        /// <summary>
        /// Creates a deep clone of an attribute value, handling all CRM SDK types including
        /// EntityReference, OptionSetValue, Money, AliasedValue, EntityCollection, and primitive types.
        /// </summary>
        /// <param name="attributeValue">The attribute value to clone.</param>
        /// <param name="context">Optional fake context used to populate EntityReference.Name from metadata.</param>
        /// <returns>A deep copy of the attribute value, or the original value for value types.</returns>
        /// <exception cref="Exception">Thrown when the attribute type is not supported for cloning.</exception>
        public static object CloneAttribute(object attributeValue, XrmFakedContext context = null)
        {
            if (attributeValue == null)
                return null;

            var type = attributeValue.GetType();
            if (type == typeof(string))
                return new string((attributeValue as string).ToCharArray());
            else if (type == typeof(EntityReference)
#if FAKE_XRM_EASY
                            || type == typeof(Microsoft.Xrm.Client.CrmEntityReference)
#endif
                    )
            {
                var original = (attributeValue as EntityReference);
                var clone = new EntityReference(original.LogicalName, original.Id);

                // Preserve pre-existing Name if it's already set (resolves test failures)
                if (!string.IsNullOrEmpty(original.Name))
                {
                    clone.Name = original.Name;
                }
                // Only auto-populate from metadata if Name is not already set
                else if (context != null && !string.IsNullOrEmpty(original.LogicalName) && context.EntityMetadata.ContainsKey(original.LogicalName) && !string.IsNullOrEmpty(context.EntityMetadata[original.LogicalName].PrimaryNameAttribute))
                {
                    ConcurrentDictionary<Guid, Entity> entityDict;
                    Entity referencedEntity;
                    if (context.Data.TryGetValue(original.LogicalName, out entityDict) &&
                        entityDict.TryGetValue(original.Id, out referencedEntity))
                    {
                        clone.Name = referencedEntity.GetAttributeValue<string>(context.EntityMetadata[original.LogicalName].PrimaryNameAttribute);
                    }
                }

#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
                if (original.KeyAttributes != null)
                {
                    clone.KeyAttributes = new KeyAttributeCollection();
                    clone.KeyAttributes.AddRange(original.KeyAttributes.Select(kvp => new KeyValuePair<string, object>(CloneAttribute(kvp.Key) as string, kvp.Value)).ToArray());
                }
#endif
                return clone;
            }
            else if (type == typeof(BooleanManagedProperty))
            {
                var original = (attributeValue as BooleanManagedProperty);
                return new BooleanManagedProperty(original.Value);
            }
            else if (type == typeof(OptionSetValue))
            {
                var original = (attributeValue as OptionSetValue);
                return new OptionSetValue(original.Value);
            }
            else if (type == typeof(AliasedValue))
            {
                var original = (attributeValue as AliasedValue);
                return new AliasedValue(original.EntityLogicalName, original.AttributeLogicalName, CloneAttribute(original.Value));
            }
            else if (type == typeof(Money))
            {
                var original = (attributeValue as Money);
                return new Money(original.Value);
            }
            else if (attributeValue.GetType() == typeof(EntityCollection))
            {
                var collection = attributeValue as EntityCollection;
                return new EntityCollection(collection.Entities.Select(e => e.Clone(e.GetType())).ToList());
            }
            else if (attributeValue is IEnumerable<Entity>)
            {
                var enumerable = attributeValue as IEnumerable<Entity>;
                return enumerable.Select(e => e.Clone(e.GetType())).ToArray();
            }
#if !FAKE_XRM_EASY
            else if (type == typeof(byte[]))
            {
                var original = (attributeValue as byte[]);
                var copy = new byte[original.Length];
                original.CopyTo(copy, 0);
                return copy;
            }
#endif
#if FAKE_XRM_EASY_9
            else if (attributeValue is OptionSetValueCollection)
            {
                var original = (attributeValue as OptionSetValueCollection);
                var copy = new OptionSetValueCollection(original.ToArray());
                return copy;
            }
#endif
            else if (type == typeof(int) || type == typeof(Int64))
                return attributeValue; //Not a reference type
            else if (type == typeof(decimal))
                return attributeValue; //Not a reference type
            else if (type == typeof(double))
                return attributeValue; //Not a reference type
            else if (type == typeof(float))
                return attributeValue; //Not a reference type
            else if (type == typeof(byte))
                return attributeValue; //Not a reference type
            else if (type == typeof(float))
                return attributeValue; //Not a reference type
            else if (type == typeof(bool))
                return attributeValue; //Not a reference type
            else if (type == typeof(Guid))
                return attributeValue; //Not a reference type
            else if (type == typeof(DateTime))
                return attributeValue; //Not a reference type
            else if (attributeValue is Enum)
                return attributeValue; //Not a reference type

            throw new Exception(string.Format("Attribute type not supported when trying to clone attribute '{0}'", type.ToString()));
        }

        /// <summary>
        /// Creates a deep clone of an entity, including all attributes, formatted values, and key attributes.
        /// </summary>
        /// <param name="e">The entity to clone.</param>
        /// <param name="context">Optional fake context used for attribute cloning (e.g., populating EntityReference names).</param>
        /// <returns>A new entity instance with cloned attributes.</returns>
        public static Entity Clone(this Entity e, XrmFakedContext context = null)
        {
      var cloned = new Entity(e.LogicalName)
      {
        Id = e.Id,
        LogicalName = e.LogicalName
      };

      if (e.FormattedValues != null)
            {
                var formattedValues = new FormattedValueCollection();
                // Take a snapshot of keys to avoid collection modified exception
                var formattedValueKeys = e.FormattedValues.Keys.ToList();
                foreach (var key in formattedValueKeys)
                {
                    string value;
                    if (e.FormattedValues.TryGetValue(key, out value))
                    {
                        formattedValues.Add(key, value);
                    }
                }

                cloned.Inject("FormattedValues", formattedValues);
            }

            // Take a snapshot of attribute keys to avoid collection modified exception
            var attributeKeys = e.Attributes.Keys.ToList();
            foreach (var attKey in attributeKeys)
            {
                object attValue;
                if (e.Attributes.TryGetValue(attKey, out attValue))
                {
                    cloned[attKey] = attValue != null ? CloneAttribute(attValue, context) : null;
                }
            }
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
            // Take a snapshot of key attribute keys to avoid collection modified exception
            var keyAttributeKeys = e.KeyAttributes.Keys.ToList();
            foreach (var attKey in keyAttributeKeys)
            {
                object attValue;
                if (e.KeyAttributes.TryGetValue(attKey, out attValue))
                {
                    cloned.KeyAttributes[attKey] = attValue != null ? CloneAttribute(attValue) : null;
                }
            }
#endif
            return cloned;
        }

        /// <summary>
        /// Creates a deep clone of an entity as a specific early-bound entity type.
        /// </summary>
        /// <typeparam name="T">The early-bound entity type to create.</typeparam>
        /// <param name="e">The entity to clone.</param>
        /// <returns>A new instance of type T with cloned attributes.</returns>
        public static T Clone<T>(this Entity e) where T : Entity
        {
            return (T)e.Clone(typeof(T));
        }

        /// <summary>
        /// Creates a deep clone of an entity as a specified type, including all attributes,
        /// formatted values, and key attributes.
        /// </summary>
        /// <param name="e">The entity to clone.</param>
        /// <param name="t">The type to create the cloned entity as. If null, creates a generic Entity.</param>
        /// <param name="context">Optional fake context used for attribute cloning.</param>
        /// <returns>A new entity instance of the specified type with cloned attributes.</returns>
        public static Entity Clone(this Entity e, Type t, XrmFakedContext context = null)
        {
            if (t == null)
                return e.Clone(context);

            var cloned = Activator.CreateInstance(t) as Entity;
            cloned.Id = e.Id;
            cloned.LogicalName = e.LogicalName;

            if (e.FormattedValues != null)
            {
                var formattedValues = new FormattedValueCollection();
                // Take a snapshot of keys to avoid collection modified exception
                var formattedValueKeys = e.FormattedValues.Keys.ToList();
                foreach (var key in formattedValueKeys)
                {
                    string value;
                    if (e.FormattedValues.TryGetValue(key, out value))
                    {
                        formattedValues.Add(key, value);
                    }
                }

                cloned.Inject("FormattedValues", formattedValues);
            }

            // Take a snapshot of attribute keys to avoid collection modified exception
            var attributeKeys = e.Attributes.Keys.ToList();
            foreach (var attKey in attributeKeys)
            {
                object attValue;
                if (e.Attributes.TryGetValue(attKey, out attValue))
                {
                    cloned[attKey] = attValue != null ? CloneAttribute(attValue, context) : null;
                }
            }

#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
            // Take a snapshot of key attribute keys to avoid collection modified exception
            var keyAttributeKeys = e.KeyAttributes.Keys.ToList();
            foreach (var attKey in keyAttributeKeys)
            {
                object attValue;
                if (e.KeyAttributes.TryGetValue(attKey, out attValue))
                {
                    cloned.KeyAttributes[attKey] = attValue != null ? CloneAttribute(attValue) : null;
                }
            }
#endif
            return cloned;
        }

        /// <summary>
        /// Joins attributes from another entity into this entity using aliased attribute names.
        /// Attributes from the other entity are prefixed with the specified alias.
        /// </summary>
        /// <param name="e">The primary entity to which attributes will be joined.</param>
        /// <param name="otherEntity">The related entity containing attributes to join. May be null for left outer joins.</param>
        /// <param name="columnSet">The column set specifying which attributes to join from the other entity.</param>
        /// <param name="alias">The alias prefix to use for joined attribute names (e.g., "contact" results in "contact.name").</param>
        /// <param name="context">The fake context used for metadata validation.</param>
        /// <returns>The primary entity with joined attributes added as AliasedValue instances.</returns>
        public static Entity JoinAttributes(this Entity e, Entity otherEntity, ColumnSet columnSet, string alias, XrmFakedContext context)
        {
            if (otherEntity == null) return e; //Left Join where otherEntity was not matched

            otherEntity = otherEntity.Clone(); //To avoid joining entities from/to the same entities, which would cause collection modified exceptions

            if (columnSet.AllColumns)
            {
                foreach (var attKey in otherEntity.Attributes.Keys)
                {
                    e[alias + "." + attKey] = new AliasedValue(otherEntity.LogicalName, attKey, otherEntity[attKey]);
                }

                foreach (var attKey in otherEntity.FormattedValues.Keys)
                {
                    e.FormattedValues[alias + "." + attKey] = otherEntity.FormattedValues[attKey];
                }
            }
            else
            {
                //Return selected list of attributes
                foreach (var attKey in columnSet.Columns)
                {
                    if (!context.AttributeExistsInMetadata(otherEntity.LogicalName, attKey))
                    {
                        FakeOrganizationServiceFault.Throw(ErrorCodes.QueryBuilderNoAttribute, string.Format("The attribute {0} does not exist on this entity.", attKey));
                    }

                    if (otherEntity.Attributes.ContainsKey(attKey))
                    {
                        e[alias + "." + attKey] = new AliasedValue(otherEntity.LogicalName, attKey, otherEntity[attKey]);
                    }
                    else
                    {
                        e[alias + "." + attKey] = new AliasedValue(otherEntity.LogicalName, attKey, null);
                    }

                    if (otherEntity.FormattedValues.ContainsKey(attKey))
                    {
                        e.FormattedValues[alias + "." + attKey] = otherEntity.FormattedValues[attKey];
                    }
                }
            }
            return e;
        }

        /// <summary>
        /// Joins attributes from multiple related entities into this entity using aliased attribute names.
        /// Each entity's attributes are prefixed with the specified alias.
        /// </summary>
        /// <param name="e">The primary entity to which attributes will be joined.</param>
        /// <param name="otherEntities">The collection of related entities containing attributes to join.</param>
        /// <param name="columnSet">The column set specifying which attributes to join from the other entities.</param>
        /// <param name="alias">The alias prefix to use for joined attribute names.</param>
        /// <param name="context">The fake context used for metadata validation.</param>
        /// <returns>The primary entity with joined attributes added as AliasedValue instances.</returns>
        public static Entity JoinAttributes(this Entity e, IEnumerable<Entity> otherEntities, ColumnSet columnSet, string alias, XrmFakedContext context)
        {
            foreach (var otherEntity in otherEntities)
            {
                var otherClonedEntity = otherEntity.Clone(); //To avoid joining entities from/to the same entities, which would cause collection modified exceptions

                if (columnSet.AllColumns)
                {
                    foreach (var attKey in otherClonedEntity.Attributes.Keys)
                    {
                        e[alias + "." + attKey] = new AliasedValue(otherEntity.LogicalName, attKey, otherClonedEntity[attKey]);
                    }

                    foreach (var attKey in otherEntity.FormattedValues.Keys)
                    {
                        e.FormattedValues[alias + "." + attKey] = otherEntity.FormattedValues[attKey];
                    }
                }
                else
                {
                    //Return selected list of attributes
                    foreach (var attKey in columnSet.Columns)
                    {
                        if (!context.AttributeExistsInMetadata(otherEntity.LogicalName, attKey))
                        {
                            FakeOrganizationServiceFault.Throw(ErrorCodes.QueryBuilderNoAttribute, string.Format("The attribute {0} does not exist on this entity.", attKey));
                        }

                        if (otherClonedEntity.Attributes.ContainsKey(attKey))
                        {
                            e[alias + "." + attKey] = new AliasedValue(otherEntity.LogicalName, attKey, otherClonedEntity[attKey]);
                        }
                        else
                        {
                            e[alias + "." + attKey] = new AliasedValue(otherEntity.LogicalName, attKey, null);
                        }

                        if (otherEntity.FormattedValues.ContainsKey(attKey))
                        {
                            e.FormattedValues[alias + "." + attKey] = otherEntity.FormattedValues[attKey];
                        }
                    }
                }
            }
            return e;
        }

        /// <summary>
        /// Returns the key value for an attribute, resolving EntityReference, OptionSetValue, and Money types
        /// to their underlying values. Used for join and grouping operations.
        /// </summary>
        /// <param name="e">The entity containing the attribute.</param>
        /// <param name="sAttributeName">The logical name of the attribute, optionally prefixed with an alias (e.g., "alias.attributename").</param>
        /// <param name="context">The fake context (currently unused but reserved for future metadata operations).</param>
        /// <returns>
        /// The underlying key value: Guid for EntityReference, int for OptionSetValue, decimal for Money,
        /// the entity's Id for primary key attributes, or Guid.Empty if the attribute doesn't exist.
        /// </returns>
        public static object KeySelector(this Entity e, string sAttributeName, XrmFakedContext context)
        {
            if (sAttributeName.Contains("."))
            {
                //Do not lowercase the alias prefix
                var splitted = sAttributeName.Split('.');
                sAttributeName = string.Format("{0}.{1}", splitted[0], splitted[1].ToLower());
            }
            else
            {
                sAttributeName = sAttributeName.ToLower();
            }

            if (!e.Attributes.ContainsKey(sAttributeName))
            {
                //Check if it is the primary key
                if (sAttributeName.Contains("id") &&
                   e.LogicalName.ToLower().Equals(sAttributeName.Substring(0, sAttributeName.Length - 2)))
                {
                    return e.Id;
                }
                return Guid.Empty; //Atrribute is null or doesn´t exists so it can´t be joined
            }

            object keyValue = null;
            AliasedValue aliasedValue;
            if ((aliasedValue = e[sAttributeName] as AliasedValue) != null)
            {
                keyValue = aliasedValue.Value;
            }
            else
            {
                keyValue = e[sAttributeName];
            }

            EntityReference entityReference = keyValue as EntityReference;
            if (entityReference != null)
                return entityReference.Id;

            OptionSetValue optionSetValue = keyValue as OptionSetValue;
            if (optionSetValue != null)
                return optionSetValue.Value;

            Money money = keyValue as Money;
            if (money != null)
                return money.Value;

            return keyValue;
        }

        /// <summary>
        /// Sets a property value on an entity using reflection, bypassing normal accessibility restrictions.
        /// Used to set read-only properties like FormattedValues on SDK classes.
        /// </summary>
        /// <param name="e">The entity on which to set the property.</param>
        /// <param name="property">The name of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        public static void Inject(this Entity e, string property, object value)
        {
            e.GetType().GetProperty(property).SetValue(e, value, null);
        }

        /// <summary>
        /// Sets an attribute value on the entity only if the attribute does not exist or has a null value.
        /// Useful for setting default values without overwriting existing data.
        /// </summary>
        /// <param name="e">The entity on which to conditionally set the attribute.</param>
        /// <param name="property">The logical name of the attribute to set.</param>
        /// <param name="value">The value to assign if the attribute is empty or null.</param>
        public static void SetValueIfEmpty(this Entity e, string property, object value)
        {
            var containsKey = e.Attributes.ContainsKey(property);
            if (!containsKey || containsKey && e[property] == null)
            {
                e[property] = value;
            }
        }

        /// <summary>
        /// Converts an entity to an EntityReference, preserving key attribute information for alternate key lookups.
        /// </summary>
        /// <param name="e">The entity to convert to an EntityReference.</param>
        /// <returns>An EntityReference containing the entity's logical name, Id, and key attributes.</returns>
        public static EntityReference ToEntityReferenceWithKeyAttributes(this Entity e)
        {
            var result = e.ToEntityReference();
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
            result.KeyAttributes = e.KeyAttributes;
#endif
            return result;
        }


    }
}
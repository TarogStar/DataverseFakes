using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FakeXrmEasy.Metadata
{
    /// <summary>
    /// Provides static methods for generating Dynamics 365 metadata from early-bound entity types.
    /// This class can be used to programmatically create EntityMetadata and AttributeMetadata
    /// from assemblies containing early-bound entity classes for use in test setups.
    /// </summary>
    /// <remarks>
    /// This class is exposed publicly to allow users to generate metadata for their own test setups
    /// without needing to go through XrmFakedContext. Typical use cases include:
    /// - Generating metadata for a subset of entities
    /// - Creating metadata for custom entity types
    /// - Building metadata programmatically for specific test scenarios
    /// </remarks>
    public class MetadataGenerator
    {
        /// <summary>
        /// Generates EntityMetadata for all early-bound entity types found in the specified assembly.
        /// Reflects over the assembly to find classes decorated with EntityLogicalNameAttribute
        /// and creates corresponding metadata including attributes and relationships.
        /// </summary>
        /// <param name="earlyBoundEntitiesAssembly">The assembly containing early-bound entity classes.</param>
        /// <returns>A collection of EntityMetadata for all entity types found in the assembly.</returns>
        /// <example>
        /// <code>
        /// // Generate metadata from an assembly containing early-bound types
        /// var metadata = MetadataGenerator.FromEarlyBoundEntities(typeof(Account).Assembly);
        ///
        /// // Use with XrmFakedContext
        /// var context = new XrmFakedContext();
        /// context.InitializeMetadata(metadata);
        /// </code>
        /// </example>
        public static IEnumerable<EntityMetadata> FromEarlyBoundEntities(Assembly earlyBoundEntitiesAssembly)
        {
            List<EntityMetadata> entityMetadatas = new List<EntityMetadata>();
            foreach (var earlyBoundEntity in earlyBoundEntitiesAssembly.GetTypes())
            {
                EntityLogicalNameAttribute entityLogicalNameAttribute = GetCustomAttribute<EntityLogicalNameAttribute>(earlyBoundEntity);
                if (entityLogicalNameAttribute == null) continue;
        EntityMetadata metadata = new EntityMetadata
        {
          LogicalName = entityLogicalNameAttribute.LogicalName
        };

        FieldInfo entityTypeCode = earlyBoundEntity.GetField("EntityTypeCode", BindingFlags.Static | BindingFlags.Public);
                if (entityTypeCode != null)
                {
                    metadata.SetFieldValue("ObjectTypeCode", entityTypeCode.GetValue(null));
                }

                List<AttributeMetadata> attributeMetadatas = new List<AttributeMetadata>();
                List<ManyToManyRelationshipMetadata> manyToManyRelationshipMetadatas = new List<ManyToManyRelationshipMetadata>();
                List<OneToManyRelationshipMetadata> oneToManyRelationshipMetadatas = new List<OneToManyRelationshipMetadata>();
                List<OneToManyRelationshipMetadata> manyToOneRelationshipMetadatas = new List<OneToManyRelationshipMetadata>();

                var idProperty = earlyBoundEntity.GetProperty("Id");
                AttributeLogicalNameAttribute attributeLogicalNameAttribute;
                if (idProperty != null && (attributeLogicalNameAttribute = GetCustomAttribute<AttributeLogicalNameAttribute>(idProperty)) != null)
                {
                    metadata.SetFieldValue("PrimaryIdAttribute", attributeLogicalNameAttribute.LogicalName);
                }

                var properties = earlyBoundEntity.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                    .Where(x => x.Name != "Id" && Attribute.IsDefined(x, typeof(AttributeLogicalNameAttribute))
                                             || Attribute.IsDefined(x, typeof(RelationshipSchemaNameAttribute)));

                foreach (var property in properties)
                {
                    RelationshipSchemaNameAttribute relationshipSchemaNameAttribute = GetCustomAttribute<RelationshipSchemaNameAttribute>(property);
                    attributeLogicalNameAttribute = GetCustomAttribute<AttributeLogicalNameAttribute>(property);

                    if (relationshipSchemaNameAttribute == null)
                    {
#if !FAKE_XRM_EASY
                        if (property.PropertyType == typeof(byte[]))
                        {
                            metadata.SetFieldValue("PrimaryImageAttribute", attributeLogicalNameAttribute.LogicalName);
                        }
#endif
                        AttributeMetadata attributeMetadata;
                        if (attributeLogicalNameAttribute.LogicalName == "statecode")
                        {
                            attributeMetadata = new StateAttributeMetadata();
                        }
                        else if (attributeLogicalNameAttribute.LogicalName == "statuscode")
                        {
                            attributeMetadata = new StatusAttributeMetadata();
                        }
                        else if (attributeLogicalNameAttribute.LogicalName == metadata.PrimaryIdAttribute)
                        {
                            attributeMetadata = new AttributeMetadata();
                            attributeMetadata.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Uniqueidentifier);
                        }
                        else
                        {
                            attributeMetadata = CreateAttributeMetadataByType(property.PropertyType);
                        }

                        attributeMetadata.SetFieldValue("EntityLogicalName", entityLogicalNameAttribute.LogicalName);
                        attributeMetadata.SetFieldValue("LogicalName", attributeLogicalNameAttribute.LogicalName);

                        attributeMetadatas.Add(attributeMetadata);
                    }
                    else
                    {
                        if (property.PropertyType.Name == "IEnumerable`1")
                        {
                            PropertyInfo peerProperty = property.PropertyType.GetGenericArguments()[0].GetProperties().SingleOrDefault(x => x.PropertyType == earlyBoundEntity && GetCustomAttribute<RelationshipSchemaNameAttribute>(x)?.SchemaName == relationshipSchemaNameAttribute.SchemaName);
                            if (peerProperty == null || peerProperty.PropertyType.Name == "IEnumerable`1") // N:N relationship
                            {
                ManyToManyRelationshipMetadata relationshipMetadata = new ManyToManyRelationshipMetadata
                {
                  SchemaName = relationshipSchemaNameAttribute.SchemaName
                };

                manyToManyRelationshipMetadatas.Add(relationshipMetadata);
                            }
                            else // 1:N relationship
                            {
                                AddOneToManyRelationshipMetadata(earlyBoundEntity, property, property.PropertyType.GetGenericArguments()[0], peerProperty, oneToManyRelationshipMetadatas);
                            }
                        }
                        else //N:1 Property
                        {
                            AddOneToManyRelationshipMetadata(property.PropertyType, property.PropertyType.GetProperties().SingleOrDefault(x => x.PropertyType.GetGenericArguments().SingleOrDefault() == earlyBoundEntity && GetCustomAttribute<RelationshipSchemaNameAttribute>(x)?.SchemaName == relationshipSchemaNameAttribute.SchemaName), earlyBoundEntity, property, manyToOneRelationshipMetadatas);
                        }
                    }
                }
                if (attributeMetadatas.Any())
                {
                    metadata.SetSealedPropertyValue("Attributes", attributeMetadatas.ToArray());
                }
                if (manyToManyRelationshipMetadatas.Any())
                {
                    metadata.SetSealedPropertyValue("ManyToManyRelationships", manyToManyRelationshipMetadatas.ToArray());
                }
                if (manyToOneRelationshipMetadatas.Any())
                {
                    metadata.SetSealedPropertyValue("ManyToOneRelationships", manyToOneRelationshipMetadatas.ToArray());
                }
                if (oneToManyRelationshipMetadatas.Any())
                {
                    metadata.SetSealedPropertyValue("OneToManyRelationships", oneToManyRelationshipMetadatas.ToArray());
                }
                entityMetadatas.Add(metadata);
            }
            return entityMetadatas;
        }

        /// <summary>
        /// Generates EntityMetadata for a single early-bound entity type.
        /// This is useful when you need metadata for just one specific entity.
        /// </summary>
        /// <typeparam name="T">The early-bound entity type (must be decorated with EntityLogicalNameAttribute).</typeparam>
        /// <returns>The EntityMetadata for the specified type, or null if the type is not an early-bound entity.</returns>
        /// <example>
        /// <code>
        /// // Generate metadata for a single entity type
        /// var accountMetadata = MetadataGenerator.FromEarlyBoundEntity&lt;Account&gt;();
        ///
        /// // Use with XrmFakedContext
        /// var context = new XrmFakedContext();
        /// context.InitializeMetadata(accountMetadata);
        /// </code>
        /// </example>
        public static EntityMetadata FromEarlyBoundEntity<T>() where T : Entity
        {
            return FromEarlyBoundEntity(typeof(T));
        }

        /// <summary>
        /// Generates EntityMetadata for a single early-bound entity type.
        /// This is useful when you need metadata for just one specific entity.
        /// </summary>
        /// <param name="earlyBoundType">The early-bound entity type (must be decorated with EntityLogicalNameAttribute).</param>
        /// <returns>The EntityMetadata for the specified type, or null if the type is not an early-bound entity.</returns>
        /// <example>
        /// <code>
        /// // Generate metadata for a single entity type
        /// var accountMetadata = MetadataGenerator.FromEarlyBoundEntity(typeof(Account));
        ///
        /// // Use with XrmFakedContext
        /// var context = new XrmFakedContext();
        /// context.InitializeMetadata(accountMetadata);
        /// </code>
        /// </example>
        public static EntityMetadata FromEarlyBoundEntity(Type earlyBoundType)
        {
            EntityLogicalNameAttribute entityLogicalNameAttribute = GetCustomAttribute<EntityLogicalNameAttribute>(earlyBoundType);
            if (entityLogicalNameAttribute == null) return null;

      EntityMetadata metadata = new EntityMetadata
      {
        LogicalName = entityLogicalNameAttribute.LogicalName
      };

      FieldInfo entityTypeCode = earlyBoundType.GetField("EntityTypeCode", BindingFlags.Static | BindingFlags.Public);
            if (entityTypeCode != null)
            {
                metadata.SetFieldValue("ObjectTypeCode", entityTypeCode.GetValue(null));
            }

            List<AttributeMetadata> attributeMetadatas = new List<AttributeMetadata>();
            List<ManyToManyRelationshipMetadata> manyToManyRelationshipMetadatas = new List<ManyToManyRelationshipMetadata>();
            List<OneToManyRelationshipMetadata> oneToManyRelationshipMetadatas = new List<OneToManyRelationshipMetadata>();
            List<OneToManyRelationshipMetadata> manyToOneRelationshipMetadatas = new List<OneToManyRelationshipMetadata>();

            var idProperty = earlyBoundType.GetProperty("Id");
            AttributeLogicalNameAttribute attributeLogicalNameAttribute;
            if (idProperty != null && (attributeLogicalNameAttribute = GetCustomAttribute<AttributeLogicalNameAttribute>(idProperty)) != null)
            {
                metadata.SetFieldValue("PrimaryIdAttribute", attributeLogicalNameAttribute.LogicalName);
            }

            var properties = earlyBoundType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Where(x => x.Name != "Id" && Attribute.IsDefined(x, typeof(AttributeLogicalNameAttribute))
                                         || Attribute.IsDefined(x, typeof(RelationshipSchemaNameAttribute)));

            foreach (var property in properties)
            {
                RelationshipSchemaNameAttribute relationshipSchemaNameAttribute = GetCustomAttribute<RelationshipSchemaNameAttribute>(property);
                attributeLogicalNameAttribute = GetCustomAttribute<AttributeLogicalNameAttribute>(property);

                if (relationshipSchemaNameAttribute == null)
                {
#if !FAKE_XRM_EASY
                    if (property.PropertyType == typeof(byte[]))
                    {
                        metadata.SetFieldValue("PrimaryImageAttribute", attributeLogicalNameAttribute.LogicalName);
                    }
#endif
                    AttributeMetadata attributeMetadata;
                    if (attributeLogicalNameAttribute.LogicalName == "statecode")
                    {
                        attributeMetadata = new StateAttributeMetadata();
                    }
                    else if (attributeLogicalNameAttribute.LogicalName == "statuscode")
                    {
                        attributeMetadata = new StatusAttributeMetadata();
                    }
                    else if (attributeLogicalNameAttribute.LogicalName == metadata.PrimaryIdAttribute)
                    {
                        attributeMetadata = new AttributeMetadata();
                        attributeMetadata.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Uniqueidentifier);
                    }
                    else
                    {
                        attributeMetadata = CreateAttributeMetadataByType(property.PropertyType);
                    }

                    attributeMetadata.SetFieldValue("EntityLogicalName", entityLogicalNameAttribute.LogicalName);
                    attributeMetadata.SetFieldValue("LogicalName", attributeLogicalNameAttribute.LogicalName);

                    attributeMetadatas.Add(attributeMetadata);
                }
                else
                {
                    if (property.PropertyType.Name == "IEnumerable`1")
                    {
                        PropertyInfo peerProperty = property.PropertyType.GetGenericArguments()[0].GetProperties().SingleOrDefault(x => x.PropertyType == earlyBoundType && GetCustomAttribute<RelationshipSchemaNameAttribute>(x)?.SchemaName == relationshipSchemaNameAttribute.SchemaName);
                        if (peerProperty == null || peerProperty.PropertyType.Name == "IEnumerable`1") // N:N relationship
                        {
              ManyToManyRelationshipMetadata relationshipMetadata = new ManyToManyRelationshipMetadata
              {
                SchemaName = relationshipSchemaNameAttribute.SchemaName
              };

              manyToManyRelationshipMetadatas.Add(relationshipMetadata);
                        }
                        else // 1:N relationship
                        {
                            AddOneToManyRelationshipMetadata(earlyBoundType, property, property.PropertyType.GetGenericArguments()[0], peerProperty, oneToManyRelationshipMetadatas);
                        }
                    }
                    else //N:1 Property
                    {
                        AddOneToManyRelationshipMetadata(property.PropertyType, property.PropertyType.GetProperties().SingleOrDefault(x => x.PropertyType.GetGenericArguments().SingleOrDefault() == earlyBoundType && GetCustomAttribute<RelationshipSchemaNameAttribute>(x)?.SchemaName == relationshipSchemaNameAttribute.SchemaName), earlyBoundType, property, manyToOneRelationshipMetadatas);
                    }
                }
            }
            if (attributeMetadatas.Any())
            {
                metadata.SetSealedPropertyValue("Attributes", attributeMetadatas.ToArray());
            }
            if (manyToManyRelationshipMetadatas.Any())
            {
                metadata.SetSealedPropertyValue("ManyToManyRelationships", manyToManyRelationshipMetadatas.ToArray());
            }
            if (manyToOneRelationshipMetadatas.Any())
            {
                metadata.SetSealedPropertyValue("ManyToOneRelationships", manyToOneRelationshipMetadatas.ToArray());
            }
            if (oneToManyRelationshipMetadatas.Any())
            {
                metadata.SetSealedPropertyValue("OneToManyRelationships", oneToManyRelationshipMetadatas.ToArray());
            }

            return metadata;
        }

        /// <summary>
        /// Gets a custom attribute of the specified type from a member (property, field, type, etc.).
        /// This is a utility method for working with reflection on early-bound entity types.
        /// </summary>
        /// <typeparam name="T">The type of attribute to retrieve.</typeparam>
        /// <param name="member">The member to get the attribute from.</param>
        /// <returns>The attribute instance if found, otherwise null.</returns>
        public static T GetCustomAttribute<T>(MemberInfo member) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(member, typeof(T));
        }

        /// <summary>
        /// Creates an AttributeMetadata instance based on a CLR property type.
        /// Maps common .NET types to their corresponding Dynamics 365 attribute metadata types.
        /// </summary>
        /// <param name="propertyType">The CLR type of the property (e.g., typeof(string), typeof(int?), typeof(EntityReference)).</param>
        /// <returns>An AttributeMetadata instance appropriate for the given property type.</returns>
        /// <exception cref="Exception">Thrown when the property type cannot be mapped to a known AttributeMetadata type.</exception>
        /// <example>
        /// <code>
        /// // Get the appropriate metadata type for a string property
        /// var stringMetadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(string));
        /// // Returns StringAttributeMetadata
        ///
        /// // Get metadata for a nullable integer
        /// var intMetadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(int?));
        /// // Returns IntegerAttributeMetadata
        ///
        /// // Get metadata for an EntityReference (lookup)
        /// var lookupMetadata = MetadataGenerator.CreateAttributeMetadataByType(typeof(EntityReference));
        /// // Returns LookupAttributeMetadata
        /// </code>
        /// </example>
        /// <remarks>
        /// The following type mappings are supported:
        /// - string -> StringAttributeMetadata
        /// - EntityReference -> LookupAttributeMetadata
        /// - OptionSetValue -> PicklistAttributeMetadata
        /// - Money -> MoneyAttributeMetadata
        /// - int? -> IntegerAttributeMetadata
        /// - double? -> DoubleAttributeMetadata
        /// - bool? -> BooleanAttributeMetadata
        /// - decimal? -> DecimalAttributeMetadata
        /// - DateTime? -> DateTimeAttributeMetadata
        /// - Guid? -> LookupAttributeMetadata
        /// - long? -> BigIntAttributeMetadata
        /// - Nullable{Enum} -> StateAttributeMetadata
        /// - IEnumerable{T} -> LookupAttributeMetadata (PartyList)
        /// - BooleanManagedProperty -> BooleanAttributeMetadata (ManagedProperty)
        /// - byte[] -> ImageAttributeMetadata (v9.x only)
        /// - OptionSetValueCollection -> MultiSelectPicklistAttributeMetadata (v9.x only)
        /// </remarks>
        public static AttributeMetadata CreateAttributeMetadataByType(Type propertyType)
        {
            if (typeof(string) == propertyType)
            {
                return new StringAttributeMetadata();
            }
            else if (typeof(EntityReference).IsAssignableFrom(propertyType))
            {
                return new LookupAttributeMetadata();
            }
#if FAKE_XRM_EASY || FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365
            else if (typeof(Microsoft.Xrm.Client.CrmEntityReference).IsAssignableFrom(propertyType))
            {
                return new LookupAttributeMetadata();
            }
#endif
            else if (typeof(OptionSetValue).IsAssignableFrom(propertyType))
            {
                return new PicklistAttributeMetadata();
            }
            else if (typeof(Money).IsAssignableFrom(propertyType))
            {
                return new MoneyAttributeMetadata();
            }
            else if (propertyType.IsGenericType)
            {
                Type genericType = propertyType.GetGenericArguments().FirstOrDefault();
                if (propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (typeof(int) == genericType)
                    {
                        return new IntegerAttributeMetadata();
                    }
                    else if (typeof(double) == genericType)
                    {
                        return new DoubleAttributeMetadata();
                    }
                    else if (typeof(bool) == genericType)
                    {
                        return new BooleanAttributeMetadata();
                    }
                    else if (typeof(decimal) == genericType)
                    {
                        return new DecimalAttributeMetadata();
                    }
                    else if (typeof(DateTime) == genericType)
                    {
                        return new DateTimeAttributeMetadata();
                    }
                    else if (typeof(Guid) == genericType)
                    {
                        return new LookupAttributeMetadata();
                    }
                    else if (typeof(long) == genericType)
                    {
                        return new BigIntAttributeMetadata();
                    }
                    else if (typeof(Enum).IsAssignableFrom(genericType))
                    {
                        return new StateAttributeMetadata();
                    }
                    else
                    {
                        throw new Exception($"Type {propertyType.Name}{genericType.Name} has not been mapped to an AttributeMetadata.");
                    }
                }
                else if (propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var partyList = new LookupAttributeMetadata();
                    partyList.SetSealedPropertyValue("AttributeType", AttributeTypeCode.PartyList);
                    return partyList;
                }
                else
                {
                    throw new Exception($"Type {propertyType.Name}{genericType.Name} has not been mapped to an AttributeMetadata.");
                }
            }
            else if (typeof(BooleanManagedProperty) == propertyType)
            {
                var booleanManaged = new BooleanAttributeMetadata();
                booleanManaged.SetSealedPropertyValue("AttributeType", AttributeTypeCode.ManagedProperty);
                return booleanManaged;
            }
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013
            else if (typeof(Guid) == propertyType)
            {
                return new UniqueIdentifierAttributeMetadata();
            }
#endif
#if !FAKE_XRM_EASY
            else if (typeof(byte[]) == propertyType)
            {

                return new ImageAttributeMetadata();
            }
#endif
#if FAKE_XRM_EASY_9
            else if (typeof(OptionSetValueCollection).IsAssignableFrom(propertyType))
            {
                return new MultiSelectPicklistAttributeMetadata();
            }
#endif
            else
            {
                throw new Exception($"Type {propertyType.Name} has not been mapped to an AttributeMetadata.");
            }
        }

        private static void AddOneToManyRelationshipMetadata(Type referencingEntity, PropertyInfo referencingAttribute, Type referencedEntity, PropertyInfo referencedAttribute, List<OneToManyRelationshipMetadata> relationshipMetadatas)
        {
            if (referencingEntity == null || referencingAttribute == null || referencedEntity == null || referencedAttribute == null) return;
      OneToManyRelationshipMetadata relationshipMetadata = new OneToManyRelationshipMetadata
      {
        SchemaName = GetCustomAttribute<RelationshipSchemaNameAttribute>(referencingAttribute).SchemaName,
        ReferencingEntity = GetCustomAttribute<EntityLogicalNameAttribute>(referencingEntity).LogicalName,
        ReferencingAttribute = GetCustomAttribute<AttributeLogicalNameAttribute>(referencingAttribute)?.LogicalName,
        ReferencedEntity = GetCustomAttribute<EntityLogicalNameAttribute>(referencedEntity).LogicalName,
        ReferencedAttribute = GetCustomAttribute<AttributeLogicalNameAttribute>(referencedAttribute).LogicalName
      };

      relationshipMetadatas.Add(relationshipMetadata);
        }
    }
}

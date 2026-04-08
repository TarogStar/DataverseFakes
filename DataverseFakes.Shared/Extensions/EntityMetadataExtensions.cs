using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FakeXrmEasy.Extensions
{
    /// <summary>
    /// Extension methods for EntityMetadata
    /// </summary>
    public static class EntityMetadataExtensions
    {
        /// <summary>
        /// Sets the attribute collection for the entity metadata
        /// </summary>
        /// <param name="entityMetadata">The entity metadata</param>
        /// <param name="attributes">The attributes to set</param>
        public static void SetAttributeCollection(this EntityMetadata entityMetadata, AttributeMetadata[] attributes)
        {
            //AttributeMetadata is internal set in a sealed class so... just doing this

            entityMetadata.GetType().GetProperty("Attributes").SetValue(entityMetadata, attributes, null);
        }

        /// <summary>
        /// Sets or updates a single attribute for the entity metadata
        /// </summary>
        /// <param name="entityMetadata">The entity metadata</param>
        /// <param name="attribute">The attribute to set</param>
        public static void SetAttribute(this EntityMetadata entityMetadata, AttributeMetadata attribute)
        {
            var currentAttributes = entityMetadata.Attributes;
            if (currentAttributes == null)
            {
                currentAttributes = new AttributeMetadata[0];
            }
            var newAttributesList = currentAttributes.Where(a => a.LogicalName != attribute.LogicalName).ToList();
            newAttributesList.Add(attribute);
            var newAttributesArray = newAttributesList.ToArray();

            entityMetadata.GetType().GetProperty("Attributes").SetValue(entityMetadata, newAttributesArray, null);
        }

        /// <summary>
        /// Sets the attribute collection for the entity metadata from an enumerable
        /// </summary>
        /// <param name="entityMetadata">The entity metadata</param>
        /// <param name="attributes">The attributes to set</param>
        public static void SetAttributeCollection(this EntityMetadata entityMetadata, IEnumerable<AttributeMetadata> attributes)
        {
            entityMetadata.GetType().GetProperty("Attributes").SetValue(entityMetadata, attributes.ToList().ToArray(), null);
        }

        /// <summary>
        /// Sets a sealed property value on entity metadata using reflection
        /// </summary>
        /// <param name="entityMetadata">The entity metadata</param>
        /// <param name="sPropertyName">The property name</param>
        /// <param name="value">The value to set</param>
        public static void SetSealedPropertyValue(this EntityMetadata entityMetadata, string sPropertyName, object value)
        {
            entityMetadata.GetType().GetProperty(sPropertyName).SetValue(entityMetadata, value, null);
        }

        /// <summary>
        /// Sets a sealed property value on attribute metadata using reflection
        /// </summary>
        /// <param name="attributeMetadata">The attribute metadata</param>
        /// <param name="sPropertyName">The property name</param>
        /// <param name="value">The value to set</param>
        public static void SetSealedPropertyValue(this AttributeMetadata attributeMetadata, string sPropertyName, object value)
        {
            attributeMetadata.GetType().GetProperty(sPropertyName).SetValue(attributeMetadata, value, null);
        }

        /// <summary>
        /// Sets a sealed property value on many-to-many relationship metadata using reflection
        /// </summary>
        /// <param name="manyToManyRelationshipMetadata">The relationship metadata</param>
        /// <param name="sPropertyName">The property name</param>
        /// <param name="value">The value to set</param>
        public static void SetSealedPropertyValue(this ManyToManyRelationshipMetadata manyToManyRelationshipMetadata, string sPropertyName, object value)
        {
            manyToManyRelationshipMetadata.GetType().GetProperty(sPropertyName).SetValue(manyToManyRelationshipMetadata, value, null);
        }

        /// <summary>
        /// Sets a sealed property value on one-to-many relationship metadata using reflection
        /// </summary>
        /// <param name="oneToManyRelationshipMetadata">The relationship metadata</param>
        /// <param name="sPropertyName">The property name</param>
        /// <param name="value">The value to set</param>
        public static void SetSealedPropertyValue(this OneToManyRelationshipMetadata oneToManyRelationshipMetadata, string sPropertyName, object value)
        {
            oneToManyRelationshipMetadata.GetType().GetProperty(sPropertyName).SetValue(oneToManyRelationshipMetadata, value, null);
        }
    }
}

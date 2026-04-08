using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FakeXrmEasy
{
    /// <summary>
    /// A comparer for sorting CRM attribute values used in OrderBy operations.
    /// Supports comparison of various CRM types including OptionSetValue, EntityReference, Money, and primitive types.
    /// </summary>
    public class XrmOrderByAttributeComparer : IComparer<object>
    {
        /// <summary>
        /// Compares two CRM attribute values and returns their relative sort order.
        /// Handles null values and supports multiple CRM types including OptionSetValue, EntityReference, Money, AliasedValue, and primitive types.
        /// </summary>
        /// <param name="objectA">The first object to compare.</param>
        /// <param name="objectB">The second object to compare.</param>
        /// <returns>A negative value if objectA is less than objectB, zero if they are equal, or a positive value if objectA is greater than objectB.</returns>
        public int Compare(Object objectA, Object objectB)
        {
            if (objectA == null && objectB == null) return 0;  //Equal

            if (objectA == null)
                return -1;
            if (objectB == null)
                return 1;

            Type attributeType = objectA.GetType();

            if (attributeType == typeof(OptionSetValue))
            {
                // we'll want the text value
                OptionSetValue attributeValueA = (OptionSetValue)(objectA);
                OptionSetValue attributeValueB = (OptionSetValue)(objectB);
                return attributeValueA.Value.CompareTo(attributeValueB.Value);
            }
            else if (attributeType == typeof(EntityReference)
#if FAKE_XRM_EASY
                    || attributeType == typeof(Microsoft.Xrm.Client.CrmEntityReference)
#endif
                )
            {
                // Name might well be Null in an entity reference?
                EntityReference entityRefA = (EntityReference)objectA;
                EntityReference entityRefB = (EntityReference)objectB;

                if (entityRefA.Name == null && entityRefB.Name == null) return 0;  //Equal

                if (entityRefA.Name == null)
                    return -1;
                if (entityRefB.Name == null)
                    return 1;

                return entityRefA.Name.CompareTo(entityRefB.Name);
            }
            else if (attributeType == typeof(Money))
            {
                Decimal valueA = ((Money)objectA).Value;
                Decimal valueB = ((Money)objectB).Value;
                var x = valueA.CompareTo(valueB);
                return x;
            }
            else if (attributeType == typeof(string))
            {
                return String.Compare(objectA.ToString(), objectB.ToString());
            }
            else if (attributeType == typeof(int))
            {
                return ((int)objectA).CompareTo(((int)objectB));
            }
            else if (attributeType == typeof(DateTime))
            {
                return ((DateTime)objectA).CompareTo((DateTime)objectB);
            }
            else if (attributeType == typeof(Guid))
            {
                return ((Guid)objectA).CompareTo((Guid)objectB);
            }
            else if (attributeType == typeof(decimal))
            {
                return ((decimal)objectA).CompareTo((decimal)objectB);
            }
            else if (attributeType == typeof(double))
            {
                return ((double)objectA).CompareTo((double)objectB);
            }
            else if (attributeType == typeof(float))
            {
                return ((float)objectA).CompareTo((float)objectB);
            }
            else if (attributeType == typeof(bool))
            {
                return ((bool)objectA).CompareTo((bool)objectB);
            }
            else if (attributeType == typeof(AliasedValue))
            {
                return Compare((objectA as AliasedValue)?.Value, (objectB as AliasedValue)?.Value);
            }
#if FAKE_XRM_EASY_9
            else if (attributeType == typeof(OptionSetValueCollection))
            {
                // Compare MultiOptionSetValue by converting to a comparable representation
                // Sort by the first value in the collection, or by count if first values are equal
                OptionSetValueCollection collectionA = (OptionSetValueCollection)objectA;
                OptionSetValueCollection collectionB = (OptionSetValueCollection)objectB;

                // Get the minimum value from each collection (stable ordering)
                var sortedA = collectionA.OrderBy(osv => osv.Value).ToList();
                var sortedB = collectionB.OrderBy(osv => osv.Value).ToList();

                // Compare element by element
                int minCount = Math.Min(sortedA.Count, sortedB.Count);
                for (int i = 0; i < minCount; i++)
                {
                    int cmp = sortedA[i].Value.CompareTo(sortedB[i].Value);
                    if (cmp != 0)
                        return cmp;
                }

                // If all compared elements are equal, compare by count
                return sortedA.Count.CompareTo(sortedB.Count);
            }
#endif
            else
            {
                return 0;
            }
        }
    }
}

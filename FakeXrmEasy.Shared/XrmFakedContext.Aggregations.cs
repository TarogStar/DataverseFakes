using FakeXrmEasy.Extensions.FetchXml;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FakeXrmEasy
{
    /// <summary>
    /// Partial class containing FetchXML aggregate query processing functionality for the faked CRM context.
    /// Provides support for aggregate functions (COUNT, SUM, AVG, MIN, MAX) and grouping operations
    /// in FetchXML queries within the in-memory test environment.
    /// </summary>
    public partial class XrmFakedContext
    {
        /// <summary>
        /// Processes an aggregate FetchXML query and returns the aggregated results.
        /// Supports aggregate functions including COUNT, COUNTCOLUMN, MIN, MAX, AVG, and SUM,
        /// as well as grouping by attributes with optional date grouping.
        /// </summary>
        /// <param name="ctx">The faked CRM context for query execution.</param>
        /// <param name="xmlDoc">The parsed FetchXML document containing the aggregate query.</param>
        /// <param name="resultOfQuery">The pre-filtered list of entities to aggregate.</param>
        /// <returns>A list of entities containing the aggregated results with aliased values.</returns>
        /// <exception cref="Exception">Thrown when the FetchXML contains invalid aggregate syntax or unsupported operations.</exception>
        internal static List<Entity> ProcessAggregateFetchXml(XrmFakedContext ctx, XDocument xmlDoc, List<Entity> resultOfQuery)
        {
            // Validate that <all-attributes> is not present,
            // that all attributes have groupby or aggregate, and an alias,
            // and that there is exactly 1 groupby.
            if (RetrieveFetchXmlNode(xmlDoc, "all-attributes") != null)
            {
                throw new Exception("Can't have <all-attributes /> present when using aggregate");
            }

            var ns = xmlDoc.Root.Name.Namespace;

            var entityName = RetrieveFetchXmlNode(xmlDoc, "entity")?.GetAttribute("name")?.Value;
            if (string.IsNullOrEmpty(entityName))
            {
                throw new Exception("Can't find entity name for aggregate query");
            }

            var aggregates = new List<FetchAggregate>();
            var groups = new List<FetchGrouping>();

            foreach (var attr in xmlDoc.Descendants(ns + "attribute"))
            {
                // Get the immediate parent link-entity's alias (if any)
                // The query execution stores joined attributes with just the immediate alias,
                // not the full ancestor path. Fixes upstream issue #545 - nested aggregate values.
                var parentLinkEntity = attr.Ancestors(ns + "link-entity").FirstOrDefault();
                var immediateAlias = parentLinkEntity != null
                    ? (parentLinkEntity.GetAttribute("alias")?.Value ?? parentLinkEntity.GetAttribute("name").Value)
                    : null;

                string alias;
                string logicalName;

                if (!string.IsNullOrEmpty(immediateAlias))
                {
                    // Attribute is from a linked entity - use the immediate alias only
                    alias = immediateAlias + "." + attr.GetAttribute("alias")?.Value;
                    logicalName = immediateAlias + "." + attr.GetAttribute("name")?.Value;
                }
                else
                {
                    // Attribute is from the main entity
                    alias = attr.GetAttribute("alias")?.Value;
                    logicalName = attr.GetAttribute("name")?.Value;
                }

                if (string.IsNullOrEmpty(alias))
                {
                    throw new Exception("Missing alias for attribute in aggregate fetch xml");
                }

                if (attr.IsAttributeTrue("groupby"))
                {
                    var dategrouping = attr.GetAttribute("dategrouping")?.Value;
                    if (dategrouping != null)
                    {
                        DateGroupType t;
                        if (!Enum.TryParse(dategrouping, true, out t))
                        {
                            throw new Exception("Unknown dategrouping value '" + dategrouping + "'");
                        }
                        groups.Add(new DateTimeGroup()
                        {
                            Type = t,
                            OutputAlias = alias,
                            Attribute = logicalName
                        });
                    }
                    else
                    {
                        groups.Add(new SimpleValueGroup()
                        {
                            OutputAlias = alias,
                            Attribute = logicalName
                        });
                    }
                }
                else
                {
                    var agrFn = attr.GetAttribute("aggregate")?.Value;
                    if (string.IsNullOrEmpty(agrFn))
                    {
                        throw new Exception("Attributes must have be aggregated or grouped by when using aggregation");
                    }

                    FetchAggregate newAgr = null;
                    switch (agrFn?.ToLower())
                    {
                        case "count":
                            newAgr = new CountAggregate();
                            break;

                        case "countcolumn":
                            if (attr.IsAttributeTrue("distinct"))
                            {
                                newAgr = new CountDistinctAggregate();
                            }
                            else
                            {
                                newAgr = new CountColumnAggregate();
                            }
                            break;

                        case "min":
                            newAgr = new MinAggregate();
                            break;

                        case "max":
                            newAgr = new MaxAggregate();
                            break;

                        case "avg":
                            newAgr = new AvgAggregate();
                            break;

                        case "sum":
                            newAgr = new SumAggregate();
                            break;

                        default:
                            throw new Exception("Unknown aggregate function '" + agrFn + "'");
                    }

                    newAgr.OutputAlias = alias;
                    newAgr.Attribute = logicalName;
                    aggregates.Add(newAgr);
                }
            }

            List<Entity> aggregateResult;

            if (groups.Any())
            {
                aggregateResult = ProcessGroupedAggregate(entityName, resultOfQuery, aggregates, groups);
            }
            else
            {
                aggregateResult = new List<Entity>();
                var ent = ProcessAggregatesForSingleGroup(entityName, resultOfQuery, aggregates);
                aggregateResult.Add(ent);
            }

            return OrderAggregateResult(xmlDoc, aggregateResult.AsQueryable());
        }

        /// <summary>
        /// Orders the aggregate query results based on the order clauses in the FetchXML document.
        /// Aggregate queries must use aliases for ordering, not attribute names.
        /// </summary>
        /// <param name="xmlDoc">The FetchXML document containing order clauses.</param>
        /// <param name="result">The aggregate query results to order.</param>
        /// <returns>The ordered list of aggregate result entities.</returns>
        /// <exception cref="Exception">Thrown when an attribute is specified instead of an alias for ordering.</exception>
        private static List<Entity> OrderAggregateResult(XDocument xmlDoc, IQueryable<Entity> result)
        {
            var ns = xmlDoc.Root.Name.Namespace;
            foreach (var order in
                xmlDoc.Root.Element(ns + "entity")
                .Elements(ns + "order"))
            {
                var alias = order.GetAttribute("alias")?.Value;

                // These error is also thrown by CRM
                if (order.GetAttribute("attribute") != null)
                {
                    throw new Exception("An attribute cannot be specified for an order clause for an aggregate Query. Use an alias");
                }
                if (string.IsNullOrEmpty(alias))
                {
                    throw new Exception("An alias is required for an order clause for an aggregate Query.");
                }

                if (order.IsAttributeTrue("descending"))
                    result = result.OrderByDescending(e => e.Attributes.ContainsKey(alias) ? e.Attributes[alias] : null, new XrmOrderByAttributeComparer());
                else
                    result = result.OrderBy(e => e.Attributes.ContainsKey(alias) ? e.Attributes[alias] : null, new XrmOrderByAttributeComparer());
            }

            return result.ToList();
        }

        /// <summary>
        /// Processes aggregate functions for a single group of entities and returns the aggregated result.
        /// Creates a new entity with aliased values containing the aggregate results.
        /// </summary>
        /// <param name="entityName">The logical name of the entity being aggregated.</param>
        /// <param name="entities">The collection of entities to aggregate.</param>
        /// <param name="aggregates">The list of aggregate functions to apply.</param>
        /// <returns>An entity containing the aggregated values as <see cref="AliasedValue"/> attributes.</returns>
        private static Entity ProcessAggregatesForSingleGroup(string entityName, IEnumerable<Entity> entities, IList<FetchAggregate> aggregates)
        {
            var ent = new Entity(entityName);

            foreach (var agg in aggregates)
            {
                var val = agg.Process(entities);
                if (val != null)
                {
                    ent[agg.OutputAlias] = new AliasedValue(null, agg.Attribute, val);
                }
                else
                {
                    //if the aggregate value cannot be calculated
                    //CRM still returns an alias
                    ent[agg.OutputAlias] = new AliasedValue(null, agg.Attribute, null);
                }
            }

            return ent;
        }

        /// <summary>
        /// Processes aggregate functions with grouping and returns the grouped aggregate results.
        /// Groups entities by the specified grouping criteria and applies aggregate functions to each group.
        /// </summary>
        /// <param name="entityName">The logical name of the entity being aggregated.</param>
        /// <param name="resultOfQuery">The list of entities to group and aggregate.</param>
        /// <param name="aggregates">The list of aggregate functions to apply to each group.</param>
        /// <param name="groups">The list of grouping definitions specifying how to group the entities.</param>
        /// <returns>A list of entities, one per group, containing both group values and aggregated values.</returns>
        private static List<Entity> ProcessGroupedAggregate(string entityName, IList<Entity> resultOfQuery, IList<FetchAggregate> aggregates, IList<FetchGrouping> groups)
        {
            // Group by the groupBy-attribute
            var grouped = resultOfQuery.GroupBy(e =>
            {
                return groups
                    .Select(g => g.Process(e))
                    .ToArray();
            }, new ArrayComparer());

            // Perform aggregates in each group
            var result = new List<Entity>();
            foreach (var g in grouped)
            {
                var firstInGroup = g.First();

                // Find the aggregates values in the group
                var ent = ProcessAggregatesForSingleGroup(entityName, g, aggregates);

                // Find the group values
                for (var rule = 0; rule < groups.Count; ++rule)
                {
                    if (g.Key[rule] != null)
                    {
                        object value = g.Key[rule];
                        ent[groups[rule].OutputAlias] = new AliasedValue(null, groups[rule].Attribute, value is ComparableEntityReference ? (value as ComparableEntityReference).entityReference : value);
                    }
                }

                result.Add(ent);
            }

            return result;
        }

        /// <summary>
        /// Abstract base class for FetchXML aggregate functions.
        /// Provides common properties and processing logic for aggregate operations.
        /// </summary>
        private abstract class FetchAggregate
        {
            /// <summary>
            /// Gets or sets the logical name of the attribute to aggregate.
            /// </summary>
            public string Attribute { get; set; }

            /// <summary>
            /// Gets or sets the output alias for the aggregated value in the result.
            /// </summary>
            public string OutputAlias { get; set; }

            /// <summary>
            /// Processes the aggregate function over a collection of entities.
            /// Extracts the attribute values and delegates to the specific aggregate implementation.
            /// </summary>
            /// <param name="entities">The entities to aggregate.</param>
            /// <returns>The aggregated result value.</returns>
            public object Process(IEnumerable<Entity> entities)
            {
                return AggregateValues(entities.Select(e =>
                    e.Contains(Attribute) ? e[Attribute] : null
                ));
            }

            /// <summary>
            /// When overridden in a derived class, performs the specific aggregation logic on the values.
            /// </summary>
            /// <param name="values">The attribute values to aggregate.</param>
            /// <returns>The aggregated result.</returns>
            protected abstract object AggregateValues(IEnumerable<object> values);
        }

        /// <summary>
        /// Abstract base class for aggregate functions that handle aliased values.
        /// Unwraps <see cref="AliasedValue"/> instances before performing aggregation.
        /// </summary>
        private abstract class AliasedAggregate : FetchAggregate
        {
            /// <summary>
            /// Processes values by unwrapping any AliasedValue instances before aggregation.
            /// </summary>
            /// <param name="values">The values to aggregate, which may be wrapped in AliasedValue.</param>
            /// <returns>The aggregated result.</returns>
            protected override object AggregateValues(IEnumerable<object> values)
            {
                var lst = values.Where(x => x != null);
                bool alisedValue = lst.FirstOrDefault() is AliasedValue;
                if (alisedValue)
                {
                    lst = lst.Select(x => (x as AliasedValue)?.Value);
                }

                return AggregateAliasedValues(lst);
            }

            /// <summary>
            /// When overridden in a derived class, performs the specific aggregation on unwrapped values.
            /// </summary>
            /// <param name="values">The unwrapped attribute values to aggregate.</param>
            /// <returns>The aggregated result.</returns>
            protected abstract object AggregateAliasedValues(IEnumerable<object> values);
        }

        /// <summary>
        /// Aggregate function that counts all rows including those with null values.
        /// Equivalent to COUNT(*) in SQL.
        /// </summary>
        private class CountAggregate : FetchAggregate
        {
            /// <summary>
            /// Returns the count of all values in the collection.
            /// </summary>
            protected override object AggregateValues(IEnumerable<object> values)
            {
                return values.Count();
            }
        }

        /// <summary>
        /// Aggregate function that counts non-null values in a column.
        /// Equivalent to COUNT(column) in SQL.
        /// </summary>
        private class CountColumnAggregate : AliasedAggregate
        {
            /// <summary>
            /// Returns the count of non-null values in the collection.
            /// </summary>
            protected override object AggregateAliasedValues(IEnumerable<object> values)
            {
                return values.Where(x => x != null).Count();
            }
        }

        /// <summary>
        /// Aggregate function that counts distinct non-null values in a column.
        /// Equivalent to COUNT(DISTINCT column) in SQL.
        /// </summary>
        private class CountDistinctAggregate : AliasedAggregate
        {
            /// <summary>
            /// Returns the count of distinct non-null values in the collection.
            /// </summary>
            protected override object AggregateAliasedValues(IEnumerable<object> values)
            {
                return values.Where(x => x != null).Distinct().Count();
            }
        }

        /// <summary>
        /// Aggregate function that returns the minimum value in a column.
        /// Supports decimal, Money, int, float, double, and DateTime types.
        /// </summary>
        private class MinAggregate : AliasedAggregate
        {
            /// <summary>
            /// Returns the minimum value from the collection.
            /// </summary>
            /// <exception cref="Exception">Thrown when the value type is not supported for MIN aggregation.</exception>
            protected override object AggregateAliasedValues(IEnumerable<object> values)
            {
                var lst = values.Where(x => x != null);
                if (!lst.Any()) return null;

                var firstValue = lst.Where(x => x != null).First();
                var valType = firstValue.GetType();

                if (valType == typeof(decimal) || valType == typeof(decimal?))
                {
                    return lst.Min(x => (decimal)x);
                }

                if (valType == typeof(Money))
                {
                    return new Money(lst.Min(x => (x as Money).Value));
                }

                if (valType == typeof(int) || valType == typeof(int?))
                {
                    return lst.Min(x => (int)x);
                }

                if (valType == typeof(float) || valType == typeof(float?))
                {
                    return lst.Min(x => (float)x);
                }

                if (valType == typeof(double) || valType == typeof(double?))
                {
                    return lst.Min(x => (double)x);
                }
                
                if (valType == typeof(DateTime) || valType == typeof(DateTime?))
                {
                    return lst.Min(x => (DateTime)x);
                }

                throw new Exception("Unhndled property type '" + valType.FullName + "' in 'min' aggregate");
            }
        }

        /// <summary>
        /// Aggregate function that returns the maximum value in a column.
        /// Supports decimal, Money, int, float, double, and DateTime types.
        /// </summary>
        private class MaxAggregate : AliasedAggregate
        {
            /// <summary>
            /// Returns the maximum value from the collection.
            /// </summary>
            /// <exception cref="Exception">Thrown when the value type is not supported for MAX aggregation.</exception>
            protected override object AggregateAliasedValues(IEnumerable<object> values)
            {
                var lst = values.Where(x => x != null);
                if (!lst.Any()) return null;

                var firstValue = lst.First();
                var valType = firstValue.GetType();

                if (valType == typeof(decimal) || valType == typeof(decimal?))
                {
                    return lst.Max(x => (decimal)x);
                }

                if (valType == typeof(Money))
                {
                    return new Money(lst.Max(x => (x as Money).Value));
                }

                if (valType == typeof(int) || valType == typeof(int?))
                {
                    return lst.Max(x => (int)x);
                }

                if (valType == typeof(float) || valType == typeof(float?))
                {
                    return lst.Max(x => (float)x);
                }

                if (valType == typeof(double) || valType == typeof(double?))
                {
                    return lst.Max(x => (double)x);
                }
                  
                if (valType == typeof(DateTime) || valType == typeof(DateTime?))
                {
                    return lst.Max(x => (DateTime)x);
                }

                throw new Exception("Unhndled property type '" + valType.FullName + "' in 'max' aggregate");
            }
        }

        /// <summary>
        /// Aggregate function that returns the average value in a column.
        /// Supports decimal, Money, int, float, and double types.
        /// </summary>
        private class AvgAggregate : AliasedAggregate
        {
            /// <summary>
            /// Returns the average of values in the collection.
            /// </summary>
            /// <exception cref="Exception">Thrown when the value type is not supported for AVG aggregation.</exception>
            protected override object AggregateAliasedValues(IEnumerable<object> values)
            {
                var lst = values.Where(x => x != null);
                if (!lst.Any()) return null;

                var firstValue = lst.First();
                var valType = firstValue.GetType();

                if (valType == typeof(decimal) || valType == typeof(decimal?))
                {
                    return lst.Average(x => (decimal)x);
                }

                if (valType == typeof(Money))
                {
                    return new Money(lst.Average(x => (x as Money).Value));
                }

                if (valType == typeof(int) || valType == typeof(int?))
                {
                    return lst.Average(x => (int)x);
                }

                if (valType == typeof(float) || valType == typeof(float?))
                {
                    return lst.Average(x => (float)x);
                }

                if (valType == typeof(double) || valType == typeof(double?))
                {
                    return lst.Average(x => (double)x);
                }

                throw new Exception("Unhndled property type '" + valType.FullName + "' in 'avg' aggregate");
            }
        }

        /// <summary>
        /// Aggregate function that returns the sum of values in a column.
        /// Supports decimal, Money, int, float, and double types.
        /// </summary>
        private class SumAggregate : AliasedAggregate
        {
            /// <summary>
            /// Returns the sum of values in the collection.
            /// </summary>
            /// <exception cref="Exception">Thrown when the value type is not supported for SUM aggregation.</exception>
            protected override object AggregateAliasedValues(IEnumerable<object> values)
            {
                var lst = values.ToList().Where(x => x != null);
                // TODO: Check these cases in CRM proper
                if (!lst.Any()) return null;

                var valType = lst.First().GetType();

                if (valType == typeof(decimal) || valType == typeof(decimal?))
                {
                    return lst.Sum(x => x as decimal? ?? 0m);
                }
                if (valType == typeof(Money))
                {
                    return new Money(lst.Sum(x => (x as Money)?.Value ?? 0m));
                }

                if (valType == typeof(int) || valType == typeof(int?))
                {
                    return lst.Sum(x => x as int? ?? 0);
                }

                if (valType == typeof(float) || valType == typeof(float?))
                {
                    return lst.Sum(x => x as float? ?? 0f);
                }

                if (valType == typeof(double) || valType == typeof(double?))
                {
                    return lst.Sum(x => x as double? ?? 0d);
                }
              
                throw new Exception("Unhndled property type '" + valType.FullName + "' in 'sum' aggregate");
            }
        }

        /// <summary>
        /// Abstract base class for FetchXML grouping operations.
        /// Provides common properties and processing logic for group-by operations.
        /// </summary>
        private abstract class FetchGrouping
        {
            /// <summary>
            /// Gets or sets the logical name of the attribute to group by.
            /// </summary>
            public string Attribute { get; set; }

            /// <summary>
            /// Gets or sets the output alias for the grouping value in the result.
            /// </summary>
            public string OutputAlias { get; set; }

            /// <summary>
            /// Processes an entity and extracts the group value for this grouping.
            /// </summary>
            /// <param name="entity">The entity to extract the group value from.</param>
            /// <returns>A comparable value used for grouping.</returns>
            public IComparable Process(Entity entity)
            {
                var attr = entity.Contains(Attribute) ? entity[Attribute] : null;
                return FindGroupValue(attr);
            }

            /// <summary>
            /// When overridden in a derived class, extracts the group value from an attribute value.
            /// </summary>
            /// <param name="attributeValue">The attribute value to process.</param>
            /// <returns>A comparable value used for grouping.</returns>
            public abstract IComparable FindGroupValue(object attributeValue);
        }

        /// <summary>
        /// Equality comparer for arrays of comparable objects.
        /// Used to compare grouping key arrays when grouping by multiple attributes.
        /// </summary>
        private class ArrayComparer : IEqualityComparer<IComparable[]>
        {
            /// <summary>
            /// Determines whether two arrays are equal by comparing their elements sequentially.
            /// </summary>
            /// <param name="x">The first array to compare.</param>
            /// <param name="y">The second array to compare.</param>
            /// <returns>True if the arrays contain equal elements in the same order; otherwise, false.</returns>
            public bool Equals(IComparable[] x, IComparable[] y)
            {
                return x.SequenceEqual(y);
            }

            /// <summary>
            /// Returns a hash code for the array based on XOR of element hash codes.
            /// </summary>
            /// <param name="obj">The array to compute a hash code for.</param>
            /// <returns>A hash code for the array.</returns>
            public int GetHashCode(IComparable[] obj)
            {
                int result = 0;
                foreach (IComparable x in obj)
                {
                    result ^= x == null ? 0 : x.GetHashCode();
                }
                return result;
            }
        }

        /// <summary>
        /// Wrapper class that makes EntityReference comparable for use in grouping operations.
        /// Compares entity references by their Id and LogicalName.
        /// </summary>
        private class ComparableEntityReference : IComparable
        {
            /// <summary>
            /// Gets the wrapped entity reference.
            /// </summary>
            public EntityReference entityReference { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="ComparableEntityReference"/> class.
            /// </summary>
            /// <param name="entityReference">The entity reference to wrap.</param>
            public ComparableEntityReference(EntityReference entityReference)
            {
                this.entityReference = entityReference;
            }

            /// <summary>
            /// Compares this entity reference to another object.
            /// </summary>
            /// <param name="obj">The object to compare to.</param>
            /// <returns>0 if equal; otherwise, 1.</returns>
            int IComparable.CompareTo(object obj)
            {
                return Equals(obj) ? 0 : 1;
            }

            /// <summary>
            /// Determines whether this entity reference equals another object.
            /// Compares by Id and LogicalName.
            /// </summary>
            /// <param name="obj">The object to compare.</param>
            /// <returns>True if the objects represent the same entity reference.</returns>
            public override bool Equals(object obj)
            {
                EntityReference other;
                if (obj is EntityReference)
                {
                    other = obj as EntityReference;
                }
                else if (obj is ComparableEntityReference)
                {
                    other = (obj as ComparableEntityReference).entityReference;
                }
                else
                {
                    return false;
                }
                return entityReference.Id == other.Id && entityReference.LogicalName == other.LogicalName;
            }

            /// <summary>
            /// Returns a hash code based on the entity's LogicalName and Id.
            /// </summary>
            /// <returns>A hash code for this entity reference.</returns>
            public override int GetHashCode()
            {
                return (entityReference.LogicalName == null ? 0 : entityReference.LogicalName.GetHashCode()) ^ entityReference.Id.GetHashCode();
            }
        }

        /// <summary>
        /// Grouping class that groups by the raw attribute value.
        /// Handles EntityReference values by wrapping them in ComparableEntityReference.
        /// </summary>
        private class SimpleValueGroup : FetchGrouping
        {
            /// <summary>
            /// Extracts the group value from an attribute value.
            /// EntityReference values are wrapped for comparison; other values are used directly.
            /// </summary>
            /// <param name="attributeValue">The attribute value to use for grouping.</param>
            /// <returns>A comparable value for grouping.</returns>
            public override IComparable FindGroupValue(object attributeValue)
            {
                if (attributeValue is EntityReference)
                {
                    return new ComparableEntityReference(attributeValue as EntityReference) as IComparable;
                }
                else if (attributeValue is OptionSetValue)
                {
                    return ((OptionSetValue)attributeValue).Value;
                }
                else
                {
                    return attributeValue as IComparable;
                }
            }
        }

        /// <summary>
        /// Specifies the type of date grouping to apply in aggregate queries.
        /// </summary>
        private enum DateGroupType
        {
            /// <summary>Group by the full DateTime value.</summary>
            DateTime,
            /// <summary>Group by day of month (1-31).</summary>
            Day,
            /// <summary>Group by week of year.</summary>
            Week,
            /// <summary>Group by month (1-12).</summary>
            Month,
            /// <summary>Group by quarter (1-4).</summary>
            Quarter,
            /// <summary>Group by year.</summary>
            Year
        }

        /// <summary>
        /// Grouping class that groups DateTime values by a specified date component
        /// such as day, week, month, quarter, or year.
        /// </summary>
        private class DateTimeGroup : FetchGrouping
        {
            /// <summary>
            /// Gets or sets the type of date grouping to apply.
            /// </summary>
            public DateGroupType Type { get; set; }

            /// <summary>
            /// Extracts the appropriate date component from a DateTime value for grouping.
            /// </summary>
            /// <param name="attributeValue">The DateTime value to extract the component from.</param>
            /// <returns>The date component value (day, week, month, quarter, year, or full DateTime).</returns>
            /// <exception cref="Exception">Thrown when the value is not a DateTime or when an unknown DateGroupType is specified.</exception>
            public override IComparable FindGroupValue(object attributeValue)
            {
                if (attributeValue == null) return null;

                if (!(attributeValue is DateTime || attributeValue is DateTime?))
                {
                    throw new Exception("Can only do date grouping of DateTime values");
                }

                var d = attributeValue as DateTime?;

                switch (Type)
                {
                    case DateGroupType.DateTime:
                        return d;

                    case DateGroupType.Day:
                        return d?.Day;

                    case DateGroupType.Week:
                        var cal = System.Globalization.DateTimeFormatInfo.InvariantInfo;
                        return cal.Calendar.GetWeekOfYear(d.Value, cal.CalendarWeekRule, cal.FirstDayOfWeek);

                    case DateGroupType.Month:
                        return d?.Month;

                    case DateGroupType.Quarter:
                        return (d?.Month + 2) / 3;

                    case DateGroupType.Year:
                        return d?.Year;

                    default:
                        throw new Exception("Unhandled date group type");
                }
            }
        }
    }
}

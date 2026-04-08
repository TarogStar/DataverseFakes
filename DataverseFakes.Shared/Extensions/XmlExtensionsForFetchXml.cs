using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using System.Globalization;
using FakeXrmEasy.Models;

namespace FakeXrmEasy.Extensions.FetchXml
{
    /// <summary>
    /// Helper methods that translate FetchXML documents into the SDK query representation used by FakeXrmEasy.
    /// </summary>
    public static class XmlExtensionsForFetchXml
    {
        private static IEnumerable<ConditionOperator> OperatorsNotToConvertArray = new[]
        {
#if FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
            ConditionOperator.OlderThanXWeeks,
            ConditionOperator.OlderThanXYears,
            ConditionOperator.OlderThanXDays,
            ConditionOperator.OlderThanXHours,
            ConditionOperator.OlderThanXMinutes,
#endif
            ConditionOperator.OlderThanXMonths,
            ConditionOperator.LastXDays,
            ConditionOperator.LastXHours,
            ConditionOperator.LastXMonths,
            ConditionOperator.LastXWeeks,
            ConditionOperator.LastXYears,
            ConditionOperator.NextXHours,
            ConditionOperator.NextXDays,
            ConditionOperator.NextXWeeks,
            ConditionOperator.NextXMonths,
            ConditionOperator.NextXYears,
            ConditionOperator.NextXWeeks,
            ConditionOperator.InFiscalYear,
            ConditionOperator.InFiscalPeriod,
            ConditionOperator.InFiscalPeriodAndYear
        };

        /// <summary>
        /// Checks whether the given attribute on the element is logically true ("true" or "1").
        /// </summary>
        /// <param name="elem">The element that contains the attribute.</param>
        /// <param name="attributeName">The attribute name to inspect.</param>
        /// <returns><c>true</c> when the attribute exists and represents truth; otherwise <c>false</c>.</returns>
        public static bool IsAttributeTrue(this XElement elem, string attributeName)
        {
            var val = elem.GetAttribute(attributeName)?.Value;

            return "true".Equals(val, StringComparison.InvariantCultureIgnoreCase)
                || "1".Equals(val, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Indicates whether the FetchXML document aggregates results.
        /// </summary>
        /// <param name="doc">The FetchXML document.</param>
        /// <returns><c>true</c> when the root fetch node has aggregate="true".</returns>
        public static bool IsAggregateFetchXml(this XDocument doc)
        {
            return doc.Root.IsAttributeTrue("aggregate");
        }

        /// <summary>
        /// Indicates whether the FetchXML request is marked as distinct.
        /// </summary>
        /// <param name="doc">The FetchXML document.</param>
        /// <returns><c>true</c> when the root fetch node has distinct="true".</returns>
        public static bool IsDistincFetchXml(this XDocument doc)
        {
            return doc.Root.IsAttributeTrue("distinct");
        }

        /// <summary>
        /// Validates a FetchXML node ensuring the required attributes are present based on the node type.
        /// </summary>
        /// <param name="elem">The node to validate.</param>
        /// <returns><c>true</c> for known nodes with the needed attributes.</returns>
        /// <exception cref="Exception">Thrown when the node type is unknown or missing mandatory attributes.</exception>
        public static bool IsFetchXmlNodeValid(this XElement elem)
        {
            switch (elem.Name.LocalName)
            {
                case "filter":
                    return true;

                case "value":
                case "fetch":
                    return true;

                case "entity":
                    return elem.GetAttribute("name") != null;

                case "all-attributes":
                    return true;

                case "no-attrs":
                    return true;

                case "attribute":
                    return elem.GetAttribute("name") != null;

                case "link-entity":
                    return elem.GetAttribute("name") != null
                            && elem.GetAttribute("from") != null
                            && elem.GetAttribute("to") != null;

                case "order":
                    if (elem.Document.IsAggregateFetchXml())
                    {
                        return elem.GetAttribute("alias") != null
                            && elem.GetAttribute("attribute") == null;
                    }
                    else
                    {
                        return elem.GetAttribute("attribute") != null;
                    }

                case "condition":
                    return elem.GetAttribute("attribute") != null
                           && elem.GetAttribute("operator") != null;

                default:
                    throw new Exception(string.Format("Node {0} is not a valid FetchXml node or it doesn't have the required attributes", elem.Name.LocalName));
            }
        }

        /// <summary>
        /// Retrieves an attribute from an element using the local name.
        /// </summary>
        /// <param name="elem">The source element.</param>
        /// <param name="sAttributeName">The attribute local name.</param>
        /// <returns>The matching attribute or <c>null</c> if it does not exist.</returns>
        public static XAttribute GetAttribute(this XElement elem, string sAttributeName)
        {
            return elem.Attributes().FirstOrDefault((a => a.Name.LocalName.Equals(sAttributeName)));
        }

        /// <summary>
        /// Converts an entity node into a <see cref="ColumnSet"/> representation.
        /// </summary>
        /// <param name="el">The entity element.</param>
        /// <returns>A populated <see cref="ColumnSet"/> honoring all-attributes, no-attrs, or explicit attribute declarations.</returns>
        public static ColumnSet ToColumnSet(this XElement el)
        {
            var allAttributes = el.Elements()
                    .Where(e => e.Name.LocalName.Equals("all-attributes"))
                    .FirstOrDefault();

            if (allAttributes != null)
            {
                return new ColumnSet(true);
            }

            // Get explicitly specified attributes
            var attributes = el.Elements()
                                .Where(e => e.Name.LocalName.Equals("attribute"))
                                .Select(e => e.GetAttribute("name").Value)
                                .ToArray();

            // Check for no-attrs element
            // Per Dataverse behavior: no-attrs defaults to empty ColumnSet,
            // but if explicit attribute elements are ALSO present, those attributes are returned
            // Issue #490
            var noAttrs = el.Elements()
                    .Where(e => e.Name.LocalName.Equals("no-attrs"))
                    .FirstOrDefault();

            if (noAttrs != null && attributes.Length == 0)
            {
                // no-attrs present with no explicit attributes = empty ColumnSet
                return new ColumnSet();
            }

            // Return explicitly specified attributes (or empty if none specified and no no-attrs)
            return new ColumnSet(attributes);
        }

        /// <summary>
        /// Reads the top attribute and converts it into an integer value.
        /// </summary>
        /// <param name="el">The fetch element.</param>
        /// <returns>The parsed top count, or <c>null</c> when not specified.</returns>
        /// <exception cref="Exception">Thrown when the attribute cannot be parsed.</exception>
        public static int? ToTopCount(this XElement el)
        {
            var countAttr = el.GetAttribute("top");
            if (countAttr == null) return null;

            int iCount;
            if (!int.TryParse(countAttr.Value, out iCount))
                throw new Exception("Top attribute in fetch node must be an integer");

            return iCount;
        }

        /// <summary>
        /// Reads the count attribute and converts it into an integer value.
        /// </summary>
        /// <param name="el">The fetch element.</param>
        /// <returns>The parsed count, or <c>null</c> when not specified.</returns>
        /// <exception cref="Exception">Thrown when the attribute cannot be parsed.</exception>
        public static int? ToCount(this XElement el)
        {
            var countAttr = el.GetAttribute("count");
            if (countAttr == null) return null;

            int iCount;
            if (!int.TryParse(countAttr.Value, out iCount))
                throw new Exception("Count attribute in fetch node must be an integer");

            return iCount;
        }


        /// <summary>
        /// Determines whether the caller requested the total record count in the FetchXML.
        /// </summary>
        /// <param name="el">The fetch element.</param>
        /// <returns><c>true</c> when returntotalrecordcount is truthy.</returns>
        /// <exception cref="Exception">Thrown when the attribute cannot be parsed.</exception>
        public static bool ToReturnTotalRecordCount(this XElement el)
        {
            var returnTotalRecordCountAttr = el.GetAttribute("returntotalrecordcount");
            if (returnTotalRecordCountAttr == null) return false;

            bool bReturnCount;
            if (!bool.TryParse(returnTotalRecordCountAttr.Value, out bReturnCount))
                throw new Exception("returntotalrecordcount attribute in fetch node must be an boolean");

            return bReturnCount;
        }

        /// <summary>
        /// Parses the page attribute from the fetch element.
        /// </summary>
        /// <param name="el">The fetch element.</param>
        /// <returns>The page number or <c>null</c> if not present.</returns>
        /// <exception cref="Exception">Thrown when the attribute cannot be parsed.</exception>
        public static int? ToPageNumber(this XElement el)
        {
            var pageAttr = el.GetAttribute("page");
            if (pageAttr == null) return null;

            int iPage;
            if (!int.TryParse(pageAttr.Value, out iPage))
                throw new Exception("Count attribute in fetch node must be an integer");

            return iPage;
        }

        /// <summary>
        /// Converts the FetchXML document root into a <see cref="ColumnSet"/>.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <returns>The resolved column set.</returns>
        public static ColumnSet ToColumnSet(this XDocument xlDoc)
        {
            //Check if all-attributes exist
            return xlDoc.Elements()   //fetch
                    .Elements()
                    .FirstOrDefault()
                    .ToColumnSet();
        }


        /// <summary>
        /// Gets the top attribute from the FetchXML document.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <returns>The parsed top count.</returns>
        public static int? ToTopCount(this XDocument xlDoc)
        {
            //Check if all-attributes exist
            return xlDoc.Elements()   //fetch
                    .FirstOrDefault()
                    .ToTopCount();
        }

        /// <summary>
        /// Gets the count attribute from the FetchXML document.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <returns>The parsed count.</returns>
        public static int? ToCount(this XDocument xlDoc)
        {
            //Check if all-attributes exist
            return xlDoc.Elements()   //fetch
                    .FirstOrDefault()
                    .ToCount();
        }

        /// <summary>
        /// Indicates whether the FetchXML request asked for the total record count.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <returns><c>true</c> when returntotalrecordcount evaluates to true.</returns>
        public static bool ToReturnTotalRecordCount(this XDocument xlDoc)
        {
            return xlDoc.Elements()   //fetch
                    .FirstOrDefault()
                    .ToReturnTotalRecordCount();
        }


        /// <summary>
        /// Gets the requested page number from the FetchXML document.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <returns>The parsed page number or <c>null</c>.</returns>
        public static int? ToPageNumber(this XDocument xlDoc)
        {
            //Check if all-attributes exist
            return xlDoc.Elements()   //fetch
                    .FirstOrDefault()
                    .ToPageNumber();
        }

        /// <summary>
        /// Converts the FetchXML filter nodes into a <see cref="FilterExpression"/> tree.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <param name="ctx">The fake context used to resolve metadata.</param>
        /// <returns>A combined filter expression or <c>null</c> when no filters exist.</returns>
        public static FilterExpression ToCriteria(this XDocument xlDoc, XrmFakedContext ctx)
        {
            var filters = xlDoc.Elements()   //fetch
                    .Elements()     //entity
                    .Elements()     //child nodes of entity
                    .Where(el => el.Name.LocalName.Equals("filter"))
                    .Select(el => el.ToFilterExpression(ctx))
                    .ToList();

            // Handle multiple filter elements (resolves upstream issue #507)
            if (filters.Count == 0)
                return null;

            if (filters.Count == 1)
                return filters[0];

            // Multiple filters at the same level are combined with AND logic
            var combinedFilter = new FilterExpression(LogicalOperator.And);
            foreach (var filter in filters)
            {
                combinedFilter.AddFilter(filter);
            }

            return combinedFilter;
        }

        /// <summary>
        /// Traverses the XML tree to find the entity name associated with a condition.
        /// </summary>
        /// <param name="el">The condition element.</param>
        /// <returns>The logical name of the owning entity or <c>null</c>.</returns>
        public static string GetAssociatedEntityNameForConditionExpression(this XElement el)
        {

            while (el != null)
            {
                var parent = el.Parent;
                if (parent.Name.LocalName.Equals("entity") || parent.Name.LocalName.Equals("link-entity"))
                {
                    return parent.GetAttribute("name").Value;
                }
                el = parent;
            }

            return null;
        }

        /// <summary>
        /// Converts a link-entity node into a <see cref="LinkEntity"/> object.
        /// </summary>
        /// <param name="el">The link-entity element.</param>
        /// <param name="ctx">The fake context used for nested conversions.</param>
        /// <returns>A populated <see cref="LinkEntity"/>.</returns>
        public static LinkEntity ToLinkEntity(this XElement el, XrmFakedContext ctx)
        {
      //Create this node
      var linkEntity = new LinkEntity
      {
        LinkFromEntityName = el.Parent.GetAttribute("name").Value,
        LinkFromAttributeName = el.GetAttribute("to").Value,
        LinkToAttributeName = el.GetAttribute("from").Value,
        LinkToEntityName = el.GetAttribute("name").Value
      };

      if (el.GetAttribute("alias") != null)
            {
                linkEntity.EntityAlias = el.GetAttribute("alias").Value;
            }

            //Join operator
            if (el.GetAttribute("link-type") != null)
            {
                switch (el.GetAttribute("link-type").Value.ToLowerInvariant())
                {
                    case "outer":
                        linkEntity.JoinOperator = JoinOperator.LeftOuter;
                        break;
                    case "any":
                        linkEntity.JoinOperator = JoinOperator.Any;
                        break;
                    case "not any":
                        linkEntity.JoinOperator = JoinOperator.NotAny;
                        break;
                    case "all":
                        linkEntity.JoinOperator = JoinOperator.All;
                        break;
                    case "not all":
                        linkEntity.JoinOperator = JoinOperator.NotAll;
                        break;
                    case "exists":
                        linkEntity.JoinOperator = JoinOperator.Exists;
                        break;
                    case "in":
                        linkEntity.JoinOperator = JoinOperator.In;
                        break;
                    case "matchfirstrowusingcrossapply":
                        linkEntity.JoinOperator = JoinOperator.MatchFirstRowUsingCrossApply;
                        break;
                    default:
                        linkEntity.JoinOperator = JoinOperator.Inner;
                        break;
                }
            }

            //Process other link entities recursively
            var convertedLinkEntityNodes = el.Elements()
                                .Where(e => e.Name.LocalName.Equals("link-entity"))
                                .Select(e => e.ToLinkEntity(ctx))
                                .ToList();

            foreach (var le in convertedLinkEntityNodes)
            {
                linkEntity.LinkEntities.Add(le);
            }

            //Process column sets
            linkEntity.Columns = el.ToColumnSet();

            //Process filter - handle multiple filters (resolves upstream issue #507)
            var linkFilters = el.Elements()
                                .Where(e => e.Name.LocalName.Equals("filter"))
                                .Select(e => e.ToFilterExpression(ctx))
                                .ToList();

            if (linkFilters.Count == 1)
            {
                linkEntity.LinkCriteria = linkFilters[0];
            }
            else if (linkFilters.Count > 1)
            {
                // Multiple filters at the same level are combined with AND logic
                var combinedFilter = new FilterExpression(LogicalOperator.And);
                foreach (var filter in linkFilters)
                {
                    combinedFilter.AddFilter(filter);
                }
                linkEntity.LinkCriteria = combinedFilter;
            }

            return linkEntity;
        }

        /// <summary>
        /// Converts all link-entity nodes under the root entity into <see cref="LinkEntity"/> instances.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <param name="ctx">The fake context used for nested conversions.</param>
        /// <returns>A list of link entities.</returns>
        public static List<LinkEntity> ToLinkEntities(this XDocument xlDoc, XrmFakedContext ctx)
        {
            return xlDoc.Elements()   //fetch
                    .Elements()     //entity
                    .Elements()     //child nodes of entity
                    .Where(el => el.Name.LocalName.Equals("link-entity"))
                    .Select(el => el.ToLinkEntity(ctx))
                    .ToList();
        }

        /// <summary>
        /// Converts order nodes into a list of <see cref="OrderExpression"/> instances.
        /// </summary>
        /// <param name="xlDoc">The FetchXML document.</param>
        /// <returns>A list of order expressions in document order.</returns>
        public static List<OrderExpression> ToOrderExpressionList(this XDocument xlDoc)
        {
            var orderByElements = xlDoc.Elements()   //fetch
                                .Elements()     //entity
                                .Elements()     //child nodes of entity
                                .Where(el => el.Name.LocalName.Equals("order"))
                                .Select(el =>
                                        new OrderExpression
                                        {
                                            AttributeName = el.GetAttribute("attribute").Value,
                                            OrderType = el.IsAttributeTrue("descending") ? OrderType.Descending : OrderType.Ascending
                                        })
                                .ToList();

            return orderByElements;
        }

        /// <summary>
        /// Converts a filter node into a <see cref="FilterExpression"/> with nested conditions.
        /// </summary>
        /// <param name="elem">The filter element.</param>
        /// <param name="ctx">The fake context used for metadata-driven conversions.</param>
        /// <returns>A populated filter expression.</returns>
        public static FilterExpression ToFilterExpression(this XElement elem, XrmFakedContext ctx)
        {
            var filterExpression = new FilterExpression();

            var filterType = elem.GetAttribute("type");
            if (filterType == null)
            {
                filterExpression.FilterOperator = LogicalOperator.And; //By default
            }
            else
            {
                filterExpression.FilterOperator = filterType.Value.Equals("and") ?
                                                  LogicalOperator.And : LogicalOperator.Or;
            }

            //Process other filters recursively
            var otherFilters = elem
                        .Elements() //child nodes of this filter
                        .Where(el => el.Name.LocalName.Equals("filter"))
                        .Select(el => el.ToFilterExpression(ctx))
                        .ToList();


            //Process conditions
            var conditions = elem
                        .Elements() //child nodes of this filter
                        .Where(el => el.Name.LocalName.Equals("condition"))
                        .Select(el => el.ToConditionExpression(ctx))
                        .ToList();

            foreach (var c in conditions)
                filterExpression.AddCondition(c);

            foreach (var f in otherFilters)
                filterExpression.AddFilter(f);

            return filterExpression;
        }

        /// <summary>
        /// Converts the textual value of a condition node into a typed value.
        /// </summary>
        /// <param name="elem">The value element.</param>
        /// <param name="ctx">The fake context providing metadata.</param>
        /// <param name="sEntityName">The logical name of the entity.</param>
        /// <param name="sAttributeName">The attribute the condition targets.</param>
        /// <param name="op">The condition operator.</param>
        /// <returns>A value cast to the correct SDK type.</returns>
        public static object ToValue(this XElement elem, XrmFakedContext ctx, string sEntityName, string sAttributeName, ConditionOperator op)
        {
            return GetConditionExpressionValueCast(elem.Value, ctx, sEntityName, sAttributeName, op);
        }

        /// <summary>
        /// Converts a condition node into a <see cref="ConditionExpression"/>.
        /// </summary>
        /// <param name="elem">The condition element.</param>
        /// <param name="ctx">The fake context providing metadata.</param>
        /// <returns>A fully populated condition expression.</returns>
        public static ConditionExpression ToConditionExpression(this XElement elem, XrmFakedContext ctx)
        {
            var conditionExpression = new ConditionExpression();

            var conditionEntityName = "";

            var attributeName = elem.GetAttribute("attribute").Value;
            ConditionOperator op = ConditionOperator.Equal;

            string value = null;
            string valueOfColumn = null;

            if (elem.GetAttribute("value") != null)
            {
                value = elem.GetAttribute("value").Value;
            }
            // Support for FetchXML valueof attribute (column-to-column comparison)
            // Addresses upstream issue #514
            if (elem.GetAttribute("valueof") != null)
            {
                valueOfColumn = elem.GetAttribute("valueof").Value;
            }
            if (elem.GetAttribute("entityname") != null)
            {
                conditionEntityName = elem.GetAttribute("entityname").Value;
            }

            switch (elem.GetAttribute("operator").Value)
            {
                case "eq":
                    op = ConditionOperator.Equal;
                    break;
                case "ne":
                case "neq":
                    op = ConditionOperator.NotEqual;
                    break;
                case "begins-with":
                    op = ConditionOperator.BeginsWith;
                    break;
                case "not-begin-with":
                    op = ConditionOperator.DoesNotBeginWith;
                    break;
                case "ends-with":
                    op = ConditionOperator.EndsWith;
                    break;
                case "not-end-with":
                    op = ConditionOperator.DoesNotEndWith;
                    break;
                case "in":
                    op = ConditionOperator.In;
                    break;
                case "not-in":
                    op = ConditionOperator.NotIn;
                    break;
                case "null":
                    op = ConditionOperator.Null;
                    break;
                case "not-null":
                    op = ConditionOperator.NotNull;
                    break;
                case "like":
                    op = ConditionOperator.Like;

                    if (value != null)
                    {
                        if (value.StartsWith("%") && !value.EndsWith("%"))
                            op = ConditionOperator.EndsWith;
                        else if (!value.StartsWith("%") && value.EndsWith("%"))
                            op = ConditionOperator.BeginsWith;
                        else if (value.StartsWith("%") && value.EndsWith("%"))
                            op = ConditionOperator.Contains;

                        value = value.Replace("%", "");
                    }
                    break;
                case "not-like":
                    op = ConditionOperator.NotLike;
                    if (value != null)
                    {
                        if (value.StartsWith("%") && !value.EndsWith("%"))
                            op = ConditionOperator.DoesNotEndWith;
                        else if (!value.StartsWith("%") && value.EndsWith("%"))
                            op = ConditionOperator.DoesNotBeginWith;
                        else if (value.StartsWith("%") && value.EndsWith("%"))
                            op = ConditionOperator.DoesNotContain;

                        value = value.Replace("%", "");
                    }
                    break;
                case "gt":
                    op = ConditionOperator.GreaterThan;
                    break;
                case "ge":
                    op = ConditionOperator.GreaterEqual;
                    break;
                case "lt":
                    op = ConditionOperator.LessThan;
                    break;
                case "le":
                    op = ConditionOperator.LessEqual;
                    break;
                case "on":
                    op = ConditionOperator.On;
                    break;
                case "on-or-before":
                    op = ConditionOperator.OnOrBefore;
                    break;
                case "on-or-after":
                    op = ConditionOperator.OnOrAfter;
                    break;
                case "today":
                    op = ConditionOperator.Today;
                    break;
                case "yesterday":
                    op = ConditionOperator.Yesterday;
                    break;
                case "tomorrow":
                    op = ConditionOperator.Tomorrow;
                    break;
                case "between":
                    op = ConditionOperator.Between;
                    break;
                case "not-between":
                    op = ConditionOperator.NotBetween;
                    break;
                case "eq-userid":
                    op = ConditionOperator.EqualUserId;
                    break;
                case "ne-userid":
                    op = ConditionOperator.NotEqualUserId;
                    break;
                case "olderthan-x-months":
                    op = ConditionOperator.OlderThanXMonths;
                    break;
                case "last-seven-days":
                    op = ConditionOperator.Last7Days;
                    break;
                case "eq-businessid":
                    op = ConditionOperator.EqualBusinessId;
                    break;
                case "neq-businessid":
                    op = ConditionOperator.NotEqualBusinessId;
                    break;
                case "next-x-weeks":
                    op = ConditionOperator.NextXWeeks;
                    break;
                case "next-seven-days":
                    op = ConditionOperator.Next7Days;
                    break;
                case "this-year":
                    op = ConditionOperator.ThisYear;
                    break;
                case "last-year":
                    op = ConditionOperator.LastYear;
                    break;
                case "next-year":
                    op = ConditionOperator.NextYear;
                    break;
                case "last-x-hours":
                    op = ConditionOperator.LastXHours;
                    break;
                case "last-x-days":
                    op = ConditionOperator.LastXDays;
                    break;
                case "last-x-weeks":
                    op = ConditionOperator.LastXWeeks;
                    break;
                case "last-x-months":
                    op = ConditionOperator.LastXMonths;
                    break;
                case "last-x-years":
                    op = ConditionOperator.LastXYears;
                    break;
                case "next-x-hours":
                    op = ConditionOperator.NextXHours;
                    break;
                case "next-x-days":
                    op = ConditionOperator.NextXDays;
                    break;
                case "next-x-months":
                    op = ConditionOperator.NextXMonths;
                    break;
                case "next-x-years":
                    op = ConditionOperator.NextXYears;
                    break;
                case "this-month":
                    op = ConditionOperator.ThisMonth;
                    break;
                case "last-month":
                    op = ConditionOperator.LastMonth;
                    break;
                case "next-month":
                    op = ConditionOperator.NextMonth;
                    break;
                case "last-week":
                    op = ConditionOperator.LastWeek;
                    break;
                case "this-week":
                    op = ConditionOperator.ThisWeek;
                    break;
                case "next-week":
                    op = ConditionOperator.NextWeek;
                    break;
                case "in-fiscal-year":
                    op = ConditionOperator.InFiscalYear;
                    break;
                case "in-fiscal-period":
                    op = ConditionOperator.InFiscalPeriod;
                    break;
                case "in-fiscal-period-and-year":
                    op = ConditionOperator.InFiscalPeriodAndYear;
                    break;
                case "this-fiscal-period":
                    op = ConditionOperator.ThisFiscalPeriod;
                    break;
                case "last-fiscal-period":
                    op = ConditionOperator.LastFiscalPeriod;
                    break;
                case "next-fiscal-period":
                    op = ConditionOperator.NextFiscalPeriod;
                    break;
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013
                case "olderthan-x-minutes":
                    op = ConditionOperator.OlderThanXMinutes;
                    break;
                case "olderthan-x-hours":
                    op = ConditionOperator.OlderThanXHours;
                    break;
                case "olderthan-x-days":
                    op = ConditionOperator.OlderThanXDays;
                    break;
                case "olderthan-x-weeks":
                    op = ConditionOperator.OlderThanXWeeks;
                    break;
                case "olderthan-x-years":
                    op = ConditionOperator.OlderThanXYears;
                    break;
#endif
#if FAKE_XRM_EASY_9
                case "contain-values":
                    op = ConditionOperator.ContainValues;
                    break;
                case "not-contain-values":
                    op = ConditionOperator.DoesNotContainValues;
                    break;
#endif
                // Hierarchy operators (issue #287)
                case "above":
                    op = ConditionOperator.Above;
                    break;
                case "eq-or-above":
                    op = ConditionOperator.AboveOrEqual;
                    break;
                case "under":
                    op = ConditionOperator.Under;
                    break;
                case "eq-or-under":
                    op = ConditionOperator.UnderOrEqual;
                    break;
                case "not-under":
                    op = ConditionOperator.NotUnder;
                    break;
                default:
                    throw PullRequestException.FetchXmlOperatorNotImplemented(elem.GetAttribute("operator").Value);
            }

            //Process values
            object[] values = null;


            var entityName = GetAssociatedEntityNameForConditionExpression(elem);

            //Find values inside the condition expression, if apply
            values = elem
                        .Elements() //child nodes of this filter
                        .Where(el => el.Name.LocalName.Equals("value"))
                        .Select(el => el.ToValue(ctx, entityName, attributeName, op))
                        .ToArray();


            // Handle column-to-column comparison (valueof attribute)
            // Store the column name as a ColumnComparisonValue marker in the Values array
            if (valueOfColumn != null)
            {
                var columnComparisonValue = new ColumnComparisonValue(valueOfColumn);
#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
                if (string.IsNullOrWhiteSpace(conditionEntityName))
                {
                    return new ConditionExpression(attributeName, op, columnComparisonValue);
                }
                else
                {
                    return new ConditionExpression(conditionEntityName, attributeName, op, columnComparisonValue);
                }
#else
                return new ConditionExpression(attributeName, op, columnComparisonValue);
#endif
            }

            //Otherwise, a single value was used
            if (value != null)
            {
#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
                if (string.IsNullOrWhiteSpace(conditionEntityName))
                {
                    return new ConditionExpression(attributeName, op, GetConditionExpressionValueCast(value, ctx, entityName, attributeName, op));
                }
                else
                {
                    return new ConditionExpression(conditionEntityName, attributeName, op, GetConditionExpressionValueCast(value, ctx, entityName, attributeName, op));
                }

#else
                return new ConditionExpression(attributeName, op, GetConditionExpressionValueCast(value, ctx, entityName, attributeName, op));

#endif
            }

#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

            if (string.IsNullOrWhiteSpace(conditionEntityName))
            {
                return new ConditionExpression(attributeName, op, values);
            }
            else
            {
                return new ConditionExpression(conditionEntityName, attributeName, op, values);
            }
#else
            return new ConditionExpression(attributeName, op, values);
#endif



        }

        /// <summary>
        /// Casts a string literal from FetchXML into the expected SDK type for comparisons.
        /// </summary>
        /// <param name="t">The destination type.</param>
        /// <param name="value">The string representation coming from FetchXML.</param>
        /// <returns>The value converted to the target type.</returns>
        /// <exception cref="Exception">Thrown when the value cannot be parsed as the requested type.</exception>
        public static object GetValueBasedOnType(Type t, string value)
        {
            if (t == typeof(int)
                || t == typeof(int?)
                || t.IsOptionSet()
#if FAKE_XRM_EASY_9
                || t.IsOptionSetValueCollection()
#endif
            )
            {
                int intValue = 0;

                if (int.TryParse(value, out intValue))
                {
                    if (t.IsOptionSet())
                    {
                        return new OptionSetValue(intValue);
                    }
                    return intValue;
                }
                else
                {
                    throw new Exception("Integer value expected");
                }
            }

            else if (t == typeof(Guid)
                || t == typeof(Guid?)
                || t == typeof(EntityReference)
#if FAKE_XRM_EASY
                    || t == typeof(Microsoft.Xrm.Client.CrmEntityReference) 
#endif
                )
            {
                Guid gValue = Guid.Empty;

                if (Guid.TryParse(value, out gValue))
                {
                    if (t == typeof(EntityReference)
#if FAKE_XRM_EASY
                    || t == typeof(Microsoft.Xrm.Client.CrmEntityReference) 
#endif
                        )
                    {
                        return new EntityReference() { Id = gValue };
                    }
                    return gValue;
                }
                else
                {
                    throw new Exception("Guid value expected");
                }
            }
            else if (t == typeof(decimal)
                || t == typeof(decimal?)
                || t == typeof(Money))
            {
                decimal decValue = 0;
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decValue))
                {
                    if (t == typeof(Money))
                    {
                        return new Money(decValue);
                    }
                    return decValue;
                }
                else
                {
                    throw new Exception("Decimal value expected");
                }
            }

            else if (t == typeof(double)
                || t == typeof(double?))
            {
                double dblValue = 0;
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out dblValue))
                {
                    return dblValue;
                }
                else
                {
                    throw new Exception("Double value expected");
                }
            }

            else if (t == typeof(float)
                || t == typeof(float?))
            {
                float fltValue = 0;
                if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out fltValue))
                {
                    return fltValue;
                }
                else
                {
                    throw new Exception("Float value expected");
                }
            }

            else if (t == typeof(DateTime)
                || t == typeof(DateTime?))
            {
                DateTime dtValue = DateTime.MinValue;
                // Issue #439: FetchXML values are always in ISO format, so use InvariantCulture for parsing
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtValue))
                {
                    return dtValue;
                }
                else
                {
                    throw new Exception("DateTime value expected");
                }
            }
            //fix Issue #141
            else if (t == typeof(bool)
                || t == typeof(bool?))
            {
                bool boolValue = false;
                if (bool.TryParse(value, out boolValue))
                {
                    return boolValue;
                }
                else
                {
                    switch (value)
                    {
                        case "0": return false;
                        case "1": return true;
                        default:
                            throw new Exception("Boolean value expected");
                    }
                }
            }

            //Otherwise, return the string
            return value;
        }

        /// <summary>
        /// Determines whether a condition operator requires type conversion instead of integer parsing.
        /// </summary>
        /// <param name="conditionOperator">The operator used in the condition.</param>
        /// <returns><c>true</c> when conversion is required.</returns>
        public static bool ValueNeedsConverting(ConditionOperator conditionOperator)
        {
            return !OperatorsNotToConvertArray.Contains(conditionOperator);
        }

        /// <summary>
        /// Converts a FetchXML value into the appropriate type by consulting proxy metadata when available.
        /// </summary>
        /// <param name="value">The raw value from FetchXML.</param>
        /// <param name="ctx">The fake context used to inspect metadata.</param>
        /// <param name="sEntityName">The logical name of the entity.</param>
        /// <param name="sAttributeName">The logical name of the attribute.</param>
        /// <param name="op">The operator applied to the condition.</param>
        /// <returns>A typed representation of the value.</returns>
        public static object GetConditionExpressionValueCast(string value, XrmFakedContext ctx, string sEntityName, string sAttributeName, ConditionOperator op)
        {
            if (ctx.ProxyTypesAssembly != null)
            {
                //We have proxy types so get appropiate type value based on entity name and attribute type
                var reflectedType = ctx.FindReflectedType(sEntityName);
                if (reflectedType != null)
                {
                    var attributeType = ctx.FindReflectedAttributeType(reflectedType, sEntityName, sAttributeName);
                    if (attributeType != null)
                    {
                        try
                        {
                            if (ValueNeedsConverting(op))
                            {
                                return GetValueBasedOnType(attributeType, value);
                            }

                            else
                            {
                                return int.Parse(value);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception(string.Format("When trying to parse value for entity {0} and attribute {1}: {2}", sEntityName, sAttributeName, e.Message));
                        }

                    }
                }
            }


            //Try parsing a guid
            Guid gOut = Guid.Empty;
            if (Guid.TryParse(value, out gOut))
                return gOut;

            //Try checking if it is a numeric value, cause, from the fetchxml it
            //would be impossible to know the real typed based on the string value only
            // ex: "123" might compared as a string, or, as an int, it will depend on the attribute
            //    data type, therefore, in this case we do need to use proxy types
            //
            // However, we can make a best-effort attempt to parse the value and let the query
            // engine handle type comparison at runtime (resolves upstream issue #507)
            //
            // IMPORTANT: We try decimal BEFORE int because:
            // 1. Decimal can represent both integer and decimal values
            // 2. The query engine can convert decimal to int for OptionSetValue comparisons
            // 3. But int cannot be converted to decimal for Money comparisons
            // 4. This way "100" works for both Money(100) and OptionSetValue(100)

            // Try parsing as decimal first (for money, decimal fields, and integers)
            decimal decValue = 0.0m;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decValue))
                return decValue;

            // Try parsing as double (for double fields)
            double dblValue = 0.0;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out dblValue))
                return dblValue;

            // Try parsing as DateTime (for date fields)
            // Issue #439: FetchXML values are always in ISO format, so use InvariantCulture for parsing
            DateTime dtValue = DateTime.MinValue;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtValue))
                return dtValue;

            //Default value - return as string
            return value;
        }
    }
}

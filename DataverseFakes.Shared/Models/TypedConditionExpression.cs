using Microsoft.Xrm.Sdk.Query;
using System;

namespace FakeXrmEasy.Models
{
    /// <summary>
    /// A condition expression with a decorated type.
    /// Wraps a ConditionExpression and provides additional type information for query processing.
    /// </summary>
    public class TypedConditionExpression
    {
        /// <summary>
        /// Gets or sets the underlying ConditionExpression from the SDK.
        /// </summary>
        public ConditionExpression CondExpression { get; set; }

        /// <summary>
        /// Gets or sets the CLR type of the attribute being filtered.
        /// </summary>
        public Type AttributeType { get; set; }

        /// <summary>
        /// True if the condition came from a left outer join, in which case should be applied only if not null
        /// </summary>
        public bool IsOuter { get; set; }

        /// <summary>
        /// Gets or sets the attribute name for column-to-column comparison (FetchXML valueof attribute).
        /// When set, the condition compares the attribute value against another column's value
        /// instead of a constant value. Addresses upstream issue #514.
        /// </summary>
        public string ValueOfAttribute { get; set; }

        /// <summary>
        /// Gets whether this condition is a column-to-column comparison.
        /// </summary>
        public bool IsColumnComparison => !string.IsNullOrEmpty(ValueOfAttribute);

        /// <summary>
        /// Initializes a new instance of the TypedConditionExpression class with the specified condition expression.
        /// </summary>
        /// <param name="c">The ConditionExpression to wrap.</param>
        public TypedConditionExpression(ConditionExpression c)
        {
            IsOuter = false;
            CondExpression = c;
            ValueOfAttribute = null;
        }
    }
}

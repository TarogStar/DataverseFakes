namespace FakeXrmEasy.Models
{
    /// <summary>
    /// Marker class to indicate a column-to-column comparison in FetchXML conditions.
    /// When this is stored in ConditionExpression.Values, it indicates the condition
    /// should compare against another column's value instead of a constant.
    /// Addresses upstream issue #514 - FetchXML valueof attribute support.
    /// </summary>
    public class ColumnComparisonValue
    {
        /// <summary>
        /// Gets or sets the name of the column to compare against.
        /// Can be just a column name for same-entity comparison (e.g., "lastname"),
        /// or an alias-qualified name for cross-entity comparison (e.g., "acct.name").
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Initializes a new instance of the ColumnComparisonValue class.
        /// </summary>
        /// <param name="columnName">The column name from the valueof attribute.</param>
        public ColumnComparisonValue(string columnName)
        {
            ColumnName = columnName;
        }

        /// <summary>
        /// Returns the column name.
        /// </summary>
        public override string ToString()
        {
            return ColumnName;
        }
    }
}

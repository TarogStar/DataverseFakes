namespace FakeXrmEasy
{
    /// <summary>
    /// Represents a relationship between two entities in the faked CRM context.
    /// Supports both many-to-many (N:N) and one-to-many (1:N) relationship types.
    /// </summary>
    public class XrmFakedRelationship
    {

        private string entity1Attribute = string.Empty;
        private string entity2Attribute = string.Empty;

        /// <summary>
        /// Schema name of the many to many intersect entity
        /// </summary>
        public string IntersectEntity { get; set; }

        /// <summary>
        /// Entity name and attribute of the first entity participating in the relationship
        /// </summary>
        public string Entity1Attribute
        {
            get
            {
                if (entity1Attribute == entity2Attribute && Entity1LogicalName == Entity2LogicalName)
                {
                    return entity1Attribute + "one";
                }
                else
                {
                    return entity1Attribute;
                }
            }
            set { entity1Attribute = value; }
        }

        /// <summary>
        /// Gets or sets the logical name of the first entity participating in the relationship.
        /// </summary>
        public string Entity1LogicalName { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the second entity participating in the relationship.
        /// </summary>
        public string Entity2LogicalName { get; set; }

        /// <summary>
        /// Entity name and attribute of the second entity participating in the relationship
        /// </summary>
        public string Entity2Attribute
        {
            get
            {
                if (entity1Attribute == entity2Attribute && Entity1LogicalName == Entity2LogicalName)
                {
                    return entity2Attribute + "two";
                }
                else
                {
                    return entity2Attribute;
                }
            }
            set { entity2Attribute = value; }
        }

        /// <summary>
        /// Initializes a new instance of the XrmFakedRelationship class with default ManyToMany relationship type.
        /// </summary>
        public XrmFakedRelationship()
        {
            RelationshipType = enmFakeRelationshipType.ManyToMany;
        }

        /// <summary>
        /// Specifies the type of relationship between entities.
        /// </summary>
        public enum enmFakeRelationshipType
        {
            /// <summary>
            /// Represents a many-to-many (N:N) relationship between entities.
            /// </summary>
            ManyToMany = 0,

            /// <summary>
            /// Represents a one-to-many (1:N) relationship between entities.
            /// </summary>
            OneToMany = 1
        }

        /// <summary>
        /// Gets or sets the type of relationship (ManyToMany or OneToMany).
        /// </summary>
        public enmFakeRelationshipType RelationshipType { get; set; }

        /// <summary>
        /// Initializes a N:N relationship type
        /// </summary>
        /// <param name="entityName">The schema name of the intersect entity for the many-to-many relationship.</param>
        /// <param name="entity1Attribute">The attribute name on the first entity.</param>
        /// <param name="entity2Attribute">The attribute name on the second entity.</param>
        /// <param name="entity1LogicalName">The logical name of the first entity.</param>
        /// <param name="entity2LogicalName">The logical name of the second entity.</param>
        public XrmFakedRelationship(string entityName, string entity1Attribute, string entity2Attribute, string entity1LogicalName, string entity2LogicalName)
        {
            IntersectEntity = entityName;
            Entity1Attribute = entity1Attribute;
            Entity2Attribute = entity2Attribute;
            Entity1LogicalName = entity1LogicalName;
            Entity2LogicalName = entity2LogicalName;
            RelationshipType = enmFakeRelationshipType.ManyToMany;
        }

        /// <summary>
        /// Initializes a 1:N (one-to-many) relationship type.
        /// </summary>
        /// <param name="entity1Attribute">The attribute name on the first entity (the "one" side).</param>
        /// <param name="entity2Attribute">The attribute name on the second entity (the "many" side).</param>
        /// <param name="entity1LogicalName">The logical name of the first entity.</param>
        /// <param name="entity2LogicalName">The logical name of the second entity.</param>
        public XrmFakedRelationship(string entity1Attribute, string entity2Attribute, string entity1LogicalName, string entity2LogicalName)
        {
            Entity1Attribute = entity1Attribute;
            Entity2Attribute = entity2Attribute;
            Entity1LogicalName = entity1LogicalName;
            Entity2LogicalName = entity2LogicalName;
            RelationshipType = enmFakeRelationshipType.OneToMany;
        }
    }
}

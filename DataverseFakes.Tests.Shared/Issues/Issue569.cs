using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.Issues
{
    /// <summary>
    /// GitHub Issue #569: Query on associatedentitytypecode should work with both Entity Name and ObjectTypeCode
    ///
    /// In Dynamics 365, the associatedentitytypecode attribute on document templates (and similar entities)
    /// accepts two types of input:
    /// - String values (entity names like "salesorder")
    /// - Integer values (ObjectTypeCode like 1088)
    ///
    /// FakeXrmEasy fails with integer inputs because the query engine calls ToLowerInvariant() on an integer,
    /// producing: "Method 'System.String ToLowerInvariant()' declared on type 'System.String' cannot be called
    /// with instance of type 'System.Int32'"
    /// </summary>
    public class Issue569 : FakeXrmEasyTestsBase
    {
        private readonly Entity _documentTemplateWithSalesOrderTypeCode;
        private readonly Entity _documentTemplateWithOrderTypeCode;
        private readonly int _salesOrderTypeCode = 1088;

        public Issue569()
        {
            // Create document template with integer ObjectTypeCode (matching salesorder)
            _documentTemplateWithSalesOrderTypeCode = new Entity("documenttemplate")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Sales Order Template",
                ["associatedentitytypecode"] = "salesorder" // Could also be set as integer 1088
            };

            // Create document template with a different entity type
            _documentTemplateWithOrderTypeCode = new Entity("documenttemplate")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Order Template",
                ["associatedentitytypecode"] = "order"
            };
        }

        /// <summary>
        /// This test passes - querying with a string entity name works correctly.
        /// </summary>
        [Fact]
        public void Should_query_associatedentitytypecode_with_string_entity_name()
        {
            _context.Initialize(new List<Entity>()
            {
                _documentTemplateWithSalesOrderTypeCode,
                _documentTemplateWithOrderTypeCode
            });

            // Query using entity name as string - this works
            var query = new QueryExpression("documenttemplate")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("associatedentitytypecode", ConditionOperator.Equal, "salesorder");

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_documentTemplateWithSalesOrderTypeCode.Id, results[0].Id);
        }

        /// <summary>
        /// This test fails in affected versions - querying with an integer ObjectTypeCode
        /// throws: "Method 'System.String ToLowerInvariant()' declared on type 'System.String'
        /// cannot be called with instance of type 'System.Int32'"
        ///
        /// The issue is in GetAppropiateCastExpressionBasedOnStringAndType and GetAppropiateTypedValue methods
        /// which call GetCaseInsensitiveExpression (which calls ToLowerInvariant) even when the comparison
        /// value is an integer.
        /// </summary>
        [Fact]
        public void Should_query_associatedentitytypecode_with_integer_objecttypecode()
        {
            // Setup: Create entity with integer ObjectTypeCode stored
            var documentTemplateWithIntegerTypeCode = new Entity("documenttemplate")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Sales Order Template with Int",
                ["associatedentitytypecode"] = _salesOrderTypeCode // Store as integer
            };

            _context.Initialize(new List<Entity>()
            {
                documentTemplateWithIntegerTypeCode,
                _documentTemplateWithOrderTypeCode
            });

            // Query using ObjectTypeCode as integer - this fails in affected versions
            var query = new QueryExpression("documenttemplate")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("associatedentitytypecode", ConditionOperator.Equal, _salesOrderTypeCode);

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(documentTemplateWithIntegerTypeCode.Id, results[0].Id);
        }

        /// <summary>
        /// This test verifies that querying with integer value against string-stored attribute also works.
        /// In Dynamics 365, this would work because the system knows to convert ObjectTypeCode to entity name.
        /// In FakeXrmEasy, without metadata, this is trickier - but the basic case should not throw.
        /// </summary>
        [Fact]
        public void Should_not_throw_when_querying_string_attribute_with_integer_value()
        {
            _context.Initialize(new List<Entity>()
            {
                _documentTemplateWithSalesOrderTypeCode,
                _documentTemplateWithOrderTypeCode
            });

            // Query using integer against string-stored value - should not throw
            var query = new QueryExpression("documenttemplate")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("associatedentitytypecode", ConditionOperator.Equal, _salesOrderTypeCode);

            // This should not throw an exception
            var exception = Record.Exception(() => _service.RetrieveMultiple(query));

            // Note: The result may be empty because 1088 != "salesorder" without metadata mapping,
            // but the key point is it should not throw a ToLowerInvariant type mismatch error.
            Assert.Null(exception);
        }

        /// <summary>
        /// Test with In operator using integer values - this also exercises the same code path.
        /// </summary>
        [Fact]
        public void Should_query_with_in_operator_using_integer_values()
        {
            var documentTemplateWithIntegerTypeCode = new Entity("documenttemplate")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Sales Order Template with Int",
                ["associatedentitytypecode"] = _salesOrderTypeCode
            };

            _context.Initialize(new List<Entity>()
            {
                documentTemplateWithIntegerTypeCode
            });

            var query = new QueryExpression("documenttemplate")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("associatedentitytypecode", ConditionOperator.In, _salesOrderTypeCode, 9999);

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(documentTemplateWithIntegerTypeCode.Id, results[0].Id);
        }
    }
}

using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.Issues
{
    /// <summary>
    /// GitHub Issue #258: GUID comparison type validation
    ///
    /// This test suite verifies type compatibility validation for condition expressions.
    ///
    /// Key behaviors:
    /// - GUID-to-GUID comparisons work correctly
    /// - EntityReference-to-EntityReference comparisons work correctly
    /// - GUID-to-EntityReference (comparing lookup field with GUID value) IS ALLOWED
    ///   because the SDK LINQ provider and FetchXML commonly use GUIDs for lookup comparisons
    /// - EntityReference-to-GUID (comparing GUID field with EntityReference value) throws an exception
    ///   because this is a true type mismatch - a GUID field expects a GUID, not an EntityReference
    /// </summary>
    public class Issue258 : FakeXrmEasyTestsBase
    {
        private readonly Guid _productId;
        private readonly Guid _salesOrderDetailId;
        private readonly Entity _salesOrderDetail;

        public Issue258()
        {
            // Set up the ProxyTypesAssembly to enable early-bound type resolution
            _context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            _productId = Guid.NewGuid();
            _salesOrderDetailId = Guid.NewGuid();

            // Create a SalesOrderDetail with an EntityReference field (ProductId)
            _salesOrderDetail = new SalesOrderDetail
            {
                Id = _salesOrderDetailId,
                SalesOrderDetailId = _salesOrderDetailId,
                ProductId = new EntityReference(Product.EntityLogicalName, _productId)
            };
        }

        /// <summary>
        /// Verifies that GUID-to-GUID comparison works correctly.
        /// The SalesOrderDetailId field is a GUID type, so comparing it to a GUID value should succeed.
        /// </summary>
        [Fact]
        public void When_comparing_guid_field_to_guid_value_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("salesorderdetailid", ConditionOperator.Equal, _salesOrderDetailId);

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that EntityReference-to-EntityReference comparison works correctly.
        /// The ProductId field is an EntityReference type, so comparing it to an EntityReference value should succeed.
        /// </summary>
        [Fact]
        public void When_comparing_entityreference_field_to_entityreference_value_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("productid", ConditionOperator.Equal, new EntityReference(Product.EntityLogicalName, _productId));

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that comparing an EntityReference field to a raw GUID value succeeds.
        /// This is the common pattern used by LINQ providers and FetchXML where lookup fields
        /// are compared using the GUID (the Id of the EntityReference).
        /// </summary>
        [Fact]
        public void When_comparing_entityreference_field_to_guid_value_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // This should succeed - comparing lookup field with GUID is common and allowed
            query.Criteria.AddCondition("productid", ConditionOperator.Equal, _productId);

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that comparing a GUID field to an EntityReference value throws a FaultException.
        /// This is a true type mismatch - a GUID field (like a primary key) expects a simple GUID,
        /// not an EntityReference which includes additional information like entity name.
        /// </summary>
        [Fact]
        public void When_comparing_guid_field_to_entityreference_value_should_throw_fault_exception()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // This is the problematic case: SalesOrderDetailId is GUID but we're passing an EntityReference
            query.Criteria.AddCondition("salesorderdetailid", ConditionOperator.Equal,
                new EntityReference(SalesOrderDetail.EntityLogicalName, _salesOrderDetailId));

            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(
                () => _service.RetrieveMultiple(query));

            Assert.Contains("salesorderdetailid", exception.Message.ToLower());
            Assert.Contains("entityreference", exception.Message.ToLower());
            Assert.Contains("guid", exception.Message.ToLower());
        }

        /// <summary>
        /// Verifies that the NotEqual operator works correctly with EntityReference field and GUID value.
        /// </summary>
        [Fact]
        public void When_using_notequal_with_entityreference_field_and_guid_value_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // NotEqual with GUID value should work
            query.Criteria.AddCondition("productid", ConditionOperator.NotEqual, Guid.NewGuid());

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that the In operator works correctly with EntityReference field and GUID values.
        /// </summary>
        [Fact]
        public void When_using_in_operator_with_entityreference_field_and_guid_values_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // Passing GUID values to an EntityReference field should work
            query.Criteria.AddCondition("productid", ConditionOperator.In, _productId, Guid.NewGuid());

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that the In operator throws exception when passing EntityReference to GUID field.
        /// </summary>
        [Fact]
        public void When_using_in_operator_with_guid_field_and_entityreference_values_should_throw_fault_exception()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // Passing EntityReference values to a GUID field should fail
            query.Criteria.AddCondition("salesorderdetailid", ConditionOperator.In,
                new EntityReference(SalesOrderDetail.EntityLogicalName, _salesOrderDetailId));

            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(
                () => _service.RetrieveMultiple(query));

            Assert.Contains("salesorderdetailid", exception.Message.ToLower());
        }

        /// <summary>
        /// Verifies that the In operator works correctly with EntityReference values.
        /// </summary>
        [Fact]
        public void When_using_in_operator_with_entityreference_field_and_entityreference_values_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // Passing EntityReference values to an EntityReference field
            query.Criteria.AddCondition("productid", ConditionOperator.In,
                new EntityReference(Product.EntityLogicalName, _productId),
                new EntityReference(Product.EntityLogicalName, Guid.NewGuid()));

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that comparing a GUID field to a string representation of a GUID still works.
        /// String GUID values are allowed in Dataverse and should be parsed correctly.
        /// </summary>
        [Fact]
        public void When_comparing_guid_field_to_string_guid_value_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // String GUID values are parsed and should work
            query.Criteria.AddCondition("salesorderdetailid", ConditionOperator.Equal, _salesOrderDetailId.ToString());

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that comparing an EntityReference field to a string representation of a GUID still works.
        /// String GUID values are allowed in Dataverse and should be parsed correctly.
        /// </summary>
        [Fact]
        public void When_comparing_entityreference_field_to_string_guid_value_should_succeed()
        {
            _context.Initialize(new List<Entity> { _salesOrderDetail });

            var query = new QueryExpression(SalesOrderDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            // String GUID values are parsed and should work for EntityReference fields
            query.Criteria.AddCondition("productid", ConditionOperator.Equal, _productId.ToString());

            var results = _service.RetrieveMultiple(query).Entities;

            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }

        /// <summary>
        /// Verifies that late-bound entities (without ProxyTypesAssembly) still allow GUID comparisons
        /// because type validation requires metadata which is not available for late-bound entities.
        /// </summary>
        [Fact]
        public void When_using_late_bound_entities_guid_comparison_should_work()
        {
            // Create a new context without ProxyTypesAssembly
            var lateBoundContext = new XrmFakedContext();
            var lateBoundService = lateBoundContext.GetOrganizationService();

            var entity = new Entity("salesorderdetail")
            {
                Id = _salesOrderDetailId,
                ["salesorderdetailid"] = _salesOrderDetailId,
                ["productid"] = new EntityReference("product", _productId)
            };

            lateBoundContext.Initialize(new List<Entity> { entity });

            // Without type information, the comparison should be allowed (late-bound behavior)
            var query = new QueryExpression("salesorderdetail")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("productid", ConditionOperator.Equal, _productId);

            // This should work in late-bound mode because we don't have type information
            var results = lateBoundService.RetrieveMultiple(query).Entities;

            // The query executes and should find results
            Assert.Single(results);
            Assert.Equal(_salesOrderDetailId, results[0].Id);
        }
    }
}

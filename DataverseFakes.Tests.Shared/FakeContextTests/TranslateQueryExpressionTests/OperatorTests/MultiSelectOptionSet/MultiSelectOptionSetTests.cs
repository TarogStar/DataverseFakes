#if FAKE_XRM_EASY_9
using System.Linq;
using System.ServiceModel;
using Crm;
using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.TranslateQueryExpressionTests.OperatorTests.MultiSelectOptionSet
{
    public class MultiSelectOptionSetTests
    {
        [Fact]
        public void When_executing_a_query_expression_equal_operator_returns_exact_matches_for_int_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.Equal, 2);

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_equal_operator_returns_exact_matches_for_string_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.Equal, "2");

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_equal_operator_throws_exception_for_optionsetvalue_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.Equal, new OptionSetValue(2));

            Assert.Throws<FaultException>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_equal_operator_throws_exception_for_optionsetvaluecollection_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.Equal, new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) });

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_equal_operator_returns_exact_matches_for_single_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.Equal, new[] { 2 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_equal_operator_throws_exception_for_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.Equal, new[] { 1, 2, 3 });

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_equal_operator_throws_exception_for_string_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.Equal, new[] { "1", "2", "3" });

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_notequal_operator_excludes_exact_matches_for_int_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.NotEqual, 2);

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(4, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string) != "2"));
        }

        [Fact]
        public void When_executing_a_query_expression_notequal_operator_excludes_exact_matches_for_single_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.NotEqual, new[] { 2 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(4, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string) != "2"));
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_int_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, 2);

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_string_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, "2");

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_throws_exception_for_optionsetvalue_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, new OptionSetValue(2));

            Assert.Throws<FaultException>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_throws_exception_for_optionsetvaluecollection_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) });

            Assert.Throws<FaultException>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_single_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, new[] { 2 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, new[] { 2, 3 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2,3", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_out_of_order_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, new[] { 3, 1, 2 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("1,2,3", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_out_of_order_int_params_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, 3, 1, 2);

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("1,2,3", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_string_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, new[] { "2", "3" });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2,3", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_string_params_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, "2", "3");

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2,3", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_in_operator_returns_exact_matches_for_out_of_order_string_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.In, new[] { "3", "2" });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("2,3", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_notin_operator_excludes_exact_matches_for_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.NotIn, new[] { 2, 3 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(4, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string) != "2,3"));
        }


        [Fact]
        public void When_executing_a_query_expression_notin_operator_excludes_exact_matches_for_out_of_order_string_params_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.NotIn, "3", "2");

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(4, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string) != "2,3"));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_int_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, 1);

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("1")));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_string_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, "1");

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("1")));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_throws_exception_for_optionsetvalue_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, new OptionSetValue(2));

            Assert.Throws<FaultException>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_throws_exception_for_optionsetvaluecollection_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) });

            Assert.Throws<FaultException>(() => service.RetrieveMultiple(qe));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_single_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, new[] { 1 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("1")));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, new[] { 1, 3 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(3, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("1") || (e["firstname"] as string).Contains("3")));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_int_params_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, 1, 3);

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(3, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("1") || (e["firstname"] as string).Contains("3")));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_out_of_order_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, new[] { 3, 2 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(4, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("3") || (e["firstname"] as string).Contains("2")));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_string_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, new[] { "1", "3" });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(3, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("1") || (e["firstname"] as string).Contains("3")));
        }

        [Fact]
        public void When_executing_a_query_expression_containvalues_operator_returns_partial_matches_for_out_of_order_string_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, new[] { "3", "1" });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(3, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string).Contains("3") || (e["firstname"] as string).Contains("1")));
        }

        [Fact]
        public void When_executing_a_query_expression_doesnotcontainvalues_operator_excludes_partial_matches_for_int_array_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.DoesNotContainValues, new[] { 2, 3 });

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Single(entities);
            Assert.Equal("null", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_doesnotcontainvalues_operator_excludes_partial_matches_for_out_of_order_string_params_right_hand_side()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "1,2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "1,2,3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "null" });

      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.DoesNotContainValues, "3", "1");

            var entities = service.RetrieveMultiple(qe).Entities;

            Assert.Equal(2, entities.Count);
            Assert.True(entities.All(e => !(e["firstname"] as string).Contains("3") && !(e["firstname"] as string).Contains("1")));
        }

        #region Issue #467 - Late-bound entity tests for ContainValues/DoesNotContainValues

        [Fact]
        public void When_executing_containvalues_with_late_bound_entity_should_return_matching_results()
        {
            // Arrange - Issue #467: NRE when using ContainValues with late-bound entities
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create late-bound entities (no early-bound types, no ProxyTypesAssembly)
            var entity1 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity1["firstname"] = "1,2";
            entity1["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) };
            service.Create(entity1);

            var entity2 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity2["firstname"] = "2";
            entity2["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(2) };
            service.Create(entity2);

            var entity3 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity3["firstname"] = "2,3";
            entity3["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) };
            service.Create(entity3);

            var entity4 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity4["firstname"] = "null";
            // No multi-select attribute
            service.Create(entity4);

      // Act
      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, 1);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert
            Assert.Single(entities);
            Assert.Equal("1,2", entities[0]["firstname"]);
        }

        [Fact]
        public void When_executing_containvalues_with_late_bound_entity_and_null_attribute_should_return_no_results()
        {
            // Arrange - Issue #467: NRE when attribute value is null
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create late-bound entity with null multi-select attribute
            var entity1 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity1["firstname"] = "has null multiselect";
            entity1["new_multiselectattribute"] = null;
            service.Create(entity1);

            var entity2 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity2["firstname"] = "no multiselect attribute";
            // Attribute not set at all
            service.Create(entity2);

      // Act - should not throw NRE
      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, 1);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - no results should match when attribute is null
            Assert.Empty(entities);
        }

        [Fact]
        public void When_executing_doesnotcontainvalues_with_late_bound_entity_should_return_correct_results()
        {
            // Arrange - Issue #467: NRE when using DoesNotContainValues with late-bound entities
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create late-bound entities
            var entity1 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity1["firstname"] = "1,2";
            entity1["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) };
            service.Create(entity1);

            var entity2 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity2["firstname"] = "2";
            entity2["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(2) };
            service.Create(entity2);

            var entity3 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity3["firstname"] = "3";
            entity3["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(3) };
            service.Create(entity3);

            var entity4 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity4["firstname"] = "null";
            // No multi-select attribute - should be included in DoesNotContainValues results
            service.Create(entity4);

      // Act
      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.DoesNotContainValues, 1);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - entities 2, 3, and 4 should be returned (those that don't contain value 1)
            Assert.Equal(3, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string) != "1,2"));
        }

        [Fact]
        public void When_executing_doesnotcontainvalues_with_late_bound_entity_and_null_attribute_should_include_in_results()
        {
            // Arrange - Issue #467: entities with null multi-select attribute should be included in DoesNotContainValues
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entity1 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity1["firstname"] = "has value";
            entity1["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(1) };
            service.Create(entity1);

            var entity2 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity2["firstname"] = "has null";
            entity2["new_multiselectattribute"] = null;
            service.Create(entity2);

            var entity3 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity3["firstname"] = "no attribute";
            // Attribute not set at all
            service.Create(entity3);

      // Act
      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.DoesNotContainValues, 1);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - entities with null/missing attribute should be included since they don't contain value 1
            Assert.Equal(2, entities.Count);
            Assert.True(entities.All(e => (e["firstname"] as string) != "has value"));
        }

        [Fact]
        public void When_executing_containvalues_with_late_bound_entity_multiple_values_should_work()
        {
            // Arrange - Issue #467: Testing with multiple values in ContainValues
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entity1 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity1["firstname"] = "1,2";
            entity1["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) };
            service.Create(entity1);

            var entity2 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity2["firstname"] = "2";
            entity2["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(2) };
            service.Create(entity2);

            var entity3 = new Entity("contact") { Id = System.Guid.NewGuid() };
            entity3["firstname"] = "3,4";
            entity3["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(3), new OptionSetValue(4) };
            service.Create(entity3);

      // Act - search for entities containing 1 or 3
      var qe = new QueryExpression("contact")
      {
        ColumnSet = new ColumnSet(new[] { "firstname" })
      };
      qe.Criteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, 1, 3);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - should return entities 1 and 3 (those containing either 1 or 3)
            Assert.Equal(2, entities.Count);
            Assert.Contains(entities, e => (e["firstname"] as string) == "1,2");
            Assert.Contains(entities, e => (e["firstname"] as string) == "3,4");
        }

        #endregion

        #region Issue #354 - OrderBy on MultiOptionSetValue

        [Fact]
        public void MultiOptionSetValue_OrderBy_Should_Work()
        {
            // Arrange - Issue #354: OrderBy on MultiOptionSetValue (OptionSetValueCollection) fields
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create contacts with different multi-select values
            service.Create(new Contact { FirstName = "B", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "A", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "C", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(3), new OptionSetValue(4) } });
            service.Create(new Contact { FirstName = "D", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1) } });

            // Act - order by multi-select attribute ascending
            var qe = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(new[] { "firstname", "new_multiselectattribute" })
            };
            qe.AddOrder("new_multiselectattribute", OrderType.Ascending);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - should be ordered by first value in sorted collection, then by count
            Assert.Equal(4, entities.Count);
            // D has [1], A has [1,2], B has [2,3], C has [3,4]
            Assert.Equal("D", entities[0]["firstname"]); // [1] - smallest first value, smallest count
            Assert.Equal("A", entities[1]["firstname"]); // [1,2] - same first value as D, but more items
            Assert.Equal("B", entities[2]["firstname"]); // [2,3] - first value is 2
            Assert.Equal("C", entities[3]["firstname"]); // [3,4] - first value is 3
        }

        [Fact]
        public void MultiOptionSetValue_OrderBy_Descending_Should_Work()
        {
            // Arrange - Issue #354: OrderBy descending on MultiOptionSetValue
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { FirstName = "B", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } });
            service.Create(new Contact { FirstName = "A", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "C", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(3), new OptionSetValue(4) } });
            service.Create(new Contact { FirstName = "D", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1) } });

            // Act - order by multi-select attribute descending
            var qe = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(new[] { "firstname", "new_multiselectattribute" })
            };
            qe.AddOrder("new_multiselectattribute", OrderType.Descending);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - should be in reverse order
            Assert.Equal(4, entities.Count);
            Assert.Equal("C", entities[0]["firstname"]); // [3,4] - largest first value
            Assert.Equal("B", entities[1]["firstname"]); // [2,3]
            Assert.Equal("A", entities[2]["firstname"]); // [1,2] - same first value as D, but more items
            Assert.Equal("D", entities[3]["firstname"]); // [1] - smallest
        }

        [Fact]
        public void MultiOptionSetValue_OrderBy_With_Null_Values_Should_Work()
        {
            // Arrange - Issue #354: Null values should be handled in OrderBy
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { FirstName = "A", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(2) } });
            service.Create(new Contact { FirstName = "B" }); // null multi-select
            service.Create(new Contact { FirstName = "C", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1) } });

            // Act
            var qe = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(new[] { "firstname", "new_multiselectattribute" })
            };
            qe.AddOrder("new_multiselectattribute", OrderType.Ascending);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - null values come first in ascending order
            Assert.Equal(3, entities.Count);
            Assert.Equal("B", entities[0]["firstname"]); // null
            Assert.Equal("C", entities[1]["firstname"]); // [1]
            Assert.Equal("A", entities[2]["firstname"]); // [2]
        }

        #endregion

        #region Issue #354 - FormattedValues for MultiOptionSetValue

        [Fact]
        public void MultiOptionSetValue_FormattedValues_Should_Be_Populated_Without_Metadata()
        {
            // Arrange - Issue #354: FormattedValues should show numeric values when no metadata
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { FirstName = "Test", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2), new OptionSetValue(3) } });

            // Act
            var qe = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(new[] { "firstname", "new_multiselectattribute" })
            };

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - FormattedValues should contain the attribute with numeric values
            Assert.Single(entities);
            Assert.True(entities[0].FormattedValues.ContainsKey("new_multiselectattribute"));
            Assert.Equal("1; 2; 3", entities[0].FormattedValues["new_multiselectattribute"]);
        }

        [Fact]
        public void MultiOptionSetValue_FormattedValues_Should_Be_Populated_With_Metadata()
        {
            // Arrange - Issue #354: FormattedValues should show labels when metadata is available
            var context = new XrmFakedContext();

            // Set up metadata with option labels
            var multiSelectAttr = new MultiSelectPicklistAttributeMetadata()
            {
                LogicalName = "new_multiselectattribute"
            };
            multiSelectAttr.OptionSet = new OptionSetMetadata(
                new OptionMetadataCollection(new[]
                {
                    new OptionMetadata(new Label("Option A", 1033), 1),
                    new OptionMetadata(new Label("Option B", 1033), 2),
                    new OptionMetadata(new Label("Option C", 1033), 3)
                }));

            var entityMetadata = new EntityMetadata { LogicalName = "contact" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[] { multiSelectAttr });

            context.InitializeMetadata(entityMetadata);

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "Test", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(3) } });

            // Act
            var qe = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(new[] { "firstname", "new_multiselectattribute" })
            };

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - FormattedValues should contain the attribute with labels from metadata
            Assert.Single(entities);
            Assert.True(entities[0].FormattedValues.ContainsKey("new_multiselectattribute"));
            Assert.Equal("Option A; Option C", entities[0].FormattedValues["new_multiselectattribute"]);
        }

        [Fact]
        public void MultiOptionSetValue_FormattedValues_Empty_Collection_Should_Not_Add_FormattedValue()
        {
            // Arrange - Issue #354: Empty collections should not add a formatted value
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            service.Create(new Contact { FirstName = "Test", new_MultiSelectAttribute = new OptionSetValueCollection() });

            // Act
            var qe = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(new[] { "firstname", "new_multiselectattribute" })
            };

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - FormattedValues should NOT contain the attribute for empty collection
            Assert.Single(entities);
            Assert.False(entities[0].FormattedValues.ContainsKey("new_multiselectattribute"));
        }

        #endregion

        #region Issue #354 - LinkedEntity queries with MultiOptionSetValue

        [Fact]
        public void MultiOptionSetValue_In_LinkedEntity_Should_Work()
        {
            // Arrange - Issue #354: LinkEntity queries with MultiOptionSetValue conditions
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create contacts with multi-select values
            var contact1 = new Contact { Id = System.Guid.NewGuid(), FirstName = "Contact1", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) } };
            var contact2 = new Contact { Id = System.Guid.NewGuid(), FirstName = "Contact2", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(3), new OptionSetValue(4) } };
            var contact3 = new Contact { Id = System.Guid.NewGuid(), FirstName = "Contact3", new_MultiSelectAttribute = new OptionSetValueCollection() { new OptionSetValue(1) } };

            service.Create(contact1);
            service.Create(contact2);
            service.Create(contact3);

            // Create accounts linked to contacts
            var account1 = new Account { Id = System.Guid.NewGuid(), Name = "Account1", PrimaryContactId = contact1.ToEntityReference() };
            var account2 = new Account { Id = System.Guid.NewGuid(), Name = "Account2", PrimaryContactId = contact2.ToEntityReference() };
            var account3 = new Account { Id = System.Guid.NewGuid(), Name = "Account3", PrimaryContactId = contact3.ToEntityReference() };

            service.Create(account1);
            service.Create(account2);
            service.Create(account3);

            // Act - query accounts with linked contact having multi-select value containing 1
            var qe = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(new[] { "name" })
            };
            var linkedContact = qe.AddLink("contact", "primarycontactid", "contactid");
            linkedContact.Columns.AddColumns("firstname", "new_multiselectattribute");
            linkedContact.LinkCriteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, 1);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - should return accounts linked to contacts with value 1
            Assert.Equal(2, entities.Count);
            Assert.Contains(entities, e => (e["name"] as string) == "Account1");
            Assert.Contains(entities, e => (e["name"] as string) == "Account3");
        }

        [Fact]
        public void MultiOptionSetValue_ContainValues_On_LinkedEntity_Should_Work()
        {
            // Arrange - Issue #354: ContainValues on LinkedEntity with multiple values
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create late-bound entities to test without early-bound types
            var contact1 = new Entity("contact") { Id = System.Guid.NewGuid() };
            contact1["firstname"] = "Contact1";
            contact1["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) };
            service.Create(contact1);

            var contact2 = new Entity("contact") { Id = System.Guid.NewGuid() };
            contact2["firstname"] = "Contact2";
            contact2["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(3), new OptionSetValue(4) };
            service.Create(contact2);

            var contact3 = new Entity("contact") { Id = System.Guid.NewGuid() };
            contact3["firstname"] = "Contact3";
            // No multi-select attribute (null)
            service.Create(contact3);

            var account1 = new Entity("account") { Id = System.Guid.NewGuid() };
            account1["name"] = "Account1";
            account1["primarycontactid"] = contact1.ToEntityReference();
            service.Create(account1);

            var account2 = new Entity("account") { Id = System.Guid.NewGuid() };
            account2["name"] = "Account2";
            account2["primarycontactid"] = contact2.ToEntityReference();
            service.Create(account2);

            var account3 = new Entity("account") { Id = System.Guid.NewGuid() };
            account3["name"] = "Account3";
            account3["primarycontactid"] = contact3.ToEntityReference();
            service.Create(account3);

            // Act - query accounts with linked contact having multi-select value containing 2 or 4
            var qe = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(new[] { "name" })
            };
            var linkedContact = qe.AddLink("contact", "primarycontactid", "contactid");
            linkedContact.LinkCriteria.AddCondition("new_multiselectattribute", ConditionOperator.ContainValues, 2, 4);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - should return Account1 (contact has 2) and Account2 (contact has 4)
            Assert.Equal(2, entities.Count);
            Assert.Contains(entities, e => (e["name"] as string) == "Account1");
            Assert.Contains(entities, e => (e["name"] as string) == "Account2");
        }

        [Fact]
        public void MultiOptionSetValue_DoesNotContainValues_On_LinkedEntity_Should_Work()
        {
            // Arrange - Issue #354: DoesNotContainValues on LinkedEntity
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var contact1 = new Entity("contact") { Id = System.Guid.NewGuid() };
            contact1["firstname"] = "Contact1";
            contact1["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(1), new OptionSetValue(2) };
            service.Create(contact1);

            var contact2 = new Entity("contact") { Id = System.Guid.NewGuid() };
            contact2["firstname"] = "Contact2";
            contact2["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(3), new OptionSetValue(4) };
            service.Create(contact2);

            var contact3 = new Entity("contact") { Id = System.Guid.NewGuid() };
            contact3["firstname"] = "Contact3";
            // No multi-select attribute (null)
            service.Create(contact3);

            var account1 = new Entity("account") { Id = System.Guid.NewGuid() };
            account1["name"] = "Account1";
            account1["primarycontactid"] = contact1.ToEntityReference();
            service.Create(account1);

            var account2 = new Entity("account") { Id = System.Guid.NewGuid() };
            account2["name"] = "Account2";
            account2["primarycontactid"] = contact2.ToEntityReference();
            service.Create(account2);

            var account3 = new Entity("account") { Id = System.Guid.NewGuid() };
            account3["name"] = "Account3";
            account3["primarycontactid"] = contact3.ToEntityReference();
            service.Create(account3);

            // Act - query accounts with linked contact NOT having multi-select value containing 1
            var qe = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(new[] { "name" })
            };
            var linkedContact = qe.AddLink("contact", "primarycontactid", "contactid");
            linkedContact.LinkCriteria.AddCondition("new_multiselectattribute", ConditionOperator.DoesNotContainValues, 1);

            var entities = service.RetrieveMultiple(qe).Entities;

            // Assert - should return Account2 (contact has 3,4) and Account3 (contact has null)
            Assert.Equal(2, entities.Count);
            Assert.Contains(entities, e => (e["name"] as string) == "Account2");
            Assert.Contains(entities, e => (e["name"] as string) == "Account3");
        }

        #endregion
    }
}
#endif
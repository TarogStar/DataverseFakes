using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.TranslateQueryExpressionTests.OperatorTests.Strings
{
    public class StringOperatorsTests
    {
        [Fact]
        public void When_executing_a_query_expression_begins_with_operator_is_case_insensitive()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "Jimmy" });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("firstname", ConditionOperator.BeginsWith, "jim");

            Assert.Single(service.RetrieveMultiple(qe).Entities);
        }

        [Fact]
        public void When_executing_a_query_expression_ends_with_operator_is_case_insensitive()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();
            service.Create(new Contact { FirstName = "JimmY" });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("firstname", ConditionOperator.EndsWith, "y");

            Assert.Single(service.RetrieveMultiple(qe).Entities);
        }

        [Fact]
        public void When_executing_a_query_expression_like_operator_is_case_insensitive()
        {
            var context = new XrmFakedContext { ProxyTypesAssembly = Assembly.GetExecutingAssembly() };
            var service = context.GetOrganizationService();

            service.Create(new Contact { FirstName = "Jimmy" });

            var qe = new QueryExpression("contact");
            qe.Criteria.AddCondition("firstname", ConditionOperator.Like, "JIM%");

            Assert.Single(service.RetrieveMultiple(qe).Entities);
        }

        [Fact]
        public void When_executing_a_query_expression_with_endswith_operator_right_result_is_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "Contact 1"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "Contact 2"; contact2["firstname"] = "First 2";

            context.Initialize(new List<Entity>() { contact1, contact2 });

      var qe = new QueryExpression
      {
        EntityName = "contact",
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression(LogicalOperator.And)
      };
      var condition = new ConditionExpression("fullname", ConditionOperator.EndsWith, "2");
            qe.Criteria.AddCondition(condition);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe).ToList();

            Assert.True(result.Count() == 1);
        }

        [Fact]
        public void When_executing_a_query_expression_with_beginswith_operator_right_result_is_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "1 Contact"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "2 Contact"; contact2["firstname"] = "First 2";

            context.Initialize(new List<Entity>() { contact1, contact2 });

      var qe = new QueryExpression
      {
        EntityName = "contact",
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression(LogicalOperator.And)
      };
      var condition = new ConditionExpression("fullname", ConditionOperator.BeginsWith, "2");
            qe.Criteria.AddCondition(condition);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe).ToList();

            Assert.True(result.Count() == 1);
        }

        [Fact]
        public void When_executing_a_query_expression_with_contains_operator_right_result_is_returned()
        {
            var context = new XrmFakedContext();
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() }; contact1["fullname"] = "1 Contact"; contact1["firstname"] = "First 1";
            var contact2 = new Entity("contact") { Id = Guid.NewGuid() }; contact2["fullname"] = "2 Contact"; contact2["firstname"] = "First 2";
            var contact3 = new Entity("contact") { Id = Guid.NewGuid() }; contact3["fullname"] = "Other"; contact3["firstname"] = "First 2";

            context.Initialize(new List<Entity>() { contact1, contact2, contact3 });

      var qe = new QueryExpression
      {
        EntityName = "contact",
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression(LogicalOperator.And)
      };
      var condition = new ConditionExpression("fullname", ConditionOperator.Contains, "Contact");
            qe.Criteria.AddCondition(condition);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(context, qe).ToList();

            Assert.True(result.Count() == 2);
        }

        [Fact]
        public void When_executing_a_query_expression_with_lessthan_operator_right_result_is_returned()
        {
            var ctx = new XrmFakedContext();
            var ct1 = new Contact() { Id = Guid.NewGuid(), NickName = "Al" };
            var ct2 = new Contact() { Id = Guid.NewGuid(), NickName = "Bob" };
            var ct3 = new Contact() { Id = Guid.NewGuid(), NickName = "Charlie" };

            ctx.Initialize(new[] { ct2, ct3, ct1 });

      var qe = new QueryExpression
      {
        EntityName = "contact",
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression(LogicalOperator.And)
      };
      var condition = new ConditionExpression("nickname", ConditionOperator.LessThan, "B");
            qe.Criteria.AddCondition(condition);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(ctx, qe).ToList();

            Assert.Single(result);
            Assert.Equal("Al", Assert.Single(result)["nickname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_with_lessthanorequal_operator_right_result_is_returned()
        {
            var ctx = new XrmFakedContext();
            var ct1 = new Contact() { Id = Guid.NewGuid(), NickName = "Al" };
            var ct2 = new Contact() { Id = Guid.NewGuid(), NickName = "Bob" };
            var ct3 = new Contact() { Id = Guid.NewGuid(), NickName = "Charlie" };

            ctx.Initialize(new[] { ct2, ct3, ct1 });

      var qe = new QueryExpression
      {
        EntityName = "contact",
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression(LogicalOperator.And)
      };
      var condition = new ConditionExpression("nickname", ConditionOperator.LessEqual, "Bob");
            qe.Criteria.AddCondition(condition);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(ctx, qe).ToList();

            Assert.Equal(2, result.Count);
            var nicknames = result.Select(e => (string)e["nickname"]).ToList();
            Assert.Contains("Bob", nicknames);
            Assert.Contains("Al", nicknames);
        }

        [Fact]
        public void When_executing_a_query_expression_with_greaterthan_operator_right_result_is_returned()
        {
            var ctx = new XrmFakedContext();
            var ct1 = new Contact() { Id = Guid.NewGuid(), NickName = "Al" };
            var ct2 = new Contact() { Id = Guid.NewGuid(), NickName = "Bob" };
            var ct3 = new Contact() { Id = Guid.NewGuid(), NickName = "Charlie" };

            ctx.Initialize(new[] { ct2, ct3, ct1 });

      var qe = new QueryExpression
      {
        EntityName = "contact",
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression(LogicalOperator.And)
      };
      var condition = new ConditionExpression("nickname", ConditionOperator.GreaterThan, "Bob");
            qe.Criteria.AddCondition(condition);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(ctx, qe).ToList();

            Assert.Single(result);
            Assert.Equal("Charlie", Assert.Single(result)["nickname"]);
        }

        [Fact]
        public void When_executing_a_query_expression_with_greaterthanorequal_operator_right_result_is_returned()
        {
            var ctx = new XrmFakedContext();
            var ct1 = new Contact() { Id = Guid.NewGuid(), NickName = "Al" };
            var ct2 = new Contact() { Id = Guid.NewGuid(), NickName = "Bob" };
            var ct3 = new Contact() { Id = Guid.NewGuid(), NickName = "Charlie" };

            ctx.Initialize(new[] { ct2, ct3, ct1 });

      var qe = new QueryExpression
      {
        EntityName = "contact",
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression(LogicalOperator.And)
      };
      var condition = new ConditionExpression("nickname", ConditionOperator.GreaterEqual, "Bob");
            qe.Criteria.AddCondition(condition);

            var result = XrmFakedContext.TranslateQueryExpressionToLinq(ctx, qe).ToList();

            Assert.Equal(2, result.Count);
            var nicknames = result.Select(e => (string)e["nickname"]).ToList();
            Assert.Contains("Bob", nicknames);
            Assert.Contains("Charlie", nicknames);
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Reflection;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FetchXml.OperatorTests.Strings
{
    public class StringOperatorTests
    {
        [Fact]
        public void FetchXml_Operator_Lt_Translation()
        {
      var ctx = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Contact))
      };

      var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='nickname' operator='lt' value='Bob' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var ct = new Contact();

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Single(query.Criteria.Conditions);
            Assert.Equal("nickname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.LessThan, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Bob", query.Criteria.Conditions[0].Values[0]);
        }

        [Fact]
        public void FetchXml_Operator_Lt_Execution()
        {
            var ctx = new XrmFakedContext();

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='nickname' />
                                        <filter type='and'>
                                            <condition attribute='nickname' operator='lt' value='C' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var ct1 = new Contact() { Id = Guid.NewGuid(), NickName = "Alice" };
            var ct2 = new Contact() { Id = Guid.NewGuid(), NickName = "Bob" };
            var ct3 = new Contact() { Id = Guid.NewGuid(), NickName = "Nati" };
            ctx.Initialize(new[] { ct1, ct2, ct3 });
            var service = ctx.GetOrganizationService();

            var collection = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, collection.Entities.Count);
            var nicknames = collection.Entities.Select(e => (string)e["nickname"]).ToList();
            Assert.Contains("Alice", nicknames);
            Assert.Contains("Bob", nicknames);
        }

        [Fact]
        public void FetchXml_Operator_Gt_Translation()
        {
      var ctx = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Contact))
      };

      var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='contactid' />
                                        <filter type='and'>
                                            <condition attribute='nickname' operator='gt' value='Bob' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var ct = new Contact();

            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

            Assert.True(query.Criteria != null);
            Assert.Single(query.Criteria.Conditions);
            Assert.Equal("nickname", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.GreaterThan, query.Criteria.Conditions[0].Operator);
            Assert.Equal("Bob", query.Criteria.Conditions[0].Values[0]);
        }

        [Fact]
        public void FetchXml_Operator_Gt_Execution()
        {
            var ctx = new XrmFakedContext();

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                    <attribute name='nickname' />
                                        <filter type='and'>
                                            <condition attribute='nickname' operator='gt' value='Alice' />
                                        </filter>
                                  </entity>
                            </fetch>";

            var ct1 = new Contact() { Id = Guid.NewGuid(), NickName = "Alice" };
            var ct2 = new Contact() { Id = Guid.NewGuid(), NickName = "Bob" };
            var ct3 = new Contact() { Id = Guid.NewGuid(), NickName = "Nati" };
            ctx.Initialize(new[] { ct1, ct2, ct3 });
            var service = ctx.GetOrganizationService();

            var collection = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, collection.Entities.Count);
            var nicknames = collection.Entities.Select(e => (string)e["nickname"]).ToList();
            Assert.Contains("Bob", nicknames);
            Assert.Contains("Nati", nicknames);
        }
    }
}

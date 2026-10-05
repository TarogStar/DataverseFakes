using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.ConcurrencyTests
{
    /// <summary>
    /// Concurrent queries against a single IOrganizationService must each get their own result.
    /// Regression: the faked RetrieveMultiple/Execute handed results from Invokes to ReturnsLazily
    /// through one shared variable, so a concurrent call could return another call's (often empty) result.
    /// </summary>
    public class ParallelQueryTests
    {
        private static readonly string[] EmptyEntityNames = { "contact", "new_matter", "new_bill" };

        private static QueryExpression BuildQuery(string entityName)
        {
            return new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet("new_thirdpartyid"),
                PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
            };
        }

        private static (XrmFakedContext context, Guid accountId) SeededContext()
        {
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();
            context.Initialize(new List<Entity>
            {
                new Entity("account", accountId) { ["new_thirdpartyid"] = "TP-1" }
            });
            return (context, accountId);
        }

        [Fact]
        public void When_retrieve_multiple_runs_in_parallel_with_queries_for_empty_entities_each_call_gets_its_own_result()
        {
            var (context, accountId) = SeededContext();
            var service = context.GetOrganizationService();
            var failures = new ConcurrentBag<string>();

            Parallel.For(0, 1000, i =>
            {
                var entityName = i % 4 == 0 ? "account" : EmptyEntityNames[i % 4 - 1];
                var result = service.RetrieveMultiple(BuildQuery(entityName));

                if (result.EntityName != entityName)
                    failures.Add($"{entityName}: got collection for '{result.EntityName}'");
                else if (entityName == "account" && (result.Entities.Count != 1 || result.Entities[0].Id != accountId))
                    failures.Add($"account: got {result.Entities.Count} rows");
                else if (entityName != "account" && result.Entities.Count != 0)
                    failures.Add($"{entityName}: got {result.Entities.Count} rows");
            });

            Assert.True(failures.IsEmpty, $"{failures.Count} mismatched results, e.g. {failures.FirstOrDefault()}");
        }

        [Fact]
        public void When_execute_runs_in_parallel_with_queries_for_empty_entities_each_call_gets_its_own_response()
        {
            var (context, _) = SeededContext();
            var service = context.GetOrganizationService();
            var failures = new ConcurrentBag<string>();

            Parallel.For(0, 1000, i =>
            {
                var entityName = i % 4 == 0 ? "account" : EmptyEntityNames[i % 4 - 1];
                var response = (RetrieveMultipleResponse)service.Execute(new RetrieveMultipleRequest { Query = BuildQuery(entityName) });
                var expected = entityName == "account" ? 1 : 0;

                if (response.EntityCollection.EntityName != entityName || response.EntityCollection.Entities.Count != expected)
                    failures.Add($"{entityName}: got {response.EntityCollection.Entities.Count} rows for '{response.EntityCollection.EntityName}'");
            });

            Assert.True(failures.IsEmpty, $"{failures.Count} mismatched results, e.g. {failures.FirstOrDefault()}");
        }
    }
}

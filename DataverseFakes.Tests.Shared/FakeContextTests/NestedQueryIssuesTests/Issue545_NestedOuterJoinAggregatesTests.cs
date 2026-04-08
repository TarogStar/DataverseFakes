using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.NestedQueryIssuesTests
{
    /// <summary>
    /// Tests for issue #545 - Aggregate values with nested outer joins.
    /// The issue is that aggregates on nested linked entities return zero
    /// when they should return actual values.
    /// </summary>
    public class Issue545_NestedOuterJoinAggregatesTests
    {
        [Fact]
        public void When_Aggregate_On_First_Level_Link_Should_Work()
        {
            // Baseline test - aggregates on first-level linked entities should work
            var context = new XrmFakedContext();

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Test Account";

            var task1 = new Entity("task") { Id = Guid.NewGuid() };
            task1["regardingobjectid"] = account.ToEntityReference();
            task1["subject"] = "Task 1";

            var task2 = new Entity("task") { Id = Guid.NewGuid() };
            task2["regardingobjectid"] = account.ToEntityReference();
            task2["subject"] = "Task 2";

            context.Initialize(new[] { account, task1, task2 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='accountid' alias='accountid' groupby='true' />
                        <link-entity name='task' from='regardingobjectid' to='accountid' alias='tasks' link-type='outer'>
                            <attribute name='activityid' alias='task_count' aggregate='count' />
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(results.Entities);
            var taskCount = results.Entities[0].GetAttributeValue<AliasedValue>("tasks.task_count")?.Value;
            Assert.Equal(2, taskCount);
        }

        [Fact]
        public void When_Aggregate_On_Nested_Link_Should_Return_Correct_Count()
        {
            // This is the problematic case - aggregates on nested (second-level) linked entities
            // Structure: Account -> Contact -> Task
            var context = new XrmFakedContext();

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Test Account";

            var contact = new Entity("contact") { Id = Guid.NewGuid() };
            contact["parentcustomerid"] = account.ToEntityReference();
            contact["fullname"] = "John Doe";

            // Tasks related to the contact (not directly to account)
            var task1 = new Entity("task") { Id = Guid.NewGuid() };
            task1["regardingobjectid"] = contact.ToEntityReference();
            task1["subject"] = "Contact Task 1";

            var task2 = new Entity("task") { Id = Guid.NewGuid() };
            task2["regardingobjectid"] = contact.ToEntityReference();
            task2["subject"] = "Contact Task 2";

            context.Initialize(new[] { account, contact, task1, task2 });

            var service = context.GetOrganizationService();

            // Query: Account -> Contact (outer) -> Task (outer) with count on tasks
            var fetchXml = @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='accountid' alias='accountid' groupby='true' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' alias='contacts' link-type='outer'>
                            <link-entity name='task' from='regardingobjectid' to='contactid' alias='contact_tasks' link-type='outer'>
                                <attribute name='activityid' alias='nested_task_count' aggregate='count' />
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(results.Entities);

            // This is the failing assertion - nested aggregate returns 0 instead of 2
            var nestedTaskCount = results.Entities[0].GetAttributeValue<AliasedValue>("contact_tasks.nested_task_count")?.Value;
            Assert.Equal(2, nestedTaskCount);
        }

        [Fact]
        public void When_Multiple_Accounts_With_Nested_Tasks_Should_Count_Correctly()
        {
            // Test with multiple accounts having different nested task counts
            var context = new XrmFakedContext();

            // Account 1 with 1 contact and 2 tasks
            var account1 = new Entity("account") { Id = Guid.NewGuid() };
            account1["name"] = "Account 1";

            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["parentcustomerid"] = account1.ToEntityReference();

            var task1a = new Entity("task") { Id = Guid.NewGuid() };
            task1a["regardingobjectid"] = contact1.ToEntityReference();

            var task1b = new Entity("task") { Id = Guid.NewGuid() };
            task1b["regardingobjectid"] = contact1.ToEntityReference();

            // Account 2 with 1 contact and 3 tasks
            var account2 = new Entity("account") { Id = Guid.NewGuid() };
            account2["name"] = "Account 2";

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["parentcustomerid"] = account2.ToEntityReference();

            var task2a = new Entity("task") { Id = Guid.NewGuid() };
            task2a["regardingobjectid"] = contact2.ToEntityReference();

            var task2b = new Entity("task") { Id = Guid.NewGuid() };
            task2b["regardingobjectid"] = contact2.ToEntityReference();

            var task2c = new Entity("task") { Id = Guid.NewGuid() };
            task2c["regardingobjectid"] = contact2.ToEntityReference();

            context.Initialize(new[] { account1, account2, contact1, contact2, task1a, task1b, task2a, task2b, task2c });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='name' alias='account_name' groupby='true' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' alias='contacts' link-type='outer'>
                            <link-entity name='task' from='regardingobjectid' to='contactid' alias='contact_tasks' link-type='outer'>
                                <attribute name='activityid' alias='task_count' aggregate='count' />
                            </link-entity>
                        </link-entity>
                        <order alias='account_name' />
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Equal(2, results.Entities.Count);

            var account1Result = results.Entities.FirstOrDefault(e =>
                e.GetAttributeValue<AliasedValue>("account_name")?.Value?.ToString() == "Account 1");
            var account2Result = results.Entities.FirstOrDefault(e =>
                e.GetAttributeValue<AliasedValue>("account_name")?.Value?.ToString() == "Account 2");

            Assert.NotNull(account1Result);
            Assert.NotNull(account2Result);

            // These assertions should pass but currently fail (return 0)
            Assert.Equal(2, account1Result.GetAttributeValue<AliasedValue>("contact_tasks.task_count")?.Value);
            Assert.Equal(3, account2Result.GetAttributeValue<AliasedValue>("contact_tasks.task_count")?.Value);
        }

        [Fact]
        public void When_Sum_Aggregate_On_Nested_Link_Should_Return_Correct_Total()
        {
            // Test SUM aggregate on nested link entity
            var context = new XrmFakedContext();

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "Test Account";

            var opportunity1 = new Entity("opportunity") { Id = Guid.NewGuid() };
            opportunity1["parentaccountid"] = account.ToEntityReference();
            opportunity1["name"] = "Opp 1";

            var product1 = new Entity("opportunityproduct") { Id = Guid.NewGuid() };
            product1["opportunityid"] = opportunity1.ToEntityReference();
            product1["extendedamount"] = new Money(100m);

            var product2 = new Entity("opportunityproduct") { Id = Guid.NewGuid() };
            product2["opportunityid"] = opportunity1.ToEntityReference();
            product2["extendedamount"] = new Money(200m);

            context.Initialize(new[] { account, opportunity1, product1, product2 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='accountid' alias='accountid' groupby='true' />
                        <link-entity name='opportunity' from='parentaccountid' to='accountid' alias='opps' link-type='outer'>
                            <link-entity name='opportunityproduct' from='opportunityid' to='opportunityid' alias='products' link-type='outer'>
                                <attribute name='extendedamount' alias='total_amount' aggregate='sum' />
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(results.Entities);

            var totalAmount = results.Entities[0].GetAttributeValue<AliasedValue>("products.total_amount")?.Value as Money;
            Assert.NotNull(totalAmount);
            Assert.Equal(300m, totalAmount.Value);
        }

        [Fact]
        public void When_CountDistinct_On_Nested_Link_Should_Work()
        {
            // Test COUNT DISTINCT on nested link entity
            var context = new XrmFakedContext();

            var account = new Entity("account") { Id = Guid.NewGuid() };

            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["parentcustomerid"] = account.ToEntityReference();

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["parentcustomerid"] = account.ToEntityReference();

            // Tasks with some duplicate subjects
            var task1 = new Entity("task") { Id = Guid.NewGuid() };
            task1["regardingobjectid"] = contact1.ToEntityReference();
            task1["subject"] = "Follow up";

            var task2 = new Entity("task") { Id = Guid.NewGuid() };
            task2["regardingobjectid"] = contact1.ToEntityReference();
            task2["subject"] = "Follow up"; // Duplicate

            var task3 = new Entity("task") { Id = Guid.NewGuid() };
            task3["regardingobjectid"] = contact2.ToEntityReference();
            task3["subject"] = "Meeting";

            context.Initialize(new[] { account, contact1, contact2, task1, task2, task3 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch aggregate='true'>
                    <entity name='account'>
                        <attribute name='accountid' alias='accountid' groupby='true' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' alias='contacts' link-type='outer'>
                            <link-entity name='task' from='regardingobjectid' to='contactid' alias='tasks' link-type='outer'>
                                <attribute name='subject' alias='unique_subjects' aggregate='countcolumn' distinct='true' />
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(results.Entities);

            // Should be 2 distinct subjects: "Follow up" and "Meeting"
            var uniqueSubjects = results.Entities[0].GetAttributeValue<AliasedValue>("tasks.unique_subjects")?.Value;
            Assert.Equal(2, uniqueSubjects);
        }
    }
}

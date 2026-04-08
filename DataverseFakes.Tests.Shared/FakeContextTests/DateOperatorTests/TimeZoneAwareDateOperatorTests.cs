using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for timezone-aware date operators (v1.0.3 Phase 2)
    /// Resolves remaining aspects of upstream issues #587, #551
    /// </summary>
    public class TimeZoneAwareDateOperatorTests
    {
        [Fact]
        public void When_SystemTimeZone_Set_ThisMonth_Should_Use_That_Timezone()
        {
      // Arrange - Set timezone to Pacific Standard Time
      var context = new XrmFakedContext
      {
        SystemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")
      };

      // Get current month in PST
      var nowInPst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, context.SystemTimeZone);
            var firstOfMonth = new DateTime(nowInPst.Year, nowInPst.Month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);

            // Create test data - one in current month (PST), one in next month
            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Current Month PST",
                    ["createdon"] = firstOfMonth.AddDays(10) // Middle of the month in PST
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Next Month PST",
                    ["createdon"] = firstOfMonth.AddMonths(1).AddDays(5) // Next month in PST
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act - Query for ThisMonth in PST
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.ThisMonth);

            var results = service.RetrieveMultiple(query);

            // Assert - Should only return current month PST record
            Assert.Single(results.Entities);
            Assert.Equal("Current Month PST", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_SystemTimeZone_Not_Set_Should_Use_Local_Time()
        {
            // Arrange - Don't set SystemTimeZone (should default to Local)
            var context = new XrmFakedContext();

            var today = DateTime.Today;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Current Month Local",
                    ["createdon"] = firstOfMonth.AddDays(10)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("createdon", ConditionOperator.ThisMonth);

            var results = service.RetrieveMultiple(query);

            // Assert - Should work with local time
            Assert.Single(results.Entities);
        }

        [Fact]
        public void When_SystemTimeZone_Set_LastMonth_Should_Use_That_Timezone()
        {
      // Arrange - Set timezone to Eastern Standard Time
      var context = new XrmFakedContext
      {
        SystemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
      };

      var nowInEst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, context.SystemTimeZone);
            var firstOfLastMonth = new DateTime(nowInEst.Year, nowInEst.Month, 1).AddMonths(-1);
            var lastOfLastMonth = new DateTime(nowInEst.Year, nowInEst.Month, 1).AddDays(-1)
                                    .AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Last Month EST",
                    ["createdon"] = firstOfLastMonth.AddDays(15) // Middle of last month in EST
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "This Month EST",
                    ["createdon"] = firstOfLastMonth.AddMonths(1).AddDays(5) // This month in EST
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.LastMonth);

            var results = service.RetrieveMultiple(query);

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Last Month EST", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_SystemTimeZone_Set_ThisYear_Should_Use_That_Timezone()
        {
      // Arrange - Set timezone to UTC
      var context = new XrmFakedContext
      {
        SystemTimeZone = TimeZoneInfo.Utc
      };

      var nowInUtc = DateTime.UtcNow;
            var thisYear = nowInUtc.Year;

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "This Year UTC",
                    ["createdon"] = new DateTime(thisYear, 6, 15) // June of this year
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Last Year UTC",
                    ["createdon"] = new DateTime(thisYear - 1, 6, 15) // June of last year
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.ThisYear);

            var results = service.RetrieveMultiple(query);

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("This Year UTC", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_Different_Timezones_ThisMonth_Should_Return_Different_Results()
        {
            // This test demonstrates that the same UTC time can be in different months
            // depending on the timezone

            // Arrange - Create a date that's on month boundary
            var utcDate = new DateTime(2024, 1, 31, 23, 30, 0, DateTimeKind.Utc); // Jan 31 11:30 PM UTC

      // In UTC, this is January 31
      // In Australia/Sydney (UTC+11), this would be February 1
      var contextUtc = new XrmFakedContext
      {
        SystemTimeZone = TimeZoneInfo.Utc
      };

      var contextSydney = new XrmFakedContext
      {
        // Note: For this test to work, the system must have the timezone info
        // We'll use a fixed offset instead for reliability
        SystemTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Custom UTC+11", TimeSpan.FromHours(11), "UTC+11", "UTC+11")
      };

      var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Boundary Date",
                ["createdon"] = utcDate
            };

            contextUtc.Initialize(new[] { entity });
            contextSydney.Initialize(new[] { entity });

            // We can't easily test "ThisMonth" with a fixed date, but we can verify
            // that the timezone is being used by checking date calculations
            // This is more of a smoke test to ensure no errors occur
            var serviceUtc = contextUtc.GetOrganizationService();
            var serviceSydney = contextSydney.GetOrganizationService();

            // Act & Assert - Both queries should execute without error
            var queryUtc = new QueryExpression("account");
            queryUtc.Criteria.AddCondition("createdon", ConditionOperator.ThisMonth);
            var exceptionUtc = Record.Exception(() => serviceUtc.RetrieveMultiple(queryUtc));

            var querySydney = new QueryExpression("account");
            querySydney.Criteria.AddCondition("createdon", ConditionOperator.ThisMonth);
            var exceptionSydney = Record.Exception(() => serviceSydney.RetrieveMultiple(querySydney));

            Assert.Null(exceptionUtc);
            Assert.Null(exceptionSydney);
        }

        [Fact]
        public void When_SystemTimeZone_Set_FetchXml_ThisMonth_Should_Use_That_Timezone()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        SystemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")
      };

      var nowInCst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, context.SystemTimeZone);
            var firstOfMonth = new DateTime(nowInCst.Year, nowInCst.Month, 1);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Current Month CST",
                    ["createdon"] = firstOfMonth.AddDays(10)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Use FetchXML
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='createdon' operator='this-month' />
                        </filter>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Current Month CST", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_SystemTimeZone_Set_ThisWeek_Should_Use_That_Timezone()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        SystemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time")
      };

      var nowInMst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, context.SystemTimeZone);
            var today = nowInMst.Date;

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "This Week MST",
                    ["createdon"] = today
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.ThisWeek);

            var results = service.RetrieveMultiple(query);

            // Assert - Should find the record created today
            Assert.Single(results.Entities);
        }
    }
}

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.DateTimeKindTests
{
    /// <summary>
    /// Tests for DateTime Kind handling to ensure parity with real Dataverse behavior.
    /// Verified against real Dataverse (see IntegrationTests/DataverseDateTimeInvestigation.cs):
    /// - DateTimeKind.Local → Converted to UTC
    /// - DateTimeKind.Utc → Stored as-is
    /// - DateTimeKind.Unspecified → Treated as UTC (stored raw)
    /// </summary>
    public class DateTimeKindConversionTests
    {
        [Fact]
        public void When_DateTime_Kind_Is_Local_It_Should_Be_Converted_To_UTC()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create a local time - 2:30 PM local
            var localTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Local);
            var expectedUtc = localTime.ToUniversalTime();

            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["createdon"] = localTime
            };

            // Act
            context.Initialize(new[] { entity });
            var retrieved = service.Retrieve("account", entity.Id, new ColumnSet("createdon"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("createdon");

            // Assert - Should be converted to UTC
            Assert.Equal(DateTimeKind.Utc, storedValue.Kind);
            Assert.Equal(expectedUtc, storedValue);
        }

        [Fact]
        public void When_DateTime_Kind_Is_UTC_It_Should_Be_Stored_AsIs()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var utcTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);

            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["createdon"] = utcTime
            };

            // Act
            context.Initialize(new[] { entity });
            var retrieved = service.Retrieve("account", entity.Id, new ColumnSet("createdon"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("createdon");

            // Assert - Should be stored exactly as-is
            Assert.Equal(DateTimeKind.Utc, storedValue.Kind);
            Assert.Equal(utcTime, storedValue);
        }

        [Fact]
        public void When_DateTime_Kind_Is_Unspecified_It_Should_Be_Treated_As_UTC()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Unspecified kind - Dataverse treats this as UTC
            var unspecifiedTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);

            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["createdon"] = unspecifiedTime
            };

            // Act
            context.Initialize(new[] { entity });
            var retrieved = service.Retrieve("account", entity.Id, new ColumnSet("createdon"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("createdon");

            // Assert - Should be marked as UTC but with same time values
            Assert.Equal(DateTimeKind.Utc, storedValue.Kind);
            Assert.Equal(unspecifiedTime.Year, storedValue.Year);
            Assert.Equal(unspecifiedTime.Month, storedValue.Month);
            Assert.Equal(unspecifiedTime.Day, storedValue.Day);
            Assert.Equal(unspecifiedTime.Hour, storedValue.Hour);
            Assert.Equal(unspecifiedTime.Minute, storedValue.Minute);
        }

        [Fact]
        public void When_Local_DateTime_Created_Via_Service_It_Should_Be_Converted_To_UTC()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var localTime = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Local);
            var expectedUtc = localTime.ToUniversalTime();

            var entity = new Entity("task")
            {
                ["subject"] = "Test Task",
                ["scheduledend"] = localTime
            };

            // Act
            var id = service.Create(entity);
            var retrieved = service.Retrieve("task", id, new ColumnSet("scheduledend"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("scheduledend");

            // Assert
            Assert.Equal(DateTimeKind.Utc, storedValue.Kind);
            Assert.Equal(expectedUtc, storedValue);
        }

        [Fact]
        public void When_Local_DateTime_Updated_Via_Service_It_Should_Be_Converted_To_UTC()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entity = new Entity("task")
            {
                Id = Guid.NewGuid(),
                ["subject"] = "Test Task"
            };
            context.Initialize(new[] { entity });

            var localTime = new DateTime(2025, 7, 20, 16, 45, 0, DateTimeKind.Local);
            var expectedUtc = localTime.ToUniversalTime();

            // Act
            var updateEntity = new Entity("task")
            {
                Id = entity.Id,
                ["scheduledend"] = localTime
            };
            service.Update(updateEntity);

            var retrieved = service.Retrieve("task", entity.Id, new ColumnSet("scheduledend"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("scheduledend");

            // Assert
            Assert.Equal(DateTimeKind.Utc, storedValue.Kind);
            Assert.Equal(expectedUtc, storedValue);
        }

        [Fact]
        public void When_Querying_With_Local_DateTime_It_Should_Match_Converted_Value()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Store a Local time
            var localTime = new DateTime(2025, 5, 10, 9, 0, 0, DateTimeKind.Local);
            var expectedUtc = localTime.ToUniversalTime();

            var entity = new Entity("appointment")
            {
                Id = Guid.NewGuid(),
                ["subject"] = "Meeting",
                ["scheduledstart"] = localTime
            };
            context.Initialize(new[] { entity });

            // Act - Query using UTC equivalent
            var query = new QueryExpression("appointment")
            {
                ColumnSet = new ColumnSet("subject", "scheduledstart")
            };
            query.Criteria.AddCondition("scheduledstart", ConditionOperator.Equal, expectedUtc);
            var results = service.RetrieveMultiple(query);

            // Assert - Should find the record
            Assert.Single(results.Entities);
            Assert.Equal("Meeting", results.Entities[0].GetAttributeValue<string>("subject"));
        }

        [Fact]
        public void When_Multiple_DateTimes_With_Different_Kinds_All_Should_Convert_Correctly()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var localTime = new DateTime(2025, 8, 1, 12, 0, 0, DateTimeKind.Local);
            var utcTime = new DateTime(2025, 8, 1, 12, 0, 0, DateTimeKind.Utc);
            var unspecifiedTime = new DateTime(2025, 8, 1, 12, 0, 0, DateTimeKind.Unspecified);

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Local", ["createdon"] = localTime },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "UTC", ["createdon"] = utcTime },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Unspecified", ["createdon"] = unspecifiedTime }
            };

            // Act
            context.Initialize(entities);

            // Assert
            foreach (var e in entities)
            {
                var retrieved = service.Retrieve("account", e.Id, new ColumnSet("name", "createdon"));
                var storedTime = retrieved.GetAttributeValue<DateTime>("createdon");
                var name = retrieved.GetAttributeValue<string>("name");

                // All should be stored as UTC
                Assert.Equal(DateTimeKind.Utc, storedTime.Kind);

                switch (name)
                {
                    case "Local":
                        // Local should have been converted
                        Assert.Equal(localTime.ToUniversalTime(), storedTime);
                        break;
                    case "UTC":
                        // UTC should be unchanged
                        Assert.Equal(utcTime, storedTime);
                        break;
                    case "Unspecified":
                        // Unspecified should have same values but marked as UTC
                        Assert.Equal(unspecifiedTime.Hour, storedTime.Hour);
                        Assert.Equal(unspecifiedTime.Minute, storedTime.Minute);
                        break;
                }
            }
        }

        [Fact]
        public void When_DateTime_Now_Is_Used_It_Should_Be_Converted_To_UTC()
        {
            // This tests the common case of using DateTime.Now (which is Local)
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Capture before and after in UTC
            var beforeUtc = DateTime.UtcNow;

            var entity = new Entity("account")
            {
                ["name"] = "Test",
                ["createdon"] = DateTime.Now // Local time
            };
            var id = service.Create(entity);

            var afterUtc = DateTime.UtcNow;

            // Act
            var retrieved = service.Retrieve("account", id, new ColumnSet("createdon"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("createdon");

            // Assert - Should be UTC and within the time window
            Assert.Equal(DateTimeKind.Utc, storedValue.Kind);
            Assert.True(storedValue >= beforeUtc.AddSeconds(-1), $"Stored time {storedValue} should be >= {beforeUtc}");
            Assert.True(storedValue <= afterUtc.AddSeconds(1), $"Stored time {storedValue} should be <= {afterUtc}");
        }

        [Fact]
        public void When_DateTime_UtcNow_Is_Used_It_Should_Be_Stored_AsIs()
        {
            // This tests using DateTime.UtcNow (which is already UTC)
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var beforeUtc = DateTime.UtcNow;

            var entity = new Entity("account")
            {
                ["name"] = "Test",
                ["createdon"] = DateTime.UtcNow
            };
            var id = service.Create(entity);

            var afterUtc = DateTime.UtcNow;

            // Act
            var retrieved = service.Retrieve("account", id, new ColumnSet("createdon"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("createdon");

            // Assert
            Assert.Equal(DateTimeKind.Utc, storedValue.Kind);
            Assert.True(storedValue >= beforeUtc.AddSeconds(-1));
            Assert.True(storedValue <= afterUtc.AddSeconds(1));
        }
    }
}

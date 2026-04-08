using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.DateOperatorTests
{
    /// <summary>
    /// Tests for date range operator fixes (v1.0.3)
    /// Resolves upstream issues #588, #587, #551, #543
    /// </summary>
    public class DateRangeOperatorTests
    {
        [Fact]
        public void When_ThisMonth_Operator_Should_Include_Full_End_Day()
        {
            // Arrange
            var context = new XrmFakedContext();
            var today = DateTime.Today;
            var lastDayOfMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);

            // Create records throughout the month
            var entities = new List<Entity>();

            // First day
            entities.Add(new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "First Day",
                ["createdon"] = new DateTime(today.Year, today.Month, 1, 9, 0, 0)
            });

            // Last day at 11:59 PM (should be included!)
            entities.Add(new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Last Day Late",
                ["createdon"] = new DateTime(lastDayOfMonth.Year, lastDayOfMonth.Month, lastDayOfMonth.Day, 23, 59, 0)
            });

            // Next month (should NOT be included)
            entities.Add(new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Next Month",
                ["createdon"] = lastDayOfMonth.AddDays(1)
            });

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.ThisMonth);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "First Day");
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Last Day Late");
            Assert.DoesNotContain(results, r => r.GetAttributeValue<string>("name") == "Next Month");
        }

        [Fact]
        public void When_LastMonth_Operator_Should_Include_Full_End_Day()
        {
            // Arrange
            var context = new XrmFakedContext();
            var today = DateTime.Today;
            var firstDayLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var lastDayLastMonth = new DateTime(today.Year, today.Month, 1).AddDays(-1);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(firstDayLastMonth.Year, firstDayLastMonth.Month, 1, 10, 0, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(lastDayLastMonth.Year, lastDayLastMonth.Month, lastDayLastMonth.Day, 23, 30, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = today // This month, should not be included
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.LastMonth);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void When_ThisYear_Operator_Should_Include_Full_December_31()
        {
            // Arrange
            var context = new XrmFakedContext();
            var currentYear = DateTime.Today.Year;

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(currentYear, 1, 1, 0, 0, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(currentYear, 12, 31, 23, 59, 59)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(currentYear + 1, 1, 1, 0, 0, 0) // Next year
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.ThisYear);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void When_ThisWeek_Operator_Should_Include_Full_End_Day()
        {
            // Arrange
            var context = new XrmFakedContext();
            // Use UTC dates - Dataverse stores all dates as UTC
            var today = DateTime.UtcNow.Date;

            // Get this week's boundaries (specify UTC kind to avoid conversion)
            var firstDayOfWeek = DateTime.SpecifyKind(today.ToFirstDayOfDeltaWeek(), DateTimeKind.Utc);
            var lastDayOfWeek = DateTime.SpecifyKind(today.ToLastDayOfDeltaWeek(), DateTimeKind.Utc);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "First Day",
                    ["createdon"] = firstDayOfWeek.AddHours(10)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Last Day Evening",
                    ["createdon"] = lastDayOfWeek.AddHours(23).AddMinutes(30)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Next Week",
                    ["createdon"] = lastDayOfWeek.AddDays(2)
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.ThisWeek);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "First Day");
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Last Day Evening");
        }

        [Fact]
        public void When_Between_Operator_With_Dates_Should_Include_Full_End_Day()
        {
            // Arrange
            var context = new XrmFakedContext();
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Start Day",
                    ["createdon"] = new DateTime(2024, 1, 1, 0, 0, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "End Day Late",
                    ["createdon"] = new DateTime(2024, 1, 31, 23, 59, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "After Range",
                    ["createdon"] = new DateTime(2024, 2, 1, 0, 0, 0)
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.Between, startDate, endDate);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Start Day");
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "End Day Late");
        }

        [Fact]
        public void When_LastWeek_Operator_Should_Include_Full_End_Day()
        {
            // Arrange
            var context = new XrmFakedContext();
            // Use UTC dates - Dataverse stores all dates as UTC
            var today = DateTime.UtcNow.Date;

            var firstDayLastWeek = DateTime.SpecifyKind(today.ToFirstDayOfDeltaWeek(-1), DateTimeKind.Utc);
            var lastDayLastWeek = DateTime.SpecifyKind(today.ToLastDayOfDeltaWeek(-1), DateTimeKind.Utc);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = firstDayLastWeek.AddHours(10)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = lastDayLastWeek.AddHours(23)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = DateTime.SpecifyKind(today, DateTimeKind.Utc) // This week
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.LastWeek);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void When_NextMonth_Operator_Should_Include_Full_End_Day()
        {
            // Arrange
            var context = new XrmFakedContext();
            var today = DateTime.Today;
            var firstDayNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            var lastDayNextMonth = firstDayNextMonth.AddMonths(1).AddDays(-1);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = firstDayNextMonth.AddHours(10)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(lastDayNextMonth.Year, lastDayNextMonth.Month, lastDayNextMonth.Day, 23, 59, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = lastDayNextMonth.AddDays(2) // Month after next
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.NextMonth);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void When_LastYear_Operator_Should_Include_Full_December_31()
        {
            // Arrange
            var context = new XrmFakedContext();
            var lastYear = DateTime.Today.Year - 1;

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(lastYear, 1, 1, 0, 0, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(lastYear, 12, 31, 23, 59, 59)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(lastYear + 1, 1, 1, 0, 0, 0)
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.LastYear);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void When_InFiscalYear_Operator_Should_Include_Full_End_Day()
        {
            // Arrange
            var context = new XrmFakedContext();
            var fiscalYear = 2024;

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(2024, 4, 1, 0, 0, 0) // FY start
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(2025, 3, 31, 23, 59, 0) // FY end
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["createdon"] = new DateTime(2025, 4, 1, 0, 0, 0) // Next FY
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalYear, fiscalYear);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
        }
    }
}

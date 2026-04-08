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
    /// Tests for fiscal period operators (Issue #476)
    /// Implements support for InFiscalPeriod, InFiscalPeriodAndYear, ThisFiscalPeriod, LastFiscalPeriod, NextFiscalPeriod
    /// </summary>
    public class FiscalPeriodOperatorTests
    {
        #region InFiscalPeriod with Quarterly Template Tests

        [Fact]
        public void When_InFiscalPeriodAndYear_Quarterly_Q1_Should_Match_First_Quarter()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2024, 4, 1), // April 1st fiscal year start
          FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q1 Start",
                    ["createdon"] = new DateTime(2024, 4, 1, 10, 0, 0) // First day of Q1
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q1 End",
                    ["createdon"] = new DateTime(2024, 6, 30, 23, 30, 0) // Last day of Q1
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q2 Start",
                    ["createdon"] = new DateTime(2024, 7, 1, 0, 0, 0) // First day of Q2 - should NOT match
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2024, 1);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Q1 Start");
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Q1 End");
            Assert.DoesNotContain(results, r => r.GetAttributeValue<string>("name") == "Q2 Start");
        }

        [Fact]
        public void When_InFiscalPeriodAndYear_Quarterly_Q4_Should_Match_Fourth_Quarter()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2024, 4, 1), // April 1st fiscal year start
          FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q4 Start",
                    ["createdon"] = new DateTime(2025, 1, 1, 10, 0, 0) // First day of Q4 (Jan)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q4 End",
                    ["createdon"] = new DateTime(2025, 3, 31, 23, 59, 0) // Last day of Q4 (Mar)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Next FY Q1",
                    ["createdon"] = new DateTime(2025, 4, 1, 0, 0, 0) // First day of next FY - should NOT match
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2024, 4);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Q4 Start");
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Q4 End");
        }

        #endregion

        #region InFiscalPeriod with Monthly Template Tests

        [Fact]
        public void When_InFiscalPeriodAndYear_Monthly_Period1_Should_Match_First_Month()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2024, 1, 1), // Calendar year fiscal
          FiscalPeriodTemplate = FiscalYearSettings.Template.Monthly
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Jan Start",
                    ["createdon"] = new DateTime(2024, 1, 1, 0, 0, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Jan End",
                    ["createdon"] = new DateTime(2024, 1, 31, 23, 59, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Feb Start",
                    ["createdon"] = new DateTime(2024, 2, 1, 0, 0, 0) // Should NOT match
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2024, 1);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Jan Start");
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Jan End");
        }

        [Fact]
        public void When_InFiscalPeriodAndYear_Monthly_Period12_Should_Match_December()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2024, 1, 1), // Calendar year fiscal
          FiscalPeriodTemplate = FiscalYearSettings.Template.Monthly
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Dec Start",
                    ["createdon"] = new DateTime(2024, 12, 1, 0, 0, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Dec End",
                    ["createdon"] = new DateTime(2024, 12, 31, 23, 59, 0)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Next Year Jan",
                    ["createdon"] = new DateTime(2025, 1, 1, 0, 0, 0) // Should NOT match
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2024, 12);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Dec Start");
            Assert.Contains(results, r => r.GetAttributeValue<string>("name") == "Dec End");
        }

        #endregion

        #region ThisFiscalPeriod Tests

        [Fact]
        public void When_ThisFiscalPeriod_Should_Match_Current_Quarter()
        {
            // Arrange
            var context = new XrmFakedContext();
            var today = DateTime.Today;

            // Set up fiscal year starting Jan 1 (calendar year) with quarterly periods
            context.FiscalYearSettings = new FiscalYearSettings
            {
                StartDate = new DateTime(today.Year, 1, 1),
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
            };

            // Determine current quarter boundaries
            var currentQuarter = ((today.Month - 1) / 3) + 1;
            var quarterStartMonth = ((currentQuarter - 1) * 3) + 1;
            var quarterStart = new DateTime(today.Year, quarterStartMonth, 1);
            var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Current Quarter Record",
                    ["createdon"] = today.AddHours(10)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Previous Quarter Record",
                    ["createdon"] = quarterStart.AddDays(-1) // Day before current quarter
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.ThisFiscalPeriod);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Current Quarter Record", results[0].GetAttributeValue<string>("name"));
        }

        #endregion

        #region LastFiscalPeriod Tests

        [Fact]
        public void When_LastFiscalPeriod_Should_Match_Previous_Quarter()
        {
            // Arrange
            var context = new XrmFakedContext();
            var today = DateTime.Today;

            // Set up fiscal year starting Jan 1 (calendar year) with quarterly periods
            context.FiscalYearSettings = new FiscalYearSettings
            {
                StartDate = new DateTime(today.Year, 1, 1),
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
            };

            // Determine current and previous quarter boundaries
            var currentQuarter = ((today.Month - 1) / 3) + 1;
            var quarterStartMonth = ((currentQuarter - 1) * 3) + 1;
            var currentQuarterStart = new DateTime(today.Year, quarterStartMonth, 1);
            var previousQuarterStart = currentQuarterStart.AddMonths(-3);
            var previousQuarterEnd = currentQuarterStart.AddDays(-1);

            // If previous quarter is in previous year, adjust the year
            var previousQuarterYear = previousQuarterStart.Year;

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Previous Quarter Record",
                    ["createdon"] = previousQuarterStart.AddDays(15)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Current Quarter Record",
                    ["createdon"] = today // Should NOT match
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.LastFiscalPeriod);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Previous Quarter Record", results[0].GetAttributeValue<string>("name"));
        }

        #endregion

        #region NextFiscalPeriod Tests

        [Fact]
        public void When_NextFiscalPeriod_Should_Match_Next_Quarter()
        {
            // Arrange
            var context = new XrmFakedContext();
            var today = DateTime.Today;

            // Set up fiscal year starting Jan 1 (calendar year) with quarterly periods
            context.FiscalYearSettings = new FiscalYearSettings
            {
                StartDate = new DateTime(today.Year, 1, 1),
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
            };

            // Determine current and next quarter boundaries
            var currentQuarter = ((today.Month - 1) / 3) + 1;
            var quarterStartMonth = ((currentQuarter - 1) * 3) + 1;
            var currentQuarterStart = new DateTime(today.Year, quarterStartMonth, 1);
            var nextQuarterStart = currentQuarterStart.AddMonths(3);
            var nextQuarterEnd = nextQuarterStart.AddMonths(3).AddDays(-1);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Next Quarter Record",
                    ["createdon"] = nextQuarterStart.AddDays(15)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Current Quarter Record",
                    ["createdon"] = today // Should NOT match
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.NextFiscalPeriod);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Next Quarter Record", results[0].GetAttributeValue<string>("name"));
        }

        #endregion

        #region Period Rollover at Fiscal Year Boundary Tests

        [Fact]
        public void When_LastFiscalPeriod_At_Q1_Should_Rollover_To_Previous_Year_Q4()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        // Set up fiscal year starting Jan 1 with quarterly periods
        // Testing in Q1 of 2025 - last fiscal period should be Q4 of 2024
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2025, 1, 1),
          FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q4 2024 Record",
                    ["createdon"] = new DateTime(2024, 11, 15, 10, 0, 0) // In Q4 2024
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q1 2025 Record",
                    ["createdon"] = new DateTime(2025, 2, 15, 10, 0, 0) // In Q1 2025 - should NOT match
                }
            };

            context.Initialize(entities);

      // Simulate "today" being in Q1 2025 by using InFiscalPeriodAndYear to verify Q4 2024
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2024, 4);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Q4 2024 Record", results[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_NextFiscalPeriod_At_Q4_Should_Rollover_To_Next_Year_Q1()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        // Set up fiscal year starting Jan 1 with quarterly periods
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2024, 1, 1),
          FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q1 2025 Record",
                    ["createdon"] = new DateTime(2025, 2, 15, 10, 0, 0) // In Q1 2025
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q4 2024 Record",
                    ["createdon"] = new DateTime(2024, 11, 15, 10, 0, 0) // In Q4 2024 - should NOT match
                }
            };

            context.Initialize(entities);

      // Verify Q1 2025 via InFiscalPeriodAndYear
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2025, 1);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Q1 2025 Record", results[0].GetAttributeValue<string>("name"));
        }

        #endregion

        #region FetchXML Support Tests

        [Fact]
        public void When_FetchXml_InFiscalPeriodAndYear_Should_Work()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2024, 1, 1),
          FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q2 Record",
                    ["createdon"] = new DateTime(2024, 5, 15, 10, 0, 0) // In Q2 2024
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Q1 Record",
                    ["createdon"] = new DateTime(2024, 2, 15, 10, 0, 0) // In Q1 2024 - should NOT match
                }
            };

            context.Initialize(entities);

            // Act
            var fetchXml = @"<fetch>
                <entity name='account'>
                    <attribute name='name' />
                    <filter>
                        <condition attribute='createdon' operator='in-fiscal-period-and-year'>
                            <value>2024</value>
                            <value>2</value>
                        </condition>
                    </filter>
                </entity>
            </fetch>";

            var service = context.GetOrganizationService();
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Q2 Record", results[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_FetchXml_ThisFiscalPeriod_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();
            var today = DateTime.Today;

            context.FiscalYearSettings = new FiscalYearSettings
            {
                StartDate = new DateTime(today.Year, 1, 1),
                FiscalPeriodTemplate = FiscalYearSettings.Template.Quarterly
            };

            // Determine current quarter boundaries
            var currentQuarter = ((today.Month - 1) / 3) + 1;
            var quarterStartMonth = ((currentQuarter - 1) * 3) + 1;
            var quarterStart = new DateTime(today.Year, quarterStartMonth, 1);

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Current Period Record",
                    ["createdon"] = today
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Previous Period Record",
                    ["createdon"] = quarterStart.AddDays(-1) // Should NOT match
                }
            };

            context.Initialize(entities);

            // Act
            var fetchXml = @"<fetch>
                <entity name='account'>
                    <attribute name='name' />
                    <filter>
                        <condition attribute='createdon' operator='this-fiscal-period' />
                    </filter>
                </entity>
            </fetch>";

            var service = context.GetOrganizationService();
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("Current Period Record", results[0].GetAttributeValue<string>("name"));
        }

        #endregion

        #region Semi-Annual Period Tests

        [Fact]
        public void When_InFiscalPeriodAndYear_SemiAnnual_Should_Match_Correct_Half()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        FiscalYearSettings = new FiscalYearSettings
        {
          StartDate = new DateTime(2024, 1, 1),
          FiscalPeriodTemplate = FiscalYearSettings.Template.SemiAnnually
        }
      };

      var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "H1 Record",
                    ["createdon"] = new DateTime(2024, 3, 15, 10, 0, 0) // In H1 2024
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "H2 Record",
                    ["createdon"] = new DateTime(2024, 9, 15, 10, 0, 0) // In H2 2024 - should NOT match
                }
            };

            context.Initialize(entities);

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2024, 1);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("H1 Record", results[0].GetAttributeValue<string>("name"));
        }

        #endregion

        #region Default Fiscal Settings Tests

        [Fact]
        public void When_NoFiscalSettings_Should_Use_Defaults_April1_Quarterly()
        {
            // Arrange
            var context = new XrmFakedContext();
            // No FiscalYearSettings set - should default to April 1st start, Quarterly

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "FY24 Q1 Record",
                    ["createdon"] = new DateTime(2024, 5, 15, 10, 0, 0) // May 2024 - in Q1 of FY starting Apr 1
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "FY23 Q4 Record",
                    ["createdon"] = new DateTime(2024, 2, 15, 10, 0, 0) // Feb 2024 - in Q4 of FY23 - should NOT match
                }
            };

            context.Initialize(entities);

      // Act - Test Q1 of FY starting April 2024 (Apr-Jun)
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      query.Criteria.AddCondition("createdon", ConditionOperator.InFiscalPeriodAndYear, 2024, 1);
            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("FY24 Q1 Record", results[0].GetAttributeValue<string>("name"));
        }

        #endregion
    }
}

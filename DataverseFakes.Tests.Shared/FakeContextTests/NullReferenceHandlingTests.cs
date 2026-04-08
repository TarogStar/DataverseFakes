using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for null reference exception handling in query operators (v1.0.3)
    /// Resolves upstream issues #608 and #607
    /// </summary>
    public class NullReferenceHandlingTests
    {
        [Fact]
        public void When_Like_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null } // Null name
            };

            context.Initialize(entities);

            // Act - Create query with Like operator
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.Like, "%Test%");

            // Assert - Should not throw NullReferenceException
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_BeginsWith_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.BeginsWith, "Test");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_EndsWith_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.EndsWith, "Account");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_Contains_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.Contains, "Test");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_NotLike_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.NotLike, "%Test%");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_DoesNotContain_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.DoesNotContain, "Test");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_In_Operator_With_Null_Values_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["accountnumber"] = "A001" },
                new Entity("account") { Id = Guid.NewGuid(), ["accountnumber"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("accountnumber", ConditionOperator.In, "A001", "A002");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_NotIn_Operator_With_Null_Values_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["accountnumber"] = "A003" },
                new Entity("account") { Id = Guid.NewGuid(), ["accountnumber"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("accountnumber", ConditionOperator.NotIn, "A001", "A002");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_Like_Operator_With_Empty_Values_Should_Return_No_Results()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" }
            };

            context.Initialize(entities);

            // Act - Create condition with empty values array
            var query = new QueryExpression("account");
            query.Criteria.Conditions.Add(new ConditionExpression
            {
                AttributeName = "name",
                Operator = ConditionOperator.Like,
                Values = { } // Empty values
            });

            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert - Should return no results without throwing
            Assert.Empty(results);
        }

        [Fact]
        public void When_Contains_Operator_With_Empty_Values_Should_Return_No_Results()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.Conditions.Add(new ConditionExpression
            {
                AttributeName = "name",
                Operator = ConditionOperator.Contains,
                Values = { } // Empty values
            });

            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void When_In_Operator_With_Empty_Values_Should_Return_No_Results()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["accountnumber"] = "A001" }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.Conditions.Add(new ConditionExpression
            {
                AttributeName = "accountnumber",
                Operator = ConditionOperator.In,
                Values = { } // Empty values
            });

            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void When_Multiple_String_Operators_On_Null_Values_Should_Filter_Correctly()
        {
            // Arrange
            var context = new XrmFakedContext();

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = guid1, ["name"] = "Test Account", ["description"] = "Description" },
                new Entity("account") { Id = guid2, ["name"] = null, ["description"] = "Description" },
                new Entity("account") { Id = guid3, ["name"] = "Another Account", ["description"] = null }
            };

            context.Initialize(entities);

            // Act - Query with multiple string operators
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.Like, "%Test%");
            query.Criteria.AddCondition("description", ConditionOperator.Contains, "Desc");

            var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();

            // Assert - Should only return first entity
            Assert.Single(results);
            Assert.Equal(guid1, results[0].Id);
        }

        [Fact]
        public void When_FetchXML_With_Like_And_Null_Values_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            var service = context.GetOrganizationService();

            // Act - Use FetchXML with like condition
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='name' operator='like' value='%Test%' />
                        </filter>
                    </entity>
                </fetch>";

            // Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_DoesNotBeginWith_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.DoesNotBeginWith, "Test");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }

        [Fact]
        public void When_DoesNotEndWith_Operator_With_Null_Value_Should_Not_Throw()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = null }
            };

            context.Initialize(entities);

            // Act
            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.DoesNotEndWith, "Account");

            // Assert
            var exception = Record.Exception(() =>
            {
                var results = XrmFakedContext.TranslateQueryExpressionToLinq(context, query).ToList();
            });

            Assert.Null(exception);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for thread-safe parallel CRUD operations on the XrmFakedContext.
    /// These tests verify that the Data dictionary is thread-safe and can handle
    /// concurrent Create, Update, Delete, and Retrieve operations.
    /// Issue #74: Thread-safe parallel creates
    /// </summary>
    public class ParallelCrudTests
    {
        #region Parallel Create Tests

        [Fact]
        public void When_creating_entities_in_parallel_all_entities_should_be_created()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfEntities = 100;
            var createdIds = new List<Guid>();
            var lockObj = new object();

            // Act - Create entities in parallel
            Parallel.For(0, numberOfEntities, i =>
            {
                var entity = new Entity("account")
                {
                    ["name"] = $"Account {i}"
                };
                var id = service.Create(entity);
                lock (lockObj)
                {
                    createdIds.Add(id);
                }
            });

            // Assert
            Assert.Equal(numberOfEntities, createdIds.Count);
            Assert.Equal(numberOfEntities, createdIds.Distinct().Count()); // All IDs should be unique
            Assert.Equal(numberOfEntities, context.Data["account"].Count);
        }

        [Fact]
        public void When_creating_different_entity_types_in_parallel_all_entities_should_be_created()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfEntitiesPerType = 50;
            var entityTypes = new[] { "account", "contact", "lead", "opportunity" };
            var createdIds = new List<Guid>();
            var lockObj = new object();

            // Act - Create different entity types in parallel
            Parallel.ForEach(entityTypes, entityType =>
            {
                Parallel.For(0, numberOfEntitiesPerType, i =>
                {
                    var entity = new Entity(entityType)
                    {
                        ["name"] = $"{entityType} {i}"
                    };
                    var id = service.Create(entity);
                    lock (lockObj)
                    {
                        createdIds.Add(id);
                    }
                });
            });

            // Assert
            var expectedTotal = numberOfEntitiesPerType * entityTypes.Length;
            Assert.Equal(expectedTotal, createdIds.Count);
            Assert.Equal(expectedTotal, createdIds.Distinct().Count());

            foreach (var entityType in entityTypes)
            {
                Assert.Equal(numberOfEntitiesPerType, context.Data[entityType].Count);
            }
        }

        [Fact]
        public void When_creating_entities_with_predefined_ids_in_parallel_all_should_succeed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfEntities = 100;
            var predefinedIds = Enumerable.Range(0, numberOfEntities).Select(_ => Guid.NewGuid()).ToList();
            var createdIds = new List<Guid>();
            var lockObj = new object();

            // Act - Create entities with predefined IDs in parallel
            Parallel.For(0, numberOfEntities, i =>
            {
                var entity = new Entity("account")
                {
                    Id = predefinedIds[i],
                    ["name"] = $"Account {i}"
                };
                var id = service.Create(entity);
                lock (lockObj)
                {
                    createdIds.Add(id);
                }
            });

            // Assert
            Assert.Equal(numberOfEntities, createdIds.Count);
            Assert.All(predefinedIds, id => Assert.Contains(id, createdIds));
            Assert.Equal(numberOfEntities, context.Data["account"].Count);
        }

        #endregion

        #region Parallel Update Tests

        [Fact]
        public void When_updating_entities_in_parallel_all_updates_should_be_applied()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfEntities = 100;

            // Create initial entities
            var ids = new List<Guid>();
            for (int i = 0; i < numberOfEntities; i++)
            {
                var entity = new Entity("account")
                {
                    ["name"] = $"Account {i}",
                    ["revenue"] = 0m
                };
                ids.Add(service.Create(entity));
            }

            // Act - Update all entities in parallel
            Parallel.For(0, numberOfEntities, i =>
            {
                var entity = new Entity("account")
                {
                    Id = ids[i],
                    ["revenue"] = (decimal)(i * 1000)
                };
                service.Update(entity);
            });

            // Assert
            for (int i = 0; i < numberOfEntities; i++)
            {
                var entity = context.Data["account"][ids[i]];
                Assert.Equal((decimal)(i * 1000), entity.GetAttributeValue<decimal>("revenue"));
            }
        }

        [Fact]
        public void When_updating_same_entity_in_parallel_from_multiple_threads_last_write_wins()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var id = Guid.NewGuid();
            var entity = new Entity("account")
            {
                Id = id,
                ["name"] = "Original Name",
                ["numberofemployees"] = 0
            };
            service.Create(entity);

            const int numberOfUpdates = 100;

            // Act - Update the same entity from multiple threads
            Parallel.For(0, numberOfUpdates, i =>
            {
                var update = new Entity("account")
                {
                    Id = id,
                    ["numberofemployees"] = i
                };
                service.Update(update);
            });

            // Assert - Entity should exist and have some value (last one written wins)
            Assert.True(context.Data["account"].ContainsKey(id));
            var finalValue = context.Data["account"][id].GetAttributeValue<int>("numberofemployees");
            Assert.InRange(finalValue, 0, numberOfUpdates - 1);
        }

        #endregion

        #region Parallel Delete Tests

        [Fact]
        public void When_deleting_entities_in_parallel_all_should_be_deleted()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfEntities = 100;

            // Create initial entities
            var ids = new List<Guid>();
            for (int i = 0; i < numberOfEntities; i++)
            {
                var entity = new Entity("account")
                {
                    ["name"] = $"Account {i}"
                };
                ids.Add(service.Create(entity));
            }
            Assert.Equal(numberOfEntities, context.Data["account"].Count);

            // Act - Delete all entities in parallel
            Parallel.ForEach(ids, id =>
            {
                service.Delete("account", id);
            });

            // Assert
            Assert.Empty(context.Data["account"]);
        }

        #endregion

        #region Mixed Parallel Operations Tests

        [Fact]
        public async System.Threading.Tasks.Task When_performing_mixed_crud_operations_in_parallel_no_exceptions_should_occur()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfOperations = 50;
            var exceptions = new List<Exception>();
            var lockObj = new object();

            // Seed with some initial data
            var initialIds = new List<Guid>();
            for (int i = 0; i < 5; i++)
            {
                var entity = new Entity("account")
                {
                    ["name"] = $"Initial Account {i}"
                };
                initialIds.Add(service.Create(entity));
            }

            // Act - Perform mixed operations in parallel
            var tasks = new List<System.Threading.Tasks.Task>();

            // Creates
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Parallel.For(0, numberOfOperations, i =>
                    {
                        var entity = new Entity("account")
                        {
                            ["name"] = $"New Account {i}"
                        };
                        service.Create(entity);
                    });
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));

            // Updates
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(initialIds, id =>
                    {
                        var entity = new Entity("account")
                        {
                            Id = id,
                            ["name"] = $"Updated Account {id}"
                        };
                        service.Update(entity);
                    });
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));

            // Retrieves
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(initialIds, id =>
                    {
                        service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                    });
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public void When_creating_and_retrieving_in_parallel_retrieves_should_see_created_entities()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfEntities = 100;
            var createdIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();
            var retrievedEntities = new System.Collections.Concurrent.ConcurrentBag<Entity>();

            // Act - Create and retrieve in parallel
            Parallel.For(0, numberOfEntities, i =>
            {
                // Create an entity
                var entity = new Entity("account")
                {
                    ["name"] = $"Account {i}"
                };
                var id = service.Create(entity);
                createdIds.Add(id);

                // Immediately try to retrieve it
                var retrieved = service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                retrievedEntities.Add(retrieved);
            });

            // Assert
            Assert.Equal(numberOfEntities, createdIds.Count);
            Assert.Equal(numberOfEntities, retrievedEntities.Count);
            Assert.All(retrievedEntities, e => Assert.NotNull(e));
        }

        #endregion

        #region Thread Safety of Data Dictionary Access Tests

        [Fact]
        public async System.Threading.Tasks.Task When_accessing_data_dictionary_from_multiple_threads_no_collection_modified_exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfIterations = 200;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Seed with initial data
            for (int i = 0; i < 5; i++)
            {
                service.Create(new Entity("account") { ["name"] = $"Account {i}" });
            }

            // Act - Multiple threads accessing Data dictionary simultaneously
            var tasks = new List<System.Threading.Tasks.Task>
            {
                // Thread 1: Enumerating
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < numberOfIterations; i++)
                        {
                            if (context.Data.ContainsKey("account"))
                            {
                                var count = context.Data["account"].Values.Count();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }),

                // Thread 2: Creating
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < numberOfIterations / 5; i++)
                        {
                            service.Create(new Entity("account") { ["name"] = $"New {i}" });
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }),

                // Thread 3: Reading specific entities
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < numberOfIterations; i++)
                        {
                            if (context.Data.ContainsKey("account"))
                            {
                                var firstKey = context.Data["account"].Keys.FirstOrDefault();
                                if (firstKey != Guid.Empty)
                                {
                                    Entity entity;
                                    context.Data["account"].TryGetValue(firstKey, out entity);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                })
            };

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should have occurred
            Assert.Empty(exceptions);
        }

        #endregion

        #region High Concurrency Stress Tests

        [Fact]
        public async System.Threading.Tasks.Task When_creating_many_entities_across_many_threads_all_should_succeed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            const int numberOfThreads = 10;
            const int entitiesPerThread = 100;
            var allIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();

            // Act - Create many entities from many threads
            var tasks = new List<System.Threading.Tasks.Task>();
            for (int t = 0; t < numberOfThreads; t++)
            {
                var threadNum = t;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    for (int i = 0; i < entitiesPerThread; i++)
                    {
                        var entity = new Entity("account")
                        {
                            ["name"] = $"Thread {threadNum} - Account {i}"
                        };
                        var id = service.Create(entity);
                        allIds.Add(id);
                    }
                }));
            }
            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            var expectedTotal = numberOfThreads * entitiesPerThread;
            Assert.Equal(expectedTotal, allIds.Count);
            Assert.Equal(expectedTotal, allIds.Distinct().Count());
            Assert.Equal(expectedTotal, context.Data["account"].Count);
        }

        #endregion
    }
}

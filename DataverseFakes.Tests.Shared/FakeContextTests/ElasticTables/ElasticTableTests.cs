using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.ElasticTables
{
    /// <summary>
    /// Tests for elastic table support:
    /// - Marking/querying elastic tables
    /// - Auto-detection from EntityMetadata.TableType
    /// - partitionid and ttlinseconds round-trip
    /// - Bulk messages allowed on elastic tables
    /// - ExecuteTransaction with elastic op throws
    /// - Associate/Disassociate on elastic throws
    /// - RemoveExpiredElasticRecords
    /// </summary>
    public class ElasticTableTests
    {
        #region Mark / IsElastic API

        [Fact]
        public void MarkAsElasticTable_Then_IsElasticTable_Returns_True()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            Assert.True(ctx.IsElasticTable("contoso_sensor"));
        }

        [Fact]
        public void IsElasticTable_For_Unmarked_Table_Returns_False()
        {
            var ctx = new XrmFakedContext();
            Assert.False(ctx.IsElasticTable("account"));
        }

        [Fact]
        public void MarkAsStandardTable_Removes_Elastic_Registration()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            ctx.MarkAsStandardTable("contoso_sensor");
            Assert.False(ctx.IsElasticTable("contoso_sensor"));
        }

        [Fact]
        public void IsElasticTable_Is_Case_Insensitive()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            Assert.True(ctx.IsElasticTable("Contoso_Sensor"));
            Assert.True(ctx.IsElasticTable("CONTOSO_SENSOR"));
        }

        #endregion

        #region Auto-detect from EntityMetadata.TableType

        [Fact]
        public void InitializeMetadata_With_TableType_Elastic_AutoRegisters_ElasticTable()
        {
            var ctx = new XrmFakedContext();

            var metadata = new EntityMetadata { LogicalName = "contoso_elasticsensor" };

            // Set TableType via reflection (property may not exist on older SDK surfaces)
            var tableTypeProp = metadata.GetType().GetProperty("TableType",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (tableTypeProp == null)
            {
                // Older SDK surface — TableType property does not exist; skip gracefully
                return;
            }

            tableTypeProp.SetValue(metadata, "Elastic");

            ctx.InitializeMetadata(metadata);

            Assert.True(ctx.IsElasticTable("contoso_elasticsensor"));
        }

        [Fact]
        public void InitializeMetadata_With_TableType_Standard_Does_Not_Register_AsElastic()
        {
            var ctx = new XrmFakedContext();

            var metadata = new EntityMetadata { LogicalName = "account" };

            var tableTypeProp = metadata.GetType().GetProperty("TableType",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (tableTypeProp != null)
            {
                tableTypeProp.SetValue(metadata, "Standard");
            }

            ctx.InitializeMetadata(metadata);

            Assert.False(ctx.IsElasticTable("account"));
        }

        #endregion

        #region partitionid and ttlinseconds round-trip

        [Fact]
        public void Create_And_Retrieve_ElasticRecord_With_Partitionid_And_Ttlinseconds()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var entity = new Entity("contoso_sensor")
            {
                ["name"] = "sensor-01",
                ["partitionid"] = "region-west",
                ["ttlinseconds"] = 3600
            };

            var id = service.Create(entity);

            var retrieved = service.Retrieve("contoso_sensor", id, new ColumnSet(true));

            Assert.Equal("region-west", retrieved.GetAttributeValue<string>("partitionid"));
            Assert.Equal(3600, retrieved.GetAttributeValue<int>("ttlinseconds"));
        }

        [Fact]
        public void Partitionid_And_Ttlinseconds_Persist_Through_Update()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var entity = new Entity("contoso_sensor")
            {
                ["name"] = "sensor-02",
                ["partitionid"] = "region-east",
                ["ttlinseconds"] = 7200
            };
            var id = service.Create(entity);

            // Update name only — partition/ttl should remain
            var update = new Entity("contoso_sensor") { Id = id, ["name"] = "sensor-02-updated" };
            service.Update(update);

            var retrieved = service.Retrieve("contoso_sensor", id, new ColumnSet(true));

            Assert.Equal("region-east", retrieved.GetAttributeValue<string>("partitionid"));
            Assert.Equal(7200, retrieved.GetAttributeValue<int>("ttlinseconds"));
            Assert.Equal("sensor-02-updated", retrieved.GetAttributeValue<string>("name"));
        }

        #endregion

        #region Bulk messages allowed on elastic tables

        [Fact]
        public void CreateMultiple_Works_On_Elastic_Table()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var targets = new EntityCollection();
            targets.Entities.Add(new Entity("contoso_sensor") { ["name"] = "s1", ["partitionid"] = "p1" });
            targets.Entities.Add(new Entity("contoso_sensor") { ["name"] = "s2", ["partitionid"] = "p2" });

            var response = (CreateMultipleResponse)service.Execute(new CreateMultipleRequest { Targets = targets });

            Assert.Equal(2, response.Ids.Length);
        }

        [Fact]
        public void UpdateMultiple_Works_On_Elastic_Table()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");

            var id = Guid.NewGuid();
            ctx.Initialize(new[] { new Entity("contoso_sensor") { Id = id, ["name"] = "original" } });

            var service = ctx.GetOrganizationService();
            var updates = new EntityCollection();
            updates.Entities.Add(new Entity("contoso_sensor") { Id = id, ["name"] = "updated" });

            // Should NOT throw
            service.Execute(new UpdateMultipleRequest { Targets = updates });

            var retrieved = service.Retrieve("contoso_sensor", id, new ColumnSet(true));
            Assert.Equal("updated", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void UpsertMultiple_Works_On_Elastic_Table()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var targets = new EntityCollection();
            targets.Entities.Add(new Entity("contoso_sensor") { ["name"] = "new-sensor" });

            var response = (UpsertMultipleResponse)service.Execute(new UpsertMultipleRequest { Targets = targets });

            Assert.Single(response.Results);
            Assert.True(response.Results[0].RecordCreated);
        }

        [Fact]
        public void DeleteMultiple_Works_On_Elastic_Table()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            ctx.Initialize(new[]
            {
                new Entity("contoso_sensor") { Id = id1, ["name"] = "s1" },
                new Entity("contoso_sensor") { Id = id2, ["name"] = "s2" }
            });

            var service = ctx.GetOrganizationService();
            var refs = new EntityReferenceCollection
            {
                new EntityReference("contoso_sensor", id1),
                new EntityReference("contoso_sensor", id2)
            };

            // Should NOT throw
            service.Execute(new DeleteMultipleRequest { Targets = refs });

            Assert.Empty(ctx.CreateQuery("contoso_sensor"));
        }

        #endregion

        #region ExecuteTransaction with elastic op throws

#if FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
        [Fact]
        public void ExecuteTransaction_With_Elastic_Create_Target_Throws_FaultException()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");

            var executor = new DataverseFakes.FakeMessageExecutors.ExecuteTransactionExecutor();

            var req = new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = new Entity("contoso_sensor") }
                }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                executor.Execute(req, ctx));
        }

        [Fact]
        public void ExecuteTransaction_With_NonElastic_Target_Succeeds()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");

            // Standard table — should NOT throw
            var executor = new DataverseFakes.FakeMessageExecutors.ExecuteTransactionExecutor();
            var req = new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = new Entity("account") }
                }
            };

            var response = executor.Execute(req, ctx);
            Assert.NotNull(response);
        }

        [Fact]
        public void ExecuteTransaction_Mixed_Elastic_And_Standard_Throws_FaultException()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");

            var executor = new DataverseFakes.FakeMessageExecutors.ExecuteTransactionExecutor();
            var req = new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = new Entity("account") },
                    new CreateRequest { Target = new Entity("contoso_sensor") }
                }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                executor.Execute(req, ctx));
        }
#endif

        #endregion

        #region Associate / Disassociate on elastic throws

        [Fact]
        public void Associate_With_Elastic_Target_Throws_FaultException()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var associateRequest = new AssociateRequest
            {
                Target = new EntityReference("contoso_sensor", Guid.NewGuid()),
                Relationship = new Relationship("some_relationship"),
                RelatedEntities = new EntityReferenceCollection
                {
                    new EntityReference("account", Guid.NewGuid())
                }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Execute(associateRequest));
        }

        [Fact]
        public void Associate_With_Elastic_Related_Entity_Throws_FaultException()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var associateRequest = new AssociateRequest
            {
                Target = new EntityReference("account", Guid.NewGuid()),
                Relationship = new Relationship("some_relationship"),
                RelatedEntities = new EntityReferenceCollection
                {
                    new EntityReference("contoso_sensor", Guid.NewGuid())
                }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Execute(associateRequest));
        }

        [Fact]
        public void Disassociate_With_Elastic_Target_Throws_FaultException()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var disassociateRequest = new DisassociateRequest
            {
                Target = new EntityReference("contoso_sensor", Guid.NewGuid()),
                Relationship = new Relationship("some_relationship"),
                RelatedEntities = new EntityReferenceCollection
                {
                    new EntityReference("account", Guid.NewGuid())
                }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Execute(disassociateRequest));
        }

        [Fact]
        public void Disassociate_With_Elastic_Related_Entity_Throws_FaultException()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            var service = ctx.GetOrganizationService();

            var disassociateRequest = new DisassociateRequest
            {
                Target = new EntityReference("account", Guid.NewGuid()),
                Relationship = new Relationship("some_relationship"),
                RelatedEntities = new EntityReferenceCollection
                {
                    new EntityReference("contoso_sensor", Guid.NewGuid())
                }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Execute(disassociateRequest));
        }

        #endregion

        #region RemoveExpiredElasticRecords

        [Fact]
        public void RemoveExpiredElasticRecords_Removes_Only_Expired_Rows()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");

            var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Expired: createdon=baseTime, ttl=10s -> expires at baseTime+10s
            var expiredId = Guid.NewGuid();
            var expiredEntity = new Entity("contoso_sensor")
            {
                Id = expiredId,
                ["name"] = "expired",
                ["createdon"] = baseTime,
                ["ttlinseconds"] = 10
            };

            // Not expired: createdon=baseTime, ttl=9999s -> expires far in future
            var activeId = Guid.NewGuid();
            var activeEntity = new Entity("contoso_sensor")
            {
                Id = activeId,
                ["name"] = "active",
                ["createdon"] = baseTime,
                ["ttlinseconds"] = 9999
            };

            // No TTL: should never be deleted by this method
            var noTtlId = Guid.NewGuid();
            var noTtlEntity = new Entity("contoso_sensor")
            {
                Id = noTtlId,
                ["name"] = "no-ttl",
                ["createdon"] = baseTime
            };

            ctx.Initialize(new[] { expiredEntity, activeEntity, noTtlEntity });

            // asOfUtc = baseTime + 60s => expired record (ttl=10) has passed, active (ttl=9999) has not
            var asOf = baseTime.AddSeconds(60);

            var deleted = ctx.RemoveExpiredElasticRecords("contoso_sensor", asOf);

            Assert.Equal(1, deleted);
            Assert.False(ctx.Data["contoso_sensor"].ContainsKey(expiredId));
            Assert.True(ctx.Data["contoso_sensor"].ContainsKey(activeId));
            Assert.True(ctx.Data["contoso_sensor"].ContainsKey(noTtlId));
        }

        [Fact]
        public void RemoveExpiredElasticRecords_Returns_Zero_When_No_Records_Expired()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");

            var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var id = Guid.NewGuid();
            ctx.Initialize(new[]
            {
                new Entity("contoso_sensor") { Id = id, ["createdon"] = baseTime, ["ttlinseconds"] = 9999 }
            });

            // asOfUtc is before expiry
            var deleted = ctx.RemoveExpiredElasticRecords("contoso_sensor", baseTime.AddSeconds(1));

            Assert.Equal(0, deleted);
            Assert.True(ctx.Data["contoso_sensor"].ContainsKey(id));
        }

        [Fact]
        public void RemoveExpiredElasticRecords_Throws_When_Table_Not_Elastic()
        {
            var ctx = new XrmFakedContext();
            // "account" is NOT marked elastic

            Assert.Throws<InvalidOperationException>(() =>
                ctx.RemoveExpiredElasticRecords("account", DateTime.UtcNow));
        }

        [Fact]
        public void RemoveExpiredElasticRecords_Returns_Zero_When_No_Data_For_Table()
        {
            var ctx = new XrmFakedContext();
            ctx.MarkAsElasticTable("contoso_sensor");
            // No records initialized

            var deleted = ctx.RemoveExpiredElasticRecords("contoso_sensor", DateTime.UtcNow);

            Assert.Equal(0, deleted);
        }

        #endregion
    }
}

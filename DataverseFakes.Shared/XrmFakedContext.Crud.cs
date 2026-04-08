using FakeItEasy;
using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

namespace DataverseFakes
{
    /// <summary>
    /// Partial class containing CRUD (Create, Retrieve, Update, Delete) operations for the faked CRM context.
    /// This partial class handles all basic entity manipulation operations that simulate the behavior of
    /// the Dynamics 365 / Power Platform IOrganizationService.
    /// </summary>
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// The state code value representing an active entity record in Dynamics 365.
        /// </summary>
        protected const int EntityActiveStateCode = 0;

        /// <summary>
        /// The state code value representing an inactive (deactivated) entity record in Dynamics 365.
        /// </summary>
        protected const int EntityInactiveStateCode = 1;

        /// <summary>
        /// The minimum DateTime value supported by CRM/Dataverse (SQL Server datetime limitation).
        /// </summary>
        protected static readonly DateTime CrmMinDateTime = new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Gets or sets a value indicating whether entity references should be validated when creating or updating entities.
        /// When set to true, the context will verify that referenced entities exist in the in-memory data store
        /// before allowing the operation to proceed.
        /// </summary>
        /// <value>
        /// <c>true</c> if entity references should be validated; otherwise, <c>false</c>.
        /// </value>
        public bool ValidateReferences { get; set; }

        #region CRUD

        /// <summary>
        /// Gets the unique identifier (GUID) for a CRM entity record based on its entity reference.
        /// This method supports both standard ID-based lookups and alternate key lookups for Dynamics 365 v9.x and later.
        /// </summary>
        /// <param name="record">The entity reference containing the logical name and either the ID or alternate key attributes to identify the record.</param>
        /// <param name="validate">
        /// When set to <c>true</c>, the method will throw an exception if the record is not found.
        /// When set to <c>false</c>, the method will return <see cref="Guid.Empty"/> if the record is not found via alternate keys.
        /// Defaults to <c>true</c>.
        /// </param>
        /// <returns>The unique identifier (GUID) of the entity record.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the entity logical name is null or empty, when the entity logical name is not valid,
        /// or when the requested alternate key attributes do not exist for the entity.
        /// </exception>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the record with the specified alternate keys does not exist (if validate is true).
        /// </exception>
        public Guid GetRecordUniqueId(EntityReference record, bool validate = true)
        {
            if (string.IsNullOrWhiteSpace(record.LogicalName))
            {
                throw new InvalidOperationException("The entity logical name must not be null or empty.");
            }

            // Don't fail with invalid operation exception, if no record of this entity exists, but entity is known
            if (!Data.ContainsKey(record.LogicalName) && !EntityMetadata.ContainsKey(record.LogicalName))
            {
                if (ProxyTypesAssembly == null)
                {
                    throw new InvalidOperationException($"The entity logical name {record.LogicalName} is not valid.");
                }

                if (!ProxyTypesAssembly.GetTypes().Any(type => FindReflectedType(record.LogicalName) != null))
                {
                    throw new InvalidOperationException($"The entity logical name {record.LogicalName} is not valid.");
                }
            }

#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
            if (record.Id == Guid.Empty && record.HasKeyAttributes())
            {
                if (EntityMetadata.ContainsKey(record.LogicalName))
                {
                    var entityMetadata = EntityMetadata[record.LogicalName];
                    foreach (var key in entityMetadata.Keys)
                    {
                        if (record.KeyAttributes.Keys.Count == key.KeyAttributes.Length && key.KeyAttributes.All(x => record.KeyAttributes.Keys.Contains(x)))
                        {
                            ConcurrentDictionary<Guid, Entity> entityDict;
                            if (Data.TryGetValue(record.LogicalName, out entityDict))
                            {
                                var matchedRecord = entityDict.Values.SingleOrDefault(x => record.KeyAttributes.All(k => x.Attributes.ContainsKey(k.Key) && x.Attributes[k.Key] != null && x.Attributes[k.Key].Equals(k.Value)));
                                if (matchedRecord != null)
                                {
                                    return matchedRecord.Id;
                                }
                            }
                            if (validate)
                            {
                                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), $"{record.LogicalName} with the specified Alternate Keys Does Not Exist");
                            }
                        }
                    }
                }
                if (validate)
                {
                    throw new InvalidOperationException($"The requested key attributes do not exist for the entity {record.LogicalName}");
                }
            }
#endif
            /*
            if (validate && record.Id == Guid.Empty)
            {
                throw new InvalidOperationException("The id must not be empty.");
            }
            */
            
            return record.Id;
        }

        /// <summary>
        /// Checks if creating or updating an entity would violate any alternate key uniqueness constraints.
        /// </summary>
        /// <param name="entity">The entity being created or updated.</param>
        /// <param name="excludeId">Optional ID to exclude from the check (used for updates to exclude the entity being updated).</param>
        /// <returns>The <see cref="EntityKeyMetadata"/> that was violated, or null if no violation occurred.</returns>
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
        protected internal EntityKeyMetadata FindViolatedAlternateKey(Entity entity, Guid? excludeId = null)
        {
            if (!EntityMetadata.ContainsKey(entity.LogicalName))
            {
                return null;
            }

            var metadata = EntityMetadata[entity.LogicalName];
            if (metadata.Keys == null || metadata.Keys.Length == 0)
            {
                return null;
            }

            ConcurrentDictionary<Guid, Entity> entityDict;
            if (!Data.TryGetValue(entity.LogicalName, out entityDict))
            {
                return null;
            }

            foreach (var key in metadata.Keys)
            {
                // Dataverse alternate key null-handling behavior:
                // - If ANY key attribute is missing or null in the entity being created/updated,
                //   the alternate key constraint does not apply to that record.
                // - This means multiple records can have null values in key attributes without conflict.
                // - Only records where ALL key attributes have non-null values are subject to uniqueness.
                // - This matches real Dataverse behavior where null values effectively "opt out" of the key.
                if (!key.KeyAttributes.All(attr =>
                    entity.Attributes.ContainsKey(attr) && entity[attr] != null))
                {
                    continue; // Key doesn't apply - not all attributes present or have non-null values
                }

                // Build key values for comparison
                var keyValues = key.KeyAttributes
                    .Select(attr => new { Attribute = attr, Value = entity[attr] })
                    .ToList();

                // Find any existing record with matching key values.
                // Note: We also require existing records to have non-null values for all key attributes.
                // Records with null key attributes are not considered for duplicate checking,
                // matching Dataverse behavior where null effectively opts out of the key constraint.
                var duplicate = entityDict.Values
                    .Where(existing => excludeId == null || existing.Id != excludeId.Value)
                    .FirstOrDefault(existing =>
                        keyValues.All(kv =>
                            existing.Attributes.ContainsKey(kv.Attribute) &&
                            existing[kv.Attribute] != null &&
                            CompareKeyValues(existing[kv.Attribute], kv.Value)));

                if (duplicate != null)
                {
                    return key;
                }
            }

            return null;
        }

        /// <summary>
        /// Compares two attribute values for equality, handling SDK types used in alternate keys.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns><c>true</c> if the values are equal; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Dataverse alternate keys only support specific attribute types:
        /// - Decimal, Integer, String, DateTime, Lookup, Picklist/OptionSet
        /// - Money is NOT supported as an alternate key attribute type
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
        /// </remarks>
        private bool CompareKeyValues(object value1, object value2)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            // EntityReference handles Lookup and Customer types
            if (value1 is EntityReference er1 && value2 is EntityReference er2)
                return er1.LogicalName == er2.LogicalName && er1.Id == er2.Id;

            // OptionSetValue handles Picklist, State, and Status types
            if (value1 is OptionSetValue osv1 && value2 is OptionSetValue osv2)
                return osv1.Value == osv2.Value;

            // Note: Money is NOT a valid alternate key attribute type in Dataverse.
            // Money attributes cannot be used as part of alternate keys.
            // If you encounter Money values here, it indicates an invalid key configuration.

            // Handles Decimal, Integer, String, DateTime, and other primitive types
            return value1.Equals(value2);
        }
#endif

        /// <summary>
        /// Configures the faked IOrganizationService to intercept Retrieve method calls and return entities
        /// from the in-memory data store. This method sets up the FakeItEasy mock to handle retrieve operations
        /// by delegating to the RetrieveRequest message executor.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> instance containing the in-memory data store and message executors.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> instance to configure with the Retrieve behavior.</param>
        protected static void FakeRetrieve(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Retrieve(A<string>._, A<Guid>._, A<ColumnSet>._))
                .ReturnsLazily((string entityName, Guid id, ColumnSet columnSet) =>
                {
                    RetrieveRequest retrieveRequest = new RetrieveRequest()
                    {
                        Target = new EntityReference() { LogicalName = entityName, Id = id },
                        ColumnSet = columnSet
                    };
                    var executor = context.FakeMessageExecutors[typeof(RetrieveRequest)];

                    RetrieveResponse retrieveResponse = (RetrieveResponse)executor.Execute(retrieveRequest, context);

                    return retrieveResponse.Entity;
                });
        }
        /// <summary>
        /// Configures the faked IOrganizationService to intercept Create method calls and store new entities
        /// in the in-memory data store. This method sets up the FakeItEasy mock to handle create operations
        /// by delegating to the <see cref="CreateEntity"/> method.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> instance containing the in-memory data store.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> instance to configure with the Create behavior.</param>
        protected static void FakeCreate(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Create(A<Entity>._))
                .ReturnsLazily((Entity e) =>
                {
                    return context.CreateEntity(e);
                });
        }

        /// <summary>
        /// Configures the faked IOrganizationService to intercept Update method calls and modify existing entities
        /// in the in-memory data store. This method sets up the FakeItEasy mock to handle update operations
        /// by delegating to the <see cref="UpdateEntity"/> method.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> instance containing the in-memory data store.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> instance to configure with the Update behavior.</param>
        protected static void FakeUpdate(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Update(A<Entity>._))
                .Invokes((Entity e) =>
                {
                    context.UpdateEntity(e);
                });
        }

        /// <summary>
        /// Updates an existing entity record in the in-memory data store with the provided attribute values.
        /// This method simulates the Dynamics 365 Update operation, including support for alternate key lookups,
        /// entity reference validation, and plugin pipeline execution when enabled.
        /// </summary>
        /// <param name="e">The entity containing the attributes to update. The entity must have a valid logical name and either an ID or alternate key attributes.</param>
        /// <exception cref="InvalidOperationException">Thrown when the entity is null.</exception>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when the entity record does not exist in the data store.</exception>
        /// <remarks>
        /// The method performs the following operations:
        /// <list type="bullet">
        /// <item><description>Clones the input entity to prevent modification of the original</description></item>
        /// <item><description>Resolves the entity ID using alternate keys if necessary</description></item>
        /// <item><description>Executes pre-operation and post-operation plugin pipeline stages if pipeline simulation is enabled</description></item>
        /// <item><description>Updates modified attributes, removing attributes set to null</description></item>
        /// <item><description>Converts DateTime values to UTC</description></item>
        /// <item><description>Validates entity references if ValidateReferences is enabled</description></item>
        /// <item><description>Updates the modifiedon and modifiedby system attributes</description></item>
        /// </list>
        /// </remarks>
        protected void UpdateEntity(Entity e)
        {
            if (e == null)
            {
                throw new InvalidOperationException("The entity must not be null");
            }
            e = e.Clone(e.GetType());
            var reference = e.ToEntityReferenceWithKeyAttributes();
            e.Id = GetRecordUniqueId(reference);

            // Update specific validations: The entity record must exist in the context
            ConcurrentDictionary<Guid, Entity> entityDict;
            Entity cachedEntity;
            if (Data.TryGetValue(e.LogicalName, out entityDict) &&
                entityDict.TryGetValue(e.Id, out cachedEntity))
            {
                // Capture pre-image BEFORE any modifications (clone the cached entity with all fields)
                Entity preImage = null;
                if (this.UsePipelineSimulation)
                {
                    preImage = cachedEntity.Clone(cachedEntity.GetType());
                }

                if (this.UsePipelineSimulation)
                {
                    // For pre-operation stages, we have the preImage but no postImage yet
                    ExecutePipelineStage("Update", ProcessingStepStage.Prevalidation, ProcessingStepMode.Synchronous, e, preImage, null);
                    ExecutePipelineStage("Update", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous, e, preImage, null);
                }

                // Add as many attributes to the entity as the ones received (this will keep existing ones)

                // Check alternate key uniqueness constraints for the merged entity
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
                var mergedForKeyCheck = cachedEntity.Clone(cachedEntity.GetType());
                foreach (var attr in e.Attributes)
                {
                    if (attr.Value != null)
                    {
                        mergedForKeyCheck[attr.Key] = attr.Value;
                    }
                }

                var violatedKey = FindViolatedAlternateKey(mergedForKeyCheck, e.Id);
                if (violatedKey != null)
                {
                    var keyDescription = string.Join(", ", violatedKey.KeyAttributes);
                    throw new FaultException<OrganizationServiceFault>(
                        new OrganizationServiceFault(),
                        $"A record that has the attribute values {keyDescription} already exists. The duplicate values are in the following attributes: {keyDescription}.");
                }
#endif

                foreach (var sAttributeName in e.Attributes.Keys.ToList())
                {
                    var attribute = e[sAttributeName];
                    if (attribute == null)
                    {
                        cachedEntity.Attributes.Remove(sAttributeName);
                    }
                    else if (attribute is DateTime)
                    {
                        var dateValue = (DateTime)e[sAttributeName];
                        ValidateDateTime(dateValue, sAttributeName);
                        cachedEntity[sAttributeName] = ConvertToUtc(dateValue);
                    }
                    else
                    {
                        if (attribute is EntityReference && ValidateReferences)
                        {
                            var target = (EntityReference)e[sAttributeName];
                            attribute = ResolveEntityReference(target);
                        }
                        cachedEntity[sAttributeName] = attribute;
                    }
                }

                // Update ModifiedOn
                cachedEntity["modifiedon"] = DateTime.UtcNow;
                cachedEntity["modifiedby"] = CallerId;

                // Increment versionnumber for optimistic concurrency
                cachedEntity["versionnumber"] = GetNextVersionNumber();

                if (this.UsePipelineSimulation)
                {
                    // Capture post-image AFTER the update (clone the updated cached entity with all fields)
                    Entity postImage = cachedEntity.Clone(cachedEntity.GetType());

                    ExecutePipelineStage("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous, e, preImage, postImage);

                    var clone = e.Clone(e.GetType());
                    ExecutePipelineStage("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Asynchronous, clone, preImage, postImage);
                }
            }
            else
            {
                // The entity record was not found, return a CRM-ish update error message
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), $"{e.LogicalName} with Id {e.Id} Does Not Exist");
            }
        }

        /// <summary>
        /// Resolves an entity reference by verifying that the referenced entity exists in the in-memory data store.
        /// If the entity reference has an empty ID but contains alternate key attributes, the method attempts
        /// to resolve the reference using alternate keys.
        /// </summary>
        /// <param name="er">The entity reference to resolve and validate.</param>
        /// <returns>The validated entity reference, or a new entity reference with the resolved ID if alternate keys were used.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the referenced entity does not exist in the data store.
        /// </exception>
        protected EntityReference ResolveEntityReference(EntityReference er)
        {
            ConcurrentDictionary<Guid, Entity> entityDict;
            if (!Data.TryGetValue(er.LogicalName, out entityDict) || !entityDict.ContainsKey(er.Id))
            {
                if (er.Id == Guid.Empty && er.HasKeyAttributes())
                {
                    return ResolveEntityReferenceByAlternateKeys(er);
                }
                else
                {
                    throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), $"{er.LogicalName} With Id = {er.Id:D} Does Not Exist");
                }
            }
            return er;
        }

        /// <summary>
        /// Resolves an entity reference by looking up the entity using its alternate key attributes.
        /// This method creates a new entity reference with the resolved GUID ID.
        /// </summary>
        /// <param name="er">The entity reference containing alternate key attributes to use for the lookup.</param>
        /// <returns>A new <see cref="EntityReference"/> with the logical name and resolved ID populated.</returns>
        /// <remarks>
        /// This method is used when an entity reference is provided with alternate keys instead of a GUID.
        /// It delegates to <see cref="GetRecordUniqueId"/> to perform the actual lookup.
        /// </remarks>
        protected EntityReference ResolveEntityReferenceByAlternateKeys(EntityReference er)
        {
            var resolvedId = GetRecordUniqueId(er);

            return new EntityReference()
            {
                LogicalName = er.LogicalName,
                Id = resolvedId
            };
        }
        /// <summary>
        /// Configures the faked IOrganizationService to intercept Delete method calls and remove entities
        /// from the in-memory data store. This method sets up the FakeItEasy mock to handle delete operations
        /// by delegating to the <see cref="DeleteEntity"/> method.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> instance containing the in-memory data store.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> instance to configure with the Delete behavior.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the entity logical name is null or empty, or when the ID is empty.
        /// </exception>
        protected static void FakeDelete(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Delete(A<string>._, A<Guid>._))
                .Invokes((string entityName, Guid id) =>
                {
                    if (string.IsNullOrWhiteSpace(entityName))
                    {
                        throw new InvalidOperationException("The entity logical name must not be null or empty.");
                    }

                    if (id == Guid.Empty)
                    {
                        throw new InvalidOperationException("The id must not be empty.");
                    }

                    var entityReference = new EntityReference(entityName, id);

                    context.DeleteEntity(entityReference);
                });
        }

        /// <summary>
        /// Deletes an entity record from the in-memory data store.
        /// This method simulates the Dynamics 365 Delete operation, including validation of entity existence
        /// and plugin pipeline execution when enabled.
        /// </summary>
        /// <param name="er">The entity reference identifying the record to delete.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the entity logical name is not valid (not found in proxy types assembly or entity metadata).
        /// </exception>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the entity record does not exist in the data store.
        /// </exception>
        /// <remarks>
        /// The method performs the following operations:
        /// <list type="bullet">
        /// <item><description>Validates that the entity logical name is known (exists in proxy types assembly or entity metadata)</description></item>
        /// <item><description>Verifies that the record exists in the data store</description></item>
        /// <item><description>Executes pre-operation and post-operation plugin pipeline stages if pipeline simulation is enabled</description></item>
        /// <item><description>Removes the entity from the in-memory data store</description></item>
        /// </list>
        /// </remarks>
        protected void DeleteEntity(EntityReference er)
        {
            // Don't fail with invalid operation exception, if no record of this entity exists, but entity is known
            if (!this.Data.ContainsKey(er.LogicalName))
            {
                if (this.ProxyTypesAssembly == null)
                {
                    throw new InvalidOperationException($"The entity logical name {er.LogicalName} is not valid.");
                }

                if (!this.ProxyTypesAssembly.GetTypes().Any(type => this.FindReflectedType(er.LogicalName) != null))
                {
                    throw new InvalidOperationException($"The entity logical name {er.LogicalName} is not valid.");
                }
            }

            // Entity logical name exists, so check if the requested entity exists
            ConcurrentDictionary<Guid, Entity> entityDict;
            if (this.Data.TryGetValue(er.LogicalName, out entityDict) && entityDict != null &&
                entityDict.ContainsKey(er.Id))
            {
                // Capture pre-image BEFORE deletion (clone the entity with all fields)
                Entity preImage = null;
                Entity entityToDelete;
                if (entityDict.TryGetValue(er.Id, out entityToDelete))
                {
                    if (this.UsePipelineSimulation)
                    {
                        preImage = entityToDelete.Clone(entityToDelete.GetType());
                    }
                }

                if (this.UsePipelineSimulation)
                {
                    // For Delete, we have preImage but no postImage (entity no longer exists after delete)
                    ExecutePipelineStage("Delete", ProcessingStepStage.Prevalidation, ProcessingStepMode.Synchronous, er, preImage, null);
                    ExecutePipelineStage("Delete", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous, er, preImage, null);
                }

                // Entity found => remove it using thread-safe TryRemove
                Entity removedEntity;
                entityDict.TryRemove(er.Id, out removedEntity);

                if (this.UsePipelineSimulation)
                {
                    // Post-operation still has preImage, but no postImage (entity is deleted)
                    ExecutePipelineStage("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous, er, preImage, null);
                    ExecutePipelineStage("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Asynchronous, er, preImage, null);
                }
            }
            else
            {
                // Entity not found in the context => throw not found exception
                // The entity record was not found, return a CRM-ish update error message
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), $"{er.LogicalName} with Id {er.Id} Does Not Exist");
            }
        }
        #endregion

        #region Other protected methods

        /// <summary>
        /// Ensures that the specified entity name exists in the metadata cache.
        /// This method validates the entity name against relationships, the in-memory Data dictionary,
        /// or the proxy types assembly.
        /// </summary>
        /// <param name="sEntityName">The logical name of the entity to validate.</param>
        /// <exception cref="Exception">
        /// Thrown when the entity name does not exist in the metadata cache and a proxy types assembly is configured.
        /// </exception>
        /// <remarks>
        /// The method performs validation in the following order:
        /// <list type="number">
        /// <item><description>Checks if the entity name is part of any defined relationship (as entity1, entity2, or intersect entity)</description></item>
        /// <item><description>Checks if the entity exists in the Data dictionary (supports late-bound entities initialized before query)</description></item>
        /// <item><description>If a proxy types assembly is set, attempts to find the entity type via reflection</description></item>
        /// </list>
        /// This order ensures that late-bound entities can be used alongside early-bound entities when ProxyTypesAssembly is set.
        /// Fix for GitHub issue #462.
        /// </remarks>
        protected void EnsureEntityNameExistsInMetadata(string sEntityName)
        {
            // Check relationships first
            if (Relationships.Values.Any(value => new[] { value.Entity1LogicalName, value.Entity2LogicalName, value.IntersectEntity }.Contains(sEntityName, StringComparer.InvariantCultureIgnoreCase)))
            {
                return;
            }

            // Check if entity exists in Data dictionary (supports late-bound entities initialized before proxy types)
            // This allows mixing early-bound and late-bound entities when ProxyTypesAssembly is set
            // Fix for GitHub issue #462
            if (Data.ContainsKey(sEntityName))
            {
                return;
            }

            // Entity metadata is checked differently when we are using a ProxyTypesAssembly => we can infer that from the generated types assembly
            if (ProxyTypesAssembly != null)
            {
                var subClassType = FindReflectedType(sEntityName);
                if (subClassType == null)
                {
                    throw new Exception($"Entity {sEntityName} does not exist in the metadata cache");
                }
            }
        }

        /// <summary>
        /// Adds default system attributes to an entity when it is being created.
        /// This method initializes the entity with standard CRM attributes such as createdon, modifiedon,
        /// createdby, modifiedby, statecode, and statuscode.
        /// </summary>
        /// <param name="e">The entity to which default attributes should be added.</param>
        /// <remarks>
        /// This method performs the following operations:
        /// <list type="bullet">
        /// <item><description>Creates a default CallerId (system user) if one is not already set</description></item>
        /// <item><description>If ValidateReferences is enabled, ensures the caller user exists in the data store</description></item>
        /// <item><description>Delegates to the EntityInitializerService to set standard entity attributes</description></item>
        /// </list>
        /// </remarks>
        protected void AddEntityDefaultAttributes(Entity e)
        {
            // Add createdon, modifiedon, createdby, modifiedby properties
            if (CallerId == null)
            {
                lock (_dataLock)
                {
                    // Double-check after acquiring lock
                    if (CallerId == null)
                    {
                        CallerId = new EntityReference("systemuser", Guid.NewGuid()); // Create a new instance by default
                        if (ValidateReferences)
                        {
                            var systemUserDict = Data.GetOrAdd("systemuser", _ => new ConcurrentDictionary<Guid, Entity>());
                            systemUserDict.TryAdd(CallerId.Id, new Entity("systemuser") { Id = CallerId.Id });
                        }
                    }
                }
            }

            var isManyToManyRelationshipEntity = e.LogicalName != null && this.Relationships.ContainsKey(e.LogicalName);

            EntityInitializerService.Initialize(e, CallerId.Id, this, isManyToManyRelationshipEntity);
        }

        /// <summary>
        /// Validates that an entity has the required properties for storage in the in-memory data store.
        /// This method ensures the entity is not null, has a valid logical name, and has a non-empty ID.
        /// </summary>
        /// <param name="e">The entity to validate.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the entity is null, the LogicalName property is null or empty,
        /// or the Id property is <see cref="Guid.Empty"/>.
        /// </exception>
        protected void ValidateEntity(Entity e)
        {
            if (e == null)
            {
                throw new InvalidOperationException("The entity must not be null");
            }

            // Validate the entity
            if (string.IsNullOrWhiteSpace(e.LogicalName))
            {
                throw new InvalidOperationException("The LogicalName property must not be empty");
            }

            if (e.Id == Guid.Empty)
            {
                throw new InvalidOperationException("The Id property must not be empty");
            }
        }

        /// <summary>
        /// Creates a new entity record in the in-memory data store.
        /// This method simulates the Dynamics 365 Create operation, including automatic ID generation,
        /// default attribute initialization, validation, and support for related entities.
        /// </summary>
        /// <param name="e">The entity to create. If the entity has no ID, one will be generated automatically.</param>
        /// <returns>The unique identifier (GUID) of the newly created entity record.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description>The entity is null</description></item>
        /// <item><description>A record with the same logical name and ID already exists</description></item>
        /// <item><description>The entity contains a statecode attribute (statecode must be set after creation)</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// The method performs the following operations:
        /// <list type="number">
        /// <item><description>Clones the input entity to prevent modification of the original</description></item>
        /// <item><description>Generates a new GUID if the entity has no ID</description></item>
        /// <item><description>Sets the primary key attribute (entitynameid) if not already present</description></item>
        /// <item><description>Validates the entity</description></item>
        /// <item><description>Adds default attributes and stores the entity</description></item>
        /// <item><description>Processes any related entities and creates associations</description></item>
        /// </list>
        /// </remarks>
        protected internal Guid CreateEntity(Entity e)
        {
            if (e == null)
            {
                throw new InvalidOperationException("The entity must not be null");
            }

            var clone = e.Clone(e.GetType());

            if (clone.Id == Guid.Empty)
            {
                clone.Id = Guid.NewGuid(); // Add default guid if none present
            }

            // Hack for Dynamic Entities where the Id property doesn't populate the "entitynameid" primary key
            var primaryKeyAttribute = $"{e.LogicalName}id";
            if (!clone.Attributes.ContainsKey(primaryKeyAttribute))
            {
                clone[primaryKeyAttribute] = clone.Id;
            }

            ValidateEntity(clone);

            // Create specific validations - thread-safe check
            ConcurrentDictionary<Guid, Entity> existingEntityDict;
            if (clone.Id != Guid.Empty && Data.TryGetValue(clone.LogicalName, out existingEntityDict) &&
                existingEntityDict.ContainsKey(clone.Id))
            {
                throw new InvalidOperationException($"There is already a record of entity {clone.LogicalName} with id {clone.Id}, can't create with this Id.");
            }

            // Check alternate key uniqueness constraints
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
            var violatedKey = FindViolatedAlternateKey(clone);
            if (violatedKey != null)
            {
                var keyDescription = string.Join(", ", violatedKey.KeyAttributes);
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    $"A record that has the attribute values {keyDescription} already exists. The duplicate values are in the following attributes: {keyDescription}.");
            }
#endif

            // Dataverse enforces state/status code rules on Create:
            // - statecode defaults to Active (0) if not provided
            // - Cannot create inactive records (statecode != 0) directly
            // - statuscode must be valid for the state
            // See: https://github.com/DynamicsValue/fake-xrm-easy/issues/479
            if (clone.Attributes.ContainsKey("statecode"))
            {
                // Dataverse behavior: explicitly passing statecode = null is treated as "not provided",
                // and the platform defaults it to Active (0) on Create.
                if (clone["statecode"] == null)
                {
                    clone.Attributes.Remove("statecode");
                }
                else
                {
                    var stateValue = clone.GetAttributeValue<OptionSetValue>("statecode")?.Value
                        ?? (clone["statecode"] is int ? (int)clone["statecode"] : 0);

                    if (stateValue != 0)
                    {
                        throw new FaultException<OrganizationServiceFault>(
                            new OrganizationServiceFault(),
                            $"Cannot create entity '{clone.LogicalName}' with statecode {stateValue}. " +
                            "Dataverse requires records to be created in Active state. " +
                            "To create inactive records, first create with Active state then update the statecode.");
                    }
                }
            }

            AddEntityWithDefaults(clone, false, this.UsePipelineSimulation);

            if (e.RelatedEntities.Count > 0)
            {
                foreach (var relationshipSet in e.RelatedEntities)
                {
                    var relationship = relationshipSet.Key;

                    var entityReferenceCollection = new EntityReferenceCollection();

                    foreach (var relatedEntity in relationshipSet.Value.Entities)
                    {
                        var relatedId = CreateEntity(relatedEntity);
                        entityReferenceCollection.Add(new EntityReference(relatedEntity.LogicalName, relatedId));
                    }

                    if (FakeMessageExecutors.ContainsKey(typeof(AssociateRequest)))
                    {
                        var request = new AssociateRequest
                        {
                            Target = clone.ToEntityReference(),
                            Relationship = relationship,
                            RelatedEntities = entityReferenceCollection
                        };
                        FakeMessageExecutors[typeof(AssociateRequest)].Execute(request, this);
                    }
                    else
                    {
                        throw PullRequestException.NotImplementedOrganizationRequest(typeof(AssociateRequest));
                    }
                }
            }

            return clone.Id;
        }

        /// <summary>
        /// Adds an entity to the in-memory data store with default system attributes.
        /// This method initializes the entity with default values and optionally executes plugin pipeline stages.
        /// </summary>
        /// <param name="e">The entity to add to the data store.</param>
        /// <param name="clone">
        /// When set to <c>true</c>, the entity is cloned before being stored to prevent external modifications.
        /// Defaults to <c>false</c>.
        /// </param>
        /// <param name="usePluginPipeline">
        /// When set to <c>true</c>, the plugin pipeline stages (pre-operation and post-operation) are executed.
        /// Defaults to <c>false</c>.
        /// </param>
        /// <param name="validateEntityType">
        /// When set to <c>true</c>, the method will throw an exception if ProxyTypesAssembly is set and the entity
        /// type cannot be found via reflection. When set to <c>false</c>, late-bound entities are allowed alongside
        /// early-bound entities. Defaults to <c>true</c>.
        /// Fix for GitHub issue #462.
        /// </param>
        /// <remarks>
        /// This method is typically called during entity creation and performs:
        /// <list type="bullet">
        /// <item><description>Addition of default attributes (createdon, createdby, modifiedon, modifiedby, statecode, statuscode)</description></item>
        /// <item><description>Execution of pre-operation synchronous plugins if pipeline simulation is enabled</description></item>
        /// <item><description>Storage of the entity in the data store</description></item>
        /// <item><description>Execution of post-operation synchronous and asynchronous plugins if pipeline simulation is enabled</description></item>
        /// </list>
        /// </remarks>
        protected internal void AddEntityWithDefaults(Entity e, bool clone = false, bool usePluginPipeline = false, bool validateEntityType = true)
        {
            // Create the entity with defaults
            AddEntityDefaultAttributes(e);

            if (usePluginPipeline)
            {
                // For Create pre-operation stages, there's no preImage (entity doesn't exist yet)
                // and no postImage yet (entity not created yet)
                ExecutePipelineStage("Create", ProcessingStepStage.Prevalidation, ProcessingStepMode.Synchronous, e, null, null);
                ExecutePipelineStage("Create", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous, e, null, null);
            }

            // Store
            AddEntity(clone ? e.Clone(e.GetType()) : e, validateEntityType);

            if (usePluginPipeline)
            {
                // For Create post-operation, capture the postImage (entity state after creation)
                // No preImage for Create (entity didn't exist before)
                Entity postImage = null;
                ConcurrentDictionary<Guid, Entity> entityDict;
                Entity storedEntity;
                if (Data.TryGetValue(e.LogicalName, out entityDict) &&
                    entityDict.TryGetValue(e.Id, out storedEntity))
                {
                    postImage = storedEntity.Clone(storedEntity.GetType());
                }

                ExecutePipelineStage("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous, e, null, postImage);
                ExecutePipelineStage("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Asynchronous, e, null, postImage);
            }
        }

        /// <summary>
        /// Adds an entity directly to the in-memory data store without adding default system attributes.
        /// This method handles validation, DateTime conversion, entity reference resolution, and metadata updates.
        /// </summary>
        /// <param name="e">The entity to add to the data store. Must have a valid logical name and ID.</param>
        /// <param name="validateEntityType">
        /// When set to <c>true</c>, the method will throw an exception if ProxyTypesAssembly is set and the entity
        /// type cannot be found via reflection. When set to <c>false</c>, late-bound entities are allowed alongside
        /// early-bound entities, and attribute metadata will be inferred from the entity's attributes.
        /// Defaults to <c>true</c>.
        /// Fix for GitHub issue #462.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the entity is null, has no logical name, or has an empty ID.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when validateEntityType is true, using early-bound types, and the entity type cannot be found via reflection.
        /// </exception>
        /// <remarks>
        /// This method performs the following operations:
        /// <list type="bullet">
        /// <item><description>Automatically detects and sets the ProxyTypesAssembly if an early-bound type is used</description></item>
        /// <item><description>Validates entity properties (logical name and ID)</description></item>
        /// <item><description>Converts DateTime attributes to UTC</description></item>
        /// <item><description>Validates and resolves entity references if ValidateReferences is enabled</description></item>
        /// <item><description>Adds the entity to the appropriate collection in the Data dictionary</description></item>
        /// <item><description>Updates the AttributeMetadataNames dictionary with attribute information</description></item>
        /// </list>
        /// </remarks>
        protected internal void AddEntity(Entity e, bool validateEntityType = true)
        {
            //Automatically detect proxy types assembly if an early bound type was used.
            if (ProxyTypesAssembly == null &&
                e.GetType().IsSubclassOf(typeof(Entity)))
            {
                ProxyTypesAssembly = Assembly.GetAssembly(e.GetType());
            }

            ValidateEntity(e); //Entity must have a logical name and an Id

            foreach (var sAttributeName in e.Attributes.Keys.ToList())
            {
                var attribute = e[sAttributeName];
                if (attribute is DateTime)
                {
                    var dateValue = (DateTime)e[sAttributeName];
                    ValidateDateTime(dateValue, sAttributeName);
                    e[sAttributeName] = ConvertToUtc(dateValue);
                }
                if (attribute is EntityReference && ValidateReferences)
                {
                    var target = (EntityReference)e[sAttributeName];
                    e[sAttributeName] = ResolveEntityReference(target);
                }
            }

            //Add the entity collection - thread-safe using ConcurrentDictionary
            var entityDict = Data.GetOrAdd(e.LogicalName, _ => new ConcurrentDictionary<Guid, Entity>());
            entityDict[e.Id] = e;

            //Update metadata for that entity - thread-safe using ConcurrentDictionary
            var attributeDict = AttributeMetadataNames.GetOrAdd(e.LogicalName, _ => new ConcurrentDictionary<string, string>());

            //Update attribute metadata
            if (ProxyTypesAssembly != null)
            {
                //If the context is using a proxy types assembly then we can just guess the metadata from the generated attributes
                var type = FindReflectedType(e.LogicalName);
                if (type != null)
                {
                    var props = type.GetProperties();
                    foreach (var p in props)
                    {
                        attributeDict.TryAdd(p.Name, p.Name);
                    }
                }
                else if (validateEntityType)
                {
                    // When validateEntityType is true (e.g., service.Create()), throw for unknown entity types
                    // This matches real CRM behavior where unknown entities are rejected
                    throw new Exception(string.Format("Couldnt find reflected type for {0}", e.LogicalName));
                }
                else
                {
                    // When validateEntityType is false (e.g., Initialize()), allow late-bound entities
                    // alongside early-bound entities by inferring metadata from the entity's attributes.
                    // Fix for GitHub issue #462.
                    foreach (var attKey in e.Attributes.Keys)
                    {
                        attributeDict.TryAdd(attKey, attKey);
                    }
                }
            }
            else
            {
                //If dynamic entities are being used, then the only way of guessing if a property exists is just by checking
                //if the entity has the attribute in the dictionary
                foreach (var attKey in e.Attributes.Keys)
                {
                    attributeDict.TryAdd(attKey, attKey);
                }
            }

        }

        /// <summary>
        /// Determines whether an attribute exists in the metadata for a specified entity.
        /// This method checks multiple sources including relationships, early-bound types, and injected entity metadata.
        /// </summary>
        /// <param name="sEntityName">The logical name of the entity.</param>
        /// <param name="sAttributeName">The logical name of the attribute to check.</param>
        /// <returns>
        /// <c>true</c> if the attribute exists in the entity metadata; otherwise, <c>false</c>.
        /// Returns <c>true</c> by default for dynamic entities when no metadata is explicitly configured.
        /// </returns>
        /// <remarks>
        /// The method performs validation in the following order:
        /// <list type="number">
        /// <item><description>Checks if the attribute is part of any defined relationship</description></item>
        /// <item><description>If using early-bound types, checks the type's properties for the attribute</description></item>
        /// <item><description>Falls back to injected entity metadata if available</description></item>
        /// <item><description>Returns true for dynamic entities without explicitly defined metadata</description></item>
        /// </list>
        /// </remarks>
        protected internal bool AttributeExistsInMetadata(string sEntityName, string sAttributeName)
        {
            var relationships = this.Relationships.Values.Where(value => new[] { value.Entity1LogicalName, value.Entity2LogicalName, value.IntersectEntity }.Contains(sEntityName, StringComparer.InvariantCultureIgnoreCase)).ToArray();
            if (relationships.Any(e => e.Entity1Attribute == sAttributeName || e.Entity2Attribute == sAttributeName))
            {
                return true;
            }

            //Early bound types
            if (ProxyTypesAssembly != null)
            {
                //Check if attribute exists in the early bound type 
                var earlyBoundType = FindReflectedType(sEntityName);
                if (earlyBoundType != null)
                {
                    //Get that type properties
                    var attributeFound = earlyBoundType
                        .GetProperties()
                        .Where(pi => pi.GetCustomAttributes(typeof(AttributeLogicalNameAttribute), true).Length > 0)
                        .Where(pi => (pi.GetCustomAttributes(typeof(AttributeLogicalNameAttribute), true)[0] as AttributeLogicalNameAttribute).LogicalName.Equals(sAttributeName))
                        .FirstOrDefault();

                    if (attributeFound != null)
                        return true;

                    // Some attributes (e.g. entityimage) have a constant in a nested class but no
                    // [AttributeLogicalName]-decorated C# property on the proxy class.
                    // Check nested classes for a matching string constant value.
                    // XrmToolkit uses "Properties", XrmToolbox Early Bound Generator uses "Fields".
                    foreach (var nestedType in earlyBoundType.GetNestedTypes())
                    {
                        var constantFound = nestedType
                            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
                            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                            .Any(f => string.Equals((string)f.GetRawConstantValue(), sAttributeName, StringComparison.OrdinalIgnoreCase));

                        if (constantFound)
                            return true;
                    }

                    if (EntityMetadata.ContainsKey(sEntityName))
                    {
                        //Try with metadata
                        return AttributeExistsInInjectedMetadata(sEntityName, sAttributeName);
                    }
                    else
                    {
                        return false;
                    }
                }
                // No early-bound type exists for this logical name in the proxy assembly.
                // In that case we can't use proxy type reflection as a source of truth for attribute existence.
                // Fall back to injected metadata if present; otherwise treat as a dynamic entity (allow attributes).
                if (EntityMetadata.ContainsKey(sEntityName))
                {
                    var metadata = EntityMetadata[sEntityName];
                    if (metadata.Attributes == null || metadata.Attributes.Length == 0)
                    {
                        return true;
                    }

                    return AttributeExistsInInjectedMetadata(sEntityName, sAttributeName);
                }

                return true;
            }

            if (EntityMetadata.ContainsKey(sEntityName))
            {
                // Check if metadata has attributes defined
                var metadata = EntityMetadata[sEntityName];
                if (metadata.Attributes == null || metadata.Attributes.Length == 0)
                {
                    // Metadata exists but no attributes are defined - treat as dynamic entity
                    // This allows tests to set up minimal metadata (e.g., just PrimaryNameAttribute)
                    // without having to define all attributes
                    return true;
                }

                //Try with metadata
                return AttributeExistsInInjectedMetadata(sEntityName, sAttributeName);
            }

            //Dynamic entities and not entity metadata injected for entity => just return true if not found
            return true;
        }

        /// <summary>
        /// Determines whether an attribute exists in the explicitly injected entity metadata.
        /// </summary>
        /// <param name="sEntityName">The logical name of the entity.</param>
        /// <param name="sAttributeName">The logical name of the attribute to check.</param>
        /// <returns>
        /// <c>true</c> if the attribute is found in the injected metadata; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method delegates to <see cref="FindAttributeTypeInInjectedMetadata"/> to locate the attribute
        /// in the EntityMetadata dictionary.
        /// </remarks>
        protected internal bool AttributeExistsInInjectedMetadata(string sEntityName, string sAttributeName)
        {
            var attributeInMetadata = FindAttributeTypeInInjectedMetadata(sEntityName, sAttributeName);
            return attributeInMetadata != null;
        }

        /// <summary>
        /// Converts a DateTime value to UTC matching real Dataverse behavior.
        /// </summary>
        /// <param name="attribute">The DateTime value to convert.</param>
        /// <returns>A DateTime in UTC format.</returns>
        /// <remarks>
        /// Verified against real Dataverse (Issue #491):
        /// - DateTimeKind.Local → Converted to UTC using ToUniversalTime()
        /// - DateTimeKind.Utc → Stored as-is
        /// - DateTimeKind.Unspecified → Treated as UTC (stored raw, marked as UTC)
        /// </remarks>
        protected internal DateTime ConvertToUtc(DateTime attribute)
        {
            switch (attribute.Kind)
            {
                case DateTimeKind.Local:
                    // Dataverse converts Local to UTC
                    return attribute.ToUniversalTime();

                case DateTimeKind.Utc:
                case DateTimeKind.Unspecified:
                default:
                    // Dataverse treats Utc and Unspecified as already UTC
                    return DateTime.SpecifyKind(attribute, DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Validates that a DateTime value is within the range supported by CRM/Dataverse.
        /// </summary>
        /// <param name="dateTime">The DateTime value to validate.</param>
        /// <param name="attributeName">The name of the attribute being validated (for error messages).</param>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the DateTime value is less than the minimum supported date (01/01/1753).
        /// </exception>
        protected void ValidateDateTime(DateTime dateTime, string attributeName)
        {
            if (dateTime < CrmMinDateTime)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    $"Date is less than the minimum value supported by CrmDateTime. Actual value: {dateTime:MM/dd/yyyy HH:mm:ss}, Minimum value supported: 01/01/1753 00:00:00");
            }
        }
        #endregion
    }
}

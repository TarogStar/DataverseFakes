using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Concurrent;

#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements the fake message executor for <see cref="UpsertRequest"/>.
    /// This executor simulates the CRM Upsert operation, which either updates an existing record
    /// or creates a new record if one does not exist with the specified key attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Upsert (Update or Insert) operation is useful for data synchronization scenarios where
    /// you may not know whether a record already exists in the system. The operation uses the entity's
    /// primary key or alternate key attributes to determine whether to perform an update or create.
    /// </para>
    /// <para>
    /// This executor is only available for Dynamics 365 v9.x and later (not available for CRM 2011, 2013, or 2015).
    /// </para>
    /// </remarks>
    public class UpsertRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is an <see cref="UpsertRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpsertRequest;
        }

        /// <summary>
        /// Executes the Upsert request, either updating an existing record or creating a new one based on
        /// whether a matching record exists in the context.
        /// </summary>
        /// <param name="request">The <see cref="UpsertRequest"/> containing the target entity to upsert.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> providing the in-memory CRM context for the operation.</param>
        /// <returns>
        /// An <see cref="UpsertResponse"/> containing:
        /// <list type="bullet">
        ///   <item><description><c>RecordCreated</c> - A boolean indicating whether a new record was created (<c>true</c>) or an existing record was updated (<c>false</c>).</description></item>
        ///   <item><description><c>Target</c> - An EntityReference to the created or updated record.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// The executor determines whether to create or update by checking if a record with the same
        /// primary key or alternate key attributes already exists in the context's data store.
        /// If a match is found, an Update operation is performed; otherwise, a Create operation is executed.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var upsertRequest = (UpsertRequest)request;
            bool recordCreated;

            var service = ctx.GetOrganizationService();

            var entityLogicalName = upsertRequest.Target.LogicalName;
            var entityId = ctx.GetRecordUniqueId(upsertRequest.Target.ToEntityReferenceWithKeyAttributes(), validate: false);

            ConcurrentDictionary<Guid, Entity> entityDict;
            if (ctx.Data.TryGetValue(entityLogicalName, out entityDict) &&
                entityDict.ContainsKey(entityId))
            {
                recordCreated = false;
                service.Update(upsertRequest.Target);
            }
            else
            {
                recordCreated = true;

                // Issue #566: Copy KeyAttributes to Attributes when creating a new record
                // Per Microsoft docs: "If there's no alternate key data in Entity.Attributes collection,
                // the alternate key data from Entity.KeyAttributes are copied into Entity.Attributes"
                foreach (var keyAttr in upsertRequest.Target.KeyAttributes)
                {
                    if (!upsertRequest.Target.Attributes.ContainsKey(keyAttr.Key))
                    {
                        upsertRequest.Target.Attributes[keyAttr.Key] = keyAttr.Value;
                    }
                }

                entityId = service.Create(upsertRequest.Target);
            }

            var result = new UpsertResponse();
            result.Results.Add("RecordCreated", recordCreated);
            result.Results.Add("Target", new EntityReference(entityLogicalName, entityId));
            return result;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="UpsertRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpsertRequest);
        }
    }
}
#endif

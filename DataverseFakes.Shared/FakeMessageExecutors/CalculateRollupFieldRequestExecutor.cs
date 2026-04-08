using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Executor for CalculateRollupFieldRequest
    /// Simulates the calculation of rollup fields for unit testing purposes.
    ///
    /// IMPORTANT: This implementation provides a simplified mock for rollup field calculations.
    /// In Dynamics 365, rollup fields execute complex queries on related entities with filters,
    /// and aggregate results using functions like SUM, COUNT, MIN, MAX, AVG.
    ///
    /// Testing Approaches:
    ///
    /// 1. SIMPLE APPROACH (Current Default):
    ///    Pre-populate the rollup field value in your test data before calling CalculateRollupField.
    ///    The executor will preserve the existing value, allowing you to test plugin/workflow behavior
    ///    that depends on rollup field values without implementing the full calculation logic.
    ///
    ///    Example:
    ///    var account = new Account {
    ///        Id = accountId,
    ///        Revenue = new Money(50000m) // Pre-set the rollup value
    ///    };
    ///    context.Initialize(new[] { account });
    ///    service.Execute(new CalculateRollupFieldRequest { Target = accountRef, FieldName = "revenue" });
    ///    // The revenue field will remain 50000
    ///
    /// 2. ADVANCED APPROACH (Future Enhancement):
    ///    For tests requiring actual rollup calculation, you could extend this executor by:
    ///    - Defining rollup metadata (source entity, relationship, filters, aggregation type)
    ///    - Implementing the calculation logic to query related records
    ///    - Applying filters and aggregation functions
    ///
    ///    This would require:
    ///    - RollupFieldDefinition metadata class
    ///    - Registration mechanism: context.RegisterRollupField(entityName, fieldName, definition)
    ///    - Query execution against related records in the context
    ///
    ///    See the CLAUDE.md file for guidance on extending message executors.
    /// </summary>
    public class CalculateRollupFieldRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="CalculateRollupFieldRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CalculateRollupFieldRequest;
        }

        /// <summary>
        /// Executes the CalculateRollupField request, simulating the recalculation of a rollup field value.
        /// </summary>
        /// <param name="request">The <see cref="CalculateRollupFieldRequest"/> containing the target entity reference and field name.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> providing the in-memory CRM context for the operation.</param>
        /// <returns>
        /// A <see cref="CalculateRollupFieldResponse"/> containing the entity with its current rollup field value.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when Target or FieldName is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the target entity does not exist in the context.</exception>
        /// <remarks>
        /// <para>
        /// This is a simplified mock implementation that preserves any pre-populated rollup field value
        /// rather than performing actual rollup calculations. For testing scenarios, pre-populate the
        /// rollup field value in your test data before calling this executor.
        /// </para>
        /// <para>
        /// In a real Dynamics 365 system, CalculateRollupField would query related records, apply filters,
        /// and execute aggregation functions (SUM, COUNT, MIN, MAX, AVG) based on the rollup field definition.
        /// </para>
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var rollupRequest = (CalculateRollupFieldRequest)request;

            // Validate required parameters
            if (rollupRequest.Target == null)
            {
                throw new ArgumentNullException("Target", "CalculateRollupFieldRequest requires a Target EntityReference.");
            }

            if (string.IsNullOrWhiteSpace(rollupRequest.FieldName))
            {
                throw new ArgumentNullException("FieldName", "CalculateRollupFieldRequest requires a FieldName.");
            }

            var entityName = rollupRequest.Target.LogicalName;
            var entityId = rollupRequest.Target.Id;
            var fieldName = rollupRequest.FieldName;

            // Verify the entity exists in the context - thread-safe access
            ConcurrentDictionary<Guid, Entity> entityDict;
            Entity entity;
            if (!ctx.Data.TryGetValue(entityName, out entityDict) ||
                !entityDict.TryGetValue(entityId, out entity))
            {
                throw new InvalidOperationException(
                    $"Entity '{entityName}' with Id '{entityId}' does not exist in the context.");
            }

            // Verify the field exists on the entity
            if (!entity.Contains(fieldName))
            {
                // In a real system, rollup fields might not exist yet on the entity
                // We'll allow this and just set a default value
            }

            // SIMPLIFIED MOCK IMPLEMENTATION:
            // This executor does NOT perform actual rollup calculation by default.
            // It preserves any pre-populated value on the entity, allowing tests to control
            // the rollup field value directly.
            //
            // In a real D365 system, CalculateRollupField would:
            // 1. Retrieve rollup field metadata (source entity, relationship, filters, aggregation type)
            // 2. Query related records matching the criteria (e.g., all opportunities for an account)
            // 3. Apply filters (e.g., only opportunities with status = Open)
            // 4. Execute the aggregation function (SUM, COUNT, MIN, MAX, AVG)
            // 5. Update the target entity's rollup field with the calculated value
            //
            // For testing, you have two options:
            // A) Pre-populate the rollup value in test data (recommended for most scenarios)
            // B) Extend this executor with custom calculation logic if needed
            //    (see class documentation for guidance)
            //
            // The entity is returned in the response, matching real D365 behavior

            // Create and return the response
            var response = new CalculateRollupFieldResponse();
            response.Results["Entity"] = entity;

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="CalculateRollupFieldRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CalculateRollupFieldRequest);
        }
    }
}

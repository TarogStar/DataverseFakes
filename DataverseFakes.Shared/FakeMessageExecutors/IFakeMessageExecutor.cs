using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Defines the contract for fake message executors that handle specific CRM organization requests.
    /// Each executor is responsible for processing a single type of <see cref="OrganizationRequest"/>
    /// and returning an appropriate <see cref="OrganizationResponse"/>.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are used by the FakeXrmEasy framework to simulate
    /// the behavior of Dynamics 365/CRM's IOrganizationService.Execute method in unit tests.
    /// Custom executors can be registered using <c>context.AddFakeMessageExecutor()</c>.
    /// </remarks>
    public interface IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The <see cref="OrganizationRequest"/> to evaluate for execution compatibility.</param>
        /// <returns>
        /// <c>true</c> if this executor can process the specified request; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is typically used by the framework to find the appropriate executor
        /// for a given request type during message execution.
        /// </remarks>
        bool CanExecute(OrganizationRequest request);

        /// <summary>
        /// Gets the type of <see cref="OrganizationRequest"/> that this executor is responsible for handling.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of the organization request that this executor processes
        /// (e.g., <c>typeof(CreateRequest)</c>, <c>typeof(UpdateRequest)</c>).
        /// </returns>
        /// <remarks>
        /// This method is used by the framework to register and look up executors
        /// based on request types.
        /// </remarks>
        Type GetResponsibleRequestType();

        /// <summary>
        /// Executes the specified organization request against the faked CRM context.
        /// </summary>
        /// <param name="request">The <see cref="OrganizationRequest"/> to execute.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that provides the in-memory CRM simulation.</param>
        /// <returns>
        /// An <see cref="OrganizationResponse"/> containing the results of the executed request.
        /// The specific response type depends on the request being processed.
        /// </returns>
        /// <remarks>
        /// Implementations should simulate the behavior of the actual CRM operation,
        /// including validation, data manipulation, and appropriate exception handling.
        /// </remarks>
        OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx);
    }
}

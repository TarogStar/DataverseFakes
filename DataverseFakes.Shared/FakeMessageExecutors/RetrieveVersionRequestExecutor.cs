using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using System.Reflection;
using System.Diagnostics;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for RetrieveVersionRequest
    /// </summary>
    public class RetrieveVersionRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is RetrieveVersionRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveVersionRequest;
        }

        /// <summary>
        /// Executes the RetrieveVersionRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>RetrieveVersionResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var assemblyPath = Assembly.GetAssembly(typeof(RetrieveVersionRequest)).Location;
            var versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
            var version = versionInfo.FileVersion;

            return new RetrieveVersionResponse
            {
                Results = new ParameterCollection
                {
                    { "Version", version }
                }
            };
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of RetrieveVersionRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveVersionRequest);
        }
    }
}

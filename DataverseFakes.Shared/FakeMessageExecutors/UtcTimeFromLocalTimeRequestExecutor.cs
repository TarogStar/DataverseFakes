using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for UtcTimeFromLocalTimeRequest.
    /// Converts a local time to UTC time.
    /// Addresses upstream issue #340.
    /// </summary>
    public class UtcTimeFromLocalTimeRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is UtcTimeFromLocalTimeRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UtcTimeFromLocalTimeRequest;
        }

        /// <summary>
        /// Executes the UtcTimeFromLocalTimeRequest by converting local time to UTC
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>UtcTimeFromLocalTimeResponse with the UTC time</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as UtcTimeFromLocalTimeRequest;

            if (req == null)
            {
                throw new InvalidOperationException("Request must be a UtcTimeFromLocalTimeRequest");
            }

            var response = new UtcTimeFromLocalTimeResponse();

            // Convert the local time to UTC using the system timezone
            // In Dynamics, the TimeZoneCode would be used, but we use the local system timezone
            DateTime utcTime;
            if (req.LocalTime.Kind == DateTimeKind.Utc)
            {
                // Already UTC, just return it
                utcTime = req.LocalTime;
            }
            else if (req.LocalTime.Kind == DateTimeKind.Local)
            {
                // Convert from local to UTC
                utcTime = req.LocalTime.ToUniversalTime();
            }
            else
            {
                // Unspecified - treat as local and convert
                utcTime = TimeZoneInfo.ConvertTimeToUtc(req.LocalTime, TimeZoneInfo.Local);
            }

            response["UtcTime"] = utcTime;
            return response;
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of UtcTimeFromLocalTimeRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UtcTimeFromLocalTimeRequest);
        }
    }
}

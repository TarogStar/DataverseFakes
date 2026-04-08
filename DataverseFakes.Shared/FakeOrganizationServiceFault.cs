using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace FakeXrmEasy
{
    /// <summary>
    /// Provides utility methods for simulating organization service faults in tests.
    /// Allows throwing FaultExceptions with specific error codes and messages.
    /// </summary>
    public class FakeOrganizationServiceFault
    {
        /// <summary>
        /// Throws a FaultException with the specified error code and message, simulating a CRM organization service fault.
        /// </summary>
        /// <param name="errorCode">The CRM error code to include in the fault.</param>
        /// <param name="message">The error message to include in the fault.</param>
        /// <exception cref="FaultException{OrganizationServiceFault}">Always thrown with the specified error details.</exception>
        public static void Throw(ErrorCodes errorCode, string message)
        {
            throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault() { ErrorCode = (int)errorCode, Message = message }, message);
        }
    }
}

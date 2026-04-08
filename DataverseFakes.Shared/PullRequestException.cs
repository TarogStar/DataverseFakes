using System;

namespace FakeXrmEasy
{
    /// <summary>
    /// Exception thrown when functionality is not yet implemented in FakeXrmEasy.
    /// Encourages contributors to submit pull requests with the missing implementation.
    /// </summary>
    public class PullRequestException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PullRequestException class with a specified error message.
        /// </summary>
        /// <param name="sMessage">The message describing the missing functionality.</param>
        public PullRequestException(string sMessage) :
            base(string.Format("Exception: {0}. This functionality is not available yet. Please consider contributing to FakeXrmEasy.Community by cloning the repository and issuing a pull request.", sMessage))
        {
        }

        /// <summary>
        /// Creates a PullRequestException for an organization request type that is not yet implemented.
        /// </summary>
        /// <param name="t">The Type of the organization request that is not implemented.</param>
        /// <returns>A PullRequestException with a message describing the missing organization request support.</returns>
        public static PullRequestException NotImplementedOrganizationRequest(Type t)
        {
            return new PullRequestException(string.Format("The organization request type '{0}' is not yet supported... but we DO love pull requests so please feel free to submit one! :)", t.ToString()));
        }

        /// <summary>
        /// Creates a PullRequestException for an organization request type that is only partially implemented.
        /// </summary>
        /// <param name="t">The Type of the organization request that is partially implemented.</param>
        /// <param name="missingImplementation">A description of what functionality is missing.</param>
        /// <returns>A PullRequestException with a message describing the partial implementation.</returns>
        public static PullRequestException PartiallyNotImplementedOrganizationRequest(Type t, string missingImplementation)
        {
            return new PullRequestException(string.Format("The organization request type '{0}' is not yet fully supported... {1}... but we DO love pull requests so please feel free to submit one! :)", t.ToString(), missingImplementation));
        }

        /// <summary>
        /// Creates a PullRequestException for a FetchXML operator that is not yet supported.
        /// </summary>
        /// <param name="op">The name of the FetchXML operator that is not implemented.</param>
        /// <returns>A PullRequestException with a message describing the unsupported operator.</returns>
        public static PullRequestException FetchXmlOperatorNotImplemented(string op)
        {
            return new PullRequestException(string.Format("The fetchxml operator '{0}' is not yet supported... but we DO love pull requests so please feel free to submit one! :)", op));
        }
    }
}

using System;

namespace DataverseFakes
{
    /// <summary>
    /// Thrown by the <c>Assert*</c> extension methods on <see cref="XrmFakedContext"/> when an
    /// expectation about the in-memory data is not met.
    /// </summary>
    /// <remarks>
    /// DataverseFakes is test-runner-neutral and does not depend on xUnit, NUnit, or MSTest, so the
    /// assertion helpers signal failure by throwing this exception. Any test runner reports it as a
    /// failed test, exactly like an assertion from its own framework.
    /// </remarks>
    public class XrmFakedAssertException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XrmFakedAssertException"/> class.
        /// </summary>
        /// <param name="message">A description of the failed expectation.</param>
        public XrmFakedAssertException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XrmFakedAssertException"/> class.
        /// </summary>
        /// <param name="message">A description of the failed expectation.</param>
        /// <param name="innerException">The underlying exception that caused the failure.</param>
        public XrmFakedAssertException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

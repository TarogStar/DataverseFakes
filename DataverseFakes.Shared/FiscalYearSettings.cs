using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;

namespace FakeXrmEasy
{
    /// <summary>
    /// Represents the fiscal year settings for an organization in Dynamics 365.
    /// Maps to the organization entity's fiscal calendar configuration.
    /// </summary>
    [EntityLogicalName("organization")]
    public class FiscalYearSettings
    {
        /// <summary>
        /// Gets or sets the start date of the fiscal calendar year.
        /// </summary>
        [AttributeLogicalName("fiscalcalendarstart")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the fiscal period template that defines how the fiscal year is divided.
        /// </summary>
        [AttributeLogicalName("fiscalperiodtype")]
        public Template FiscalPeriodTemplate { get; set; }

        /// <summary>
        /// Defines the available fiscal period templates for dividing the fiscal year.
        /// </summary>
        public enum Template
        {
            /// <summary>
            /// Fiscal year is divided into one annual period.
            /// </summary>
            Annually = 2000,

            /// <summary>
            /// Fiscal year is divided into two semi-annual periods.
            /// </summary>
            SemiAnnually = 2001,

            /// <summary>
            /// Fiscal year is divided into four quarterly periods.
            /// </summary>
            Quarterly = 2002,

            /// <summary>
            /// Fiscal year is divided into twelve monthly periods.
            /// </summary>
            Monthly = 2003,

            /// <summary>
            /// Fiscal year is divided into thirteen four-week periods.
            /// </summary>
            FourWeek = 2004
        }
    }
}

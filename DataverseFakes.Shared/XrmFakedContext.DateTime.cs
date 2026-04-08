using FakeXrmEasy.Metadata;
using System;
using System.Collections.Generic;

namespace FakeXrmEasy
{
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// Gets or sets the system time zone for the faked context
        /// </summary>
        public TimeZoneInfo SystemTimeZone { get; set; }

        /// <summary>
        /// Gets or sets the fiscal year settings for the faked context
        /// </summary>
        public FiscalYearSettings FiscalYearSettings { get; set; }

        /// <summary>
        /// Gets or sets the date behavior for entity attributes
        /// </summary>
        public Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>> DateBehaviour { get; set; }

        private static Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>> DefaultDateBehaviour()
        {
#if FAKE_XRM_EASY || FAKE_XRM_EASY_2013
            return new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>();
#else
            return new Dictionary<string, Dictionary<string, DateTimeAttributeBehavior>>
            {
                {
                    "contact", new Dictionary<string, DateTimeAttributeBehavior>
                    {
                        { "anniversary", DateTimeAttributeBehavior.DateOnly },
                        { "birthdate", DateTimeAttributeBehavior.DateOnly }
                    }
                },
                {
                    "invoice", new Dictionary<string, DateTimeAttributeBehavior>
                    {
                        { "duedate", DateTimeAttributeBehavior.DateOnly }
                    }
                },
                {
                    "lead", new Dictionary<string, DateTimeAttributeBehavior>
                    {
                        { "estimatedclosedate", DateTimeAttributeBehavior.DateOnly }
                    }
                },
                {
                    "opportunity", new Dictionary<string, DateTimeAttributeBehavior>
                    {
                        { "actualclosedate", DateTimeAttributeBehavior.DateOnly },
                        { "estimatedclosedate", DateTimeAttributeBehavior.DateOnly },
                        { "finaldecisiondate", DateTimeAttributeBehavior.DateOnly }
                    }
                },
                {
                    "product", new Dictionary<string, DateTimeAttributeBehavior>
                    {
                        { "validfromdate", DateTimeAttributeBehavior.DateOnly },
                        { "validtodate", DateTimeAttributeBehavior.DateOnly }
                    }
                },
                {
                    "quote", new Dictionary<string, DateTimeAttributeBehavior>
                    {
                        { "closedon", DateTimeAttributeBehavior.DateOnly },
                        { "dueby", DateTimeAttributeBehavior.DateOnly }
                    }
                }
            };
#endif
        }
    }
}
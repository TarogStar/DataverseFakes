using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace FakeXrmEasy.Services
{
    /// <summary>
    /// Ensures invoice entities mimic Dynamics 365 auto-numbering behavior.
    /// </summary>
    public class InvoiceInitializerService : IEntityInitializerService
    {
        /// <summary>
        /// Logical name for invoice entities.
        /// </summary>
        public const string EntityLogicalName = "invoice";

        /// <inheritdoc />
        public Entity Initialize(Entity e, Guid gCallerId, XrmFakedContext ctx, bool isManyToManyRelationshipEntity = false)
        {
            if (string.IsNullOrEmpty(e.GetAttributeValue<string>("invoicenumber")))
            {
                //first FakeXrmEasy auto-numbering emulation
                e["invoicenumber"] = "INV-" + DateTime.Now.Ticks;
            }

            return e;
        }

        /// <inheritdoc />
        public Entity Initialize(Entity e, XrmFakedContext ctx, bool isManyToManyRelationshipEntity = false)
        {
            return this.Initialize(e, Guid.NewGuid(), ctx, isManyToManyRelationshipEntity);
        }
    }
}

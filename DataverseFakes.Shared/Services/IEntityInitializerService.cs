using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Services
{
    /// <summary>
    /// Provides hooks to populate default attribute values before an entity is stored.
    /// </summary>
    public interface IEntityInitializerService
    {
        /// <summary>
        /// Initializes the supplied entity using the context defaults.
        /// </summary>
        /// <param name="e">The entity instance being initialized.</param>
        /// <param name="ctx">The fake context that supplies metadata and helpers.</param>
        /// <param name="isManyToManyRelationshipEntity">True when the entity represents an intersect record.</param>
        /// <returns>The same entity instance with default values applied.</returns>
        Entity Initialize(Entity e, XrmFakedContext ctx, bool isManyToManyRelationshipEntity = false);

        /// <summary>
        /// Initializes the entity while honoring the provided caller identifier.
        /// </summary>
        /// <param name="e">The entity instance being initialized.</param>
        /// <param name="gCallerId">Caller identifier to stamp onto created/modified fields.</param>
        /// <param name="ctx">The fake context that supplies metadata and helpers.</param>
        /// <param name="isManyToManyRelationshipEntity">True when the entity represents an intersect record.</param>
        /// <returns>The same entity instance with default values applied.</returns>
        Entity Initialize(Entity e, Guid gCallerId, XrmFakedContext ctx, bool isManyToManyRelationshipEntity = false);
    }
}

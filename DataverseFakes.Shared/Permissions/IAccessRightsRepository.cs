using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Permissions
{
    /// <summary>
    /// Contract that abstracts access right CRUD operations used by the fake security model.
    /// </summary>
    public interface IAccessRightsRepository
    {
        /// <summary>
        /// Grants the specified rights to the security principal (user or team) for the specified record.
        /// </summary>
        /// <param name="er">The record to which permissions should be granted.</param>
        /// <param name="pa">The access mask and principal being granted rights.</param>
        void GrantAccessTo(EntityReference er, PrincipalAccess pa);

        /// <summary>
        /// Revokes the specified rights to the security principal (user or team) for the specified record.
        /// </summary>
        /// <param name="er">The record from which rights should be revoked.</param>
        /// <param name="principal">The principal whose access will be revoked.</param>
        void RevokeAccessTo(EntityReference er, EntityReference principal);

        /// <summary>
        /// Retrieves the access mask for a specific security principal and record.
        /// </summary>
        /// <param name="er">The record being queried.</param>
        /// <param name="principal">The principal whose rights should be returned.</param>
        /// <returns>A response that includes the effective <see cref="AccessRights"/> mask.</returns>
        RetrievePrincipalAccessResponse RetrievePrincipalAccess(EntityReference er, EntityReference principal);

        /// <summary>
        /// Retrieves the list of permitted security principals (user or team) that have access to the given record
        /// </summary>
        /// <param name="er">The record from which we want the list of principals.</param>
        /// <returns>A response containing all principals with their access masks.</returns>
        RetrieveSharedPrincipalsAndAccessResponse RetrieveSharedPrincipalsAndAccess(EntityReference er);

        /// <summary>
        /// Retrieves all principals (security principals) who have any access to the specified record
        /// </summary>
        /// <param name="er">The record whose access list should be retrieved.</param>
        void GetAllPrincipalAccessFor(EntityReference er);

        /// <summary>
        /// Modify the access rights for a specific entity
        /// </summary>
        /// <param name="er">The entity reference whose permissions should change.</param>
        /// <param name="pa">The new access rights to store for the principal.</param>
        void ModifyAccessOn(EntityReference er, PrincipalAccess pa);
    }
}
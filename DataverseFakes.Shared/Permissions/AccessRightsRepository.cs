using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FakeXrmEasy.Permissions
{
    /// <summary>
    /// In-memory repository that stores access rights per record for fake security operations.
    /// </summary>
    public class AccessRightsRepository : IAccessRightsRepository
    {
        private readonly Dictionary<EntityReference, List<PrincipalAccess>> _accessRights;

        /// <summary>
        /// Initializes a new <see cref="AccessRightsRepository"/> instance.
        /// </summary>
        public AccessRightsRepository()
        {
            // One record might be accessed from many security principals
            _accessRights = new Dictionary<EntityReference, List<PrincipalAccess>>();
        }

        /// <summary>
        /// Grants the specified rights to the security principal (user or team) for the specified record
        /// </summary>
        /// <param name="er">The record to which the access rights will be assigned.</param>
        /// <param name="pa">The access mask and principal being granted rights.</param>
        public void GrantAccessTo(EntityReference er, PrincipalAccess pa)
        {
            List<PrincipalAccess> accessList = GetAccessListForRecord(er);
            PrincipalAccess paMatch = accessList.Where(p => p.Principal.Id == pa.Principal.Id).SingleOrDefault();
            if (paMatch == null)
                accessList.Add(pa);
        }

        /// <summary>
        /// Modify access on a specific record
        /// </summary>
        /// <param name="er">The entity for which we are modifying permissions.</param>
        /// <param name="pa">The new access rights that should overwrite the current permissions.</param>
        public void ModifyAccessOn(EntityReference er, PrincipalAccess pa)
        {
            List<PrincipalAccess> accessList = GetAccessListForRecord(er);
            PrincipalAccess paMatch = accessList.Where(p => p.Principal.Id == pa.Principal.Id).SingleOrDefault();
            if (paMatch != null)
            {
                accessList[accessList.IndexOf(paMatch)] = pa;
            }
            else
            {
                accessList.Add(pa);
            }
        }

        /// <summary>
        /// Retrieves the RetrievePrincipalAccessResponse for the specified security principal (user or team) and record.
        /// </summary>
        /// <param name="er">The record whose access rights should be retrieved.</param>
        /// <param name="principal">The principal whose access mask is being queried.</param>
        /// <returns>A response containing the access rights mask for the specified principal and record.</returns>
        public RetrievePrincipalAccessResponse RetrievePrincipalAccess(EntityReference er, EntityReference principal)
        {
            List<PrincipalAccess> accessList = GetAccessListForRecord(er);
            PrincipalAccess pAcc = accessList.Where(pa => pa.Principal.Id == principal.Id).SingleOrDefault();
            RetrievePrincipalAccessResponse resp = new RetrievePrincipalAccessResponse();

            if (pAcc != null)
                resp.Results["AccessRights"] = pAcc.AccessMask;

            return resp;
        }

        /// <summary>
        /// Retrieves the list of permitted security principals (user or team) that have access to the given record
        /// </summary>
        /// <param name="er">The record for which to list all principals with access.</param>
        /// <returns>A response containing the principals and their access masks.</returns>
        public RetrieveSharedPrincipalsAndAccessResponse RetrieveSharedPrincipalsAndAccess(EntityReference er)
        {
            List<PrincipalAccess> accessList = GetAccessListForRecord(er);
            RetrieveSharedPrincipalsAndAccessResponse resp = new RetrieveSharedPrincipalsAndAccessResponse();
            resp.Results["PrincipalAccesses"] = accessList.ToArray();
            return resp;
        }

        /// <summary>
        /// Revokes the specified rights to the security principal (user or team) for the specified record
        /// </summary>
        /// <param name="er">The record from which the access rights will be revoked.</param>
        /// <param name="principal">The principal whose access should be removed.</param>
        public void RevokeAccessTo(EntityReference er, EntityReference principal)
        {
            List<PrincipalAccess> accessList = GetAccessListForRecord(er);

            for (int x = accessList.Count - 1; x >= 0; x--)
            {
                PrincipalAccess pa = accessList[x];
                if (pa.Principal.Id == principal.Id)
                    accessList.RemoveAt(x);
            }
        }

        /// <summary>
        /// Retrieves all principals (security principals) who have any access to the specified record
        /// </summary>
        /// <param name="er">The record whose access list should be retrieved.</param>
        public void GetAllPrincipalAccessFor(EntityReference er)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fetches the List&lt;PrincipalAccess&gt; for the given EntityReference.
        /// </summary>
        /// <param name="er">The record that needs its access list.</param>
        /// <returns>The list of principal access entries for the specified record, creating one if it does not exist.</returns>
        private List<PrincipalAccess> GetAccessListForRecord(EntityReference er)
        {
            List<PrincipalAccess> accessList = null;
            if (!_accessRights.TryGetValue(er, out accessList))
            {
                accessList = new List<PrincipalAccess>();
                _accessRights.Add(er, accessList);
            }

            return accessList;
        }
    }
}
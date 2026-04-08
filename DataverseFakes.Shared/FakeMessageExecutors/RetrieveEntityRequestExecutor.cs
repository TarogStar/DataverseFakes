using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Xrm.Sdk.Client;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="RetrieveEntityRequest"/> messages.
    /// Retrieves entity metadata from the faked CRM context's metadata cache.
    /// </summary>
    public class RetrieveEntityRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="RetrieveEntityRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveEntityRequest;
        }

        /// <summary>
        /// Gets the early-bound proxy type for the specified entity from the proxy types assembly.
        /// </summary>
        /// <param name="entityName">The logical name of the entity to find the proxy type for.</param>
        /// <param name="ctx">The faked XRM context containing the proxy types assembly.</param>
        /// <returns>The <see cref="Type"/> representing the early-bound entity class.</returns>
        /// <exception cref="Exception">Thrown when the entity is not found in the proxy types assembly.</exception>
        public static Type GetEntityProxyType(string entityName, XrmFakedContext ctx)
        {
            //Find the reflected type in the proxy types assembly
            var assembly = ctx.ProxyTypesAssembly;
            var subClassType = assembly.GetTypes()
                    .Where(t => typeof(Entity).IsAssignableFrom(t))
                    .Where(t => t.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true).Length > 0)
                    .Where(t => ((EntityLogicalNameAttribute)t.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true)[0]).LogicalName.Equals(entityName.ToLower()))
                    .FirstOrDefault();

            if (subClassType == null)
            {
                throw new Exception(string.Format("Entity {0} was not found in the proxy types", entityName));
            }
            return subClassType;
        }

        /// <summary>
        /// Executes the <see cref="RetrieveEntityRequest"/> and returns the corresponding entity metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="RetrieveEntityRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>
        /// A <see cref="RetrieveEntityResponse"/> containing the <see cref="EntityMetadata"/>
        /// for the requested entity in the Results collection.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when the LogicalName property is not specified in the request,
        /// when the entity is not found in the metadata cache,
        /// or when neither EntityFilters.Entity nor EntityFilters.Attributes is specified.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as RetrieveEntityRequest;

            if (string.IsNullOrWhiteSpace(req.LogicalName))
            {
                throw new Exception("A logical name property must be specified in the request");
            }

            // HasFlag -> used to verify flag matches --> to verify EntityFilters.Entity | EntityFilters.Attributes
            if (req.EntityFilters.HasFlag(Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity) ||
                req.EntityFilters.HasFlag(Microsoft.Xrm.Sdk.Metadata.EntityFilters.Attributes))
            {
                if(!ctx.EntityMetadata.ContainsKey(req.LogicalName))
                {
                    throw new Exception($"Entity '{req.LogicalName}' is not found in the metadata cache");
                }

                var entityMetadata = ctx.GetEntityMetadataByName(req.LogicalName);

                var response = new RetrieveEntityResponse()
                {
                    Results = new ParameterCollection
                        {
                            { "EntityMetadata", entityMetadata }
                        }
                };

                return response;
            }

            throw new Exception("At least EntityFilters.Entity or EntityFilters.Attributes must be present on EntityFilters of Request.");
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="RetrieveEntityRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveEntityRequest);
        }
    }
}

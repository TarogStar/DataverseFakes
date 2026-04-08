using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements the fake message executor for <see cref="InitializeFromRequest"/>.
    /// This executor simulates the CRM InitializeFrom operation, which creates a new entity record
    /// pre-populated with values mapped from an existing source entity based on entity mappings.
    /// </summary>
    /// <remarks>
    /// The InitializeFrom operation is commonly used in Dynamics 365 to create related records
    /// with pre-populated fields, such as creating a Quote from an Opportunity or an Order from a Quote.
    /// This executor queries the entitymap and attributemap entities to determine which fields
    /// should be copied from the source to the target entity.
    /// </remarks>
    public class InitializeFromRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is an <see cref="InitializeFromRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is InitializeFromRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="InitializeFromRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(InitializeFromRequest);
        }

        /// <summary>
        /// Executes the InitializeFrom request, creating a new entity pre-populated with mapped attribute values
        /// from the source entity.
        /// </summary>
        /// <param name="request">The <see cref="InitializeFromRequest"/> containing the source entity reference and target entity name.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> providing the in-memory CRM context for the operation.</param>
        /// <returns>
        /// An <see cref="InitializeFromResponse"/> containing the newly created entity with mapped attributes.
        /// The entity has an empty GUID as its Id since it has not yet been saved to the database.
        /// </returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when the request is null or cannot be cast to InitializeFromRequest.</exception>
        /// <exception cref="PullRequestException">Thrown when TargetFieldType is not set to All, as other field type filtering is not yet implemented.</exception>
        /// <remarks>
        /// This method queries the entitymap and attributemap entities to find attribute mappings between the source
        /// and target entities. If proxy types are configured, the returned entity will be an instance of the
        /// appropriate early-bound class. Entity reference fields are automatically converted from GUID values
        /// to proper EntityReference objects.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as InitializeFromRequest;
            if (req == null)
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Cannot execute InitializeFromRequest without the request");

            //TODO: Implement logic to filter mapping attributes based on the req.TargetFieldType
            if (req.TargetFieldType != TargetFieldType.All)
                throw PullRequestException.PartiallyNotImplementedOrganizationRequest(req.GetType(), "logic for filtering attributes based on TargetFieldType other than All is missing");

            var service = ctx.GetOrganizationService();
            var fetchXml = string.Format(FetchMappingsByEntity, req.EntityMoniker.LogicalName, req.TargetEntityName);
            var mapping = service.RetrieveMultiple(new FetchExpression(fetchXml));
            var sourceAttributes = mapping.Entities.Select(a => a.GetAttributeValue<AliasedValue>("attributemap.sourceattributename").Value.ToString()).ToArray();
            var columnSet = sourceAttributes.Length == 0 ? new ColumnSet(true) : new ColumnSet(sourceAttributes);
            var source = service.Retrieve(req.EntityMoniker.LogicalName, req.EntityMoniker.Id, columnSet);

            // If we are using proxy types, and the appropriate proxy type is found in 
            // the assembly create an instance of the appropiate class
            // Othersise return a simple Entity
            Entity entity = new Entity
            {
                LogicalName = req.TargetEntityName,
                Id = Guid.Empty
            };

            if (ctx.ProxyTypesAssembly != null)
            {                
                var subClassType = ctx.FindReflectedType(req.TargetEntityName);
                if (subClassType != null)
                {
                    var instance = Activator.CreateInstance(subClassType);
                    entity = (Entity) instance;                    
                }
            }

            if (mapping.Entities.Count > 0)
            {
                foreach (var attr in source.Attributes)
                {
                    var mappingEntity = mapping.Entities.FirstOrDefault(e => e.GetAttributeValue<AliasedValue>("attributemap.sourceattributename").Value.ToString() == attr.Key);
                    if (mappingEntity == null) continue;
                    var targetAttribute = mappingEntity.GetAttributeValue<AliasedValue>("attributemap.targetattributename").Value.ToString();
                    entity[targetAttribute] = attr.Value;

                    var isEntityReference = string.Equals(attr.Key, source.LogicalName + "id", StringComparison.CurrentCultureIgnoreCase);
                    if (isEntityReference)
                    {
                        entity[targetAttribute] = new EntityReference(source.LogicalName, (Guid)attr.Value);
                    }
                    else
                    {
                        entity[targetAttribute] = attr.Value;
                    }
                }
            }

            var response = new InitializeFromResponse
            {
                Results =
                {
                    ["Entity"] = entity
                }
            };

            return response;
        }

        private const string FetchMappingsByEntity = @"<fetch version='1.0' mapping='logical' distinct='false'>
                                                           <entity name='entitymap'>
                                                              <attribute name='sourceentityname'/>
                                                              <attribute name='targetentityname'/>
                                                              <link-entity name='attributemap' alias='attributemap' to='entitymapid' from='entitymapid' link-type='inner'>
                                                                 <attribute name='sourceattributename'/>
                                                                 <attribute name='targetattributename'/>
                                                              </link-entity>
                                                              <filter type='and'>
                                                                 <condition attribute='sourceentityname' operator='eq' value='{0}' />
                                                                 <condition attribute='targetentityname' operator='eq' value='{1}' />
                                                              </filter>
                                                           </entity>
                                                        </fetch>";
    }
}
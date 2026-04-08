using System;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.FakeMessageExecutors.CustomExecutors
{
    /// <summary>
    /// It will navigate to next Entity in Workflow path and add next Stage Id to traversed path.
    /// 
    /// Additional links:
    /// https://www.magnetismsolutions.com/blog/gayanperera/2016/02/19/programmatically-move-cross-entity-business-process-flow-stages-in-crm-2016
    /// https://community.dynamics.com/crm/b/magnetismsolutionscrmblog/archive/2016/02/19/programmatically-move-cross-entity-business-process-flow-stages-in-crm-2016
    /// https://crmtipoftheday.com/589/programmatically-move-cross-entity-business-process-flow-stages-in-crm-2016/
    /// </summary>
    public class NavigateToNextEntityOrganizationRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// The request name for NavigateToNextEntity
        /// </summary>
        public static readonly string RequestName = "NavigateToNextEntity";

        /// <summary>
        /// Required parameter - Workflow Id
        /// </summary>
        public static readonly string ParameterProcessId = "ProcessId"; // Workflow Id - Guid
        /// <summary>
        /// Required parameter - ProcessStage Id
        /// </summary>
        public static readonly string ParameterNewActiveStageId = "NewActiveStageId"; // ProcessStage Id - Guid

        /// <summary>
        /// Required parameter - Current entity logical name
        /// </summary>
        public static readonly string ParameterCurrentEntityLogicalName = "CurrentEntityLogicalName"; // string
        /// <summary>
        /// Required parameter - Current entity Id
        /// </summary>
        public static readonly string ParameterCurrentEntityId = "CurrentEntityId"; // Guid

        /// <summary>
        /// Required parameter - Next entity logical name
        /// </summary>
        public static readonly string ParameterNextEntityLogicalName = "NextEntityLogicalName"; // string
        /// <summary>
        /// Required parameter - Next entity Id
        /// </summary>
        public static readonly string ParameterNextEntityId = "NextEntityId"; // Guid

        /// <summary>
        /// Required parameter - New traversed path
        /// </summary>
        public static readonly string ParameterNewTraversedPath = "NewTraversedPath"; // string
        /// <summary>
        /// Required parameter - Traversed path
        /// </summary>
        public static readonly string ParameterTraversedPath = "TraversedPath"; // string

        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request name is NavigateToNextEntity</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            // Since it is a custom OrganizationRequest it can only be execute if the Request Name is correct.
            return request.RequestName.Equals(RequestName);
        }

        /// <summary>
        /// Executes the NavigateToNextEntity request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>OrganizationResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var orgService = ctx.GetOrganizationService();

            // Checking parameters

            Guid processId = (Guid)request.Parameters[ParameterProcessId]; // Workflow Id
            if (processId == null) throw new Exception(ParameterProcessId + " is a required parameter.");

            Guid newActiveStageId = (Guid)request.Parameters[ParameterNewActiveStageId];
            if (newActiveStageId == null) throw new Exception(ParameterNewActiveStageId + " is a required parameter.");

            string currentEntityLogicalName = (string)request.Parameters[ParameterCurrentEntityLogicalName];
            if (currentEntityLogicalName == null) throw new Exception(ParameterCurrentEntityLogicalName + " is a required parameter.");

            Guid currentEntityId = (Guid)request.Parameters[ParameterCurrentEntityId];
            if (currentEntityId == null) throw new Exception(ParameterCurrentEntityId + " is a required parameter.");

            string nextEntityLogicalName = (string)request.Parameters[ParameterNextEntityLogicalName];
            if (nextEntityLogicalName == null) throw new Exception(ParameterNextEntityLogicalName + " is a required parameter.");

            Guid nextEntityId = (Guid)request.Parameters[ParameterNextEntityId];
            if (nextEntityId == null) throw new Exception(ParameterNextEntityId + " is a required parameter.");

            string traversedPath = (string)request.Parameters[ParameterNewTraversedPath];

            // Actual request logic

            // All current Entities (should be only one)
            var currentEntities = (from c in ctx.CreateQuery(currentEntityLogicalName)
                                   where c.Id == currentEntityId
                                   select c);

            if (!currentEntities.Any() && currentEntities.Count() != 1) throw new Exception(string.Format("There are no or more than one {0} with Id {1}", currentEntityLogicalName, currentEntityId));

            // Current Entity
            var currentEntity = currentEntities.First();
            currentEntity["stageid"] = newActiveStageId;
            currentEntity["processid"] = processId;
            currentEntity["traversedpath"] = traversedPath;

            orgService.Update(currentEntity);

            // All next Entities (should be only one)
            var nextEntities = (from n in ctx.CreateQuery(nextEntityLogicalName)
                                where n.Id == nextEntityId
                                select n);

            if (!nextEntities.Any() && nextEntities.Count() != 1) throw new Exception(string.Format("There are no or more than one {0} with Id {1}", nextEntityLogicalName, nextEntityId));

            // Next Entity
            var nextEntity = nextEntities.First();
            nextEntity["stageid"] = newActiveStageId;
            nextEntity["processid"] = processId;
            nextEntity["traversedpath"] = traversedPath;

            orgService.Update(nextEntity);

            // Response
            var response = new OrganizationResponse()
            {
                // Response name should be equal with Request name to check if the response is corrent.
                ResponseName = RequestName,
                Results = new ParameterCollection()
            };

            // Add TraversedPath parameter
            response.Results[ParameterTraversedPath] = traversedPath;

            return response;
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of OrganizationRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(OrganizationRequest);
        }
    }
}
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements a fake message executor for the AddMemberListRequest CRM message.
    /// This executor handles adding a single member (account, contact, or lead) to a marketing list
    /// in the faked CRM context by creating a listmember association record.
    /// </summary>
    public class AddMemberListRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Specifies the entity type code values that indicate which entity type
        /// a marketing list was created from. The marketing list can only contain
        /// members of this entity type.
        /// </summary>
        public enum ListCreatedFromCode
        {
            /// <summary>
            /// The marketing list contains account records.
            /// </summary>
            Account = 1,

            /// <summary>
            /// The marketing list contains contact records.
            /// </summary>
            Contact = 2,

            /// <summary>
            /// The marketing list contains lead records.
            /// </summary>
            Lead = 4
        }

        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is an AddMemberListRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is AddMemberListRequest;
        }

        /// <summary>
        /// Executes the AddMemberListRequest, adding a single member to a marketing list in the faked CRM context.
        /// This method validates the list and member existence, verifies the member type matches the list's
        /// CreatedFromCode attribute, and creates a listmember association record.
        /// </summary>
        /// <param name="request">The AddMemberListRequest containing the list ID and entity ID of the member to add.</param>
        /// <param name="ctx">The faked XRM context containing the in-memory CRM data.</param>
        /// <returns>An AddMemberListResponse indicating successful completion of the add member operation.</returns>
        /// <exception cref="FakeOrganizationServiceFault">
        /// Thrown when the ListId or EntityId is empty, the marketing list cannot be found,
        /// the list does not have a valid CreatedFromCode attribute, or the member entity cannot be found.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = (AddMemberListRequest)request;

            if ( req.ListId == Guid.Empty)
            {
				FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "ListId parameter is required");
            }

            if ( req.EntityId == Guid.Empty)
            {
				FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "EntityId parameter is required");
            }

            var service = ctx.GetOrganizationService();

            //Find the list
            var list = ctx.CreateQuery("list")
                        .Where(e => e.Id == req.ListId)
                        .FirstOrDefault();

            if (list == null)
            {
				FakeOrganizationServiceFault.Throw(ErrorCodes.IsvAborted, string.Format("List with Id {0} wasn't found", req.ListId.ToString()));
            }

            //Find the member
            if (!list.Attributes.ContainsKey("createdfromcode"))
            {
				FakeOrganizationServiceFault.Throw(ErrorCodes.IsvUnExpected, string.Format("List with Id {0} must have a CreatedFromCode attribute defined and it has to be an option set value.", req.ListId.ToString()));
            }

            if (list["createdfromcode"] != null && !(list["createdfromcode"] is OptionSetValue))
            {
				FakeOrganizationServiceFault.Throw(ErrorCodes.IsvUnExpected, string.Format("List with Id {0} must have a CreatedFromCode attribute defined and it has to be an option set value.", req.ListId.ToString()));
            }

            var createdFromCodeValue = (list["createdfromcode"] as OptionSetValue).Value;
            string memberEntityName = "";
            switch (createdFromCodeValue)
            {
                case (int)ListCreatedFromCode.Account:
                    memberEntityName = "account";
                    break;

                case (int)ListCreatedFromCode.Contact:
                    memberEntityName = "contact";
                    break;

                case (int)ListCreatedFromCode.Lead:
                    memberEntityName = "lead";
                    break;

                default:
					FakeOrganizationServiceFault.Throw(ErrorCodes.IsvUnExpected, string.Format("List with Id {0} must have a supported CreatedFromCode value (Account, Contact or Lead).", req.ListId.ToString()));
					break;
			}

            var member = ctx.CreateQuery(memberEntityName)
                        .Where(e => e.Id == req.EntityId)
                        .FirstOrDefault();

            if (member == null)
            {
				FakeOrganizationServiceFault.Throw(ErrorCodes.IsvAborted, string.Format("Member with Id {0} wasn't found", req.EntityId.ToString()));
            }

            //create member list
            var listmember = new Entity("listmember");
            listmember["listid"] = new EntityReference("list", req.ListId);
            listmember["entityid"] = new EntityReference(memberEntityName, req.EntityId);
            service.Create(listmember);

            return new AddMemberListResponse();
        }

        /// <summary>
        /// Gets the type of CRM request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of AddMemberListRequest, indicating this executor handles AddMemberListRequest messages.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(AddMemberListRequest);
        }
    }
}
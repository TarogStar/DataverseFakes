using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements the fake message executor for <see cref="FetchXmlToQueryExpressionRequest"/>.
    /// This executor converts a FetchXML query string into an equivalent <see cref="QueryExpression"/> object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FetchXML and QueryExpression are the two primary query languages in Dynamics 365. While FetchXML
    /// is an XML-based query language that supports aggregation and grouping, QueryExpression is an
    /// object-oriented query model. This executor enables conversion between these formats for testing
    /// scenarios that involve query transformation.
    /// </para>
    /// <para>
    /// Note that not all FetchXML features may be fully supported in the conversion, particularly
    /// advanced features like aggregation, distinct, and paging cookies. The conversion is performed
    /// using the XrmFakedContext's internal translation methods.
    /// </para>
    /// </remarks>
    public class FetchXmlToQueryExpressionRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="FetchXmlToQueryExpressionRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is FetchXmlToQueryExpressionRequest;
        }

        /// <summary>
        /// Executes the FetchXmlToQueryExpression request, converting the FetchXML string to a QueryExpression.
        /// </summary>
        /// <param name="request">The <see cref="FetchXmlToQueryExpressionRequest"/> containing the FetchXML string to convert.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> providing the in-memory CRM context for the operation.</param>
        /// <returns>
        /// A <see cref="FetchXmlToQueryExpressionResponse"/> containing the converted <see cref="QueryExpression"/>
        /// in the "Query" property of the Results collection.
        /// </returns>
        /// <remarks>
        /// The conversion process first parses the FetchXML string into an XML document, then translates
        /// the document structure into an equivalent QueryExpression object. The resulting QueryExpression
        /// can be used with RetrieveMultiple or further manipulated programmatically.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as FetchXmlToQueryExpressionRequest;
            var service = ctx.GetOrganizationService();
            FetchXmlToQueryExpressionResponse response = new FetchXmlToQueryExpressionResponse();
            response["Query"] = XrmFakedContext.TranslateFetchXmlDocumentToQueryExpression(ctx, XrmFakedContext.ParseFetchXml(req.FetchXml)); ;
            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="FetchXmlToQueryExpressionRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(FetchXmlToQueryExpressionRequest);
        }
    }
}

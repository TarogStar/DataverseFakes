using FakeItEasy;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;

namespace FakeXrmEasy
{
    /// <summary>
    /// Partial class containing workflow code activity execution functionality for the faked CRM context.
    /// Provides methods to execute Dynamics 365 custom workflow activities in an in-memory test environment.
    /// </summary>
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// Returns a workflow context with default properties that can be customized for testing.
        /// The context includes standard properties like Depth, UserId, BusinessUnitId, and empty parameter collections.
        /// </summary>
        /// <returns>A new <see cref="XrmFakedWorkflowContext"/> instance with default values populated.</returns>
        public XrmFakedWorkflowContext GetDefaultWorkflowContext()
        {
            var userId = CallerId?.Id ?? Guid.NewGuid();
            Guid businessUnitId = BusinessUnitId?.Id ?? Guid.NewGuid();

            return new XrmFakedWorkflowContext
            {
                Depth = 1,
                IsExecutingOffline = false,
                MessageName = "Create",
                UserId = userId,
                BusinessUnitId = businessUnitId,
                InitiatingUserId = userId,
                InputParameters = new ParameterCollection(),
                OutputParameters = new ParameterCollection(),
                SharedVariables = new ParameterCollection(),
                PreEntityImages = new EntityImageCollection(),
                PostEntityImages = new EntityImageCollection()
            };
        }

        /// <summary>
        /// Executes a custom workflow code activity against this faked context with the specified input parameters.
        /// Creates a default workflow context and invokes the activity using the WorkflowInvoker.
        /// </summary>
        /// <typeparam name="T">The code activity type to execute. Must inherit from <see cref="CodeActivity"/> and have a parameterless constructor.</typeparam>
        /// <param name="inputs">Dictionary of input parameter names and values to pass to the activity.</param>
        /// <param name="instance">Optional pre-constructed activity instance. If null, a new instance will be created.</param>
        /// <returns>Dictionary containing the output parameter names and values from the activity execution.</returns>
        public IDictionary<string, object> ExecuteCodeActivity<T>(Dictionary<string, object> inputs, T instance = null)
            where T : CodeActivity, new()
        {
            var wfContext = GetDefaultWorkflowContext();
            return this.ExecuteCodeActivity(wfContext, inputs, instance);
        }

        /// <summary>
        /// Executes a custom workflow code activity with a primary entity set in the workflow context.
        /// The primary entity's Id and LogicalName are used to populate PrimaryEntityId and PrimaryEntityName in the context.
        /// </summary>
        /// <typeparam name="T">The code activity type to execute. Must inherit from <see cref="CodeActivity"/> and have a parameterless constructor.</typeparam>
        /// <param name="primaryEntity">The primary entity for the workflow. Its Id and LogicalName will be set in the context.</param>
        /// <param name="inputs">Optional dictionary of input parameter names and values to pass to the activity.</param>
        /// <param name="instance">Optional pre-constructed activity instance. If null, a new instance will be created.</param>
        /// <returns>Dictionary containing the output parameter names and values from the activity execution.</returns>
        public IDictionary<string, object> ExecuteCodeActivity<T>(Entity primaryEntity, Dictionary<string, object> inputs = null, T instance = null)
            where T : CodeActivity, new()
        {
            var wfContext = GetDefaultWorkflowContext();
            wfContext.PrimaryEntityId = primaryEntity.Id;
            wfContext.PrimaryEntityName = primaryEntity.LogicalName;

            if (inputs == null)
            {
                inputs = new Dictionary<string, object>();
            }

            return this.ExecuteCodeActivity(wfContext, inputs, instance);
        }

        /// <summary>
        /// Executes a custom workflow code activity with a fully customized workflow context.
        /// This overload provides complete control over the workflow context properties.
        /// The WorkflowInvoker is configured with ITracingService, IWorkflowContext, IOrganizationServiceFactory,
        /// and IServiceEndpointNotificationService extensions.
        /// </summary>
        /// <typeparam name="T">The code activity type to execute. Must inherit from <see cref="CodeActivity"/> and have a parameterless constructor.</typeparam>
        /// <param name="wfContext">The customized workflow context containing execution details.</param>
        /// <param name="inputs">Optional dictionary of input parameter names and values to pass to the activity.</param>
        /// <param name="instance">Optional pre-constructed activity instance. If null, a new instance will be created.</param>
        /// <returns>Dictionary containing the output parameter names and values from the activity execution.</returns>
        /// <exception cref="TypeLoadException">Thrown when the activity type cannot be loaded, with details about the loading failure.</exception>
        public IDictionary<string, object> ExecuteCodeActivity<T>(XrmFakedWorkflowContext wfContext, Dictionary<string, object> inputs = null, T instance = null)
            where T : CodeActivity, new()
        {
            var debugText = "";
            try
            {
                debugText = "Creating instance..." + Environment.NewLine;
                if (instance == null)
                {
                    instance = new T();
                }
                var invoker = new WorkflowInvoker(instance);
                debugText += "Invoker created" + Environment.NewLine;
                debugText += "Adding extensions..." + Environment.NewLine;
                invoker.Extensions.Add<ITracingService>(() => TracingService);
                invoker.Extensions.Add<IWorkflowContext>(() => wfContext);
                invoker.Extensions.Add(() =>
                {
                    var fakedServiceFactory = A.Fake<IOrganizationServiceFactory>();
                    A.CallTo(() => fakedServiceFactory.CreateOrganizationService(A<Guid?>._)).ReturnsLazily((Guid? g) => GetOrganizationService());
                    return fakedServiceFactory;
                });
                invoker.Extensions.Add<IServiceEndpointNotificationService>(() => GetFakedServiceEndpointNotificationService());

                debugText += "Adding extensions...ok." + Environment.NewLine;
                debugText += "Invoking activity..." + Environment.NewLine;

                if (inputs == null)
                {
                    inputs = new Dictionary<string, object>();
                }

                return invoker.Invoke(inputs);
            }
            catch (TypeLoadException exception)
            {
                var typeName = exception.TypeName != null ? exception.TypeName : "(null)";
                throw new TypeLoadException($"When loading type: {typeName}.{exception.Message}in domain directory: {AppDomain.CurrentDomain.BaseDirectory}\nDebug={debugText}");
            }
        }
    }
}
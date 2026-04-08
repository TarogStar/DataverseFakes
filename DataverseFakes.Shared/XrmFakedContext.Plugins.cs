using FakeItEasy;
using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy
{
    /// <summary>
    /// Partial class containing plugin execution functionality for the faked CRM context.
    /// Provides methods to execute Dynamics 365 plugins in an in-memory test environment.
    /// </summary>
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// Defines the expected parameter types for each CRM message to match real CRM/Dataverse behavior.
        /// This mapping is used to validate that InputParameters contain the correct types before executing plugins.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, Type>> ExpectedParameterTypes = new Dictionary<string, Dictionary<string, Type>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Create"] = new Dictionary<string, Type> { ["Target"] = typeof(Entity) },
            ["Update"] = new Dictionary<string, Type> { ["Target"] = typeof(Entity) },
            ["Delete"] = new Dictionary<string, Type> { ["Target"] = typeof(EntityReference) },
            ["Retrieve"] = new Dictionary<string, Type> { ["Target"] = typeof(EntityReference) },
            ["SetState"] = new Dictionary<string, Type>
            {
                ["EntityMoniker"] = typeof(EntityReference),
                ["State"] = typeof(OptionSetValue),
                ["Status"] = typeof(OptionSetValue)
            },
            ["SetStateDynamicEntity"] = new Dictionary<string, Type>
            {
                ["EntityMoniker"] = typeof(EntityReference),
                ["State"] = typeof(OptionSetValue),
                ["Status"] = typeof(OptionSetValue)
            },
            ["Assign"] = new Dictionary<string, Type>
            {
                ["Target"] = typeof(EntityReference),
                ["Assignee"] = typeof(EntityReference)
            },
            ["Associate"] = new Dictionary<string, Type>
            {
                ["Target"] = typeof(EntityReference)
            },
            ["Disassociate"] = new Dictionary<string, Type>
            {
                ["Target"] = typeof(EntityReference)
            }
        };

        /// <summary>
        /// Gets or sets a value indicating whether plugin parameter type validation is enabled.
        /// When enabled, the framework validates that InputParameters contain the correct types for each message
        /// (e.g., Delete requires EntityReference, Create requires Entity). Default is true.
        /// Set to false if you need backward compatibility with tests that use incorrect parameter types.
        /// </summary>
        public bool ValidatePluginParameterTypes { get; set; } = true;

        /// <summary>
        /// Validates that the InputParameters in the plugin context contain the correct types for the message.
        /// Throws a FaultException if validation fails, matching real CRM/Dataverse behavior.
        /// </summary>
        /// <param name="ctx">The plugin execution context to validate.</param>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when a parameter has an invalid type.</exception>
        protected void ValidatePluginContextParameterTypes(XrmFakedPluginExecutionContext ctx)
        {
            if (!ValidatePluginParameterTypes || ctx == null || string.IsNullOrEmpty(ctx.MessageName))
            {
                return;
            }

            if (!ExpectedParameterTypes.TryGetValue(ctx.MessageName, out var expectedTypes))
            {
                // Unknown message, no validation
                return;
            }

            if (ctx.InputParameters == null)
            {
                return;
            }

            foreach (var expectedParam in expectedTypes)
            {
                var paramName = expectedParam.Key;
                var expectedType = expectedParam.Value;

                if (ctx.InputParameters.Contains(paramName))
                {
                    var actualValue = ctx.InputParameters[paramName];
                    if (actualValue != null && !expectedType.IsInstanceOfType(actualValue))
                    {
                        var actualType = actualValue.GetType();
                        var errorMessage = $"The '{paramName}' parameter for the '{ctx.MessageName}' message must be of type '{expectedType.Name}', but was '{actualType.Name}'. " +
                                          $"This matches real CRM/Dataverse behavior where {ctx.MessageName} expects {expectedType.Name}.";

                        throw new FaultException<OrganizationServiceFault>(
                            new OrganizationServiceFault() { ErrorCode = (int)ErrorCodes.InvalidArgument, Message = errorMessage },
                            errorMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a plugin execution context with default properties that can be customized for testing.
        /// The context includes standard properties like Depth, UserId, BusinessUnitId, and empty parameter collections.
        /// </summary>
        /// <returns>A new <see cref="XrmFakedPluginExecutionContext"/> instance with default values populated.</returns>
        public XrmFakedPluginExecutionContext GetDefaultPluginContext()
        {
            var userId = CallerId?.Id ?? Guid.NewGuid();
            Guid businessUnitId = BusinessUnitId?.Id ?? Guid.NewGuid();

            return new XrmFakedPluginExecutionContext
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
                PostEntityImages = new EntityImageCollection(),
                IsolationMode = 1
            };
        }

        /// <summary>
        /// Creates a faked <see cref="IPluginExecutionContext"/> from the provided faked plugin execution context.
        /// Uses FakeItEasy to mock the interface and populate all properties from the source context.
        /// </summary>
        /// <param name="ctx">The faked plugin execution context containing the property values to use.</param>
        /// <returns>A mocked <see cref="IPluginExecutionContext"/> with all properties configured.</returns>
        protected IPluginExecutionContext GetFakedPluginContext(XrmFakedPluginExecutionContext ctx)
        {
            var context = A.Fake<IPluginExecutionContext4>();

            PopulateExecutionContextPropertiesFromFakedContext(context, ctx);

            A.CallTo(() => context.ParentContext).ReturnsLazily(() => ctx.ParentContext);
            A.CallTo(() => context.Stage).ReturnsLazily(() => ctx.Stage);

            // IPluginExecutionContext2 properties
            A.CallTo(() => context.InitiatingUserAzureActiveDirectoryObjectId)
                .ReturnsLazily(() => ctx.InitiatingUserAzureActiveDirectoryObjectId);
            A.CallTo(() => context.UserAzureActiveDirectoryObjectId)
                .ReturnsLazily(() => ctx.UserAzureActiveDirectoryObjectId);
            A.CallTo(() => context.InitiatingUserApplicationId)
                .ReturnsLazily(() => ctx.InitiatingUserApplicationId);
            A.CallTo(() => context.PortalsContactId)
                .ReturnsLazily(() => ctx.PortalsContactId);
            A.CallTo(() => context.IsPortalsClientCall)
                .ReturnsLazily(() => ctx.IsPortalsClientCall);

            // IPluginExecutionContext3 properties
            A.CallTo(() => ((IPluginExecutionContext3)context).AuthenticatedUserId)
                .ReturnsLazily(() => ctx.AuthenticatedUserId);
            // Note: ParentContextProperties, IsTransactionIntegrationMessage, and image collections
            // are on the concrete XrmFakedPluginExecutionContext class, not in the interface definitions

            return context;
        }

        /// <summary>
        /// Populates a mocked execution context with property values from a faked plugin execution context.
        /// Configures all standard execution context properties including user information, message details,
        /// input/output parameters, and entity images.
        /// </summary>
        /// <param name="context">The mocked execution context to populate with values.</param>
        /// <param name="ctx">The source faked plugin execution context containing the property values.</param>
        protected void PopulateExecutionContextPropertiesFromFakedContext(IExecutionContext context, XrmFakedPluginExecutionContext ctx)
        {
            var newUserId = Guid.NewGuid();

            A.CallTo(() => context.Depth).ReturnsLazily(() => ctx.Depth <= 0 ? 1 : ctx.Depth);
            A.CallTo(() => context.IsExecutingOffline).ReturnsLazily(() => ctx.IsExecutingOffline);
            A.CallTo(() => context.InputParameters).ReturnsLazily(() => ctx.InputParameters);
            A.CallTo(() => context.OutputParameters).ReturnsLazily(() => ctx.OutputParameters);
            A.CallTo(() => context.PreEntityImages).ReturnsLazily(() => ctx.PreEntityImages);
            A.CallTo(() => context.PostEntityImages).ReturnsLazily(() => ctx.PostEntityImages);
            A.CallTo(() => context.MessageName).ReturnsLazily(() => ctx.MessageName);
            A.CallTo(() => context.Mode).ReturnsLazily(() => ctx.Mode);
            A.CallTo(() => context.OrganizationName).ReturnsLazily(() => ctx.OrganizationName);
            A.CallTo(() => context.OrganizationId).ReturnsLazily(() => ctx.OrganizationId);
            A.CallTo(() => context.OwningExtension).ReturnsLazily(() => ctx.OwningExtension);
            A.CallTo(() => context.InitiatingUserId).ReturnsLazily(() => ctx.InitiatingUserId == Guid.Empty ? newUserId : ctx.InitiatingUserId);
            A.CallTo(() => context.UserId).ReturnsLazily(() => ctx.UserId == Guid.Empty ? newUserId : ctx.UserId);
            A.CallTo(() => context.PrimaryEntityId).ReturnsLazily(() => ctx.PrimaryEntityId);
            A.CallTo(() => context.PrimaryEntityName).ReturnsLazily(() => ctx.PrimaryEntityName);
            A.CallTo(() => context.SecondaryEntityName).ReturnsLazily(() => ctx.SecondaryEntityName);
            A.CallTo(() => context.SharedVariables).ReturnsLazily(() => ctx.SharedVariables);
            A.CallTo(() => context.BusinessUnitId).ReturnsLazily(() => ctx.BusinessUnitId);
            A.CallTo(() => context.CorrelationId).ReturnsLazily(() => ctx.CorrelationId);
            A.CallTo(() => context.OperationCreatedOn).ReturnsLazily(() => ctx.OperationCreatedOn);
            A.CallTo(() => context.IsolationMode).ReturnsLazily(() => ctx.IsolationMode);
            A.CallTo(() => context.IsInTransaction).ReturnsLazily(() => ctx.IsInTransaction);


            // Create message will pass an Entity as the target but this is not always true
            // For instance, a Delete request will receive an EntityReference
            if (ctx.InputParameters != null && ctx.InputParameters.ContainsKey("Target"))
            {
                if (ctx.InputParameters["Target"] is Entity)
                {
                    var target = (Entity)ctx.InputParameters["Target"];
                    A.CallTo(() => context.PrimaryEntityId).ReturnsLazily(() => target.Id);
                    A.CallTo(() => context.PrimaryEntityName).ReturnsLazily(() => target.LogicalName);
                }
                else if (ctx.InputParameters["Target"] is EntityReference)
                {
                    var target = (EntityReference)ctx.InputParameters["Target"];
                    A.CallTo(() => context.PrimaryEntityId).ReturnsLazily(() => target.Id);
                    A.CallTo(() => context.PrimaryEntityName).ReturnsLazily(() => target.LogicalName);
                }
            }
        }

        /// <summary>
        /// Creates a faked <see cref="IExecutionContext"/> from the provided faked plugin execution context.
        /// This provides a simpler execution context interface compared to the full plugin execution context.
        /// </summary>
        /// <param name="ctx">The faked plugin execution context containing the property values to use.</param>
        /// <returns>A mocked <see cref="IExecutionContext"/> with properties configured from the source context.</returns>
        protected IExecutionContext GetFakedExecutionContext(XrmFakedPluginExecutionContext ctx)
        {
            var context = A.Fake<IExecutionContext>();

            PopulateExecutionContextPropertiesFromFakedContext(context, ctx);

            return context;
        }

        /// <summary>
        /// Executes a plugin of the specified type with a custom plugin execution context.
        /// This is useful when you need to mock complex plugin contexts with specific values for
        /// MessageName, plugin Depth, InitiatingUserId, and other context properties.
        /// </summary>
        /// <typeparam name="T">The plugin type to execute. Must implement <see cref="IPlugin"/> and have a parameterless constructor.</typeparam>
        /// <param name="ctx">The custom plugin execution context. If null, a default context will be created.</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        public IPlugin ExecutePluginWith<T>(XrmFakedPluginExecutionContext ctx = null)
            where T : IPlugin, new()
        {
            if (ctx == null)
            {
                ctx = GetDefaultPluginContext();
            }

            return this.ExecutePluginWith(ctx, new T());
        }

        /// <summary>
        /// Executes a specific plugin instance with a custom plugin execution context.
        /// This overload allows passing a pre-constructed plugin instance, which is useful when
        /// the plugin requires constructor parameters or specific initialization.
        /// </summary>
        /// <param name="ctx">The custom plugin execution context containing message details, parameters, and images.</param>
        /// <param name="instance">The pre-constructed plugin instance to execute.</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when InputParameters contain invalid types for the message.</exception>
        public IPlugin ExecutePluginWith(XrmFakedPluginExecutionContext ctx, IPlugin instance)
        {
            // Validate parameter types before execution to match real CRM/Dataverse behavior
            ValidatePluginContextParameterTypes(ctx);

            var fakedServiceProvider = GetFakedServiceProvider(ctx);

            var fakedPlugin = A.Fake<IPlugin>();
            A.CallTo(() => fakedPlugin.Execute(A<IServiceProvider>._))
                .Invokes((IServiceProvider provider) =>
                {
                    var plugin = instance;
                    plugin.Execute(fakedServiceProvider);
                });

            fakedPlugin.Execute(fakedServiceProvider); //Execute the plugin
            return fakedPlugin;
        }

        /// <summary>
        /// Executes a plugin of the specified type with explicit input parameters, output parameters, and entity images.
        /// This method creates a default plugin context and populates it with the provided collections.
        /// </summary>
        /// <typeparam name="T">The plugin type to execute. Must implement <see cref="IPlugin"/> and have a parameterless constructor.</typeparam>
        /// <param name="inputParameters">The input parameters to pass to the plugin (e.g., Target entity).</param>
        /// <param name="outputParameters">The output parameters collection for plugin output.</param>
        /// <param name="preEntityImages">The pre-entity images available to the plugin.</param>
        /// <param name="postEntityImages">The post-entity images available to the plugin.</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        public IPlugin ExecutePluginWith<T>(ParameterCollection inputParameters, ParameterCollection outputParameters, EntityImageCollection preEntityImages, EntityImageCollection postEntityImages)
            where T : IPlugin, new()
        {
            var ctx = GetDefaultPluginContext();
            ctx.InputParameters.AddRange(inputParameters);
            ctx.OutputParameters.AddRange(outputParameters);
            ctx.PreEntityImages.AddRange(preEntityImages);
            ctx.PostEntityImages.AddRange(postEntityImages);

            var fakedServiceProvider = GetFakedServiceProvider(ctx);

            var fakedPlugin = A.Fake<IPlugin>();
            A.CallTo(() => fakedPlugin.Execute(A<IServiceProvider>._))
                .Invokes((IServiceProvider provider) =>
                {
                    var plugin = new T();
                    plugin.Execute(fakedServiceProvider);
                });

            fakedPlugin.Execute(fakedServiceProvider); //Execute the plugin
            return fakedPlugin;
        }

        /// <summary>
        /// Executes a plugin of the specified type with secure and unsecure configuration strings.
        /// The plugin must have a constructor that accepts two string parameters (unsecure and secure configuration).
        /// </summary>
        /// <typeparam name="T">The plugin type to execute. Must implement <see cref="IPlugin"/> and have a constructor accepting two string parameters.</typeparam>
        /// <param name="plugCtx">The custom plugin execution context.</param>
        /// <param name="unsecureConfiguration">The unsecure configuration string to pass to the plugin constructor.</param>
        /// <param name="secureConfiguration">The secure configuration string to pass to the plugin constructor.</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        /// <exception cref="ArgumentException">Thrown when the plugin type does not have a constructor accepting two string parameters.</exception>
        public IPlugin ExecutePluginWithConfigurations<T>(XrmFakedPluginExecutionContext plugCtx, string unsecureConfiguration, string secureConfiguration)
            where T : class, IPlugin
        {
            var pluginType = typeof(T);
            var constructors = pluginType.GetConstructors().ToList();

            if (!constructors.Any(c => c.GetParameters().Length == 2 && c.GetParameters().All(param => param.ParameterType == typeof(string))))
            {
                throw new ArgumentException("The plugin you are trying to execute does not specify a constructor for passing in two configuration strings.");
            }

            var pluginInstance = (T)Activator.CreateInstance(typeof(T), unsecureConfiguration, secureConfiguration);

            return this.ExecutePluginWith(plugCtx, pluginInstance);
        }

        /// <summary>
        /// Executes a pre-constructed plugin instance with configuration strings.
        /// </summary>
        /// <typeparam name="T">The plugin type. Must implement <see cref="IPlugin"/>.</typeparam>
        /// <param name="plugCtx">The custom plugin execution context.</param>
        /// <param name="instance">The pre-constructed plugin instance to execute.</param>
        /// <param name="unsecureConfiguration">The unsecure configuration string (not used in this overload, retained for API compatibility).</param>
        /// <param name="secureConfiguration">The secure configuration string (not used in this overload, retained for API compatibility).</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        /// <exception cref="ArgumentException">Thrown when the plugin type does not have a constructor accepting two string parameters.</exception>
        [Obsolete("Use ExecutePluginWith(XrmFakedPluginExecutionContext ctx, IPlugin instance).")]
        public IPlugin ExecutePluginWithConfigurations<T>(XrmFakedPluginExecutionContext plugCtx, T instance, string unsecureConfiguration="", string secureConfiguration="")
            where T : class, IPlugin
        {
            var fakedServiceProvider = GetFakedServiceProvider(plugCtx);

            var fakedPlugin = A.Fake<IPlugin>();

            A.CallTo(() => fakedPlugin.Execute(A<IServiceProvider>._))
                .Invokes((IServiceProvider provider) =>
                {
                    var pluginType = typeof(T);
                    var constructors = pluginType.GetConstructors();

                    if (!constructors.Any(c => c.GetParameters().Length == 2 && c.GetParameters().All(param => param.ParameterType == typeof(string))))
                    {
                        throw new ArgumentException("The plugin you are trying to execute does not specify a constructor for passing in two configuration strings.");
                    }

                    var plugin = instance;
                    plugin.Execute(fakedServiceProvider);
                });

            fakedPlugin.Execute(fakedServiceProvider); //Execute the plugin
            return fakedPlugin;
        }

        /// <summary>
        /// Executes a plugin of the specified type with a target entity and custom plugin context.
        /// The target entity is automatically added to the InputParameters collection.
        /// </summary>
        /// <typeparam name="T">The plugin type to execute. Must implement <see cref="IPlugin"/> and have a parameterless constructor.</typeparam>
        /// <param name="ctx">The custom plugin execution context to use.</param>
        /// <param name="target">The target entity to execute the plugin against.</param>
        /// <param name="messageName">The message name (e.g., "Create", "Update", "Delete"). Defaults to "Create".</param>
        /// <param name="stage">The pipeline stage (e.g., 10 for Pre-validation, 20 for Pre-operation, 40 for Post-operation). Defaults to 40.</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        public IPlugin ExecutePluginWithTarget<T>(XrmFakedPluginExecutionContext ctx, Entity target, string messageName = "Create", int stage = 40)
          where T : IPlugin, new()
        {
            ctx.InputParameters.Add("Target", target);
            ctx.MessageName = messageName;
            ctx.Stage = stage;

            return this.ExecutePluginWith<T>(ctx);
        }

        /// <summary>
        /// Executes the plugin of type T against the faked context for an entity target
        /// and returns the faked plugin
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The entity to execute the plug-in for.</param>
        /// <param name="messageName">Sets the message name.</param>
        /// <param name="stage">Sets the stage.</param>
        /// <returns></returns>
        public IPlugin ExecutePluginWithTarget<T>(Entity target, string messageName = "Create", int stage = 40)
            where T : IPlugin, new()
        {
            return this.ExecutePluginWithTarget(new T(), target, messageName, stage);
        }

        /// <summary>
        /// Executes the plugin of type T against the faked context for an entity target
        /// with automatic pre and/or post entity image population
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The entity to execute the plug-in for.</param>
        /// <param name="messageName">Sets the message name.</param>
        /// <param name="stage">Sets the stage.</param>
        /// <param name="preImageColumns">ColumnSet for pre-entity image. If specified, the entity will be retrieved from context and added as a pre-image.</param>
        /// <param name="postImageColumns">ColumnSet for post-entity image. If specified, the entity will be retrieved from context and added as a post-image.</param>
        /// <param name="preImageName">Name for the pre-entity image. Defaults to "PreImage".</param>
        /// <param name="postImageName">Name for the post-entity image. Defaults to "PostImage".</param>
        /// <returns></returns>
        public IPlugin ExecutePluginWithTarget<T>(Entity target, string messageName = "Create", int stage = 40,
            ColumnSet preImageColumns = null, ColumnSet postImageColumns = null,
            string preImageName = "PreImage", string postImageName = "PostImage")
            where T : IPlugin, new()
        {
            return this.ExecutePluginWithTarget(new T(), target, messageName, stage, preImageColumns, postImageColumns, preImageName, postImageName);
        }

        /// <summary>
        /// Executes the plugin of type T against the faked context for an entity target
        /// and returns the faked plugin
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="target">The entity to execute the plug-in for.</param>
        /// <param name="messageName">Sets the message name.</param>
        /// <param name="stage">Sets the stage.</param>
        /// <returns></returns>
        public IPlugin ExecutePluginWithTarget(IPlugin instance, Entity target, string messageName = "Create", int stage = 40)
        {
            var ctx = GetDefaultPluginContext();

            // Add the target entity to the InputParameters
            ctx.InputParameters.Add("Target", target);
            ctx.MessageName = messageName;
            ctx.Stage = stage;

            return this.ExecutePluginWith(ctx, instance);
        }

        /// <summary>
        /// Executes the plugin against the faked context for an entity target
        /// with automatic pre and/or post entity image population
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="target">The entity to execute the plug-in for.</param>
        /// <param name="messageName">Sets the message name.</param>
        /// <param name="stage">Sets the stage.</param>
        /// <param name="preImageColumns">ColumnSet for pre-entity image. If specified, the entity will be retrieved from context and added as a pre-image.</param>
        /// <param name="postImageColumns">ColumnSet for post-entity image. If specified, the entity will be retrieved from context and added as a post-image.</param>
        /// <param name="preImageName">Name for the pre-entity image. Defaults to "PreImage".</param>
        /// <param name="postImageName">Name for the post-entity image. Defaults to "PostImage".</param>
        /// <returns></returns>
        public IPlugin ExecutePluginWithTarget(IPlugin instance, Entity target, string messageName = "Create", int stage = 40,
            ColumnSet preImageColumns = null, ColumnSet postImageColumns = null,
            string preImageName = "PreImage", string postImageName = "PostImage")
        {
            var ctx = GetDefaultPluginContext();

            // Add the target entity to the InputParameters
            ctx.InputParameters.Add("Target", target);
            ctx.MessageName = messageName;
            ctx.Stage = stage;

            // Auto-populate pre-entity image if requested
            if (preImageColumns != null && target.Id != Guid.Empty)
            {
                var preImage = RetrieveEntityForImage(target.LogicalName, target.Id, preImageColumns);
                if (preImage != null)
                {
                    ctx.PreEntityImages.Add(preImageName, preImage);
                }
            }

            // Auto-populate post-entity image if requested
            if (postImageColumns != null && target.Id != Guid.Empty)
            {
                var postImage = RetrieveEntityForImage(target.LogicalName, target.Id, postImageColumns);
                if (postImage != null)
                {
                    ctx.PostEntityImages.Add(postImageName, postImage);
                }
            }

            return this.ExecutePluginWith(ctx, instance);
        }

        /// <summary>
        /// Helper method to retrieve an entity from context for use as an entity image
        /// </summary>
        private Entity RetrieveEntityForImage(string entityName, Guid entityId, ColumnSet columnSet)
        {
            // Check if entity exists in context - thread-safe access
            ConcurrentDictionary<Guid, Entity> entityDict;
            Entity entity;
            if (Data.TryGetValue(entityName, out entityDict) && entityDict.TryGetValue(entityId, out entity))
            {
                // Clone the entity with specified columns
                if (columnSet.AllColumns)
                {
                    return entity.Clone();
                }
                else
                {
                    var image = new Entity(entityName, entityId);
                    foreach (var column in columnSet.Columns)
                    {
                        if (entity.Contains(column))
                        {
                            image[column] = entity[column];
                        }
                    }
                    return image;
                }
            }

            return null;
        }

        /// <summary>
        /// Executes the plugin of type T against the faked context for an entity reference target
        /// and returns the faked plugin
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The entity reference to execute the plug-in for.</param>
        /// <param name="messageName">Sets the message name.</param>
        /// <param name="stage">Sets the stage.</param>
        /// <returns></returns>
        public IPlugin ExecutePluginWithTargetReference<T>(EntityReference target, string messageName = "Delete", int stage = 40)
            where T : IPlugin, new()
        {
            return this.ExecutePluginWithTargetReference(new T(), target, messageName, stage);
        }

        /// <summary>
        /// Executes the plugin of type T against the faked context for an entity reference target
        /// and returns the faked plugin
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="target">The entity reference to execute the plug-in for.</param>
        /// <param name="messageName">Sets the message name.</param>
        /// <param name="stage">Sets the stage.</param>
        /// <returns></returns>
        public IPlugin ExecutePluginWithTargetReference(IPlugin instance, EntityReference target, string messageName = "Delete", int stage = 40)
        {
            var ctx = GetDefaultPluginContext();
            // Add the target entity to the InputParameters
            ctx.InputParameters.Add("Target", target);
            ctx.MessageName = messageName;
            ctx.Stage = stage;

            return this.ExecutePluginWith(ctx, instance);
        }

        /// <summary>
        /// Executes a plugin with a target and pre-entity images.
        /// </summary>
        /// <typeparam name="T">The plugin type to execute. Must implement <see cref="IPlugin"/> and have a parameterless constructor.</typeparam>
        /// <param name="target">The target object (Entity or EntityReference) to execute the plugin against.</param>
        /// <param name="preEntityImages">The pre-entity images collection to include in the context.</param>
        /// <param name="messageName">The message name. Defaults to "Create".</param>
        /// <param name="stage">The pipeline stage. Defaults to 40 (Post-operation).</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        [Obsolete]
        public IPlugin ExecutePluginWithTargetAndPreEntityImages<T>(object target, EntityImageCollection preEntityImages, string messageName = "Create", int stage = 40)
            where T : IPlugin, new()
        {
            var ctx = GetDefaultPluginContext();
            // Add the target entity to the InputParameters
            ctx.InputParameters.Add("Target", target);
            ctx.PreEntityImages.AddRange(preEntityImages);
            ctx.MessageName = messageName;
            ctx.Stage = stage;

            return this.ExecutePluginWith<T>(ctx);
        }

        /// <summary>
        /// Executes a plugin with a target and post-entity images.
        /// </summary>
        /// <typeparam name="T">The plugin type to execute. Must implement <see cref="IPlugin"/> and have a parameterless constructor.</typeparam>
        /// <param name="target">The target object (Entity or EntityReference) to execute the plugin against.</param>
        /// <param name="postEntityImages">The post-entity images collection to include in the context.</param>
        /// <param name="messageName">The message name. Defaults to "Create".</param>
        /// <param name="stage">The pipeline stage. Defaults to 40 (Post-operation).</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        [Obsolete]
        public IPlugin ExecutePluginWithTargetAndPostEntityImages<T>(object target, EntityImageCollection postEntityImages, string messageName = "Create", int stage = 40)
            where T : IPlugin, new()
        {
            var ctx = GetDefaultPluginContext();
            // Add the target entity to the InputParameters
            ctx.InputParameters.Add("Target", target);
            ctx.PostEntityImages.AddRange(postEntityImages);
            ctx.MessageName = messageName;
            ctx.Stage = stage;

            return this.ExecutePluginWith<T>(ctx);
        }

        /// <summary>
        /// Executes a plugin with a target entity and additional input parameters.
        /// </summary>
        /// <typeparam name="T">The plugin type to execute. Must implement <see cref="IPlugin"/> and have a parameterless constructor.</typeparam>
        /// <param name="target">The target entity to execute the plugin against.</param>
        /// <param name="inputParameters">Additional input parameters to include in the context.</param>
        /// <param name="messageName">The message name. Defaults to "Create".</param>
        /// <param name="stage">The pipeline stage. Defaults to 40 (Post-operation).</param>
        /// <returns>The executed plugin instance wrapped in a FakeItEasy fake.</returns>
        [Obsolete]
        public IPlugin ExecutePluginWithTargetAndInputParameters<T>(Entity target, ParameterCollection inputParameters, string messageName = "Create", int stage = 40)
            where T : IPlugin, new()
        {
            var ctx = GetDefaultPluginContext();

            ctx.InputParameters.AddRange(inputParameters);

            return this.ExecutePluginWithTarget<T>(ctx, target, messageName, stage);
        }

        /// <summary>
        /// Creates a faked service provider for plugin execution.
        /// The service provider can resolve IOrganizationService, ITracingService, IPluginExecutionContext,
        /// IOrganizationServiceFactory, and IServiceEndpointNotificationService.
        /// </summary>
        /// <param name="plugCtx">The plugin execution context to use when resolving context-related services.</param>
        /// <returns>A mocked <see cref="IServiceProvider"/> that can resolve CRM plugin services.</returns>
        /// <exception cref="PullRequestException">Thrown when an unsupported service type is requested.</exception>
        protected IServiceProvider GetFakedServiceProvider(XrmFakedPluginExecutionContext plugCtx)
        {
            var fakedServiceProvider = A.Fake<IServiceProvider>();

            A.CallTo(() => fakedServiceProvider.GetService(A<Type>._))
               .ReturnsLazily((Type t) =>
               {
                   if (t == typeof(IOrganizationService))
                   {
                       //Return faked or real organization service
                       return GetOrganizationService();
                   }

                   if (t == typeof(ITracingService))
                   {
                       return TracingService;
                   }

                   if (t == typeof(IPluginExecutionContext) ||
                       t == typeof(IPluginExecutionContext2) ||
                       t == typeof(IPluginExecutionContext3) ||
                       t == typeof(IPluginExecutionContext4))
                   {
                       return GetFakedPluginContext(plugCtx);
                   }

                   if (t == typeof(IExecutionContext))
                   {
                       return GetFakedExecutionContext(plugCtx);
                   }

                   if (t == typeof(IOrganizationServiceFactory))
                   {
                       var fakedServiceFactory = A.Fake<IOrganizationServiceFactory>();
                       A.CallTo(() => fakedServiceFactory.CreateOrganizationService(A<Guid?>._)).ReturnsLazily((Guid? g) => GetOrganizationService());
                       return fakedServiceFactory;
                   }

                   if (t == typeof(IServiceEndpointNotificationService))
                   {
                       return GetFakedServiceEndpointNotificationService();
                   }
#if FAKE_XRM_EASY_9
                   if (t == typeof(IEntityDataSourceRetrieverService))
                   {
                       // Set the current virtual entity logical name for auto-lookup (GitHub issue #579)
                       // Try to get it from the plugin context
                       if (!string.IsNullOrEmpty(plugCtx.PrimaryEntityName))
                       {
                           CurrentVirtualEntityLogicalName = plugCtx.PrimaryEntityName;
                       }
                       else if (plugCtx.InputParameters != null && plugCtx.InputParameters.ContainsKey("Query"))
                       {
                           // For RetrieveMultiple, try to get entity name from the query
                           var query = plugCtx.InputParameters["Query"];
                           if (query is QueryExpression queryExpression)
                           {
                               CurrentVirtualEntityLogicalName = queryExpression.EntityName;
                           }
                       }
                       return GetFakedEntityDataSourceRetrieverService();
                   }
#endif
                   throw new PullRequestException($"The service type '{t.FullName}' is not supported");
               });

            return fakedServiceProvider;
        }

#if FAKE_XRM_EASY_9
        /// <summary>
        /// Gets or sets the entity used by the EntityDataSourceRetrieverService for virtual entity data sources.
        /// Only available in Dynamics 365 v9.x and later.
        /// </summary>
        public Entity EntityDataSourceRetriever { get; set; }
#endif

        /// <summary>
        /// Gets the faked tracing service used by plugins during execution.
        /// The tracing service captures all trace messages written by plugins for verification in tests.
        /// </summary>
        /// <returns>The <see cref="XrmFakedTracingService"/> instance used by this context.</returns>
        public XrmFakedTracingService GetFakeTracingService()
        {
            return TracingService;
        }
    }
}

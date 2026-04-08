using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace FakeXrmEasy
{
    /// <summary>
    /// Partial class containing plugin pipeline simulation functionality for the faked CRM context.
    /// Provides support for simulating the Dynamics 365 plugin execution pipeline by registering
    /// and executing plugins based on SDK message processing step configurations.
    /// </summary>
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// Gets or sets a value indicating whether pipeline simulation is enabled.
        /// When enabled, registered plugins will be automatically executed during CRUD operations
        /// based on their SDK Message Processing Step configuration.
        /// </summary>
        public bool UsePipelineSimulation { get; set; }

        /// <summary>
        /// Dictionary storing plugin factories by processing step ID.
        /// When a factory is registered, it will be called each time the plugin step executes
        /// to create a fresh instance.
        /// </summary>
        private readonly Dictionary<Guid, Func<IPlugin>> _pluginFactories = new Dictionary<Guid, Func<IPlugin>>();

        /// <summary>
        /// Dictionary storing plugin instances by processing step ID.
        /// When an instance is registered, the same instance will be reused for all executions.
        /// Note: It is the caller's responsibility to ensure the instance is stateless or
        /// properly handles state between executions.
        /// </summary>
        private readonly Dictionary<Guid, IPlugin> _pluginInstances = new Dictionary<Guid, IPlugin>();

        /// <summary>
        /// Registers the specified plugin type as an SDK Message Processing Step for the specified entity type.
        /// This creates the necessary sdkmessage, plugintype, and sdkmessageprocessingstep records
        /// in the faked context to simulate plugin registration.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin type to register. Must implement <see cref="IPlugin"/>.</typeparam>
        /// <typeparam name="TEntity">The entity type to filter this step for. Must inherit from <see cref="Entity"/>.</typeparam>
        /// <param name="message">The message that should trigger the execution of the plugin (e.g., "Create", "Update", "Delete").</param>
        /// <param name="stage">The pipeline stage when the plugin should be executed. Defaults to Post-operation.</param>
        /// <param name="mode">The execution mode (Synchronous or Asynchronous). Defaults to Synchronous.</param>
        /// <param name="rank">The execution order relative to other plugins registered for the same message and stage. Lower values execute first.</param>
        /// <param name="filteringAttributes">Optional array of attribute names. If specified, the plugin only executes when at least one of these attributes is modified.</param>
        public void RegisterPluginStep<TPlugin, TEntity>(string message, ProcessingStepStage stage = ProcessingStepStage.Postoperation, ProcessingStepMode mode = ProcessingStepMode.Synchronous, int rank = 1, string[] filteringAttributes = null)
            where TPlugin : IPlugin
            where TEntity : Entity, new()
        {
            var entity = new TEntity();
            var entityTypeCode = GetEntityTypeCodeForRegistration(entity);

            RegisterPluginStep<TPlugin>(message, stage, mode, rank, filteringAttributes, entityTypeCode);
        }

        /// <summary>
        /// Gets the entity type code for plugin registration.
        /// First tries reflection on EntityTypeCode field (early-bound entities),
        /// then falls back to looking up ObjectTypeCode from EntityMetadata using LogicalName.
        /// Throws a helpful exception if neither approach works.
        /// </summary>
        /// <param name="entity">The entity to get the type code for.</param>
        /// <returns>The entity type code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the entity type code cannot be determined.</exception>
        private int GetEntityTypeCodeForRegistration(Entity entity)
        {
            // First, try to get EntityTypeCode from early-bound entities via reflection
            var entityTypeCodeField = entity.GetType().GetField("EntityTypeCode");
            if (entityTypeCodeField != null)
            {
                var entityTypeCodeValue = entityTypeCodeField.GetValue(entity);
                if (entityTypeCodeValue != null)
                {
                    return (int)entityTypeCodeValue;
                }
            }

            // Fallback: For late-bound Entity class, try to look up ObjectTypeCode from EntityMetadata
            if (!string.IsNullOrEmpty(entity.LogicalName) && EntityMetadata.ContainsKey(entity.LogicalName))
            {
                var metadata = EntityMetadata[entity.LogicalName];
                if (metadata.ObjectTypeCode.HasValue)
                {
                    return metadata.ObjectTypeCode.Value;
                }
            }

            // Neither approach worked - provide a helpful error message
            var entityTypeName = entity.GetType().Name;
            var logicalName = entity.LogicalName;

            if (entityTypeName == "Entity" || string.IsNullOrEmpty(logicalName))
            {
                throw new InvalidOperationException(
                    "Cannot determine entity type code for late-bound Entity class. " +
                    "Either use early-bound entity types with EntityTypeCode, " +
                    "initialize EntityMetadata with ObjectTypeCode for the entity, " +
                    "or use the RegisterPluginStep<TPlugin> overload with an explicit primaryEntityTypeCode parameter.");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot determine entity type code for entity type '{entityTypeName}' (LogicalName: '{logicalName}'). " +
                    "The entity type does not have an EntityTypeCode field and no EntityMetadata with ObjectTypeCode was found. " +
                    "Either regenerate your early-bound classes with EntityTypeCode generation enabled, " +
                    "initialize EntityMetadata with ObjectTypeCode for this entity, " +
                    "or use the RegisterPluginStep<TPlugin> overload with an explicit primaryEntityTypeCode parameter.");
            }
        }

        /// <summary>
        /// Registers the specified plugin type as an SDK Message Processing Step.
        /// This overload allows specifying an entity type code directly instead of using a generic type parameter.
        /// Creates the necessary sdkmessage, plugintype, sdkmessagefilter, and sdkmessageprocessingstep records.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin type to register. Must implement <see cref="IPlugin"/>.</typeparam>
        /// <param name="message">The message that should trigger the execution of the plugin (e.g., "Create", "Update", "Delete").</param>
        /// <param name="stage">The pipeline stage when the plugin should be executed. Defaults to Post-operation.</param>
        /// <param name="mode">The execution mode (Synchronous or Asynchronous). Defaults to Synchronous.</param>
        /// <param name="rank">The execution order relative to other plugins registered for the same message and stage. Lower values execute first.</param>
        /// <param name="filteringAttributes">Optional array of attribute names. If specified, the plugin only executes when at least one of these attributes is modified.</param>
        /// <param name="primaryEntityTypeCode">Optional entity type code to filter this step for. If null, the plugin will execute for all entities.</param>
        public void RegisterPluginStep<TPlugin>(string message, ProcessingStepStage stage = ProcessingStepStage.Postoperation, ProcessingStepMode mode = ProcessingStepMode.Synchronous, int rank = 1, string[] filteringAttributes = null, int? primaryEntityTypeCode = null)
            where TPlugin : IPlugin
        {
            // Message
            var sdkMessage = this.CreateQuery("sdkmessage").FirstOrDefault(sm => string.Equals(sm.GetAttributeValue<string>("name"), message));
            if (sdkMessage == null)
            {
                sdkMessage = new Entity("sdkmessage")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = message
                };
                this.AddEntityWithDefaults(sdkMessage);
            }

            // Plugin Type
            var type = typeof(TPlugin);
            var assemblyName = type.Assembly.GetName();

            var pluginType = this.CreateQuery("plugintype").FirstOrDefault(pt => string.Equals(pt.GetAttributeValue<string>("typename"), type.FullName) && string.Equals(pt.GetAttributeValue<string>("asemblyname"), assemblyName.Name));
            if (pluginType == null)
            {
                pluginType = new Entity("plugintype")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = type.FullName,
                    ["typename"] = type.FullName,
                    ["assemblyname"] = assemblyName.Name,
                    ["major"] = assemblyName.Version.Major,
                    ["minor"] = assemblyName.Version.Minor,
                    ["version"] = assemblyName.Version.ToString(),
                };
                this.AddEntityWithDefaults(pluginType);
            }

            // Filter
            Entity sdkFilter = null;
            if (primaryEntityTypeCode.HasValue)
            {
                sdkFilter = new Entity("sdkmessagefilter")
                {
                    Id = Guid.NewGuid(),
                    ["primaryobjecttypecode"] = primaryEntityTypeCode
                };
                this.AddEntityWithDefaults(sdkFilter);
            }

            // Message Step
            var sdkMessageProcessingStep = new Entity("sdkmessageprocessingstep")
            {
                Id = Guid.NewGuid(),
                ["eventhandler"] = pluginType.ToEntityReference(),
                ["sdkmessageid"] = sdkMessage.ToEntityReference(),
                ["sdkmessagefilterid"] = sdkFilter?.ToEntityReference(),
                ["filteringattributes"] = filteringAttributes != null ? string.Join(",", filteringAttributes) : null,
                ["mode"] = new OptionSetValue((int)mode),
                ["stage"] = new OptionSetValue((int)stage),
                ["rank"] = rank
            };
            this.AddEntityWithDefaults(sdkMessageProcessingStep);
        }

        /// <summary>
        /// Registers a pre-configured plugin instance as an SDK Message Processing Step for the specified entity type.
        /// This allows dependency injection by passing a plugin instance with constructor dependencies already configured.
        /// The same instance will be reused for all executions - ensure your plugin is stateless or handles state properly.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin type. Must implement <see cref="IPlugin"/>.</typeparam>
        /// <typeparam name="TEntity">The entity type to filter this step for. Must inherit from <see cref="Entity"/>.</typeparam>
        /// <param name="message">The message that should trigger the execution of the plugin (e.g., "Create", "Update", "Delete").</param>
        /// <param name="pluginInstance">The pre-configured plugin instance to execute.</param>
        /// <param name="stage">The pipeline stage when the plugin should be executed. Defaults to Post-operation.</param>
        /// <param name="mode">The execution mode (Synchronous or Asynchronous). Defaults to Synchronous.</param>
        /// <param name="rank">The execution order relative to other plugins registered for the same message and stage. Lower values execute first.</param>
        /// <param name="filteringAttributes">Optional array of attribute names. If specified, the plugin only executes when at least one of these attributes is modified.</param>
        public void RegisterPluginStep<TPlugin, TEntity>(string message, TPlugin pluginInstance, ProcessingStepStage stage = ProcessingStepStage.Postoperation, ProcessingStepMode mode = ProcessingStepMode.Synchronous, int rank = 1, string[] filteringAttributes = null)
            where TPlugin : IPlugin
            where TEntity : Entity, new()
        {
            if (pluginInstance == null)
            {
                throw new ArgumentNullException(nameof(pluginInstance), "Plugin instance cannot be null");
            }

            var entity = new TEntity();
            var entityTypeCode = GetEntityTypeCodeForRegistration(entity);

            var stepId = RegisterPluginStepInternal<TPlugin>(message, stage, mode, rank, filteringAttributes, entityTypeCode);
            _pluginInstances[stepId] = pluginInstance;
        }

        /// <summary>
        /// Registers a plugin factory as an SDK Message Processing Step for the specified entity type.
        /// The factory will be called each time the plugin step executes, providing a fresh instance.
        /// This is the recommended approach for plugins with dependencies as it ensures clean state.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin type. Must implement <see cref="IPlugin"/>.</typeparam>
        /// <typeparam name="TEntity">The entity type to filter this step for. Must inherit from <see cref="Entity"/>.</typeparam>
        /// <param name="message">The message that should trigger the execution of the plugin (e.g., "Create", "Update", "Delete").</param>
        /// <param name="pluginFactory">A factory function that creates a new plugin instance for each execution.</param>
        /// <param name="stage">The pipeline stage when the plugin should be executed. Defaults to Post-operation.</param>
        /// <param name="mode">The execution mode (Synchronous or Asynchronous). Defaults to Synchronous.</param>
        /// <param name="rank">The execution order relative to other plugins registered for the same message and stage. Lower values execute first.</param>
        /// <param name="filteringAttributes">Optional array of attribute names. If specified, the plugin only executes when at least one of these attributes is modified.</param>
        public void RegisterPluginStep<TPlugin, TEntity>(string message, Func<TPlugin> pluginFactory, ProcessingStepStage stage = ProcessingStepStage.Postoperation, ProcessingStepMode mode = ProcessingStepMode.Synchronous, int rank = 1, string[] filteringAttributes = null)
            where TPlugin : IPlugin
            where TEntity : Entity, new()
        {
            if (pluginFactory == null)
            {
                throw new ArgumentNullException(nameof(pluginFactory), "Plugin factory cannot be null");
            }

            var entity = new TEntity();
            var entityTypeCode = GetEntityTypeCodeForRegistration(entity);

            var stepId = RegisterPluginStepInternal<TPlugin>(message, stage, mode, rank, filteringAttributes, entityTypeCode);
            _pluginFactories[stepId] = () => pluginFactory();
        }

        /// <summary>
        /// Registers a pre-configured plugin instance as an SDK Message Processing Step.
        /// This overload allows specifying an entity type code directly instead of using a generic type parameter.
        /// The same instance will be reused for all executions - ensure your plugin is stateless or handles state properly.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin type. Must implement <see cref="IPlugin"/>.</typeparam>
        /// <param name="message">The message that should trigger the execution of the plugin (e.g., "Create", "Update", "Delete").</param>
        /// <param name="pluginInstance">The pre-configured plugin instance to execute.</param>
        /// <param name="stage">The pipeline stage when the plugin should be executed. Defaults to Post-operation.</param>
        /// <param name="mode">The execution mode (Synchronous or Asynchronous). Defaults to Synchronous.</param>
        /// <param name="rank">The execution order relative to other plugins registered for the same message and stage. Lower values execute first.</param>
        /// <param name="filteringAttributes">Optional array of attribute names. If specified, the plugin only executes when at least one of these attributes is modified.</param>
        /// <param name="primaryEntityTypeCode">Optional entity type code to filter this step for. If null, the plugin will execute for all entities.</param>
        public void RegisterPluginStep<TPlugin>(string message, TPlugin pluginInstance, ProcessingStepStage stage = ProcessingStepStage.Postoperation, ProcessingStepMode mode = ProcessingStepMode.Synchronous, int rank = 1, string[] filteringAttributes = null, int? primaryEntityTypeCode = null)
            where TPlugin : IPlugin
        {
            if (pluginInstance == null)
            {
                throw new ArgumentNullException(nameof(pluginInstance), "Plugin instance cannot be null");
            }

            var stepId = RegisterPluginStepInternal<TPlugin>(message, stage, mode, rank, filteringAttributes, primaryEntityTypeCode);
            _pluginInstances[stepId] = pluginInstance;
        }

        /// <summary>
        /// Registers a plugin factory as an SDK Message Processing Step.
        /// This overload allows specifying an entity type code directly instead of using a generic type parameter.
        /// The factory will be called each time the plugin step executes, providing a fresh instance.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin type. Must implement <see cref="IPlugin"/>.</typeparam>
        /// <param name="message">The message that should trigger the execution of the plugin (e.g., "Create", "Update", "Delete").</param>
        /// <param name="pluginFactory">A factory function that creates a new plugin instance for each execution.</param>
        /// <param name="stage">The pipeline stage when the plugin should be executed. Defaults to Post-operation.</param>
        /// <param name="mode">The execution mode (Synchronous or Asynchronous). Defaults to Synchronous.</param>
        /// <param name="rank">The execution order relative to other plugins registered for the same message and stage. Lower values execute first.</param>
        /// <param name="filteringAttributes">Optional array of attribute names. If specified, the plugin only executes when at least one of these attributes is modified.</param>
        /// <param name="primaryEntityTypeCode">Optional entity type code to filter this step for. If null, the plugin will execute for all entities.</param>
        public void RegisterPluginStep<TPlugin>(string message, Func<TPlugin> pluginFactory, ProcessingStepStage stage = ProcessingStepStage.Postoperation, ProcessingStepMode mode = ProcessingStepMode.Synchronous, int rank = 1, string[] filteringAttributes = null, int? primaryEntityTypeCode = null)
            where TPlugin : IPlugin
        {
            if (pluginFactory == null)
            {
                throw new ArgumentNullException(nameof(pluginFactory), "Plugin factory cannot be null");
            }

            var stepId = RegisterPluginStepInternal<TPlugin>(message, stage, mode, rank, filteringAttributes, primaryEntityTypeCode);
            _pluginFactories[stepId] = () => pluginFactory();
        }

        /// <summary>
        /// Internal method that handles the common plugin step registration logic.
        /// Creates the necessary sdkmessage, plugintype, sdkmessagefilter, and sdkmessageprocessingstep records.
        /// </summary>
        /// <typeparam name="TPlugin">The plugin type to register.</typeparam>
        /// <param name="message">The message name.</param>
        /// <param name="stage">The pipeline stage.</param>
        /// <param name="mode">The execution mode.</param>
        /// <param name="rank">The execution order.</param>
        /// <param name="filteringAttributes">Optional filtering attributes.</param>
        /// <param name="primaryEntityTypeCode">Optional entity type code.</param>
        /// <returns>The ID of the created sdkmessageprocessingstep entity.</returns>
        private Guid RegisterPluginStepInternal<TPlugin>(string message, ProcessingStepStage stage, ProcessingStepMode mode, int rank, string[] filteringAttributes, int? primaryEntityTypeCode)
            where TPlugin : IPlugin
        {
            // Message
            var sdkMessage = this.CreateQuery("sdkmessage").FirstOrDefault(sm => string.Equals(sm.GetAttributeValue<string>("name"), message));
            if (sdkMessage == null)
            {
                sdkMessage = new Entity("sdkmessage")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = message
                };
                this.AddEntityWithDefaults(sdkMessage);
            }

            // Plugin Type
            var type = typeof(TPlugin);
            var assemblyName = type.Assembly.GetName();

            var pluginType = this.CreateQuery("plugintype").FirstOrDefault(pt => string.Equals(pt.GetAttributeValue<string>("typename"), type.FullName) && string.Equals(pt.GetAttributeValue<string>("asemblyname"), assemblyName.Name));
            if (pluginType == null)
            {
                pluginType = new Entity("plugintype")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = type.FullName,
                    ["typename"] = type.FullName,
                    ["assemblyname"] = assemblyName.Name,
                    ["major"] = assemblyName.Version.Major,
                    ["minor"] = assemblyName.Version.Minor,
                    ["version"] = assemblyName.Version.ToString(),
                };
                this.AddEntityWithDefaults(pluginType);
            }

            // Filter
            Entity sdkFilter = null;
            if (primaryEntityTypeCode.HasValue)
            {
                sdkFilter = new Entity("sdkmessagefilter")
                {
                    Id = Guid.NewGuid(),
                    ["primaryobjecttypecode"] = primaryEntityTypeCode
                };
                this.AddEntityWithDefaults(sdkFilter);
            }

            // Message Step
            var stepId = Guid.NewGuid();
            var sdkMessageProcessingStep = new Entity("sdkmessageprocessingstep")
            {
                Id = stepId,
                ["eventhandler"] = pluginType.ToEntityReference(),
                ["sdkmessageid"] = sdkMessage.ToEntityReference(),
                ["sdkmessagefilterid"] = sdkFilter?.ToEntityReference(),
                ["filteringattributes"] = filteringAttributes != null ? string.Join(",", filteringAttributes) : null,
                ["mode"] = new OptionSetValue((int)mode),
                ["stage"] = new OptionSetValue((int)stage),
                ["rank"] = rank
            };
            this.AddEntityWithDefaults(sdkMessageProcessingStep);

            return stepId;
        }

        /// <summary>
        /// Executes registered plugins for a specific pipeline stage when processing an entity.
        /// Retrieves and executes all matching plugins ordered by rank.
        /// </summary>
        /// <param name="method">The message name (e.g., "Create", "Update", "Delete").</param>
        /// <param name="stage">The pipeline stage to execute plugins for.</param>
        /// <param name="mode">The execution mode to filter plugins by.</param>
        /// <param name="entity">The target entity being processed.</param>
        private void ExecutePipelineStage(string method, ProcessingStepStage stage, ProcessingStepMode mode, Entity entity)
        {
            ExecutePipelineStage(method, stage, mode, entity, null, null);
        }

        /// <summary>
        /// Executes registered plugins for a specific pipeline stage when processing an entity,
        /// with support for pre and post entity images.
        /// </summary>
        /// <param name="method">The message name (e.g., "Create", "Update", "Delete").</param>
        /// <param name="stage">The pipeline stage to execute plugins for.</param>
        /// <param name="mode">The execution mode to filter plugins by.</param>
        /// <param name="entity">The target entity being processed.</param>
        /// <param name="preImage">The entity state before the operation (for Update/Delete).</param>
        /// <param name="postImage">The entity state after the operation (for Create/Update).</param>
        private void ExecutePipelineStage(string method, ProcessingStepStage stage, ProcessingStepMode mode, Entity entity, Entity preImage, Entity postImage)
        {
            var plugins = GetStepsForStage(method, stage, mode, entity);

            ExecutePipelinePlugins(plugins, entity, preImage, postImage);
        }

        private void ExecutePipelineStage(string method, ProcessingStepStage stage, ProcessingStepMode mode, EntityReference entityReference)
        {
            ExecutePipelineStage(method, stage, mode, entityReference, null, null);
        }

        /// <summary>
        /// Executes registered plugins for a specific pipeline stage when processing an entity reference,
        /// with support for pre entity images.
        /// </summary>
        /// <param name="method">The message name (e.g., "Delete").</param>
        /// <param name="stage">The pipeline stage to execute plugins for.</param>
        /// <param name="mode">The execution mode to filter plugins by.</param>
        /// <param name="entityReference">The entity reference being processed.</param>
        /// <param name="preImage">The entity state before the operation.</param>
        /// <param name="postImage">The entity state after the operation (typically null for Delete).</param>
        private void ExecutePipelineStage(string method, ProcessingStepStage stage, ProcessingStepMode mode, EntityReference entityReference, Entity preImage, Entity postImage)
        {
            var entityType = FindReflectedType(entityReference.LogicalName);
            if (entityType == null)
            {
                return;
            }

            var plugins = GetStepsForStage(method, stage, mode, (Entity)Activator.CreateInstance(entityType));

            ExecutePipelinePlugins(plugins, entityReference, preImage, postImage);
        }

        private void ExecutePipelinePlugins(IEnumerable<Entity> plugins, object target)
        {
            ExecutePipelinePlugins(plugins, target, null, null);
        }

        /// <summary>
        /// Executes the collection of registered plugins for the pipeline, populating entity images
        /// based on the operation being performed.
        /// </summary>
        /// <param name="plugins">The collection of plugin step entities to execute.</param>
        /// <param name="target">The target object (Entity or EntityReference) being processed.</param>
        /// <param name="preImage">The entity state before the operation (for Update/Delete). Contains all fields.</param>
        /// <param name="postImage">The entity state after the operation (for Create/Update). Contains all fields.</param>
        /// <remarks>
        /// Entity images follow real CRM behavior:
        /// - Create: No PreImage (entity didn't exist), PostImage available (after creation)
        /// - Update: PreImage available (before update), PostImage available (after update)
        /// - Delete: PreImage available (before deletion), No PostImage (entity no longer exists)
        ///
        /// Image names used are "PreImage" and "PostImage" for simplicity.
        /// All fields are included in the images by default.
        /// </remarks>
        private void ExecutePipelinePlugins(IEnumerable<Entity> plugins, object target, Entity preImage, Entity postImage)
        {
            foreach (var plugin in plugins)
            {
                // Check filtering attributes
                var filteringAttributes = plugin.GetAttributeValue<string>("filteringattributes");
                if (!string.IsNullOrEmpty(filteringAttributes) && target is Entity targetEntity)
                {
                    var attributes = filteringAttributes.Split(',').Select(a => a.Trim()).ToArray();

                    // Only execute if at least one filtering attribute is present in the target
                    if (!attributes.Any(a => targetEntity.Contains(a)))
                    {
                        continue; // Skip this plugin - no filtered attributes present
                    }
                }

                var pluginContext = this.GetDefaultPluginContext();
                pluginContext.Mode = plugin.GetAttributeValue<OptionSetValue>("mode").Value;
                pluginContext.Stage = plugin.GetAttributeValue<OptionSetValue>("stage").Value;
                pluginContext.MessageName = (string)plugin.GetAttributeValue<AliasedValue>("sdkmessage.name").Value;
                pluginContext.InputParameters = new ParameterCollection
                {
                    { "Target", target }
                };
                pluginContext.OutputParameters = new ParameterCollection();

                // Populate entity images based on what was provided
                // Following real CRM behavior:
                // - Create: No PreImage, PostImage available
                // - Update: PreImage and PostImage available
                // - Delete: PreImage available, No PostImage
                pluginContext.PreEntityImages = new EntityImageCollection();
                pluginContext.PostEntityImages = new EntityImageCollection();

                if (preImage != null)
                {
                    pluginContext.PreEntityImages.Add("PreImage", preImage);
                }
                if (postImage != null)
                {
                    pluginContext.PostEntityImages.Add("PostImage", postImage);
                }

                var stepId = plugin.Id;

                // Check if we have a registered factory for this step
                if (_pluginFactories.TryGetValue(stepId, out var factory))
                {
                    var pluginInstance = factory();
                    this.ExecutePluginWith(pluginContext, pluginInstance);
                }
                // Check if we have a registered instance for this step
                else if (_pluginInstances.TryGetValue(stepId, out var instance))
                {
                    this.ExecutePluginWith(pluginContext, instance);
                }
                // Fall back to the original reflection-based approach
                else
                {
                    var pluginMethod = GetPluginMethod(plugin);
                    pluginMethod.Invoke(this, new object[] { pluginContext });
                }
            }
        }

        private static MethodInfo GetPluginMethod(Entity pluginEntity)
        {
            var assemblyName = (string)pluginEntity.GetAttributeValue<AliasedValue>("plugintype.assemblyname").Value;
            var assembly = AppDomain.CurrentDomain.Load(assemblyName);

            var pluginTypeName = (string)pluginEntity.GetAttributeValue<AliasedValue>("plugintype.typename").Value;
            var pluginType = assembly.GetType(pluginTypeName);

            var methodInfo = typeof(XrmFakedContext).GetMethod("ExecutePluginWith", new[] { typeof(XrmFakedPluginExecutionContext) });
            var pluginMethod = methodInfo.MakeGenericMethod(pluginType);

            return pluginMethod;
        }

        private IEnumerable<Entity> GetStepsForStage(string method, ProcessingStepStage stage, ProcessingStepMode mode, Entity entity)
        {
            var query = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("configuration", "filteringattributes", "stage", "mode"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("stage", ConditionOperator.Equal, (int)stage),
                        new ConditionExpression("mode", ConditionOperator.Equal, (int)mode)
                    }
                },
                Orders =
                {
                    new OrderExpression("rank", OrderType.Ascending)
                },
                LinkEntities =
                {
                    new LinkEntity("sdkmessageprocessingstep", "sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter)
                    {
                        EntityAlias = "sdkmessagefilter",
                        Columns = new ColumnSet("primaryobjecttypecode")
                    },
                    new LinkEntity("sdkmessageprocessingstep", "sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.Inner)
                    {
                        EntityAlias = "sdkmessage",
                        Columns = new ColumnSet("name"),
                        LinkCriteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("name", ConditionOperator.Equal, method)
                            }
                        }
                    },
                    new LinkEntity("sdkmessageprocessingstep", "plugintype", "eventhandler", "plugintypeid", JoinOperator.Inner)
                    {
                        EntityAlias = "plugintype",
                        Columns = new ColumnSet("assemblyname", "typename")
                    }
                }
            };

            var entityTypeCode = GetEntityTypeCode(entity);

            var plugins = this.Service.RetrieveMultiple(query).Entities.AsEnumerable();
            plugins = plugins.Where(p =>
            {
                var primaryObjectTypeCode = p.GetAttributeValue<AliasedValue>("sdkmessagefilter.primaryobjecttypecode");

                return primaryObjectTypeCode == null || entityTypeCode.HasValue && (int)primaryObjectTypeCode.Value == entityTypeCode.Value;
            });

            // Note: Filtering on attributes is handled in ExecutePipelinePlugins where we have access to the target entity

            return plugins;
        }

        /// <summary>
        /// Gets the entity type code (ObjectTypeCode) for an entity.
        /// First tries to get EntityTypeCode from early-bound entities via reflection.
        /// If not available (late-bound Entity class), falls back to looking up the ObjectTypeCode
        /// from EntityMetadata using the entity's LogicalName.
        /// </summary>
        /// <param name="entity">The entity to get the type code for.</param>
        /// <returns>The entity type code, or null if it cannot be determined.</returns>
        private int? GetEntityTypeCode(Entity entity)
        {
            // First, try to get EntityTypeCode from early-bound entities via reflection
            var entityTypeCodeField = entity.GetType().GetField("EntityTypeCode");
            if (entityTypeCodeField != null)
            {
                var value = entityTypeCodeField.GetValue(entity);
                if (value != null)
                {
                    return (int)value;
                }
            }

            // Fallback: For late-bound Entity class, try to look up ObjectTypeCode from EntityMetadata
            if (!string.IsNullOrEmpty(entity.LogicalName) && EntityMetadata.ContainsKey(entity.LogicalName))
            {
                var metadata = EntityMetadata[entity.LogicalName];
                if (metadata.ObjectTypeCode.HasValue)
                {
                    return metadata.ObjectTypeCode.Value;
                }
            }

            // Could not determine entity type code - this is acceptable for plugins registered without entity filtering
            return null;
        }
    }
}
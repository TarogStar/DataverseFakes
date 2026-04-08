using FakeItEasy;
using FakeXrmEasy.FakeMessageExecutors;
using FakeXrmEasy.Permissions;
using FakeXrmEasy.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FakeXrmEasy
{
    /// <summary>
    /// The main context class for the FakeXrmEasy testing framework that simulates the Dynamics 365 CRM context.
    /// Stores in-memory entities indexed by logical name and then by entity record GUID, simulating
    /// how entities are persisted in Dataverse tables (with the logical name) and then the records themselves
    /// where the Primary Key is the Guid. This class provides a complete mock of the IOrganizationService
    /// for unit testing plugins, custom workflow activities, and other CRM-related code.
    /// </summary>
    public partial class XrmFakedContext : IXrmContext
    {
        /// <summary>
        /// Gets or sets the underlying <see cref="IOrganizationService"/> instance used by this context.
        /// This service is automatically created and configured when <see cref="GetOrganizationService"/> is called.
        /// </summary>
        protected internal IOrganizationService Service { get; set; }

        private IServiceEndpointNotificationService _serviceEndpointNotificationService;

        private readonly Lazy<XrmFakedTracingService> _tracingService = new Lazy<XrmFakedTracingService>(() => new XrmFakedTracingService());

        /// <summary>
        /// All proxy type assemblies available on mocked database.
        /// </summary>
        private List<Assembly> ProxyTypesAssemblies { get; set; }

        /// <summary>
        /// Gets the tracing service used to capture trace messages during plugin and workflow activity execution.
        /// </summary>
        protected internal XrmFakedTracingService TracingService => _tracingService.Value;

        /// <summary>
        /// Gets or sets a value indicating whether this context has been initialized with entity data.
        /// The Initialize method can only be called once per context instance.
        /// </summary>
        protected internal bool Initialised { get; set; }

        /// <summary>
        /// Gets or sets the in-memory data store containing all entities in this fake CRM context.
        /// The outer dictionary is keyed by entity logical name, and the inner dictionary maps entity GUIDs to Entity objects.
        /// This is a thread-safe concurrent dictionary to support parallel operations.
        /// </summary>
        public ConcurrentDictionary<string, ConcurrentDictionary<Guid, Entity>> Data { get; set; }

        /// <summary>
        /// Lock object used to ensure thread safety for complex operations on the Data dictionary
        /// that require atomicity across multiple dictionary operations.
        /// </summary>
        private readonly object _dataLock = new object();

        /// <summary>
        /// Counter for generating auto-incrementing version numbers (RowVersion).
        /// </summary>
        private long _currentVersionNumber = 0;

        /// <summary>
        /// Generates the next version number in a thread-safe manner.
        /// </summary>
        protected internal long GetNextVersionNumber()
        {
            return System.Threading.Interlocked.Increment(ref _currentVersionNumber);
        }

        /// <summary>
        /// Specify which assembly is used to search for early-bound proxy
        /// types when used within simulated CRM context.
        ///
        /// If you want to specify multiple different assemblies for early-bound
        /// proxy types please use <see cref="EnableProxyTypes(Assembly)"/>
        /// instead.
        /// </summary>
        public Assembly ProxyTypesAssembly
        {
            get
            {
                // TODO What we should do when ProxyTypesAssemblies contains multiple assemblies? One shouldn't throw exceptions from properties.
                return ProxyTypesAssemblies.FirstOrDefault();
            }
            set
            {
                ProxyTypesAssemblies = new List<Assembly>();
                if (value != null)
                {
                    ProxyTypesAssemblies.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the user to assign the CreatedBy and ModifiedBy properties when entities are added to the context.
        /// All requests will be executed on behalf of this user, simulating the calling user in the CRM system.
        /// </summary>
        public EntityReference CallerId { get; set; }

        /// <summary>
        /// Gets or sets the business unit context for the current user.
        /// This is used to simulate the business unit hierarchy in Dynamics 365.
        /// </summary>
        public EntityReference BusinessUnitId { get; set; }

        /// <summary>
        /// Delegate type for custom service request execution handlers.
        /// Allows registering custom logic to handle specific <see cref="OrganizationRequest"/> types.
        /// </summary>
        /// <param name="req">The organization request to be executed.</param>
        /// <returns>The <see cref="OrganizationResponse"/> resulting from executing the request.</returns>
        public delegate OrganizationResponse ServiceRequestExecution(OrganizationRequest req);

        /// <summary>
        /// Probably should be replaced by FakeMessageExecutors, more generic, which can use custom interfaces rather than a single method / delegate
        /// </summary>
        private Dictionary<Type, ServiceRequestExecution> ExecutionMocks { get; set; }

        private Dictionary<Type, IFakeMessageExecutor> FakeMessageExecutors { get; set; }

        private Dictionary<string, IFakeMessageExecutor> GenericFakeMessageExecutors { get; set; }

        private Dictionary<string, XrmFakedRelationship> Relationships { get; set; }

        /// <summary>
        /// Stores hierarchical relationship definitions for entities.
        /// Key is the entity logical name, value is the attribute name that stores the parent reference
        /// (e.g., "account" -> "parentaccountid").
        /// Used by hierarchy operators (Above, AboveOrEqual/EqOrAbove, Under, UnderOrEqual/EqOrUnder, NotUnder).
        /// </summary>
        public Dictionary<string, string> HierarchicalRelationships { get; set; }


        /// <summary>
        /// Gets or sets the entity initializer service responsible for setting default values on entities
        /// when they are created or initialized. Use this to customize how entities are populated with
        /// default field values during testing.
        /// </summary>
        public IEntityInitializerService EntityInitializerService { get; set; }

        /// <summary>
        /// Gets or sets the access rights repository that manages security permissions for entities.
        /// Use this to configure and test security-related operations such as GrantAccess, ModifyAccess,
        /// RevokeAccess, and RetrievePrincipalAccess requests.
        /// </summary>
        public IAccessRightsRepository AccessRightsRepository { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of records returned by a RetrieveMultiple operation.
        /// Default value is 5000, which matches the Dynamics 365 default behavior.
        /// </summary>
        public int MaxRetrieveCount { get; set; }

        /// <summary>
        /// Gets or sets the entity initialization level that determines how entities are populated
        /// with default values when added to the context.
        /// </summary>
        public EntityInitializationLevel InitializationLevel { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XrmFakedContext"/> class.
        /// Sets up default values, registers built-in message executors, and prepares the
        /// in-memory data store for simulating Dynamics 365 operations.
        /// </summary>
        public XrmFakedContext()
        {
            MaxRetrieveCount = 5000;

            AttributeMetadataNames = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            Data = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, Entity>>();
            ExecutionMocks = new Dictionary<Type, ServiceRequestExecution>();
            OptionSetValuesMetadata = new Dictionary<string, OptionSetMetadata>();
            StatusAttributeMetadata = new Dictionary<string, StatusAttributeMetadata>();

            FakeMessageExecutors = new Dictionary<Type, IFakeMessageExecutor>();
            GenericFakeMessageExecutors = new Dictionary<string, IFakeMessageExecutor>();

            var executorTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IFakeMessageExecutor)));

            foreach (var executorType in executorTypes)
            {
                try
                {
                    var executor = Activator.CreateInstance(executorType) as IFakeMessageExecutor;
                    if (executor != null)
                    {
                        var responsibleType = executor.GetResponsibleRequestType();
                        if (responsibleType != null && !FakeMessageExecutors.ContainsKey(responsibleType))
                        {
                            FakeMessageExecutors.Add(responsibleType, executor);

                            // Also register bulk operation executors by RequestName for loosely-typed requests
                            // (resolves upstream issue #XXX - bulk operations with OrganizationRequest)
                            if (responsibleType.Name.EndsWith("MultipleRequest"))
                            {
                                // Extract request name from type (e.g., CreateMultipleRequest -> CreateMultiple)
                                string requestName = responsibleType.Name.Replace("Request", "");
                                if (!GenericFakeMessageExecutors.ContainsKey(requestName))
                                {
                                    GenericFakeMessageExecutors.Add(requestName, executor);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip executors that fail to instantiate or register
                    // This can happen if the request type doesn't exist in the SDK version being used
                    System.Diagnostics.Debug.WriteLine($"Failed to register executor {executorType.Name}: {ex.Message}");
                    continue;
                }
            }

            Relationships = new Dictionary<string, XrmFakedRelationship>();

            HierarchicalRelationships = new Dictionary<string, string>();

            EntityInitializerService = new DefaultEntityInitializerService();

            AccessRightsRepository = new AccessRightsRepository();

            SystemTimeZone = TimeZoneInfo.Local;
            DateBehaviour = DefaultDateBehaviour();

            EntityMetadata = new Dictionary<string, EntityMetadata>();

            UsePipelineSimulation = false;

            InitializationLevel = EntityInitializationLevel.Default;

            ProxyTypesAssemblies = new List<Assembly>();
        }

        /// <summary>
        /// Initializes the context with the provided entities, populating the in-memory data store.
        /// This method can only be called once per context instance and is typically called at the
        /// beginning of a unit test to set up the test data.
        /// </summary>
        /// <param name="entities">The collection of entities to add to the fake CRM context as initial data.</param>
        /// <exception cref="Exception">Thrown if Initialize has already been called on this context instance.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the entities parameter is null.</exception>
        /// <remarks>
        /// When ProxyTypesAssembly is set, Initialize() allows both early-bound and late-bound entities
        /// to be added to the context. Late-bound entities (entities whose types are not found in the
        /// ProxyTypesAssembly) will have their attribute metadata inferred from their attributes.
        /// This supports scenarios where test data includes entities not present in the early-bound assembly.
        /// Fix for GitHub issue #462.
        /// </remarks>
        public virtual void Initialize(IEnumerable<Entity> entities)
        {
            if (Initialised)
            {
                throw new Exception("Initialize should be called only once per unit test execution and XrmFakedContext instance.");
            }

            if (entities == null)
            {
                throw new InvalidOperationException("The entities parameter must be not null");
            }

            foreach (var e in entities)
            {
                // Pass validateEntityType=false to allow late-bound entities alongside early-bound entities
                // when ProxyTypesAssembly is set. Fix for GitHub issue #462.
                AddEntityWithDefaults(e, clone: true, usePluginPipeline: false, validateEntityType: false);
            }

            Initialised = true;
        }

        /// <summary>
        /// Initializes the context with a single entity, populating the in-memory data store.
        /// This is a convenience overload that wraps the entity in a collection.
        /// </summary>
        /// <param name="e">The entity to add to the fake CRM context as initial data.</param>
        public void Initialize(Entity e)
        {
            this.Initialize(new List<Entity>() { e });
        }

        /// <summary>
        /// Enables support for the early-bound entity types exposed in a specified assembly.
        /// Call this method to register additional assemblies containing strongly-typed entity classes
        /// when using multiple assemblies for early-bound types.
        /// </summary>
        /// <param name="assembly">An assembly containing early-bound entity types generated by CrmSvcUtil or similar tools.</param>
        /// <exception cref="ArgumentNullException">Thrown if the assembly parameter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the specified assembly has already been enabled.</exception>
        /// <remarks>
        /// See issue #334 on GitHub. This has quite similar idea as is on SDK method
        /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.client.organizationserviceproxy.enableproxytypes.
        /// </remarks>
        public void EnableProxyTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (ProxyTypesAssemblies.Contains(assembly))
            {
                throw new InvalidOperationException($"Proxy types assembly { assembly.GetName().Name } is already enabled.");
            }

            ProxyTypesAssemblies.Add(assembly);
        }

        /// <summary>
        /// Registers a custom execution mock for a specific organization request type.
        /// This allows you to define custom behavior for handling specific CRM requests during testing.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="OrganizationRequest"/> to mock.</typeparam>
        /// <param name="mock">The delegate that will be invoked when a request of type T is executed.</param>
        public void AddExecutionMock<T>(ServiceRequestExecution mock) where T : OrganizationRequest
        {
            if (!ExecutionMocks.ContainsKey(typeof(T)))
                ExecutionMocks.Add(typeof(T), mock);
            else
                ExecutionMocks[typeof(T)] = mock;
        }

        /// <summary>
        /// Removes a previously registered execution mock for a specific organization request type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="OrganizationRequest"/> whose mock should be removed.</typeparam>
        public void RemoveExecutionMock<T>() where T : OrganizationRequest
        {
            ExecutionMocks.Remove(typeof(T));
        }

        /// <summary>
        /// Registers a custom fake message executor for a specific organization request type.
        /// This allows you to provide a complete implementation for handling specific CRM requests during testing.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="OrganizationRequest"/> to handle.</typeparam>
        /// <param name="executor">The <see cref="IFakeMessageExecutor"/> implementation that will handle requests of type T.</param>
        public void AddFakeMessageExecutor<T>(IFakeMessageExecutor executor) where T : OrganizationRequest
        {
            if (!FakeMessageExecutors.ContainsKey(typeof(T)))
                FakeMessageExecutors.Add(typeof(T), executor);
            else
                FakeMessageExecutors[typeof(T)] = executor;
        }

        /// <summary>
        /// Removes a previously registered fake message executor for a specific organization request type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="OrganizationRequest"/> whose executor should be removed.</typeparam>
        public void RemoveFakeMessageExecutor<T>() where T : OrganizationRequest
        {
            FakeMessageExecutors.Remove(typeof(T));
        }

        /// <summary>
        /// Registers a generic fake message executor for a specific message name.
        /// This is useful for handling custom actions or messages that don't have strongly-typed request classes.
        /// </summary>
        /// <param name="message">The message name (e.g., "custom_Action") that the executor will handle.</param>
        /// <param name="executor">The <see cref="IFakeMessageExecutor"/> implementation that will handle the message.</param>
        public void AddGenericFakeMessageExecutor(string message, IFakeMessageExecutor executor)
        {
            if (!GenericFakeMessageExecutors.ContainsKey(message))
                GenericFakeMessageExecutors.Add(message, executor);
            else
                GenericFakeMessageExecutors[message] = executor;
        }

        /// <summary>
        /// Removes a previously registered generic fake message executor for a specific message name.
        /// </summary>
        /// <param name="message">The message name whose executor should be removed.</param>
        public void RemoveGenericFakeMessageExecutor(string message)
        {
            if (GenericFakeMessageExecutors.ContainsKey(message))
                GenericFakeMessageExecutors.Remove(message);
        }

        /// <summary>
        /// Registers a relationship definition that can be used in Associate and Disassociate operations.
        /// This is required for testing N:N (many-to-many) relationship operations.
        /// </summary>
        /// <param name="schemaname">The schema name of the relationship as defined in Dynamics 365.</param>
        /// <param name="relationship">The <see cref="XrmFakedRelationship"/> definition containing relationship metadata.</param>
        public void AddRelationship(string schemaname, XrmFakedRelationship relationship)
        {
            Relationships.Add(schemaname, relationship);
        }

        /// <summary>
        /// Removes a previously registered relationship definition.
        /// </summary>
        /// <param name="schemaname">The schema name of the relationship to remove.</param>
        public void RemoveRelationship(string schemaname)
        {
            Relationships.Remove(schemaname);
        }

        /// <summary>
        /// Retrieves a relationship definition by its schema name.
        /// </summary>
        /// <param name="schemaName">The schema name of the relationship to retrieve.</param>
        /// <returns>The <see cref="XrmFakedRelationship"/> definition if found; otherwise, null.</returns>
        public XrmFakedRelationship GetRelationship(string schemaName)
        {
            if (Relationships.ContainsKey(schemaName))
            {
                return Relationships[schemaName];
            }

            return null;
        }

        /// <summary>
        /// Adds an attribute mapping between source and target entities for use with InitializeFrom operations.
        /// This simulates the attribute mapping configuration in Dynamics 365 that copies values from
        /// one entity to another when creating related records.
        /// </summary>
        /// <param name="sourceEntityName">The logical name of the source entity.</param>
        /// <param name="sourceAttributeName">The logical name of the source attribute.</param>
        /// <param name="targetEntityName">The logical name of the target entity.</param>
        /// <param name="targetAttributeName">The logical name of the target attribute that will receive the mapped value.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null or whitespace.</exception>
        public void AddAttributeMapping(string sourceEntityName, string sourceAttributeName, string targetEntityName, string targetAttributeName)
        {
            if (string.IsNullOrWhiteSpace(sourceEntityName))
                throw new ArgumentNullException("sourceEntityName");
            if (string.IsNullOrWhiteSpace(sourceAttributeName))
                throw new ArgumentNullException("sourceAttributeName");
            if (string.IsNullOrWhiteSpace(targetEntityName))
                throw new ArgumentNullException("targetEntityName");
            if (string.IsNullOrWhiteSpace(targetAttributeName))
                throw new ArgumentNullException("targetAttributeName");

            var entityMap = new Entity
            {
                LogicalName = "entitymap",
                Id = Guid.NewGuid(),
                ["targetentityname"] = targetEntityName,
                ["sourceentityname"] = sourceEntityName
            };

            var attributeMap = new Entity
            {
                LogicalName = "attributemap",
                Id = Guid.NewGuid(),
                ["entitymapid"] = new EntityReference("entitymap", entityMap.Id),
                ["targetattributename"] = targetAttributeName,
                ["sourceattributename"] = sourceAttributeName
            };

            AddEntityWithDefaults(entityMap);
            AddEntityWithDefaults(attributeMap);
        }

        /// <summary>
        /// Gets or creates a mocked <see cref="IOrganizationService"/> instance configured with this fake context.
        /// The returned service intercepts all CRM operations and executes them against the in-memory data store.
        /// </summary>
        /// <returns>A mocked <see cref="IOrganizationService"/> instance that can be used to perform CRM operations in tests.</returns>
        public virtual IOrganizationService GetOrganizationService()
        {
            if (this is XrmRealContext)
            {
                Service = GetOrganizationService();
                return Service;
            }
            return GetFakedOrganizationService(this);
        }

        /// <summary>
        /// Gets a mocked <see cref="IOrganizationService"/> instance configured with this fake context.
        /// </summary>
        /// <returns>A mocked <see cref="IOrganizationService"/> instance.</returns>
        /// <remarks>This method is deprecated. Use <see cref="GetOrganizationService"/> instead.</remarks>
        [Obsolete("Use GetOrganizationService instead")]
        public IOrganizationService GetFakedOrganizationService()
        {
            return GetFakedOrganizationService(this);
        }

        /// <summary>
        /// Creates and configures a mocked <see cref="IOrganizationService"/> for the specified context.
        /// Sets up fake handlers for all CRUD operations and organization requests.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> to configure the service with.</param>
        /// <returns>A mocked <see cref="IOrganizationService"/> instance.</returns>
        protected IOrganizationService GetFakedOrganizationService(XrmFakedContext context)
        {
            if (context.Service != null)
            {
                return context.Service;
            }

            var fakedService = A.Fake<IOrganizationService>();

            //Fake CRUD methods
            FakeRetrieve(context, fakedService);
            FakeCreate(context, fakedService);
            FakeUpdate(context, fakedService);
            FakeDelete(context, fakedService);

            //Fake / Intercept Retrieve Multiple Requests
            FakeRetrieveMultiple(context, fakedService);

            //Fake / Intercept other requests
            FakeExecute(context, fakedService);
            FakeAssociate(context, fakedService);
            FakeDisassociate(context, fakedService);
            context.Service = fakedService;

            return context.Service;
        }

        /// <summary>
        /// Configures the Execute method handler on the faked organization service.
        /// Routes incoming <see cref="OrganizationRequest"/> objects to the appropriate executor based on request type.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> containing the executors and mocks.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> to configure.</param>
        /// <remarks>
        /// Not all OrganizationRequest types are implemented. Custom executors can be added using
        /// <see cref="AddFakeMessageExecutor{T}"/> or <see cref="AddGenericFakeMessageExecutor"/>.
        /// </remarks>
        public static void FakeExecute(XrmFakedContext context, IOrganizationService fakedService)
        {
            OrganizationResponse response = null;
            Func<OrganizationRequest, OrganizationResponse> execute = (req) =>
            {
                if (context.ExecutionMocks.ContainsKey(req.GetType()))
                    return context.ExecutionMocks[req.GetType()].Invoke(req);

                if (context.FakeMessageExecutors.ContainsKey(req.GetType())
                    && context.FakeMessageExecutors[req.GetType()].CanExecute(req))
                    return context.FakeMessageExecutors[req.GetType()].Execute(req, context);

                if (req.GetType() == typeof(OrganizationRequest)
                    && context.GenericFakeMessageExecutors.ContainsKey(req.RequestName))
                    return context.GenericFakeMessageExecutors[req.RequestName].Execute(req, context);

                throw PullRequestException.NotImplementedOrganizationRequest(req.GetType());
            };

            A.CallTo(() => fakedService.Execute(A<OrganizationRequest>._))
                .Invokes((OrganizationRequest req) => response = execute(req))
                .ReturnsLazily((OrganizationRequest req) => response);
        }

        /// <summary>
        /// Configures the Associate method handler on the faked organization service.
        /// Handles Associate requests that link entities through N:N relationships.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> containing the data and relationship definitions.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> to configure.</param>
        public static void FakeAssociate(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Associate(A<string>._, A<Guid>._, A<Relationship>._, A<EntityReferenceCollection>._))
                .Invokes((string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection entityCollection) =>
                {
                    if (context.FakeMessageExecutors.ContainsKey(typeof(AssociateRequest)))
                    {
                        var request = new AssociateRequest()
                        {
                            Target = new EntityReference() { Id = entityId, LogicalName = entityName },
                            Relationship = relationship,
                            RelatedEntities = entityCollection
                        };
                        context.FakeMessageExecutors[typeof(AssociateRequest)].Execute(request, context);
                    }
                    else
                        throw PullRequestException.NotImplementedOrganizationRequest(typeof(AssociateRequest));
                });
        }

        /// <summary>
        /// Configures the Disassociate method handler on the faked organization service.
        /// Handles Disassociate requests that unlink entities in N:N relationships.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> containing the data and relationship definitions.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> to configure.</param>
        public static void FakeDisassociate(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Disassociate(A<string>._, A<Guid>._, A<Relationship>._, A<EntityReferenceCollection>._))
                .Invokes((string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection entityCollection) =>
                {
                    if (context.FakeMessageExecutors.ContainsKey(typeof(DisassociateRequest)))
                    {
                        var request = new DisassociateRequest()
                        {
                            Target = new EntityReference() { Id = entityId, LogicalName = entityName },
                            Relationship = relationship,
                            RelatedEntities = entityCollection
                        };
                        context.FakeMessageExecutors[typeof(DisassociateRequest)].Execute(request, context);
                    }
                    else
                        throw PullRequestException.NotImplementedOrganizationRequest(typeof(DisassociateRequest));
                });
        }

        /// <summary>
        /// Configures the RetrieveMultiple method handler on the faked organization service.
        /// Handles QueryExpression, FetchExpression, and QueryByAttribute queries against the in-memory data store.
        /// </summary>
        /// <param name="context">The <see cref="XrmFakedContext"/> containing the data to query.</param>
        /// <param name="fakedService">The faked <see cref="IOrganizationService"/> to configure.</param>
        public static void FakeRetrieveMultiple(XrmFakedContext context, IOrganizationService fakedService)
        {
            EntityCollection entities = null;
            Func<QueryBase, EntityCollection> retriveMultiple = (QueryBase req) =>
            {
                var request = new RetrieveMultipleRequest { Query = req };

                var executor = new RetrieveMultipleRequestExecutor();
                var response = executor.Execute(request, context) as RetrieveMultipleResponse;

                return response.EntityCollection;
            };

            //refactored from RetrieveMultipleExecutor
            A.CallTo(() => fakedService.RetrieveMultiple(A<QueryBase>._))
                .Invokes((QueryBase req) => entities = retriveMultiple(req))
                .ReturnsLazily((QueryBase req) => entities);
        }

        /// <summary>
        /// Gets a faked <see cref="IServiceEndpointNotificationService"/> instance for testing plugins
        /// that use service endpoint notifications (Azure Service Bus integration).
        /// </summary>
        /// <returns>A faked <see cref="IServiceEndpointNotificationService"/> instance.</returns>
        public IServiceEndpointNotificationService GetFakedServiceEndpointNotificationService()
        {
            return _serviceEndpointNotificationService ??
                   (_serviceEndpointNotificationService = A.Fake<IServiceEndpointNotificationService>());
        }
#if FAKE_XRM_EASY_9
        /// <summary>
        /// Gets or sets the entity logical name for which the entity data source should be retrieved.
        /// This is used by <see cref="GetFakedEntityDataSourceRetrieverService"/> when auto-lookup is performed.
        /// Set this before calling GetFakedEntityDataSourceRetrieverService() to enable automatic lookup
        /// of the entity data source from EntityMetadata's DataSourceId property.
        /// </summary>
        /// <remarks>
        /// This property is typically set automatically when resolving IEntityDataSourceRetrieverService
        /// from the service provider during plugin execution. It is populated from either:
        /// <list type="bullet">
        /// <item><description>The plugin context's PrimaryEntityName</description></item>
        /// <item><description>The EntityName from a QueryExpression in the Query input parameter</description></item>
        /// </list>
        /// You can also set this property manually before calling <see cref="GetFakedEntityDataSourceRetrieverService"/>
        /// for direct testing scenarios.
        /// </remarks>
        public string CurrentVirtualEntityLogicalName { get; set; }

        /// <summary>
        /// Gets a faked <see cref="IEntityDataSourceRetrieverService"/> instance for testing
        /// virtual entity data providers in Dynamics 365 v9.x and later.
        /// </summary>
        /// <returns>A faked <see cref="IEntityDataSourceRetrieverService"/> instance configured with the current context's entity data source retriever.</returns>
        /// <remarks>
        /// This method supports two modes of operation:
        /// <list type="number">
        /// <item>
        /// <description>
        /// <b>Manual configuration</b>: If <see cref="EntityDataSourceRetriever"/> is set, it is returned directly.
        /// This preserves backward compatibility with existing code.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>Automatic lookup (GitHub issue #579)</b>: If <see cref="EntityDataSourceRetriever"/> is null,
        /// the method attempts to auto-lookup the entity data source by:
        /// <list type="bullet">
        /// <item><description>Getting the EntityMetadata for the virtual entity (from <see cref="CurrentVirtualEntityLogicalName"/>)</description></item>
        /// <item><description>Reading the DataSourceId from the EntityMetadata using reflection (since it's a sealed property)</description></item>
        /// <item><description>Looking up an "entitydatasource" entity in the Data dictionary with that ID</description></item>
        /// </list>
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        public IEntityDataSourceRetrieverService GetFakedEntityDataSourceRetrieverService()
        {
            var service = A.Fake<IEntityDataSourceRetrieverService>();
            A.CallTo(() => service.RetrieveEntityDataSource())
                .ReturnsLazily(() =>
                {
                    // If EntityDataSourceRetriever is explicitly set, use it (backward compatibility)
                    if (EntityDataSourceRetriever != null)
                    {
                        return EntityDataSourceRetriever;
                    }

                    // Auto-lookup: try to find entity data source from metadata
                    return GetEntityDataSourceFromMetadata(CurrentVirtualEntityLogicalName);
                });
            return service;
        }

        /// <summary>
        /// Gets the entity data source for a virtual entity by looking up its DataSourceId from EntityMetadata.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the virtual entity to look up the data source for.</param>
        /// <returns>The entitydatasource entity if found; otherwise, null.</returns>
        /// <remarks>
        /// This method uses reflection to access the DataSourceId property from EntityMetadata since it's a sealed property.
        /// The entitydatasource entity is looked up in the Data dictionary by matching the DataSourceId.
        /// </remarks>
        protected internal Entity GetEntityDataSourceFromMetadata(string entityLogicalName)
        {
            if (string.IsNullOrWhiteSpace(entityLogicalName))
            {
                return null;
            }

            // Check if we have metadata for this entity
            if (!EntityMetadata.ContainsKey(entityLogicalName))
            {
                return null;
            }

            var entityMetadata = EntityMetadata[entityLogicalName];

            // Use reflection to get the DataSourceId property (it's a sealed property)
            var dataSourceIdProperty = typeof(EntityMetadata).GetProperty("DataSourceId",
                BindingFlags.Public | BindingFlags.Instance);

            if (dataSourceIdProperty == null)
            {
                return null;
            }

            var dataSourceIdValue = dataSourceIdProperty.GetValue(entityMetadata);
            if (dataSourceIdValue == null)
            {
                return null;
            }

            Guid dataSourceId;
            if (dataSourceIdValue is Guid?)
            {
                var nullableGuid = (Guid?)dataSourceIdValue;
                if (!nullableGuid.HasValue || nullableGuid.Value == Guid.Empty)
                {
                    return null;
                }
                dataSourceId = nullableGuid.Value;
            }
            else if (dataSourceIdValue is Guid)
            {
                dataSourceId = (Guid)dataSourceIdValue;
                if (dataSourceId == Guid.Empty)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            // Look up the entitydatasource entity in the Data dictionary
            if (Data.TryGetValue("entitydatasource", out var entityDataSourceDict) &&
                entityDataSourceDict.TryGetValue(dataSourceId, out var entityDataSource))
            {
                return entityDataSource;
            }

            return null;
        }
#endif
    }
}
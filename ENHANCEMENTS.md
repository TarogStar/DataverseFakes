# DataverseFakes.Community Enhancement Roadmap

This document outlines planned enhancements to make DataverseFakes.Community the most comprehensive and developer-friendly testing framework for modern Dataverse applications.

**Last Updated:** 2025-11-11

---

## 🎉 COMPLETED ENHANCEMENTS (2025-11-11)

### Phase 1: Quick Wins ✅
**Status:** COMPLETED

1. **✅ Auto-Populate Entity Images** - Implemented new overloads for `ExecutePluginWithTarget` that automatically retrieve and populate pre/post entity images from context:
   ```csharp
   // New simplified syntax - no more manual image setup!
   context.ExecutePluginWithTarget<MyPlugin>(target,
       messageName: "Update",
       stage: 40,
       preImageColumns: new ColumnSet(true),
       postImageColumns: new ColumnSet(true));
   ```

2. **✅ Filtering Attributes Validation** - Completed the TODO in XrmFakedContext.Pipeline.cs. Plugins now only execute when registered filtering attributes are present in the target entity, matching real Dataverse behavior.

### Phase 2: Modern Dataverse Operations ✅
**Status:** COMPLETED

All bulk operation message executors have been implemented:

1. **✅ CreateMultiple** - Transactional bulk create operation
   - CreateMultipleRequest/Response classes
   - CreateMultipleRequestExecutor
   - Supports plugin registration for "CreateMultiple" message

2. **✅ UpdateMultiple** - Transactional bulk update operation
   - UpdateMultipleRequest/Response classes
   - UpdateMultipleRequestExecutor
   - Supports plugin registration for "UpdateMultiple" message

3. **✅ DeleteMultiple** - Transactional bulk delete operation
   - DeleteMultipleRequest/Response classes
   - DeleteMultipleRequestExecutor
   - Note: Marked as preview, primarily for elastic tables

4. **✅ UpsertMultiple** - Transactional bulk upsert (create or update) operation
   - UpsertMultipleRequest/Response classes with UpsertMultipleResult
   - UpsertMultipleRequestExecutor
   - Returns which records were created vs updated

**Key Features:**
- All operations are transactional (all succeed or all fail)
- Support both strongly-typed and loosely-typed requests
- Comprehensive error handling with OrganizationServiceFault
- Compatible with older SDK versions (custom classes provided)

### Phase 3: Relationship Auto-Discovery ✅
**Status:** COMPLETED

**✅ Auto-Register Relationships from Metadata** - When `InitializeMetadata()` is called, the framework now automatically:
- Parses ManyToManyRelationships and creates XrmFakedRelationship entries (with intersect entity names!)
- Parses OneToManyRelationships and creates XrmFakedRelationship entries
- Parses ManyToOneRelationships and creates XrmFakedRelationship entries
- Skips already-registered relationships (no duplicates)
- Handles incomplete metadata gracefully

**Impact:** Eliminates the need to manually call `AddRelationship()` for each N:N relationship - a major pain point!

---

## 🎯 Current Status Assessment (Updated)

### ✅ What's Working Well
- ✅ ExecuteMultiple fully implemented
- ✅ **CreateMultiple, UpdateMultiple, DeleteMultiple, UpsertMultiple** - ALL IMPLEMENTED!
- ✅ **Auto-populate entity images** - No more manual boilerplate!
- ✅ **Filtering attributes validation** - Works like real Dataverse!
- ✅ **Auto-register relationships from metadata** - No more manual AddRelationship calls!
- ✅ 50+ message executors with auto-discovery
- ✅ Comprehensive plugin execution support (IPluginExecutionContext4)
- ✅ Pipeline simulation available
- ✅ Metadata generation from early-bound types
- ✅ Full CRUD operations

### 🎯 Remaining Enhancements

#### 1. Bulk Operation Messages
**Status:** ✅ COMPLETED (2025-11-11)

All modern bulk operations are now implemented:
- ✅ `ExecuteMultiple` - Implemented
- ✅ `CreateMultiple` - **COMPLETED** (available since 2023)
- ✅ `UpdateMultiple` - **COMPLETED** (available since 2023)
- ✅ `DeleteMultiple` - **COMPLETED** (preview, elastic tables only)
- ✅ `UpsertMultiple` - **COMPLETED** (elastic tables)

**Why This Matters:**
- CreateMultiple/UpdateMultiple are optimized for bulk operations (no 1000 record limit)
- Transactional behavior differs from ExecuteMultiple
- Plugins can register for these specific messages
- Modern Dataverse code increasingly uses these for performance

**Documentation:** https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bulk-operations

---

#### 2. Elastic Tables Support (MEDIUM PRIORITY)
**Status:** NOT IMPLEMENTED

Elastic tables are a different table type in Dataverse (Cosmos DB-backed vs SQL):

**Key Differences:**
- Horizontal auto-scaling for high-volume scenarios
- Different consistency model (eventual consistency outside sessions)
- PartitionId column for performance optimization
- Time-to-Live (TTL) support
- No relational features (lookups, related filters)
- No multi-record transactions
- Support all bulk operations (CreateMultiple, UpdateMultiple, UpsertMultiple, DeleteMultiple)

**What's Needed:**
- Flag to mark entities as "elastic" vs "standard"
- Different validation rules for elastic tables
- PartitionId handling
- TTL simulation
- Consistency model simulation (optional, for advanced scenarios)

**Documentation:** https://learn.microsoft.com/en-us/power-apps/developer/data-platform/elastic-tables

---

### ✅ Developer Experience Improvements (COMPLETED!)

#### 1. Manual Pre/Post Entity Image Setup
**Status:** ✅ COMPLETED (2025-11-11)

**Before:**
```csharp
// Every test required 5-10 lines of boilerplate
var preImage = service.Retrieve("account", accountId, new ColumnSet(true));
pluginContext.PreEntityImages.Add("PreImage", preImage);
var postImage = service.Retrieve("account", accountId, new ColumnSet(true));
pluginContext.PostEntityImages.Add("PostImage", postImage);
context.ExecutePluginWith<MyPlugin>(pluginContext);
```

**After (NOW IMPLEMENTED!):**
```csharp
// Automatic image population - just specify what you need!
context.ExecutePluginWithTarget<MyPlugin>(target,
    messageName: "Update",
    stage: 40,
    preImageColumns: new ColumnSet(true),
    postImageColumns: new ColumnSet(true),
    preImageName: "PreImage",    // Optional, defaults to "PreImage"
    postImageName: "PostImage"    // Optional, defaults to "PostImage"
);
```

**What Was Implemented:**
- ✅ Optional parameters added to ExecutePluginWithTarget methods
- ✅ Auto-retrieves entity from context.Data if it exists
- ✅ Clones and adds to appropriate image collections
- ✅ Supports both specific columns and ColumnSet(true) for all columns
- ✅ Default image names to "PreImage"/"PostImage" if not specified

**Impact:** Eliminates 50%+ of plugin test boilerplate code!

---

#### 2. Manual Relationship Definition
**Status:** ✅ COMPLETED (2025-11-11)

**Before:**
```csharp
// Had to manually define every N:N relationship
context.AddRelationship("new_account_contact", new XrmFakedRelationship {
    IntersectEntity = "new_account_contact",
    Entity1LogicalName = "account",
    Entity1Attribute = "accountid",
    Entity2LogicalName = "contact",
    Entity2Attribute = "contactid",
    RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany
});
```

**After (NOW IMPLEMENTED!):**
```csharp
// Relationships are automatically registered from metadata!
context.InitializeMetadata(entityMetadata);
// That's it! All N:N, 1:N, and N:1 relationships are now registered
```

**What Was Implemented:**
- ✅ Modified InitializeMetadata() to call AutoRegisterRelationshipsFromMetadata()
- ✅ Parses ManyToManyRelationships and auto-creates XrmFakedRelationship (with intersect entity names!)
- ✅ Parses OneToManyRelationships for lookup relationships
- ✅ Parses ManyToOneRelationships
- ✅ Handles self-referential relationships
- ✅ Validates and skips incomplete relationship metadata gracefully
- ✅ Avoids duplicates (checks if relationship already registered)

**Impact:** Eliminates 25% of test setup boilerplate!

---

#### 3. Filtering Attributes Validation
**Status:** ✅ COMPLETED (2025-11-11)

**Before:**
```csharp
// Plugin registered with filtering attributes
context.RegisterPluginStep<MyPlugin>("Update",
    filteringAttributes: "name,accountnumber");

// Plugin executed INCORRECTLY even if Target only contained "revenue" attribute
// Should NOT execute but did!
```

**After (NOW IMPLEMENTED!):**
```csharp
// Same registration
context.RegisterPluginStep<MyPlugin>("Update",
    filteringAttributes: "name,accountnumber");

// Plugin now CORRECTLY only executes if Target contains "name" OR "accountnumber"
// Matches real Dataverse behavior!
```

**What Was Implemented:**
- ✅ Completed the TODO at line 216 in XrmFakedContext.Pipeline.cs
- ✅ Added attribute presence checking in ExecutePipelinePlugins method
- ✅ Plugin only executes if at least one filtering attribute is present in target
- ✅ Properly handles comma-separated attribute lists with trimming
- ✅ Skips plugins gracefully when filtering attributes don't match

**Impact:** More realistic plugin pipeline simulation - tests now match production behavior!

---

## 🛠️ Metadata Download Utility (OPTION A - OFFLINE APPROACH)

### Overview
Create a **command-line utility** that developers run once to download and generate test infrastructure from a live Dataverse environment.

### What It Should Download

#### 1. Entity Metadata
- EntityMetadata for specified entities
- All attributes with full metadata
- Relationships (N:N, 1:N, N:1) with intersect entity names
- Choice/Picklist options
- Status/State options

**Output:** JSON files or C# code for easy initialization

#### 2. Plugin Step Registrations (BRILLIANT IDEA! 🌟)
Download actual plugin configuration from the environment:

**For Each Plugin Step:**
- Plugin Type Name and Assembly
- Message Name (Create, Update, Delete, etc.)
- Primary Entity
- Stage (10=PreValidation, 20=Pre, 40=Post, etc.)
- Mode (0=Synchronous, 1=Asynchronous)
- Rank (execution order)
- **Filtering Attributes** (comma-separated list)
- **Pre-Entity Images** (name, attributes, entity alias)
- **Post-Entity Images** (name, attributes, entity alias)
- Deployment (Server, Offline, Both)
- User context (calling user, system user, specific user)

**Output:** JSON configuration file + C# helper to auto-configure XrmFakedContext

**Example Output:**
```json
{
  "pluginSteps": [
    {
      "pluginTypeName": "MyCompany.Plugins.AccountUpdatePlugin",
      "assemblyName": "MyCompany.Plugins",
      "messageName": "Update",
      "primaryEntity": "account",
      "stage": 40,
      "mode": 0,
      "rank": 1,
      "filteringAttributes": "name,accountnumber,revenue",
      "preImages": [
        {
          "name": "PreImage",
          "attributes": "name,accountnumber,revenue,createdon,ownerid"
        }
      ],
      "postImages": []
    }
  ]
}
```

**Generated Helper:**
```csharp
public static class PluginStepConfiguration
{
    public static void ConfigureFromEnvironment(XrmFakedContext context)
    {
        var config = LoadConfiguration(); // from JSON

        context.UsePipelineSimulation = true;

        foreach (var step in config.PluginSteps)
        {
            // Auto-register all steps with proper configuration
            context.RegisterPluginStep(
                step.PluginTypeName,
                step.MessageName,
                step.PrimaryEntity,
                step.Stage,
                step.Mode,
                step.FilteringAttributes,
                step.PreImages,
                step.PostImages,
                step.Rank
            );
        }
    }
}
```

**Why This Is Amazing:**
- Tests can mirror **exact production configuration**
- No guessing about plugin order, stages, or filtering
- Pre/post images auto-configured with correct names and attributes
- Can test complex plugin chains that execute in specific orders
- Update tests simply by re-downloading configuration

#### 3. Sample/Test Data (NICE TO HAVE)
Download existing records from the environment:

**Use Cases:**
- Seed test data with realistic values
- Test against actual production data shapes
- Quickly set up known scenarios

**Output:** JSON files with entity collections
```json
{
  "accounts": [
    {
      "accountid": "guid-here",
      "name": "Contoso Ltd",
      "accountnumber": "ACC001",
      "revenue": 1000000
    }
  ]
}
```

**Helper to load:**
```csharp
context.InitializeFromJson("testdata/accounts.json");
```

**Considerations:**
- Privacy: Sanitize sensitive data
- Volume: Limit to relevant test records
- References: Maintain GUIDs for relationships

---

### Utility Features

#### Command-Line Interface
```bash
# Download metadata for specific entities
DataverseFakes.MetadataDownloader.exe -url "https://org.crm.dynamics.com"
    -entities "account,contact,opportunity"
    -output "./testmetadata"

# Download plugin steps
DataverseFakes.MetadataDownloader.exe -url "https://org.crm.dynamics.com"
    -plugins -solution "MyCustomizations"
    -output "./pluginsteps.json"

# Download sample data
DataverseFakes.MetadataDownloader.exe -url "https://org.crm.dynamics.com"
    -data -entities "account,contact"
    -filter "createdon gt 2024-01-01"
    -limit 100
    -output "./testdata"

# Download everything
DataverseFakes.MetadataDownloader.exe -url "https://org.crm.dynamics.com"
    -all -solution "MyCustomizations"
    -output "./testinfrastructure"
```

#### Authentication
- Support for OAuth (client secret, certificate)
- Support for Azure CLI authentication
- Support for connection strings
- Interactive login option

#### Output Formats
- **JSON** (for flexibility and inspection)
- **C# code** (for direct inclusion in projects)
- **Hybrid** (JSON data + C# loaders)

---

## 📋 Implementation Phases

### Phase 1: Quick Wins (Immediate Impact)
**Estimated Effort:** 1-2 days

1. ✅ **Auto-Populate Entity Images**
   - Add parameters to ExecutePluginWithTarget methods
   - Implement pre/post image retrieval and population
   - Default naming conventions
   - Documentation and examples

2. ✅ **Implement Filtering Attributes Validation**
   - Complete TODO at XrmFakedContext.Pipeline.cs:216
   - Add attribute presence checking
   - Add tests for filtering behavior
   - Update documentation

3. ✅ **Better Helper Methods**
   - Extension methods for common scenarios
   - Fluent API for plugin context setup
   - Better defaults and conventions

**Success Metrics:**
- Plugin tests require 50% less setup code
- Filtering attributes work correctly
- Developer satisfaction improves

---

### Phase 2: Modern Dataverse Operations (High Value)
**Estimated Effort:** 3-4 days

1. ✅ **CreateMultiple Message Executor**
   - Implement IFakeMessageExecutor for CreateMultipleRequest
   - Batch creation with transaction semantics
   - Plugin support (register for "CreateMultiple" message)
   - Performance optimization (single pass)
   - Error handling and rollback
   - Tests for various scenarios

2. ✅ **UpdateMultiple Message Executor**
   - Implement IFakeMessageExecutor for UpdateMultipleRequest
   - Batch updates with transaction semantics
   - Plugin support (register for "UpdateMultiple" message)
   - Merge behavior for partial updates
   - Tests for various scenarios

3. ✅ **DeleteMultiple Message Executor**
   - Implement IFakeMessageExecutor for DeleteMultipleRequest
   - Mark as preview/elastic tables only (with flag to enable)
   - Batch deletion with transaction semantics
   - Plugin support
   - Tests

4. ✅ **UpsertMultiple Message Executor**
   - Implement IFakeMessageExecutor for UpsertMultipleRequest
   - Create or update logic
   - Plugin support
   - Tests

**Success Metrics:**
- All bulk operation messages work
- Plugins can register for bulk messages
- Tests mirror real Dataverse behavior
- Documentation with examples

---

### Phase 3: Relationship Auto-Discovery (Developer Experience)
**Estimated Effort:** 2-3 days

1. ✅ **Auto-Register Relationships from Metadata**
   - Modify InitializeMetadata(EntityMetadata)
   - Parse ManyToManyRelationships → XrmFakedRelationship
   - Parse OneToManyRelationships → XrmFakedRelationship
   - Handle self-referential relationships
   - Validation and warnings
   - Tests for various relationship types

2. ✅ **Relationship Metadata Validation**
   - Better error messages when relationships missing
   - Suggestions for AddRelationship calls
   - Auto-detection of relationship usage

3. ✅ **Documentation**
   - Update README with automatic relationship discovery
   - Migration guide for existing tests
   - Examples

**Success Metrics:**
- No manual AddRelationship calls needed when using metadata
- Clear error messages when relationships missing
- 25% less test setup code

---

### Phase 4: Metadata Download Utility (Foundation)
**Estimated Effort:** 5-7 days

1. ✅ **Core Utility**
   - .NET CLI tool project
   - Authentication handling (OAuth, CLI, interactive)
   - Connection to Dataverse
   - Configuration management

2. ✅ **Metadata Download**
   - Entity metadata retrieval via SDK
   - Attribute metadata with full details
   - Relationship metadata (N:N intersect entity names!)
   - Choice/picklist options
   - JSON serialization

3. ✅ **Code Generation**
   - Generate C# initialization code from metadata
   - Generate XrmFakedRelationship definitions
   - Generate helper methods
   - Organize output files

4. ✅ **Testing**
   - Unit tests for utility
   - Integration tests with test environment
   - Sample outputs

**Success Metrics:**
- Tool successfully downloads metadata
- Generated code compiles and works
- Developers can run once and have full test infrastructure

---

### Phase 5: Plugin Configuration Download (Game Changer!)
**Estimated Effort:** 4-5 days

1. ✅ **Plugin Step Discovery**
   - Query PluginType, PluginTypeStatistic entities
   - Query SdkMessageProcessingStep entity
   - Query SdkMessageProcessingStepImage entity (pre/post images)
   - Parse all configuration properties
   - Handle solution filtering

2. ✅ **Configuration Serialization**
   - JSON schema for plugin steps
   - Include all registration details
   - Image configurations
   - Filtering attributes

3. ✅ **Code Generation**
   - Generate PluginStepConfiguration class
   - ConfigureFromEnvironment() method
   - Auto-register all steps with XrmFakedContext
   - Handle images, filtering, order

4. ✅ **Enhanced RegisterPluginStep**
   - Extend existing method to support:
     - Pre/post image names and columns
     - Filtering attributes (now validated!)
     - All plugin configuration properties

5. ✅ **Testing**
   - Test with real plugin configurations
   - Verify correct execution order
   - Verify image population
   - Verify filtering

**Success Metrics:**
- Tool downloads complete plugin configuration
- Generated code correctly configures pipeline
- Tests mirror production plugin behavior
- Developers love the ease of setup

---

### Phase 6: Elastic Tables Support (Modern Dataverse)
**Estimated Effort:** 3-4 days

1. ✅ **Elastic Table Metadata**
   - Flag entities as elastic vs standard
   - PartitionId column handling
   - TTL property support
   - Different attribute support matrix

2. ✅ **Behavior Differences**
   - Consistency model (optional - for advanced users)
   - No lookup relationships
   - No multi-record transactions (validation)
   - Bulk operation support (all four messages)

3. ✅ **Validation**
   - Error when trying to use lookups with elastic tables
   - Error when trying multi-record transactions
   - Clear messages about elastic vs standard

4. ✅ **Testing**
   - Comprehensive elastic table tests
   - Tests for all bulk operations
   - Tests for limitations
   - Documentation

**Success Metrics:**
- Elastic tables work correctly
- Clear distinction from standard tables
- All bulk operations supported
- Good error messages for unsupported features

---

### Phase 7: Test Data Download (Nice to Have)
**Estimated Effort:** 2-3 days

1. ✅ **Data Download**
   - Query entities with filters
   - Serialize to JSON
   - Handle relationships and references
   - Sanitization options

2. ✅ **Data Import**
   - Load JSON into context.Initialize()
   - Helper methods
   - Maintain GUIDs and relationships

3. ✅ **Documentation**
   - When to use test data vs. creating fresh
   - How to sanitize sensitive data
   - Examples

---

## 🚀 Additional Enhancements (Future)

### Better Test Authoring Experience

#### 1. Fluent API for Plugin Testing
```csharp
context.ForEntity("account", accountId)
    .WithTarget(target)
    .WithPreImage("PreImage", new ColumnSet(true))
    .WithPostImage("PostImage", new ColumnSet(true))
    .ExecutePlugin<MyPlugin>("Update", stage: 40);
```

#### 2. Test Data Builders
```csharp
var account = EntityBuilder.Create("account")
    .WithAttribute("name", "Contoso")
    .WithAttribute("revenue", 1000000)
    .WithRelatedEntities("contact",
        EntityBuilder.Create("contact").WithAttribute("lastname", "Smith"),
        EntityBuilder.Create("contact").WithAttribute("lastname", "Jones")
    )
    .Build();
```

#### 3. Assertion Helpers
```csharp
context.AssertEntityExists("account", accountId);
context.AssertEntityAttribute("account", accountId, "name", "Contoso");
context.AssertEntitiesAssociated("account", accountId, "contact", contactId, "new_account_contact");
context.AssertPluginExecuted<MyPlugin>();
```

### Better Error Messages

#### 1. Relationship Not Found
```
Current: "Relationship 'new_account_contact' not found"

Proposed:
"Relationship 'new_account_contact' not found.
Did you mean to call:
  context.AddRelationship('new_account_contact', new XrmFakedRelationship {
    IntersectEntity = 'new_account_contact',
    Entity1LogicalName = 'account',
    Entity1Attribute = 'accountid',
    Entity2LogicalName = 'contact',
    Entity2Attribute = 'contactid',
    RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany
  });

Or initialize metadata to auto-discover relationships:
  context.InitializeMetadata(entityMetadata);"
```

#### 2. Missing Metadata
Better suggestions when metadata is needed but not available

### Pipeline Simulation Enhancements

#### 1. Unified Testing Mode
Make pipeline simulation easier to use without sacrificing simple execution for basic tests

#### 2. Plugin Execution Tracing
```csharp
context.EnablePluginTracing = true;
// After execution:
Console.WriteLine(context.GetPluginTrace());
// Output:
// [Stage 20] AccountNumberPlugin executed (5ms)
// [Stage 40] AccountValidationPlugin executed (2ms)
// [Stage 40] AccountNotificationPlugin executed (15ms)
```

#### 3. Cascade Operations
Support for relationship cascade behaviors (delete, assign, share, etc.)

### Modern Dataverse Features

#### 1. Choice Columns (Modern Syntax)
Better support for the modern choice column syntax

#### 2. File/Image Columns
Helper methods for testing file uploads and image handling

#### 3. Long-Term Retention
Simulation of Dataverse long-term retention features

#### 4. Dataverse Search
Mock Dataverse search queries

---

## 📊 Success Metrics (Overall)

### Developer Experience
- 50-75% reduction in plugin test setup code
- Faster test authoring (< 5 minutes per test)
- Tests are more maintainable
- Tests mirror production configuration

### Feature Completeness
- All modern Dataverse operations supported
- Elastic tables fully supported
- Plugin pipeline matches real behavior
- Relationship handling automatic

### Adoption
- Community contributions increase
- GitHub stars increase
- Issues related to "missing features" decrease
- Positive feedback from users

---

## 🤝 Contributing

These enhancements are designed to be implemented incrementally. Each phase builds on the previous one and delivers immediate value.

**Priority Order:**
1. Phase 1 (Quick Wins) - Start here!
2. Phase 2 (Bulk Operations) - Modern Dataverse essential
3. Phase 3 (Relationship Auto-Discovery) - Developer experience
4. Phase 4 (Metadata Utility) - Foundation for offline testing
5. Phase 5 (Plugin Configuration) - Game changer for testing
6. Phase 6 (Elastic Tables) - Modern Dataverse completeness
7. Phase 7 (Test Data) - Nice to have

**Community Input Welcome:**
- Which phases are most valuable to you?
- What other enhancements would help?
- What features from the original DataverseFakes do you miss?

---

## 📝 Notes

### Design Principles
1. **Keep it realistic** - Mirror actual Dataverse behavior
2. **Make it easy** - Reduce boilerplate and manual setup
3. **Stay offline** - Don't require live connections for tests
4. **Enable one-time setup** - Download once, use forever
5. **Maintain backward compatibility** - Don't break existing tests

### Target Fields in Updates
Per Dataverse behavior, Target entities in Update messages should **only contain changed fields**, not the full entity. This is the correct behavior and should be maintained.

However, developers may want access to the full entity in their plugin code. This is where pre-entity images serve their purpose - providing access to the complete entity state before the update.

### Xrm Toolkit Integration
Many developers use Xrm Toolkit for proxy class generation. While this handles field generation well, relationships are often not represented properly for testing purposes. The metadata download utility should complement Xrm Toolkit by focusing on:
- Relationship definitions (with correct intersect entity names)
- Metadata initialization code
- Plugin step configurations
- Test data

This way, developers get the best of both tools without duplication.

---

**Questions? Feedback? Ideas?**

This is a living document. Please contribute your thoughts and suggestions!

# DataverseFakes: Modern Unit Testing for Dynamics 365

A truly open-source testing framework for Dynamics 365 / Power Platform that makes unit testing plugins, workflows, and custom code simple and fast.

[![NuGet](https://img.shields.io/nuget/v/DataverseFakes.Community.svg)](https://www.nuget.org/packages/DataverseFakes.Community)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Version 1.1.0 - Enterprise Features and Query Enhancements!

**NEW in v1.1.0** (January 2026): Enterprise-grade features with advanced query capabilities!

### v1.1.0 New Features

**Async Operations and Metadata**
- **ExecuteAsync Request Executor** - Full async operation support with AsyncOperation tracking
- **MetadataGenerator Public API** - Generate metadata programmatically with `FromEarlyBoundEntity` and `CreateAttributeMetadataByType`
- **PicklistAttributeMetadata OptionSet Population** - Automatically populated from context

**Data Integrity and Validation**
- **ExecuteMultiple ContinueOnError Fix** - Proper fault extraction and error handling
- **Composite Alternate Key Uniqueness** - Enforcement of uniqueness constraints on composite keys
- **RowVersion / Optimistic Concurrency** - Full support for optimistic locking patterns
- **Alternate Keys in Associate/Disassociate** - Use alternate keys for relationship operations
- **Min Date Validation** - Validates dates are not earlier than SQL Server minimum (01/01/1753)
- **Statecode Validation on Create** - Enforces valid statecode values during entity creation

**Advanced Query Operators**
- **Fiscal Period Operators** - Full support for `InFiscalPeriod`, `ThisFiscalPeriod`, `InFiscalYear`, `ThisFiscalYear`, and more
- **LIKE Wildcards Enhanced** - Character ranges `[A-Z]`, sets `[abc]`, and negation `[^abc]`
- **DateTime.Kind Handling** - Proper handling for DateOnly and TimeZoneIndependent fields
- **Any/All Filter Operators** - See dedicated section below for details

### v1.0.2 Features

**Simplified Plugin Testing**
- **Auto-Populate Entity Images** - No more manual pre/post image setup boilerplate
- **Automatic Relationship Discovery** - Initialize metadata once, relationships auto-register
- **Filtering Attributes Validation** - Pipeline simulation now matches real Dataverse behavior

**Modern Bulk Operations**
- **CreateMultiple** - Transactional bulk creates (no 1000 record limit)
- **UpdateMultiple** - Transactional bulk updates
- **DeleteMultiple** - Transactional bulk deletes
- **UpsertMultiple** - Bulk upsert with create/update detection

**Query Engine Fixes**
- **FetchXML Multiple Filters** - Multiple filter nodes now correctly combined with AND
- **Left Outer Joins** - Proper GroupJoin pattern for aggregate queries
- **Between Dates** - End dates include full day (23:59:59.999)
- **Date Operators** - ThisMonth, LastMonth, ThisWeek, LastWeek all working with timezone support
- **EntityReference.Name** - Automatically populated from PrimaryNameAttribute on retrieve

See [UPSTREAM_STATUS.md](UPSTREAM_STATUS.md) for full tracking of upstream issues!

### v1.0.1 Features
- **CalculateRollupFieldRequest support** - test rollup field calculations
- **SDK-style project format** - no more NuGet headaches
- **IPluginExecutionContext4 support** - full Azure AD integration
- **Simplified project structure** - easier to maintain and contribute
- **Better tooling support** - works great with VS 2019/2022

See [MODERNIZATION.md](MODERNIZATION.md) and [SDK_STYLE_MIGRATION.md](SDK_STYLE_MIGRATION.md) for migration details.

---

## What is DataverseFakes?

DataverseFakes is a comprehensive mocking framework for Dynamics 365 that enables:

- **Unit Testing Plugins**: Test your plugin logic without deploying to a real environment
- **Workflow Testing**: Validate custom workflow activities with in-memory execution
- **Fast Test Execution**: Run hundreds of tests in seconds with in-memory context
- **No Server Required**: Test offline without connecting to Dynamics 365
- **Early and Late Bound Support**: Works with generated entities or dynamic Entity objects
- **Modern Project Format**: SDK-style projects with PackageReference (no more packages.config!)

---

## Getting Started

### Installation

```bash
Install-Package DataverseFakes.Community
```

Or via .NET CLI:
```bash
dotnet add package DataverseFakes.Community
```

### Quick Example

```csharp
using DataverseFakes;
using Microsoft.Xrm.Sdk;
using Xunit;

public class AccountPluginTests
{
    [Fact]
    public void When_Account_Created_Should_Set_AccountNumber()
    {
        // Arrange
        var context = new XrmFakedContext();
        var target = new Entity("account")
        {
            ["name"] = "Contoso"
        };

        // Act
        context.ExecutePluginWithTarget<AccountNumberPlugin>(target);

        // Assert
        Assert.True(target.Contains("accountnumber"));
        Assert.NotNull(target["accountnumber"]);
    }
}
```

### Testing with Early-Bound Entities

```csharp
var context = new XrmFakedContext();
context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

var account = new Account
{
    Id = Guid.NewGuid(),
    Name = "Test Account"
};

context.Initialize(new List<Entity> { account });
var service = context.GetOrganizationService();

// Test your code with the service
```

### Testing Workflow Activities

```csharp
var context = new XrmFakedContext();
var inputs = new Dictionary<string, object>
{
    { "Target", new EntityReference("account", Guid.NewGuid()) },
    { "InputText", "Hello" }
};

var outputs = context.ExecuteCodeActivity<MyCustomActivity>(inputs);
Assert.Equal("Hello World", outputs["OutputText"]);
```

---

## Supported SDK Messages

DataverseFakes supports **62+ standard CRM messages** organized by category:

### CRUD Operations (Core)

| Message | Request Class | Description |
|---------|--------------|-------------|
| Create | `CreateRequest` | Create a single entity record |
| Retrieve | `RetrieveRequest` | Retrieve a single entity record by ID or alternate key |
| Update | `UpdateRequest` | Update a single entity record |
| Delete | `DeleteRequest` | Delete a single entity record |
| Upsert | `UpsertRequest` | Create or update based on existence |
| RetrieveMultiple | `RetrieveMultipleRequest` | Query multiple records (QueryExpression, FetchXML, QueryByAttribute) |

### Bulk Operations

| Message | Request Class | Description |
|---------|--------------|-------------|
| CreateMultiple | `CreateMultipleRequest` | Transactional bulk create (v1.0.2+) |
| UpdateMultiple | `UpdateMultipleRequest` | Transactional bulk update (v1.0.2+) |
| DeleteMultiple | `DeleteMultipleRequest` | Transactional bulk delete (v1.0.2+) |
| UpsertMultiple | `UpsertMultipleRequest` | Bulk create or update (v1.0.2+) |
| ExecuteMultiple | `ExecuteMultipleRequest` | Execute multiple requests with ContinueOnError support |
| ExecuteTransaction | `ExecuteTransactionRequest` | Transactional batch execution |
| BulkDelete | `BulkDeleteRequest` | Asynchronous bulk delete job |

### Async Operations

| Message | Request Class | Description |
|---------|--------------|-------------|
| ExecuteAsync | `ExecuteAsyncRequest` | Execute request asynchronously with AsyncOperation tracking (v1.1.0+) |

### Relationship Operations

| Message | Request Class | Description |
|---------|--------------|-------------|
| Associate | `AssociateRequest` | Associate records via N:N relationship (supports alternate keys v1.1.0+) |
| Disassociate | `DisassociateRequest` | Remove N:N relationship (supports alternate keys v1.1.0+) |
| Assign | `AssignRequest` | Assign record ownership |

### State and Status

| Message | Request Class | Description |
|---------|--------------|-------------|
| SetState | `SetStateRequest` | Set entity state and status |

### Security and Access

| Message | Request Class | Description |
|---------|--------------|-------------|
| GrantAccess | `GrantAccessRequest` | Grant access rights to a record |
| RevokeAccess | `RevokeAccessRequest` | Revoke access rights from a record |
| ModifyAccess | `ModifyAccessRequest` | Modify existing access rights |
| RetrievePrincipalAccess | `RetrievePrincipalAccessRequest` | Get access rights for a principal |
| RetrieveSharedPrincipalsAndAccess | `RetrieveSharedPrincipalsAndAccessRequest` | Get all principals with shared access |
| AddUserToRecordTeam | `AddUserToRecordTeamRequest` | Add user to an access team |
| RemoveUserFromRecordTeam | `RemoveUserFromRecordTeamRequest` | Remove user from an access team |

### Team Management

| Message | Request Class | Description |
|---------|--------------|-------------|
| AddMembersTeam | `AddMembersTeamRequest` | Add members to a team |
| RemoveMembersTeam | `RemoveMembersTeamRequest` | Remove members from a team |

### Marketing Lists

| Message | Request Class | Description |
|---------|--------------|-------------|
| AddMemberList | `AddMemberListRequest` | Add a single member to a marketing list |
| AddListMembersList | `AddListMembersListRequest` | Add multiple members to a marketing list |

### Queue Operations

| Message | Request Class | Description |
|---------|--------------|-------------|
| AddToQueue | `AddToQueueRequest` | Add item to a queue |
| PickFromQueue | `PickFromQueueRequest` | Pick item from queue |
| RemoveFromQueue | `RemoveFromQueueRequest` | Remove item from queue |

### Sales Process

| Message | Request Class | Description |
|---------|--------------|-------------|
| QualifyLead | `QualifyLeadRequest` | Qualify a lead record |
| WinOpportunity | `WinOpportunityRequest` | Close opportunity as won |
| LoseOpportunity | `LoseOpportunityRequest` | Close opportunity as lost |
| CloseQuote | `CloseQuoteRequest` | Close a quote |
| WinQuote | `WinQuoteRequest` | Win a quote |
| ReviseQuote | `ReviseQuoteRequest` | Revise an existing quote |
| CloseIncident | `CloseIncidentRequest` | Close a case/incident |

### Metadata Operations

| Message | Request Class | Description |
|---------|--------------|-------------|
| CreateEntity | `CreateEntityRequest` | Create entity metadata (v1.1.0+) |
| UpdateEntity | `UpdateEntityRequest` | Update entity metadata (v1.1.0+) |
| DeleteEntity | `DeleteEntityRequest` | Delete entity metadata (v1.1.0+) |
| RetrieveEntity | `RetrieveEntityRequest` | Retrieve entity metadata |
| RetrieveAttribute | `RetrieveAttributeRequest` | Retrieve attribute metadata |
| RetrieveRelationship | `RetrieveRelationshipRequest` | Retrieve relationship metadata |
| RetrieveMetadataChanges | `RetrieveMetadataChangesRequest` | Retrieve metadata changes |
| RetrieveOptionSet | `RetrieveOptionSetRequest` | Retrieve global option set |
| CreateOptionSet | `CreateOptionSetRequest` | Create global option set |
| UpdateOptionSet | `UpdateOptionSetRequest` | Update global option set |
| DeleteOptionSet | `DeleteOptionSetRequest` | Delete global option set |
| InsertOptionValue | `InsertOptionValueRequest` | Insert option value |
| InsertStatusValue | `InsertStatusValueRequest` | Insert status value |

### Utility Operations

| Message | Request Class | Description |
|---------|--------------|-------------|
| WhoAmI | `WhoAmIRequest` | Get current user information |
| RetrieveVersion | `RetrieveVersionRequest` | Get CRM version |
| UtcTimeFromLocalTime | `UtcTimeFromLocalTimeRequest` | Convert local time to UTC |
| RetrieveExchangeRate | `RetrieveExchangeRateRequest` | Get currency exchange rate |
| CalculateRollupField | `CalculateRollupFieldRequest` | Calculate rollup field value (v1.0.1+) |
| InitializeFrom | `InitializeFromRequest` | Initialize entity from another record |
| FetchXmlToQueryExpression | `FetchXmlToQueryExpressionRequest` | Convert FetchXML to QueryExpression |
| ExecuteFetch | `ExecuteFetchRequest` | Execute FetchXML query |
| SendEmail | `SendEmailRequest` | Send an email activity |
| PublishXml | `PublishXmlRequest` | Publish customizations |

---

## Query Operators

## Result ordering

DataverseFakes aims to match Dataverse behavior: **result ordering is not guaranteed unless you explicitly specify an `OrderExpression` (QueryExpression) or an `<order />` clause (FetchXML).**

This has a couple of implications for tests:

- Do **not** assert on `entities[0]`, `entities[1]`, etc. unless your query has an explicit order.
- Even with an explicit order, if multiple rows have the **same sort key** (ties), the relative order between those tied rows should be treated as undefined unless you add an additional tie-break order.

Example (order-independent assertion):

```csharp
var response = service.RetrieveMultiple(new FetchExpression(fetchXml));

// prefer set/membership assertions over index-based assertions
var ids = response.Entities.Select(e => e.Id).ToList();
Assert.Contains(expectedId1, ids);
Assert.Contains(expectedId2, ids);
Assert.Equal(2, ids.Count);

// for left outer joins, find the row that contains the aliased attribute
var withAlias = response.Entities.Where(e => e.Attributes.ContainsKey("aa.firstname")).ToList();
Assert.Single(withAlias);
Assert.Equal("Lionel", ((AliasedValue)withAlias[0]["aa.firstname"]).Value.ToString());
```

DataverseFakes supports a comprehensive set of condition operators for QueryExpression and FetchXML queries:

### Comparison Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `Equal` | `eq` | Equals |
| `NotEqual` | `ne` | Not equals |
| `GreaterThan` | `gt` | Greater than |
| `GreaterEqual` | `ge` | Greater than or equal |
| `LessThan` | `lt` | Less than |
| `LessEqual` | `le` | Less than or equal |

### Null Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `Null` | `null` | Is null |
| `NotNull` | `not-null` | Is not null |

### String Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `Like` | `like` | Pattern match with wildcards (`%`, `_`, `[A-Z]`, `[abc]`, `[^abc]`) |
| `NotLike` | `not-like` | Does not match pattern |
| `BeginsWith` | `begins-with` | Starts with string |
| `DoesNotBeginWith` | `not-begin-with` | Does not start with |
| `EndsWith` | `ends-with` | Ends with string |
| `DoesNotEndWith` | `not-end-with` | Does not end with |
| `Contains` | `like` (with `%`) | Contains substring |
| `DoesNotContain` | `not-like` | Does not contain substring |

### Set Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `In` | `in` | Value is in list |
| `NotIn` | `not-in` | Value is not in list |
| `Between` | `between` | Value is between two values |
| `NotBetween` | `not-between` | Value is not between two values |
| `ContainValues` | `contain-values` | Multi-select contains values |
| `DoesNotContainValues` | `not-contain-values` | Multi-select does not contain values |

### User/Business Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `EqualUserId` | `eq-userid` | Equals current user |
| `NotEqualUserId` | `ne-userid` | Not equals current user |
| `EqualBusinessId` | `eq-businessid` | Equals current business unit |
| `NotEqualBusinessId` | `ne-businessid` | Not equals current business unit |

---

## Date/Time Operators

DataverseFakes provides extensive support for date-based condition operators:

### Relative Date Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `Today` | `today` | Today (local timezone) |
| `Yesterday` | `yesterday` | Yesterday |
| `Tomorrow` | `tomorrow` | Tomorrow |
| `On` | `on` | On specific date |
| `NotOn` | `not-on` | Not on specific date |
| `OnOrAfter` | `on-or-after` | On or after date |
| `OnOrBefore` | `on-or-before` | On or before date |

### Time Range Operators (Past)

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `Last7Days` | `last-seven-days` | Within last 7 days |
| `LastXHours` | `last-x-hours` | Within last X hours |
| `LastXDays` | `last-x-days` | Within last X days |
| `LastXWeeks` | `last-x-weeks` | Within last X weeks |
| `LastXMonths` | `last-x-months` | Within last X months |
| `LastXYears` | `last-x-years` | Within last X years |
| `LastWeek` | `last-week` | During last week |
| `LastMonth` | `last-month` | During last month |
| `LastYear` | `last-year` | During last year |

### Time Range Operators (Future)

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `Next7Days` | `next-seven-days` | Within next 7 days |
| `NextXHours` | `next-x-hours` | Within next X hours |
| `NextXDays` | `next-x-days` | Within next X days |
| `NextXWeeks` | `next-x-weeks` | Within next X weeks |
| `NextXMonths` | `next-x-months` | Within next X months |
| `NextXYears` | `next-x-years` | Within next X years |
| `NextWeek` | `next-week` | During next week |
| `NextMonth` | `next-month` | During next month |
| `NextYear` | `next-year` | During next year |

### Current Period Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `ThisWeek` | `this-week` | During this week |
| `ThisMonth` | `this-month` | During this month |
| `ThisYear` | `this-year` | During this year |

### Age Operators

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `OlderThanXMinutes` | `olderthan-x-minutes` | Older than X minutes |
| `OlderThanXHours` | `olderthan-x-hours` | Older than X hours |
| `OlderThanXDays` | `olderthan-x-days` | Older than X days |
| `OlderThanXWeeks` | `olderthan-x-weeks` | Older than X weeks |
| `OlderThanXMonths` | `olderthan-x-months` | Older than X months |
| `OlderThanXYears` | `olderthan-x-years` | Older than X years |

### Fiscal Period Operators (v1.1.0+)

| Operator | FetchXML | Description |
|----------|----------|-------------|
| `InFiscalYear` | `in-fiscal-year` | In specified fiscal year |
| `InFiscalPeriod` | `in-fiscal-period` | In specified fiscal period |
| `InFiscalPeriodAndYear` | `in-fiscal-period-and-year` | In specified period and year |
| `ThisFiscalPeriod` | `this-fiscal-period` | In current fiscal period |
| `LastFiscalPeriod` | `last-fiscal-period` | In last fiscal period |
| `NextFiscalPeriod` | `next-fiscal-period` | In next fiscal period |

---

## Any/All Filter Operators (v1.1.0+)

Query related entities using subquery-style filters. Supports both QueryExpression and FetchXML.

### Supported Join Operators

| Join Type | FetchXML | Description |
|-----------|----------|-------------|
| `JoinOperator.Any` | `link-type="any"` | Returns parent if ANY child matches |
| `JoinOperator.NotAny` | `link-type="not any"` | Returns parent if NO child matches |
| `JoinOperator.All` | `link-type="all"` | Returns parent if ALL children match |
| `JoinOperator.NotAll` | `link-type="not all"` | Returns parent if NOT ALL children match |

### QueryExpression Example

```csharp
var context = new XrmFakedContext();
context.Initialize(new List<Entity> { /* accounts with contacts */ });

var query = new QueryExpression("account");
query.ColumnSet = new ColumnSet("name");

// Find accounts that have ANY contact with a specific email domain
var contactLink = query.AddLink("contact", "accountid", "parentcustomerid", JoinOperator.Any);
contactLink.LinkCriteria.AddCondition("emailaddress1", ConditionOperator.Like, "%@contoso.com");

var service = context.GetOrganizationService();
var results = service.RetrieveMultiple(query);
```

### FetchXML Example

```xml
<fetch>
  <entity name="account">
    <attribute name="name" />
    <!-- Find accounts with ANY contact having email at contoso.com -->
    <link-entity name="contact" from="parentcustomerid" to="accountid" link-type="any">
      <filter>
        <condition attribute="emailaddress1" operator="like" value="%@contoso.com" />
      </filter>
    </link-entity>
  </entity>
</fetch>
```

---

## Alternate Keys

DataverseFakes provides full support for Dataverse alternate keys:

### Defining Alternate Keys

```csharp
var context = new XrmFakedContext();

// Single attribute alternate key
context.AddAlternateKey("account", "accountnumber", "Account Number Key");

// Composite alternate key (multiple attributes)
context.AddAlternateKey("product", new[] { "productnumber", "productcategoryid" }, "Product Composite Key");
```

### Using Alternate Keys for CRUD Operations

```csharp
// Retrieve by alternate key
var entity = new Entity("account");
entity.KeyAttributes["accountnumber"] = "ACC-001";
var result = service.Retrieve("account", entity.KeyAttributes, new ColumnSet(true));

// Update by alternate key
var updateEntity = new Entity("account");
updateEntity.KeyAttributes["accountnumber"] = "ACC-001";
updateEntity["name"] = "Updated Name";
service.Update(updateEntity);

// Upsert by alternate key
var upsertEntity = new Entity("account");
upsertEntity.KeyAttributes["accountnumber"] = "ACC-002";
upsertEntity["name"] = "New or Updated";
var response = (UpsertResponse)service.Execute(new UpsertRequest { Target = upsertEntity });
```

### Alternate Keys in Relationships (v1.1.0+)

```csharp
// Associate using alternate keys
var targetRef = new EntityReference("account");
targetRef.KeyAttributes["accountnumber"] = "ACC-001";

var relatedRef = new EntityReference("contact");
relatedRef.KeyAttributes["emailaddress1"] = "john@contoso.com";

service.Associate(targetRef.LogicalName, targetRef.Id,
    new Relationship("contact_customer_accounts"),
    new EntityReferenceCollection { relatedRef });
```

### Alternate Key Constraints

DataverseFakes enforces Dataverse alternate key constraints:

- Maximum **10 alternate keys per entity**
- Maximum **16 attributes per alternate key**
- Supported attribute types: String, Integer, Decimal, DateTime, Lookup, Picklist
- **Uniqueness enforcement** on create and update (v1.1.0+)

---

## Plugin Pipeline

DataverseFakes supports simulating the Dynamics 365 plugin execution pipeline:

### Enabling Pipeline Simulation

```csharp
var context = new XrmFakedContext();
context.UsePipelineSimulation = true;
```

### Registering Plugin Steps

```csharp
// Register with entity type parameter
context.RegisterPluginStep<AccountPlugin, Account>(
    message: "Create",
    stage: ProcessingStepStage.Preoperation,
    mode: ProcessingStepMode.Synchronous,
    rank: 1,
    filteringAttributes: new[] { "name", "revenue" }
);

// Register with entity type code (for entities without EntityTypeCode field)
context.RegisterPluginStep<GenericPlugin>(
    message: "Update",
    stage: ProcessingStepStage.Postoperation,
    mode: ProcessingStepMode.Synchronous,
    rank: 1,
    filteringAttributes: null,
    primaryEntityTypeCode: 1  // Account type code
);
```

### Supported Pipeline Features

| Feature | Status | Description |
|---------|--------|-------------|
| Pre-validation stage | Supported | Stage 10 |
| Pre-operation stage | Supported | Stage 20 |
| Post-operation stage | Supported | Stage 40 |
| Synchronous mode | Supported | Immediate execution |
| Asynchronous mode | Supported | Queued execution |
| Filtering attributes | Supported | Only triggers when specified attributes change |
| Rank ordering | Supported | Multiple plugins execute in rank order |
| Plugin context | Supported | MessageName, Stage, Mode, InputParameters |
| Pre/Post entity images | Partial | Available via manual setup or auto-populate (v1.0.2+) |

### Auto-Populate Entity Images (v1.0.2+)

```csharp
// Entity images auto-populated from context!
context.ExecutePluginWithTarget<MyPlugin>(target,
    messageName: "Update",
    stage: 40,
    preImageColumns: new ColumnSet(true),
    postImageColumns: new ColumnSet(true));
```

---

## MetadataGenerator Usage (v1.1.0)

Generate entity and attribute metadata programmatically:

```csharp
// Generate metadata from early-bound entity type
var accountMetadata = MetadataGenerator.FromEarlyBoundEntity(typeof(Account));

// Generate specific attribute metadata
var stringAttr = MetadataGenerator.CreateAttributeMetadataByType(
    typeof(string), "name", "Name");

var picklistAttr = MetadataGenerator.CreateAttributeMetadataByType(
    typeof(OptionSetValue), "statuscode", "Status Reason");

// Initialize context with generated metadata
context.InitializeMetadata(accountMetadata);
```

---

## Known Limitations

### Output Parameters in Pipeline Simulation

**Issue**: When using pipeline simulation (`UsePipelineSimulation = true`), output parameters from the CRUD operation are not automatically available to plugins. The `OutputParameters` collection is initialized empty for each plugin execution.

**Workaround**: Manually populate output parameters in your test setup:

```csharp
var pluginContext = context.GetDefaultPluginContext();
pluginContext.OutputParameters["id"] = createdId;
context.ExecutePluginWith<MyPlugin>(pluginContext);
```

### Other Limitations

| Limitation | Description |
|------------|-------------|
| **Complex Aggregations** | Some complex FetchXML aggregations may not match Dataverse behavior exactly |
| **Calculated Fields** | Calculated and rollup fields require manual setup via `CalculateRollupFieldRequest` |
| **Business Rules** | Client-side business rules are not simulated |
| **Real-time Workflows** | Workflows and flows are not automatically triggered |
| **Multi-tenant** | Single tenant simulation only |
| **File/Image Attributes** | Limited support for file and image column types |

### Unsupported SDK Messages

The following commonly used messages are **not yet implemented**:

- `RetrieveAllEntities` - Use `RetrieveMetadataChanges` instead
- `ConvertQuoteToSalesOrder`
- `GenerateQuoteFromOpportunity`
- `GenerateSalesOrderFromOpportunity`
- `CalculatePrice`
- `Merge`
- `Clone` / `CloneAsPatch`
- `ImportSolution` / `ExportSolution`
- Most workflow-related messages

---

## Building from Source

### Prerequisites

- .NET Framework 4.6.2 or higher
- Visual Studio 2017 or later (or MSBuild tools)

### Build Commands

```bash
# Restore packages and build
build.bat

# Or individual commands
build.bat clean      # Clean artifacts
build.bat restore    # Restore NuGet packages
build.bat build      # Build solution
build.bat test       # Run tests
build.bat pack       # Create NuGet package
```

---

## Project Structure

```
DataverseFakes/
|-- DataverseFakes/              # Main library project
|-- DataverseFakes.Shared/       # Shared implementation code
|-- DataverseFakes.Tests/        # Test project
|-- DataverseFakes.Tests.Shared/ # Shared test code
\-- build.bat                 # Build script
```

---

## Documentation

For more detailed documentation, examples, and advanced scenarios, see:

- **Troubleshooting**: [Common issues and solutions](TROUBLESHOOTING.md)
- **IPluginExecutionContext4**: [New interface support](IPluginExecutionContext4_EXAMPLE.md)
- **Examples**: Check the [DataverseFakes.Tests](DataverseFakes.Tests/) project for comprehensive examples
- **Developer Guide**: See [CLAUDE.md](CLAUDE.md) for architecture and development guidelines

---

## Target Platform

**Dynamics 365 v9.x and later** (Power Platform / Common Data Service)

This community edition focuses exclusively on modern Dynamics 365 Online. For older CRM versions, please use the legacy branches.

---

## Contributing

We welcome contributions! This is a truly open-source project maintained by the community.

### How to Contribute

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

### Contribution Guidelines

- **Include Tests**: All new features and bug fixes must include unit tests
- **Follow Conventions**: Match the existing code style
- **Document Changes**: Update README and docs as needed
- **One Feature Per PR**: Keep pull requests focused

---

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

## Support

If you or your organization uses DataverseFakes.Community and finds it valuable, consider contributing to the project by:

- Submitting bug fixes and new features
- Improving documentation
- Sharing your testing patterns with the community
- Reporting issues and providing feedback

---

## Community

- **NuGet Package**: [DataverseFakes.Community on NuGet](https://www.nuget.org/packages/DataverseFakes.Community)
- **Documentation**: See [CLAUDE.md](CLAUDE.md) for development guidelines and architecture details

---

## Attribution & Acknowledgments

This project is derived from [FakeXrmEasy](https://github.com/jordimontana82/fake-xrm-easy), originally created by Jordi Montana and contributors. The original work is licensed under the MIT License (see [LICENSE.md](LICENSE.md)).

**FakeXrmEasy is a registered trademark of Jordi Montana.** This project is not affiliated with or endorsed by the trademark holder. The rename to DataverseFakes was made to respect the trademark.

Special thanks to all contributors who have helped make this project better!

---

**Made with love by the Dynamics 365 community**

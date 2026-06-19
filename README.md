# DataverseFakes

A modern, open-source unit testing framework for Dynamics 365 / Dataverse that mocks `IOrganizationService` with an in-memory context for fast plugin, workflow, and custom code testing.

[![NuGet](https://img.shields.io/nuget/v/DataverseFakes.Community.svg)](https://www.nuget.org/packages/DataverseFakes.Community)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- **Unit test plugins** without deploying to a real environment
- **Test workflow activities** with in-memory execution (.NET Framework)
- **Fast execution** - run 1000+ tests in seconds
- **No server required** - test offline, no Dynamics 365 connection needed
- **Early and late bound** - works with generated entities or dynamic `Entity` objects
- **Cross-platform** - multi-targets `net462`, `net48`, and `net10.0`
- **70+ SDK messages** supported out of the box (including metadata & OptionSet CRUD)
- **Pipeline simulation** - register plugin steps that auto-fire on Create/Update/Delete, with filtering attributes, stages, and rank ordering
- **Plugin execution tracing** - assert which plugins ran with `AssertPluginExecuted<T>()`
- **Relationship cascades** - metadata-driven Cascade / RemoveLink / Restrict on delete
- **Elastic tables** - mark tables elastic, with partitionid/TTL and limitation validation
- **Assertion helpers** - fluent `AssertExists`, `AssertAttributeValue`, `AssertAssociated`, and more
- **FetchXML and QueryExpression** - full query translation with aggregates, joins, and date operators

---

## Getting Started

### Installation

```bash
dotnet add package DataverseFakes.Community
```

Or via Package Manager:
```bash
Install-Package DataverseFakes.Community
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

### Early-Bound Entities

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

## Plugin Pipeline Simulation

```csharp
var context = new XrmFakedContext();
context.UsePipelineSimulation = true;

// Register plugin steps with filtering attributes
context.RegisterPluginStep<AccountPlugin, Account>(
    message: "Create",
    stage: ProcessingStepStage.Preoperation,
    mode: ProcessingStepMode.Synchronous,
    rank: 1,
    filteringAttributes: new[] { "name", "revenue" }
);

// Entity images auto-populated from context
context.ExecutePluginWithTarget<MyPlugin>(target,
    messageName: "Update",
    stage: 40,
    preImageColumns: new ColumnSet(true),
    postImageColumns: new ColumnSet(true));
```

With `UsePipelineSimulation = true`, registered steps **auto-fire** on regular CRUD calls
(`service.Create` / `Update` / `Delete`) at the correct stage, honoring filtering attributes and rank:

```csharp
context.UsePipelineSimulation = true;
context.RegisterPluginStep<AccountPlugin>("Create", ProcessingStepStage.Postoperation);

service.Create(new Entity("account") { ["name"] = "Contoso" }); // AccountPlugin runs automatically
```

---

## Plugin Execution Tracing & Assertions

Every plugin execution (explicit or pipeline-fired) is recorded, so you can assert what ran:

```csharp
context.AssertPluginExecuted<AccountPlugin>();              // ran at least once
context.AssertPluginExecuted<AccountPlugin>("Create");      // ran for the Create message
context.AssertPluginExecutedTimes<AccountPlugin>(1);        // ran exactly once
context.AssertPluginNotExecuted<ValidationPlugin>();        // never ran

foreach (var rec in context.PluginExecutions)              // structured execution log
    Console.WriteLine($"{rec.Stage} {rec.MessageName} {rec.PluginType.Name}");

Console.WriteLine(context.GetPluginStepTrace());           // human-readable dump
context.ClearPluginExecutions();
```

---

## Assertion Helpers

Fluent, test-runner-neutral assertions over the in-memory data:

```csharp
context.AssertExists("account", accountId);
context.AssertDoesNotExist("account", accountId);
context.AssertAttributeValue("account", accountId, "name", "Contoso");
context.AssertHasAttribute("account", accountId, "name");
context.AssertAttributeNull("account", accountId, "fax");
context.AssertAssociated("account", accountId, "contact", contactId, "account_contacts");
context.AssertRecordCount("account", 3);
```

---

## Relationship Cascades

Simulate Dataverse cascade behavior on delete. Register from metadata, or with the convenience helper:

```csharp
context.AddCascadeDeleteRelationship(
    schemaName: "account_contacts",
    referencedEntity: "account",      // parent
    referencingEntity: "contact",     // child
    referencingAttribute: "parentcustomerid",
    deleteBehavior: CascadeType.Cascade);   // or RemoveLink / Restrict

service.Delete("account", accountId);  // child contacts are cascade-deleted
```

`CascadeConfiguration` on initialized `EntityMetadata` is auto-applied. Restrict throws and leaves data
unchanged when children exist; RemoveLink nulls the child lookup. (Share/Reparent are not yet simulated.)

Self-referential hierarchies reject circular references on Create/Update, matching Dataverse:

```csharp
context.AddSelfReferentialHierarchy("account", "parentaccountid");
// service.Update setting A.parentaccountid = B when B.parentaccountid = A throws:
//   "Creating this parental association would create a loop in account hierarchy."
```

Hierarchies are also auto-detected from self-referential 1:N `EntityMetadata`. The guard catches both
direct self-references and transitive loops, and is inert unless a hierarchy is registered.

---

## Elastic Tables

```csharp
context.MarkAsElasticTable("contoso_telemetry");
context.IsElasticTable("contoso_telemetry"); // true (also auto-detected from EntityMetadata.TableType)

// partitionid + ttlinseconds round-trip; bulk messages work; opt-in TTL purge:
var removed = context.RemoveExpiredElasticRecords("contoso_telemetry", DateTime.UtcNow);
```

Elastic limitations are enforced: multi-record transactions (`ExecuteTransaction`) and
`Associate`/`Disassociate` against an elastic table throw, matching Dataverse.

---

## Supported SDK Messages

DataverseFakes supports **70+ standard CRM messages**:

| Category | Messages |
|----------|----------|
| **CRUD** | Create, Retrieve, Update, Delete, Upsert, RetrieveMultiple |
| **Bulk** | CreateMultiple, UpdateMultiple, DeleteMultiple, UpsertMultiple, ExecuteMultiple, ExecuteTransaction, BulkDelete |
| **Async** | ExecuteAsync (with AsyncOperation tracking) |
| **Relationships** | Associate, Disassociate, Assign (with alternate key support) |
| **Security** | GrantAccess, RevokeAccess, ModifyAccess, RetrievePrincipalAccess, AddUserToRecordTeam |
| **Teams** | AddMembersTeam, RemoveMembersTeam |
| **Queues** | AddToQueue, PickFromQueue, RemoveFromQueue |
| **Sales** | QualifyLead, WinOpportunity, LoseOpportunity, CloseQuote, WinQuote, ReviseQuote, CloseIncident |
| **Entity Metadata** | CreateEntity, UpdateEntity, DeleteEntity, RetrieveEntity, RetrieveAllEntities, RetrieveAttribute, CreateAttribute, UpdateAttribute, DeleteAttribute, RetrieveRelationship, RetrieveMetadataChanges |
| **OptionSet Metadata** | CreateOptionSet, UpdateOptionSet, DeleteOptionSet, RetrieveOptionSet, RetrieveAllOptionSets, InsertOptionValue, UpdateOptionValue, InsertStatusValue, UpdateStateValue |
| **Utility** | WhoAmI, RetrieveVersion, CalculateRollupField, InitializeFrom, FetchXmlToQueryExpression, SendEmail, PublishXml |

---

## Query Support

### Result Ordering

DataverseFakes matches Dataverse behavior: **result ordering is not guaranteed unless you explicitly specify an `OrderExpression` or `<order />` clause.** Use set/membership assertions instead of index-based assertions:

```csharp
var results = service.RetrieveMultiple(query);
var ids = results.Entities.Select(e => e.Id).ToList();
Assert.Contains(expectedId, ids);
```

### Condition Operators

| Category | Operators |
|----------|-----------|
| **Comparison** | Equal, NotEqual, GreaterThan, GreaterEqual, LessThan, LessEqual |
| **Null** | Null, NotNull |
| **String** | Like (with `%`, `_`, `[A-Z]`, `[abc]`, `[^abc]`), NotLike, BeginsWith, EndsWith, Contains |
| **Set** | In, NotIn, Between, NotBetween, ContainValues, DoesNotContainValues |
| **User/Business** | EqualUserId, NotEqualUserId, EqualBusinessId, NotEqualBusinessId |
| **Date** | Today, Yesterday, Tomorrow, Last7Days, LastXDays, NextXDays, ThisWeek, ThisMonth, ThisYear, and many more |
| **Fiscal** | InFiscalYear, InFiscalPeriod, ThisFiscalPeriod, LastFiscalPeriod, NextFiscalPeriod |
| **Any/All** | JoinOperator.Any, NotAny, All, NotAll (subquery-style filters) |

### FetchXML Aggregates

```csharp
var fetchXml = @"<fetch aggregate='true'>
  <entity name='contact'>
    <attribute name='contactid' alias='count' aggregate='count' />
    <attribute name='lastname' alias='group' groupby='true' />
  </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

---

## Alternate Keys

```csharp
// Define alternate keys
context.AddAlternateKey("account", "accountnumber", "Account Number Key");
context.AddAlternateKey("product", new[] { "productnumber", "productcategoryid" }, "Composite Key");

// Retrieve by alternate key
var entity = new Entity("account");
entity.KeyAttributes["accountnumber"] = "ACC-001";
var result = service.Retrieve("account", entity.KeyAttributes, new ColumnSet(true));

// Upsert by alternate key
var upsert = new Entity("account");
upsert.KeyAttributes["accountnumber"] = "ACC-002";
upsert["name"] = "New or Updated";
service.Execute(new UpsertRequest { Target = upsert });
```

---

## Known Limitations

| Limitation | Description |
|------------|-------------|
| Complex Aggregations | Some complex FetchXML aggregations may not match Dataverse behavior exactly |
| Calculated Fields | Require manual setup via `CalculateRollupFieldRequest` |
| Business Rules | Client-side business rules are not simulated |
| Real-time Workflows | Workflows and flows are not automatically triggered |
| File/Image Attributes | Limited support for file and image column types |
| OOB cascade rules | Out-of-box cascade behaviors are not built in — register them with `AddCascadeDeleteRelationship(...)` or supply `CascadeConfiguration` via `InitializeMetadata` |
| Labels without metadata | `FormattedValues` use configured labels only when attribute metadata is loaded; without it, option sets fall back to the numeric value and booleans to "Yes"/"No" (Dataverse's default two-option labels, which differ from customized labels like "Allow") |

---

## Building from Source

```bash
build.bat              # Restore, build, and test
build.bat test         # Run tests only
build.bat pack         # Create NuGet package (+ symbol package)
```

**Target Platforms**: Dynamics 365 v9.x and later. The library multi-targets **`net462`**, **`net48`**,
and **`net10.0`**. The .NET Framework legs build against the `Microsoft.CrmSdk.*` assemblies; the `net10.0`
leg builds against `Microsoft.PowerPlatform.Dataverse.Client` (same `Microsoft.Xrm.Sdk` identity as modern
consumers). Workflow/CodeActivity simulation (`ExecuteCodeActivity`) is .NET Framework only; `XrmRealContext`
(live-connect) works on all legs (via `ServiceClient` on `net10.0`).

The package ships **Source Link** and a **symbol package (`.snupkg`)** so you can step into the framework
while debugging.

---

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for your changes
4. Open a Pull Request

All new features and bug fixes must include unit tests.

---

## License

MIT License - see [LICENSE.md](LICENSE.md) for details.

## Attribution

This project is derived from [FakeXrmEasy](https://github.com/jordimontana82/fake-xrm-easy), originally created by Jordi Montana and contributors.

**FakeXrmEasy is a registered trademark of Jordi Montana.** This project is not affiliated with or endorsed by the trademark holder.

See [CHANGELOG.md](CHANGELOG.md) for version history.

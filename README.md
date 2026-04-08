# DataverseFakes

A modern, open-source unit testing framework for Dynamics 365 / Dataverse that mocks `IOrganizationService` with an in-memory context for fast plugin, workflow, and custom code testing.

[![NuGet](https://img.shields.io/nuget/v/DataverseFakes.Community.svg)](https://www.nuget.org/packages/DataverseFakes.Community)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- **Unit test plugins** without deploying to a real environment
- **Test workflow activities** with in-memory execution
- **Fast execution** - run 1000+ tests in seconds
- **No server required** - test offline, no Dynamics 365 connection needed
- **Early and late bound** - works with generated entities or dynamic `Entity` objects
- **62+ SDK messages** supported out of the box
- **Pipeline simulation** - register plugin steps with filtering attributes, stages, and rank ordering
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

---

## Supported SDK Messages

DataverseFakes supports **62+ standard CRM messages**:

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
| **Metadata** | CreateEntity, UpdateEntity, DeleteEntity, RetrieveEntity, RetrieveAttribute, RetrieveRelationship, RetrieveMetadataChanges, CRUD OptionSets |
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

---

## Building from Source

```bash
build.bat              # Restore, build, and test
build.bat test         # Run tests only
build.bat pack         # Create NuGet package
```

**Target Platform**: Dynamics 365 v9.x and later (.NET Framework 4.6.2)

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

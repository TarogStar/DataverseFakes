# Changelog

All notable changes to this project will be documented in this file.

## [1.3.0] - 2026-06-19

### Added
- **Metadata / OptionSet CRUD tail** - `UpdateOptionValue`, `UpdateStateValue`, `RetrieveAllOptionSets`,
  `RetrieveAllEntities`, `CreateAttribute`, `UpdateAttribute`, `DeleteAttribute` message executors.
- **FormattedValues from global OptionSets** (#218) - `Retrieve`/`RetrieveMultiple` resolve OptionSet
  labels from `OptionSetValuesMetadata` when entity-attribute metadata is absent.
- **Elastic tables** - `MarkAsElasticTable` / `IsElasticTable` (+ auto-detect from `EntityMetadata.TableType`),
  `partitionid`/`ttlinseconds` round-trip, opt-in `RemoveExpiredElasticRecords`, and validation that rejects
  multi-record transactions and Associate/Disassociate against elastic tables.
- **Plugin execution tracing** - `PluginExecutions`, `GetPluginStepTrace()`, `ClearPluginExecutions()`.
- **Plugin assertion helpers** - `AssertPluginExecuted<T>()`, `AssertPluginExecuted<T>(message)`,
  `AssertPluginExecutedTimes<T>(n)`, `AssertPluginNotExecuted<T>()`.
- **Relationship cascade behaviors** - metadata-driven Delete cascade (Cascade / RemoveLink / Restrict)
  for 1:N relationships, `AddCascadeDeleteRelationship` convenience helper, and Assign cascade. Self-
  referential/cyclic cascades terminate safely. (Share/Unshare/Reparent/Merge not yet simulated.)
- **Hierarchy cycle guard** - Create/Update reject circular parent/child references on self-referential
  (hierarchical) relationships, matching Dataverse ("...would create a loop in {entity} hierarchy.").
  Detected from self-referential 1:N `EntityMetadata` or via `AddSelfReferentialHierarchy(entity, attr)`;
  validated against a live Dataverse environment.
- **Full net10 test leg** - the shared test suite now runs against the `net10.0` build in CI (previously
  only 3 net10 smoke tests).

### Changed
- **Packaging**: emit a **symbol package** (`.snupkg`) with **Source Link** for step-into debugging;
  enabled Deterministic + ContinuousIntegrationBuild. Introduced **Central Package Management**
  (`Directory.Packages.props`). Unified the NuGet pack path onto `dotnet pack` and removed the redundant
  hand-maintained `DataverseFakes.nuspec`.
- Confirmed registered plugin steps **auto-fire** on Create/Update/Delete when
  `UsePipelineSimulation = true` (upstream #183), now verified end-to-end and assertable.

### Fixed
- **NU5048**: replaced the deprecated nuspec `<iconUrl>` with a packaged `<icon>`.
- Removed the unused `Newtonsoft.Json` dependency from the shipped library.

## [1.2.0] - 2026-06

### Added
- **Cross-platform targets** - the library now multi-targets `net462`, `net48`, and `net10.0`. The
  `net10.0` leg builds against `Microsoft.PowerPlatform.Dataverse.Client`.
- **XrmRealContext on net10** - ported to `ServiceClient` so live-connect compiles on all legs.

### Changed
- Upgraded FakeItEasy to 8.3.0.
- `net10.0` leg excludes .NET Framework-only workflow/CodeActivity simulation.

## [1.1.5] - 2026-04-07

### Changed
- Renamed project from FakeXrmEasy.Community to DataverseFakes.Community to respect the FakeXrmEasy trademark
- Namespace changed from `FakeXrmEasy` to `DataverseFakes`
- NuGet package ID changed from `FakeXrmEasy.Community` to `DataverseFakes.Community`

### Fixed
- FetchXml aggregate groupby now works correctly with OptionSet attributes
- Alias and name validation in aggregate FetchXml now checks actual values instead of string literals
- Test isolation issue with CounterPlugin static state across parallel test runs

## [1.1.4] - 2026-04-01

### Fixed
- Match Dataverse image column behavior in Retrieve vs RetrieveMultiple
- Resolve entityimage attribute not found in early-bound proxy validation

## [1.1.0] - 2026-01

### Added
- **ExecuteAsync Request Executor** - Full async operation support with AsyncOperation tracking
- **MetadataGenerator Public API** - `FromEarlyBoundEntity` and `CreateAttributeMetadataByType`
- **PicklistAttributeMetadata OptionSet Population** - Automatically populated from context
- **Composite Alternate Key Uniqueness** - Enforcement of uniqueness constraints
- **RowVersion / Optimistic Concurrency** - Full support for optimistic locking patterns
- **Alternate Keys in Associate/Disassociate** - Use alternate keys for relationship operations
- **Fiscal Period Operators** - `InFiscalPeriod`, `ThisFiscalPeriod`, `InFiscalYear`, `ThisFiscalYear`, and more
- **LIKE Wildcards Enhanced** - Character ranges `[A-Z]`, sets `[abc]`, negation `[^abc]`
- **CreateEntityRequest / UpdateEntityRequest / DeleteEntityRequest** - Entity metadata CRUD
- **Any/All Filter Operators** - Subquery-style filters via `JoinOperator.Any`, `NotAny`, `All`, `NotAll`

### Fixed
- ExecuteMultiple ContinueOnError and fault extraction
- Min Date Validation (01/01/1753)
- DateTime.Kind handling for DateOnly/TimeZoneIndependent fields
- Statecode Validation on Create

## [1.0.2] - 2025

### Added
- **Auto-Populate Entity Images** - No more manual pre/post image setup boilerplate
- **Automatic Relationship Discovery** - Initialize metadata once, relationships auto-register
- **Filtering Attributes Validation** - Pipeline simulation matches real Dataverse behavior
- **CreateMultiple / UpdateMultiple / DeleteMultiple / UpsertMultiple** - Transactional bulk operations

### Fixed
- FetchXML Multiple Filters - Multiple filter nodes now correctly combined with AND
- Left Outer Joins - Proper GroupJoin pattern for aggregate queries
- Between Dates - End dates include full day (23:59:59.999)
- Date Operators - ThisMonth, LastMonth, ThisWeek, LastWeek with timezone support
- EntityReference.Name - Automatically populated from PrimaryNameAttribute on retrieve

## [1.0.1] - 2025

### Added
- CalculateRollupFieldRequest support
- IPluginExecutionContext4 support

## [1.0.0] - 2025

### Added
- Initial release as a community-driven fork
- Focus exclusively on Dynamics 365 v9.x and later (Power Platform / Dataverse)
- Removed support for legacy CRM versions (2011-2016)

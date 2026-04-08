# Changelog

All notable changes to this project will be documented in this file.

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

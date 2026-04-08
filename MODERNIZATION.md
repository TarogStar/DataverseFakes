# DataverseFakes Modernization Summary

This document describes the modernization changes made to transform DataverseFakes v1.x into a modern, truly open-source testing framework for Dynamics 365.

## Overview

The project has been streamlined to focus exclusively on modern Dynamics 365 (v9.x and later / Power Platform), removing legacy version support and obsolete warnings.

## Major Changes

### 1. Removed Legacy Version Support

**Deleted Projects:**
- DataverseFakes (CRM 2011)
- DataverseFakes.2013
- DataverseFakes.2015
- DataverseFakes.2016
- DataverseFakes.365
- All corresponding test projects

**Kept Projects:**
- DataverseFakes (renamed from DataverseFakes.9)
- DataverseFakes.Tests (renamed from DataverseFakes.Tests.9)
- DataverseFakes.Shared
- DataverseFakes.Tests.Shared

### 2. Removed Obsolete Warnings

**Changed Files:**
- `DataverseFakes.Shared\XrmFakedContext.cs`
  - Removed `[Obsolete]` attribute from constructor
  - Removed `[Obsolete]` attribute from Initialize method
  - Removed all references to v2.x/v3.x migration

### 3. Simplified Build System

**Deleted:**
- Complex FAKE build system (build.fsx)
- Old batch scripts (00_boot.bat, 01_boot_and_build.bat, 02_build.bat)
- Build tool dependencies (tools/ directory)
- Old NuGet specifications for each CRM version

**Created:**
- Simple `build.bat` script with clear commands:
  - `build.bat clean` - Clean artifacts
  - `build.bat restore` - Restore packages
  - `build.bat build` - Build solution
  - `build.bat test` - Run tests
  - `build.bat pack` - Create NuGet package
  - `build.bat` (no args) - Run restore, build, test

### 4. New Solution File

**Created:**
- `DataverseFakes.sln` - Simplified solution with only 4 projects:
  1. DataverseFakes (main library)
  2. DataverseFakes.Tests
  3. DataverseFakes.Shared
  4. DataverseFakes.Tests.Shared

**Removed:**
- Old complex solution with 13+ projects

### 5. Updated Documentation

**README.md:**
- Complete rewrite with modern, welcoming tone
- Clear "Getting Started" section with examples
- Emphasis on community-driven, truly open-source nature
- Removed deprecation warnings and migration links
- Added comprehensive feature list
- Simple build instructions

**CLAUDE.md:**
- Updated to reflect simplified structure
- Removed multi-version complexity
- Clear focus on D365 v9.x+
- Updated build commands
- Added development guidelines

**Created:**
- `DataverseFakes.nuspec` - Single NuGet package specification
- `MODERNIZATION.md` - This document

### 6. Cleaned Up Repository

**Deleted:**
- Install-scripts directory (all version-specific install scripts)
- Old NuGet specs (6 separate .nuspec files)
- Build artifacts and logs
- FAKE tool dependencies

## Target Platform

The modernized version **only supports**:
- Dynamics 365 v9.x and later
- Power Platform
- Common Data Service
- Dataverse

**Dependencies:**
- .NET Framework 4.6.2
- Microsoft.CrmSdk.CoreAssemblies 9.0.2.4
- Microsoft.CrmSdk.Workflow 9.0.2.4
- Microsoft.CrmSdk.XrmTooling.CoreAssembly 9.0.2.4
- FakeItEasy 6.0.0

## Version Number

The community edition is designated as **v1.0.0** to indicate:
- First release of DataverseFakes.Community
- Major breaking change from original (removed legacy version support)
- Clean slate for truly open-source development
- Focus on modern Dynamics 365

## Project Vision

This modernization transforms DataverseFakes into:
- **Truly Open Source**: MIT licensed, community-driven
- **Modern**: Focused on current Dynamics 365 / Power Platform
- **Simple**: Easy to build, test, and contribute
- **Well-Documented**: Clear examples and guidelines
- **Community-Focused**: Welcoming to contributors

## Migration Path

For users still on legacy CRM versions (2011-2016):
- Use the original DataverseFakes v1.x branch (if maintained)
- Consider upgrading to Dynamics 365 v9.x+
- The v1.x codebase remains available in git history

## Benefits of Modernization

1. **Simplified Maintenance**: One codebase, one target platform
2. **Faster Development**: No multi-version testing required
3. **Better Community**: Lower barrier to contribution
4. **Clearer Focus**: Modern Dynamics 365 features
5. **Easier Onboarding**: Simple build process

## New Features Added Post-Modernization

### SDK-Style Project Migration (v1.0.0) 🎉

**MAJOR IMPROVEMENT**: Converted from old-style .NET Framework projects to modern SDK-style projects.

**What Changed:**
- Converted both DataverseFakes.csproj and DataverseFakes.Tests.csproj to SDK-style
- Replaced packages.config with PackageReference
- Removed app.config (binding redirects now auto-generated)
- Project files reduced from 180+ lines to ~95 lines
- Embedded NuGet package metadata directly in .csproj

**Benefits:**
- ✅ **No more NuGet GAC reference issues** (System.Numerics.Vectors error is GONE!)
- ✅ **Automatic binding redirects** - no manual configuration needed
- ✅ **Easy package updates** - Visual Studio package manager works perfectly
- ✅ **Cleaner project files** - much more readable and maintainable
- ✅ **Automatic file discovery** - no need to manually add files to project
- ✅ **Modern tooling support** - better VS 2019/2022 integration

**Migration Details:**
- Old project files backed up as `.csproj.old`
- Old config files backed up as `.old` extensions
- All functionality preserved - 100% backward compatible
- `dotnet restore` now works flawlessly

See [SDK_STYLE_MIGRATION.md](SDK_STYLE_MIGRATION.md) for complete details.

### IPluginExecutionContext4 Support (v1.0.0)

Added full support for `IPluginExecutionContext4` and all predecessor interfaces:

**Changes:**
- `XrmFakedPluginExecutionContext` now implements `IPluginExecutionContext4`
- Added support for Azure AD Object IDs (IPluginExecutionContext2)
- Added support for ParentContextProperties (IPluginExecutionContext3)
- Added support for IsTransactionIntegrationMessage (IPluginExecutionContext4)
- Updated service provider to return correct interface when requesting any version
- Added comprehensive tests in `PluginExecutionContext4Tests.cs`

**New Properties Available:**
- `InitiatingUserAzureActiveDirectoryObjectId` (string)
- `UserAzureActiveDirectoryObjectId` (string)
- `ParentContextProperties` (DataCollection<string, string>)
- `IsTransactionIntegrationMessage` (bool)

See [IPluginExecutionContext4_EXAMPLE.md](IPluginExecutionContext4_EXAMPLE.md) for usage examples.

## Next Steps

Suggested future improvements:
1. Migrate to .NET Standard / .NET 6+ for cross-platform support
2. Add GitHub Actions CI/CD pipeline
3. Publish to NuGet.org
4. Create comprehensive documentation site
5. Add more example tests and scenarios
6. Continue adding modern SDK features as they are released
7. Consider multi-targeting (net462, net6.0, net8.0)

### CalculateRollupFieldRequest Support (v1.0.1)

Added support for testing rollup field calculations:

**Changes:**
- New `CalculateRollupFieldRequestExecutor` in `DataverseFakes.Shared\FakeMessageExecutors\`
- Comprehensive test coverage in `FakeContextTestCalculateRollupField.cs`
- Support for pre-populating rollup values in tests
- Detailed documentation on testing approaches

**Testing Approach:**
The executor provides a pragmatic solution for unit testing by allowing tests to pre-populate rollup field values, enabling validation of plugin/workflow behavior that depends on rollup calculations without implementing full aggregation logic.

See the executor documentation for details on extending with actual calculation logic if needed.

---

**Date**: October 2025
**Version**: 1.0.1 (DataverseFakes.Community)

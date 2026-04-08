using Microsoft.Extensions.Configuration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using Xunit;
using Xunit.Abstractions;

namespace FakeXrmEasy.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests to investigate DateTime behavior in real Dataverse.
    ///
    /// PURPOSE: Determine what happens when DateTime values with different Kinds
    /// (Local, UTC, Unspecified) are sent to Dataverse via the SDK.
    ///
    /// USAGE:
    /// 1. Set your connection string using dotnet user-secrets:
    ///    dotnet user-secrets set "Dataverse:ConnectionString" "your-connection-string" --project FakeXrmEasy.Tests
    /// 2. Run these specific tests when needed:
    ///    dotnet test --filter "Category=RequiresDataverse"
    ///
    /// CONNECTION STRING FORMAT:
    /// AuthType=OAuth;Url=https://prenticeworx-dev.crm.dynamics.com;AppId=your-app-id;RedirectUri=app://your-redirect;LoginPrompt=Auto
    ///
    /// Or for client credentials:
    /// AuthType=ClientSecret;Url=https://prenticeworx-dev.crm.dynamics.com;ClientId=your-client-id;ClientSecret=your-secret
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresDataverse")]
    public class DataverseDateTimeInvestigation
    {
        private readonly ITestOutputHelper _output;

        public DataverseDateTimeInvestigation(ITestOutputHelper output)
        {
            _output = output;
        }

        private IOrganizationService GetRealService()
        {
            // Use the explicit UserSecretsId from the .csproj file
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets("52306faf-85ce-4d22-a7b8-3e872eeee272")
                .Build();
                
            var connectionString = configuration["Dataverse:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "No Dataverse connection string found. " +
                    "Set using: dotnet user-secrets set \"Dataverse:ConnectionString\" \"your-connection-string\" --project FakeXrmEasy.Tests");
            }

            var client = new CrmServiceClient(connectionString);
            if (!client.IsReady)
            {
                throw new InvalidOperationException($"Failed to connect: {client.LastCrmError}");
            }

            _output.WriteLine($"Connected to: {client.ConnectedOrgUniqueName}");
            return client;
        }

        /// <summary>
        /// Test #1: Send DateTime with DateTimeKind.Local to Dataverse
        /// Question: Does Dataverse convert it to UTC, or store the raw value as UTC?
        /// </summary>
        [Fact(Skip = "Integration test - set user secret and run with --filter Category=RequiresDataverse")]
        public void When_DateTime_Kind_Is_Local_What_Gets_Stored()
        {
            var service = GetRealService();

            // Create a phonecall with actualend set to LOCAL time
            var localTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Local);

            _output.WriteLine($"=== TEST: DateTimeKind.Local ===");
            _output.WriteLine($"Input DateTime: {localTime}");
            _output.WriteLine($"Input Kind: {localTime.Kind}");
            _output.WriteLine($"Input as UTC would be: {localTime.ToUniversalTime()}");
            _output.WriteLine($"Local timezone offset: {TimeZoneInfo.Local.GetUtcOffset(localTime)}");

            var phonecall = new Entity("phonecall")
            {
                ["subject"] = $"DateTime Test - Local Kind - {DateTime.UtcNow:yyyyMMddHHmmss}",
                ["actualend"] = localTime,
                ["description"] = $"Test input: {localTime} Kind={localTime.Kind}"
            };

            var id = service.Create(phonecall);
            _output.WriteLine($"Created phonecall: {id}");

            // Retrieve it back
            var retrieved = service.Retrieve("phonecall", id, new ColumnSet("actualend", "subject"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("actualend");

            _output.WriteLine($"");
            _output.WriteLine($"Retrieved DateTime: {storedValue}");
            _output.WriteLine($"Retrieved Kind: {storedValue.Kind}");
            _output.WriteLine($"");
            _output.WriteLine($"ANALYSIS:");
            _output.WriteLine($"  Input local time: {localTime:yyyy-MM-dd HH:mm:ss} ({localTime.Kind})");
            _output.WriteLine($"  Stored/retrieved: {storedValue:yyyy-MM-dd HH:mm:ss} ({storedValue.Kind})");

            if (storedValue == localTime.ToUniversalTime())
                _output.WriteLine($"  RESULT: Dataverse CONVERTED local to UTC");
            else if (storedValue.Hour == localTime.Hour && storedValue.Minute == localTime.Minute)
                _output.WriteLine($"  RESULT: Dataverse stored raw value as UTC (no conversion)");
            else
                _output.WriteLine($"  RESULT: Unexpected behavior - investigate further");

            // Cleanup
            service.Delete("phonecall", id);
            _output.WriteLine($"Cleaned up phonecall: {id}");
        }

        /// <summary>
        /// Test #2: Send DateTime with DateTimeKind.Utc to Dataverse
        /// This should be the normal/expected path
        /// </summary>
        [Fact(Skip = "Integration test - set user secret and run with --filter Category=RequiresDataverse")]
        public void When_DateTime_Kind_Is_Utc_What_Gets_Stored()
        {
            var service = GetRealService();

            var utcTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);

            _output.WriteLine($"=== TEST: DateTimeKind.Utc ===");
            _output.WriteLine($"Input DateTime: {utcTime}");
            _output.WriteLine($"Input Kind: {utcTime.Kind}");

            var phonecall = new Entity("phonecall")
            {
                ["subject"] = $"DateTime Test - Utc Kind - {DateTime.UtcNow:yyyyMMddHHmmss}",
                ["actualend"] = utcTime,
                ["description"] = $"Test input: {utcTime} Kind={utcTime.Kind}"
            };

            var id = service.Create(phonecall);
            _output.WriteLine($"Created phonecall: {id}");

            var retrieved = service.Retrieve("phonecall", id, new ColumnSet("actualend", "subject"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("actualend");

            _output.WriteLine($"");
            _output.WriteLine($"Retrieved DateTime: {storedValue}");
            _output.WriteLine($"Retrieved Kind: {storedValue.Kind}");
            _output.WriteLine($"");
            _output.WriteLine($"ANALYSIS:");
            _output.WriteLine($"  Input:  {utcTime:yyyy-MM-dd HH:mm:ss} ({utcTime.Kind})");
            _output.WriteLine($"  Output: {storedValue:yyyy-MM-dd HH:mm:ss} ({storedValue.Kind})");

            if (storedValue == utcTime)
                _output.WriteLine($"  RESULT: Values match exactly (expected)");
            else
                _output.WriteLine($"  RESULT: Values differ - investigate");

            service.Delete("phonecall", id);
            _output.WriteLine($"Cleaned up phonecall: {id}");
        }

        /// <summary>
        /// Test #3: Send DateTime with DateTimeKind.Unspecified to Dataverse
        /// Question: Does Dataverse assume UTC or Local?
        /// </summary>
        [Fact(Skip = "Integration test - set user secret and run with --filter Category=RequiresDataverse")]
        public void When_DateTime_Kind_Is_Unspecified_What_Gets_Stored()
        {
            var service = GetRealService();

            var unspecifiedTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);

            _output.WriteLine($"=== TEST: DateTimeKind.Unspecified ===");
            _output.WriteLine($"Input DateTime: {unspecifiedTime}");
            _output.WriteLine($"Input Kind: {unspecifiedTime.Kind}");
            _output.WriteLine($"If treated as Local, UTC would be: {DateTime.SpecifyKind(unspecifiedTime, DateTimeKind.Local).ToUniversalTime()}");

            var phonecall = new Entity("phonecall")
            {
                ["subject"] = $"DateTime Test - Unspecified Kind - {DateTime.UtcNow:yyyyMMddHHmmss}",
                ["actualend"] = unspecifiedTime,
                ["description"] = $"Test input: {unspecifiedTime} Kind={unspecifiedTime.Kind}"
            };

            var id = service.Create(phonecall);
            _output.WriteLine($"Created phonecall: {id}");

            var retrieved = service.Retrieve("phonecall", id, new ColumnSet("actualend", "subject"));
            var storedValue = retrieved.GetAttributeValue<DateTime>("actualend");

            _output.WriteLine($"");
            _output.WriteLine($"Retrieved DateTime: {storedValue}");
            _output.WriteLine($"Retrieved Kind: {storedValue.Kind}");
            _output.WriteLine($"");
            _output.WriteLine($"ANALYSIS:");
            _output.WriteLine($"  Input:  {unspecifiedTime:yyyy-MM-dd HH:mm:ss} ({unspecifiedTime.Kind})");
            _output.WriteLine($"  Output: {storedValue:yyyy-MM-dd HH:mm:ss} ({storedValue.Kind})");

            if (storedValue.Hour == unspecifiedTime.Hour && storedValue.Minute == unspecifiedTime.Minute)
                _output.WriteLine($"  RESULT: Dataverse treated Unspecified as UTC (stored raw value)");
            else
                _output.WriteLine($"  RESULT: Dataverse converted - may have treated as Local");

            service.Delete("phonecall", id);
            _output.WriteLine($"Cleaned up phonecall: {id}");
        }

        /// <summary>
        /// Test #4: Compare all three in one test for side-by-side comparison
        /// </summary>
        [Fact(Skip = "Integration test - set user secret and run with --filter Category=RequiresDataverse")]
        public void Compare_All_DateTime_Kinds_Side_By_Side()
        {
            var service = GetRealService();

            var baseTime = new DateTime(2025, 6, 15, 14, 30, 0); // 2:30 PM
            var localTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Local);
            var utcTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);
            var unspecifiedTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);

            _output.WriteLine($"=== SIDE-BY-SIDE COMPARISON ===");
            _output.WriteLine($"Base time: 2025-06-15 14:30:00");
            _output.WriteLine($"Local timezone: {TimeZoneInfo.Local.Id} (offset: {TimeZoneInfo.Local.GetUtcOffset(localTime)})");
            _output.WriteLine($"");

            var testCases = new[]
            {
                ("Local", localTime),
                ("Utc", utcTime),
                ("Unspecified", unspecifiedTime)
            };

            var results = new System.Collections.Generic.List<(string Kind, DateTime Input, DateTime Output, Guid Id)>();

            foreach (var (kind, inputTime) in testCases)
            {
                var phonecall = new Entity("phonecall")
                {
                    ["subject"] = $"DateTime Comparison - {kind} - {DateTime.UtcNow:yyyyMMddHHmmss}",
                    ["actualend"] = inputTime
                };

                var id = service.Create(phonecall);
                var retrieved = service.Retrieve("phonecall", id, new ColumnSet("actualend"));
                var storedValue = retrieved.GetAttributeValue<DateTime>("actualend");

                results.Add((kind, inputTime, storedValue, id));
            }

            _output.WriteLine($"| Kind        | Input Time          | Input Kind    | Output Time         | Output Kind | Delta |");
            _output.WriteLine($"|-------------|---------------------|---------------|---------------------|-------------|-------|");

            foreach (var r in results)
            {
                var delta = r.Output - r.Input;
                _output.WriteLine($"| {r.Kind,-11} | {r.Input:yyyy-MM-dd HH:mm:ss} | {r.Input.Kind,-13} | {r.Output:yyyy-MM-dd HH:mm:ss} | {r.Output.Kind,-11} | {delta.TotalHours:+0.0;-0.0;0} hrs |");
            }

            _output.WriteLine($"");
            _output.WriteLine($"INTERPRETATION:");
            _output.WriteLine($"- If Local shows a delta matching your timezone offset, Dataverse CONVERTS to UTC");
            _output.WriteLine($"- If all deltas are 0, Dataverse stores raw values and just marks as UTC");
            _output.WriteLine($"- UTC should always have 0 delta");

            // Cleanup
            foreach (var r in results)
            {
                service.Delete("phonecall", r.Id);
            }
            _output.WriteLine($"");
            _output.WriteLine($"Cleaned up {results.Count} test records");
        }
    }
}

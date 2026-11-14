using System;
using Xunit;

namespace MentalHealthTrackerBlazor.Tests;

/// <summary>
/// A fact that only runs when a test database is configured. Set
/// <c>MHT_TEST_CONNECTION</c> to a SQL Server connection string to enable these;
/// otherwise they are reported as skipped rather than failed.
/// </summary>
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public const string VariableName = "MHT_TEST_CONNECTION";

    public RequiresDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableName)))
        {
            Skip = $"Set {VariableName} to a SQL Server connection string to run database tests.";
        }
    }
}

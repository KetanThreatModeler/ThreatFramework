using System.Data.Common;

namespace ThreatFramework.Core.Abstractions;

public interface IDatabaseReader
{
    // (Guid, Name) lightweight enumerations
    IAsyncEnumerable<(Guid Guid, string Name)> EnumerateComponentsAsync(CancellationToken ct);
    IAsyncEnumerable<(Guid Guid, string Name)> EnumeratePropertiesAsync(CancellationToken ct);
    IAsyncEnumerable<(Guid Guid, string Name)> EnumerateThreatsAsync(CancellationToken ct);
    IAsyncEnumerable<(Guid Guid, string Name)> EnumerateSecurityRequirementsAsync(CancellationToken ct);
    IAsyncEnumerable<(Guid Guid, string Name)> EnumerateTestCasesAsync(CancellationToken ct);
    IAsyncEnumerable<(Guid Guid, string Name)> EnumerateLibrariesAsync(CancellationToken ct);
    IAsyncEnumerable<(Guid? Guid, string OptionText)> EnumeratePropertyOptionsAsync(CancellationToken ct);
}

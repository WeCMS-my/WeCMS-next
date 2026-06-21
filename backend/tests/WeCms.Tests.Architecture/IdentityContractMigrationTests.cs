namespace WeCms.Tests.Architecture;

public sealed class IdentityContractMigrationTests
{
    private static readonly string IdentityRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity");

    private static readonly string SystemRoot = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule);

    private static readonly string[] IdentityContractFiles =
    [
        Path.Combine("Contracts", "AuthDtos.cs"),
        Path.Combine("Contracts", "AccountProfileDtos.cs"),
        Path.Combine("Contracts", "AccountTwoFactorDtos.cs"),
        Path.Combine("Contracts", "UserDtos.cs")
    ];

    private static readonly string[] IdentityRecordFiles =
    [
        Path.Combine("Records", "AuthRecords.cs"),
        Path.Combine("Records", "AccountProfileRecords.cs"),
        Path.Combine("Records", "UserRecords.cs"),
        Path.Combine("Records", "TwoFactorRecords.cs")
    ];

    private static readonly string[] LegacyIdentityContractFiles =
    [
        Path.Combine("Auth", "AuthDtos.cs"),
        Path.Combine("Auth", "AccountProfileDtos.cs"),
        Path.Combine("Auth", "AccountTwoFactorDtos.cs"),
        Path.Combine("Users", "UserDtos.cs"),
        Path.Combine("Auth", "AuthRecords.cs"),
        Path.Combine("Users", "UserRecords.cs"),
        Path.Combine("TwoFactor", "TwoFactorRecords.cs")
    ];

    [Fact]
    public void IdentityDtoAndRecordFiles_LiveInIdentityModule()
    {
        foreach (var relativePath in IdentityContractFiles.Concat(IdentityRecordFiles))
        {
            var path = Path.Combine(IdentityRoot, relativePath);
            Assert.True(File.Exists(path), $"Missing Identity contract/record file: {relativePath}");
        }
    }

    [Fact]
    public async Task IdentityDtoAndRecordFiles_UseIdentityNamespaces()
    {
        foreach (var relativePath in IdentityContractFiles)
        {
            var source = await File.ReadAllTextAsync(Path.Combine(IdentityRoot, relativePath), TestContext.Current.CancellationToken);
            Assert.Contains("namespace WeCms.Modules.Identity.Contracts;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemModule + ".", source, StringComparison.Ordinal);
        }

        foreach (var relativePath in IdentityRecordFiles)
        {
            var source = await File.ReadAllTextAsync(Path.Combine(IdentityRoot, relativePath), TestContext.Current.CancellationToken);
            Assert.Contains("namespace WeCms.Modules.Identity.Records;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace " + LegacyBoundaryNames.SystemModule + ".", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LegacySystemIdentityDtoAndRecordFiles_AreRemoved()
    {
        var remaining = LegacyIdentityContractFiles
            .Select(relativePath => Path.Combine(SystemRoot, relativePath))
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(SystemRoot, path))
            .ToArray();

        Assert.True(
            remaining.Length == 0,
            "Legacy System identity DTO/record files remain: " + string.Join(", ", remaining));
    }
}

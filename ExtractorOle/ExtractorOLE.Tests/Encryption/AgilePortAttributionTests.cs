using System.Xml.Linq;
using Xunit;

namespace ExtractorOLE.Tests.Encryption;

// ADR-012 decision 2: the Agile port keeps its Apache-2.0 attribution and adds no project reference.
public class AgilePortAttributionTests
{
    private const string NpoiCommit = "72ff7c756fc5bb52ec86848c3724556171bc4b23";

    public static TheoryData<string> PortedFiles => new()
    {
        "AgileDecryptor.cs",
        "AgileEncryptionHeader.cs",
        "AgileEncryptionVerifier.cs",
    };

    [Theory]
    [MemberData(nameof(PortedFiles))]
    public void PortedFile_KeepsApacheHeaderAndNamesSourceAndCommit(string fileName)
    {
        var text = File.ReadAllText(RepoPath("ExtractorOle/ExtractorOLE/Helpers/Encryption/Agile/" + fileName));

        Assert.StartsWith("/* ====", text, StringComparison.Ordinal);
        Assert.Contains("Licensed to the Apache Software Foundation (ASF)", text, StringComparison.Ordinal);
        Assert.Contains("http://www.apache.org/licenses/LICENSE-2.0", text, StringComparison.Ordinal);
        Assert.Contains("third_party/npoi/ooxml/POIFS/Crypt/Agile/" + fileName, text, StringComparison.Ordinal);
        Assert.Contains(NpoiCommit, text, StringComparison.Ordinal);
    }

    [Fact]
    public void ThirdPartyNotices_HasNpoiApacheEntry()
    {
        var text = File.ReadAllText(RepoPath("THIRD-PARTY-NOTICES"));

        Assert.Contains("nissl-lab/npoi", text, StringComparison.Ordinal);
        Assert.Contains("Apache Software Foundation", text, StringComparison.Ordinal);
        Assert.Contains("Apache License, Version 2.0", text, StringComparison.Ordinal);
        Assert.Contains(NpoiCommit, text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractorOleProject_ReferencesUnchanged_NoNpoiOoxml()
    {
        var project = XDocument.Load(RepoPath("ExtractorOle/ExtractorOLE/ExtractorOLE.csproj"));
        var references = project.Descendants("ProjectReference")
            .Select(r => r.Attribute("Include")!.Value.Replace('\\', '/'))
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedProjectReferences, references);
    }

    private static readonly string[] ExpectedProjectReferences =
    [
        "../../third_party/b2xtranslator/Doc/b2xtranslator.doc.csproj",
        "../../third_party/b2xtranslator/Ppt/b2xtranslator.ppt.csproj",
        "../../third_party/npoi/main/NPOI.Core.csproj",
    ];

    private static string RepoPath(string relativePath)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' from {AppContext.BaseDirectory}");
    }
}

using System.Buffers.Binary;
using System.Reflection;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.Crypt.CryptoAPI;
using NPOI.POIFS.FileSystem;
using Xunit;
using static ExtractorOLE.Tests.Encryption.ConfidentialityAssertions;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// The statics snapshot/compare tests read process-wide state, so nothing else may run alongside them.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LegacyStaticsCollectionDefinition
{
    public const string Name = "LegacyStatics";
}

/// <summary>
/// #409 (S-lg-quality): a distinctive sentinel password (or, on success paths, the real password) is
/// supplied through every outcome of the legacy .doc/.ppt password paths, single and batch. It must
/// never reach the guardrail log, an exception, a FieldError, a serialized result or telemetry, and
/// the legacy encryption types must retain no text in static state. Reuses the #385 helpers.
/// </summary>
[Collection(LegacyStaticsCollectionDefinition.Name)]
public class LegacyConfidentialityTests
{
    private const string Sentinel = "SENTINEL-pw-9X7qLk3M-ZzTop";
    private const string HovercraftXorDoc = "Test-weak-XOR_myhovercraftisfullofeels_.doc";
    private const string HovercraftPassword = "myhovercraftisfullofeels";
    private const string DocMime = "application/msword";
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const string LegacyNamespace = "ExtractorOLE.Helpers.Encryption.Legacy";
    private const int OffLKey = 0x0E;
    private const int OffKeySize = 12 + 16;
    private const int OffFlags = 0x0A;
    private const int OffsetHeaderToken = 12;
    private const uint TokenEncrypted = 0xF3D1C4DF;
    private const string SummaryName = "\u0005SummaryInformation";

    // Decrypted plaintext of the fixtures: it must never be echoed into an error message.
    private static readonly string[] DecryptedTexts =
    [
        "This is a test",
        "This is an encrypted Word 2007 File.",
        "This is a protected document",
        "It only has the one slide",
        "It does have notes",
        "Just for the hell of it, we have some notes",
        "Lorem ipsum dolor sit amet",
    ];

    // Decrypted content each success / FieldError scenario must really return (so the needles above are not vacuous).
    private static readonly Dictionary<string, string[]> ExpectedDecryptedText = new()
    {
        ["doc-success-tika"] = ["This is an encrypted Word 2007 File."],
        ["doc-g2-field-error"] = ["This is a test"],
        ["doc-xor-success-hovercraft"] = ["Lorem ipsum dolor sit amet", "Test File For Cracking"],
        ["ppt-success-hello"] = ["This is a protected document", "It only has the one slide", "It does have notes", "Just for the hell of it, we have some notes"],
    };

    private static void AssertDecryptedTextPresent(ConfidentialityScenario scenario, DocumentExtractionResult? result)
    {
        if (!ExpectedDecryptedText.TryGetValue(scenario.Name, out var needles)) return;
        Assert.NotNull(result);
        var content = result.ExtractedText + " | " + result.Metadata.Title;
        foreach (var needle in needles)
        {
            Assert.True(content.Contains(needle, StringComparison.Ordinal), $"{scenario.Name}: decrypted text '{needle}' not returned");
        }
    }

    // ---- fixtures ----

    private static ExtractionRequest Req(byte[] bytes, string fileName, string mime, string? password) =>
        new() { FileBytes = bytes, FileName = fileName, DetectedMimeType = mime, Password = password };

    private static byte[] CryptoApiDoc() => LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc);

    private static byte[] DocWithLKeyMax() =>
        CfbMutator.ReplaceStream(CryptoApiDoc(), "WordDocument", w =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(w.AsSpan(OffLKey), 0xFFFFFFFF);
            return w;
        });

    // The fixture keeps its encryption header in 1Table.
    private static byte[] DocWithKeySize41() =>
        CfbMutator.ReplaceStream(CryptoApiDoc(), "1Table", t =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(t.AsSpan(OffKeySize), 41);
            return t;
        });

    private static byte[] DocWithVersion44() =>
        CfbMutator.ReplaceStream(CryptoApiDoc(), "1Table", t =>
        {
            BinaryPrimitives.WriteUInt16LittleEndian(t.AsSpan(0), 4);
            BinaryPrimitives.WriteUInt16LittleEndian(t.AsSpan(2), 4);
            return t;
        });

    private static byte[] XorDoc() =>
        CfbMutator.ReplaceStream(CryptoApiDoc(), "WordDocument", w =>
        {
            BinaryPrimitives.WriteUInt16LittleEndian(w.AsSpan(OffFlags), (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(w.AsSpan(OffFlags)) | 0x8000));
            return w;
        });

    // Gap G2: encrypted document properties the reader cannot use -> text kept, one Metadata FieldError.
    private static byte[] G2Doc()
    {
        var bytes = CfbMutator.ReplaceStream(CryptoApiDoc(), "1Table", t =>
        {
            t[4] &= unchecked((byte)~LegacyEncryptionHeaderReader.FlagDocProps);
            return t;
        });
        bytes = CfbMutator.RemoveEntry(bytes, SummaryName);
        var encryptionStream = new byte[64];
        new Random(409).NextBytes(encryptionStream);
        return CfbMutator.AddStream(bytes, "encryption", encryptionStream);
    }

    private static byte[] PptWithSummaryDescriptorCountMax()
    {
        var hello = LegacyFixtures.Bytes(LegacyFixtures.HelloPpt);
        var decryptor = DecryptorFor(hello, "hello");
        return CfbMutator.ReplaceStream(hello, CryptoApiEncryptedSummaryReader.PptStreamName, summary =>
        {
            // RC4 is symmetric: the same block-0 transform decrypts and re-encrypts.
            var header = Block0(decryptor, summary, 0, 8);
            var arrayOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
            var arraySize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
            var array = Block0(decryptor, summary, arrayOffset, arraySize);
            BinaryPrimitives.WriteUInt32LittleEndian(array, 0x7FFFFFFFu);

            var result = (byte[])summary.Clone();
            Block0(decryptor, header, 0, 8).CopyTo(result, 0);
            Block0(decryptor, array, 0, array.Length).CopyTo(result, arrayOffset);
            return result;
        });
    }

    private static byte[] PptWithUnreadableStructure() =>
        CfbMutator.ReplaceStream(File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainReferencePptPath)), "Current User", b =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(OffsetHeaderToken), TokenEncrypted);
            return b;
        });

    private static byte[] PptWithTruncatedPictures() =>
        CfbMutator.ReplaceStream(LegacyFixtures.Bytes(LegacyFixtures.CryptoApiProc2356Ppt), "Pictures", p => p.AsSpan(0, p.Length / 2).ToArray());

    private static byte[] Block0(CryptoAPIDecryptor decryptor, byte[] data, int offset, int length)
    {
        var copy = data.AsSpan(offset, length).ToArray();
        decryptor.InitCipherForBlock(null, 0).Update(copy, 0, copy.Length, copy);
        return copy;
    }

    private static CryptoAPIDecryptor DecryptorFor(byte[] pptBytes, string password)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(pptBytes, writable: false));
        var probe = PptEncryptionProbe.Probe(fs.Root);
        var payload = PptEncryptionProbe.CryptSessionPayload(probe.Layout!, PptMime);
        var header = LegacyEncryptionHeaderReader.Parse(payload, LegacyContainer.Ppt, PptMime);
        var decryptor = LegacyEncryptionHeaderReader.CreateDecryptor(header);
        Assert.True(LegacyEncryptionHeaderReader.VerifyPassword(decryptor, password));
        return (CryptoAPIDecryptor)decryptor;
    }

    private static readonly Lazy<IReadOnlyList<ConfidentialityScenario>> Scenarios = new(() => new List<ConfidentialityScenario>
    {
        new("doc-success-tika", "tika", () => Req(LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc), "fixture-b.doc", DocMime, "tika"), null),
        new("doc-invalid-password", Sentinel, () => Req(LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc), "fixture-b.doc", DocMime, Sentinel), typeof(InvalidPasswordException)),
        new("doc-corrupt-lkey-max", Sentinel, () => Req(DocWithLKeyMax(), "lkey.doc", DocMime, Sentinel), typeof(CorruptFileException)),
        new("doc-corrupt-keysize-41", Sentinel, () => Req(DocWithKeySize41(), "keysize.doc", DocMime, Sentinel), typeof(CorruptFileException)),
        new("doc-xor-invalid-password", Sentinel, () => Req(XorDoc(), "xor.doc", DocMime, Sentinel), typeof(InvalidPasswordException)),
        new("doc-v44-password-protected", Sentinel, () => Req(DocWithVersion44(), "v44.doc", DocMime, Sentinel), typeof(PasswordProtectedException)),
        new("doc-xor-success-hovercraft", HovercraftPassword, () => Req(LegacyFixtures.Bytes(HovercraftXorDoc), "fixture-x.doc", DocMime, HovercraftPassword), null),
        new("doc-g2-field-error", "password", () => Req(G2Doc(), "g2.doc", DocMime, "password"), null, true),
        new("ppt-success-hello", "hello", () => Req(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), "fixture-a.ppt", PptMime, "hello"), null),
        new("ppt-invalid-password", Sentinel, () => Req(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), "fixture-a.ppt", PptMime, Sentinel), typeof(InvalidPasswordException)),
        new("ppt-corrupt-descriptor-count", "hello", () => Req(PptWithSummaryDescriptorCountMax(), "count.ppt", PptMime, "hello"), typeof(CorruptFileException)),
        new("ppt-corrupt-structure-unreadable", Sentinel, () => Req(PptWithUnreadableStructure(), "structure.ppt", PptMime, Sentinel), typeof(CorruptFileException)),
        new("ppt-pictures-field-error", "crypto", () => Req(PptWithTruncatedPictures(), "pictures.ppt", PptMime, "crypto"), null, true),
    });

    public static TheoryData<string> ScenarioNames()
    {
        var data = new TheoryData<string>();
        foreach (var s in Scenarios.Value) data.Add(s.Name);
        return data;
    }

    private static (ConfidentialityScenario Scenario, ConfidentialityRig Rig, DocumentExtractionResult? Result, Exception? Error) Execute(string name)
    {
        var executed = ConfidentialityRig.Execute(Scenarios.Value.Single(s => s.Name == name));
        AssertDecryptedTextPresent(executed.Scenario, executed.Result);
        ScrubLegitimateContent(executed.Scenario, executed.Result);
        return executed;
    }

    // Two fixtures contain their own real password as decrypted document content: the hello deck says
    // "The password is “hello”" on a slide, and the proc2356 deck is titled "RC4 CryptoAPI
    // Encryption" ("crypto"). That content is asserted exactly and then blanked, so the secret is still
    // found anywhere else in the result.
    private static void ScrubLegitimateContent(ConfidentialityScenario scenario, DocumentExtractionResult? result)
    {
        if (result is null) return;
        switch (scenario.Name)
        {
            case "ppt-pictures-field-error":
                Assert.Equal("RC4 CryptoAPI Encryption", result.Metadata.Title);
                result.Metadata.Title = null;
                break;
            case "ppt-success-hello":
                const string quoted = "“hello”";
                Assert.Single(result.ExtractedText.Split(quoted)[1..]);
                result.ExtractedText = result.ExtractedText.Replace(quoted, string.Empty, StringComparison.Ordinal);
                break;
        }
    }

    // Every secret in play: the sentinel plus the real passwords of the success and FieldError scenarios.
    private static string[] AllSecrets() => Scenarios.Value.Select(s => s.Secret).Distinct(StringComparer.Ordinal).ToArray();

    // ---- no decrypted text in error messages ----

    private static void AssertNoDecryptedText(Exception? error, DocumentExtractionResult? result)
    {
        foreach (var text in DecryptedTexts)
        {
            if (error is not null)
            {
                AssertTextClean(error.Message, text, "exception message (decrypted text)");
                AssertTextClean(error.ToString(), text, "exception ToString (decrypted text)");
            }

            foreach (var fieldError in result?.FieldErrors ?? [])
            {
                AssertTextClean(fieldError.Message, text, "FieldError.Message (decrypted text)");
            }
        }
    }

    // ---- retained static state in the legacy encryption types ----

    private static List<FieldInfo> LegacyStaticFields() =>
        typeof(LegacyDecryption).Assembly.GetTypes()
            .Where(t => t.Namespace == LegacyNamespace && !t.ContainsGenericParameters)
            .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(f => !f.IsLiteral) // const fields are compile-time literals: nothing is stored at run time
            .ToList();

    private static bool IsTextHolder(Type type) =>
        type == typeof(string) || type == typeof(string[]) || type == typeof(char[]) || type == typeof(byte[]) || type == typeof(object);

    // What a text-holding field contains, as comparable text (null for anything else).
    private static string? TextSnapshot(object? value) => value switch
    {
        null => "<null>",
        string s => s,
        string[] a => string.Join("|", a),
        char[] c => new string(c),
        byte[] b => Convert.ToHexString(b),
        _ => null,
    };

    private static Dictionary<string, string?> StaticSnapshot() =>
        LegacyStaticFields().Where(f => IsTextHolder(f.FieldType)).ToDictionary(f => $"{f.DeclaringType!.FullName}.{f.Name}", f => TextSnapshot(f.GetValue(null)));

    // Before the call (snapshot taken first): every static field of the legacy types is readonly, or a
    // compiler-generated delegate cache, so a call cannot store anything in it. After: the text-holding
    // fields are unchanged, nothing reachable from any field contains the secret, and the test-only
    // AsyncLocal hook is null.
    private static void AssertLegacyStaticsHoldNothing(IReadOnlyDictionary<string, string?> before, params string[] secrets)
    {
        var fields = LegacyStaticFields();
        Assert.NotEmpty(fields);
        foreach (var field in fields)
        {
            var name = $"{field.DeclaringType!.FullName}.{field.Name}";
            Assert.True(field.IsInitOnly || typeof(Delegate).IsAssignableFrom(field.FieldType), $"{name} is a mutable static of type {field.FieldType}");
            var value = field.GetValue(null);
            foreach (var secret in secrets) AssertNoSecretInGraph(secret, name, value);
            if (IsTextHolder(field.FieldType))
            {
                Assert.True(before.TryGetValue(name, out var snapshot), $"{name} appeared after the call");
                Assert.Equal(snapshot, TextSnapshot(value));
            }
        }

        Assert.Null(LegacyDecryption.BeforeDocDecryptForTest.Value);
    }

    // The sentinel scenarios additionally get the full #385 sweep (extractor and provider graphs, every
    // ExtractorOLE static). A real password such as "password" or "hello" is a common word that can occur
    // in unrelated library strings, so those scenarios keep to the targeted outputs and the legacy statics.
    private static void AssertScenarioClean(ConfidentialityScenario scenario, ConfidentialityRig rig, DocumentExtractionResult? result, Exception? error)
    {
        if (scenario.Secret == Sentinel)
        {
            rig.AssertAllClean(scenario.Secret, result, error);
            return;
        }

        AssertLoggerClean(rig.Logger, scenario.Secret);
        AssertExceptionClean(error, scenario.Secret);
        AssertResultClean(result, scenario.Secret);
        rig.Sink.AssertClean(scenario.Secret);
        AssertHssfSlotCleared();
    }

    // [S-lg-quality#1] The guardrail log, exceptions (message, ToString, Data, inner chain) and telemetry carry no secret.
    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Extract_Outcome_LogsExceptionsAndTelemetryCarryNoSecret(string name)
    {
        var (scenario, rig, _, error) = Execute(name);
        using (rig)
        {
            AssertLoggerClean(rig.Logger, scenario.Secret);
            AssertExceptionClean(error, scenario.Secret);
            rig.Sink.AssertClean(scenario.Secret);
        }
    }

    // [S-lg-quality#1] FieldError text and the JSON-serialized result (FormatMetadata by runtime type, raw byte[] content too) carry no secret.
    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Extract_Outcome_FieldErrorsAndSerializedResultCarryNoSecret(string name)
    {
        var (scenario, rig, result, _) = Execute(name);
        using (rig)
        {
            AssertResultClean(result, scenario.Secret);
            foreach (var error in result?.FieldErrors ?? [])
            {
                AssertTextClean(error.Message, scenario.Secret, "FieldError.Message");
            }
        }
    }

    // [S-lg-quality#1] Nothing retained in the extractor graph or statics (sentinel scenarios); HSSF slot null.
    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Extract_Outcome_NoRetainedSecret(string name)
    {
        var (scenario, rig, result, error) = Execute(name);
        using (rig)
        {
            AssertScenarioClean(scenario, rig, result, error);
        }
    }

    // [S-lg-quality#2] The legacy encryption types hold no text in static state, before and after the call.
    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Extract_Outcome_LegacyEncryptionStaticsRetainNothing(string name)
    {
        var scenario = Scenarios.Value.Single(s => s.Name == name);
        var request = scenario.Request(); // built (and its types initialised) before the snapshot
        var before = StaticSnapshot();

        using var rig = new ConfidentialityRig(scenario.Limits);
        var (result, error) = rig.Run(request);
        if (scenario.ExpectedException is not null)
        {
            Assert.NotNull(error);
            Assert.Equal(scenario.ExpectedException, error.GetType());
        }
        else
        {
            Assert.True(error is null, $"{scenario.Name}: unexpected {error}");
            Assert.NotNull(result);
            Assert.Equal(scenario.ExpectFieldErrors, result.FieldErrors.Count > 0);
        }

        AssertLegacyStaticsHoldNothing(before, scenario.Secret);
    }

    // [S-lg-quality#1] The CORRUPT_FILE paths (and every FieldError) never echo decrypted document text.
    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Extract_Outcome_ErrorMessagesCarryNoDecryptedText(string name)
    {
        var (_, rig, result, error) = Execute(name);
        using (rig)
        {
            AssertNoDecryptedText(error, result);
        }
    }

    // [S-lg-quality#1] The scenarios really hit the pinned failure modes (so a clean result is not vacuous).
    [Fact]
    public void Scenarios_PinTheirOutcomes()
    {
        var names = Scenarios.Value.Select(s => s.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(Scenarios.Value, s => s.ExpectedException == typeof(CorruptFileException));
        Assert.Contains(Scenarios.Value, s => s.ExpectedException == typeof(InvalidPasswordException));
        Assert.Contains(Scenarios.Value, s => s.ExpectedException == typeof(PasswordProtectedException));
        Assert.Contains(Scenarios.Value, s => s.ExpectFieldErrors);

        var (_, g2Rig, g2Result, _) = Execute("doc-g2-field-error");
        using (g2Rig)
        {
            Assert.Equal("This is a test", g2Result!.ExtractedText.Trim());
            var g2 = Assert.Single(g2Result.FieldErrors);
            Assert.Equal("Metadata", g2.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, g2.Code);
        }

        var (_, picturesRig, picturesResult, _) = Execute("ppt-pictures-field-error");
        using (picturesRig)
        {
            var pictures = Assert.Single(picturesResult!.FieldErrors, e => e.FieldPath == "EmbeddedFiles[Pictures]");
            Assert.Equal(FieldErrorCode.CorruptFile, pictures.Code);
        }
    }

    // ---- batch ----

    private static List<ExtractionRequest> BatchRequests() => Scenarios.Value.Select(s => s.Request()).ToList();

    private static void AssertBatchClean(ConfidentialityRig rig, IReadOnlyList<BatchExtractionResult> items, IReadOnlyDictionary<string, string?> staticsBefore)
    {
        var scenarios = Scenarios.Value;
        Assert.Equal(scenarios.Count, items.Count);
        Assert.Equal(Enumerable.Range(0, scenarios.Count), items.Select(i => i.Index).Order());
        foreach (var item in items.OrderBy(i => i.Index))
        {
            var scenario = scenarios[item.Index];
            if (scenario.ExpectedException is null)
            {
                Assert.True(item.Succeeded, $"{scenario.Name}: {item.Error}");
                Assert.Equal(scenario.ExpectFieldErrors, item.Result!.FieldErrors.Count > 0);
            }
            else
            {
                Assert.NotNull(item.Error);
                Assert.Equal(scenario.ExpectedException, item.Error.GetType());
            }

            AssertDecryptedTextPresent(scenario, item.Result);
            ScrubLegitimateContent(scenario, item.Result);
            // One rule for the batch: the sentinel is checked everywhere (every item, log, telemetry, graphs);
            // a common-word real password ("hello", "password") only against its own item's targeted outputs.
            AssertBatchItemClean(item, Sentinel);
            if (scenario.Secret != Sentinel) AssertBatchItemClean(item, scenario.Secret);
            AssertNoDecryptedText(item.Error, item.Result);
        }

        Assert.True(rig.Sink.ActivityCount > 0);
        AssertLoggerClean(rig.Logger, Sentinel);
        rig.Sink.AssertClean(Sentinel);

        AssertNoSecretInGraph(Sentinel, nameof(ExtractorOLE.MainExtractor), rig.Extractor);
        AssertNoSecretInStatics(Sentinel, typeof(ExtractorOLE.MainExtractor).Assembly);
        AssertLegacyStaticsHoldNothing(staticsBefore, AllSecrets());
        AssertHssfSlotCleared(); // the test thread's slot; worker threads' slots are not reachable from here
    }

    // [S-lg-quality#1][S-lg-quality#2]
    [Fact]
    public async Task ExtractManyAsync_MixedLegacyOutcomes_CarryNoSecret()
    {
        var requests = BatchRequests();
        var before = StaticSnapshot();
        using var rig = new ConfidentialityRig();

        var items = await rig.Extractor.ExtractManyAsync(requests, 2);

        AssertBatchClean(rig, items, before);
    }

    // [S-lg-quality#1][S-lg-quality#2]
    [Fact]
    public async Task ExtractManyStreamingAsync_MixedLegacyOutcomes_CarryNoSecret()
    {
        var requests = BatchRequests();
        var before = StaticSnapshot();
        using var rig = new ConfidentialityRig();
        var items = new List<BatchExtractionResult>();

        await foreach (var item in rig.Extractor.ExtractManyStreamingAsync(requests, 2))
        {
            items.Add(item);
        }

        AssertBatchClean(rig, items, before);
    }

    // ---- negative controls ----

    private static class MutableTextHolder
    {
        internal static string? Leak;
    }

    // [S-lg-quality#2] The static-state check bites: a mutable text field is not accepted, a leaked secret is found.
    [Fact]
    public void Control_StaticFieldHoldingSecret_IsDetected()
    {
        var field = typeof(MutableTextHolder).GetField(nameof(MutableTextHolder.Leak), BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.False(field.IsInitOnly);
        Assert.True(IsTextHolder(field.FieldType));

        MutableTextHolder.Leak = "x-" + Sentinel;
        try
        {
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertNoSecretInGraph(Sentinel, "Leak", field.GetValue(null)));
        }
        finally
        {
            MutableTextHolder.Leak = null;
        }
    }

    // [S-lg-quality#2] The real AssertLegacyStaticsHoldNothing fails when a text-holding static differs from
    // its snapshot, or is missing from it.
    [Fact]
    public void Control_LegacyStaticsHelper_DetectsChangedOrUnknownTextField()
    {
        var snapshot = StaticSnapshot();
        var key = Assert.Single(snapshot.Keys, k => k.EndsWith("XlsEncryptedProperties.WorkbookStreamNames", StringComparison.Ordinal));
        AssertLegacyStaticsHoldNothing(snapshot, Sentinel);

        var changed = new Dictionary<string, string?>(snapshot) { [key] = "Workbook|Book|" + Sentinel };
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertLegacyStaticsHoldNothing(changed, Sentinel));

        var missing = new Dictionary<string, string?>(snapshot);
        missing.Remove(key);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertLegacyStaticsHoldNothing(missing, Sentinel));
    }

    // [S-lg-quality#1] The decrypted-text check bites.
    [Fact]
    public void Control_DecryptedTextInErrorMessage_IsDetected()
    {
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertNoDecryptedText(new CorruptFileException(DocMime, "bad: This is a test"), null));

        var result = new DocumentExtractionResult();
        result.FieldErrors.Add(new FieldError("Metadata", FieldErrorCode.CorruptFile, "near 'This is a protected document'"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertNoDecryptedText(null, result));
    }
}

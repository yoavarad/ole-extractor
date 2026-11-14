using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Tests.Excel;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.Record.Crypto;
using NPOI.HSSF.UserModel;
using Xunit;
using Xunit.Sdk;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;
using static ExtractorOLE.Tests.Encryption.ConfidentialityAssertions;

namespace ExtractorOLE.Tests.Encryption;

/// <summary>
/// #385: a distinctive sentinel password is supplied (as the real password of in-test encrypted
/// fixtures, or as the wrong password against corpus samples) through every outcome of the OOXML and
/// xls paths, detection and the batch forms. It must never reach the guardrail log, an exception, a
/// FieldError, a serialized result, telemetry, a retained field or the HSSF thread-static slot.
/// Doc/ppt reuse <see cref="ConfidentialityAssertions"/> and <see cref="ConfidentialityRig"/>.
/// Rows named "control-..." never supply the secret (the file is encrypted with it but the call
/// carries no password), so they only prove the no-password path does not echo anything.
/// </summary>
public class PasswordConfidentialityTests
{
    private const string Sentinel = "SENTINEL-pw-9X7qLk3M-ZzTop";
    private const string XlsAuthor = "Sentinel Author";

    // ---- fixtures (all built in-test with the sentinel as the real password) ----

    private static string Mime(OfficeMimeType type) => new ExtractionHelper().MimeFor(type);

    private static byte[] Adversarial(string fileName) =>
        File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(Path.Combine("samples", "adversarial", fileName)));

    private static byte[] TwinOf(string encryptedFileName) =>
        File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(Sample(encryptedFileName).UnencryptedTwin!));

    private static readonly Lazy<byte[]> SentinelDocx = new(() => EncryptedPackageDecryptorTests.EncryptStandard(TwinOf("password-protected.docx"), Sentinel));

    private static readonly Lazy<byte[]> SentinelGarbage = new(() =>
        EncryptedPackageDecryptorTests.EncryptStandard(Encoding.ASCII.GetBytes("this payload is not a zip package"), Sentinel));

    private static readonly Lazy<byte[]> SentinelFieldFaultDocx = new(() =>
        EncryptedPackageDecryptorTests.EncryptStandard(Adversarial("corrupt-embedded-object.docx"), Sentinel));

    private static readonly Lazy<byte[]> SentinelXls = new(() => BuildCryptoApiXls(Sentinel));

    private static readonly Lazy<byte[]> SentinelFieldFaultXls = new(() =>
        XlsPasswordDecryptionTests.CorruptEncryptionStreamDescriptorCount(SentinelXls.Value, Sentinel));

    private static byte[] BuildCryptoApiXls(string password)
    {
        using var workbook = new HSSFWorkbook();
        workbook.CreateSheet("Data").CreateRow(0).CreateCell(0).SetCellValue("confidential round trip text");
        workbook.CreateInformationProperties();
        workbook.SummaryInformation.Author = XlsAuthor;
        Biff8EncryptionKey.CurrentUserPassword = password;
        try
        {
            using var output = new MemoryStream();
            workbook.Write(output);
            return output.ToArray();
        }
        finally
        {
            Biff8EncryptionKey.CurrentUserPassword = null;
        }
    }

    private static ExtractionRequest Req(byte[] bytes, string fileName, OfficeMimeType type, string? password) =>
        new() { FileBytes = bytes, FileName = fileName, DetectedMimeType = Mime(type), Password = password };

    private static ExtractionRequest CorpusReq(string sampleName, OfficeMimeType type, string? password) =>
        Req(File.ReadAllBytes(Sample(sampleName).FullPath), sampleName, type, password);

    private static readonly Lazy<IReadOnlyList<ConfidentialityScenario>> Scenarios = new(() => new List<ConfidentialityScenario>
    {
        new("success-ooxml", Sentinel, () => Req(SentinelDocx.Value, "sentinel.docx", OfficeMimeType.Word, Sentinel), null),
        new("success-xls", Sentinel, () => Req(SentinelXls.Value, "sentinel.xls", OfficeMimeType.ExcelLegacy, Sentinel), null),
        new("control-no-password-ooxml", Sentinel, () => Req(SentinelDocx.Value, "sentinel.docx", OfficeMimeType.Word, null), typeof(PasswordProtectedException)),
        new("control-no-password-xls", Sentinel, () => Req(SentinelXls.Value, "sentinel.xls", OfficeMimeType.ExcelLegacy, null), typeof(PasswordProtectedException)),
        new("wrong-password-ooxml", Sentinel, () => CorpusReq(AgilePptx, OfficeMimeType.PowerPoint, Sentinel), typeof(InvalidPasswordException)),
        new("wrong-password-xls", Sentinel, () => CorpusReq("rc4-password.xls", OfficeMimeType.ExcelLegacy, Sentinel), typeof(InvalidPasswordException)),
        new("sentinel-package-wrong-password", Sentinel, () => Req(SentinelDocx.Value, "sentinel.docx", OfficeMimeType.Word, Sentinel + "-not"), typeof(InvalidPasswordException)),
        new("tampered-ooxml-garbage-payload", Sentinel, () => Req(SentinelGarbage.Value, "garbage.docx", OfficeMimeType.Word, Sentinel), typeof(CorruptFileException)),
        // NPOI cannot build an Agile package in-test (its Agile builder is not loadable here), so the Agile
        // integrity failure uses the corpus sample and its real password; TamperedCorpusPassword_IsDistinctive guards that choice.
        new("tampered-corpus-agile", PasswordOf(AgileTamperedXlsx), () => CorpusReq(AgileTamperedXlsx, OfficeMimeType.Excel, PasswordOf(AgileTamperedXlsx)), typeof(CorruptFileException)),
        new("file-too-large-logs", Sentinel, () => Req(SentinelDocx.Value, "sentinel.docx", OfficeMimeType.Word, Sentinel), typeof(FileTooLargeException), false, new MimeDetectionLimits { MaxFileSizeBytes = 10 }),
        new("field-error-ooxml", Sentinel, () => Req(SentinelFieldFaultDocx.Value, "fault.docx", OfficeMimeType.Word, Sentinel), null, true),
        new("field-error-xls", Sentinel, () => Req(SentinelFieldFaultXls.Value, "fault.xls", OfficeMimeType.ExcelLegacy, Sentinel), null, true),
    });

    public static TheoryData<string> ScenarioNames()
    {
        var data = new TheoryData<string>();
        foreach (var s in Scenarios.Value) data.Add(s.Name);
        return data;
    }

    private static (ConfidentialityScenario Scenario, ConfidentialityRig Rig, DocumentExtractionResult? Result, Exception? Error) Execute(string name) =>
        ConfidentialityRig.Execute(Scenarios.Value.Single(s => s.Name == name));

    // [S-pw-confidential#1] The guardrail log (the library's only logger), exceptions (message, ToString, Data, inner chain) and telemetry carry no secret.
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

    // [S-pw-confidential#2] FieldError text and the JSON-serialized result (FormatMetadata by runtime type, raw byte[] content too) carry no secret.
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

    // [S-pw-confidential#3] Nothing in the extractor graph or any ExtractorOLE static retains the secret; the HSSF slot is null.
    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void Extract_Outcome_NoRetainedSecretAndHssfSlotNull(string name)
    {
        var (scenario, rig, result, error) = Execute(name);
        using (rig)
        {
            rig.AssertAllClean(scenario.Secret, result, error);
        }
    }

    // [S-pw-confidential#1] The corpus password stands in for a sentinel in tampered-corpus-agile, so it must be
    // long enough and absent from the sample itself (a hit elsewhere could then only come from a leak).
    [Fact]
    public void TamperedCorpusPassword_IsDistinctive()
    {
        var password = PasswordOf(AgileTamperedXlsx);
        var bytes = File.ReadAllBytes(Sample(AgileTamperedXlsx).FullPath);

        Assert.True(password.Length >= 8);
        Assert.True(bytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes(password)) < 0);
        Assert.True(bytes.AsSpan().IndexOf(Encoding.Unicode.GetBytes(password)) < 0);
    }

    // [S-pw-confidential#1] Telemetry actually saw the field failure (exception event), and it is clean.
    [Theory]
    [InlineData("field-error-ooxml")]
    [InlineData("field-error-xls")]
    public void FieldFailure_ReachesTelemetryExceptionEvent_WithoutSecret(string name)
    {
        var (scenario, rig, _, _) = Execute(name);
        using (rig)
        {
            Assert.True(rig.Sink.HasExceptionEvent);
            rig.Sink.AssertClean(scenario.Secret);
        }
    }

    // [S-pw-confidential#1] The guardrail log is captured (non-vacuous) and clean.
    [Fact]
    public void FileTooLarge_IsLogged_WithoutSecret()
    {
        var (scenario, rig, _, _) = Execute("file-too-large-logs");
        using (rig)
        {
            Assert.NotEmpty(rig.Logger.Snapshot().Messages);
            AssertLoggerClean(rig.Logger, scenario.Secret);
        }
    }

    // [S-pw-confidential#2] Detection: DetectMimeType with a password, every outcome. The no-password call is a control.
    [Fact]
    public void DetectMimeType_AllOutcomes_CarryNoSecret()
    {
        using var rig = new ConfidentialityRig();
        var docx = SentinelDocx.Value;

        var inner = rig.Helper.DetectMimeType(new MimeDetectionRequest { FileBytes = docx, FileName = "sentinel.docx", Password = Sentinel });
        Assert.True(inner.IsSupported);
        Assert.Equal(Mime(OfficeMimeType.Word), inner.MimeType);
        AssertDetectionClean(inner, Sentinel);

        // Control: no password supplied.
        var none = rig.Helper.DetectMimeType(new MimeDetectionRequest { FileBytes = docx, FileName = "sentinel.docx" });
        Assert.Equal(DetectedFormat.Unknown, none.DetectedFormat);
        AssertDetectionClean(none, Sentinel);

        var xls = rig.Helper.DetectMimeType(new MimeDetectionRequest { FileBytes = SentinelXls.Value, FileName = "sentinel.xls", Password = Sentinel });
        Assert.Equal(Mime(OfficeMimeType.ExcelLegacy), xls.MimeType);
        AssertDetectionClean(xls, Sentinel);

        var wrong = Assert.Throws<InvalidPasswordException>(() => rig.Helper.DetectMimeType(
            new MimeDetectionRequest { FileBytes = File.ReadAllBytes(Sample(AgilePptx).FullPath), FileName = AgilePptx, Password = Sentinel }));
        AssertExceptionClean(wrong, Sentinel);

        var wrongOnSentinelPackage = Assert.Throws<InvalidPasswordException>(() => rig.Helper.DetectMimeType(
            new MimeDetectionRequest { FileBytes = docx, FileName = "sentinel.docx", Password = Sentinel + "-not" }));
        AssertExceptionClean(wrongOnSentinelPackage, Sentinel);

        AssertLoggerClean(rig.Logger, Sentinel);
        rig.Sink.AssertClean(Sentinel);
        AssertNoSecretInGraph(Sentinel, nameof(IExtractionHelper), rig.Helper);
        AssertNoSecretInStatics(Sentinel, typeof(MainExtractor).Assembly);
        AssertHssfSlotCleared();
    }

    private static List<ExtractionRequest> BatchRequests() => new()
    {
        Req(SentinelDocx.Value, "ok.docx", OfficeMimeType.Word, Sentinel),
        Req(SentinelXls.Value, "ok.xls", OfficeMimeType.ExcelLegacy, Sentinel),
        Req(SentinelDocx.Value, "control-nopw.docx", OfficeMimeType.Word, null), // control: no password supplied
        Req(File.ReadAllBytes(Sample(AgilePptx).FullPath), "wrong.pptx", OfficeMimeType.PowerPoint, Sentinel),
        Req(SentinelGarbage.Value, "tampered.docx", OfficeMimeType.Word, Sentinel),
        Req(SentinelFieldFaultDocx.Value, "fault.docx", OfficeMimeType.Word, Sentinel),
    };

    private static readonly Type?[] BatchExpected =
    {
        null, null, typeof(PasswordProtectedException), typeof(InvalidPasswordException), typeof(CorruptFileException), null,
    };

    private static void AssertBatchClean(ConfidentialityRig rig, IReadOnlyList<BatchExtractionResult> items)
    {
        Assert.Equal(BatchExpected.Length, items.Count);
        foreach (var item in items.OrderBy(i => i.Index))
        {
            var expected = BatchExpected[item.Index];
            if (expected is null)
            {
                Assert.True(item.Succeeded, $"item {item.Index}: {item.Error}");
            }
            else
            {
                Assert.NotNull(item.Error);
                Assert.Equal(expected, item.Error.GetType());
            }

            AssertBatchItemClean(item, Sentinel);
        }

        Assert.NotEmpty(items.Single(i => i.Index == 5).Result!.FieldErrors);
        Assert.True(rig.Sink.ActivityCount > 0);
        AssertLoggerClean(rig.Logger, Sentinel);
        rig.Sink.AssertClean(Sentinel);
        AssertNoSecretInGraph(Sentinel, nameof(MainExtractor), rig.Extractor);
        AssertNoSecretInStatics(Sentinel, typeof(MainExtractor).Assembly);
        AssertHssfSlotCleared(); // the test thread's slot; worker threads' slots are not reachable from here
    }

    // [S-pw-confidential#1][S-pw-confidential#2][S-pw-confidential#3]
    [Fact]
    public async Task ExtractManyAsync_MixedOutcomes_CarryNoSecret()
    {
        using var rig = new ConfidentialityRig();

        var items = await rig.Extractor.ExtractManyAsync(BatchRequests(), 2);

        AssertBatchClean(rig, items);
    }

    // [S-pw-confidential#1][S-pw-confidential#2][S-pw-confidential#3]
    [Fact]
    public async Task ExtractManyStreamingAsync_MixedOutcomes_CarryNoSecret()
    {
        using var rig = new ConfidentialityRig();
        var items = new List<BatchExtractionResult>();

        await foreach (var item in rig.Extractor.ExtractManyStreamingAsync(BatchRequests(), 2))
        {
            items.Add(item);
        }

        AssertBatchClean(rig, items);
    }

    // ---- negative controls: each assertion must fail when the secret is present ----

    private sealed class Holder
    {
        internal string? Text;
        internal byte[]? Utf16Bytes;
        internal Holder? Next;
    }

    private static class StaticHolder
    {
        internal static readonly string Leak = "x-" + Sentinel;
    }

    // [S-pw-confidential#1]
    [Fact]
    public void Control_LoggerWithSecretInMessageOrState_Fails()
    {
        var inMessage = new CapturingLogger<PasswordConfidentialityTests>();
        inMessage.Log(LogLevel.Error, new EventId(1), "state", null, (_, _) => "pw=" + Sentinel);
        Assert.ThrowsAny<XunitException>(() => AssertLoggerClean(inMessage, Sentinel));

        var inState = new CapturingLogger<PasswordConfidentialityTests>();
        inState.Log(LogLevel.Error, new EventId(1), new List<KeyValuePair<string, object?>> { new("password", Sentinel) }, null, (_, _) => "clean");
        Assert.ThrowsAny<XunitException>(() => AssertLoggerClean(inState, Sentinel));
    }

    // [S-pw-confidential#1]
    [Fact]
    public void Control_ExceptionWithSecretAnywhere_Fails()
    {
        Assert.ThrowsAny<XunitException>(() => AssertExceptionClean(new InvalidOperationException("bad " + Sentinel), Sentinel));
        Assert.ThrowsAny<XunitException>(() => AssertExceptionClean(new InvalidOperationException("outer", new ArgumentException(Sentinel)), Sentinel));

        var withData = new InvalidOperationException("clean");
        withData.Data["k"] = Sentinel;
        Assert.ThrowsAny<XunitException>(() => AssertExceptionClean(withData, Sentinel));

        var aggregate = new AggregateException(new InvalidOperationException("clean"), new ArgumentException(Sentinel));
        Assert.ThrowsAny<XunitException>(() => AssertExceptionClean(aggregate, Sentinel));
    }

    // [S-pw-confidential#2]
    [Fact]
    public void Control_ResultOrBatchWithSecret_Fails()
    {
        var result = new DocumentExtractionResult { ExtractedText = "text " + Sentinel };
        Assert.ThrowsAny<XunitException>(() => AssertResultClean(result, Sentinel));

        var faulty = new DocumentExtractionResult();
        faulty.FieldErrors.Add(new FieldError("Metadata", FieldErrorCode.CorruptFile, Sentinel));
        Assert.ThrowsAny<XunitException>(() => AssertResultClean(faulty, Sentinel));

        // base64 hides this in JSON; only the raw byte[] graph walk can see it.
        var embedded = new DocumentExtractionResult();
        embedded.EmbeddedFiles.Add(new EmbeddedFileItem { BinaryData = Encoding.Unicode.GetBytes("x" + Sentinel) });
        Assert.ThrowsAny<XunitException>(() => AssertResultClean(embedded, Sentinel));

        var item = new BatchExtractionResult { Index = 0, Error = new InvalidOperationException(Sentinel) };
        Assert.ThrowsAny<XunitException>(() => AssertBatchItemClean(item, Sentinel));

        var detection = new MimeDetectionResult { MimeType = Sentinel };
        Assert.ThrowsAny<XunitException>(() => AssertDetectionClean(detection, Sentinel));
    }

    // [S-pw-confidential#3]
    [Fact]
    public void Control_RetainedSecretInGraphOrStatics_Fails()
    {
        Assert.ThrowsAny<XunitException>(() => AssertNoSecretInGraph(Sentinel, "h", new Holder { Text = Sentinel }));
        Assert.ThrowsAny<XunitException>(() => AssertNoSecretInGraph(Sentinel, "h", new Holder { Utf16Bytes = Encoding.Unicode.GetBytes("a" + Sentinel) }));
        Assert.ThrowsAny<XunitException>(() => AssertNoSecretInGraph(Sentinel, "h", new Holder { Next = new Holder { Next = new Holder { Text = Sentinel } } }));
        Assert.ThrowsAny<XunitException>(() => AssertNoSecretInStatics(Sentinel, typeof(StaticHolder).Assembly));

        var cycle = new Holder();
        cycle.Next = cycle;
        AssertNoSecretInGraph(Sentinel, "cycle", cycle);

        // An exhausted depth budget is a failure, not a silent pass.
        var deep = new Holder { Next = new Holder { Next = new Holder { Next = new Holder { Text = Sentinel } } } };
        Assert.ThrowsAny<XunitException>(() => AssertNoSecretInGraph(Sentinel, "deep", deep, maxDepth: 1));
    }

    // [S-pw-confidential#3] The NPOI slot is [ThreadStatic], so setting it here cannot disturb tests on other threads.
    [Fact]
    public void Control_HssfSlotHoldingSecret_Fails()
    {
        Biff8EncryptionKey.CurrentUserPassword = Sentinel;
        try
        {
            Assert.ThrowsAny<XunitException>(AssertHssfSlotCleared);
        }
        finally
        {
            Biff8EncryptionKey.CurrentUserPassword = null;
        }
    }

    // [S-pw-confidential#1] Private source/meter names: nothing here reaches the library's own telemetry or other tests' sinks.
    [Fact]
    public void Control_TelemetryWithSecretInTagEventOrMetric_Fails()
    {
        var sourceName = "ExtractorOLE.Control." + Guid.NewGuid().ToString("N");
        var meterName = sourceName + ".meter";
        using var source = new ActivitySource(sourceName);
        using var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("control.counter");

        using (var tagSink = new TelemetrySink(sourceName, meterName))
        {
            using (var activity = source.StartActivity("control"))
            {
                Assert.NotNull(activity);
                activity.SetTag("password", Sentinel);
            }

            Assert.ThrowsAny<XunitException>(() => tagSink.AssertClean(Sentinel));
        }

        using (var eventSink = new TelemetrySink(sourceName, meterName))
        {
            using (var activity = source.StartActivity("control"))
            {
                activity!.AddException(new InvalidOperationException("bad " + Sentinel));
            }

            Assert.ThrowsAny<XunitException>(() => eventSink.AssertClean(Sentinel));
        }

        using (var metricSink = new TelemetrySink(sourceName, meterName))
        {
            counter.Add(1, new KeyValuePair<string, object?>("pw", Sentinel));
            Assert.ThrowsAny<XunitException>(() => metricSink.AssertClean(Sentinel));
        }
    }
}

using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.Record.Crypto;
using NPOI.SS.UserModel;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    /// <summary>
    /// #380: encrypted xls (RC4, CryptoAPI, default key) through the thread-static
    /// Biff8EncryptionKey slot, set and cleared around the synchronous HSSF open.
    /// </summary>
    public class XlsPasswordDecryptionTests
    {
        private const string XlsMime = "application/vnd.ms-excel";
        private const string Rc4Xls = "rc4-password.xls";
        private const string CryptoApiXls = "cryptoapi-encrypted-properties.xls";
        private const string DefaultKeyXls = "default-key.xls";
        private const string XorAbcXls = "xor-encryption-abc.xls";
        private const string XorLongXls = "xor_password_123456789012345.xls";
        private static readonly double[] ExpectedAbcCells = { 1d, 2d, 3d };
        private const string WrongPassword = "not-the-password";

        private static readonly Lazy<MainExtractor> Extractor = new(() =>
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        });

        private static EncryptedSamples.EncryptedSample Sample(string fileName) =>
            EncryptedSamples.Enumerate().Single(s => string.Equals(Path.GetFileName(s.Path), fileName, StringComparison.Ordinal));

        private static byte[] TwinBytes(EncryptedSamples.EncryptedSample sample)
        {
            var repoRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(sample.FullPath)))!;
            return File.ReadAllBytes(Path.Combine(repoRoot, sample.UnencryptedTwin!));
        }

        private static ExtractionRequest Request(byte[] bytes, string? password) =>
            new() { FileBytes = bytes, FileName = "fixture.xls", DetectedMimeType = XlsMime, Password = password };

        private static string Json(object? value) => JsonSerializer.Serialize(value);

        [Fact]
        public void Extract_Rc4WithPassword_EqualsTwin()
        {
            var sample = Sample(Rc4Xls);

            var decrypted = Extractor.Value.Extract(Request(File.ReadAllBytes(sample.FullPath), sample.Password));
            var twin = Extractor.Value.Extract(Request(TwinBytes(sample), null));

            Assert.Equal(twin.ExtractedText, decrypted.ExtractedText);
            Assert.Equal(Json(twin.Metadata), Json(decrypted.Metadata));
            Assert.Equal(Json(twin.FormatMetadata), Json(decrypted.FormatMetadata));
            Assert.Empty(decrypted.EmbeddedFiles);
            Assert.Empty(decrypted.FieldErrors);
            Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
        }

        [Fact]
        public void Extract_CryptoApiWithPassword_EqualsTwinExceptEncryptedProperties()
        {
            // Encrypted document properties are not read here (T-xls-encprops), so general
            // metadata stays empty; everything else equals the twin.
            var sample = Sample(CryptoApiXls);

            var decrypted = Extractor.Value.Extract(Request(File.ReadAllBytes(sample.FullPath), sample.Password));
            var twin = Extractor.Value.Extract(Request(TwinBytes(sample), null));

            Assert.Equal(twin.ExtractedText, decrypted.ExtractedText);
            Assert.Equal(Json(twin.FormatMetadata), Json(decrypted.FormatMetadata));
            Assert.Equal(Json(new FileMetadata()), Json(decrypted.Metadata));
            Assert.Empty(decrypted.EmbeddedFiles);
            Assert.Empty(decrypted.FieldErrors);
        }

        [Theory]
        [InlineData(Rc4Xls)]
        [InlineData(CryptoApiXls)]
        [InlineData(XorAbcXls)]
        [InlineData(XorLongXls)]
        public void Extract_NoPassword_ThrowsPasswordProtected(string fileName)
        {
            var bytes = File.ReadAllBytes(Sample(fileName).FullPath);

            Assert.Throws<PasswordProtectedException>(() => Extractor.Value.Extract(Request(bytes, null)));
            Assert.Throws<PasswordProtectedException>(() => Extractor.Value.Extract(Request(bytes, string.Empty)));
            Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
        }

        [Theory]
        [InlineData(Rc4Xls)]
        [InlineData(CryptoApiXls)]
        [InlineData(XorAbcXls)]
        [InlineData(XorLongXls)]
        public void Extract_WrongPassword_ThrowsInvalidPassword(string fileName)
        {
            var bytes = File.ReadAllBytes(Sample(fileName).FullPath);

            var ex = Assert.Throws<InvalidPasswordException>(() => Extractor.Value.Extract(Request(bytes, WrongPassword)));
            Assert.DoesNotContain(WrongPassword, ex.ToString(), StringComparison.Ordinal);
            Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
        }

        // #415: XOR obfuscation (FILEPASS key/verifier) opens since the npoi#197 re-pin. The xls text
        // extractor emits string cells only and these fixtures hold numbers, so the sheet data is
        // asserted through the workbook the same Load path produces, and Extract must succeed cleanly.
        [Theory]
        [InlineData(XorAbcXls, "Sheet1", 3, 3)]
        [InlineData(XorLongXls, "420", 420, 10920)]
        public void Extract_XorWithPassword_OpensAndSheetDataIsDecrypted(string fileName, string sheetName, int rows, int nonEmptyCells)
        {
            var sample = Sample(fileName);
            var bytes = File.ReadAllBytes(sample.FullPath);

            var result = Extractor.Value.Extract(Request(bytes, sample.Password));

            var format = Assert.IsType<ExcelFormatMetadata>(result.FormatMetadata);
            Assert.Equal(1, format.SheetCount);
            Assert.Equal(sheetName, Assert.Single(format.SheetNames));
            Assert.Empty(result.FieldErrors);

            using var stream = new MemoryStream(bytes);
            var sheet = XlsWorkbooks.Load(stream, sample.Password).GetSheetAt(0);
            Assert.Equal(rows - 1, sheet.LastRowNum);
            var actualNonEmpty = sheet.Cast<IRow>().Sum(r => r.Cast<ICell>().Count(c => c.CellType != CellType.Blank));
            Assert.Equal(nonEmptyCells, actualNonEmpty);
            Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
        }

        [Fact]
        public void Extract_XorAbcWithPassword_CellsAreOneTwoThree()
        {
            var sample = Sample(XorAbcXls);
            using var stream = new MemoryStream(File.ReadAllBytes(sample.FullPath));

            var sheet = XlsWorkbooks.Load(stream, sample.Password).GetSheetAt(0);

            Assert.Equal(ExpectedAbcCells, Enumerable.Range(0, 3).Select(i => sheet.GetRow(i).GetCell(0).NumericCellValue));
        }

        [Fact]
        public void Extract_StreamInputWithPassword_EqualsByteInput()
        {
            var sample = Sample(Rc4Xls);
            var bytes = File.ReadAllBytes(sample.FullPath);

            var fromBytes = Extractor.Value.Extract(Request(bytes, sample.Password));
            using var stream = new MemoryStream(bytes);
            var fromStream = Extractor.Value.Extract(new ExtractionRequest
            {
                InputStream = stream,
                FileName = "fixture.xls",
                DetectedMimeType = XlsMime,
                Password = sample.Password,
            });

            Assert.Equal(fromBytes.ExtractedText, fromStream.ExtractedText);
            Assert.Throws<InvalidPasswordException>(() => Extractor.Value.Extract(new ExtractionRequest
            {
                InputStream = new MemoryStream(bytes),
                FileName = "fixture.xls",
                DetectedMimeType = XlsMime,
                Password = WrongPassword,
            }));
        }

        [Fact]
        public void Extract_DefaultKey_IdenticalWithAndWithoutAPassword_ViaRetryWithDefaultKey()
        {
            var bytes = File.ReadAllBytes(Sample(DefaultKeyXls).FullPath);

            var withoutPassword = Extractor.Value.Extract(Request(bytes, null));
            var before = XlsWorkbooks.LoadCount;
            var withPassword = Extractor.Value.Extract(Request(bytes, "arbitrary"));
            var loads = XlsWorkbooks.LoadCount - before;

            // The supplied password is rejected (it replaces the default key), then the open is
            // retried once with the default key.
            Assert.Equal(2, loads);
            Assert.Equal(Json(withoutPassword), Json(withPassword));
            Assert.Contains("Encrypted workbook text line one", withPassword.ExtractedText);
            Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
        }

        [Fact]
        public void Extract_UnencryptedXlsWithPassword_IgnoresIt()
        {
            var bytes = TwinBytes(Sample(Rc4Xls));

            Assert.Equal(Json(Extractor.Value.Extract(Request(bytes, null))), Json(Extractor.Value.Extract(Request(bytes, "ignored"))));
        }

        private sealed class ThrowingStrategy : IOpenStrategy
        {
            private readonly Exception _ex;
            public ThrowingStrategy(Exception ex) => _ex = ex;
            public DocumentExtractionResult? Open(byte[] fileBytes) => throw _ex;
        }

        private static Exception OpenWith(Exception thrown, string? password)
        {
            using var input = ExtractionInput.From(new ExtractionRequest { FileBytes = new byte[] { 1 }, DetectedMimeType = XlsMime });
            return Assert.ThrowsAny<Exception>(() => ParseTimeErrorGuard.OpenOrThrow(new ThrowingStrategy(thrown), input, XlsMime, password));
        }

        [Fact]
        public void OpenOrThrow_TopLevelEncryptedDocumentException_MapsByPasswordPresence()
        {
            Assert.IsType<PasswordProtectedException>(OpenWith(new NPOI.EncryptedDocumentException("x"), null));
            Assert.IsType<PasswordProtectedException>(OpenWith(new NPOI.EncryptedDocumentException("x"), string.Empty));
            Assert.IsType<InvalidPasswordException>(OpenWith(new NPOI.EncryptedDocumentException("x"), "secret"));
        }

        [Fact]
        public void OpenOrThrow_RecordFormatExceptionWrappingEncryptedDocumentException_IsCorruptFileNeverInvalidPassword()
        {
            var unsupported = new NPOI.Util.RecordFormatException("scheme", new NPOI.EncryptedDocumentException("inner"));

            Assert.IsType<CorruptFileException>(OpenWith(unsupported, "secret"));
            Assert.IsType<CorruptFileException>(OpenWith(unsupported, null));
        }

        [Fact]
        public async Task ExtractManyAsync_MixedPasswordsBarrierGated_EachItemGetsItsOwnOutcome()
        {
            var rc4 = Sample(Rc4Xls);
            var cryptoApi = Sample(CryptoApiXls);
            var rc4Bytes = await File.ReadAllBytesAsync(rc4.FullPath);
            var cryptoApiBytes = await File.ReadAllBytesAsync(cryptoApi.FullPath);
            var defaultKeyBytes = await File.ReadAllBytesAsync(Sample(DefaultKeyXls).FullPath);
            var twinBytes = TwinBytes(rc4);

            var cases = new (byte[] Bytes, string? Password, Type? Error)[]
            {
                (rc4Bytes, rc4.Password, null),
                (rc4Bytes, WrongPassword, typeof(InvalidPasswordException)),
                (rc4Bytes, null, typeof(PasswordProtectedException)),
                (cryptoApiBytes, cryptoApi.Password, null),
                (cryptoApiBytes, rc4.Password, typeof(InvalidPasswordException)),
                (defaultKeyBytes, "arbitrary", null),
                (defaultKeyBytes, null, null),
                (twinBytes, cryptoApi.Password, null),
            };

            // Every item blocks on its first read until all are in flight, so they overlap on the pool.
            using var barrier = new Barrier(cases.Length);
            var requests = cases.Select(c => new ExtractionRequest
            {
                InputStream = new BarrierStream(c.Bytes, barrier),
                FileName = "fixture.xls",
                DetectedMimeType = XlsMime,
                Password = c.Password,
            }).ToList();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var results = await Extractor.Value.ExtractManyAsync(requests, cases.Length, timeout.Token);

            for (var i = 0; i < cases.Length; i++)
            {
                if (cases[i].Error is { } expected)
                {
                    Assert.IsType(expected, results[i].Error);
                }
                else
                {
                    Assert.Null(results[i].Error);
                    Assert.Contains("Encrypted workbook text line one", results[i].Result!.ExtractedText);
                }
            }
        }

        // Seekable stream whose first read waits (bounded) until every batch item has started.
        private sealed class BarrierStream : MemoryStream
        {
            private readonly Barrier _barrier;
            private bool _released;

            public BarrierStream(byte[] bytes, Barrier barrier) : base(bytes, writable: false) => _barrier = barrier;

            public override int Read(byte[] buffer, int offset, int count)
            {
                Release();
                return base.Read(buffer, offset, count);
            }

            public override int Read(Span<byte> buffer)
            {
                Release();
                return base.Read(buffer);
            }

            private void Release()
            {
                if (_released) return;
                _released = true;
                Assert.True(_barrier.SignalAndWait(TimeSpan.FromSeconds(30)), "batch items did not overlap");
            }
        }
    }
}

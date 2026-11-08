using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>Task #171: fallback picks the OOXML flavor from [Content_Types].xml, one open per package.</summary>
    public class ZipFallbackOpenStrategyTests
    {
        private static ZipFallbackOpenStrategy Build()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<ZipFallbackOpenStrategy>();
        }

        [Fact]
        public void Pptx_OpensOnce()
        {
            var bytes = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content;
            var before = OpenXmlPackages.OpenCount;
            var r = Build().Open(bytes);
            Assert.NotNull(r);
            Assert.Equal(1, OpenXmlPackages.OpenCount - before);
            (r!.ParsedDocument as System.IDisposable)?.Dispose();
        }

        [Fact]
        public void Xlsx_OpensOnce()
        {
            var bytes = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content;
            var before = OpenXmlPackages.OpenCount;
            var r = Build().Open(bytes);
            Assert.NotNull(r);
            Assert.Equal(1, OpenXmlPackages.OpenCount - before);
            (r!.ParsedDocument as System.IDisposable)?.Dispose();
        }

        [Fact]
        public void Docx_OpensOnce()
        {
            var bytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content;
            var before = OpenXmlPackages.OpenCount;
            var r = Build().Open(bytes);
            Assert.NotNull(r);
            Assert.Equal(1, OpenXmlPackages.OpenCount - before);
            (r!.ParsedDocument as System.IDisposable)?.Dispose();
        }

        private static byte[] RewriteContentTypes(byte[] pkg, Func<string, string> edit)
        {
            using var src = new MemoryStream(pkg);
            using var dst = new MemoryStream();
            using (var inZip = new ZipArchive(src, ZipArchiveMode.Read))
            using (var outZip = new ZipArchive(dst, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var e in inZip.Entries)
                {
                    using var es = e.Open();
                    using var ms = new MemoryStream();
                    es.CopyTo(ms);
                    var data = ms.ToArray();
                    if (e.FullName == "[Content_Types].xml")
                        data = Encoding.UTF8.GetBytes(edit(Encoding.UTF8.GetString(data)));
                    var ne = outZip.CreateEntry(e.FullName);
                    using var os = ne.Open();
                    os.Write(data, 0, data.Length);
                }
            }
            return dst.ToArray();
        }

        private static string DoctypeBomb(string xml)
        {
            var decl = xml.StartsWith("<?xml", StringComparison.Ordinal) ? xml[..(xml.IndexOf("?>", StringComparison.Ordinal) + 2)] : "";
            return decl + "<!DOCTYPE Types [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;\">]>" + xml[decl.Length..];
        }

        [Fact]
        public void ContentTypes_Valid_DetectsFlavor()
        {
            var bytes = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content;
            Assert.Equal(ZipFallbackOpenStrategy.Flavor.PowerPoint, ZipFallbackOpenStrategy.DetectFlavor(new MemoryStream(bytes)));
        }

        [Fact]
        public void ContentTypesWithDtd_NotParsed()
        {
            var bytes = RewriteContentTypes(new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content, DoctypeBomb);
            Assert.Null(ZipFallbackOpenStrategy.DetectFlavor(new MemoryStream(bytes)));
        }

        [Fact]
        public void OversizedContentTypes_NotParsed()
        {
            var bytes = RewriteContentTypes(new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content,
                x => x + "<!--" + new string('a', 2 * 1024 * 1024) + "-->");
            Assert.Null(ZipFallbackOpenStrategy.DetectFlavor(new MemoryStream(bytes)));
        }
    }
}

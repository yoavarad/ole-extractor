using System.Text;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Agile;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;

namespace ExtractorOLE.Tests.Encryption;

public class AgileDescriptorReaderTests
{
    [Fact]
    public void LoadEncryptionElement_SampleDescriptor_ReturnsEncryptionRoot()
    {
        var (descriptor, _) = ReadStreams(AgilePptx);

        var root = AgileDescriptorReader.LoadEncryptionElement(new MemoryStream(descriptor));

        Assert.Equal(EncryptionNs + "encryption", root.Name);
    }

    [Fact]
    public void LoadEncryptionElement_MalformedXml_ThrowsCorruptFile()
    {
        Assert.Throws<CorruptFileException>(() => AgileDescriptorReader.LoadEncryptionElement(new MemoryStream("<encryption"u8.ToArray())));
    }

    [Fact]
    public void LoadEncryptionElement_Dtd_ThrowsCorruptFile()
    {
        var xml = "<!DOCTYPE encryption [<!ENTITY x \"y\">]><encryption xmlns=\"http://schemas.microsoft.com/office/2006/encryption\">&x;</encryption>";

        Assert.Throws<CorruptFileException>(() => AgileDescriptorReader.LoadEncryptionElement(new MemoryStream(Encoding.UTF8.GetBytes(xml))));
    }

    [Fact]
    public void LoadEncryptionElement_WrongRoot_ThrowsCorruptFile()
    {
        Assert.Throws<CorruptFileException>(() => AgileDescriptorReader.LoadEncryptionElement(new MemoryStream("<other/>"u8.ToArray())));
    }
}

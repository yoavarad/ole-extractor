// THROWAWAY spike (task 371): not to be merged. Proves crypto primitives + NPOI decryptors run on the CI OS matrix.
using System.Security.Cryptography;
using NPOI;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.FileSystem;
using NPOI.Util;
using Xunit;
using Xunit.Abstractions;

namespace ExtractorOLE.Tests.Spike;

public class CryptoMatrixSpikeTests(ITestOutputHelper output)
{
    [Fact]
    public void SystemSecurityCryptography_AesCbc_Sha512_HmacSha512()
    {
        byte[] key = new byte[32], iv = new byte[16], data = new byte[64];
        RandomNumberGenerator.Fill(key); RandomNumberGenerator.Fill(iv); RandomNumberGenerator.Fill(data);
        using var aes = Aes.Create();
        aes.Key = key; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
        var ct = aes.EncryptCbc(data, iv, PaddingMode.None);
        Assert.Equal(data, aes.DecryptCbc(ct, iv, PaddingMode.None));
        Assert.Equal(64, SHA512.HashData(data).Length);
        Assert.Equal(64, HMACSHA512.HashData(key, data).Length);
        output.WriteLine($"OS={Environment.OSVersion} ok");
    }

    [Fact]
    public void Npoi_Main_HasNoAgile()
    {
        using var fs = new POIFSFileSystem();
        Assert.Throws<EncryptedDocumentException>(() => new EncryptionInfo(fs, EncryptionMode.Agile));
    }

    [Fact]
    public void Npoi_Standard_Encrypt_Then_Decrypt_RoundTrip()
    {
        const string modeName = "Standard";
        byte[] plain = new byte[5000];
        new Random(7).NextBytes(plain);
        using var fs = new POIFSFileSystem();
        var info = new EncryptionInfo(fs, EncryptionMode.Standard);
        var enc = info.Encryptor;
        enc.ConfirmPassword("pw");
        using (var os = enc.GetDataStream(fs)) { os.Write(plain, 0, plain.Length); os.Close(); }
        using var ms = new MemoryStream();
        fs.WriteFileSystem(ms);
        ms.Position = 0;
        using var fs2 = new POIFSFileSystem(ms);
        var info2 = new EncryptionInfo(fs2);
        var dec = Decryptor.GetInstance(info2);
        Assert.True(dec.VerifyPassword("pw"));
        Assert.False(Decryptor.GetInstance(new EncryptionInfo(fs2)).VerifyPassword("wrong"));
        using var s = dec.GetDataStream(fs2);
        var got = new byte[plain.Length];
        int n = 0, r;
        while (n < got.Length && (r = s.Read(got, n, got.Length - n)) > 0) n += r;
        Assert.Equal(plain, got);
        output.WriteLine($"{modeName} OK on {Environment.OSVersion}");
    }
}

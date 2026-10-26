using System.Security.Cryptography;

namespace RandevuSistemi.Services;

public static class SifreYardimcisi
{
    private const int TuzBoyutu = 16;
    private const int HashBoyutu = 32;
    private const int Iterasyon = 100_000;

    // Rastgele tuz üretir (metin olarak saklamak kolay olsun diye Base64)
    public static string TuzUret()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TuzBoyutu));

    public static string HashUret(string sifre, string tuz)
    {
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            sifre, Convert.FromBase64String(tuz),
            Iterasyon, HashAlgorithmName.SHA256, HashBoyutu);
        return Convert.ToBase64String(hash);
    }

    public static bool Dogrula(string sifre, string tuz, string beklenenHash)
    {
        byte[] hesaplanan = Convert.FromBase64String(HashUret(sifre, tuz));
        byte[] beklenen = Convert.FromBase64String(beklenenHash);
        return CryptographicOperations.FixedTimeEquals(hesaplanan, beklenen);
    }
}
using RandevuSistemi.Models;

namespace RandevuSistemi.Services;

public class KimlikServisi
{
    private readonly List<Kullanici> _kullanicilar = new();

    public IReadOnlyList<Kullanici> Kullanicilar => _kullanicilar;

    public void KullaniciKaydet(Kullanici kullanici)
    {
        ArgumentNullException.ThrowIfNull(kullanici);

        if (_kullanicilar.Any(k => k.Email == kullanici.Email))
            throw new InvalidOperationException("Bu e-posta ile kayıtlı bir kullanıcı zaten var.");

        if (kullanici is Hasta yeni &&
            _kullanicilar.OfType<Hasta>().Any(h => h.Tckn == yeni.Tckn))
            throw new InvalidOperationException("Bu TC kimlik numarası ile kayıtlı bir hasta zaten var.");

        _kullanicilar.Add(kullanici);
    }

    // Başarılıysa kullanıcıyı, değilse null döner
    public Kullanici? GirisYap(string email, string sifre)
    {
        var kullanici = _kullanicilar.FirstOrDefault(
            k => k.Email == email.Trim().ToLowerInvariant());

        return kullanici != null && kullanici.SifreDogrula(sifre) ? kullanici : null;
    }
}
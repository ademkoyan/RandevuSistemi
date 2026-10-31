using System.Net.Mail;
using RandevuSistemi.Services;

namespace RandevuSistemi.Models;

public abstract class Kullanici
{
    private string _ad = "";
    private string _soyad = "";
    private string _email = "";
    private string _sifreHash = "";
    private string _tuz = "";

    public Guid Id { get; }

    public string Ad
    {
        get => _ad;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Ad boş olamaz.");
            _ad = value.Trim();
        }
    }

    public string Soyad
    {
        get => _soyad;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Soyad boş olamaz.");
            _soyad = value.Trim();
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !MailAddress.TryCreate(value.Trim(), out _) ||
                !value.Contains('.'))
                throw new ArgumentException("Geçerli bir e-posta adresi giriniz.");
            _email = value.Trim().ToLowerInvariant();
        }
    }

    public string TamAd => $"{Ad} {Soyad}";

    // Yeni kullanıcı oluşturma: şifre hash'lenir
    protected Kullanici(string ad, string soyad, string email, string sifre)
    {
        Id = Guid.NewGuid();
        Ad = ad;
        Soyad = soyad;
        Email = email;
        SifreyiDegistir(sifre);
    }

    // Dosyadan yükleme: şifre yeniden hash'lenmez, kayıtlı hash ve tuz aynen alınır
    protected Kullanici(Guid id, string ad, string soyad, string email, string sifreHash, string tuz)
    {
        if (string.IsNullOrWhiteSpace(sifreHash) || string.IsNullOrWhiteSpace(tuz))
            throw new ArgumentException("Kayıtlı şifre bilgisi eksik.");

        Id = id;
        Ad = ad;
        Soyad = soyad;
        Email = email;
        _sifreHash = sifreHash;
        _tuz = tuz;
    }

    // internal: sadece bu proje (veri katmanı) okuyabilir, dışarıya açık değildir
    internal string SifreHash => _sifreHash;
    internal string Tuz => _tuz;

    public void SifreyiDegistir(string yeniSifre)
    {
        if (string.IsNullOrEmpty(yeniSifre) || yeniSifre.Length < 6)
            throw new ArgumentException("Şifre en az 6 karakter olmalıdır.");

        _tuz = SifreYardimcisi.TuzUret();
        _sifreHash = SifreYardimcisi.HashUret(yeniSifre, _tuz);
    }

    public bool SifreDogrula(string sifre)
        => SifreYardimcisi.Dogrula(sifre, _tuz, _sifreHash);

    public abstract string RolAdi { get; }
    public abstract IReadOnlyList<string> Yetkiler { get; }

    public override string ToString() => $"{TamAd} ({RolAdi}) - {Email}";
}
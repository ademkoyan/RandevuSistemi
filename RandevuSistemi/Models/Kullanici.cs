using System.Net.Mail;
using RandevuSistemi.Services;

namespace RandevuSistemi.Models;

public abstract class Kullanici
{
    private string _ad = "";
    private string _soyad = "";
    private string _email = "";
    private string _sifreHash = "";   // private: dışarıdan görülemez
    private string _tuz = "";

    public Guid Id { get; } = Guid.NewGuid();

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

    protected Kullanici(string ad, string soyad, string email, string sifre)
    {
        Ad = ad;
        Soyad = soyad;
        Email = email;
        SifreyiDegistir(sifre);
    }

    public void SifreyiDegistir(string yeniSifre)
    {
        if (string.IsNullOrEmpty(yeniSifre) || yeniSifre.Length < 6)
            throw new ArgumentException("Şifre en az 6 karakter olmalıdır.");

        _tuz = SifreYardimcisi.TuzUret();
        _sifreHash = SifreYardimcisi.HashUret(yeniSifre, _tuz);
    }

    public bool SifreDogrula(string sifre)
        => SifreYardimcisi.Dogrula(sifre, _tuz, _sifreHash);

    // Çok biçimlilik: her rol bunları kendine göre doldurur
    public abstract string RolAdi { get; }
    public abstract IReadOnlyList<string> Yetkiler { get; }

    public override string ToString() => $"{TamAd} ({RolAdi}) - {Email}";
}
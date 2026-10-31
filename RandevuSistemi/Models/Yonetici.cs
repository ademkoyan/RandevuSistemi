namespace RandevuSistemi.Models;

public class Yonetici : Kullanici
{
    public Kurum Kurum { get; }

    // Yeni yönetici oluşturma (şifre hash'lenir)
    public Yonetici(string ad, string soyad, string email, string sifre, Kurum kurum)
        : base(ad, soyad, email, sifre)
    {
        ArgumentNullException.ThrowIfNull(kurum);
        Kurum = kurum;
    }
    
    internal Yonetici(Guid id, string ad, string soyad, string email, string sifreHash, string tuz,
        Kurum kurum)
        : base(id, ad, soyad, email, sifreHash, tuz)
    {
        ArgumentNullException.ThrowIfNull(kurum);
        Kurum = kurum;
    }

    public override string RolAdi => "Yönetici";

    public override IReadOnlyList<string> Yetkiler => new[]
    {
        "Kurum / şube / bölüm / hizmet tanımlama",
        "Çalışan kaydetme",
        "Tüm randevuları görme ve yönetme"
    };
    
    
    // Dosyadan yükleme için

    
}
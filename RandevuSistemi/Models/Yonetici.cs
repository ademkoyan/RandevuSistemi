namespace RandevuSistemi.Models;

public class Yonetici : Kullanici
{
    public Kurum Kurum { get; }

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
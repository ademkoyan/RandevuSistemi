namespace RandevuSistemi.Models;

public class Yonetici : Kullanici
{
    public Kurum Kurum { get; }

    public Yonetici(string ad, string soyad, string email, string sifre, Kurum kurum)
        : base(ad, soyad, email, sifre)
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
}
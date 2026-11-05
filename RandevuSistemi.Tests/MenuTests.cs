using RandevuSistemi.Models;
using RandevuSistemi.Persistence;
using RandevuSistemi.UI;
using Xunit;

namespace RandevuSistemi.Tests;

public class MenuTests
{
    private readonly UygulamaVerisi _veri = OrnekVeri.Olustur();
    private readonly DepoServisi _depo = new(Path.Combine(Path.GetTempPath(), "menu-test-kullanilmaz.json"));

    [Fact]
    public void MenuFabrikasi_HastaIcinHastaMenusuUretir()
    {
        var hasta = _veri.KimlikServisi.Kullanicilar.OfType<Hasta>().First();
        Assert.IsType<HastaMenusu>(MenuFabrikasi.Olustur(hasta, _veri, _depo));
    }

    [Fact]
    public void MenuFabrikasi_DoktorIcinDoktorMenusuUretir()
    {
        var doktor = _veri.KimlikServisi.Kullanicilar.OfType<Doktor>().First();
        Assert.IsType<DoktorMenusu>(MenuFabrikasi.Olustur(doktor, _veri, _depo));
    }

    [Fact]
    public void MenuFabrikasi_YoneticiIcinYoneticiMenusuUretir()
    {
        var yonetici = _veri.KimlikServisi.Kullanicilar.OfType<Yonetici>().First();
        Assert.IsType<YoneticiMenusu>(MenuFabrikasi.Olustur(yonetici, _veri, _depo));
    }

    [Fact]
    public void RandevuMetni_TurkceGunVeDurumIcerir()
    {
        var o = new TestOrtami();
        var r = o.Servis.RandevuAl(o.Hasta1, o.Doktor1, o.Muayene, o.Merkez,
            TestOrtami.Pazartesi, new TimeOnly(10, 0));

        var metin = Konsol.RandevuMetni(r, hastayiGoster: true);

        Assert.Contains("Pazartesi", metin);
        Assert.Contains("10:00-10:20", metin);
        Assert.Contains("Onay bekliyor", metin);
        Assert.Contains("Ali Kaya", metin);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, "Pazartesi")]
    [InlineData(DayOfWeek.Wednesday, "Çarşamba")]
    [InlineData(DayOfWeek.Sunday, "Pazar")]
    public void GunAdi_TurkceDondurur(DayOfWeek gun, string beklenen)
        => Assert.Equal(beklenen, Konsol.GunAdi(gun));
}
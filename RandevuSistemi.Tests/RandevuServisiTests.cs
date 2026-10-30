using RandevuSistemi.Exceptions;
using RandevuSistemi.Models;
using RandevuSistemi.Services;
using Xunit;

namespace RandevuSistemi.Tests;

public class RandevuServisiTests
{
    private readonly TestOrtami o = new();

    private static TimeOnly Saat(int s, int d = 0) => new(s, d);

    // Kısayol: Pazartesi günü, Merkez şubede, genel muayene için randevu al
    private Randevu Al(Hasta hasta, Doktor doktor, TimeOnly saat)
        => o.Servis.RandevuAl(hasta, doktor, o.Muayene, o.Merkez, TestOrtami.Pazartesi, saat);

    private List<TimeOnly> Saatler(Doktor doktor, int adim = 30, Hasta? hasta = null, RandevuServisi? servis = null)
        => (servis ?? o.Servis).UygunSaatleriGetir(doktor, o.Muayene, o.Merkez, TestOrtami.Pazartesi, adim, hasta);

    // ===================== RANDEVU ALMA: MUTLU YOL =====================

    [Fact]
    public void RandevuAl_GecerliIstek_BeklemedeDurumundaOlusur()
    {
        var r = Al(o.Hasta1, o.Doktor1, Saat(10));

        Assert.Equal(RandevuDurumu.Beklemede, r.Durum);
        Assert.Equal(Saat(10), r.Baslangic);
        Assert.Equal(Saat(10, 20), r.Bitis);     // 20 dk'lık hizmet
        Assert.Single(o.Servis.Randevular);
    }

    [Fact]
    public void RandevuAl_BitisikRandevular_CakismazVeAlinabilir()
    {
        Al(o.Hasta1, o.Doktor1, Saat(10));                  // 10:00-10:20

        var sonra = Al(o.Hasta2, o.Doktor1, Saat(10, 20));  // 10:20-10:40 (hemen sonrası)
        var once = Al(o.Hasta2, o.Doktor1, Saat(9, 40));    // 09:40-10:00 (hemen öncesi)

        Assert.Equal(Saat(10, 20), sonra.Baslangic);
        Assert.Equal(Saat(9, 40), once.Baslangic);
        Assert.Equal(3, o.Servis.Randevular.Count);
    }

    [Fact]
    public void RandevuAl_UygunlukAraliginiTamDoldururabilir()
    {
        // 11:40 + 20 dk = 12:00: uygunluğun bitiş sınırına tam oturuyor
        var r = Al(o.Hasta1, o.Doktor1, Saat(11, 40));
        Assert.Equal(Saat(12), r.Bitis);
    }

    // ===================== ÇAKIŞMA KURALLARI =====================

    [Fact]
    public void RandevuAl_AyniDoktorAyniSaat_CakismaHatasiVerir()
    {
        Al(o.Hasta1, o.Doktor1, Saat(10));
        Assert.Throws<RandevuCakismasiException>(() => Al(o.Hasta2, o.Doktor1, Saat(10)));
    }

    [Fact]
    public void RandevuAl_KismenCakisanSaat_CakismaHatasiVerir()
    {
        Al(o.Hasta1, o.Doktor1, Saat(10));                  // 10:00-10:20
        Assert.Throws<RandevuCakismasiException>(() => Al(o.Hasta2, o.Doktor1, Saat(10, 10)));
        Assert.Throws<RandevuCakismasiException>(() => Al(o.Hasta2, o.Doktor1, Saat(9, 50)));
    }

    [Fact]
    public void RandevuAl_HastaAyniSaatteIkinciDoktordenAlamaz()
    {
        Al(o.Hasta1, o.Doktor1, Saat(10));
        Assert.Throws<RandevuCakismasiException>(() => Al(o.Hasta1, o.Doktor2, Saat(10, 10)));
    }

    [Fact]
    public void RandevuAl_IptalEdilenRandevununSlotuYenidenAlinabilir()
    {
        var r = Al(o.Hasta1, o.Doktor1, Saat(10));
        o.Servis.Iptal(r.Id, o.Hasta1);

        var yeni = Al(o.Hasta2, o.Doktor1, Saat(10));
        Assert.Equal(RandevuDurumu.Beklemede, yeni.Durum);
    }

    [Fact]
    public void RandevuAl_ReddedilenRandevununSlotuYenidenAlinabilir()
    {
        var r = Al(o.Hasta1, o.Doktor1, Saat(10));
        o.Servis.Reddet(r.Id, o.Doktor1);

        var yeni = Al(o.Hasta2, o.Doktor1, Saat(10));
        Assert.Equal(RandevuDurumu.Beklemede, yeni.Durum);
    }

    // ===================== ZAMAN VE HİZMET KURALLARI =====================

    [Fact]
    public void RandevuAl_DoktorunCalismadigiSaat_UygunsuzZamanHatasiVerir()
        => Assert.Throws<UygunsuzZamanException>(() => Al(o.Hasta1, o.Doktor1, Saat(15)));

    [Fact]
    public void RandevuAl_DoktorunCalismadigiGun_UygunsuzZamanHatasiVerir()
        => Assert.Throws<UygunsuzZamanException>(() =>
            o.Servis.RandevuAl(o.Hasta1, o.Doktor1, o.Muayene, o.Merkez,
                               TestOrtami.Pazartesi.AddDays(1), Saat(10)));   // Salı

    [Fact]
    public void RandevuAl_UygunlukAraliginiAsanRandevu_UygunsuzZamanHatasiVerir()
        // 11:50 + 20 dk = 12:10, doktor 12:00'de bitiriyor
        => Assert.Throws<UygunsuzZamanException>(() => Al(o.Hasta1, o.Doktor1, Saat(11, 50)));

    [Fact]
    public void RandevuAl_GecmisTarih_UygunsuzZamanHatasiVerir()
        => Assert.Throws<UygunsuzZamanException>(() =>
            o.Servis.RandevuAl(o.Hasta1, o.Doktor1, o.Muayene, o.Merkez,
                               new DateOnly(2029, 12, 31), Saat(10)));

    [Fact]
    public void RandevuAl_AyniGunGecmisSaat_UygunsuzZamanHatasiVerir()
    {
        // "Şimdi" = Pazartesi 10:05 olan bir servis
        var servis = new RandevuServisi(() => new DateTime(2030, 1, 7, 10, 5, 0));

        Assert.Throws<UygunsuzZamanException>(() =>
            servis.RandevuAl(o.Hasta1, o.Doktor1, o.Muayene, o.Merkez, TestOrtami.Pazartesi, Saat(10)));

        // 10:30 ise gelecekte, alınabilir
        var r = servis.RandevuAl(o.Hasta1, o.Doktor1, o.Muayene, o.Merkez, TestOrtami.Pazartesi, Saat(10, 30));
        Assert.Equal(Saat(10, 30), r.Baslangic);
    }

    [Fact]
    public void RandevuAl_DoktorunSunmadigiHizmet_HataVerir()
        => Assert.Throws<RandevuSistemiException>(() =>
            o.Servis.RandevuAl(o.Hasta1, o.Doktor1, o.Mr, o.Merkez, TestOrtami.Pazartesi, Saat(10)));

    // ===================== SNAPSHOT VE DURUM İŞLEMLERİ =====================

    [Fact]
    public void Randevu_HizmetFiyatiDegisinceEskiRandevuEtkilenmez()
    {
        var r = Al(o.Hasta1, o.Doktor1, Saat(10));

        o.Muayene.Ucret = 900m;     // yönetici fiyatı güncelledi

        Assert.Equal(500m, r.Ucret);
        Assert.Equal(900m, o.Muayene.Ucret);
    }

    [Fact]
    public void Onayla_DoktorOnaylayinca_DurumOnaylandiOlur()
    {
        var r = Al(o.Hasta1, o.Doktor1, Saat(10));
        o.Servis.Onayla(r.Id, o.Doktor1);
        Assert.Equal(RandevuDurumu.Onaylandi, r.Durum);
    }

    [Fact]
    public void Onayla_OlmayanRandevu_HataVerir()
        => Assert.Throws<RandevuSistemiException>(() => o.Servis.Onayla(Guid.NewGuid(), o.Doktor1));

    // ===================== BOŞ SAAT ÜRETİMİ =====================

    [Fact]
    public void UygunSaatler_RandevuYokken_TumSlotlariVerir()
    {
        var beklenen = new List<TimeOnly> { Saat(9), Saat(9, 30), Saat(10), Saat(10, 30), Saat(11), Saat(11, 30) };
        Assert.Equal(beklenen, Saatler(o.Doktor1));
    }

    [Fact]
    public void UygunSaatler_AlinanSlotListedenCikar()
    {
        Al(o.Hasta1, o.Doktor1, Saat(10));

        var saatler = Saatler(o.Doktor1);
        Assert.DoesNotContain(Saat(10), saatler);
        Assert.Contains(Saat(10, 30), saatler);      // 10:30-10:50, 10:00-10:20 ile çakışmaz
        Assert.Equal(5, saatler.Count);
    }

    [Fact]
    public void UygunSaatler_YarimSlotlukRandevuKomsuSlotuDaKapatir()
    {
        Al(o.Hasta1, o.Doktor1, Saat(10, 20));       // 10:20-10:40

        var beklenen = new List<TimeOnly> { Saat(9), Saat(9, 30), Saat(10), Saat(11), Saat(11, 30) };
        Assert.Equal(beklenen, Saatler(o.Doktor1));  // 10:00 kalır (bitişik), 10:30 gider (çakışır)
    }

    [Fact]
    public void UygunSaatler_IptaldenSonraSlotGeriGelir()
    {
        var r = Al(o.Hasta1, o.Doktor1, Saat(10));
        o.Servis.Iptal(r.Id, o.Hasta1);

        Assert.Contains(Saat(10), Saatler(o.Doktor1));
    }

    [Fact]
    public void UygunSaatler_GecmisSaatlerElenir()
    {
        var servis = new RandevuServisi(() => new DateTime(2030, 1, 7, 10, 5, 0));

        var beklenen = new List<TimeOnly> { Saat(10, 30), Saat(11), Saat(11, 30) };
        Assert.Equal(beklenen, Saatler(o.Doktor1, servis: servis));
    }

    [Fact]
    public void UygunSaatler_HastaVerilirseHastaninCakisanSaatleriElenir()
    {
        Al(o.Hasta1, o.Doktor2, Saat(10));           // Hasta1, Doktor2'de 10:00-10:20

        Assert.Contains(Saat(10), Saatler(o.Doktor1));                          // hasta verilmezse görünür
        Assert.DoesNotContain(Saat(10), Saatler(o.Doktor1, hasta: o.Hasta1));   // hasta verilirse elenir
        Assert.Contains(Saat(9, 30), Saatler(o.Doktor1, hasta: o.Hasta1));
    }

    [Fact]
    public void UygunSaatler_DoktorHizmetiSunmuyorsaBosListeDoner()
        => Assert.Empty(o.Servis.UygunSaatleriGetir(o.Doktor1, o.Mr, o.Merkez, TestOrtami.Pazartesi));

    [Fact]
    public void UygunSaatler_AdimSuresi15DkIse11SlotUretir()
        // 09:00'dan 11:30'a kadar her 15 dk: 11 başlangıç saati
        => Assert.Equal(11, Saatler(o.Doktor1, adim: 15).Count);
}
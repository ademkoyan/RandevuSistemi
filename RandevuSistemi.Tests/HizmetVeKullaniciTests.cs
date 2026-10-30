using RandevuSistemi.Models;
using RandevuSistemi.Services;
using Xunit;

namespace RandevuSistemi.Tests;

public class HizmetVeKullaniciTests
{
    private readonly TestOrtami o = new();

    // ===================== HİZMETLER (kapsülleme + çok biçimlilik) =====================

    [Fact]
    public void Hizmet_SifirVeyaNegatifSure_Reddedilir()
    {
        Assert.Throws<ArgumentException>(() => new GenelMuayene("Hatalı", 0, 100m));
        Assert.Throws<ArgumentException>(() => new GenelMuayene("Hatalı", -5, 100m));
    }

    [Fact]
    public void Hizmet_NegatifUcret_Reddedilir()
        => Assert.Throws<ArgumentException>(() => new GenelMuayene("Hatalı", 20, -1m));

    [Fact]
    public void Hizmet_BosAd_Reddedilir()
        => Assert.Throws<ArgumentException>(() => new GenelMuayene("  ", 20, 100m));

    [Fact]
    public void HazirlikTalimati_HerHizmetTuruKendineGoreCevapVerir()
    {
        // Aynı tür (SaglikHizmeti) üzerinden çağrılıyor → ÇOK BİÇİMLİLİK
        SaglikHizmeti muayene = new GenelMuayene("Muayene", 20, 500m);
        SaglikHizmeti mr = new RadyolojiIslemi("Beyin MR", 45, 2500m, "MR");
        SaglikHizmeti aclikTesti = new LaboratuvarTesti("Şeker", 10, 150m, true, 2);
        SaglikHizmeti kanSayimi = new LaboratuvarTesti("Kan Sayımı", 10, 200m, false, 4);

        Assert.Contains("hazırlık gerekmez", muayene.HazirlikTalimati());
        Assert.Contains("metal", mr.HazirlikTalimati());
        Assert.Contains("aç kalın", aclikTesti.HazirlikTalimati());
        Assert.Contains("gerekmez", kanSayimi.HazirlikTalimati());
    }

    // ===================== ŞUBE VE ÇALIŞMA SAATİ =====================

    [Fact]
    public void CalismaSaati_KapanisAcilistanOnceyse_Reddedilir()
        => Assert.Throws<ArgumentException>(() =>
            new CalismaSaati(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(8, 0)));

    [Fact]
    public void Sube_AcikMi_GunVeSaateGoreDogruCevaplar()
    {
        Assert.True(o.Merkez.AcikMi(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(9, 30)));
        Assert.False(o.Merkez.AcikMi(DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(9, 30)));
        Assert.False(o.Merkez.AcikMi(DayOfWeek.Monday, new TimeOnly(16, 45), new TimeOnly(17, 15)));
    }

    [Fact]
    public void Sube_AyniGunIcinIkinciCalismaSaati_Reddedilir()
        => Assert.Throws<InvalidOperationException>(() =>
            o.Merkez.CalismaSaatiEkle(new CalismaSaati(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))));

    // ===================== KULLANICI DOĞRULAMALARI =====================

    [Theory]
    [InlineData("10000000146", true)]
    [InlineData("11111111110", true)]
    [InlineData("12345678901", false)]     // sağlama basamakları tutmuyor
    [InlineData("01234567890", false)]     // 0 ile başlayamaz
    [InlineData("1234", false)]            // kısa
    [InlineData("abcdefghijk", false)]     // rakam değil
    [InlineData("", false)]
    public void Tckn_DogrulamaAlgoritmasi(string tckn, bool beklenen)
        => Assert.Equal(beklenen, Hasta.TcknGecerliMi(tckn));

    [Fact]
    public void Kullanici_GecersizEmail_Reddedilir()
        => Assert.Throws<ArgumentException>(() =>
            new Hasta("Ahmet", "Can", "bozuk-mail", "Sifre123", "11111111110", new DateOnly(1990, 1, 1)));

    [Fact]
    public void Kullanici_KisaSifre_Reddedilir()
        => Assert.Throws<ArgumentException>(() =>
            new Doktor("Zeynep", "Arslan", "zeynep@test.com", "123", "Kardiyoloji"));

    [Fact]
    public void Hasta_GelecekDogumTarihi_Reddedilir()
        => Assert.Throws<ArgumentException>(() =>
            new Hasta("Ahmet", "Can", "ahmet@test.com", "Sifre123", "11111111110",
                      DateOnly.FromDateTime(DateTime.Today.AddDays(1))));

    [Fact]
    public void Roller_FarkliRolAdiVeYetkiDoner()
    {
        Kullanici hasta = o.Hasta1;
        Kullanici doktor = o.Doktor1;
        Kullanici yonetici = o.Yonetici;

        Assert.Equal("Hasta", hasta.RolAdi);
        Assert.Equal("Doktor", doktor.RolAdi);
        Assert.Equal("Yönetici", yonetici.RolAdi);
        Assert.NotEqual(hasta.Yetkiler, yonetici.Yetkiler);
    }

    // ===================== ŞİFRE GÜVENLİĞİ =====================

    [Fact]
    public void Sifre_DogruVeYanlisSifreAyirtEdilir()
    {
        Assert.True(o.Hasta1.SifreDogrula("Hasta123"));
        Assert.False(o.Hasta1.SifreDogrula("yanlis123"));
    }

    [Fact]
    public void Sifre_DegistirilinceEskiSifreGecersizOlur()
    {
        o.Hasta1.SifreyiDegistir("YeniSifre1");

        Assert.True(o.Hasta1.SifreDogrula("YeniSifre1"));
        Assert.False(o.Hasta1.SifreDogrula("Hasta123"));
    }

    [Fact]
    public void SifreHash_FarkliTuzlarFarkliHashUretir()
    {
        var tuz1 = SifreYardimcisi.TuzUret();
        var tuz2 = SifreYardimcisi.TuzUret();

        var hash1 = SifreYardimcisi.HashUret("Sifre123", tuz1);
        var hash2 = SifreYardimcisi.HashUret("Sifre123", tuz2);

        Assert.NotEqual(tuz1, tuz2);
        Assert.NotEqual(hash1, hash2);                                       // aynı şifre, farklı hash
        Assert.Equal(hash1, SifreYardimcisi.HashUret("Sifre123", tuz1));     // aynı tuz, aynı hash
        Assert.NotEqual("Sifre123", hash1);                                  // düz metin değil
        Assert.True(SifreYardimcisi.Dogrula("Sifre123", tuz1, hash1));
        Assert.False(SifreYardimcisi.Dogrula("Yanlis1", tuz1, hash1));
    }

    // ===================== KİMLİK SERVİSİ =====================

    private KimlikServisi KimlikHazirla()
    {
        var kimlik = new KimlikServisi();
        kimlik.KullaniciKaydet(o.Hasta1);
        kimlik.KullaniciKaydet(o.Doktor1);
        return kimlik;
    }

    [Fact]
    public void GirisYap_DogruBilgiler_KullaniciyiDoner()
    {
        var kimlik = KimlikHazirla();
        var giris = kimlik.GirisYap("ali@test.com", "Hasta123");

        Assert.NotNull(giris);
        Assert.Same(o.Hasta1, giris);
    }

    [Fact]
    public void GirisYap_EmailBuyukKucukHarfeDuyarsiz()
        => Assert.NotNull(KimlikHazirla().GirisYap("  ALI@Test.com ", "Hasta123"));

    [Fact]
    public void GirisYap_YanlisSifreVeyaOlmayanEmail_NullDoner()
    {
        var kimlik = KimlikHazirla();

        Assert.Null(kimlik.GirisYap("ali@test.com", "yanlis"));
        Assert.Null(kimlik.GirisYap("yok@yok.com", "Hasta123"));
    }

    [Fact]
    public void KullaniciKaydet_AyniEmail_Reddedilir()
    {
        var kimlik = KimlikHazirla();
        var kopya = new Doktor("Başka", "Biri", "mehmet@test.com", "Sifre123", "KBB");

        Assert.Throws<InvalidOperationException>(() => kimlik.KullaniciKaydet(kopya));
    }

    [Fact]
    public void KullaniciKaydet_AyniTckn_Reddedilir()
    {
        var kimlik = KimlikHazirla();
        var kopya = new Hasta("Başka", "Biri", "baska@test.com", "Sifre123", "10000000146", new DateOnly(1980, 1, 1));

        Assert.Throws<InvalidOperationException>(() => kimlik.KullaniciKaydet(kopya));
    }
}
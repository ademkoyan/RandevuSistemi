using System.Text.Json;
using RandevuSistemi.Exceptions;
using RandevuSistemi.Models;
using RandevuSistemi.Persistence;
using Xunit;

namespace RandevuSistemi.Tests;

public class DepoServisiTests : IDisposable
{
    // Her test kendi geçici dosyasını kullanır; gerçek veri.json'a dokunmaz
    private readonly string _yol = Path.Combine(Path.GetTempPath(), $"randevu-test-{Guid.NewGuid():N}.json");

    private DepoServisi Depo() => new(_yol);

    public void Dispose()
    {
        foreach (var dosya in new[] { _yol, _yol + ".tmp" })
            if (File.Exists(dosya)) File.Delete(dosya);
    }

    private static DateOnly SonrakiPazartesi()
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);
        int fark = ((int)DayOfWeek.Monday - (int)bugun.DayOfWeek + 7) % 7;
        return bugun.AddDays(fark == 0 ? 7 : fark);
    }

    private static Doktor Mehmet(UygulamaVerisi v)
        => v.KimlikServisi.Kullanicilar.OfType<Doktor>().First(d => d.Email == "mehmet@sifa.com");

    private static Hasta HastaBul(UygulamaVerisi v, string email)
        => v.KimlikServisi.Kullanicilar.OfType<Hasta>().First(h => h.Email == email);

    private static SaglikHizmeti MerkezMuayene(UygulamaVerisi v)
        => v.Kurum.Subeler[0].Bolumler.First(b => b.Ad == "Dahiliye").Hizmetler[0];

    // ===================== İLK AÇILIŞ =====================

    [Fact]
    public void Yukle_DosyaYoksa_OrnekVeriOlusturupDosyayaYazar()
    {
        var veri = Depo().Yukle();

        Assert.True(File.Exists(_yol));
        Assert.Equal(2, veri.Kurum.Subeler.Count);
        Assert.NotEmpty(veri.KimlikServisi.Kullanicilar);
        Assert.Empty(veri.RandevuServisi.Randevular);
    }

    // ===================== KULLANICILAR VE ŞİFRELER =====================

    [Fact]
    public void KaydetYukle_KullanicilarVeSifreleriKorunur()
    {
        var ilk = Depo().Yukle();
        var yuklenen = Depo().Yukle();           // bu kez dosyadan okur

        Assert.Equal(
            ilk.KimlikServisi.Kullanicilar.Select(k => k.Id).OrderBy(x => x),
            yuklenen.KimlikServisi.Kullanicilar.Select(k => k.Id).OrderBy(x => x));

        // Hash dosyadan aynen geldiği için eski şifre hâlâ geçerli
        Assert.NotNull(yuklenen.KimlikServisi.GirisYap("ali@mail.com", "Hasta123"));
        Assert.NotNull(yuklenen.KimlikServisi.GirisYap("ayse@sifa.com", "Yonetici123"));
        Assert.Null(yuklenen.KimlikServisi.GirisYap("ali@mail.com", "yanlis"));
    }

    [Fact]
    public void Kaydet_DosyadaDuzMetinSifreBulunmaz()
    {
        Depo().Yukle();
        var json = File.ReadAllText(_yol);

        Assert.DoesNotContain("Hasta123", json);
        Assert.DoesNotContain("Doktor123", json);
        Assert.DoesNotContain("Yonetici123", json);
        Assert.Contains("SifreHash", json);
    }

    [Fact]
    public void KaydetYukle_DegisenSifreKalicidir()
    {
        var depo = Depo();
        var veri = depo.Yukle();
        HastaBul(veri, "ali@mail.com").SifreyiDegistir("YeniSifre1");
        depo.Kaydet(veri);

        var yuklenen = Depo().Yukle();

        Assert.NotNull(yuklenen.KimlikServisi.GirisYap("ali@mail.com", "YeniSifre1"));
        Assert.Null(yuklenen.KimlikServisi.GirisYap("ali@mail.com", "Hasta123"));
    }

    // ===================== DOKTOR, HİZMET, UYGUNLUK =====================

    [Fact]
    public void KaydetYukle_DoktorunHizmetVeUygunluklariKorunur()
    {
        var ilk = Depo().Yukle();
        var yuklenen = Depo().Yukle();

        var once = Mehmet(ilk);
        var sonra = Mehmet(yuklenen);

        Assert.Equal(once.Uzmanlik, sonra.Uzmanlik);
        Assert.Equal(2, sonra.Hizmetler.Count);
        Assert.Equal(2, sonra.Uygunluklar.Count);     // Merkez Pzt + Kadıköy Çar (çok şubeli)

        // Referanslar: uygunluğun şubesi, yüklenen kurumdaki AYNI nesne olmalı
        Assert.All(sonra.Uygunluklar, u => Assert.Contains(u.Sube, yuklenen.Kurum.Subeler));
    }

    [Fact]
    public void KaydetYukle_HizmetTurleriVeOzellikleriKorunur()
    {
        var yuklenen = (Depo().Yukle(), Depo().Yukle()).Item2;
        var hizmetler = yuklenen.Kurum.Subeler[0].Bolumler.SelectMany(b => b.Hizmetler).ToList();

        var mr = Assert.Single(hizmetler.OfType<RadyolojiIslemi>(), h => h.Ad == "Beyin MR");
        Assert.Equal("MR", mr.Cihaz);

        var seker = Assert.Single(hizmetler.OfType<LaboratuvarTesti>(), h => h.Ad == "Açlık Kan Şekeri");
        Assert.True(seker.AclikGerekli);
        Assert.Equal(2, seker.SonucSuresiSaat);

        // Çok biçimlilik yüklemeden sonra da çalışıyor
        Assert.Contains("metal", mr.HazirlikTalimati());
    }

    // ===================== RANDEVULAR =====================

    [Fact]
    public void KaydetYukle_RandevuDurumuVeSnapshotKorunur()
    {
        var depo = Depo();
        var veri = depo.Yukle();
        var doktor = Mehmet(veri);
        var muayene = MerkezMuayene(veri);

        var r = veri.RandevuServisi.RandevuAl(HastaBul(veri, "ali@mail.com"), doktor, muayene,
                                              veri.Kurum.Subeler[0], SonrakiPazartesi(), new TimeOnly(10, 0));
        veri.RandevuServisi.Onayla(r.Id, doktor);

        muayene.Ucret = 900m;          // fiyat sonradan değişti
        depo.Kaydet(veri);

        var yuklenen = Depo().Yukle();
        var r2 = Assert.Single(yuklenen.RandevuServisi.Randevular);

        Assert.Equal(r.Id, r2.Id);
        Assert.Equal(RandevuDurumu.Onaylandi, r2.Durum);
        Assert.Equal("Dahiliye Muayenesi", r2.HizmetAdi);
        Assert.Equal(20, r2.SureDakika);
        Assert.Equal(500m, r2.Ucret);                       // snapshot: 900 değil
        Assert.Equal(new TimeOnly(10, 20), r2.Bitis);
        Assert.Equal(r.OlusturmaZamani, r2.OlusturmaZamani);
    }

    [Fact]
    public void Yukleme_SonrasiCakismaKontroluCalismayaDevamEder()
    {
        var depo = Depo();
        var veri = depo.Yukle();
        var doktor = Mehmet(veri);
        var muayene = MerkezMuayene(veri);
        var tarih = SonrakiPazartesi();

        veri.RandevuServisi.RandevuAl(HastaBul(veri, "ali@mail.com"), doktor, muayene,
                                      veri.Kurum.Subeler[0], tarih, new TimeOnly(10, 0));
        depo.Kaydet(veri);

        // Yeniden yükle ve YÜKLENEN nesnelerle işlem yap
        var y = Depo().Yukle();
        var yDoktor = Mehmet(y);
        var yMuayene = MerkezMuayene(y);
        var yVeli = HastaBul(y, "veli@mail.com");
        var yMerkez = y.Kurum.Subeler[0];

        Assert.Throws<RandevuCakismasiException>(() =>
            y.RandevuServisi.RandevuAl(yVeli, yDoktor, yMuayene, yMerkez, tarih, new TimeOnly(10, 0)));

        var bitisik = y.RandevuServisi.RandevuAl(yVeli, yDoktor, yMuayene, yMerkez, tarih, new TimeOnly(10, 20));
        Assert.Equal(RandevuDurumu.Beklemede, bitisik.Durum);
    }

    [Fact]
    public void Yukleme_SonrasiIptalEdilmisRandevuSlotuTutmaz()
    {
        var depo = Depo();
        var veri = depo.Yukle();
        var doktor = Mehmet(veri);
        var ali = HastaBul(veri, "ali@mail.com");
        var tarih = SonrakiPazartesi();

        var r = veri.RandevuServisi.RandevuAl(ali, doktor, MerkezMuayene(veri),
                                              veri.Kurum.Subeler[0], tarih, new TimeOnly(10, 0));
        veri.RandevuServisi.Iptal(r.Id, ali);
        depo.Kaydet(veri);

        var y = Depo().Yukle();
        var yeni = y.RandevuServisi.RandevuAl(HastaBul(y, "veli@mail.com"), Mehmet(y), MerkezMuayene(y),
                                              y.Kurum.Subeler[0], tarih, new TimeOnly(10, 0));

        Assert.Equal(RandevuDurumu.Beklemede, yeni.Durum);
        Assert.Equal(RandevuDurumu.IptalEdildi,
                     y.RandevuServisi.Randevular.First(x => x.Id == r.Id).Durum);
    }

    // ===================== HATALI DOSYA =====================

    [Fact]
    public void Yukle_BozukJsonDosyasi_HataVerir()
    {
        File.WriteAllText(_yol, "{ bu gecerli bir json degil");
        Assert.ThrowsAny<JsonException>(() => Depo().Yukle());
    }

    [Fact]
    public void Yukle_BilinmeyenRolIceren_InvalidDataExceptionVerir()
    {
        Depo().Yukle();
        var json = File.ReadAllText(_yol).Replace("\"Rol\": \"Hasta\"", "\"Rol\": \"Hayalet\"");
        File.WriteAllText(_yol, json);

        Assert.Throws<InvalidDataException>(() => Depo().Yukle());
    }
}
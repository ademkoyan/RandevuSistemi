using RandevuSistemi.Exceptions;
using RandevuSistemi.Models;
using Xunit;

namespace RandevuSistemi.Tests;

public class RandevuDurumTests
{
    private readonly TestOrtami o = new();

    private Randevu YeniRandevu()
        => new(o.Hasta1, o.Doktor1, o.Muayene, o.Merkez, TestOrtami.Pazartesi, new TimeOnly(10, 0));

    // ===================== MUTLU YOLLAR =====================

    [Fact]
    public void YeniRandevu_BeklemedeVeAktiftir()
    {
        var r = YeniRandevu();
        Assert.Equal(RandevuDurumu.Beklemede, r.Durum);
        Assert.True(r.AktifMi);
    }

    [Fact]
    public void Onayla_ArdindanTamamla_DurumlarSirayladaDegisir()
    {
        var r = YeniRandevu();

        r.Onayla(o.Doktor1);
        Assert.Equal(RandevuDurumu.Onaylandi, r.Durum);

        r.Tamamla(o.Doktor1);
        Assert.Equal(RandevuDurumu.Tamamlandi, r.Durum);
        Assert.False(r.AktifMi);
    }

    [Fact]
    public void Yonetici_HerRandevuyuOnaylayipReddedebilir()
    {
        var r1 = YeniRandevu();
        var r2 = YeniRandevu();

        r1.Onayla(o.Yonetici);
        r2.Reddet(o.Yonetici);

        Assert.Equal(RandevuDurumu.Onaylandi, r1.Durum);
        Assert.Equal(RandevuDurumu.Reddedildi, r2.Durum);
    }

    [Fact]
    public void Hasta_KendiRandevusunuIptalEdebilir()
    {
        var r = YeniRandevu();
        r.Iptal(o.Hasta1);

        Assert.Equal(RandevuDurumu.IptalEdildi, r.Durum);
        Assert.False(r.AktifMi);
    }

    // ===================== YETKİ KURALLARI =====================

    [Fact]
    public void Hasta_RandevuyuOnaylayamaz()
        => Assert.Throws<YetkisizIslemException>(() => YeniRandevu().Onayla(o.Hasta1));

    [Fact]
    public void BaskaDoktor_RandevuyuOnaylayamaz()
        => Assert.Throws<YetkisizIslemException>(() => YeniRandevu().Onayla(o.Doktor2));

    [Fact]
    public void BaskaHasta_RandevuyuIptalEdemez()
        => Assert.Throws<YetkisizIslemException>(() => YeniRandevu().Iptal(o.Hasta2));

    // ===================== GEÇERSİZ GEÇİŞLER =====================

    [Fact]
    public void ReddedilmisRandevu_OnaylanamaZ()
    {
        var r = YeniRandevu();
        r.Reddet(o.Doktor1);

        var hata = Assert.Throws<GecersizDurumGecisiException>(() => r.Onayla(o.Doktor1));
        Assert.Equal(RandevuDurumu.Reddedildi, hata.MevcutDurum);
        Assert.Equal(RandevuDurumu.Onaylandi, hata.HedefDurum);
    }

    [Fact]
    public void TamamlanmisRandevu_IptalEdilemez()
    {
        var r = YeniRandevu();
        r.Onayla(o.Doktor1);
        r.Tamamla(o.Doktor1);

        Assert.Throws<GecersizDurumGecisiException>(() => r.Iptal(o.Hasta1));
    }

    [Fact]
    public void IptalEdilmisRandevu_TekrarOnaylanamaz()
    {
        var r = YeniRandevu();
        r.Iptal(o.Hasta1);

        Assert.Throws<GecersizDurumGecisiException>(() => r.Onayla(o.Doktor1));
    }

    [Fact]
    public void BeklemedekiRandevu_OnaysizTamamlanamaz()
        => Assert.Throws<GecersizDurumGecisiException>(() => YeniRandevu().Tamamla(o.Doktor1));

    // ===================== ÇAKIŞMA YARDIMCISI (Theory) =====================
    // Randevu: 10:00-10:20

    [Theory]
    [InlineData("10:10", "10:30", true)]    // sondan çakışır
    [InlineData("09:50", "10:10", true)]    // baştan çakışır
    [InlineData("09:00", "12:00", true)]    // tamamen kapsar
    [InlineData("10:05", "10:15", true)]    // içinde kalır
    [InlineData("10:20", "10:40", false)]   // hemen sonrası (bitişik)
    [InlineData("09:40", "10:00", false)]   // hemen öncesi (bitişik)
    [InlineData("11:00", "11:20", false)]   // uzakta
    public void Cakisiyor_AraliklariDogruHesaplar(string bas, string bit, bool beklenen)
    {
        var r = YeniRandevu();
        Assert.Equal(beklenen, r.Cakisiyor(TestOrtami.Pazartesi, TimeOnly.Parse(bas), TimeOnly.Parse(bit)));
    }

    [Fact]
    public void Cakisiyor_FarkliGunlerdeCakismaz()
        => Assert.False(YeniRandevu().Cakisiyor(TestOrtami.Pazartesi.AddDays(1),
                                                 new TimeOnly(10, 0), new TimeOnly(10, 20)));

    [Fact]
    public void Cakisiyor_IptalEdilmisRandevuSlotTutmaz()
    {
        var r = YeniRandevu();
        r.Iptal(o.Hasta1);

        Assert.False(r.Cakisiyor(TestOrtami.Pazartesi, new TimeOnly(10, 0), new TimeOnly(10, 20)));
    }
}
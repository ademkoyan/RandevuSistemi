using RandevuSistemi.Exceptions;
using RandevuSistemi.Models;
using Xunit;

namespace RandevuSistemi.Tests;

public class DoktorUygunlukTests
{
    private readonly TestOrtami o = new();

    private static TimeOnly Saat(int s, int d = 0) => new(s, d);

    [Fact]
    public void UygunlukEkle_CakisanAralik_Reddedilir()
    {
        // Doktor1 zaten Pzt 09:00-12:00 çalışıyor
        var yeni = new UygunlukAraligi(o.Merkez, DayOfWeek.Monday, Saat(11), Saat(13));
        Assert.Throws<InvalidOperationException>(() => o.Doktor1.UygunlukEkle(yeni));
    }

    [Fact]
    public void UygunlukEkle_BitisikAralik_Kabul​Edilir()
    {
        var yeni = new UygunlukAraligi(o.Merkez, DayOfWeek.Monday, Saat(12), Saat(14));
        o.Doktor1.UygunlukEkle(yeni);

        Assert.Equal(2, o.Doktor1.Uygunluklar.Count);
    }

    [Fact]
    public void UygunlukEkle_SubeninKapaliOlduguGun_Reddedilir()
    {
        var pazar = new UygunlukAraligi(o.Merkez, DayOfWeek.Sunday, Saat(9), Saat(12));
        Assert.Throws<InvalidOperationException>(() => o.Doktor1.UygunlukEkle(pazar));
    }

    [Fact]
    public void UygunlukEkle_SubeAcilisindanOnce_Reddedilir()
    {
        // Şube 08:00'da açılıyor
        var erken = new UygunlukAraligi(o.Merkez, DayOfWeek.Monday, Saat(7), Saat(9));
        Assert.Throws<InvalidOperationException>(() => o.Doktor1.UygunlukEkle(erken));
    }

    [Fact]
    public void UygunlukAraligi_BitisBaslangictanOnceyse_OlusmaZ()
        => Assert.Throws<ArgumentException>(() =>
            new UygunlukAraligi(o.Merkez, DayOfWeek.Monday, Saat(12), Saat(10)));

    [Fact]
    public void Doktor_IkiFarkliSubedeFarkliGunlerdeCalisabilir()
    {
        var kadikoy = TestOrtami.YeniSube("Kadıköy Şube");
        o.Doktor1.UygunlukEkle(new UygunlukAraligi(kadikoy, DayOfWeek.Wednesday, Saat(13), Saat(16)));

        var carsamba = new DateOnly(2030, 1, 9);   // 9 Ocak 2030 Çarşamba

        // Merkez'de çarşamba günü çalışmıyor
        Assert.Throws<UygunsuzZamanException>(() =>
            o.Servis.RandevuAl(o.Hasta1, o.Doktor1, o.Muayene, o.Merkez, carsamba, Saat(14)));

        // Kadıköy'de çalışıyor
        var r = o.Servis.RandevuAl(o.Hasta1, o.Doktor1, o.Muayene, kadikoy, carsamba, Saat(14));
        Assert.Same(kadikoy, r.Sube);
    }

    [Fact]
    public void UygunlukEkle_AyniGundeIkiSubedeCakisanAralik_Reddedilir()
    {
        // Doktor aynı anda iki şubede olamaz
        var kadikoy = TestOrtami.YeniSube("Kadıköy Şube");
        var cakisan = new UygunlukAraligi(kadikoy, DayOfWeek.Monday, Saat(10), Saat(11));

        Assert.Throws<InvalidOperationException>(() => o.Doktor1.UygunlukEkle(cakisan));
    }
}
using RandevuSistemi.Models;
using RandevuSistemi.Persistence;

namespace RandevuSistemi.UI;

// Kullanıcının türüne göre doğru menüyü üretir
public static class MenuFabrikasi
{
    public static KullaniciMenusu Olustur(Kullanici kullanici, UygulamaVerisi veri, DepoServisi depo)
        => kullanici switch
        {
            Hasta h => new HastaMenusu(veri, depo, h),
            Doktor d => new DoktorMenusu(veri, depo, d),
            Yonetici y => new YoneticiMenusu(veri, depo, y),
            _ => throw new NotSupportedException($"Bu kullanıcı türü için menü yok: {kullanici.GetType().Name}")
        };
}
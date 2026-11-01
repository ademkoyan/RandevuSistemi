using RandevuSistemi.Models;
using RandevuSistemi.Services;

namespace RandevuSistemi.Persistence;

public static class OrnekVeri
{
    public static UygulamaVerisi Olustur()
    {
        var kurum = new Kurum("Şifa Sağlık Grubu");

        // ---------- Şubeler ----------
        var merkez = SubeOlustur("Merkez Şube", "Atatürk Cad. No:1", cumartesiAcik: false);
        var kadikoy = SubeOlustur("Kadıköy Şube", "Bahariye Cad. No:25", cumartesiAcik: true);

        // ---------- Merkez: bölümler ve hizmetler ----------
        var merkezDahiliye = new GenelMuayene("Dahiliye Muayenesi", 20, 500m);
        var kardiyoloji = new GenelMuayene("Kardiyoloji Muayenesi", 30, 700m);
        var beyinMr = new RadyolojiIslemi("Beyin MR", 45, 2500m, "MR");
        var akciger = new RadyolojiIslemi("Akciğer Grafisi", 10, 400m, "Röntgen");
        var aclikSekeri = new LaboratuvarTesti("Açlık Kan Şekeri", 10, 150m, true, 2);
        var kanSayimi = new LaboratuvarTesti("Tam Kan Sayımı", 10, 200m, false, 4);

        merkez.BolumEkle(BolumOlustur("Dahiliye", merkezDahiliye));
        merkez.BolumEkle(BolumOlustur("Kardiyoloji", kardiyoloji));
        merkez.BolumEkle(BolumOlustur("Radyoloji", beyinMr, akciger));
        merkez.BolumEkle(BolumOlustur("Laboratuvar", aclikSekeri, kanSayimi));

        // ---------- Kadıköy ----------
        var kadikoyDahiliye = new GenelMuayene("Dahiliye Muayenesi", 20, 550m);
        kadikoy.BolumEkle(BolumOlustur("Dahiliye", kadikoyDahiliye));

        kurum.SubeEkle(merkez);
        kurum.SubeEkle(kadikoy);

        // ---------- Doktorlar ----------
        var mehmet = new Doktor("Mehmet", "Demir", "mehmet@sifa.com", "Doktor123", "Dahiliye");
        mehmet.HizmetEkle(merkezDahiliye);
        mehmet.HizmetEkle(kadikoyDahiliye);
        Uygunluk(mehmet, merkez, DayOfWeek.Monday, 9, 12);       // Merkez: Pazartesi
        Uygunluk(mehmet, kadikoy, DayOfWeek.Wednesday, 13, 17);  // Kadıköy: Çarşamba (çok şubeli örnek)

        var selin = new Doktor("Selin", "Aydın", "selin@sifa.com", "Doktor456", "Kardiyoloji");
        selin.HizmetEkle(kardiyoloji);
        Uygunluk(selin, merkez, DayOfWeek.Tuesday, 9, 17);
        Uygunluk(selin, merkez, DayOfWeek.Thursday, 9, 12);

        var can = new Doktor("Can", "Öztürk", "can@sifa.com", "Doktor789", "Radyoloji");
        can.HizmetEkle(beyinMr);
        can.HizmetEkle(akciger);
        Uygunluk(can, merkez, DayOfWeek.Monday, 9, 16);
        Uygunluk(can, merkez, DayOfWeek.Wednesday, 9, 16);

        var elif = new Doktor("Elif", "Şahin", "elif@sifa.com", "Doktor321", "Biyokimya");
        elif.HizmetEkle(aclikSekeri);
        elif.HizmetEkle(kanSayimi);
        foreach (var gun in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                    DayOfWeek.Thursday, DayOfWeek.Friday })
            Uygunluk(elif, merkez, gun, 8, 12);

        // ---------- Hastalar ve yönetici ----------
        // Not: TCKN'ler sadece doğrulama algoritmasından geçen TEST numaralarıdır.
        var ali = new Hasta("Ali", "Kaya", "ali@mail.com", "Hasta123", "10000000146", new DateOnly(1995, 5, 12));
        var veli = new Hasta("Veli", "Çelik", "veli@mail.com", "Hasta456", "11111111110", new DateOnly(1990, 3, 3));
        var ayse = new Yonetici("Ayşe", "Yılmaz", "ayse@sifa.com", "Yonetici123", kurum);

        var kimlik = new KimlikServisi();
        foreach (Kullanici k in new Kullanici[] { ayse, mehmet, selin, can, elif, ali, veli })
            kimlik.KullaniciKaydet(k);

        return new UygulamaVerisi(kurum, kimlik, new RandevuServisi());
    }

    private static Sube SubeOlustur(string ad, string adres, bool cumartesiAcik)
    {
        var sube = new Sube(ad, adres);
        foreach (var gun in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                    DayOfWeek.Thursday, DayOfWeek.Friday })
            sube.CalismaSaatiEkle(new CalismaSaati(gun, new TimeOnly(8, 0), new TimeOnly(17, 0)));

        if (cumartesiAcik)
            sube.CalismaSaatiEkle(new CalismaSaati(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0)));

        return sube;
    }

    private static Bolum BolumOlustur(string ad, params SaglikHizmeti[] hizmetler)
    {
        var bolum = new Bolum(ad);
        foreach (var h in hizmetler) bolum.HizmetEkle(h);
        return bolum;
    }

    private static void Uygunluk(Doktor doktor, Sube sube, DayOfWeek gun, int baslangicSaat, int bitisSaat)
        => doktor.UygunlukEkle(new UygunlukAraligi(sube, gun, new TimeOnly(baslangicSaat, 0), new TimeOnly(bitisSaat, 0)));
}

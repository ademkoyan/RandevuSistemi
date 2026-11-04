using RandevuSistemi.Models;
using RandevuSistemi.Persistence;

namespace RandevuSistemi.UI;

public sealed class YoneticiMenusu : KullaniciMenusu
{
    private readonly Yonetici _yonetici;

    // Şube-bölüm-hizmet üçlüsünü tek satırda göstermek için
    private sealed record HizmetKaydi(Sube Sube, Bolum Bolum, SaglikHizmeti Hizmet);

    public YoneticiMenusu(UygulamaVerisi veri, DepoServisi depo, Yonetici yonetici)
        : base(veri, depo, yonetici)
    {
        _yonetici = yonetici;
    }

    protected override string Baslik => "YÖNETİCİ MENÜSÜ";

    protected override List<MenuSecenegi> SecenekleriOlustur() => new()
    {
        new MenuSecenegi("Kurum yapısını göster", KurumYapisi),
        new MenuSecenegi("Tüm randevuları listele", TumRandevular),
        new MenuSecenegi("Randevu yönet (onayla / reddet / iptal / tamamla)", RandevuYonet),
        new MenuSecenegi("Şube ekle", SubeEkle),
        new MenuSecenegi("Bölüm ekle", BolumEkle),
        new MenuSecenegi("Hizmet ekle", HizmetEkleAkisi),
        new MenuSecenegi("Doktor ekle", DoktorEkle),
        new MenuSecenegi("Doktora hizmet ata", HizmetAta),
        new MenuSecenegi("Doktor uygunluk saati tanımla", DoktorUygunlukTanimla),
        new MenuSecenegi("Hizmet ücretini güncelle", UcretGuncelle),
        new MenuSecenegi("Raporlar", Raporlar)
    };

    // =====================================================================
    //  GÖRÜNTÜLEME
    // =====================================================================
    private void KurumYapisi()
    {
        Konsol.Baslik(Veri.Kurum.Ad);

        foreach (var sube in Veri.Kurum.Subeler)
        {
            Console.WriteLine($"\n* {sube.Ad} - {sube.Adres}");

            foreach (var c in sube.CalismaSaatleri)
                Console.WriteLine($"    {Konsol.GunAdi(c.Gun),-10} {c.Acilis:HH:mm}-{c.Kapanis:HH:mm}");

            foreach (var bolum in sube.Bolumler)
            {
                Console.WriteLine($"  > {bolum.Ad}");
                foreach (var h in bolum.Hizmetler)
                    Console.WriteLine($"      - {h.Ad} [{h.TurAdi}] {h.SureDakika} dk, {h.Ucret:N2} TL");
            }
        }

        Console.WriteLine("\nDoktorlar:");
        foreach (var d in Veri.KimlikServisi.Kullanicilar.OfType<Doktor>())
            Console.WriteLine($"  - Dr. {d.TamAd} ({d.Uzmanlik}) | {d.Hizmetler.Count} hizmet, {d.Uygunluklar.Count} uygunluk aralığı");
    }

    private void TumRandevular()
    {
        Konsol.Baslik("Tüm Randevular");
        var liste = Veri.RandevuServisi.Randevular.OrderBy(r => r.BaslangicZamani).ToList();
        Konsol.Liste("Randevular", liste, r => Konsol.RandevuMetni(r, hastayiGoster: true));
    }

    // =====================================================================
    //  RANDEVU YÖNETİMİ
    // =====================================================================
    private void RandevuYonet()
    {
        Konsol.Baslik("Randevu Yönetimi");
        var aktifler = Veri.RandevuServisi.Randevular
            .Where(r => r.AktifMi)
            .OrderBy(r => r.BaslangicZamani)
            .ToList();

        var secilen = Konsol.Sec("Randevu seçin", aktifler, r => Konsol.RandevuMetni(r, hastayiGoster: true));
        if (secilen is null) return;

        Console.WriteLine("\n  1) Onayla\n  2) Reddet\n  3) İptal et\n  4) Tamamlandı işaretle\n  0) Vazgeç");
        var servis = Veri.RandevuServisi;

        switch (Konsol.SayiOku("İşlem"))
        {
            case 1: servis.Onayla(secilen.Id, _yonetici); Konsol.Basari("Randevu onaylandı."); break;
            case 2: servis.Reddet(secilen.Id, _yonetici); Konsol.Basari("Randevu reddedildi."); break;
            case 3: servis.Iptal(secilen.Id, _yonetici); Konsol.Basari("Randevu iptal edildi."); break;
            case 4: servis.Tamamla(secilen.Id, _yonetici); Konsol.Basari("Randevu tamamlandı."); break;
            case 0: return;
            default: Konsol.Hata("Geçersiz seçim."); return;
        }

        Kaydet();
    }

    // =====================================================================
    //  KURUM TANIMLARI
    // =====================================================================
    private void SubeEkle()
    {
        Konsol.Baslik("Şube Ekle");

        var ad = Konsol.ZorunluOku("Şube adı");
        if (Veri.Kurum.Subeler.Any(s => string.Equals(s.Ad, ad, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Bu isimde bir şube zaten var.");

        var adres = Konsol.ZorunluOku("Adres");
        var sube = new Sube(ad, adres);

        Console.WriteLine("\nHafta içi (Pazartesi-Cuma) çalışma saatleri:");
        var acilis = Konsol.SaatOku("Açılış saati");
        var kapanis = Konsol.SaatOku("Kapanış saati");

        foreach (var gun in Konsol.HaftaninGunleri.Take(5))
            sube.CalismaSaatiEkle(new CalismaSaati(gun, acilis, kapanis));

        if (Konsol.EvetMi("Cumartesi açık mı"))
        {
            var cumartesiAcilis = Konsol.SaatOku("Cumartesi açılış");
            var cumartesiKapanis = Konsol.SaatOku("Cumartesi kapanış");
            sube.CalismaSaatiEkle(new CalismaSaati(DayOfWeek.Saturday, cumartesiAcilis, cumartesiKapanis));
        }

        Veri.Kurum.SubeEkle(sube);
        Kaydet();
        Konsol.Basari($"'{sube.Ad}' şubesi eklendi.");
    }

    private void BolumEkle()
    {
        Konsol.Baslik("Bölüm Ekle");

        var sube = Konsol.Sec("Şube seçin", Veri.Kurum.Subeler, s => s.Ad);
        if (sube is null) return;

        var ad = Konsol.ZorunluOku("Bölüm adı");
        if (sube.Bolumler.Any(b => string.Equals(b.Ad, ad, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Bu şubede aynı isimde bir bölüm zaten var.");

        sube.BolumEkle(new Bolum(ad));
        Kaydet();
        Konsol.Basari($"'{ad}' bölümü '{sube.Ad}' şubesine eklendi.");
    }

    private void HizmetEkleAkisi()
    {
        Konsol.Baslik("Hizmet Ekle");

        var sube = Konsol.Sec("Şube seçin", Veri.Kurum.Subeler, s => s.Ad);
        if (sube is null) return;

        var bolum = Konsol.Sec("Bölüm seçin", sube.Bolumler, b => b.Ad);
        if (bolum is null) return;

        Console.WriteLine("\nHizmet türü:\n  1) Genel muayene\n  2) Radyoloji işlemi\n  3) Laboratuvar testi\n  0) Geri");
        int tur = Konsol.SayiOku("Tür");
        if (tur == 0) return;
        if (tur < 1 || tur > 3)
        {
            Konsol.Hata("Geçersiz seçim.");
            return;
        }

        var ad = Konsol.ZorunluOku("Hizmet adı");
        int sure = Konsol.SayiOku("Süre (dakika)");
        decimal ucret = Konsol.DecimalOku("Ücret (TL)");

        // Türe göre doğru alt sınıf oluşturulur; bölüm ise hepsini SaglikHizmeti olarak tutar
        SaglikHizmeti hizmet = tur switch
        {
            1 => new GenelMuayene(ad, sure, ucret),
            2 => new RadyolojiIslemi(ad, sure, ucret, Konsol.ZorunluOku("Cihaz (MR, Röntgen...)")),
            _ => new LaboratuvarTesti(ad, sure, ucret,
                     Konsol.EvetMi("Açlık gerekli mi"), Konsol.SayiOku("Sonuç süresi (saat)"))
        };

        bolum.HizmetEkle(hizmet);
        Kaydet();
        Konsol.Basari($"'{hizmet.Ad}' hizmeti eklendi.");
    }

    private void UcretGuncelle()
    {
        Konsol.Baslik("Hizmet Ücreti Güncelle");

        var kayit = Konsol.Sec("Hizmet seçin", TumHizmetler(),
            k => $"{k.Sube.Ad} / {k.Bolum.Ad} / {k.Hizmet.Ad} - {k.Hizmet.Ucret:N2} TL");
        if (kayit is null) return;

        var yeniUcret = Konsol.DecimalOku("Yeni ücret (TL)");
        kayit.Hizmet.Ucret = yeniUcret;      // doğrulama property içinde
        Kaydet();

        Konsol.Basari($"Ücret güncellendi: {yeniUcret:N2} TL");
        Konsol.Bilgi("Not: Mevcut randevuların ücreti değişmez, yeni fiyat sadece yeni randevulara uygulanır.");
    }

    // =====================================================================
    //  ÇALIŞAN YÖNETİMİ
    // =====================================================================
    private void DoktorEkle()
    {
        Konsol.Baslik("Doktor Ekle");

        var ad = Konsol.ZorunluOku("Ad");
        var soyad = Konsol.ZorunluOku("Soyad");
        var email = Konsol.ZorunluOku("E-posta");
        Console.Write("Geçici şifre (en az 6 karakter): ");
        var sifre = Konsol.SifreOku();
        var uzmanlik = Konsol.ZorunluOku("Uzmanlık alanı");

        var doktor = new Doktor(ad, soyad, email, sifre, uzmanlik);
        Veri.KimlikServisi.KullaniciKaydet(doktor);
        Kaydet();

        Konsol.Basari($"Dr. {doktor.TamAd} eklendi. Hizmet atamayı ve uygunluk tanımlamayı unutmayın.");
    }

    private void HizmetAta()
    {
        Konsol.Baslik("Doktora Hizmet Ata");

        var doktor = DoktorSec();
        if (doktor is null) return;

        var atanabilir = TumHizmetler().Where(k => !doktor.HizmetSunuyorMu(k.Hizmet)).ToList();
        var kayit = Konsol.Sec($"Dr. {doktor.TamAd} için hizmet seçin", atanabilir,
            k => $"{k.Sube.Ad} / {k.Bolum.Ad} / {k.Hizmet.Ad} [{k.Hizmet.TurAdi}]");
        if (kayit is null) return;

        doktor.HizmetEkle(kayit.Hizmet);
        Kaydet();
        Konsol.Basari($"'{kayit.Hizmet.Ad}' hizmeti Dr. {doktor.TamAd} adlı doktora atandı.");
    }

    private void DoktorUygunlukTanimla()
    {
        Konsol.Baslik("Doktor Uygunluk Saati");

        var doktor = DoktorSec();
        if (doktor is null) return;

        UygunlukYazdir(doktor);
        UygunlukEkleAkisi(doktor);
    }

    // =====================================================================
    //  RAPORLAR
    // =====================================================================
    private void Raporlar()
    {
        Konsol.Baslik("Raporlar");
        var tum = Veri.RandevuServisi.Randevular;

        Console.WriteLine($"Toplam randevu: {tum.Count}");
        foreach (var durum in Enum.GetValues<RandevuDurumu>())
            Console.WriteLine($"  {Konsol.DurumMetni(durum),-15}: {tum.Count(r => r.Durum == durum)}");

        decimal gelir = tum.Where(r => r.Durum == RandevuDurumu.Tamamlandi).Sum(r => r.Ucret);
        decimal beklenen = tum.Where(r => r.Durum == RandevuDurumu.Onaylandi).Sum(r => r.Ucret);
        Console.WriteLine($"\nGerçekleşen gelir (tamamlanan) : {gelir:N2} TL");
        Console.WriteLine($"Beklenen gelir (onaylanan)     : {beklenen:N2} TL");

        Console.WriteLine("\nDoktor bazında (iptal ve red hariç):");
        var gruplar = tum
            .Where(r => r.Durum is not (RandevuDurumu.IptalEdildi or RandevuDurumu.Reddedildi))
            .GroupBy(r => r.Doktor)
            .OrderByDescending(g => g.Count());

        bool varMi = false;
        foreach (var g in gruplar)
        {
            varMi = true;
            Console.WriteLine($"  Dr. {g.Key.TamAd,-20} {g.Count()} randevu, {g.Sum(r => r.Ucret):N2} TL");
        }
        if (!varMi) Konsol.Bilgi("  (kayıt yok)");
    }

    // =====================================================================
    //  YARDIMCILAR
    // =====================================================================
    private Doktor? DoktorSec()
        => Konsol.Sec("Doktor seçin", Veri.KimlikServisi.Kullanicilar.OfType<Doktor>().ToList(),
            d => $"Dr. {d.TamAd} ({d.Uzmanlik}) - {d.Email}");

    private List<HizmetKaydi> TumHizmetler()
        => Veri.Kurum.Subeler
            .SelectMany(s => s.Bolumler.SelectMany(b => b.Hizmetler.Select(h => new HizmetKaydi(s, b, h))))
            .ToList();
}
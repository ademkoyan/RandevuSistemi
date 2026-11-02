using RandevuSistemi.Models;
using RandevuSistemi.Persistence;

namespace RandevuSistemi.UI;

public sealed class HastaMenusu : KullaniciMenusu
{
    private readonly Hasta _hasta;

    public HastaMenusu(UygulamaVerisi veri, DepoServisi depo, Hasta hasta)
        : base(veri, depo, hasta)
    {
        _hasta = hasta;
    }

    protected override string Baslik => "HASTA MENÜSÜ";

    protected override List<MenuSecenegi> SecenekleriOlustur() => new()
    {
        new MenuSecenegi("Randevu al", RandevuAl),
        new MenuSecenegi("Randevularım", RandevularimiListele),
        new MenuSecenegi("Randevu iptal et", RandevuIptal),
        new MenuSecenegi("Randevu hazırlık talimatı", HazirlikTalimati)
    };

    private void RandevuAl()
    {
        Konsol.Baslik("Randevu Al");

        var sube = Konsol.Sec("Şube seçin", Veri.Kurum.Subeler, s => $"{s.Ad} ({s.Adres})");
        if (sube is null) return;

        var bolum = Konsol.Sec("Bölüm seçin", sube.Bolumler, b => b.Ad);
        if (bolum is null) return;

        var hizmet = Konsol.Sec("Hizmet seçin", bolum.Hizmetler,
            h => $"{h.Ad} [{h.TurAdi}] - {h.SureDakika} dk - {h.Ucret:N2} TL");
        if (hizmet is null) return;

        // Bu hizmeti sunan ve bu şubede çalışan doktorlar
        var doktorlar = Veri.KimlikServisi.Kullanicilar.OfType<Doktor>()
            .Where(d => d.HizmetSunuyorMu(hizmet) && d.Uygunluklar.Any(u => u.Sube == sube))
            .ToList();

        if (doktorlar.Count == 0)
        {
            Konsol.Bilgi("Bu hizmeti bu şubede sunan doktor bulunamadı.");
            return;
        }

        var doktor = Konsol.Sec("Doktor seçin", doktorlar, d => $"Dr. {d.TamAd} ({d.Uzmanlik})");
        if (doktor is null) return;

        var gunler = doktor.Uygunluklar
            .Where(u => u.Sube == sube)
            .OrderBy(u => Array.IndexOf(Konsol.HaftaninGunleri, u.Gun))
            .Select(u => $"{Konsol.GunAdi(u.Gun)} {u.Baslangic:HH:mm}-{u.Bitis:HH:mm}");
        Console.WriteLine($"\nÇalışma günleri: {string.Join(", ", gunler)}");

        var tarih = Konsol.TarihOku("Randevu tarihi");

        // Hastanın kendi randevularıyla çakışan saatler de elenir
        var saatler = Veri.RandevuServisi.UygunSaatleriGetir(doktor, hizmet, sube, tarih, 30, _hasta);
        if (saatler.Count == 0)
        {
            Konsol.Bilgi("Bu tarihte uygun saat bulunamadı. Başka bir gün deneyin.");
            return;
        }

        int saatIndeks = Konsol.SecIndeks("Uygun saatler", saatler, s => s.ToString("HH:mm"));
        if (saatIndeks < 0) return;
        var saat = saatler[saatIndeks];

        var bitis = saat.AddMinutes(hizmet.SureDakika);
        Console.WriteLine();
        Console.WriteLine($"Özet : {tarih:dd.MM.yyyy} {Konsol.GunAdi(tarih.DayOfWeek)} {saat:HH:mm}-{bitis:HH:mm}");
        Console.WriteLine($"       {hizmet.Ad} | Dr. {doktor.TamAd} | {sube.Ad} | {hizmet.Ucret:N2} TL");
        Console.WriteLine($"Hazırlık: {hizmet.HazirlikTalimati()}");

        if (!Konsol.EvetMi("Randevuyu oluşturmak istiyor musunuz"))
        {
            Konsol.Bilgi("Vazgeçildi.");
            return;
        }

        Veri.RandevuServisi.RandevuAl(_hasta, doktor, hizmet, sube, tarih, saat);
        Kaydet();
        Konsol.Basari("Randevunuz oluşturuldu. Doktor onayı bekleniyor.");
    }

    private void RandevularimiListele()
    {
        Konsol.Baslik("Randevularım");
        var liste = Veri.RandevuServisi.HastaRandevulari(_hasta);
        Konsol.Liste("Tüm randevularınız", liste, r => Konsol.RandevuMetni(r));
    }

    private void RandevuIptal()
    {
        Konsol.Baslik("Randevu İptal");
        var aktifler = Veri.RandevuServisi.HastaRandevulari(_hasta).Where(r => r.AktifMi).ToList();

        var secilen = Konsol.Sec("İptal edilecek randevu", aktifler, r => Konsol.RandevuMetni(r));
        if (secilen is null) return;

        if (!Konsol.EvetMi("Bu randevuyu iptal etmek istediğinize emin misiniz"))
        {
            Konsol.Bilgi("Vazgeçildi.");
            return;
        }

        Veri.RandevuServisi.Iptal(secilen.Id, _hasta);
        Kaydet();
        Konsol.Basari("Randevu iptal edildi.");
    }

    private void HazirlikTalimati()
    {
        Konsol.Baslik("Hazırlık Talimatı");
        var aktifler = Veri.RandevuServisi.HastaRandevulari(_hasta).Where(r => r.AktifMi).ToList();

        var secilen = Konsol.Sec("Randevu seçin", aktifler, r => Konsol.RandevuMetni(r));
        if (secilen is null) return;

        // Çok biçimlilik: hizmetin türüne göre farklı talimat döner
        Console.WriteLine($"\n[{secilen.Hizmet.TurAdi}] {secilen.HizmetAdi}");
        Console.WriteLine(secilen.Hizmet.HazirlikTalimati());
    }
}
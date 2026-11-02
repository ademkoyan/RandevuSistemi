using RandevuSistemi.Exceptions;
using RandevuSistemi.Models;
using RandevuSistemi.Persistence;

namespace RandevuSistemi.UI;

public sealed record MenuSecenegi(string Metin, Action Eylem);

public abstract class KullaniciMenusu
{
    protected UygulamaVerisi Veri { get; }
    protected DepoServisi Depo { get; }
    protected Kullanici Oturum { get; }

    protected KullaniciMenusu(UygulamaVerisi veri, DepoServisi depo, Kullanici oturum)
    {
        ArgumentNullException.ThrowIfNull(veri);
        ArgumentNullException.ThrowIfNull(depo);
        ArgumentNullException.ThrowIfNull(oturum);
        Veri = veri;
        Depo = depo;
        Oturum = oturum;
    }

    // Alt sınıflar sadece bu ikisini doldurur
    protected abstract string Baslik { get; }
    protected abstract List<MenuSecenegi> SecenekleriOlustur();

    // ---------- Şablon: tüm roller için ortak döngü ----------
    public void Calistir()
    {
        var secenekler = SecenekleriOlustur();
        secenekler.Add(new MenuSecenegi("Şifre değiştir", SifreDegistir));

        while (true)
        {
            Konsol.Baslik($"{Baslik} - {Oturum.TamAd}");
            for (int i = 0; i < secenekler.Count; i++)
                Console.WriteLine($"  {i + 1}) {secenekler[i].Metin}");
            Console.WriteLine("  0) Oturumu kapat");

            int secim = Konsol.SayiOku("Seçiminiz");
            if (secim == 0) return;
            if (secim < 1 || secim > secenekler.Count)
            {
                Konsol.Hata("Geçersiz seçim.");
                continue;
            }

            try
            {
                secenekler[secim - 1].Eylem();
            }
            // Domain katmanının fırlattığı iş kuralı hataları kullanıcıya mesaj olarak gösterilir
            catch (Exception ex) when (ex is RandevuSistemiException
                                          or ArgumentException
                                          or InvalidOperationException)
            {
                Konsol.Hata($"İşlem yapılamadı: {ex.Message}");
            }
        }
    }

    // ---------- Alt sınıfların ortak kullandığı yardımcılar ----------
    protected void Kaydet() => Depo.Kaydet(Veri);

    protected void SifreDegistir()
    {
        Console.Write("Mevcut şifre: ");
        var eski = Konsol.SifreOku();
        if (!Oturum.SifreDogrula(eski))
            throw new YetkisizIslemException("Mevcut şifre hatalı.");

        Console.Write("Yeni şifre (en az 6 karakter): ");
        var yeni = Konsol.SifreOku();
        Console.Write("Yeni şifre (tekrar): ");
        var tekrar = Konsol.SifreOku();

        if (yeni != tekrar)
            throw new ArgumentException("Şifreler eşleşmiyor.");

        Oturum.SifreyiDegistir(yeni);
        Kaydet();
        Konsol.Basari("Şifreniz değiştirildi.");
    }

    protected static void UygunlukYazdir(Doktor doktor)
    {
        var sirali = doktor.Uygunluklar
            .OrderBy(u => Array.IndexOf(Konsol.HaftaninGunleri, u.Gun))
            .ThenBy(u => u.Baslangic)
            .ToList();

        Konsol.Liste($"Dr. {doktor.TamAd} - uygunluk saatleri", sirali,
            u => $"{u.Sube.Ad} - {Konsol.GunAdi(u.Gun)} {u.Baslangic:HH:mm}-{u.Bitis:HH:mm}");
    }

    // Hem doktor hem yönetici kullanır
    protected void UygunlukEkleAkisi(Doktor doktor)
    {
        var sube = Konsol.Sec("Şube seçin", Veri.Kurum.Subeler, s => s.Ad);
        if (sube is null) return;

        var gun = Konsol.GunSec();
        if (gun is null) return;

        var baslangic = Konsol.SaatOku("Başlangıç saati");
        var bitis = Konsol.SaatOku("Bitiş saati");

        // Şube saatine uyma ve çakışma kuralları Doktor sınıfının içinde
        doktor.UygunlukEkle(new UygunlukAraligi(sube, gun.Value, baslangic, bitis));
        Kaydet();
        Konsol.Basari("Uygunluk saati eklendi.");
    }
}
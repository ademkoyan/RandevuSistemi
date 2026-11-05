using RandevuSistemi.Models;
using RandevuSistemi.Persistence;

namespace RandevuSistemi.UI;

public class UygulamaAkisi
{
    private readonly UygulamaVerisi _veri;
    private readonly DepoServisi _depo;

    public UygulamaAkisi(UygulamaVerisi veri, DepoServisi depo)
    {
        _veri = veri;
        _depo = depo;
    }

    public void Calistir()
    {
        while (true)
        {
            Konsol.Baslik("HASTANE / KLİNİK RANDEVU SİSTEMİ");
            Console.WriteLine("  1) Giriş yap");
            Console.WriteLine("  2) Hasta olarak kayıt ol");
            Console.WriteLine("  0) Çıkış");

            switch (Konsol.SayiOku("Seçiminiz"))
            {
                case 1: GirisYap(); break;
                case 2: KayitOl(); break;
                case 0: return;
                default: Konsol.Hata("Geçersiz seçim."); break;
            }
        }
    }

    private void GirisYap()
    {
        Konsol.Baslik("Giriş");

        for (int deneme = 1; deneme <= 3; deneme++)
        {
            var email = Konsol.Oku("E-posta");
            Console.Write("Şifre: ");
            var sifre = Konsol.SifreOku();

            var kullanici = _veri.KimlikServisi.GirisYap(email, sifre);
            if (kullanici != null)
            {
                Konsol.Basari($"\nHoş geldiniz, {kullanici.TamAd} ({kullanici.RolAdi})");

                // Çok biçimlilik: hangi rol olursa olsun aynı çağrı
                MenuFabrikasi.Olustur(kullanici, _veri, _depo).Calistir();

                Konsol.Bilgi("Oturum kapatıldı.");
                return;
            }

            // Hangisinin yanlış olduğunu söylemiyoruz (güvenlik)
            Konsol.Hata($"E-posta veya şifre hatalı ({deneme}/3).");
        }

        Konsol.Hata("Çok fazla hatalı deneme. Ana menüye dönülüyor.");
    }

    private void KayitOl()
    {
        Konsol.Baslik("Hasta Kaydı");

        try
        {
            var ad = Konsol.ZorunluOku("Ad");
            var soyad = Konsol.ZorunluOku("Soyad");
            var email = Konsol.ZorunluOku("E-posta");

            Console.Write("Şifre (en az 6 karakter): ");
            var sifre = Konsol.SifreOku();
            Console.Write("Şifre (tekrar): ");
            var tekrar = Konsol.SifreOku();
            if (sifre != tekrar)
            {
                Konsol.Hata("Şifreler eşleşmiyor.");
                return;
            }

            var tckn = Konsol.ZorunluOku("TC kimlik no");
            var dogum = Konsol.TarihOku("Doğum tarihi");

            var hasta = new Hasta(ad, soyad, email, sifre, tckn, dogum);
            _veri.KimlikServisi.KullaniciKaydet(hasta);
            _depo.Kaydet(_veri);

            Konsol.Basari("Kaydınız oluşturuldu. Şimdi giriş yapabilirsiniz.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Konsol.Hata($"Kayıt yapılamadı: {ex.Message}");
        }
    }
}
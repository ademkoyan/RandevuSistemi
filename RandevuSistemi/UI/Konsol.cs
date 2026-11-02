using System.Globalization;
using System.Text;
using RandevuSistemi.Models;

namespace RandevuSistemi.UI;

public static class Konsol
{
    // ---------- Türkçe gün adları ----------
    public static readonly DayOfWeek[] HaftaninGunleri =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public static string GunAdi(DayOfWeek gun) => gun switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        DayOfWeek.Saturday => "Cumartesi",
        _ => "Pazar"
    };

    public static string DurumMetni(RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Beklemede => "Onay bekliyor",
        RandevuDurumu.Onaylandi => "Onaylandı",
        RandevuDurumu.Reddedildi => "Reddedildi",
        RandevuDurumu.IptalEdildi => "İptal edildi",
        RandevuDurumu.Tamamlandi => "Tamamlandı",
        _ => durum.ToString()
    };

    // Tek satırlık randevu özeti (rolüne göre hasta/doktor adı gösterilir)
    public static string RandevuMetni(Randevu r, bool hastayiGoster = false, bool doktoruGoster = true)
    {
        var parcalar = new List<string>
        {
            $"{r.Tarih:dd.MM.yyyy} {GunAdi(r.Tarih.DayOfWeek)} {r.Baslangic:HH:mm}-{r.Bitis:HH:mm}",
            r.HizmetAdi
        };
        if (doktoruGoster) parcalar.Add($"Dr. {r.Doktor.TamAd}");
        if (hastayiGoster) parcalar.Add($"Hasta: {r.Hasta.TamAd}");
        parcalar.Add(r.Sube.Ad);
        parcalar.Add($"{r.Ucret:N2} TL");
        parcalar.Add(DurumMetni(r.Durum));
        return string.Join(" | ", parcalar);
    }

    // ---------- Çıktı ----------
    public static void Baslik(string metin)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {metin} ===");
    }

    public static void Basari(string mesaj) => Renkli(mesaj, ConsoleColor.Green);
    public static void Hata(string mesaj) => Renkli(mesaj, ConsoleColor.Red);
    public static void Bilgi(string mesaj) => Renkli(mesaj, ConsoleColor.Yellow);

    private static void Renkli(string mesaj, ConsoleColor renk)
    {
        var onceki = Console.ForegroundColor;
        Console.ForegroundColor = renk;
        Console.WriteLine(mesaj);
        Console.ForegroundColor = onceki;
    }

    public static void Liste<T>(string baslik, IReadOnlyList<T> kayitlar, Func<T, string> metin)
    {
        Console.WriteLine($"\n{baslik}");
        if (kayitlar.Count == 0)
        {
            Bilgi("  (kayıt yok)");
            return;
        }
        for (int i = 0; i < kayitlar.Count; i++)
            Console.WriteLine($"  {i + 1}) {metin(kayitlar[i])}");
    }

    // ---------- Girdi ----------
    public static string Oku(string soru)
    {
        Console.Write($"{soru}: ");
        var satir = Console.ReadLine();
        if (satir is null) throw new EndOfStreamException("Girdi akışı kapandı.");
        return satir.Trim();
    }

    public static string ZorunluOku(string soru)
    {
        while (true)
        {
            var s = Oku(soru);
            if (s.Length > 0) return s;
            Hata("Bu alan boş bırakılamaz.");
        }
    }

    public static int SayiOku(string soru)
    {
        while (true)
        {
            if (int.TryParse(Oku(soru), out int n)) return n;
            Hata("Lütfen bir sayı giriniz.");
        }
    }

    public static decimal DecimalOku(string soru)
    {
        while (true)
        {
            var s = Oku(soru).Replace(',', '.');
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
            Hata("Lütfen geçerli bir tutar giriniz (örn. 450 veya 450,50).");
        }
    }

    private static readonly string[] TarihFormatlari = { "dd.MM.yyyy", "d.M.yyyy" };
    private static readonly string[] SaatFormatlari = { "HH:mm", "H:mm" };

    public static DateOnly TarihOku(string soru)
    {
        while (true)
        {
            var s = Oku($"{soru} (gg.aa.yyyy)");
            if (DateOnly.TryParseExact(s, TarihFormatlari, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var tarih))
                return tarih;
            Hata("Geçersiz tarih. Örnek: 09.11.2026");
        }
    }

    public static TimeOnly SaatOku(string soru)
    {
        while (true)
        {
            var s = Oku($"{soru} (ss:dd)");
            if (TimeOnly.TryParseExact(s, SaatFormatlari, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var saat))
                return saat;
            Hata("Geçersiz saat. Örnek: 09:30");
        }
    }

    public static bool EvetMi(string soru)
    {
        while (true)
        {
            var c = Oku($"{soru} (e/h)").ToLowerInvariant();
            if (c is "e" or "evet") return true;
            if (c is "h" or "hayır" or "hayir") return false;
            Hata("Lütfen 'e' veya 'h' yazın.");
        }
    }

    // ---------- Seçim menüleri ----------
    // Seçilen sıra numarasını döner (0'dan başlar). Geri (0) veya boş liste için -1.
    public static int SecIndeks<T>(string baslik, IReadOnlyList<T> secenekler, Func<T, string> metin)
    {
        if (secenekler.Count == 0)
        {
            Bilgi($"{baslik}: seçilebilecek kayıt yok.");
            return -1;
        }

        Liste(baslik, secenekler, metin);
        Console.WriteLine("  0) Geri");

        while (true)
        {
            int n = SayiOku("Seçiminiz");
            if (n == 0) return -1;
            if (n >= 1 && n <= secenekler.Count) return n - 1;
            Hata("Geçersiz seçim.");
        }
    }

    // Sınıf türleri için kolaylık: seçilen nesneyi, geri/boşsa null döner
    public static T? Sec<T>(string baslik, IReadOnlyList<T> secenekler, Func<T, string> metin) where T : class
    {
        int i = SecIndeks(baslik, secenekler, metin);
        return i < 0 ? null : secenekler[i];
    }

    public static DayOfWeek? GunSec()
    {
        Console.WriteLine("\nGün:");
        for (int i = 0; i < HaftaninGunleri.Length; i++)
            Console.WriteLine($"  {i + 1}) {GunAdi(HaftaninGunleri[i])}");
        Console.WriteLine("  0) Geri");

        while (true)
        {
            int n = SayiOku("Seçiminiz");
            if (n == 0) return null;
            if (n >= 1 && n <= HaftaninGunleri.Length) return HaftaninGunleri[n - 1];
            Hata("Geçersiz seçim.");
        }
    }

    // Şifreyi yıldızlayarak okur. Etkileşimli terminal yoksa normal okumaya düşer.
    public static string SifreOku()
    {
        try
        {
            if (Console.IsInputRedirected) return Console.ReadLine() ?? "";

            var sb = new StringBuilder();
            while (true)
            {
                var tus = Console.ReadKey(intercept: true);

                if (tus.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return sb.ToString();
                }
                if (tus.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
                }
                else if (!char.IsControl(tus.KeyChar))
                {
                    sb.Append(tus.KeyChar);
                    Console.Write('*');
                }
            }
        }
        catch (InvalidOperationException)
        {
            return Console.ReadLine() ?? "";
        }
    }
}
using RandevuSistemi.Models;
using RandevuSistemi.Services;

namespace RandevuSistemi.Tests;

public class TestOrtami
{
    // 7 Ocak 2030 bir PAZARTESİDİR. Testlerde hep bu günü kullanacağız.
    public static readonly DateOnly Pazartesi = new(2030, 1, 7);

    // "Şimdi" sabit: Pazartesi'den önceki bir Salı sabahı. Böylece Pazartesi hep "gelecek".
    public static readonly DateTime SabitSimdi = new(2030, 1, 1, 8, 0, 0);

    public Kurum Kurum { get; }
    public Sube Merkez { get; }
    public GenelMuayene Muayene { get; }       // 20 dk, 500 TL
    public RadyolojiIslemi Mr { get; }         // hiçbir doktor sunmuyor
    public Doktor Doktor1 { get; }             // Pzt 09:00-12:00
    public Doktor Doktor2 { get; }             // Pzt 09:00-12:00
    public Hasta Hasta1 { get; }
    public Hasta Hasta2 { get; }
    public Yonetici Yonetici { get; }
    public RandevuServisi Servis { get; }

    public TestOrtami()
    {
        Kurum = new Kurum("Test Sağlık Grubu");
        Merkez = YeniSube("Merkez Şube");
        Kurum.SubeEkle(Merkez);

        Muayene = new GenelMuayene("Dahiliye Muayenesi", 20, 500m);
        Mr = new RadyolojiIslemi("Beyin MR", 45, 2500m, "MR");

        Doktor1 = new Doktor("Mehmet", "Demir", "mehmet@test.com", "Doktor123", "Dahiliye");
        Doktor1.HizmetEkle(Muayene);
        Doktor1.UygunlukEkle(new UygunlukAraligi(Merkez, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)));

        Doktor2 = new Doktor("Selin", "Aydın", "selin@test.com", "Doktor456", "Dahiliye");
        Doktor2.HizmetEkle(Muayene);
        Doktor2.UygunlukEkle(new UygunlukAraligi(Merkez, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)));

        Hasta1 = new Hasta("Ali", "Kaya", "ali@test.com", "Hasta123", "10000000146", new DateOnly(1995, 5, 12));
        Hasta2 = new Hasta("Veli", "Çelik", "veli@test.com", "Hasta456", "11111111110", new DateOnly(1990, 3, 3));

        Yonetici = new Yonetici("Ayşe", "Yılmaz", "ayse@test.com", "Yonetici123", Kurum);

        Servis = new RandevuServisi(() => SabitSimdi);
    }

    // Pazartesi-Cuma 08:00-17:00 açık bir şube
    public static Sube YeniSube(string ad)
    {
        var sube = new Sube(ad, "Test Adres");
        foreach (var gun in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                                    DayOfWeek.Thursday, DayOfWeek.Friday })
            sube.CalismaSaatiEkle(new CalismaSaati(gun, new TimeOnly(8, 0), new TimeOnly(17, 0)));
        return sube;
    }
}
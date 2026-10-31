namespace RandevuSistemi.Persistence;

// Dosyada saklanan tür adları
public static class HizmetTurleri
{
    public const string GenelMuayene = "GenelMuayene";
    public const string Radyoloji = "Radyoloji";
    public const string Laboratuvar = "Laboratuvar";
}

public static class Roller
{
    public const string Hasta = "Hasta";
    public const string Doktor = "Doktor";
    public const string Yonetici = "Yonetici";
}

// Dosyanın kökü
public class VeriDosyasi
{
    public int SurumNo { get; set; } = 1;
    public string KurumAdi { get; set; } = "";
    public List<SubeDto> Subeler { get; set; } = new();
    public List<KullaniciDto> Kullanicilar { get; set; } = new();
    public List<RandevuDto> Randevular { get; set; } = new();
}

public class SubeDto
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = "";
    public string Adres { get; set; } = "";
    public List<CalismaSaatiDto> CalismaSaatleri { get; set; } = new();
    public List<BolumDto> Bolumler { get; set; } = new();
}

public class CalismaSaatiDto
{
    public DayOfWeek Gun { get; set; }
    public TimeOnly Acilis { get; set; }
    public TimeOnly Kapanis { get; set; }
}

public class BolumDto
{
    public string Ad { get; set; } = "";
    public List<HizmetDto> Hizmetler { get; set; } = new();
}

// Üç hizmet türünün alanları tek DTO'da; kullanılmayanlar boş kalır
public class HizmetDto
{
    public Guid Id { get; set; }
    public string Tur { get; set; } = "";
    public string Ad { get; set; } = "";
    public int SureDakika { get; set; }
    public decimal Ucret { get; set; }

    public string? Cihaz { get; set; }              // Radyoloji
    public bool AclikGerekli { get; set; }          // Laboratuvar
    public int SonucSuresiSaat { get; set; }        // Laboratuvar
}

public class KullaniciDto
{
    public string Rol { get; set; } = "";
    public Guid Id { get; set; }
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string Email { get; set; } = "";
    public string SifreHash { get; set; } = "";
    public string Tuz { get; set; } = "";

    public string? Tckn { get; set; }               // Hasta
    public DateOnly? DogumTarihi { get; set; }      // Hasta

    public string? Uzmanlik { get; set; }           // Doktor
    public List<Guid> HizmetIdleri { get; set; } = new();
    public List<UygunlukDto> Uygunluklar { get; set; } = new();
}

public class UygunlukDto
{
    public Guid SubeId { get; set; }
    public DayOfWeek Gun { get; set; }
    public TimeOnly Baslangic { get; set; }
    public TimeOnly Bitis { get; set; }
}

public class RandevuDto
{
    public Guid Id { get; set; }
    public Guid HastaId { get; set; }
    public Guid DoktorId { get; set; }
    public Guid HizmetId { get; set; }
    public Guid SubeId { get; set; }
    public DateOnly Tarih { get; set; }
    public TimeOnly Baslangic { get; set; }

    // Snapshot değerleri
    public string HizmetAdi { get; set; } = "";
    public int SureDakika { get; set; }
    public decimal Ucret { get; set; }

    public Models.RandevuDurumu Durum { get; set; }
    public DateTime OlusturmaZamani { get; set; }
}
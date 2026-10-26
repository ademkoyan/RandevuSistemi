namespace RandevuSistemi.Models;

public class Hasta : Kullanici
{
    public string Tckn { get; }
    public DateOnly DogumTarihi { get; }

    public Hasta(string ad, string soyad, string email, string sifre,
                 string tckn, DateOnly dogumTarihi)
        : base(ad, soyad, email, sifre)
    {
        if (!TcknGecerliMi(tckn))
            throw new ArgumentException("Geçersiz TC kimlik numarası.");
        if (dogumTarihi > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Doğum tarihi gelecekte olamaz.");

        Tckn = tckn;
        DogumTarihi = dogumTarihi;
    }

    public override string RolAdi => "Hasta";

    public override IReadOnlyList<string> Yetkiler => new[]
    {
        "Randevu alma",
        "Kendi randevularını görme",
        "Kendi randevusunu iptal etme"
    };

    // TCKN doğrulama algoritması (resmi kural)
    public static bool TcknGecerliMi(string? tckn)
    {
        if (tckn is null || tckn.Length != 11 || !tckn.All(char.IsDigit) || tckn[0] == '0')
            return false;

        int[] d = tckn.Select(c => c - '0').ToArray();

        int tekler = d[0] + d[2] + d[4] + d[6] + d[8];
        int ciftler = d[1] + d[3] + d[5] + d[7];

        int onuncu = (((tekler * 7) - ciftler) % 10 + 10) % 10;
        int onbirinci = d.Take(10).Sum() % 10;

        return d[9] == onuncu && d[10] == onbirinci;
    }
}
namespace RandevuSistemi.Models;

public class Sube
{
    private readonly List<CalismaSaati> _calismaSaatleri = new();
    private readonly List<Bolum> _bolumler = new();

    public string Ad { get; }
    public string Adres { get; }

    public IReadOnlyList<CalismaSaati> CalismaSaatleri => _calismaSaatleri;
    public IReadOnlyList<Bolum> Bolumler => _bolumler;

    public Sube(string ad, string adres)
    {
        if (string.IsNullOrWhiteSpace(ad))
            throw new ArgumentException("Şube adı boş olamaz.");
        Ad = ad;
        Adres = adres;
    }

    public void CalismaSaatiEkle(CalismaSaati saat)
    {
        // Aynı gün için ikinci bir kayıt eklenmesin
        if (_calismaSaatleri.Any(c => c.Gun == saat.Gun))
            throw new InvalidOperationException($"{saat.Gun} günü için çalışma saati zaten tanımlı.");
        _calismaSaatleri.Add(saat);
    }

    public void BolumEkle(Bolum bolum) => _bolumler.Add(bolum);

    // Şube verilen gün ve saat aralığında açık mı?
    public bool AcikMi(DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
        => _calismaSaatleri.Any(c => c.Gun == gun && c.IcindeMi(baslangic, bitis));

    public override string ToString() => $"{Ad} ({Adres})";

}
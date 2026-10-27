namespace RandevuSistemi.Models;

public class Doktor : Kullanici
{
    private readonly List<SaglikHizmeti> _hizmetler = new();
    private readonly List<UygunlukAraligi> _uygunluklar = new();

    public string Uzmanlik { get; }

    public IReadOnlyList<SaglikHizmeti> Hizmetler => _hizmetler;
    public IReadOnlyList<UygunlukAraligi> Uygunluklar => _uygunluklar;

    public Doktor(string ad, string soyad, string email, string sifre, string uzmanlik)
        : base(ad, soyad, email, sifre)
    {
        if (string.IsNullOrWhiteSpace(uzmanlik))
            throw new ArgumentException("Uzmanlık alanı boş olamaz.");
        Uzmanlik = uzmanlik;
    }

    public override string RolAdi => "Doktor";

    public override IReadOnlyList<string> Yetkiler => new[]
    {
        "Uygunluk saati tanımlama",
        "Kendi randevularını onaylama / reddetme",
        "Kendi randevularını görme"
    };

    public void HizmetEkle(SaglikHizmeti hizmet)
    {
        ArgumentNullException.ThrowIfNull(hizmet);
        if (!_hizmetler.Contains(hizmet))
            _hizmetler.Add(hizmet);
    }

    public bool HizmetSunuyorMu(SaglikHizmeti hizmet) => _hizmetler.Contains(hizmet);

    public void UygunlukEkle(UygunlukAraligi uygunluk)
    {
        ArgumentNullException.ThrowIfNull(uygunluk);

        // Kural 1: Doktor, şube kapalıyken çalışamaz
        if (!uygunluk.Sube.AcikMi(uygunluk.Gun, uygunluk.Baslangic, uygunluk.Bitis))
            throw new InvalidOperationException(
                "Uygunluk aralığı şubenin çalışma saatleri dışında olamaz.");

        // Kural 2: Aynı doktor aynı anda iki yerde olamaz
        var cakisan = _uygunluklar.FirstOrDefault(u => u.Cakisiyor(uygunluk));
        if (cakisan != null)
            throw new InvalidOperationException(
                $"Bu aralık mevcut bir uygunlukla çakışıyor: {cakisan}");

        _uygunluklar.Add(uygunluk);
    }

    // Doktor bu gün ve saatte müsait mi? Müsaitse hangi aralıkta?
    public UygunlukAraligi? UygunlukBul(DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
        => _uygunluklar.FirstOrDefault(u => u.Kapsiyor(gun, baslangic, bitis));

    public bool UygunMu(DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
        => UygunlukBul(gun, baslangic, bitis) != null;
}
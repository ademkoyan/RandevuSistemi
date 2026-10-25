namespace RandevuSistemi.Models;

public class Bolum
{
    private readonly List<SaglikHizmeti> _hizmetler = new();

    public string Ad { get; }

    // Dışarıya sadece okunabilir liste veriyoruz
    public IReadOnlyList<SaglikHizmeti> Hizmetler => _hizmetler;

    public Bolum(string ad)
    {
        if (string.IsNullOrWhiteSpace(ad))
            throw new ArgumentException("Bölüm adı boş olamaz.");
        Ad = ad;
    }

    public void HizmetEkle(SaglikHizmeti hizmet)
    {
        ArgumentNullException.ThrowIfNull(hizmet);
        _hizmetler.Add(hizmet);
    }

    public override string ToString() => Ad;
}
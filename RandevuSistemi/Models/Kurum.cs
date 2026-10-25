namespace RandevuSistemi.Models;

public class Kurum
{
    private readonly List<Sube> _subeler = new();

    public string Ad { get; }
    public IReadOnlyList<Sube> Subeler => _subeler;

    public Kurum(string ad)
    {
        if (string.IsNullOrWhiteSpace(ad))
            throw new ArgumentException("Kurum adı boş olamaz.");
        Ad = ad;
    }

    public void SubeEkle(Sube sube) => _subeler.Add(sube);

    public override string ToString() => Ad;

}
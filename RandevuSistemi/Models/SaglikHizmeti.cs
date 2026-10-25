namespace RandevuSistemi.Models;

public abstract class SaglikHizmeti
{
    private string _ad = "";
    private int _sureDakika;
    private decimal _ucret;

    public string Ad
    {
        get => _ad;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Hizmet adı boş olamaz.");
            _ad = value;
        }
    }

    public int SureDakika
    {
        get => _sureDakika;
        set
        {
            if (value <= 0)
                throw new ArgumentException("Hizmet süresi 0'dan büyük olmalıdır.");
            _sureDakika = value;
        }
    }

    public decimal Ucret
    {
        get => _ucret;
        set
        {
            if (value < 0)
                throw new ArgumentException("Ücret negatif olamaz.");
            _ucret = value;
        }
    }

    protected SaglikHizmeti(string ad, int sureDakika, decimal ucret)
    {
        Ad = ad;
        SureDakika = sureDakika;
        Ucret = ucret;
    }

    // Her alt sınıf bunu kendine göre yazmak ZORUNDA
    public abstract string TurAdi { get; }
    public abstract string HazirlikTalimati();

    public override string ToString()
        => $"[{TurAdi}] {Ad} - {SureDakika} dk - {Ucret:N2} TL";
}
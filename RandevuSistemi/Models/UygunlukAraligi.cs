namespace RandevuSistemi.Models;

// Bir doktorun belirli bir şubede, belirli bir günde çalıştığı saat aralığı
public class UygunlukAraligi
{
    public Sube Sube { get; }
    public DayOfWeek Gun { get; }
    public TimeOnly Baslangic { get; }
    public TimeOnly Bitis { get; }

    public UygunlukAraligi(Sube sube, DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
    {
        ArgumentNullException.ThrowIfNull(sube);
        if (bitis <= baslangic)
            throw new ArgumentException("Bitiş saati, başlangıç saatinden sonra olmalıdır.");

        Sube = sube;
        Gun = gun;
        Baslangic = baslangic;
        Bitis = bitis;
    }

    // Verilen aralık bu uygunluğun tamamen içinde mi?
    public bool Kapsiyor(DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
        => Gun == gun && baslangic >= Baslangic && bitis <= Bitis;

    // İki uygunluk aralığı çakışıyor mu? (bitişik olanlar çakışmaz)
    public bool Cakisiyor(UygunlukAraligi diger)
        => Gun == diger.Gun && Baslangic < diger.Bitis && Bitis > diger.Baslangic;

    public override string ToString()
        => $"{Sube.Ad} - {Gun} {Baslangic:HH:mm}-{Bitis:HH:mm}";
}
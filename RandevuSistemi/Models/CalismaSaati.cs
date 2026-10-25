namespace RandevuSistemi.Models;

public class CalismaSaati
{
    public DayOfWeek Gun { get; }
    public TimeOnly Acilis { get; }
    public TimeOnly Kapanis { get; }

    public CalismaSaati(DayOfWeek gun, TimeOnly acilis, TimeOnly kapanis)
    {
        if (kapanis <= acilis)
            throw new ArgumentException("Kapanış saati, açılış saatinden sonra olmalıdır.");

        Gun = gun;
        Acilis = acilis;
        Kapanis = kapanis;
    }

    // Verilen aralık bu çalışma saatinin içinde mi?
    public bool IcindeMi(TimeOnly baslangic, TimeOnly bitis)
        => baslangic >= Acilis && bitis <= Kapanis;

    public override string ToString() => $"{Gun}: {Acilis:HH:mm} - {Kapanis:HH:mm}";
}
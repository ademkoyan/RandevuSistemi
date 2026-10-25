namespace RandevuSistemi.Models;

public class RadyolojiIslemi:SaglikHizmeti
{
    public string Cihaz { get; }   // Ör: MR, Röntgen, Tomografi

    public RadyolojiIslemi(string ad, int sureDakika, decimal ucret, string cihaz)
        : base(ad, sureDakika, ucret)
    {
        Cihaz = cihaz;
    }

    public override string TurAdi => "Radyoloji";

    public override string HazirlikTalimati()
        => $"{Cihaz} çekimi öncesi metal eşyalarınızı (takı, saat, kemer) çıkarın. Hamileyseniz mutlaka bildirin.";

}
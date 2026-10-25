namespace RandevuSistemi.Models;

public class GenelMuayene:SaglikHizmeti
{
    public GenelMuayene(string ad, int sureDakika, decimal ucret)
        : base(ad, sureDakika, ucret) { }

    public override string TurAdi => "Genel Muayene";

    public override string HazirlikTalimati()
        => "Özel bir hazırlık gerekmez. Kimliğinizi ve varsa önceki tetkiklerinizi getirin.";
}
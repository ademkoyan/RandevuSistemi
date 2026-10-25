namespace RandevuSistemi.Models;

public class LaboratuvarTesti:SaglikHizmeti
{
    public bool AclikGerekli { get; }
    public int SonucSuresiSaat { get; }

    public LaboratuvarTesti(string ad, int sureDakika, decimal ucret,
        bool aclikGerekli, int sonucSuresiSaat)
        : base(ad, sureDakika, ucret)
    {
        AclikGerekli = aclikGerekli;
        SonucSuresiSaat = sonucSuresiSaat;
    }

    public override string TurAdi => "Laboratuvar";

    public override string HazirlikTalimati()
        => AclikGerekli
            ? $"Test öncesi 8 saat aç kalın (su içebilirsiniz). Sonuç yaklaşık {SonucSuresiSaat} saat içinde hazır olur."
            : $"Açlık gerekmez. Sonuç yaklaşık {SonucSuresiSaat} saat içinde hazır olur.";

}
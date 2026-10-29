using RandevuSistemi.Models;

namespace RandevuSistemi.Exceptions;

public class GecersizDurumGecisiException : RandevuSistemiException
{
    public RandevuDurumu MevcutDurum { get; }
    public RandevuDurumu HedefDurum { get; }

    public GecersizDurumGecisiException(RandevuDurumu mevcut, RandevuDurumu hedef)
        : base($"Geçersiz durum geçişi: {mevcut} → {hedef}")
    {
        MevcutDurum = mevcut;
        HedefDurum = hedef;
    }
}
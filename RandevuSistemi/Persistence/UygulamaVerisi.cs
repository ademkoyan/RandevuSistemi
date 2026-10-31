using RandevuSistemi.Models;
using RandevuSistemi.Services;

namespace RandevuSistemi.Persistence;

// Çalışan uygulamanın tüm durumu: kurum + kullanıcılar + randevular
public class UygulamaVerisi
{
    public Kurum Kurum { get; }
    public KimlikServisi KimlikServisi { get; }
    public RandevuServisi RandevuServisi { get; }

    public UygulamaVerisi(Kurum kurum, KimlikServisi kimlikServisi, RandevuServisi randevuServisi)
    {
        Kurum = kurum;
        KimlikServisi = kimlikServisi;
        RandevuServisi = randevuServisi;
    }
}
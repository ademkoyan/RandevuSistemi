using RandevuSistemi.Exceptions;
using RandevuSistemi.Models;

namespace RandevuSistemi.Services;

public class RandevuServisi
{
    private readonly List<Randevu> _randevular = new();
    private readonly Func<DateTime> _simdi;
    private readonly object _kilit = new();

    public IReadOnlyList<Randevu> Randevular => _randevular;

    // "Şimdi" dışarıdan verilebilir. Gün 6'daki testlerde sabit bir saat vereceğiz.
    public RandevuServisi(Func<DateTime>? simdi = null)
    {
        _simdi = simdi ?? (() => DateTime.Now);
    }

    // =====================================================================
    //  RANDEVU ALMA
    // =====================================================================
    public Randevu RandevuAl(Hasta hasta, Doktor doktor, SaglikHizmeti hizmet,
                             Sube sube, DateOnly tarih, TimeOnly baslangic)
    {
        ArgumentNullException.ThrowIfNull(hasta);
        ArgumentNullException.ThrowIfNull(doktor);
        ArgumentNullException.ThrowIfNull(hizmet);
        ArgumentNullException.ThrowIfNull(sube);

        var bitis = baslangic.AddMinutes(hizmet.SureDakika);
        if (bitis <= baslangic)
            throw new UygunsuzZamanException("Randevu gece yarısını geçemez.");

        // Kontrol + kayıt tek parça: iki kişi aynı slotu aynı anda alamaz.
        lock (_kilit)
        {
            // 1) Doktor bu hizmeti sunuyor mu?
            if (!doktor.HizmetSunuyorMu(hizmet))
                throw new RandevuSistemiException(
                    $"{doktor.TamAd}, '{hizmet.Ad}' hizmetini sunmuyor.");

            // 2) Geçmiş zaman mı?
            if (tarih.ToDateTime(baslangic) <= _simdi())
                throw new UygunsuzZamanException("Geçmiş bir zamana randevu alınamaz.");

            // 3) Doktor o şubede, o gün, o saatte çalışıyor mu?
            if (!DoktorUygunMu(doktor, sube, tarih.DayOfWeek, baslangic, bitis))
                throw new UygunsuzZamanException(
                    $"{doktor.TamAd}, {sube.Ad} şubesinde {tarih:dd.MM.yyyy} " +
                    $"{baslangic:HH:mm}-{bitis:HH:mm} aralığında çalışmıyor.");

            // 4) Şube açık mı?
            if (!sube.AcikMi(tarih.DayOfWeek, baslangic, bitis))
                throw new UygunsuzZamanException(
                    $"{sube.Ad} şubesi bu saatte kapalı.");

            // 5) Doktorun başka randevusu var mı?
            if (DoktorMesgulMu(doktor, tarih, baslangic, bitis))
                throw new RandevuCakismasiException(
                    $"{doktor.TamAd}'ın {baslangic:HH:mm}-{bitis:HH:mm} aralığında başka bir randevusu var.");

            // 6) Hastanın başka randevusu var mı? (başka doktor / başka şube olsa bile)
            if (HastaMesgulMu(hasta, tarih, baslangic, bitis))
                throw new RandevuCakismasiException(
                    "Hastanın bu saatte başka bir randevusu var.");

            var randevu = new Randevu(hasta, doktor, hizmet, sube, tarih, baslangic);
            _randevular.Add(randevu);
            return randevu;
        }
    }

    // =====================================================================
    //  BOŞ SAAT ÜRETİMİ
    //  adimDakika: slotların kaç dakikada bir başlayacağı (örn. 30 → 09:00, 09:30, ...)
    //  hasta verilirse hastanın kendi randevularıyla çakışan saatler de elenir.
    // =====================================================================
    public List<TimeOnly> UygunSaatleriGetir(Doktor doktor, SaglikHizmeti hizmet,
                                             Sube sube, DateOnly tarih,
                                             int adimDakika = 30, Hasta? hasta = null)
    {
        ArgumentNullException.ThrowIfNull(doktor);
        ArgumentNullException.ThrowIfNull(hizmet);
        ArgumentNullException.ThrowIfNull(sube);
        if (adimDakika <= 0)
            throw new ArgumentException("Adım süresi 0'dan büyük olmalıdır.");

        var sonuc = new List<TimeOnly>();
        if (!doktor.HizmetSunuyorMu(hizmet)) return sonuc;

        var simdi = _simdi();

        lock (_kilit)
        {
            var araliklar = doktor.Uygunluklar
                .Where(u => u.Sube == sube && u.Gun == tarih.DayOfWeek);

            foreach (var aralik in araliklar)
            {
                var bas = aralik.Baslangic;

                while (true)
                {
                    var bit = bas.AddMinutes(hizmet.SureDakika);

                    // Aralığın dışına taştıysa (ya da gece yarısını geçtiyse) dur
                    if (bit <= bas || bit > aralik.Bitis) break;

                    bool uygun =
                        tarih.ToDateTime(bas) > simdi &&
                        sube.AcikMi(tarih.DayOfWeek, bas, bit) &&
                        !DoktorMesgulMu(doktor, tarih, bas, bit) &&
                        (hasta == null || !HastaMesgulMu(hasta, tarih, bas, bit));

                    if (uygun) sonuc.Add(bas);

                    var sonraki = bas.AddMinutes(adimDakika);
                    if (sonraki <= bas) break;   // gece yarısı koruması
                    bas = sonraki;
                }
            }
        }

        return sonuc.Distinct().OrderBy(s => s).ToList();
    }

    // =====================================================================
    //  DURUM İŞLEMLERİ (asıl kurallar Randevu sınıfının içinde)
    // =====================================================================
    public Randevu RandevuBul(Guid id)
        => _randevular.FirstOrDefault(r => r.Id == id)
           ?? throw new RandevuSistemiException("Randevu bulunamadı.");

    public void Onayla(Guid randevuId, Kullanici islemYapan) => RandevuBul(randevuId).Onayla(islemYapan);
    public void Reddet(Guid randevuId, Kullanici islemYapan) => RandevuBul(randevuId).Reddet(islemYapan);
    public void Tamamla(Guid randevuId, Kullanici islemYapan) => RandevuBul(randevuId).Tamamla(islemYapan);
    public void Iptal(Guid randevuId, Kullanici islemYapan) => RandevuBul(randevuId).Iptal(islemYapan);

    // =====================================================================
    //  SORGULAR (Gün 8'deki menüler bunları kullanacak)
    // =====================================================================
    public IReadOnlyList<Randevu> HastaRandevulari(Hasta hasta)
        => _randevular.Where(r => r.Hasta.Id == hasta.Id)
                      .OrderBy(r => r.BaslangicZamani).ToList();

    public IReadOnlyList<Randevu> DoktorRandevulari(Doktor doktor, RandevuDurumu? durum = null)
        => _randevular.Where(r => r.Doktor.Id == doktor.Id &&
                                  (durum == null || r.Durum == durum))
                      .OrderBy(r => r.BaslangicZamani).ToList();

    // =====================================================================
    //  ÖZEL YARDIMCILAR
    // =====================================================================
    private static bool DoktorUygunMu(Doktor doktor, Sube sube, DayOfWeek gun,
                                      TimeOnly baslangic, TimeOnly bitis)
        => doktor.Uygunluklar.Any(u => u.Sube == sube && u.Kapsiyor(gun, baslangic, bitis));

    // Randevu.Cakisiyor zaten "sadece aktif randevular" kuralını içeriyor:
    // iptal / red / tamamlanmış randevular slotu tutmaz.
    private bool DoktorMesgulMu(Doktor doktor, DateOnly tarih, TimeOnly baslangic, TimeOnly bitis)
        => _randevular.Any(r => r.Doktor.Id == doktor.Id && r.Cakisiyor(tarih, baslangic, bitis));

    private bool HastaMesgulMu(Hasta hasta, DateOnly tarih, TimeOnly baslangic, TimeOnly bitis)
        => _randevular.Any(r => r.Hasta.Id == hasta.Id && r.Cakisiyor(tarih, baslangic, bitis));
}

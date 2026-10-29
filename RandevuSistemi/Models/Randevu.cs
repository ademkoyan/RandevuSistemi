using RandevuSistemi.Exceptions;

namespace RandevuSistemi.Models;

public class Randevu
{
    // Hangi durumdan hangi durumlara geçilebilir? (durum makinesinin tamamı bu tabloda)
    private static readonly Dictionary<RandevuDurumu, RandevuDurumu[]> IzinliGecisler = new()
    {
        [RandevuDurumu.Beklemede]   = new[] { RandevuDurumu.Onaylandi, RandevuDurumu.Reddedildi, RandevuDurumu.IptalEdildi },
        [RandevuDurumu.Onaylandi]   = new[] { RandevuDurumu.Tamamlandi, RandevuDurumu.IptalEdildi },
        [RandevuDurumu.Reddedildi]  = Array.Empty<RandevuDurumu>(),
        [RandevuDurumu.IptalEdildi] = Array.Empty<RandevuDurumu>(),
        [RandevuDurumu.Tamamlandi]  = Array.Empty<RandevuDurumu>()
    };

    public Guid Id { get; } = Guid.NewGuid();

    // --- Kimler ve ne ---
    public Hasta Hasta { get; }
    public Doktor Doktor { get; }
    public SaglikHizmeti Hizmet { get; }
    public Sube Sube { get; }

    // --- Zaman ---
    public DateOnly Tarih { get; }
    public TimeOnly Baslangic { get; }
    public TimeOnly Bitis { get; }

    // --- Snapshot: randevu anındaki değerler, sonradan değişmez ---
    public string HizmetAdi { get; }
    public int SureDakika { get; }
    public decimal Ucret { get; }

    // --- Durum: dışarıdan okunur ama SADECE bu sınıf değiştirebilir ---
    public RandevuDurumu Durum { get; private set; } = RandevuDurumu.Beklemede;
    public DateTime OlusturmaZamani { get; } = DateTime.Now;

    public Randevu(Hasta hasta, Doktor doktor, SaglikHizmeti hizmet,
                   Sube sube, DateOnly tarih, TimeOnly baslangic)
    {
        ArgumentNullException.ThrowIfNull(hasta);
        ArgumentNullException.ThrowIfNull(doktor);
        ArgumentNullException.ThrowIfNull(hizmet);
        ArgumentNullException.ThrowIfNull(sube);

        Hasta = hasta;
        Doktor = doktor;
        Hizmet = hizmet;
        Sube = sube;
        Tarih = tarih;
        Baslangic = baslangic;

        // Snapshot: o anki değerleri kopyala
        HizmetAdi = hizmet.Ad;
        SureDakika = hizmet.SureDakika;
        Ucret = hizmet.Ucret;

        Bitis = baslangic.AddMinutes(SureDakika);
        if (Bitis <= baslangic)
            throw new ArgumentException("Randevu gece yarısını geçemez.");
    }

    // ---------- Sorgular ----------

    // Beklemede veya Onaylandi ise slot doludur. İptal/Red/Tamamlandı slotu tutmaz.
    public bool AktifMi => Durum is RandevuDurumu.Beklemede or RandevuDurumu.Onaylandi;

    public DateTime BaslangicZamani => Tarih.ToDateTime(Baslangic);
    public DateTime BitisZamani => Tarih.ToDateTime(Bitis);

    // Gün 5'te çakışma kontrolünde kullanılacak.
    // Bitişik aralıklar (09:00-09:30 ve 09:30-10:00) çakışmaz.
    public bool Cakisiyor(DateOnly tarih, TimeOnly baslangic, TimeOnly bitis)
        => AktifMi && Tarih == tarih && Baslangic < bitis && Bitis > baslangic;

    // ---------- Davranışlar (durum geçişleri) ----------

    public void Onayla(Kullanici islemYapan)
    {
        DoktorVeyaYoneticiMi(islemYapan, "onaylayabilir");
        GecisYap(RandevuDurumu.Onaylandi);
    }

    public void Reddet(Kullanici islemYapan)
    {
        DoktorVeyaYoneticiMi(islemYapan, "reddedebilir");
        GecisYap(RandevuDurumu.Reddedildi);
    }

    public void Tamamla(Kullanici islemYapan)
    {
        DoktorVeyaYoneticiMi(islemYapan, "tamamlandı olarak işaretleyebilir");
        GecisYap(RandevuDurumu.Tamamlandi);
    }

    public void Iptal(Kullanici islemYapan)
    {
        ArgumentNullException.ThrowIfNull(islemYapan);

        bool kendiHastasi = islemYapan is Hasta h && h.Id == Hasta.Id;
        bool yonetici = islemYapan is Yonetici;

        if (!kendiHastasi && !yonetici)
            throw new YetkisizIslemException(
                "Randevuyu sadece randevunun sahibi hasta veya yönetici iptal edebilir.");

        GecisYap(RandevuDurumu.IptalEdildi);
    }

    // ---------- Özel yardımcılar ----------

    private void DoktorVeyaYoneticiMi(Kullanici islemYapan, string eylem)
    {
        ArgumentNullException.ThrowIfNull(islemYapan);

        bool ilgiliDoktor = islemYapan is Doktor d && d.Id == Doktor.Id;
        bool yonetici = islemYapan is Yonetici;

        if (!ilgiliDoktor && !yonetici)
            throw new YetkisizIslemException(
                $"Randevuyu sadece ilgili doktor veya yönetici {eylem}.");
    }

    private void GecisYap(RandevuDurumu hedef)
    {
        if (!IzinliGecisler[Durum].Contains(hedef))
            throw new GecersizDurumGecisiException(Durum, hedef);

        Durum = hedef;
    }

    public override string ToString()
        => $"{Tarih:dd.MM.yyyy} {Baslangic:HH:mm}-{Bitis:HH:mm} | {HizmetAdi} | " +
           $"{Doktor.TamAd} / {Hasta.TamAd} | {Ucret:N2} TL | {Durum}";
}
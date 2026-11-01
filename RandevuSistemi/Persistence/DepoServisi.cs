using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using RandevuSistemi.Models;
using RandevuSistemi.Services;

namespace RandevuSistemi.Persistence;

public class DepoServisi
{
    private static readonly JsonSerializerOptions Ayarlar = new()
    {
        WriteIndented = true,                                        // okunaklı dosya
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,       // Türkçe karakterler bozulmasın
        Converters = { new JsonStringEnumConverter() }               // enum'lar sayı değil, ad olarak yazılsın
    };

    public string DosyaYolu { get; }

    public DepoServisi(string dosyaYolu)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dosyaYolu);
        DosyaYolu = dosyaYolu;
    }

    public static string VarsayilanYol() => Path.Combine(AppContext.BaseDirectory, "veri.json");

    // =====================================================================
    //  YÜKLE / KAYDET
    // =====================================================================
    public UygulamaVerisi Yukle()
    {
        // Dosya yoksa: örnek veriyle başla ve hemen kaydet
        if (!File.Exists(DosyaYolu))
        {
            var ornek = OrnekVeri.Olustur();
            Kaydet(ornek);
            return ornek;
        }

        string json = File.ReadAllText(DosyaYolu);
        var dosya = JsonSerializer.Deserialize<VeriDosyasi>(json, Ayarlar)
                    ?? throw new InvalidDataException("Veri dosyası okunamadı.");
        return DosyadanKur(dosya);
    }

    public void Kaydet(UygulamaVerisi veri)
    {
        ArgumentNullException.ThrowIfNull(veri);

        string json = JsonSerializer.Serialize(DosyayaCevir(veri), Ayarlar);

        var klasor = Path.GetDirectoryName(Path.GetFullPath(DosyaYolu));
        if (!string.IsNullOrEmpty(klasor)) Directory.CreateDirectory(klasor);

        // Atomik yazma: önce geçici dosya, sonra üzerine taşı
        string gecici = DosyaYolu + ".tmp";
        File.WriteAllText(gecici, json);
        File.Move(gecici, DosyaYolu, overwrite: true);
    }

    // =====================================================================
    //  NESNE → DTO (kaydederken)
    // =====================================================================
    private static VeriDosyasi DosyayaCevir(UygulamaVerisi veri)
    {
        var dosya = new VeriDosyasi { KurumAdi = veri.Kurum.Ad };

        foreach (var sube in veri.Kurum.Subeler)
        {
            dosya.Subeler.Add(new SubeDto
            {
                Id = sube.Id,
                Ad = sube.Ad,
                Adres = sube.Adres,
                CalismaSaatleri = sube.CalismaSaatleri
                    .Select(c => new CalismaSaatiDto { Gun = c.Gun, Acilis = c.Acilis, Kapanis = c.Kapanis })
                    .ToList(),
                Bolumler = sube.Bolumler
                    .Select(b => new BolumDto
                    {
                        Ad = b.Ad,
                        Hizmetler = b.Hizmetler.Select(HizmetDtoOlustur).ToList()
                    })
                    .ToList()
            });
        }

        foreach (var kullanici in veri.KimlikServisi.Kullanicilar)
            dosya.Kullanicilar.Add(KullaniciDtoOlustur(kullanici));

        foreach (var r in veri.RandevuServisi.Randevular)
        {
            dosya.Randevular.Add(new RandevuDto
            {
                Id = r.Id,
                HastaId = r.Hasta.Id,
                DoktorId = r.Doktor.Id,
                HizmetId = r.Hizmet.Id,
                SubeId = r.Sube.Id,
                Tarih = r.Tarih,
                Baslangic = r.Baslangic,
                HizmetAdi = r.HizmetAdi,
                SureDakika = r.SureDakika,
                Ucret = r.Ucret,
                Durum = r.Durum,
                OlusturmaZamani = r.OlusturmaZamani
            });
        }

        return dosya;
    }

    private static HizmetDto HizmetDtoOlustur(SaglikHizmeti hizmet)
    {
        var dto = new HizmetDto
        {
            Id = hizmet.Id,
            Ad = hizmet.Ad,
            SureDakika = hizmet.SureDakika,
            Ucret = hizmet.Ucret
        };

        switch (hizmet)
        {
            case RadyolojiIslemi radyoloji:
                dto.Tur = HizmetTurleri.Radyoloji;
                dto.Cihaz = radyoloji.Cihaz;
                break;
            case LaboratuvarTesti lab:
                dto.Tur = HizmetTurleri.Laboratuvar;
                dto.AclikGerekli = lab.AclikGerekli;
                dto.SonucSuresiSaat = lab.SonucSuresiSaat;
                break;
            case GenelMuayene:
                dto.Tur = HizmetTurleri.GenelMuayene;
                break;
            default:
                throw new NotSupportedException($"Kaydedilemeyen hizmet türü: {hizmet.GetType().Name}");
        }

        return dto;
    }

    private static KullaniciDto KullaniciDtoOlustur(Kullanici kullanici)
    {
        var dto = new KullaniciDto
        {
            Id = kullanici.Id,
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            Email = kullanici.Email,
            SifreHash = kullanici.SifreHash,   // düz şifre değil, hash
            Tuz = kullanici.Tuz
        };

        switch (kullanici)
        {
            case Hasta hasta:
                dto.Rol = Roller.Hasta;
                dto.Tckn = hasta.Tckn;
                dto.DogumTarihi = hasta.DogumTarihi;
                break;
            case Doktor doktor:
                dto.Rol = Roller.Doktor;
                dto.Uzmanlik = doktor.Uzmanlik;
                dto.HizmetIdleri = doktor.Hizmetler.Select(h => h.Id).ToList();
                dto.Uygunluklar = doktor.Uygunluklar
                    .Select(u => new UygunlukDto
                    {
                        SubeId = u.Sube.Id,
                        Gun = u.Gun,
                        Baslangic = u.Baslangic,
                        Bitis = u.Bitis
                    })
                    .ToList();
                break;
            case Yonetici:
                dto.Rol = Roller.Yonetici;
                break;
            default:
                throw new NotSupportedException($"Kaydedilemeyen kullanıcı türü: {kullanici.GetType().Name}");
        }

        return dto;
    }

    // =====================================================================
    //  DTO → NESNE (yüklerken). Sıra önemli:
    //  1) şubeler/hizmetler  2) kullanıcılar  3) randevular
    // =====================================================================
    private static UygulamaVerisi DosyadanKur(VeriDosyasi dosya)
    {
        var kurum = new Kurum(dosya.KurumAdi);
        var subeler = new Dictionary<Guid, Sube>();
        var hizmetler = new Dictionary<Guid, SaglikHizmeti>();

        // --- 1) Şubeler, bölümler, hizmetler ---
        foreach (var sd in dosya.Subeler)
        {
            var sube = new Sube(sd.Ad, sd.Adres) { Id = sd.Id };

            foreach (var c in sd.CalismaSaatleri)
                sube.CalismaSaatiEkle(new CalismaSaati(c.Gun, c.Acilis, c.Kapanis));

            foreach (var bd in sd.Bolumler)
            {
                var bolum = new Bolum(bd.Ad);
                foreach (var hd in bd.Hizmetler)
                {
                    var hizmet = HizmetOlustur(hd);
                    bolum.HizmetEkle(hizmet);
                    hizmetler[hizmet.Id] = hizmet;
                }
                sube.BolumEkle(bolum);
            }

            kurum.SubeEkle(sube);
            subeler[sube.Id] = sube;
        }

        // --- 2) Kullanıcılar ---
        var kimlik = new KimlikServisi();
        var hastalar = new Dictionary<Guid, Hasta>();
        var doktorlar = new Dictionary<Guid, Doktor>();

        foreach (var kd in dosya.Kullanicilar)
        {
            var kullanici = KullaniciOlustur(kd, kurum, subeler, hizmetler);
            kimlik.KullaniciKaydet(kullanici);

            if (kullanici is Hasta h) hastalar[h.Id] = h;
            else if (kullanici is Doktor d) doktorlar[d.Id] = d;
        }

        // --- 3) Randevular ---
        var randevuServisi = new RandevuServisi();

        foreach (var rd in dosya.Randevular)
        {
            if (!hastalar.TryGetValue(rd.HastaId, out var hasta) ||
                !doktorlar.TryGetValue(rd.DoktorId, out var doktor) ||
                !hizmetler.TryGetValue(rd.HizmetId, out var hizmet) ||
                !subeler.TryGetValue(rd.SubeId, out var sube))
                throw new InvalidDataException($"Randevu {rd.Id} geçersiz bir kayda bağlı.");

            randevuServisi.RandevuYukle(new Randevu(
                rd.Id, hasta, doktor, hizmet, sube, rd.Tarih, rd.Baslangic,
                rd.HizmetAdi, rd.SureDakika, rd.Ucret, rd.Durum, rd.OlusturmaZamani));
        }

        return new UygulamaVerisi(kurum, kimlik, randevuServisi);
    }

    private static SaglikHizmeti HizmetOlustur(HizmetDto d) => d.Tur switch
    {
        HizmetTurleri.GenelMuayene
            => new GenelMuayene(d.Ad, d.SureDakika, d.Ucret) { Id = d.Id },
        HizmetTurleri.Radyoloji
            => new RadyolojiIslemi(d.Ad, d.SureDakika, d.Ucret, d.Cihaz ?? "") { Id = d.Id },
        HizmetTurleri.Laboratuvar
            => new LaboratuvarTesti(d.Ad, d.SureDakika, d.Ucret, d.AclikGerekli, d.SonucSuresiSaat) { Id = d.Id },
        _ => throw new InvalidDataException($"Bilinmeyen hizmet türü: {d.Tur}")
    };

    private static Kullanici KullaniciOlustur(KullaniciDto kd, Kurum kurum,
        IReadOnlyDictionary<Guid, Sube> subeler, IReadOnlyDictionary<Guid, SaglikHizmeti> hizmetler)
    {
        switch (kd.Rol)
        {
            case Roller.Hasta:
                return new Hasta(kd.Id, kd.Ad, kd.Soyad, kd.Email, kd.SifreHash, kd.Tuz,
                                 kd.Tckn ?? "", kd.DogumTarihi ?? default);

            case Roller.Doktor:
            {
                var doktor = new Doktor(kd.Id, kd.Ad, kd.Soyad, kd.Email, kd.SifreHash, kd.Tuz,
                                        kd.Uzmanlik ?? "");

                foreach (var hizmetId in kd.HizmetIdleri)
                    if (hizmetler.TryGetValue(hizmetId, out var hizmet))
                        doktor.HizmetEkle(hizmet);

                foreach (var u in kd.Uygunluklar)
                    if (subeler.TryGetValue(u.SubeId, out var sube))
                        doktor.UygunlukEkle(new UygunlukAraligi(sube, u.Gun, u.Baslangic, u.Bitis));

                return doktor;
            }

            case Roller.Yonetici:
                return new Yonetici(kd.Id, kd.Ad, kd.Soyad, kd.Email, kd.SifreHash, kd.Tuz, kurum);

            default:
                throw new InvalidDataException($"Bilinmeyen kullanıcı rolü: {kd.Rol}");
        }
    }
}
using RandevuSistemi.Models;
using RandevuSistemi.Persistence;

namespace RandevuSistemi.UI;

public sealed class DoktorMenusu : KullaniciMenusu
{
    private readonly Doktor _doktor;

    public DoktorMenusu(UygulamaVerisi veri, DepoServisi depo, Doktor doktor)
        : base(veri, depo, doktor)
    {
        _doktor = doktor;
    }

    protected override string Baslik => $"DOKTOR MENÜSÜ ({_doktor.Uzmanlik})";

    protected override List<MenuSecenegi> SecenekleriOlustur() => new()
    {
        new MenuSecenegi("Onay bekleyen randevular (onayla / reddet)", BekleyenleriYonet),
        new MenuSecenegi("Tüm randevularım", RandevularimiListele),
        new MenuSecenegi("Randevuyu tamamlandı olarak işaretle", RandevuTamamla),
        new MenuSecenegi("Uygunluk saati ekle", () => UygunlukEkleAkisi(_doktor)),
        new MenuSecenegi("Uygunluk saatlerim", () => UygunlukYazdir(_doktor)),
        new MenuSecenegi("Sunduğum hizmetler", HizmetlerimiListele)
    };

    private void BekleyenleriYonet()
    {
        Konsol.Baslik("Onay Bekleyen Randevular");
        var bekleyenler = Veri.RandevuServisi.DoktorRandevulari(_doktor, RandevuDurumu.Beklemede);

        var secilen = Konsol.Sec("Randevu seçin", bekleyenler,
            r => Konsol.RandevuMetni(r, hastayiGoster: true, doktoruGoster: false));
        if (secilen is null) return;

        Console.WriteLine("\n  1) Onayla\n  2) Reddet\n  0) Vazgeç");
        switch (Konsol.SayiOku("İşlem"))
        {
            case 1:
                Veri.RandevuServisi.Onayla(secilen.Id, _doktor);
                Kaydet();
                Konsol.Basari("Randevu onaylandı.");
                break;
            case 2:
                Veri.RandevuServisi.Reddet(secilen.Id, _doktor);
                Kaydet();
                Konsol.Basari("Randevu reddedildi.");
                break;
            case 0:
                break;
            default:
                Konsol.Hata("Geçersiz seçim.");
                break;
        }
    }

    private void RandevularimiListele()
    {
        Konsol.Baslik("Randevularım");
        var liste = Veri.RandevuServisi.DoktorRandevulari(_doktor);
        Konsol.Liste("Tüm randevular", liste,
            r => Konsol.RandevuMetni(r, hastayiGoster: true, doktoruGoster: false));
    }

    private void RandevuTamamla()
    {
        Konsol.Baslik("Randevu Tamamla");
        var onaylananlar = Veri.RandevuServisi.DoktorRandevulari(_doktor, RandevuDurumu.Onaylandi);

        var secilen = Konsol.Sec("Tamamlanan randevu", onaylananlar,
            r => Konsol.RandevuMetni(r, hastayiGoster: true, doktoruGoster: false));
        if (secilen is null) return;

        Veri.RandevuServisi.Tamamla(secilen.Id, _doktor);
        Kaydet();
        Konsol.Basari("Randevu tamamlandı olarak işaretlendi.");
    }

    private void HizmetlerimiListele()
    {
        Konsol.Baslik("Sunduğum Hizmetler");
        Konsol.Liste($"Uzmanlık: {_doktor.Uzmanlik}", _doktor.Hizmetler,
            h => $"{h.Ad} [{h.TurAdi}] - {h.SureDakika} dk - {h.Ucret:N2} TL");
    }
}
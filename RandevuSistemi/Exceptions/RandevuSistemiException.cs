namespace RandevuSistemi.Exceptions;

// Projedeki tüm iş kuralı hatalarının ortak atası.
// İstenirse tek bir catch ile hepsi yakalanabilir.
public class RandevuSistemiException : Exception
{
    public RandevuSistemiException(string mesaj) : base(mesaj) { }
}
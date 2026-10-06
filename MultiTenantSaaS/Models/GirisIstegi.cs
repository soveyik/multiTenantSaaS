namespace MultiTenantSaaS.Models
{
    // login işlemi içn dışarıdan beklediğimiz veriler
    public class GirisIstegi
    {
        public string FirmaKodu { get; set; } = string.Empty;
        public string KullaniciAdi { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}

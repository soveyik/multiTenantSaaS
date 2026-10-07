namespace MultiTenantSaaS.Models
{
    // elasticsearch urun modeli
    public class UrunAramaModeli
    {
        // sql id
        public Guid Id { get; set; }
        
        // tenant id
        public Guid TenantId { get; set; }

        public string Ad { get; set; } = string.Empty;
        public string UrunKodu { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Fiyat { get; set; }
    }
}

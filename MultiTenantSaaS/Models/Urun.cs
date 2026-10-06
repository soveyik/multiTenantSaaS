using System.ComponentModel.DataAnnotations;

namespace MultiTenantSaaS.Models
{
    public class Urun
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; } 
        
        public string Ad { get; set; } = string.Empty;
        public string UrunKodu { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Fiyat { get; set; }
    }
}

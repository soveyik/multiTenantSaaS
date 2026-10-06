using System.ComponentModel.DataAnnotations;

namespace MultiTenantSaaS.Models
{
    public class SiparisDetay
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SiparisId { get; set; }
        public Siparis? Siparis { get; set; }

        public Guid UrunId { get; set; }
        public Urun? Urun { get; set; }

        public int Adet { get; set; }
        public decimal BirimFiyat { get; set; }
    }
}

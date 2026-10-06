using System.ComponentModel.DataAnnotations;

namespace MultiTenantSaaS.Models
{
    public class SepetDetay
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }
        public Guid KullaniciId { get; set; }

        public Guid UrunId { get; set; }
        public Urun? Urun { get; set; }

        public int Adet { get; set; }
    }
}

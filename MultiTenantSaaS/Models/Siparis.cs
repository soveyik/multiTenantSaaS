using System.ComponentModel.DataAnnotations;

namespace MultiTenantSaaS.Models
{
    public class Siparis
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; } 
        public Guid KullaniciId { get; set; }
        public Kullanici? Kullanici { get; set; }

        public DateTime SiparisTarihi { get; set; } = DateTime.Now;
        public decimal ToplamTutar { get; set; }

        public string Adres { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;

        public List<SiparisDetay> SiparisDetaylari { get; set; } = new List<SiparisDetay>();
    }
}

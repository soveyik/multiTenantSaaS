using System.ComponentModel.DataAnnotations;

namespace MultiTenantSaaS.Models
{
    // veritabanındaki firmalar tablosu 
    public class Firma
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // firma kodları
        public string FirmaKodu { get; set; } = string.Empty;

        // port bilgisi
        public string Port { get; set; } = string.Empty;

        // firmaya özel ayarlar tema rengi
        public string Ayar { get; set; } = string.Empty;
    }
}

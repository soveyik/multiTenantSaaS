using System.ComponentModel.DataAnnotations;

namespace MultiTenantSaaS.Models
{
    // sistemdeki kullanıcıları tuttuğumuz model
    public class Kullanici
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        //kullanıcının hangi firmaya ait olduğu bilgisi
        public Guid FirmaId { get; set; }
        public Firma? Firma { get; set; }

        //farklı firmalarda aynı isimde kullanıcılar olabileceği için firmayla birlikte kontrol edeceğiz
        public string KullaniciAdi { get; set; } = string.Empty;

        //şimdilik şifreleri düz metin tutuluyor sonradan hashleme yapılır
        public string Sifre { get; set; } = string.Empty;
    }
}

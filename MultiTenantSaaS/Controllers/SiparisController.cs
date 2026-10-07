using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Data;
using MultiTenantSaaS.Models;

namespace MultiTenantSaaS.Controllers
{
    [Authorize]
    public class SiparisController : Controller
    {
        private readonly UygulamaDbContext _context;

        public SiparisController(UygulamaDbContext context)
        {
            _context = context;
        }

        // aktif firmanin id bilgisi
        private Guid GetFirmaId() => Guid.Parse(User.FindFirst("FirmaId")!.Value);
        
        // cookie'den giriş yapan kullanıcının kendi id bilgisini alıyoruz
        private Guid GetKullaniciId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        [HttpPost]
        public IActionResult SepeteEkle(Guid urunId, int adet = 1)
        {
            var firmaId = GetFirmaId();
            var kullaniciId = GetKullaniciId();

            // yetki kontrolu
            var urun = _context.Urunler.FirstOrDefault(p => p.Id == urunId && p.TenantId == firmaId);
            if (urun == null) return NotFound("Ürün bulunamadı veya bu firmaya ait değil.");

            // mevcut urun kontrolu
            var sepetDetay = _context.SepetDetaylari.FirstOrDefault(c => c.UrunId == urunId && c.KullaniciId == kullaniciId && c.TenantId == firmaId);
            
            if (sepetDetay != null)
            {
                // adet artisi
                sepetDetay.Adet += adet;
            }
            else
            {
                // yeni urun kaydi
                _context.SepetDetaylari.Add(new SepetDetay
                {
                    Id = Guid.NewGuid(),
                    TenantId = firmaId,
                    KullaniciId = kullaniciId,
                    UrunId = urunId,
                    Adet = adet
                });
            }
            
            _context.SaveChanges();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Sepetim()
        {
            var firmaId = GetFirmaId();
            var kullaniciId = GetKullaniciId();

            // guncel sepet verisi
            var sepetDetaylari = _context.SepetDetaylari
                .Include(c => c.Urun)
                .Where(c => c.TenantId == firmaId && c.KullaniciId == kullaniciId)
                .ToList();

            return View(sepetDetaylari);
        }

        [HttpPost]
        public IActionResult SiparisOlustur(string adres, string telefon)
        {
            var firmaId = GetFirmaId();
            var kullaniciId = GetKullaniciId();

            // sepetteki urunler
            var sepetDetaylari = _context.SepetDetaylari
                .Include(c => c.Urun)
                .Where(c => c.TenantId == firmaId && c.KullaniciId == kullaniciId)
                .ToList();

            if (!sepetDetaylari.Any()) return RedirectToAction("Sepetim");

            // ana siparis kaydi
            var siparis = new Siparis
            {
                Id = Guid.NewGuid(),
                TenantId = firmaId,
                KullaniciId = kullaniciId,
                SiparisTarihi = DateTime.Now,
                ToplamTutar = sepetDetaylari.Sum(c => c.Adet * c.Urun!.Fiyat),
                Adres = adres ?? "",
                Telefon = telefon ?? ""
            };

            // siparis detaylarinin eklenmesi
            foreach (var item in sepetDetaylari)
            {
                siparis.SiparisDetaylari.Add(new SiparisDetay
                {
                    Id = Guid.NewGuid(),
                    UrunId = item.UrunId,
                    Adet = item.Adet,
                    BirimFiyat = item.Urun!.Fiyat // anlik fiyat kaydi
                });
            }

            _context.Siparisler.Add(siparis);
            
            // sepeti temizleme islemi
            _context.SepetDetaylari.RemoveRange(sepetDetaylari); 
            _context.SaveChanges();

            return RedirectToAction("Siparislerim");
        }

        public IActionResult Siparislerim()
        {
            var firmaId = GetFirmaId();
            var kullaniciId = GetKullaniciId();

            // filtreleme islemine gore siparis listesi
            var siparisler = _context.Siparisler
                .Include(o => o.SiparisDetaylari)
                .ThenInclude(oi => oi.Urun)
                .Where(o => o.TenantId == firmaId && o.KullaniciId == kullaniciId)
                .OrderByDescending(o => o.SiparisTarihi)
                .ToList();

            return View(siparisler);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Data;

namespace MultiTenantSaaS.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly UygulamaDbContext _context;

        public AdminController(UygulamaDbContext context)
        {
            _context = context;
        }

        // cookie'den giriş yapan kullanıcının firmasını buluyoruz
        private Guid GetFirmaId() => Guid.Parse(User.FindFirst("FirmaId")!.Value);

        public IActionResult Siparisler()
        {
            var firmaId = GetFirmaId();

            // sadece mevcut firmanın tüm siparişleri çekiliyor
            var siparisler = _context.Siparisler
                .Include(o => o.Kullanici)
                .Include(o => o.SiparisDetaylari)
                .ThenInclude(oi => oi.Urun)
                .Where(o => o.TenantId == firmaId)
                .OrderByDescending(o => o.SiparisTarihi)
                .ToList();

            return View(siparisler);
        }

        public IActionResult Kullanicilar()
        {
            var firmaId = GetFirmaId();

            // sadece bu firmaya kayıtlı kullanıcıları listeliyoruz
            var kullanicilar = _context.Kullanicilar
                .Where(k => k.FirmaId == firmaId)
                .ToList();

            return View(kullanicilar);
        }
    }
}

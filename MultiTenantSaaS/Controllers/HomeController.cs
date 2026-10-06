using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Data;

namespace MultiTenantSaaS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UygulamaDbContext _context;

        public HomeController(UygulamaDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var firmaIdStr = User.FindFirst("FirmaId")?.Value;
            if (string.IsNullOrEmpty(firmaIdStr)) return RedirectToAction("Login", "Account");

            var firmaId = Guid.Parse(firmaIdStr);
            
            // veritabanından sadece bulunduğumuz firmaya ait ürünleri çekiyoruz
            var urunler = _context.Urunler.Where(p => p.TenantId == firmaId).ToList();

            // firmanın tema veya diğer ayarlarını view'a yolluyoruz
            var firma = _context.Firmalar.Find(firmaId);
            ViewBag.Ayar = firma?.Ayar; 

            return View(urunler);
        }
    }
}

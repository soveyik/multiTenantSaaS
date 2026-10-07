using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Data;
using MultiTenantSaaS.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace MultiTenantSaaS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UygulamaDbContext _context;

        public AccountController(UygulamaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login() //giriş formunu getirir
        {
            return View();
        }

        [HttpPost]  //giriş butonuna bastıktan sonra çalışan metot
        public async Task<IActionResult> Login(string firmaKodu, string kullaniciAdi, string sifre)
        {
            // kullanıcının girdiği firma koduna göre aktif firma var mı korntolü
            var firma = _context.Firmalar.FirstOrDefault(f => f.FirmaKodu == firmaKodu);
            if (firma == null)
            {
                ViewBag.Error = "Firma bulunamadı.";
                return View();
            }

            // kullanıcıyı önce firma ve kullanıcı adına göre buluyoruz
            var kullanici = _context.Kullanicilar.FirstOrDefault(k => k.FirmaId == firma.Id && k.KullaniciAdi == kullaniciAdi);
            
            if (kullanici == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }
            // ASP.NET Core Identity'nin resmi şifre doğrulama aracını ayağa kaldırıyoruz
            // şifre doğrulama işlemi yapıyoruz (hash kontrolü)
            var hasher = new PasswordHasher<Kullanici>();
            var passwordResult = hasher.VerifyHashedPassword(kullanici, kullanici.Sifre, sifre);// Kullanıcının formda yazdığı düz şifreyi ('sifre'), veritabanındaki hash'li metinle ('kullanici.Sifre') kıyaslıyoruz.

            if (passwordResult != PasswordVerificationResult.Success)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            // giriş basarılı olfuğunda çerezlere yazılacak bilgiler hazırlanır
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
                new Claim("FirmaId", firma.Id.ToString())
            };

            //claim listesi ile bir kimlik oluşturuluyor
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); 
            var principal = new ClaimsPrincipal(identity); //kimliğin sahibi

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal); //çerezlere yazıoz

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string firmaKodu, string kullaniciAdi, string sifre)
        {
            // kayıt olmak istenen firma mevcut mu kontrolü yapılıyor
            var firma = _context.Firmalar.FirstOrDefault(f => f.FirmaKodu == firmaKodu);
            if (firma == null)
            {
                ViewBag.Error = "Firma bulunamadı.";
                return View();
            }

            // aynı firma içinde bu kullanıcı adı var mı kontrolü yapılıyor
            var varMi = _context.Kullanicilar.Any(k => k.FirmaId == firma.Id && k.KullaniciAdi == kullaniciAdi);
            if (varMi)
            {
                ViewBag.Error = "Bu kullanıcı adı bu firmada zaten alınmış.";
                return View();
            }

            var yeniKullanici = new Kullanici
            {
                Id = Guid.NewGuid(),
                FirmaId = firma.Id,
                KullaniciAdi = kullaniciAdi
            };

            // şifreyi veritabanına kaydetmeden önce güvenli bir şekilde hashlüyoruz
            var hasher = new PasswordHasher<Kullanici>();
            yeniKullanici.Sifre = hasher.HashPassword(yeniKullanici, sifre);

            _context.Kullanicilar.Add(yeniKullanici);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}

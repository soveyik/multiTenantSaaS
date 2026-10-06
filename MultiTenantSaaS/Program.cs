using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Data;
using MultiTenantSaaS.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı bağlantısı
builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

// Cookie Authentication (MVC için daha uygun)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// proje ilk çalıştığında veritabanı yoksa otomatik oluştursun
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();
    db.Database.EnsureCreated();

    if (!db.Firmalar.Any())
    {
        var firmaA = new Firma { Id = Guid.NewGuid(), FirmaKodu = "firma-a", Port = "5001", Ayar = "Kirmizi Tema" };
        var firmaB = new Firma { Id = Guid.NewGuid(), FirmaKodu = "firma-b", Port = "5002", Ayar = "Mavi Tema" };
        
        db.Firmalar.AddRange(firmaA, firmaB);
        db.SaveChanges(); 

        var kullaniciA = new Kullanici { Id = Guid.NewGuid(), FirmaId = firmaA.Id, KullaniciAdi = "admin" };
        var kullaniciB = new Kullanici { Id = Guid.NewGuid(), FirmaId = firmaB.Id, KullaniciAdi = "admin" };
        
        var hasher = new PasswordHasher<Kullanici>();
        kullaniciA.Sifre = hasher.HashPassword(kullaniciA, "123");
        kullaniciB.Sifre = hasher.HashPassword(kullaniciB, "123");
        
        db.Kullanicilar.AddRange(kullaniciA, kullaniciB);

        var urun1 = new Urun { Id = Guid.NewGuid(), TenantId = firmaA.Id, Ad = "Firma A Ürünü 1", UrunKodu = "PRD-A-001", Aciklama = "Bu ürün Firma A için özel üretilmiş yüksek kaliteli bir donanımdır.", Fiyat = 100 };
        var urun2 = new Urun { Id = Guid.NewGuid(), TenantId = firmaA.Id, Ad = "Firma A Ürünü 2", UrunKodu = "PRD-A-002", Aciklama = "Günlük kullanıma uygun, dayanıklı ve ergonomik tasarım.", Fiyat = 200 };
        var urun3 = new Urun { Id = Guid.NewGuid(), TenantId = firmaB.Id, Ad = "Firma B Ürünü 1", UrunKodu = "PRD-B-001", Aciklama = "Firma B'nin en çok satan, fiyat/performans ürünü.", Fiyat = 150 };

        db.Urunler.AddRange(urun1, urun2, urun3);
        db.SaveChanges();
    }
}

app.Run();

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Data;
using MultiTenantSaaS.Models;
using Microsoft.AspNetCore.Identity;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using MultiTenantSaaS.Services;

var builder = WebApplication.CreateBuilder(args);

// veritabani baglantisi
builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

// cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";               // giris yapilmamissa yonlendirilecek adres
        options.AccessDeniedPath = "/Account/AccessDenied"; // yetki yetersizse yonlendirilecek adres
    });

builder.Services.AddAuthorization();          // authorize niteliklerini denetleyen servis
builder.Services.AddControllersWithViews();   // mvc mimarisini aktif eder
builder.Services.AddSession();                // kullanici bazli gecici veri saklamak icin session servisi
builder.Services.AddHttpContextAccessor();    // controller disindaki siniflarda httpcontexte erisebilmek icin

// elasticsearch baglanti ayarlari
var elasticSettings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("products_index");
builder.Services.AddSingleton(new ElasticsearchClient(elasticSettings));

// arama servisi kaydi
builder.Services.AddScoped<SearchService>();

// redis cache entegrasyonu
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379"; // redis sunucu adresi
    options.InstanceName = "SaaSCache_";      // onbellek anahtarlarinin basina eklenecek prefix
});

var app = builder.Build();

app.UseStaticFiles();   // statik dosyalarin sunulmasini saglar
app.UseSession();       // oturum destegini devreye sokar
app.UseRouting();       // istek urlsi ile action eslestirmesi

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(// varsayilan sayfa rotasi
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// proje ilk calistiginda veritabani yoksa otomatik olusturur
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

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MultiTenantSaaS.Models;
using MultiTenantSaaS.Services;
using MultiTenantSaaS.Data;

namespace MultiTenantSaaS.Controllers
{
    [Authorize] // sadece giriş yapmış kullanıcılar arama yapabilsin
    public class SearchController : Controller
    {
        private readonly SearchService _aramaServisi;
        private readonly IDistributedCache _onbellek; // redis önbellek servisi
        private readonly UygulamaDbContext _context; // sql veritabanı

        public SearchController(SearchService aramaServisi, IDistributedCache onbellek, UygulamaDbContext context)
        {
            _aramaServisi = aramaServisi;
            _onbellek = onbellek;
            _context = context;
        }

        // arama sayfası: arama kelimesini alır, redis'te yoksa elastic'ten bulur
        public async Task<IActionResult> Index(string q)
        {
            // eğer arama kelimesi boşsa boş liste dön
            if (string.IsNullOrWhiteSpace(q))
            {
                try { ViewBag.IsSeeded = await _aramaServisi.IndexDoluMuAsync() > 10; } catch { ViewBag.IsSeeded = false; }
                return View(new List<UrunAramaModeli>());
            }

            try { ViewBag.IsSeeded = await _aramaServisi.IndexDoluMuAsync() > 10; } catch { ViewBag.IsSeeded = false; }

            // kullanici firma bilgisi

            var firmaIdMetni = User.Claims.FirstOrDefault(c => c.Type == "FirmaId")?.Value;
            Guid firmaId = string.IsNullOrEmpty(firmaIdMetni) ? Guid.Empty : Guid.Parse(firmaIdMetni);
            
            if (firmaId == Guid.Empty)
            {
                // firma id yoksa login ekranina yonlendir
                return RedirectToAction("Login", "Account");
            }
            
            // redis cache
            // tenant cache key
            string onbellekAnahtari = $"search_{firmaId}_{q.ToLower()}";
            var onbellektekiVeri = await _onbellek.GetStringAsync(onbellekAnahtari);

            List<UrunAramaModeli> sonuclar;
            Stopwatch kronometre = new Stopwatch();

            if (!string.IsNullOrEmpty(onbellektekiVeri))
            {
                // veri rediste bulundu elasticsearche gitmiyor
                kronometre.Start();
                sonuclar = JsonSerializer.Deserialize<List<UrunAramaModeli>>(onbellektekiVeri) ?? new List<UrunAramaModeli>();
                kronometre.Stop();
                
                ViewBag.Source = "REDIS";
                ViewBag.Time = kronometre.ElapsedMilliseconds;
            }
            else
            {
                // veri rediste yok elasticsearce sorgu atilir
                kronometre.Start();
                sonuclar = await _aramaServisi.UrunAramaAsync(firmaId, q);
                kronometre.Stop();

                ViewBag.Source = "ELASTICSEARCH";
                ViewBag.Time = kronometre.ElapsedMilliseconds;

                // bulunan sonuc redise kaydedilir suresi 1 dakika
                var onbellekAyarlari = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                };
                
                string serilestirilmisVeri = JsonSerializer.Serialize(sonuclar);
                await _onbellek.SetStringAsync(onbellekAnahtari, serilestirilmisVeri, onbellekAyarlari);
            }

            ViewBag.Keyword = q;
            return View(sonuclar);
        }

        // performans testi icin dummy kayitlar
        [HttpPost]
        public async Task<IActionResult> SeedData()
        {
            var firmalar = _context.Firmalar.Take(2).ToList();
            if(firmalar.Count < 2)
            {
                TempData["Message"] = "Sistemde en az 2 firma bulunamadığı için örnek veriler dağıtılamadı.";
                return RedirectToAction("Index");
            }

            var firmaAId = firmalar[0].Id;
            var firmaBId = firmalar[1].Id;

            var mevcutUrunSayisi = _context.Urunler.Count();
            var eklenecekUrunlerElastic = new List<UrunAramaModeli>();

            if (mevcutUrunSayisi > 10)
            {
                // veritabani zaten dolu cift kayit olusmamasi icin sql verileri elasticsarce aktarilir
                var sqlUrunleri = _context.Urunler.ToList();
                foreach (var urun in sqlUrunleri)
                {
                    eklenecekUrunlerElastic.Add(new UrunAramaModeli
                    {
                        Id = urun.Id,
                        TenantId = urun.TenantId,
                        Ad = urun.Ad,
                        UrunKodu = urun.UrunKodu,
                        Aciklama = urun.Aciklama,
                        Fiyat = urun.Fiyat
                    });
                }

                await _aramaServisi.TopluUrunEkleAsync(eklenecekUrunlerElastic);
                TempData["Message"] = $"SQL veritabanınızdaki {sqlUrunleri.Count} adet kayıt Elasticsearch'e aktarıldı.";
            }
            else
            {
                var eklenecekUrunlerSql = new List<Urun>();
                
                string[] gercekciUrunler = {
                    "Oyuncu Bilgisayarı", "Mekanik Klavye", "Ergonomik Mouse", "27 inç Monitör", "Kulaklık",
                    "Çalışma Masası", "Ofis Sandalyesi", "Tablet", "Akıllı Telefon", "Akıllı Saat",
                    "Bluetooth Hoparlör", "Harici Disk (1TB)", "USB Bellek (64GB)", "Oyun Konsolu", "Projeksiyon Cihazı",
                    "Webcam", "Mikrofon", "Router", "Modem", "Oyun Kolu",
                    "Yazıcı", "Tarayıcı", "Kablo Düzenleyici", "Laptop Soğutucu", "Mousepad",
                    "Masa Lambası", "Kitaplık", "Evrak Dolabı", "Hesap Makinesi", "Tükenmez Kalem Seti",
                    "Ajanda", "Çöp Kutusu", "Kupa Bardak", "Sırt Çantası", "Laptop Çantası",
                    "Tablet Kılıfı", "Telefon Kılıfı", "Ekran Koruyucu", "Şarj Aleti", "Powerbank",
                    "HDMI Kablo", "Ethernet Kablosu", "Priz Çoğaltıcı", "Akım Korumalı Priz", "Piller",
                    "Termos", "Kahve Makinesi", "Su Isıtıcı", "Masa Saati", "Duvar Saati"
                };

                int urunSayaci = 1;

                foreach (var urunAdi in gercekciUrunler)
                {
                    // firma A urunleri
                    var idA = Guid.NewGuid();
                    var kodA = $"TST-A-{urunSayaci}";
                    decimal fiyatA = 100 + (urunSayaci * 15);
                    
                    eklenecekUrunlerSql.Add(new Urun { Id = idA, TenantId = firmaAId, Ad = urunAdi, UrunKodu = kodA, Aciklama = $"{urunAdi} - Kaliteli ve dayanıklı ürün.", Fiyat = fiyatA });
                    eklenecekUrunlerElastic.Add(new UrunAramaModeli { Id = idA, TenantId = firmaAId, Ad = urunAdi, UrunKodu = kodA, Aciklama = $"{urunAdi} - Kaliteli ve dayanıklı ürün.", Fiyat = fiyatA });

                    // firma B urunleri
                    var idB = Guid.NewGuid();
                    var kodB = $"TST-B-{urunSayaci}";
                    decimal fiyatB = 100 + (urunSayaci * 12);

                    eklenecekUrunlerSql.Add(new Urun { Id = idB, TenantId = firmaBId, Ad = urunAdi, UrunKodu = kodB, Aciklama = $"{urunAdi} - Premium seri ürün.", Fiyat = fiyatB });
                    eklenecekUrunlerElastic.Add(new UrunAramaModeli { Id = idB, TenantId = firmaBId, Ad = urunAdi, UrunKodu = kodB, Aciklama = $"{urunAdi} - Premium seri ürün.", Fiyat = fiyatB });
                    
                    urunSayaci++;
                }

                // 1 sql kayit islemi
                _context.Urunler.AddRange(eklenecekUrunlerSql);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();

                // 2 elasticsearch kayit islemi
                await _aramaServisi.TopluUrunEkleAsync(eklenecekUrunlerElastic);

                eklenecekUrunlerSql.Clear();
                eklenecekUrunlerElastic.Clear();

                // 3 ekstra test urunleri dongusu
                int ekstraMiktar = 99900;
                int chunkSize = 10000;
                for (int i = 0; i < ekstraMiktar; i += chunkSize)
                {
                    var sqlChunk = new List<Urun>();
                    var elasticChunk = new List<UrunAramaModeli>();

                    for (int j = 0; j < chunkSize && (i + j) < ekstraMiktar; j++)
                    {
                        int globalIndex = i + j;
                        bool isFirmaA = globalIndex % 2 == 0;
                        var aktifFirmaId = isFirmaA ? firmaAId : firmaBId;
                        var fKodu = isFirmaA ? "A" : "B";

                        var uId = Guid.NewGuid();
                        var uAd = $"Performans Ürünü - {globalIndex}";
                        var uKod = $"PERF-{fKodu}-{globalIndex}";
                        var uAciklama = $"Elasticsearch hız testi için otomatik üretilmiştir. (Sıra: {globalIndex})";
                        var uFiyat = 10m + (globalIndex % 500);

                        sqlChunk.Add(new Urun { Id = uId, TenantId = aktifFirmaId, Ad = uAd, UrunKodu = uKod, Aciklama = uAciklama, Fiyat = uFiyat });
                        elasticChunk.Add(new UrunAramaModeli { Id = uId, TenantId = aktifFirmaId, Ad = uAd, UrunKodu = uKod, Aciklama = uAciklama, Fiyat = uFiyat });
                    }

                    _context.Urunler.AddRange(sqlChunk);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear(); // ram sismesini onle

                    await _aramaServisi.TopluUrunEkleAsync(elasticChunk);
                }

                TempData["Message"] = $"Toplam 100.000 kayıt eklendi! (100 Gerçekçi + 99.900 Performans Testi)";
            }

            return RedirectToAction("Index");
        }
    }
}

using Elastic.Clients.Elasticsearch;
using MultiTenantSaaS.Models;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace MultiTenantSaaS.Services
{
    // elasticsearch arama islemleri servisi
    public class SearchService
    {
        private readonly ElasticsearchClient _istemci;
        private const string IndeksAdi = "products_index";

        public SearchService(ElasticsearchClient istemci)
        {
            _istemci = istemci;
        }

        // seed data metodu
        public async Task TopluUrunEkleAsync(List<UrunAramaModeli> urunler)
        {
            // indeks kontrolu
            var varMiYaniti = await _istemci.Indices.ExistsAsync(IndeksAdi);
            if (!varMiYaniti.Exists)
            {
                await _istemci.Indices.CreateAsync(IndeksAdi);
            }

            // toplu kayit islemi
            var topluEklemeYaniti = await _istemci.BulkAsync(b => b
                .Index(IndeksAdi)
                .IndexMany(urunler)
            );
        }

        // multi-tenant mantığına uygun, tenant izolasyonlu ürün arama
        public async Task<long> IndexDoluMuAsync()
        {
            try {
                var varMiYaniti = await _istemci.Indices.ExistsAsync(IndeksAdi);
                if (!varMiYaniti.Exists) return 0;

                var countResponse = await _istemci.CountAsync(c => c.Index(IndeksAdi));
                return countResponse.Count;
            } catch (Exception ex) {
                System.IO.File.AppendAllText("debug_log.txt", "IndexDoluMuAsync Error: " + ex.Message + "\n");
                return 0;
            }
        }

        // multi-tenant mantığına uygun, tenant izolasyonlu ürün arama
        public async Task<List<UrunAramaModeli>> UrunAramaAsync(Guid firmaId, string arananKelime)
        {
            // must ve filter query
            var aramaYaniti = await _istemci.SearchAsync<UrunAramaModeli>(s => s
                .Index(IndeksAdi)
                .Size(50) // limit 50
                .Query(q => q
                    .Bool(b => b
                        // filter ile tenant sorgusu
                        .Filter(f => f
                            .Term(t => t.Field("tenantId.keyword").Value(firmaId.ToString()))
                        )
                        // ad aciklama ve urunkodu propertylerinde arama
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(arananKelime)
                                .Fields(new[] { "ad", "aciklama", "urunKodu" })
                            )
                        )
                    )
                )
            );

            if (!aramaYaniti.IsValidResponse)
            {
                // hata durumu
                return new List<UrunAramaModeli>();
            }
            
            System.IO.File.AppendAllText("debug_log.txt", "Search Success! Docs: " + aramaYaniti.Documents.Count + ", Debug: " + aramaYaniti.DebugInformation + "\n");

            return aramaYaniti.Documents.ToList();
        }
    }
}

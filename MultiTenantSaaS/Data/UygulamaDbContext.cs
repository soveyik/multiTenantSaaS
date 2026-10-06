using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Models;

namespace MultiTenantSaaS.Data
{
    // veritabanı bağlantı ayarlarımız
    public class UygulamaDbContext : DbContext
    {
        public UygulamaDbContext(DbContextOptions<UygulamaDbContext> options) : base(options)
        {
        }

        public DbSet<Firma> Firmalar { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<Siparis> Siparisler { get; set; }
        public DbSet<SiparisDetay> SiparisDetaylari { get; set; }
        public DbSet<SepetDetay> SepetDetaylari { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // kullanıcıların firmaya bağlanması 
            modelBuilder.Entity<Kullanici>()
                .HasOne(k => k.Firma) //her kullanıcının bir firması vardır
                .WithMany() //her firmanın birçok kullanıcısı olabilir
                .HasForeignKey(k => k.FirmaId); 
        }
    }
}

using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Data;

public class DirectoryDbContext : DbContext
{
    public DirectoryDbContext(DbContextOptions<DirectoryDbContext> options)
        : base(options)
    {
    }

    // Здесь будут свойства DbSet
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<TagGroup> TagGroups => Set<TagGroup>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<GoogleCategory> GoogleCategories => Set<GoogleCategory>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<BusinessPage> BusinessPages => Set<BusinessPage>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Уникальные индексы для Slugs (критично для SEO)
        modelBuilder.Entity<Province>().HasIndex(p => p.Slug).IsUnique();
        modelBuilder.Entity<City>().HasIndex(c => c.Slug).IsUnique();
        modelBuilder.Entity<TagGroup>().HasIndex(tg => tg.Slug).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
        modelBuilder.Entity<MediaAsset>().HasIndex(m => m.Slug).IsUnique();
        modelBuilder.Entity<BusinessPage>().HasIndex(b => b.Slug).IsUnique();

        // 2. Настройка связей Many-to-Many с понятными именами таблиц

        // Страницы <-> Теги
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Tags)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessTags"));

        // Страницы <-> Медиа-активы
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Media)
            .WithMany(m => m.BusinessPages)
            .UsingEntity(j => j.ToTable("BusinessMedia"));

        // Страницы <-> Категории Google
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.GoogleCategories)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessGoogleCategories"));

        // 3. Подключение расширения PostGIS
        modelBuilder.HasPostgresExtension("postgis");

        // 4. JSONB и География для BusinessPage
        modelBuilder.Entity<BusinessPage>(entity =>
        {
            // Настройка SEO с вложенной коллекцией Hreflangs
            entity.OwnsOne(b => b.Seo, seo =>
            {
                seo.ToJson();
                // Явно указываем, что Hreflangs — это часть JSON-объекта SEO
                seo.OwnsMany(s => s.Hreflangs);
            });

            entity.OwnsOne(b => b.Contacts, c => { c.ToJson(); });

            entity.OwnsMany(b => b.Schedule, s => { s.ToJson(); });

            entity.Property(b => b.Location).HasColumnType("geography(Point, 4326)");
        });

        // 5. География для City
        modelBuilder.Entity<City>(entity =>
        {
            entity.Property(c => c.Center).HasColumnType("geography(Point, 4326)");
        });

    }
}
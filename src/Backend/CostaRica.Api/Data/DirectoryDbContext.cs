using CostaRica.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Data;

public class DirectoryDbContext : DbContext
{
    public DirectoryDbContext(DbContextOptions<DirectoryDbContext> options)
        : base(options)
    {
    }

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

        // 1. Инфраструктура PostGIS
        modelBuilder.HasPostgresExtension("postgis");

        // 2. Уникальные индексы для Slugs (URL-адресов справочников)
        modelBuilder.Entity<Province>().HasIndex(p => p.Slug).IsUnique();
        modelBuilder.Entity<City>().HasIndex(c => c.Slug).IsUnique();
        modelBuilder.Entity<TagGroup>().HasIndex(tg => tg.Slug).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
        modelBuilder.Entity<MediaAsset>().HasIndex(m => m.Slug).IsUnique();

        // 3. Конфигурация BusinessPage
        modelBuilder.Entity<BusinessPage>(entity =>
        {
            // Текущий слаг должен быть уникальным
            entity.HasIndex(b => b.Slug).IsUnique();

            // GIN-индекс для эффективного поиска внутри JSONB-массива OldSlugs
            entity.HasIndex(b => b.OldSlugs).HasMethod("gin");

            // GIST-индекс для пространственных запросов (гео-поиск по радиусу)
            entity.HasIndex(b => b.Location).HasMethod("gist");

            // Маппинг истории слагов в JSONB
            entity.Property(b => b.OldSlugs)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb");

            // Связь с Провинцией (обязательная)
            entity.HasOne(b => b.Province)
                .WithMany()
                .HasForeignKey(b => b.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь с Городом (опциональная)
            entity.HasOne(b => b.City)
                .WithMany()
                .HasForeignKey(b => b.CityId)
                .OnDelete(DeleteBehavior.SetNull);

            // Основная категория Google (Primary)
            entity.HasOne(b => b.PrimaryCategory)
                .WithMany()
                .HasForeignKey(b => b.PrimaryCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Дополнительные категории Google (Secondary) — Many-to-Many
            entity.HasMany(b => b.SecondaryCategories)
                .WithMany()
                .UsingEntity(j => j.ToTable("BusinessSecondaryCategories"));

            // Связи Many-to-Many (Tags и Media)
            entity.HasMany(b => b.Tags)
                .WithMany()
                .UsingEntity(j => j.ToTable("BusinessTags"));

            entity.HasMany(b => b.Media)
                .WithMany(m => m.BusinessPages)
                .UsingEntity(j => j.ToTable("BusinessMedia"));

            // 4. Данные в формате JSONB (Owned Types)

            // SEO настройки
            entity.OwnsOne(b => b.Seo, seo =>
            {
                seo.ToJson();
                seo.OwnsMany(s => s.Hreflangs);
            });

            // Контакты
            entity.OwnsOne(b => b.Contacts, c =>
            {
                c.ToJson();
            });

            // Расписание
            entity.OwnsMany(b => b.Schedule, s =>
            {
                s.ToJson();
            });
        });
    }
}
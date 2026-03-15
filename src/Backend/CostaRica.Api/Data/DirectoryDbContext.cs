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

        // 2. Уникальные индексы для Slugs (URL-адресов)
        modelBuilder.Entity<Province>().HasIndex(p => p.Slug).IsUnique();
        modelBuilder.Entity<City>().HasIndex(c => c.Slug).IsUnique();
        modelBuilder.Entity<TagGroup>().HasIndex(tg => tg.Slug).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
        modelBuilder.Entity<MediaAsset>().HasIndex(m => m.Slug).IsUnique();
        modelBuilder.Entity<BusinessPage>().HasIndex(b => b.Slug).IsUnique();

        // 3. Базовые связи (География и Теги)
        modelBuilder.Entity<City>()
            .HasOne(c => c.Province)
            .WithMany(p => p.Cities)
            .HasForeignKey(c => c.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tag>()
            .HasOne(t => t.TagGroup)
            .WithMany(tg => tg.Tags)
            .HasForeignKey(t => t.TagGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. Конфигурация BusinessPage
        modelBuilder.Entity<BusinessPage>(entity =>
        {
            // Геолокация (Point в формате WGS84)
            entity.Property(b => b.Location)
                .HasColumnType("geography(Point, 4326)");

            // Внешние ключи для географии
            entity.HasOne(b => b.Province)
                .WithMany()
                .HasForeignKey(b => b.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.City)
                .WithMany()
                .HasForeignKey(b => b.CityId)
                .OnDelete(DeleteBehavior.SetNull);

            // КАТЕГОРИИ GOOGLE (ГИБРИДНАЯ МОДЕЛЬ)
            // Основная категория (Primary)
            entity.HasOne(b => b.PrimaryCategory)
                .WithMany()
                .HasForeignKey(b => b.PrimaryCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Дополнительные категории (Secondary) — Many-to-Many
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

            // 5. Данные в формате JSONB
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

            // Расписание с вложенной коллекцией интервалов
            entity.OwnsMany(b => b.Schedule, s =>
            {
                s.ToJson();
                // Явно указываем владение вложенным списком Intervals внутри JSON
                s.OwnsMany(sd => sd.Intervals);
            });
        });
    }
}
using CostaRica.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace CostaRica.Api.Data;

public class DirectoryDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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

        // 2. Конфигурация существующих справочников (защита от нежелательных изменений в миграции)
        modelBuilder.Entity<Province>().HasIndex(p => p.Slug).IsUnique();

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasIndex(c => c.Slug).IsUnique();
            // Явно оставляем Restrict, чтобы миграция не меняла FK на Cascade
            entity.HasOne(c => c.Province)
                .WithMany(p => p.Cities)
                .HasForeignKey(c => c.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TagGroup>().HasIndex(tg => tg.Slug).IsUnique();

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(t => t.Slug).IsUnique();
            // Явно оставляем Restrict для защиты существующих связей
            entity.HasOne(t => t.TagGroup)
                .WithMany(tg => tg.Tags)
                .HasForeignKey(t => t.TagGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MediaAsset>().HasIndex(m => m.Slug).IsUnique();

        // 3. Конфигурация BusinessPage
        modelBuilder.Entity<BusinessPage>(entity =>
        {
            entity.HasIndex(b => b.Slug).IsUnique();

            // GIN-индекс для JSONB массива (история URL)
            entity.HasIndex(b => b.OldSlugs).HasMethod("gin");

            // GIST-индекс для пространственного поиска
            entity.HasIndex(b => b.Location).HasMethod("gist");

            // Настройка истории слагов
            entity.Property(b => b.OldSlugs)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb");

            // КРИТИЧЕСКИЙ МОМЕНТ: фиксируем тип geography для Lat/Lon координат
            entity.Property(b => b.Location)
                .HasColumnType("geography(Point, 4326)");

            // Связи бизнес-страницы
            entity.HasOne(b => b.Province)
                .WithMany()
                .HasForeignKey(b => b.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.City)
                .WithMany()
                .HasForeignKey(b => b.CityId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(b => b.PrimaryCategory)
                .WithMany()
                .HasForeignKey(b => b.PrimaryCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(b => b.SecondaryCategories)
                .WithMany()
                .UsingEntity(j => j.ToTable("BusinessSecondaryCategories"));

            entity.HasMany(b => b.Tags)
                .WithMany()
                .UsingEntity(j => j.ToTable("BusinessTags"));

            entity.HasMany(b => b.Media)
                .WithMany(m => m.BusinessPages)
                .UsingEntity(j => j.ToTable("BusinessMedia"));

            // 4. Глубокая конфигурация JSONB (Owned Types)
            entity.OwnsOne(b => b.Seo, seo =>
            {
                seo.ToJson();
                seo.OwnsMany(s => s.Hreflangs);
            });

            entity.OwnsOne(b => b.Contacts, c =>
            {
                c.ToJson();
            });

            entity.OwnsMany(b => b.Schedule, s =>
            {
                s.ToJson();
                s.OwnsMany(day => day.Intervals);
            });
        });
    }
}
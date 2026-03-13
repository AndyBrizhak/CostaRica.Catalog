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

        // 1. Уникальные индексы для Slugs
        modelBuilder.Entity<Province>().HasIndex(p => p.Slug).IsUnique();
        modelBuilder.Entity<City>().HasIndex(c => c.Slug).IsUnique();
        modelBuilder.Entity<TagGroup>().HasIndex(tg => tg.Slug).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
        modelBuilder.Entity<MediaAsset>().HasIndex(m => m.Slug).IsUnique();
        modelBuilder.Entity<BusinessPage>().HasIndex(b => b.Slug).IsUnique();

        // 2. Настройка связей (ГЕОГРАФИЯ - Запрет каскадного удаления)

        modelBuilder.Entity<City>()
            .HasOne(c => c.Province)
            .WithMany(p => p.Cities)
            .HasForeignKey(c => c.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BusinessPage>()
            .HasOne(b => b.Province)
            .WithMany()
            .HasForeignKey(b => b.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BusinessPage>()
            .HasOne(b => b.City)
            .WithMany()
            .HasForeignKey(b => b.CityId)
            .OnDelete(DeleteBehavior.SetNull); // Если город удалят, бизнес останется в провинции

        // 3. Теги (Запрет каскадного удаления групп)
        modelBuilder.Entity<Tag>()
            .HasOne(t => t.TagGroup)
            .WithMany(tg => tg.Tags)
            .HasForeignKey(t => t.TagGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. ГИБРИДНАЯ МОДЕЛЬ КАТЕГОРИЙ GOOGLE

        // Primary Category (One-to-Many)
        modelBuilder.Entity<BusinessPage>()
            .HasOne(p => p.PrimaryCategory)
            .WithMany()
            .HasForeignKey(p => p.PrimaryCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Secondary Categories (Many-to-Many)
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.SecondaryCategories)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessSecondaryCategories"));

        // Другие Many-to-Many
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Tags)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessTags"));

        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Media)
            .WithMany(m => m.BusinessPages)
            .UsingEntity(j => j.ToTable("BusinessMedia"));

        // 5. Инфраструктура PostGIS
        modelBuilder.HasPostgresExtension("postgis");

        // 6. Конфигурация BusinessPage (JSONB и Geo)
        modelBuilder.Entity<BusinessPage>(entity =>
        {
            entity.Property(b => b.Location)
                .HasColumnType("geography(Point, 4326)");

            // SEO настройки (JSONB)
            entity.OwnsOne(b => b.Seo, seo =>
            {
                seo.ToJson();
                seo.OwnsMany(s => s.Hreflangs);
            });

            // Контакты (JSONB)
            entity.OwnsOne(b => b.Contacts, c => { c.ToJson(); });

            // Расписание (JSONB) - Делаем опциональным
            entity.Property(b => b.Schedule)
                .HasColumnType("jsonb")
                .IsRequired(false);
        });
    }
}
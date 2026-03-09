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

        // 2. Настройка связей

        // Город -> Провинция
        modelBuilder.Entity<City>()
            .HasOne(c => c.Province)
            .WithMany(p => p.Cities)
            .HasForeignKey(c => c.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Теги -> Группа (Безопасное удаление и навигация)
        modelBuilder.Entity<Tag>()
            .HasOne(t => t.TagGroup)
            .WithMany(tg => tg.Tags)
            .HasForeignKey(t => t.TagGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Бизнес-страница -> География
        modelBuilder.Entity<BusinessPage>()
            .HasOne(p => p.Province)
            .WithMany()
            .HasForeignKey(p => p.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BusinessPage>()
            .HasOne(p => p.City)
            .WithMany()
            .HasForeignKey(p => p.CityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-Many связи
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Tags)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessTags"));

        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Media)
            .WithMany(m => m.BusinessPages)
            .UsingEntity(j => j.ToTable("BusinessMedia"));

        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.GoogleCategories)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessGoogleCategories"));

        // 3. Инфраструктура PostGIS
        modelBuilder.HasPostgresExtension("postgis");

        // 4. Конфигурация BusinessPage (JSONB и Geo)
        modelBuilder.Entity<BusinessPage>(entity =>
        {
            // Явное указание типа для геолокации, чтобы избежать конфликтов при миграциях
            entity.Property(b => b.Location)
                .HasColumnType("geography(Point, 4326)");

            // SEO настройки
            entity.OwnsOne(b => b.Seo, seo =>
            {
                seo.ToJson();
                seo.OwnsMany(s => s.Hreflangs);
            });

            // Контакты
            entity.OwnsOne(b => b.Contacts, c => { c.ToJson(); });

            // Расписание
            entity.OwnsMany(b => b.Schedule, schedule =>
            {
                schedule.ToJson();
                schedule.OwnsMany(s => s.Intervals);
            });
        });
    }
}
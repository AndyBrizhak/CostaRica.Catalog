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

        // Город -> Провинция (Many-to-One)
        modelBuilder.Entity<City>()
            .HasOne(c => c.Province)
            .WithMany(p => p.Cities)
            .HasForeignKey(c => c.ProvinceId)
            // ЗАПРЕТ каскадного удаления: нельзя удалить провинцию, если в ней есть города
            .OnDelete(DeleteBehavior.Restrict);

        // Тег -> Группа (Many-to-One)
        modelBuilder.Entity<Tag>()
            .HasOne(t => t.TagGroup)
            .WithMany() // В TagGroup нет коллекции Tags
            .HasForeignKey(t => t.TagGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Бизнес-страница -> Провинция и Город
        modelBuilder.Entity<BusinessPage>()
            .HasOne(b => b.Province)
            .WithMany()
            .HasForeignKey(b => b.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BusinessPage>()
            .HasOne(b => b.City)
            .WithMany()
            .HasForeignKey(b => b.CityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-Many: Страницы <-> Теги
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Tags)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessTags"));

        // Many-to-Many: Страницы <-> Медиа
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.Media)
            .WithMany(m => m.BusinessPages)
            .UsingEntity(j => j.ToTable("BusinessMedia"));

        // Many-to-Many: Страницы <-> Категории Google
        modelBuilder.Entity<BusinessPage>()
            .HasMany(p => p.GoogleCategories)
            .WithMany()
            .UsingEntity(j => j.ToTable("BusinessGoogleCategories"));

        // 3. Инфраструктура PostGIS
        modelBuilder.HasPostgresExtension("postgis");

        // 4. JSONB конфигурация для BusinessPage
        modelBuilder.Entity<BusinessPage>(entity =>
        {
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

            // Пространственные данные
            entity.Property(b => b.Location).HasColumnType("geography(Point, 4326)");
        });
    }
}
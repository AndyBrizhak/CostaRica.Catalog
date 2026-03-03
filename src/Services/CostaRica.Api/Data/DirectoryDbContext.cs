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

        // Здесь будет конфигурация Fluent API
    }
}
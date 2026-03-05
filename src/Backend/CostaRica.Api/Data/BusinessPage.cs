using NetTopologySuite.Geometries;

namespace CostaRica.Api.Data;

public class BusinessPage
{
    public Guid Id { get; set; }

    // Язык конкретной страницы: "en" или "es"
    public string LanguageCode { get; set; } = "en";

    public string Name { get; set; } = string.Empty;

    // Уникальный слаг для URL: "hotel-del-mar"
    public string Slug { get; set; } = string.Empty;

    // Основное описание (HTML)
    public string? Description { get; set; }

    // --- ГЕОГРАФИЯ ---
    public Guid ProvinceId { get; set; }
    public Province? Province { get; set; }

    public Guid? CityId { get; set; }
    public City? City { get; set; }

    // Точные координаты на карте
    public Point Location { get; set; } = default!;

    // --- ДАННЫЕ В ФОРМАТЕ JSONB (наши вспомогательные классы) ---
    public BusinessContacts Contacts { get; set; } = new();
    public List<ScheduleDay> Schedule { get; set; } = [];
    public BusinessSeoSettings Seo { get; set; } = new();

    // --- СВЯЗИ MANY-TO-MANY ---
    // Визуальные теги (Пицца, Бассейн и т.д.)
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    // Скрытые категории Google для SEO (GCID)
    public ICollection<GoogleCategory> GoogleCategories { get; set; } = new List<GoogleCategory>();

    // Картинки из медиа-библиотеки
    public ICollection<MediaAsset> Media { get; set; } = new List<MediaAsset>();

    // --- СИСТЕМНЫЕ ПОЛЯ ---
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
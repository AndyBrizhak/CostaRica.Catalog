using NetTopologySuite.Geometries;

namespace CostaRica.Api.Data;

/// <summary>
/// Основная сущность бизнес-страницы (заведения) каталога.
/// </summary>
public class BusinessPage
{
    public Guid Id { get; set; }

    // --- СИСТЕМНЫЕ ПОЛЯ И ЖИЗНЕННЫЙ ЦИКЛ ---

    // Статус публикации: отображать ли бизнес в Discovery API
    public bool IsPublished { get; set; } = false;

    // Дата создания и последнего обновления
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Язык конкретной страницы: "en" или "es"
    public string LanguageCode { get; set; } = "en";

    // Название бизнеса
    public string Name { get; set; } = string.Empty;

    // --- SEO И URL МЕНЕДЖМЕНТ ---

    // Текущий уникальный слаг: "hotel-del-mar"
    public string Slug { get; set; } = string.Empty;

    // История предыдущих слагов для реализации 301-редиректов.
    // Хранится в БД как JSONB массив строк.
    public List<string> OldSlugs { get; set; } = [];

    // Основное описание (HTML)
    public string? Description { get; set; }

    // --- ГЕОГРАФИЯ ---

    // Провинция (обязательно для Drill-down)
    public Guid ProvinceId { get; set; }
    public Province? Province { get; set; }

    // Город (опционально)
    public Guid? CityId { get; set; }
    public City? City { get; set; }

    // Точные координаты на карте (PostGIS Point)
    public Point Location { get; set; } = default!;

    // --- КАТЕГОРИИ GOOGLE (ГИБРИДНАЯ МОДЕЛЬ ДЛЯ SEO) ---

    // 1. Основная категория (Primary) - определяет тип Schema.org
    public Guid? PrimaryCategoryId { get; set; }
    public GoogleCategory? PrimaryCategory { get; set; }

    // 2. Дополнительные категории (Secondary) - для расширенного SEO
    public ICollection<GoogleCategory> SecondaryCategories { get; set; } = new List<GoogleCategory>();

    // --- ДАННЫЕ В ФОРМАТЕ JSONB ---
    public BusinessContacts Contacts { get; set; } = new();
    public List<ScheduleDay> Schedule { get; set; } = [];
    public BusinessSeoSettings Seo { get; set; } = new();

    // --- СВЯЗИ MANY-TO-MANY ---

    // Визуальные теги (Пицца, Бассейн и т.д.) - основа Drill-down поиска
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    // Галерея изображений
    public ICollection<MediaAsset> Media { get; set; } = new List<MediaAsset>();
}
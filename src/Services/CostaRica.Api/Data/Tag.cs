namespace CostaRica.Api.Data;

public class Tag
{
    public Guid Id { get; set; }

    // Название на английском, например: "Pizza"
    public string NameEn { get; set; } = string.Empty;

    // Название на испанском, например: "Pizzería"
    public string NameEs { get; set; } = string.Empty;

    // Уникальный слаг для URL, например: "pizza"
    public string Slug { get; set; } = string.Empty;

    // Внешний ключ на Группу тегов
    public Guid TagGroupId { get; set; }

    // Навигационное свойство для связи с группой
    public TagGroup? TagGroup { get; set; }
}
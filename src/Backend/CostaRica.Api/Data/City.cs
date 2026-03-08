namespace CostaRica.Api.Data;

public class City
{
    public Guid Id { get; set; }

    // Название города, например: "Playa del Coco"
    public string Name { get; set; } = string.Empty;

    // Уникальный слаг для URL, например: "playa-del-coco"
    public string Slug { get; set; } = string.Empty;

    // Внешний ключ на Провинцию
    public Guid ProvinceId { get; set; }

    // Навигационное свойство
    public Province? Province { get; set; }
}
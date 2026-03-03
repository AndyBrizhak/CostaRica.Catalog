using NetTopologySuite.Geometries;

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

    // Навигационное свойство (позволит делать .Include(c => c.Province))
    public Province? Province { get; set; }

    // Координаты центра города (Nullable, как мы и решили)
    public Point? Center { get; set; }
}
namespace CostaRica.Api.Data;

public class MediaAsset
{
    public Guid Id { get; set; }

    // Уникальный слаг для SEO изображения, например: "playa-coco-sunset-view-01"
    public string Slug { get; set; } = string.Empty;

    // Имя файла в хранилище (локальный диск или в будущем R2)
    public string FileName { get; set; } = string.Empty;

    // MIME-тип файла (например, "image/jpeg", "image/png", "image/webp")
    // Необходим для корректной передачи заголовка Content-Type в API
    public string ContentType { get; set; } = string.Empty;

    // Альтернативный текст для SEO (Google Images)
    public string? AltTextEn { get; set; }
    public string? AltTextEs { get; set; }

    // Дата создания для аналитики и сортировки
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Связь "Многие-ко-многим" с бизнес-страницами
    public ICollection<BusinessPage> BusinessPages { get; set; } = new List<BusinessPage>();
}
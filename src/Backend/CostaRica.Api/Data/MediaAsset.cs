namespace CostaRica.Api.Data;

public class MediaAsset
{
    public Guid Id { get; set; }

    // Уникальный слаг для SEO картинки, например: "playa-coco-sunset-view-01"
    public string Slug { get; set; } = string.Empty;

    // Имя файла в хранилище Cloudflare R2
    public string FileName { get; set; } = string.Empty;

    // Альтернативный текст для поисковиков (Google Images)
    public string? AltTextEn { get; set; }
    public string? AltTextEs { get; set; }

    // Дата загрузки для сортировки в админке
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Связь "Многие-ко-многим" с страницами бизнеса
    // Примечание: Мы добавим класс BusinessPage на следующем шаге
    public ICollection<BusinessPage> BusinessPages { get; set; } = new List<BusinessPage>();
}
namespace CostaRica.Api.Data;

public class BusinessSeoSettings
{
    // Классические мета-теги
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Keywords { get; set; }
    public string? CanonicalUrl { get; set; }

    // Управление индексацией (index/noindex)
    public bool NoIndex { get; set; } = false;
    public bool NoFollow { get; set; } = false;

    // Социальные сети (Open Graph)
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgType { get; set; } = "business.business";

    // ID картинки из MediaAsset для превью в соцсетях
    public Guid? OgImageId { get; set; }

    // Ручное управление связями между языковыми версиями (Hreflang)
    public List<ManualHreflang> Hreflangs { get; set; } = [];

    // Подсказки для микроразметки Schema.org
    public string? SchemaType { get; set; } // Например, "Restaurant" или "Hotel"
    public string? PriceRange { get; set; } // Например, "$$" или "$$$"
}

public class ManualHreflang
{
    // Код языка, например "es" или "en"
    public string LangCode { get; set; } = string.Empty;

    // Прямая ссылка на страницу-дубликат
    public string Url { get; set; } = string.Empty;
}
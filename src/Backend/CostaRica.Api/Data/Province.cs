namespace CostaRica.Api.Data;

public class Province
{
    public Guid Id { get; set; }

    // Официальное название, например: "Guanacaste"
    public string Name { get; set; } = string.Empty;

    // SEO-слаг для URL, например: "guanacaste"
    public string Slug { get; set; } = string.Empty;

    // Коллекция городов в этой провинции
    public ICollection<City> Cities { get; set; } = new List<City>();
}
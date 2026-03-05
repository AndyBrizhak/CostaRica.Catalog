namespace CostaRica.Api.Data;

public class TagGroup
{
    public Guid Id { get; set; }

    // Название на английском: "Food & Drink"
    public string NameEn { get; set; } = string.Empty;

    // Название на испанском: "Comida y Bebida"
    public string NameEs { get; set; } = string.Empty;

    // Уникальный слаг для URL или иконок: "food-and-drink"
    public string Slug { get; set; } = string.Empty;
}
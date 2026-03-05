namespace CostaRica.Api.Data;

public class GoogleCategory
{
    public Guid Id { get; set; }

    // Google Category ID (GCID) с pleper.com, например: "abortion_clinic" или "restaurant"
    public string Gcid { get; set; } = string.Empty;

    // Название на английском (Name En)
    public string NameEn { get; set; } = string.Empty;

    // Название на испанском (Name Es)
    public string NameEs { get; set; } = string.Empty;
}
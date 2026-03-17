namespace CostaRica.Api.Data;

public class BusinessContacts
{
    // Сюда пойдут ссылки на соцсети
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }

    // Номера телефонов (храним как long или string, 
    // но long удобнее для автоматического набора номера)
    public long? PhoneCallable { get; set; }
    public long? PhoneWhatsapp { get; set; }

    // Имя владельца (из твоего примера в Монго)
    public string? OwnerName { get; set; }
}
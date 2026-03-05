namespace CostaRica.Api.Data;

public class ScheduleDay
{
    // Список дней недели, к которым относится это время
    // Используем стандартный Enum DayOfWeek (0 = Sunday, 1 = Monday...)
    public List<DayOfWeek> Days { get; set; } = [];

    // Интервалы работы (на случай, если есть перерыв на обед)
    public List<ScheduleInterval> Intervals { get; set; } = [];
}

public class ScheduleInterval
{
    // Время в формате "HH:mm" (например, "08:00", "20:30")
    // Это позволит нам легко сравнивать время как строки или парсить в TimeSpan
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;

    // Дополнительное поле для удобства фильтрации на стороне БД (минуты от начала дня)
    // Например, 08:00 = 480 минут, 20:00 = 1200 минут
    public int StartMinutes { get; set; }
    public int EndMinutes { get; set; }
}
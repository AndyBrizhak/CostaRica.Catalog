namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для абстракции хранилища файлов.
/// Позволяет менять провайдера (Local Disk, Cloudflare R2, S3) без изменения бизнес-логики.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Сохраняет поток данных в файл.
    /// </summary>
    /// <param name="fileStream">Поток данных файла.</param>
    /// <param name="fileName">Имя файла (уже с расширением).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Имя сохраненного файла или null при ошибке.</returns>
    Task<string?> SaveAsync(Stream fileStream, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Получает поток данных файла для чтения.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <returns>Stream или null, если файл не найден.</returns>
    Task<Stream?> GetStreamAsync(string fileName);

    /// <summary>
    /// Удаляет файл из хранилища.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <returns>True, если удаление успешно (или файл уже отсутствует).</returns>
    Task<bool> DeleteAsync(string fileName);
}
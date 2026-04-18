using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация хранилища для работы с локальной файловой системой.
/// Настройки путей и базового URL считываются из конфигурации (appsettings.json).
/// </summary>
public class LocalStorageProvider : IStorageService
{
    private readonly string _storagePath;
    private readonly string _publicUrlPrefix;
    private readonly string _baseUrl;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(IConfiguration configuration, ILogger<LocalStorageProvider> logger)
    {
        _logger = logger;

        // Приоритет отдается значениям из секции Storage в appsettings.json.
        // LocalPath — физический путь на диске.
        _storagePath = configuration["Storage:LocalPath"]
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "media-files");

        // PublicUrlPrefix — префикс пути в URL (например, /media-files).
        _publicUrlPrefix = configuration["Storage:PublicUrlPrefix"] ?? "/media-files";

        // BaseUrl — адрес сервера (например, http://localhost:5046). 
        // Если он задан, API будет возвращать полные ссылки, понятные React Admin.
        _baseUrl = configuration["Storage:BaseUrl"] ?? string.Empty;

        try
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Не удалось создать директорию для хранения медиа по пути: {Path}", _storagePath);
        }
    }

    public async Task<string?> SaveAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        try
        {
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_storagePath, safeFileName);

            using var file = File.Create(filePath);
            await fileStream.CopyToAsync(file, ct);

            return safeFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении файла на диск: {FileName}", fileName);
            return null;
        }
    }

    public Task<Stream?> GetStreamAsync(string fileName)
    {
        try
        {
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_storagePath, safeFileName);

            if (!File.Exists(filePath))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(File.OpenRead(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при открытии потока файла: {FileName}", fileName);
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task<bool> DeleteAsync(string fileName)
    {
        try
        {
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_storagePath, safeFileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении файла: {FileName}", fileName);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Формирует публичный URL. Если в конфигурации задан BaseUrl, возвращается абсолютный путь.
    /// Это позволяет избежать дублирования логики и легко переключаться между окружениями.
    /// </summary>
    public string GetPublicUrl(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;

        // Очищаем префикс и имя файла от лишних слешей для корректной склейки.
        var prefix = _publicUrlPrefix.Trim('/', ' ');
        var name = fileName.TrimStart('/');

        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            // Возвращаем относительный путь, если базовый URL не настроен.
            return $"/{prefix}/{name}";
        }

        // Собираем абсолютный URL (например, http://localhost:5046/media-files/image.jpg).
        var baseUri = _baseUrl.TrimEnd('/');
        return $"{baseUri}/{prefix}/{name}";
    }
}
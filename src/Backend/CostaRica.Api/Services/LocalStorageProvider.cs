using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация хранилища для работы с локальной файловой системой.
/// Использует путь из конфигурации Storage:LocalPath.
/// </summary>
public class LocalStorageProvider : IStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(IConfiguration configuration, ILogger<LocalStorageProvider> logger)
    {
        _logger = logger;

        // Получаем путь из переменной окружения Storage__LocalPath (мапится в Storage:LocalPath)
        // Если путь не задан, создаем папку "media" в корне приложения по умолчанию
        _storagePath = configuration["Storage:LocalPath"]
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "media");

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
            // Гарантируем плоскую структуру: берем только имя файла, игнорируя подпапки в пути
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_storagePath, safeFileName);

            using var targetStream = File.Create(filePath);
            await fileStream.CopyToAsync(targetStream, ct);

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
                _logger.LogWarning("Файл не найден в хранилище: {FileName}", fileName);
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
            _logger.LogError(ex, "Ошибка при удалении файла с диска: {FileName}", fileName);
            return Task.FromResult(false);
        }
    }
}
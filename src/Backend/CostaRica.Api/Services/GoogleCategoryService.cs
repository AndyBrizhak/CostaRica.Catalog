using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class GoogleCategoryService(DirectoryDbContext db) : IGoogleCategoryService
{
    public async Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> GetAllAsync(
        GoogleCategoryQueryParameters parameters,
        CancellationToken ct = default)
    {
        var query = db.GoogleCategories.AsNoTracking().AsQueryable();

        // 1. Фильтрация по конкретным IDs (для запросов GET_MANY от React Admin)
        if (parameters.id is { Length: > 0 })
        {
            query = query.Where(c => parameters.id.Contains(c.Id));
        }

        // 2. Умный поиск (Smart Search)
        // Используем Q (которое мапится из 'q'), как в TagService.
        // EF.Functions.ILike обеспечивает регистронезависимость на уровне PostgreSQL.
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var search = $"%{parameters.Q}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.NameEn, search) ||
                EF.Functions.ILike(c.NameEs, search) ||
                EF.Functions.ILike(c.Gcid, search));
        }

        var totalCount = await query.CountAsync(ct);

        // 3. Умная сортировка (Smart Sort)
        // Полностью исключаем EF.Property. Используем switch по нижнему регистру,
        // чтобы фронтенд мог присылать 'nameEn', 'NameEn' или 'nameen'.
        var isDescending = string.Equals(parameters._order, "DESC", StringComparison.OrdinalIgnoreCase);

        query = parameters._sort?.ToLower() switch
        {
            "gcid" => isDescending ? query.OrderByDescending(c => c.Gcid) : query.OrderBy(c => c.Gcid),
            "namees" => isDescending ? query.OrderByDescending(c => c.NameEs) : query.OrderBy(c => c.NameEs),
            "nameen" => isDescending ? query.OrderByDescending(c => c.NameEn) : query.OrderBy(c => c.NameEn),
            // По умолчанию сортируем по английскому названию
            _ => isDescending ? query.OrderByDescending(c => c.NameEn) : query.OrderBy(c => c.NameEn)
        };

        // 4. Пагинация (_start и _end)
        if (parameters._start.HasValue && parameters._end.HasValue)
        {
            int pageSize = parameters._end.Value - parameters._start.Value;
            if (pageSize > 0)
            {
                query = query.Skip(parameters._start.Value).Take(pageSize);
            }
        }

        var items = await query
            .Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.GoogleCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return c is null ? null : new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs);
    }

    public async Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid, CancellationToken ct = default)
    {
        var c = await db.GoogleCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Gcid == gcid, ct);

        return c is null ? null : new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs);
    }

    public async Task<GoogleCategoryResponseDto> CreateAsync(GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        var category = new GoogleCategory
        {
            Id = Guid.NewGuid(),
            Gcid = dto.Gcid.Trim(),
            NameEn = dto.NameEn.Trim(),
            NameEs = dto.NameEs.Trim()
        };

        db.GoogleCategories.Add(category);
        await db.SaveChangesAsync(ct);

        return new GoogleCategoryResponseDto(category.Id, category.Gcid, category.NameEn, category.NameEs);
    }

    public async Task<GoogleCategoryUpdateResult> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.FindAsync([id], ct);
        if (category is null) return GoogleCategoryUpdateResult.NotFound;

        var hasConflict = await db.GoogleCategories
            .AnyAsync(c => c.Id != id && (c.Gcid == dto.Gcid || c.NameEn == dto.NameEn || c.NameEs == dto.NameEs), ct);

        if (hasConflict) return GoogleCategoryUpdateResult.Conflict;

        category.Gcid = dto.Gcid.Trim();
        category.NameEn = dto.NameEn.Trim();
        category.NameEs = dto.NameEs.Trim();

        await db.SaveChangesAsync(ct);
        return GoogleCategoryUpdateResult.Success;
    }

    public async Task<(GoogleCategoryDeleteResult Result, int UsageCount)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.FindAsync([id], ct);
        if (category is null) return (GoogleCategoryDeleteResult.NotFound, 0);

        var usageCount = await db.BusinessPages
            .CountAsync(bp => bp.PrimaryCategoryId == id || bp.SecondaryCategories.Any(sc => sc.Id == id), ct);

        if (usageCount > 0)
            return (GoogleCategoryDeleteResult.InUse, usageCount);

        db.GoogleCategories.Remove(category);
        await db.SaveChangesAsync(ct);

        return (GoogleCategoryDeleteResult.Success, 0);
    }

    public async Task<BulkImportResponseDto> BulkImportAsync(List<GoogleCategoryImportDto> dtos, CancellationToken ct = default)
    {
        // Проверка на дубликаты внутри входного списка
        var duplicateGcid = dtos.GroupBy(x => x.Gcid).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (duplicateGcid != null)
            return new BulkImportResponseDto(0, true, $"Duplicate GCID found in file: {duplicateGcid}", "Gcid");

        // Проверка на существование в БД
        var existingGcids = await db.GoogleCategories.Select(c => c.Gcid).ToListAsync(ct);
        var conflictGcid = dtos.FirstOrDefault(d => existingGcids.Contains(d.Gcid))?.Gcid;
        if (conflictGcid != null)
            return new BulkImportResponseDto(0, true, $"Category with GCID '{conflictGcid}' already exists in database.", "Gcid");

        var newCategories = dtos.Select(dto => new GoogleCategory
        {
            Id = Guid.NewGuid(),
            Gcid = dto.Gcid.Trim(),
            NameEn = dto.NameEn.Trim(),
            NameEs = dto.NameEs.Trim()
        }).ToList();

        db.GoogleCategories.AddRange(newCategories);
        await db.SaveChangesAsync(ct);

        return new BulkImportResponseDto(newCategories.Count, false);
    }
}
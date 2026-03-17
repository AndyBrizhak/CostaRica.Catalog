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

        // 1. Фильтрация по списку ID (GET_MANY для React Admin)
        if (parameters.id != null && parameters.id.Length > 0)
        {
            query = query.Where(c => parameters.id.Contains(c.Id));
        }

        // 2. Глобальный поиск (q)
        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var lowerQ = parameters.q.ToLower();
            query = query.Where(c =>
                c.NameEn.ToLower().Contains(lowerQ) ||
                c.NameEs.ToLower().Contains(lowerQ) ||
                c.Gcid.ToLower().Contains(lowerQ));
        }

        // 3. Подсчет общего количества до применения пагинации
        var totalCount = await query.CountAsync(ct);

        // 4. Динамическая сортировка (_sort и _order)
        if (!string.IsNullOrWhiteSpace(parameters._sort))
        {
            var isDesc = parameters._order?.ToUpper() == "DESC";
            query = parameters._sort.ToLower() switch
            {
                "gcid" => isDesc ? query.OrderByDescending(c => c.Gcid) : query.OrderBy(c => c.Gcid),
                "nameen" => isDesc ? query.OrderByDescending(c => c.NameEn) : query.OrderBy(c => c.NameEn),
                "namees" => isDesc ? query.OrderByDescending(c => c.NameEs) : query.OrderBy(c => c.NameEs),
                _ => isDesc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
            };
        }

        // 5. Пагинация (_start и _end)
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 20;
        var pageSize = end - start;

        var items = await query
            .Skip(start)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.Select(MapToDto), totalCount);
    }

    public async Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return category is null ? null : MapToDto(category);
    }

    public async Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Gcid == gcid, ct);
        return category is null ? null : MapToDto(category);
    }

    public async Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        // Exception-free: просто возвращаем null, если GCID занят
        if (await db.GoogleCategories.AnyAsync(c => c.Gcid == dto.Gcid, ct))
            return null;

        var category = new GoogleCategory
        {
            Id = Guid.NewGuid(),
            Gcid = dto.Gcid,
            NameEn = dto.NameEn,
            NameEs = dto.NameEs
        };

        db.GoogleCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return MapToDto(category);
    }

    public async Task<int> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories, CancellationToken ct = default)
    {
        var existingGcids = await db.GoogleCategories.Select(c => c.Gcid).ToHashSetAsync(ct);

        var newCategories = categories
            .GroupBy(c => c.Gcid) // Защита от дублей в самом входном списке
            .Select(g => g.First())
            .Where(c => !existingGcids.Contains(c.Gcid))
            .Select(c => new GoogleCategory
            {
                Id = Guid.NewGuid(),
                Gcid = c.Gcid,
                NameEn = c.NameEn,
                NameEs = c.NameEs
            })
            .ToList();

        if (newCategories.Count == 0) return 0;

        db.GoogleCategories.AddRange(newCategories);
        return await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.FindAsync([id], ct);
        if (category is null) return false;

        // Защита: нельзя сменить GCID на уже существующий у другой категории
        if (await db.GoogleCategories.AnyAsync(c => c.Gcid == dto.Gcid && c.Id != id, ct))
            return false;

        category.Gcid = dto.Gcid;
        category.NameEn = dto.NameEn;
        category.NameEs = dto.NameEs;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.FindAsync([id], ct);
        if (category is null) return false;

        db.GoogleCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static GoogleCategoryResponseDto MapToDto(GoogleCategory c)
        => new(c.Id, c.Gcid, c.NameEn, c.NameEs);
}
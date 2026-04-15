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

        // 1. Filtering by IDs
        if (parameters.id is { Length: > 0 })
        {
            query = query.Where(c => parameters.id.Contains(c.Id));
        }

        // 2. Search (q)
        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var lowerQ = parameters.q.ToLower();
            query = query.Where(c =>
                c.NameEn.ToLower().Contains(lowerQ) ||
                c.NameEs.ToLower().Contains(lowerQ) ||
                c.Gcid.ToLower().Contains(lowerQ));
        }

        var totalCount = await query.CountAsync(ct);

        // 3. Sorting
        query = parameters._order?.ToUpper() == "DESC"
            ? query.OrderByDescending(c => EF.Property<object>(c, parameters._sort ?? "NameEn"))
            : query.OrderBy(c => EF.Property<object>(c, parameters._sort ?? "NameEn"));

        // 4. Pagination
        if (parameters._start.HasValue && parameters._end.HasValue)
        {
            var take = parameters._end.Value - parameters._start.Value;
            query = query.Skip(parameters._start.Value).Take(take);
        }

        var items = await query
            .Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return category is null ? null : new GoogleCategoryResponseDto(category.Id, category.Gcid, category.NameEn, category.NameEs);
    }

    public async Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Gcid == gcid, ct);
        return category is null ? null : new GoogleCategoryResponseDto(category.Id, category.Gcid, category.NameEn, category.NameEs);
    }

    public async Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Gcid)) return null;

        // Ensure unique GCID and Names
        var exists = await db.GoogleCategories.AnyAsync(c =>
            c.Gcid == dto.Gcid || c.NameEn == dto.NameEn || c.NameEs == dto.NameEs, ct);

        if (exists) return null;

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

    public async Task<BulkImportResponseDto> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories, CancellationToken ct = default)
    {
        var list = categories.ToList();

        // Step 1: Internal duplicates check (within the uploaded file)
        var duplicateInList = list.GroupBy(c => c.Gcid).FirstOrDefault(g => g.Count() > 1);
        if (duplicateInList != null)
            return new BulkImportResponseDto(0, true, $"Conflict: GCID '{duplicateInList.Key}' is duplicated within the source file.", "Gcid");

        // Step 2: Atomic database validation (stops at the first conflict)
        foreach (var item in list)
        {
            var conflict = await db.GoogleCategories
                .AsNoTracking()
                .Where(c => c.Gcid == item.Gcid || c.NameEn == item.NameEn || c.NameEs == item.NameEs)
                .Select(c => new { c.Gcid, c.NameEn, c.NameEs })
                .FirstOrDefaultAsync(ct);

            if (conflict != null)
            {
                string field = conflict.Gcid == item.Gcid ? "Gcid" : (conflict.NameEn == item.NameEn ? "NameEn" : "NameEs");
                string value = conflict.Gcid == item.Gcid ? conflict.Gcid : (conflict.NameEn == item.NameEn ? conflict.NameEn : conflict.NameEs);

                return new BulkImportResponseDto(0, true,
                    $"Conflict: Category with {field} '{value}' already exists. Please resolve conflicts or clean the database before importing.",
                    field);
            }
        }

        // Step 3: Atomic Insert
        var entities = list.Select(c => new GoogleCategory
        {
            Id = Guid.NewGuid(),
            Gcid = c.Gcid.Trim(),
            NameEn = c.NameEn.Trim(),
            NameEs = c.NameEs.Trim()
        }).ToList();

        db.GoogleCategories.AddRange(entities);
        await db.SaveChangesAsync(ct);

        return new BulkImportResponseDto(entities.Count, false);
    }

    public async Task<GoogleCategoryUpdateResult> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.FindAsync([id], ct);
        if (category is null) return GoogleCategoryUpdateResult.NotFound;

        // Conflict check: ensure no other record has the same Gcid or Names
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

        // Check dependencies in both Primary and Secondary category links
        var usageCount = await db.BusinessPages
            .CountAsync(bp => bp.PrimaryCategoryId == id || bp.SecondaryCategories.Any(sc => sc.Id == id), ct);

        if (usageCount > 0)
            return (GoogleCategoryDeleteResult.InUse, usageCount);

        db.GoogleCategories.Remove(category);
        await db.SaveChangesAsync(ct);

        return (GoogleCategoryDeleteResult.Success, 0);
    }
}

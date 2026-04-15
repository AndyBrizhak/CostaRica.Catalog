using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Response DTO for displaying category in lists and forms.
/// </summary>
public record GoogleCategoryResponseDto(
    Guid Id,
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// DTO for creating or updating a single category.
/// </summary>
public record GoogleCategoryUpsertDto(
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// DTO used for bulk import from JSON.
/// </summary>
public record GoogleCategoryImportDto(
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// Response for atomic bulk import operations.
/// </summary>
/// <param name="ImportedCount">Total records imported (0 if any conflict occurs).</param>
/// <param name="HasConflict">Indicates if a conflict was detected during validation.</param>
/// <param name="ErrorMessage">English message describing the first conflict found.</param>
/// <param name="ConflictType">Field that caused the conflict: Gcid, NameEn, or NameEs.</param>
public record BulkImportResponseDto(
    int ImportedCount,
    bool HasConflict,
    string? ErrorMessage = null,
    string? ConflictType = null);

/// <summary>
/// Query parameters for filtering and pagination in Google Categories list.
/// </summary>
public record GoogleCategoryQueryParameters(
    [FromQuery] int? _start = 0,
    [FromQuery] int? _end = 20,
    [FromQuery] string? _sort = "NameEn",
    [FromQuery] string? _order = "ASC",
    [FromQuery] string? q = null,
    [FromQuery] Guid[]? id = null);
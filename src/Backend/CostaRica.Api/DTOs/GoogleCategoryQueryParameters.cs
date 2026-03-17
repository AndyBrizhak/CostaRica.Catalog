using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.DTOs;

public record GoogleCategoryQueryParameters(
    [FromQuery] int? _start = 0,
    [FromQuery] int? _end = 20,
    [FromQuery] string? _sort = "NameEn",
    [FromQuery] string? _order = "ASC",
    [FromQuery] string? q = null,
    [FromQuery] Guid[]? id = null
);
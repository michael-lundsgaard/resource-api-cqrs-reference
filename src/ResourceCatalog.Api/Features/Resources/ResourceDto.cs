namespace ResourceCatalog.Api.Features.Resources
{
    public record ResourceDto(
        Guid Id,
        string Name,
        string? Description,
        DateTimeOffset CreatedAt,
        List<TagDto>? Tags = null  // null when not expanded, empty list when expanded but no tags
    );

    public record TagDto(Guid Id, string Label);

    public record CreateResourceRequest(string Name, string? Description, List<string>? Tags);
    public record UpdateResourceRequest(string Name, string? Description, List<string>? Tags);
}
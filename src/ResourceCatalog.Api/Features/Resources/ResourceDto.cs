namespace ResourceCatalog.Api.Features.Resources
{
    public record ResourceDto(
        Guid Id,
        string Name,
        string? Description,
        DateTimeOffset CreatedAt
    );

    public record CreateResourceRequest(string Name, string? Description);
    public record UpdateResourceRequest(string Name, string? Description);
}
using ResourceCatalog.Api.Entities;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class MappingExtensions
    {
        public static ResourceDto ToDto(this Resource r, bool expandTags = false) =>
            new(
                r.Id,
                r.Name,
                r.Description,
                r.CreatedAt,
                expandTags ? r.Tags.Select(t => t.ToDto()).ToList() : null
            );

        public static TagDto ToDto(this Tag t) =>
            new(t.Id, t.Label);
    }
}
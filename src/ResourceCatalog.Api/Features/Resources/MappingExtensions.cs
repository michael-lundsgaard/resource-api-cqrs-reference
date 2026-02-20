using ResourceCatalog.Api.Entities;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class MappingExtensions
    {
        public static ResourceDto ToDto(this Resource r) =>
            new(r.Id, r.Name, r.Description, r.CreatedAt);
    }
}
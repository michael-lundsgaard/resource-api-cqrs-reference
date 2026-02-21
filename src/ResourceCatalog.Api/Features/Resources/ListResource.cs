using MediatR;
using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Data;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class ListResource
    {
        public record Query(bool ExpandTags, List<string>? TagFilters) : IRequest<List<ResourceDto>>;

        public class Handler(AppDbContext db) : IRequestHandler<Query, List<ResourceDto>>
        {
            public async Task<List<ResourceDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var query = db.Resources.AsNoTracking();

                // Conditionally include tags
                if (request.ExpandTags)
                    query = query.Include(r => r.Tags);

                // Apply tag filtering if specified
                if (request.TagFilters is not null && request.TagFilters.Count > 0)
                {
                    query = query.Where(r => r.Tags.Any(t => request.TagFilters.Contains(t.Label)));
                }

                return await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.ToDto(request.ExpandTags))
                    .ToListAsync(cancellationToken);
            }
        }
    }
}
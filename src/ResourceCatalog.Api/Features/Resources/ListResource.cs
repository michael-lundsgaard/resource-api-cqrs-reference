using MediatR;
using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Data;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class ListResource
    {
        public record Query() : IRequest<List<ResourceDto>>;

        public class Handler(AppDbContext db) : IRequestHandler<Query, List<ResourceDto>>
        {
            public async Task<List<ResourceDto>> Handle(Query request, CancellationToken ct)
            {
                return await db.Resources
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.ToDto())
                    .ToListAsync(ct);
            }
        }
    }
}
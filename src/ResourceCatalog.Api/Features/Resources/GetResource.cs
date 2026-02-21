using MediatR;
using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Data;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class GetResource
    {
        // The Query is a plain record — no framework coupling, trivially testable
        public record Query(Guid Id, bool ExpandTags) : IRequest<Result>;

        // Discriminated union — forces the controller to handle every outcome explicitly.
        // In a standard service you'd return null or throw; the controller would guess what that means.
        public abstract record Result;
        public record Found(ResourceDto Resource) : Result;
        public record NotFound : Result;

        public class Handler(AppDbContext db) : IRequestHandler<Query, Result>
        {
            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                var query = db.Resources.AsNoTracking();

                // Conditionally include tags based on expand parameter
                if (request.ExpandTags)
                    query = query.Include(r => r.Tags);

                var resource = await query.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

                if (resource is null) return new NotFound();

                return new Found(resource.ToDto(request.ExpandTags));
            }
        }
    }
}
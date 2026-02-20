using MediatR;
using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Data;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class DeleteResource
    {
        public record Command(Guid Id) : IRequest<Result>;

        public abstract record Result;
        public record Success : Result;
        public record NotFound : Result;

        public class Handler(AppDbContext db) : IRequestHandler<Command, Result>
        {
            public async Task<Result> Handle(Command request, CancellationToken ct)
            {
                var resource = await db.Resources.FirstOrDefaultAsync(r => r.Id == request.Id, ct);

                if (resource is null) return new NotFound();

                db.Resources.Remove(resource);
                await db.SaveChangesAsync(ct);
                return new Success();
            }
        }
    }
}
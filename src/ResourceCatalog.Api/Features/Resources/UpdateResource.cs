using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Data;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class UpdateResource
    {
        public record Command(Guid Id, string Name, string? Description) : IRequest<Result>;

        public abstract record Result;
        public record Success(ResourceDto Resource) : Result;
        public record NotFound : Result;

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                RuleFor(x => x.Description).MaximumLength(2_000).When(x => x.Description is not null);
            }
        }

        public class Handler(AppDbContext db) : IRequestHandler<Command, Result>
        {
            public async Task<Result> Handle(Command request, CancellationToken ct)
            {
                var resource = await db.Resources.FirstOrDefaultAsync(r => r.Id == request.Id, ct);

                if (resource is null) return new NotFound();

                resource.Name = request.Name;
                resource.Description = request.Description;
                await db.SaveChangesAsync(ct);

                return new Success(resource.ToDto());
            }
        }
    }
}
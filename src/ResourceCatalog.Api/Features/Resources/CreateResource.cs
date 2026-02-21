using FluentValidation;
using MediatR;
using ResourceCatalog.Api.Data;
using ResourceCatalog.Api.Entities;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class CreateResource
    {
        public record Command(string Name, string? Description) : IRequest<ResourceDto>;

        // Validator is co-located with the command it validates.
        // In a standard service validation is often scattered: some in the controller,
        // some in the service method, some missing entirely.
        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                RuleFor(x => x.Description).MaximumLength(2_000).When(x => x.Description is not null);
            }
        }

        public class Handler(AppDbContext db) : IRequestHandler<Command, ResourceDto>
        {
            public async Task<ResourceDto> Handle(Command request, CancellationToken cancellationToken)
            {
                var resource = new Resource
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                db.Resources.Add(resource);
                await db.SaveChangesAsync(cancellationToken);
                return resource.ToDto();
            }
        }
    }
}
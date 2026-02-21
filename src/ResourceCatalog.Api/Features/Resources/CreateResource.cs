using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResourceCatalog.Api.Data;
using ResourceCatalog.Api.Entities;

namespace ResourceCatalog.Api.Features.Resources
{
    public static class CreateResource
    {
        public record Command(string Name, string? Description, List<string>? Tags) : IRequest<ResourceDto>;

        // Validator is co-located with the command it validates.
        // In a standard service validation is often scattered: some in the controller,
        // some in the service method, some missing entirely.
        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                RuleFor(x => x.Description).MaximumLength(2_000).When(x => x.Description is not null);

                // Tag validation
                RuleFor(x => x.Tags)
                    .Must(tags => tags is null || tags.Count <= 10)
                    .WithMessage("A resource can have a maximum of 10 tags.");

                RuleForEach(x => x.Tags)
                    .NotEmpty()
                    .WithMessage("Tag label cannot be empty or whitespace.")
                    .MaximumLength(50)
                    .WithMessage("Tag label cannot exceed 50 characters.");

                RuleFor(x => x.Tags)
                    .Must(HaveUniqueLabels)
                    .WithMessage("Duplicate tag labels are not allowed.")
                    .When(x => x.Tags is not null && x.Tags.Count > 0);
            }

            private static bool HaveUniqueLabels(List<string>? tags)
            {
                if (tags is null) return true;
                return tags.Count == tags.Distinct().Count();
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
                    CreatedAt = DateTimeOffset.UtcNow,
                    Tags = new List<Tag>()
                };

                // Process tags if provided
                if (request.Tags is not null && request.Tags.Count > 0)
                {
                    var tags = await GetOrCreateTagsAsync(request.Tags, cancellationToken);
                    resource.Tags = tags;
                }

                db.Resources.Add(resource);
                await db.SaveChangesAsync(cancellationToken);

                return resource.ToDto(expandTags: false);
            }

            private async Task<List<Tag>> GetOrCreateTagsAsync(
                List<string> labelStrings,
                CancellationToken cancellationToken)
            {
                // Query existing tags by labels
                var existingTags = await db.Tags
                    .Where(t => labelStrings.Contains(t.Label))
                    .ToListAsync(cancellationToken);

                // Identify new labels
                var existingLabels = existingTags.Select(t => t.Label).ToHashSet();
                var newLabels = labelStrings.Except(existingLabels).ToList();

                // Create new tags
                var newTags = newLabels.Select(label => new Tag
                {
                    Id = Guid.NewGuid(),
                    Label = label
                }).ToList();

                db.Tags.AddRange(newTags);

                return existingTags.Concat(newTags).ToList();
            }
        }
    }
}
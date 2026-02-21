# Design: Resource Tags

## Endpoints

### Modified: GET /api/v1/resources/{id}

- **Method + Route**: `GET /api/v1/resources/{id}`
- **Auth**: None (public API)
- **Query Parameters**:
    - `expand` (optional): Comma-separated list of fields to include (e.g., `tags` or `tags,other`)
- **Response**: `ResourceDto` (with or without tags based on expand parameter)
- **Status codes**:
    - `200 OK` — Resource found
    - `404 Not Found` — Resource with specified ID does not exist

### Modified: GET /api/v1/resources

- **Method + Route**: `GET /api/v1/resources`
- **Auth**: None (public API)
- **Query Parameters**:
    - `expand` (optional): Comma-separated list of fields to include (e.g., `tags`)
    - `tags` (optional): Comma-separated list of tag labels to filter by (OR logic)
- **Response**: `List<ResourceDto>` (with or without tags based on expand parameter)
- **Status codes**:
    - `200 OK` — Always succeeds, returns empty list if no matches

### Modified: POST /api/v1/resources

- **Method + Route**: `POST /api/v1/resources`
- **Auth**: None (public API)
- **Request body**:

```csharp
public record CreateResourceRequest(
    string Name,
    string? Description,
    List<string>? Tags
);
```

- **Response**: `ResourceDto` (without tags by default, matches current behavior)
- **Status codes**:
    - `201 Created` — Resource created successfully
    - `400 Bad Request` — Validation failure (duplicate tags, >10 tags, empty labels, etc.)

### Modified: PUT /api/v1/resources/{id}

- **Method + Route**: `PUT /api/v1/resources/{id}`
- **Auth**: None (public API)
- **Request body**:

```csharp
public record UpdateResourceRequest(
    string Name,
    string? Description,
    List<string>? Tags
);
```

- **Response**: `ResourceDto` (without tags by default, matches current behavior)
- **Status codes**:
    - `200 OK` — Resource updated successfully
    - `404 Not Found` — Resource with specified ID does not exist
    - `400 Bad Request` — Validation failure

## MediatR Commands / Queries

### Modified: GetResource.Query

```csharp
public record Query(Guid Id, bool ExpandTags) : IRequest<Result>;

public abstract record Result;
public record Found(ResourceDto Resource) : Result;
public record NotFound : Result;
```

### Modified: ListResource.Query

```csharp
public record Query(bool ExpandTags, List<string>? TagFilters) : IRequest<List<ResourceDto>>;
```

### Modified: CreateResource.Command

```csharp
public record Command(
    string Name,
    string? Description,
    List<string>? Tags
) : IRequest<ResourceDto>;
```

### Modified: UpdateResource.Command

```csharp
public record Command(
    Guid Id,
    string Name,
    string? Description,
    List<string>? Tags
) : IRequest<Result>;

public abstract record Result;
public record Success(ResourceDto Resource) : Result;
public record NotFound : Result;
```

### New: TagDto

```csharp
public record TagDto(Guid Id, string Label);
```

### Modified: ResourceDto

```csharp
public record ResourceDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    List<TagDto>? Tags = null  // null when not expanded, empty list when expanded but no tags
);
```

## Handler Outlines

### GetResource.Handler

```csharp
public class Handler(AppDbContext db) : IRequestHandler<Query, Result>
{
    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        // Build query with conditional Include based on ExpandTags
        var query = db.Resources.AsNoTracking();
        if (request.ExpandTags)
            query = query.Include(r => r.Tags);

        var resource = await query.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (resource is null) return new NotFound();

        return new Found(resource.ToDto(request.ExpandTags));
    }
}
```

### ListResource.Handler

```csharp
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
```

### CreateResource.Handler

```csharp
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
```

### UpdateResource.Handler

```csharp
public class Handler(AppDbContext db) : IRequestHandler<Command, Result>
{
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var resource = await db.Resources
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (resource is null) return new NotFound();

        resource.Name = request.Name;
        resource.Description = request.Description;

        // Update tags only if explicitly provided
        if (request.Tags is not null)
        {
            resource.Tags.Clear();
            if (request.Tags.Count > 0)
            {
                var tags = await GetOrCreateTagsAsync(request.Tags, cancellationToken);
                resource.Tags = tags;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return new Success(resource.ToDto(expandTags: false));
    }

    // Same helper method as CreateResource
    private async Task<List<Tag>> GetOrCreateTagsAsync(...)
    {
        // Implementation identical to CreateResource.Handler
    }
}
```

## Validation Rules (FluentValidation)

### CreateResource.Validator (Modified)

```csharp
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
```

### UpdateResource.Validator (Modified)

```csharp
public class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2_000).When(x => x.Description is not null);

        // Same tag validation as CreateResource
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
```

## Data Layer

### Entity Changes Required: Yes

#### New Entity: Tag

```csharp
namespace ResourceCatalog.Api.Entities
{
    public class Tag
    {
        public Guid Id { get; set; }
        public required string Label { get; set; }

        // Navigation property for many-to-many
        public List<Resource> Resources { get; set; } = new();
    }
}
```

#### Modified Entity: Resource

```csharp
namespace ResourceCatalog.Api.Entities
{
    public class Resource
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation property for many-to-many
        public List<Tag> Tags { get; set; } = new();
    }
}
```

### New Migration Required: Yes

Migration will create:

- `Tags` table with columns: `Id` (PK), `Label` (unique index)
- Join table `ResourceTag` with columns: `ResourcesId` (FK), `TagsId` (FK)

### EF Core Configuration (AppDbContext.OnModelCreating)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Existing Resource configuration
    modelBuilder.Entity<Resource>(e =>
    {
        e.HasKey(r => r.Id);
        e.Property(r => r.Name).HasMaxLength(200).IsRequired();

        // Many-to-many relationship
        e.HasMany(r => r.Tags)
         .WithMany(t => t.Resources);
    });

    // New Tag configuration
    modelBuilder.Entity<Tag>(e =>
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.Label).HasMaxLength(50).IsRequired();
        e.HasIndex(t => t.Label).IsUnique();
    });
}
```

### Modified: MappingExtensions

```csharp
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
```

### Controller Modifications

```csharp
[HttpGet]
public async Task<IActionResult> List(
    [FromQuery] string? expand,
    [FromQuery] string? tags,
    CancellationToken cancellationToken)
{
    var expandTags = ParseExpandParameter(expand, "tags");
    var tagFilters = ParseCommaSeparatedParameter(tags);

    var result = await mediator.Send(
        new ListResource.Query(expandTags, tagFilters),
        cancellationToken);

    return Ok(result);
}

[HttpGet("{id:guid}")]
public async Task<IActionResult> Get(
    Guid id,
    [FromQuery] string? expand,
    CancellationToken cancellationToken)
{
    var expandTags = ParseExpandParameter(expand, "tags");

    return await mediator.Send(new GetResource.Query(id, expandTags), cancellationToken) switch
    {
        GetResource.Found result => Ok(result.Resource),
        _ => NotFound()
    };
}

[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateResourceRequest body,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new CreateResource.Command(body.Name, body.Description, body.Tags),
        cancellationToken);

    return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
}

[HttpPut("{id:guid}")]
public async Task<IActionResult> Update(
    Guid id,
    [FromBody] UpdateResourceRequest body,
    CancellationToken cancellationToken)
{
    return await mediator.Send(
        new UpdateResource.Command(id, body.Name, body.Description, body.Tags),
        cancellationToken) switch
    {
        UpdateResource.Success result => Ok(result.Resource),
        _ => NotFound()
    };
}

// Helper methods
private static bool ParseExpandParameter(string? expand, string fieldName)
{
    if (string.IsNullOrWhiteSpace(expand)) return false;
    var fields = expand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return fields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
}

private static List<string>? ParseCommaSeparatedParameter(string? parameter)
{
    if (string.IsNullOrWhiteSpace(parameter)) return null;
    return parameter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
```

## Test Plan

### Happy Path Tests

- [ ] **Create resource with tags** — POST with valid tags returns 201, tags are stored
- [ ] **Create resource without tags** — POST without tags returns 201, resource has empty tags collection
- [ ] **Get resource with expand=tags** — Returns 200 with tags array populated
- [ ] **Get resource without expand** — Returns 200 without tags field
- [ ] **List resources with expand=tags** — Returns 200 with tags included for all resources
- [ ] **List resources without expand** — Returns 200 without tags field
- [ ] **Update resource tags** — PUT with new tags returns 200, tags are replaced
- [ ] **Clear resource tags** — PUT with empty tags array returns 200, all tags removed
- [ ] **Filter by single tag** — GET /api/v1/resources?tags=React returns only matching resources
- [ ] **Filter by multiple tags** — GET /api/v1/resources?tags=React,Learning returns resources with ANY of those tags
- [ ] **Tag reuse** — Creating two resources with same tag label reuses the same tag entity

### Edge Cases / Validation Tests

- [ ] **Duplicate tag labels in request** — Returns 400 with validation error
- [ ] **More than 10 tags** — Returns 400 with validation error
- [ ] **Empty tag label** — Returns 400 with validation error
- [ ] **Whitespace-only tag label** — Returns 400 with validation error
- [ ] **Tag label exceeds 50 characters** — Returns 400 with validation error
- [ ] **Resource with no tags + expand=tags** — Returns 200 with empty tags array `[]`
- [ ] **Filter by non-existent tag** — Returns 200 with empty list
- [ ] **Update without tags field** — Returns 200, existing tags are preserved
- [ ] **Expand parameter case-insensitive** — `?expand=Tags` works same as `?expand=tags`
- [ ] **Multiple expand values** — `?expand=tags,other` correctly parses tags

### Not Found Tests

- [ ] **Get non-existent resource with expand** — Returns 404
- [ ] **Update non-existent resource with tags** — Returns 404

### Integration Tests

- [ ] **End-to-end tag lifecycle** — Create resource with tags, update tags, filter by tags, verify consistency
- [ ] **Concurrent tag creation** — Two requests creating same tag label simultaneously don't cause conflicts (unique constraint)
- [ ] **Tag persistence** — Tags survive resource deletion (many-to-many means tags are independent entities)

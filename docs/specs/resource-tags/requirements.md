# Requirements: Resource Tags

## Summary

Add support for tagging resources with reusable labels. Tags are embedded in the Resource entity as a collection of Id/Label pairs. Tags are not returned by default and must be explicitly requested via an `expand` query parameter. Resources can be filtered by tags in the LIST endpoint.

## User Stories

- As a **React app developer**, I want to add tags to resources so that I can categorize and organize them
- As a **React app developer**, I want to filter resources by tags so that I can find related resources quickly
- As a **React app developer**, I want to optionally include tags in GET/LIST responses so that I can avoid fetching unnecessary data when tags aren't needed
- As a **React app developer**, I want tags to be reusable across resources so that I can maintain consistent categorization

## Acceptance Criteria

- [ ] A `Resource` entity has an optional collection of tags (defaults to empty list)
- [ ] Each tag consists of a `Guid Id` and a `string Label` (e.g., "Learning", "React", "dotnet")
- [ ] Tags are NOT returned by default from GET or LIST endpoints
- [ ] Tags are included when the `?expand=tags` query parameter is present
- [ ] The expand parameter supports multiple values (e.g., `?expand=tags,other`)
- [ ] Both GET single resource (`/api/v1/resources/{id}`) and LIST (`/api/v1/resources`) support the expand parameter
- [ ] Resources can be created with tags by providing an array of label strings
- [ ] Resources can be updated with tags by providing an array of label strings (replaces existing tags)
- [ ] Tags can be removed from a resource by updating with an empty tags array or omitting tags
- [ ] LIST endpoint supports filtering by tag labels (e.g., `?tags=React,Learning`)
- [ ] A resource can have up to 10 tags maximum
- [ ] Tags are automatically created when a new label is provided
- [ ] Tags are reused across resources (same label = same tag entity)
- [ ] Tag labels cannot be empty or whitespace
- [ ] A resource cannot contain duplicate tags (case-sensitive comparison)
- [ ] If a resource has no tags and `?expand=tags` is requested, return an empty list

## Edge Cases

| Case                                                         | Expected Behavior                                                 |
| ------------------------------------------------------------ | ----------------------------------------------------------------- |
| Create/update resource with empty tags array                 | Allowed. Resource has zero tags.                                  |
| Create/update resource with new tag label                    | Tag is automatically created and associated with resource.        |
| Create/update resource with existing tag label               | Existing tag is reused (referenced by resource).                  |
| Create/update resource with duplicate labels in request      | Validation error (400) - duplicates not allowed (case-sensitive). |
| Create/update resource with >10 tags                         | Validation error (400) - maximum 10 tags allowed.                 |
| Create/update resource with empty/whitespace tag label       | Validation error (400) - tag label cannot be empty.               |
| GET/LIST without `?expand=tags`                              | Tags are not included in response.                                |
| GET/LIST with `?expand=tags` when resource has no tags       | Return empty tags array `[]`.                                     |
| LIST with `?tags=NonExistent` filter                         | Return empty list (no resources match).                           |
| LIST with `?tags=React,Learning` filter (multiple)           | Return resources that have ANY of the specified tags (OR logic).  |
| Update resource with empty array explicitly removes all tags | Allowed. Resource tags are cleared.                               |

## Request/Response Contracts

### CreateResourceRequest

```json
{
	"name": "string (required)",
	"description": "string (optional)",
	"tags": ["string", "string"] // optional, defaults to empty
}
```

### UpdateResourceRequest

```json
{
	"name": "string (required)",
	"description": "string (optional)",
	"tags": ["string", "string"] // optional, when omitted keeps existing tags
}
```

### ResourceDto (without expand)

```json
{
	"id": "guid",
	"name": "string",
	"description": "string | null",
	"createdAt": "datetime"
}
```

### ResourceDto (with ?expand=tags)

```json
{
	"id": "guid",
	"name": "string",
	"description": "string | null",
	"createdAt": "datetime",
	"tags": [
		{ "id": "guid", "label": "string" },
		{ "id": "guid", "label": "string" }
	]
}
```

## Validation Rules

- Tag label: required, not empty/whitespace, max length 50 characters
- Tags collection: max 10 items per resource
- No duplicate tag labels in a single resource (case-sensitive)
- Tag labels are case-sensitive for equality comparison

## Out of Scope

- Authentication/authorization (public API for now)
- Pagination for resources or tags
- Caching strategies
- Rate limiting
- Tag usage analytics (e.g., "most popular tags")
- Separate tag management endpoints (tags are managed through resource operations only)
- Fuzzy/partial tag matching in filters
- Tag aliasing or hierarchies
- Bulk tagging operations

## Open Questions

None - all questions have been answered.

## Technical Notes

- Tags should be modeled as a separate `Tag` entity with many-to-many relationship to `Resource`
- The many-to-many join table is needed for EF Core to manage the relationship
- Tag labels should be unique at the database level to prevent duplicate tag creation
- When processing tag labels from requests, lookup existing tags by label and reuse them
- The expand parameter should be parsed as a comma-separated list of field names
- Filtering by tags should use OR logic (resource matches if it has ANY of the specified tags)

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ResourceCatalog.Api.Features.Resources;

namespace ResourceCatalog.Api.Controllers
{
    [ApiController]
    [Route("api/v1/resources")]
    public class ResourcesController(IMediator mediator) : ControllerBase
    {
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

            // Pattern matching on the discriminated union — every outcome is handled explicitly.
            // A standard service returning null forces the controller to guess what null means.
            return await mediator.Send(new GetResource.Query(id, expandTags), cancellationToken) switch
            {
                GetResource.Found result => Ok(result.Resource),
                _ => NotFound()
            };
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateResourceRequest body, CancellationToken cancellationToken)
        {
            // ValidationBehavior throws ValidationException before the handler runs if invalid.
            // The exception handler in Program.cs converts it to a 400 ValidationProblem.
            var result = await mediator.Send(
                new CreateResource.Command(body.Name, body.Description, body.Tags),
                cancellationToken);

            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResourceRequest body, CancellationToken cancellationToken)
        {
            return await mediator.Send(
                new UpdateResource.Command(id, body.Name, body.Description, body.Tags),
                cancellationToken) switch
            {
                UpdateResource.Success result => Ok(result.Resource),
                _ => NotFound()
            };
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            return await mediator.Send(new DeleteResource.Command(id), cancellationToken) switch
            {
                DeleteResource.Success => NoContent(),
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
    }
}
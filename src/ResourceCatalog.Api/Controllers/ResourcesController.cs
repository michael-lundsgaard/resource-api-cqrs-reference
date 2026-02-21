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
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new ListResource.Query(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        {
            // Pattern matching on the discriminated union — every outcome is handled explicitly.
            // A standard service returning null forces the controller to guess what null means.
            return await mediator.Send(new GetResource.Query(id), cancellationToken) switch
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
            var result = await mediator.Send(new CreateResource.Command(body.Name, body.Description), cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResourceRequest body, CancellationToken cancellationToken)
        {
            return await mediator.Send(new UpdateResource.Command(id, body.Name, body.Description), cancellationToken) switch
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
    }
}
using FluentValidation;
using MediatR;

namespace ResourceCatalog.Api.Infrastructure
{
    // Runs before every handler. Validation is a cross-cutting concern — it belongs in the pipeline,
    // not duplicated across every handler or scattered between controller action filters and service methods.
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (!validators.Any()) return await next(ct);

            var context = new ValidationContext<TRequest>(request);
            var failures = validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0) throw new ValidationException(failures);

            return await next(ct);
        }
    }
}
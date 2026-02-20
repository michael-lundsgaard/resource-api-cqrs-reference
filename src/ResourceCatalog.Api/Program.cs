using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using ResourceCatalog.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Extension methods to keep Program.cs clean and organized
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddMediator();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Convert ValidationException → 400 ValidationProblem
app.UseExceptionHandler(exApp => exApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (ex is not ValidationException ve) return;

    var errors = ve.Errors
        .GroupBy(e => e.PropertyName)
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

    ctx.Response.StatusCode = 400;
    await ctx.Response.WriteAsJsonAsync(
        new HttpValidationProblemDetails(errors) { Status = 400 });
}));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
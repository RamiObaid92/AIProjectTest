using Library.Application.Resources;
using Library.Application.Resources.Validation;
using Library.Application.TypeDescriptors;
using Library.Domain.Resources;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Repositories;
using Library.WebApi.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddSingleton<ITypeDescriptorRegistry>(sp =>
    new ConfigTypeDescriptorRegistry(builder.Configuration));
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
builder.Services.AddScoped<IResourceValidationService, ResourceValidationService>();
builder.Services.AddScoped<IResourceService, ResourceService>();

var app = builder.Build();

// Global exception handling middleware - must be early in the pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Only use HTTPS redirection in non-development environments
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

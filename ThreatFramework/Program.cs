using Microsoft.Extensions.Configuration;
using ThreatFramework.Core.Abstractions;
using ThreatFramework.IndexBuilder;
using ThreatFramework.Infrastructure.Data;
using ThreatFramework.Infrastructure.Options;
using ThreatFramework.Infrastructure.Cache;
using ThreatFramework.Utils.YamlFileWriter.Generation;
using ThreatFramework.Utils.YamlFileWriter.Generation.Generators;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("ConnectionStrings"));

builder.Services.AddSingleton<IDatabaseReader, SqlDatabaseReader>();
builder.Services.AddSingleton<IIndexBuilder, IndexBuilder>();
builder.Services.AddSingleton<IIndexWriter, YamlIndexWriter>();
builder.Services.AddSingleton<IIndexCache, IndexCache>();

// Generators
builder.Services.AddSingleton<IEntityYamlGenerator, ThreatYamlGenerator>();
builder.Services.AddSingleton<IEntityYamlGenerator, ComponentYamlGenerator>();
builder.Services.AddSingleton<IEntityYamlGenerator, SecurityRequirementYamlGenerator>();
builder.Services.AddSingleton<IEntityYamlGenerator, LibraryYamlGenerator>();
builder.Services.AddSingleton<ICompositeYamlGenerationService, CompositeYamlGenerationService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

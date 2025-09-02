using Microsoft.Extensions.Configuration;
using ThreatFramework.Core.Abstractions;
using ThreatFramework.IndexBuilder;
using ThreatFramework.Infrastructure.Data;
using ThreatFramework.Infrastructure.Options;



var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("ConnectionStrings"));

builder.Services.AddSingleton<IDatabaseReader, SqlDatabaseReader>();
builder.Services.AddSingleton<IIndexBuilder, IndexBuilder>();
builder.Services.AddSingleton<IIndexWriter, YamlIndexWriter>();

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

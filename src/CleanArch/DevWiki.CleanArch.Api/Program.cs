using DevWiki.CleanArch.Api.Endpoints;
using DevWiki.CleanArch.Application.Extensions;
using DevWiki.CleanArch.Infrasctructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfraServices();

var app = builder.Build();

app.MapCobrancaEndpoints();

app.Run();

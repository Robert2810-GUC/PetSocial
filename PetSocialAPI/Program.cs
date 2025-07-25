
using Persistence;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen();

builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler"; // Browse this to see results
    options.PopupRenderPosition = StackExchange.Profiling.RenderPosition.BottomRight;
    options.TrackConnectionOpenClose = true;
    options.ResultsAuthorize = request => true; // allow viewing in dev
    options.ResultsListAuthorize = request => true;
}).AddEntityFramework();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // (Optional: app.MapOpenApi(); if you use minimal APIs)
}
app.UseMiniProfiler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

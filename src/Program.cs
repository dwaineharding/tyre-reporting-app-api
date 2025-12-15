using Microsoft.AspNetCore.Mvc;
using tyre_reporting_app_api.Interfaces;
using tyre_reporting_app_api.Models;
using tyre_reporting_app_api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationInsightsTelemetry();

const string corsPolicyName = "AllowAll";

builder.Services.AddCors(options => //TODO: update to be real CORS.
{
    options.AddPolicy(corsPolicyName,
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Register the service in DI
builder.Services.AddSingleton<IVehicleLookupService, VehicleLookupService>();
builder.Services.AddSingleton<IStorageInteractor, StorageInteractor>();
builder.Services.AddHttpClient<IVehicleLookupService, VehicleLookupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(corsPolicyName);

app.MapGet("/vehicleLookup", async (string regNumber, IVehicleLookupService service) =>
{
    var vehicle = await service.LookupByRegistration(regNumber);
    return vehicle is not null ? Results.Ok(vehicle) : Results.NotFound();
})
.WithName("VehicleLookup")
.WithOpenApi();

app.MapGet("/initJob", async (string regNumber, string user, IStorageInteractor storageInteractor) =>
{
    var date = DateTime.UtcNow;
    var success = await storageInteractor.CreateContainer(regNumber, user, date);
    return success ? Results.Ok() : Results.Accepted();
})
.WithName("InitJob")
.WithOpenApi();

app.MapPost("/saveJob", async ([FromForm] SaveJobDto saveJobDto, IStorageInteractor storageInteractor) =>
{
    var date = DateTime.UtcNow;
    var success = await storageInteractor.InsertJob(saveJobDto.RegNumber, date, saveJobDto);
    return true ? Results.Ok() : Results.Accepted();
})
.WithName("SaveJob")
.WithOpenApi()
.DisableAntiforgery();

app.MapGet("/listJobs", async (IStorageInteractor storageInteractor) =>
{
    var jobs = await storageInteractor.ListJobs();

    return Results.Ok(jobs);
})
.WithName("ListJobs")
.WithOpenApi();

app.MapGet("/jobDetails", async (string regNumber, DateTime date, IStorageInteractor storageInteractor) =>
{
    var jobDetails = await storageInteractor.GetJobDetails(regNumber, date);
    return Results.Ok(jobDetails);
})
.WithName("JobDetails")
.WithOpenApi();

app.Run();
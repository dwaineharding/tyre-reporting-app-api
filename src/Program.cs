using Microsoft.AspNetCore.Mvc;
using Resend;
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
builder.Services.AddTransient<IEmailSender, ResendEmailSender>();

// Register Resend for emails
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    // Read from appsettings or environment variable
    o.ApiToken = Environment.GetEnvironmentVariable("Email__ApiKey") ?? builder.Configuration.GetValue<string>("Email:ApiKey")!;
});
builder.Services.AddTransient<IResend, ResendClient>();

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

app.MapPost("/saveJob", async ([FromForm] SaveJobDto saveJobDto, IStorageInteractor storageInteractor) =>
{
    var date = DateTime.UtcNow;

    var success = await storageInteractor.CreateContainer(saveJobDto.RegNumber, saveJobDto.User, date);

    if(!success)
    {
        return Results.StatusCode(500);
    }

    success = await storageInteractor.InsertJob(saveJobDto.RegNumber, date, saveJobDto);

    return success ? Results.Ok() : Results.StatusCode(500);
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

app.MapPost("/storeInvoice", async ([FromForm]StoreInvoiceDto storeInvoiceDto, IStorageInteractor storageInteractor) =>
{
    var invoiceName = await storageInteractor.StoreInvoice(storeInvoiceDto.File, storeInvoiceDto.RegNumber, storeInvoiceDto.Date);

    return Results.Ok(invoiceName);
})
.WithName("StoreInvoice")
.WithOpenApi()
.DisableAntiforgery();

app.MapPost("/sendInvoice", async ([FromBody]SendEmailDto sendEmailDto, IEmailSender emailSender, IStorageInteractor storageInteractor) =>
{
    var invoicePath = storageInteractor.GetInvoicePath(sendEmailDto.RegNumber, sendEmailDto.Date, sendEmailDto.InvoiceName);

    var content = $"Dear Customer,<br/><br/>" +
    $"Please find attached the invoice for the recent tyre " +
    $"service on your vehicle {sendEmailDto.RegNumber}.<br/><br/>" +
    $"Thank you for choosing our services!<br/><br/>" +
    $"Best regards,<br/>Tyre Reporting App Team";

    var emailAttachment = new EmailAttachment
    {
        Filename = sendEmailDto.InvoiceName,
        ContentType = "application/pdf",
        Content = await File.ReadAllBytesAsync(invoicePath)
    };

    await emailSender.SendEmailAsync($"Invoice - {sendEmailDto.RegNumber}", content, emailAttachment);

    return Results.Ok();
})
.WithName("SendInvoice")
.WithOpenApi();

app.Run();
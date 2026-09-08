using ClinicAPIBusiness.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<ClinicManagementSystemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ClinicAPIBusiness.Services.clsUser>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsAppointment>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsClinicSettings>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsDoctor>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsInvoice>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsPatient>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsPatientVisit>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsInvoiceStatusService>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsLoggingService>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsPayment>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsPeople>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsPrescription>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsSecurity>();
builder.Services.AddScoped<ClinicAPIBusiness.Services.clsUserRole>();





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

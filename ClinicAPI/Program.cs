using ClinicAPIBusiness.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & Handle JSON Cycles
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🛡️ Basic Shield: Configure CORS Policy
var clinicCorsPolicy = "AllowClinicClients";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: clinicCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5267", "https://localhost:7180") // استبدل أو أضف منافذ تطبيقك (WPF/Web/Mobile)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TokenValidationParameters define how incoming JWTs will be validated.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Ensures the token was issued by a trusted issuer.
            ValidateIssuer = true,


            // Ensures the token is intended for this API (audience check).
            ValidateAudience = true,


            // Ensures the token has not expired.
            ValidateLifetime = true,


            // Ensures the token signature is valid and was signed by the API.
            ValidateIssuerSigningKey = true,


            // The expected issuer value (must match the issuer used when creating the JWT).
            ValidIssuer = "ClinicApi",


            // The expected audience value (must match the audience used when creating the JWT).
            ValidAudience = "ClinicApiUsers",


            // The secret key used to validate the JWT signature.
            // This must be the same key used when generating the token.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("THIS_IS_A_VERY_SECRET_KEY_123456_VERY_LONG_KEY_32BYTES!")),

            ClockSkew = TimeSpan.Zero
        };
    });


// Database Context
builder.Services.AddDbContext<ClinicManagementSystemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application Services
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

// Configure HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // 🛡️ Basic Shield: HSTS (Strict Transport Security) for Production
    app.UseHsts();
}

// 🛡️ Basic Shield: HTTPS Redirection
app.UseHttpsRedirection();

// 🛡️ Basic Shield: CORS (Must be placed before UseAuthorization)
app.UseCors(clinicCorsPolicy);

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
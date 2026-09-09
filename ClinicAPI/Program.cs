using ClinicAPIBusiness.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & Handle JSON Cycles
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // ===============================
    // 1) Define the JWT Bearer security scheme
    // ===============================
    //
    // This tells Swagger that our API uses JWT Bearer authentication
    // through the HTTP Authorization header.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // The name of the HTTP header where the token will be sent.
        Name = "Authorization",


        // Indicates this is an HTTP authentication scheme.
        Type = SecuritySchemeType.Http,


        // Specifies the authentication scheme name.
        // Must be exactly "Bearer" for JWT Bearer tokens.
        Scheme = "Bearer",


        // Optional metadata to describe the token format.
        BearerFormat = "JWT",


        // Specifies that the token is sent in the request header.
        In = ParameterLocation.Header,


        // Text shown in Swagger UI to guide the user.
        Description = "Enter: Bearer {your JWT token}"
    });


    // ===============================
    // 2) Require the Bearer scheme for secured endpoints
    // ===============================
    //
    // This tells Swagger that endpoints protected by [Authorize]
    // require the Bearer token defined above.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                // Reference the previously defined "Bearer" security scheme.
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },


            // No scopes are required for JWT Bearer authentication.
            // This array is empty because JWT does not use OAuth scopes here.
            new string[] {}
        }
    });
});
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


var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Secret Key is not configured in appsettings.json!");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];


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


            ValidIssuer = jwtIssuer,     
            ValidAudience = jwtAudience,


            // The secret key used to validate the JWT signature.
            // This must be the same key used when generating the token.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

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

app.MapControllers().RequireAuthorization();

app.Run();
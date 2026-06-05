using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement;
using JobPortal.Services.Implement.Admin;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.Implement.Candidate;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddLogging();  

// REGISTER MYSQL DB CONTEXT
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(
      connectionString
  );
});


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!))
            };
    });


// REGISTER SERVICES
builder.Services.AddScoped<ICreditPlanService,CreditPlanService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecruiterAuthService, RecruiterAuthService>();
builder.Services.AddScoped<IRecruiterRegistrationService,RecruiterRegistrationService>();
builder.Services.AddScoped<IJobPostingService, JobPostingService>(); 
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ISubUserService, SubUserService>();
builder.Services.AddScoped<ISubUserEmailService, SubUserEmailService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICandidateAuthService, CandidateAuthService>();


FirebaseApp.Create(new AppOptions()
{
    Credential =
        GoogleCredential.FromFile(
            "firebase-adminsdk.json")
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "JobPortal API",
        Version = "v1",
        Description = "SkillBridge Job Portal — Admin, Recruiter & Candidate APIs"
    });

    // ✅ This makes enums show as dropdown in Swagger instead of int
    c.UseInlineDefinitionsForEnums();
});

// ✅ This makes enum serialize as string "Shipping" not 1
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


var app = builder.Build();


// Configure the HTTP request pipeline.
    app.UseSwagger();

    app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
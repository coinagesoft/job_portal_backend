using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IPublic;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement;
using JobPortal.Services.Implement.Admin;
using JobPortal.Services.Implement.Candidate;
using JobPortal.Services.Implement.Public;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer
    (options =>{
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
builder.Services.AddScoped<IRecruiterCreditPlanService, RecruiterCreditPlanService>();
builder.Services.AddScoped<ISubUserService, SubUserService>(); 
builder.Services.AddScoped<ICreditConfigurationService, CreditConfigurationService>();
builder.Services.AddScoped<ISubUserEmailService, SubUserEmailService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICandidateAuthService, CandidateAuthService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<IRecruiterInvoiceService, RecruiterInvoiceService>();
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>(); 
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<ICandidateJobService, CandidateJobService>();

builder.Services.AddScoped<ICandidateProfileExtendedService, CandidateProfileExtendedService>();
builder.Services.AddScoped<ICandidateDocumentService, CandidateDocumentService>();
builder.Services.AddScoped<ICreditWalletService, CreditWalletService>();
builder.Services.AddScoped<IApplicationStatusService, ApplicationStatusService>();

builder.Services.AddScoped<IHomepageService, HomepageService>(); 
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddScoped<INotificationService, NotificationService>(); 
builder.Services.AddScoped<IRecruiterSettingsService, RecruiterSettingsService>();
builder.Services.AddScoped<IRecruiterCandidateProfileService,RecruiterCandidateProfileService>();
builder.Services.AddScoped<IRecruiterJobListingService, RecruiterJobListingService>();
builder.Services.AddScoped<IRecruiterApplicantService, RecruiterApplicantService>();
builder.Services.AddScoped<ICandidateNotificationService, CandidateNotificationService>();

builder.Services.AddScoped<IRecruiterCvSearchService,RecruiterCvSearchService>();


builder.Services.AddScoped<IHomepageService, HomepageService>();
builder.Services.AddScoped<ICandidateSettingsService, CandidateSettingsService>();
builder.Services.AddScoped<ICandidateAvailabilityService, CandidateAvailabilityService>();
builder.Services.AddScoped<ICandidateItiInfoService, CandidateItiInfoService>();
builder.Services.AddScoped<ICandidateLogoutService, CandidateLogoutService>();
builder.Services.AddScoped<CandidatePagedJobService>();

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
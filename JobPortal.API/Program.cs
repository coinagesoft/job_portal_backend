using Amazon.S3;
using FirebaseAdmin;
using Google;
using Google.Apis.Auth.OAuth2;
using JobPortal.API.ModelBinders;
using JobPortal.Domain.Common;
using JobPortal.Infrastructure.JWT;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.AI;
using JobPortal.Services.IImplement.AI;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IPublic;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.Implement;
using JobPortal.Services.Implement.Admin;
using JobPortal.Services.Implement.AI;
using JobPortal.Services.Implement.Candidate;
using JobPortal.Services.Implement.Public;
//using JobPortal.Services.IImplement.AI;
using JobPortal.Services.Implement.Recruiter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLogging();


var dataSourceBuilder =
    new NpgsqlDataSourceBuilder(
        builder.Configuration.GetConnectionString("DefaultConnection"));

dataSourceBuilder.EnableDynamicJson();

var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// ── SERVICES ─────────────────────────────────────────────────

builder.Services.AddScoped<ICreditPlanService, CreditPlanService>();
builder.Services.AddScoped<IMembershipPlanService, MembershipPlanService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecruiterAuthService, RecruiterAuthService>();
builder.Services.AddScoped<IRecruiterRegistrationService, RecruiterRegistrationService>();
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
builder.Services.AddScoped<TwilioOtpService>();
builder.Services.AddScoped<Msg91OtpService>();
builder.Services.AddScoped<ITwilioOtpService, OtpProviderRouter>();
builder.Services.AddScoped<ICandidateProfileExtendedService, CandidateProfileExtendedService>();
builder.Services.AddScoped<ICandidateRegistrationService, CandidateRegistrationService>();
builder.Services.AddScoped<ICreditWalletService, CreditWalletService>();
builder.Services.AddScoped<IApplicationStatusService, ApplicationStatusService>();
builder.Services.AddScoped<IHomepageService, HomepageService>();
builder.Services.AddScoped<ISubUserPermissionService, SubUserPermissionService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRecruiterSettingsService, RecruiterSettingsService>();
builder.Services.AddScoped<IRecruiterCandidateProfileService, RecruiterCandidateProfileService>();
builder.Services.AddScoped<IRecruiterJobListingService, RecruiterJobListingService>();
builder.Services.AddScoped<IRecruiterApplicantService, RecruiterApplicantService>();
builder.Services.AddScoped<ICandidateNotificationService, CandidateNotificationService>();
builder.Services.AddScoped<ICandidateDeviceTokenService, CandidateDeviceTokenService>();
builder.Services.AddScoped<IApplicationStatusPushService, ApplicationStatusPushService>();
builder.Services.AddScoped<IResumeWatermarkService, ResumeWatermarkService>();
builder.Services.AddScoped<IRecruiterCvSearchService, RecruiterCvSearchService>();
builder.Services.AddScoped<IHomepageService, HomepageService>();
builder.Services.AddScoped<IRecruiterScreeningQuestionService, RecruiterScreeningQuestionService>();
builder.Services.AddScoped<IRecruiterCvSearchService, RecruiterCvSearchService>();
builder.Services.AddScoped<ICandidateSettingsService, CandidateSettingsService>();
builder.Services.AddScoped<ICandidateAvailabilityService, CandidateAvailabilityService>();
builder.Services.AddScoped<ICandidateLocationService, CandidateLocationService>();
builder.Services.AddScoped<ICandidateItiInfoService, CandidateItiInfoService>();
builder.Services.AddScoped<ICandidateLogoutService, CandidateLogoutService>();
builder.Services.AddScoped<ICandidateLoginService, CandidateLoginService>();
builder.Services.AddScoped<ICompanyDocumentService, CompanyDocumentService>();
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<IAdminCompanyDocumentService, AdminCompanyDocumentService>();
builder.Services.AddScoped<IAdminDocumentTypeService, AdminDocumentTypeService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAdminRevenueService, AdminRevenueService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminSupportTicketService, AdminSupportTicketService>();
builder.Services.AddScoped<ILegalDocumentService, LegalDocumentService>();
builder.Services.AddScoped<ILegalDocumentPublicService, LegalDocumentPublicService>();
builder.Services.AddScoped<IAdminHomepageManagementService, AdminHomepageManagementService>();
builder.Services.AddScoped<IAdminSettingsService, AdminSettingsService>();
builder.Services.AddScoped<CandidatePagedJobService>();
//builder.Services.AddScoped<IRecruiterRegistrationService, RecruiterRegistrationService>();
builder.Services.AddScoped<IRecruiterHomepageService, RecruiterHomepageService>();   // <-- new line
builder.Services.AddHttpClient<IGeminiCompanyDocumentParserService, GeminiCompanyDocumentParserService>();
//builder.Services.AddScoped<IAffindaService, AffindaService>();
builder.Services.AddScoped<ITradeHierarchyImportService, TradeHierarchyImportService>();
builder.Services.AddHttpContextAccessor();
// ── File storage: AWS S3 (private bucket, credentials from EC2 IAM role) ──
builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(
        builder.Configuration["AWS:Region"] ?? "ap-south-1")));
builder.Services.AddScoped<S3FileStorageService>();
builder.Services.AddScoped<IFileStorageService>(
    sp => sp.GetRequiredService<S3FileStorageService>());
builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddScoped<IEmbeddingStorageService, EmbeddingStorageService>();
//builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddScoped<IEmbeddingStorageService, EmbeddingStorageService>();
builder.Services.AddScoped<IJobMatchingService, JobMatchingService>();
builder.Services.AddScoped<ICvGenerationService, CvGenerationService>();
builder.Services.AddScoped<IAiJobDescriptionService, AiJobDescriptionService>();
builder.Services.AddScoped<IRankedCandidateService, RankedCandidateService>();
builder.Services.AddScoped<IAdminCandidateService, AdminCandidateService>();
builder.Services.AddScoped<IAdminRecruiterService, AdminRecruiterService>();

// ── Affinda AI — resume parsing ──────────────────────────────
// Uses typed HttpClient so each instance gets its own HttpClient
builder.Services.AddHttpClient<IAffindaService, AffindaService>();
builder.Services.AddHttpClient("Razorpay", client =>
{
    client.BaseAddress = new Uri("https://api.razorpay.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Document service depends on IAffindaService ──────────────
builder.Services.AddScoped<ICandidateDocumentService, CandidateDocumentService>();
builder.Services.AddScoped<IPublicCompanyService, PublicCompanyService>();
builder.Services.AddHttpClient<IGeminiDocumentParserService, GeminiDocumentParserService>();


builder.Services.AddHostedService<AccountCleanupService>();
builder.Services.AddHostedService<SupportTicketAutoResolveService>();
// ── Firebase ─────────────────────────────────────────────────
// Path defaults to the existing "firebase-adminsdk.json"; override with
// Firebase:CredentialsPath (appsettings / env var Firebase__CredentialsPath).
// The service account MUST belong to the same Firebase project as the Flutter app.
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(
        builder.Configuration["Firebase:CredentialsPath"] ?? "firebase-adminsdk.json")
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JobPortal API",
        Version = "v1",
        Description = "Job Portal — Admin, Recruiter & Candidate APIs"
    });

    c.UseInlineDefinitionsForEnums();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme.\n\n" +
            "Enter: Bearer {token}",

        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(
        0,
        new EnumMemberModelBinderProvider());
});

//builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//        options.JsonSerializerOptions.Converters
//            .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
//    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "https://localhost:3001",
                "http://localhost:3001",

                "https://job-portal-dev-phi.vercel.app",
                "https://job-portal-web-phi.vercel.app",
                "http://16.4.32.157:3001",
                "http://16.4.32.157",
                 "https://job-portal-admin-gray.vercel.app");

    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
// /uploads/* is now served from S3 by UploadsController
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JobPortal.API.Middleware.ActiveSubUserMiddleware>();
app.UseMiddleware<JobPortal.API.Middleware.AuditLogMiddleware>();
app.MapControllers();
app.Run();
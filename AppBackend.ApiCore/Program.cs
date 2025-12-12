using AppBackend.ApiCore.JsonConverters;
using AppBackend.Extensions;
using AppBackend.Services.Services.Group;
using AppBackend.ApiCore.Middlewares;
using AppBackend.Services.Services.Notification;
using AppBackend.ApiCore.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel server to accept large file uploads (500MB)
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 524288000; // 500 MB
});

// Configure form options for multipart/form-data uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 524288000; // 500 MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configs
builder.Services.AddCloudinaryConfig(builder.Configuration);
builder.Services.AddPayOSConfig(builder.Configuration);
builder.Services.AddAutoMapperConfig();
builder.Services.AddDbConfig(builder.Configuration);
builder.Services.AddCorsConfig();
builder.Services.AddSwaggerConfig();
builder.Services.AddDefaultAuth(builder.Configuration);
//Optional login with google
// builder.Services.AddGoogleAuth(builder.Configuration);builder.Services.AddServicesConfig();
builder.Services.AddSessionConfig();
builder.Services.AddHttpContextAccessor();
builder.Services.AddServicesConfig();
builder.Services.AddAutoMapperConfig();
builder.Services.AddRateLimitConfig();
builder.Services.AddScoped<IGroupService, GroupService>();

// Add SignalR for real-time notifications
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB max message size
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Register SignalR notification service
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();

builder.Services.AddControllers()   
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        
        // Add custom DateOnly converters for proper ISO 8601 format handling
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
    });


var app = builder.Build();

// Run seeding once
SeedData.Initialize(app);

// Middleware
// Add global exception handler as the first middleware (after app.Build)
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Main API documentation
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "?? All APIs - v1");
        
        // Admin API groups - organized by functionality
        c.SwaggerEndpoint("/swagger/admin-dashboard/swagger.json", "?? Admin - Dashboard");
        c.SwaggerEndpoint("/swagger/admin-charts/swagger.json", "?? Admin - Charts");
        c.SwaggerEndpoint("/swagger/admin-hall-of-fame/swagger.json", "?? Admin - Hall of Fame");
        c.SwaggerEndpoint("/swagger/admin-reports/swagger.json", "?? Admin - Reports");
        c.SwaggerEndpoint("/swagger/admin-class-management/swagger.json", "?? Admin - Class Management");
        c.SwaggerEndpoint("/swagger/admin-grader-management/swagger.json", "?? Admin - Grader Management");
        
        // Instructor API groups - organized by functionality
        c.SwaggerEndpoint("/swagger/instructor-dashboard/swagger.json", "????? Instructor - Dashboard");
        c.SwaggerEndpoint("/swagger/instructor-classes/swagger.json", "????? Instructor - Classes");
        c.SwaggerEndpoint("/swagger/instructor-groups/swagger.json", "????? Instructor - Groups");
        c.SwaggerEndpoint("/swagger/instructor-projects/swagger.json", "????? Instructor - Projects");
        c.SwaggerEndpoint("/swagger/instructor-grading/swagger.json", "????? Instructor - Grading");
        c.SwaggerEndpoint("/swagger/instructor-submissions/swagger.json", "????? Instructor - Submissions");
        c.SwaggerEndpoint("/swagger/instructor-templates/swagger.json", "????? Instructor - Templates");
        c.SwaggerEndpoint("/swagger/instructor-announcements/swagger.json", "????? Instructor - Announcements");
        
        // UI Settings
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "IoT Showroom API Documentation";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
    // Use permissive CORS in development for easier testing
    app.UseCors("AllowAllOrigins");
}
else
{
    // Use restricted CORS in production for security
    app.UseCors("AllowSpecificOrigins");
}

app.UseRateLimiter();   
app.UseHttpsRedirection();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub endpoint
app.MapHub<AppBackend.ApiCore.Hubs.NotificationHub>("/notificationHub");

app.Run();
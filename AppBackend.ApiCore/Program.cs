using AppBackend.ApiCore.JsonConverters;
using AppBackend.Extensions;
using AppBackend.Services.Services.Group;
using AppBackend.ApiCore.Middlewares;
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
    app.UseSwaggerUI();
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
app.Run();
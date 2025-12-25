using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using AppBackend.ApiCore.Swagger;

namespace AppBackend.Extensions;

public static class SwaggerConfig
{
    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AppBackend.ApiCore - All APIs",
                Version = "v1.2.0",
                Description = "Complete API documentation for AppBackend - All endpoints in one place",
                Contact = new OpenApiContact
                {
                    Name = "IoT Showroom Team",
                    Email = "support@iotshowroom.com"
                }
            });

            // Admin API groups
            c.SwaggerDoc("admin-dashboard", new OpenApiInfo 
            { 
                Title = "Admin - Dashboard", 
                Version = "v1",
                Description = "Admin dashboard overview and system statistics"
            });
            
            c.SwaggerDoc("admin-charts", new OpenApiInfo 
            { 
                Title = "Admin - Dashboard Charts", 
                Version = "v1",
                Description = "Chart data for admin dashboard visualizations"
            });
            
            c.SwaggerDoc("admin-hall-of-fame", new OpenApiInfo 
            { 
                Title = "Admin - Hall of Fame", 
                Version = "v1",
                Description = "Hall of Fame management and leaderboard"
            });
            
            c.SwaggerDoc("admin-reports", new OpenApiInfo 
            { 
                Title = "Admin - Reports & Analytics", 
                Version = "v1",
                Description = "Comprehensive reports and analytics"
            });
            
            c.SwaggerDoc("admin-class-management", new OpenApiInfo 
            { 
                Title = "Admin - Class Management", 
                Version = "v1",
                Description = "Class enrollment and student management"
            });
            
            c.SwaggerDoc("admin-grader-management", new OpenApiInfo 
            { 
                Title = "Admin - Grader Management", 
                Version = "v1",
                Description = "Class grader assignment and grading statistics"
            });

            // Instructor API groups
            c.SwaggerDoc("instructor-dashboard", new OpenApiInfo 
            { 
                Title = "Instructor - Dashboard", 
                Version = "v1",
                Description = "Instructor dashboard overview and quick stats"
            });
            
            c.SwaggerDoc("instructor-classes", new OpenApiInfo 
            { 
                Title = "Instructor - Classes", 
                Version = "v1",
                Description = "Class management, configuration, and statistics"
            });
            
            c.SwaggerDoc("instructor-groups", new OpenApiInfo 
            { 
                Title = "Instructor - Groups", 
                Version = "v1",
                Description = "Group management and member operations"
            });
            
            c.SwaggerDoc("instructor-projects", new OpenApiInfo 
            { 
                Title = "Instructor - Projects", 
                Version = "v1",
                Description = "Project management, proposals, and status updates"
            });
            
            c.SwaggerDoc("instructor-grading", new OpenApiInfo 
            { 
                Title = "Instructor - Grading", 
                Version = "v1",
                Description = "Milestone grading and final project grading"
            });
            
            c.SwaggerDoc("instructor-submissions", new OpenApiInfo 
            { 
                Title = "Instructor - Submissions", 
                Version = "v1",
                Description = "View and manage student submissions"
            });
            
            c.SwaggerDoc("instructor-templates", new OpenApiInfo 
            { 
                Title = "Instructor - Templates", 
                Version = "v1",
                Description = "Project template management"
            });
            
            c.SwaggerDoc("instructor-announcements", new OpenApiInfo 
            { 
                Title = "Instructor - Announcements", 
                Version = "v1",
                Description = "Create and manage announcements"
            });

            // JWT Security Definition
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });

            // Add DateOnly schema filter for proper Swagger documentation
            c.SchemaFilter<DateOnlySchemaFilter>();
            
            // Add file upload operation filter for proper file upload UI
            c.OperationFilter<FileUploadOperationFilter>();

            // Configure document filters to include APIs in multiple documents
            c.DocInclusionPredicate((docName, apiDesc) =>
            {
                // Include all APIs in "v1" document
                if (docName == "v1")
                {
                    return true;
                }

                // For specific group documents, only include APIs with matching GroupName
                if (!string.IsNullOrEmpty(apiDesc.GroupName))
                {
                    return apiDesc.GroupName == docName;
                }

                // APIs without GroupName only appear in "v1"
                return false;
            });

            // Order tags alphabetically but group admin tags together
            c.TagActionsBy(api =>
            {
                if (api.GroupName != null)
                {
                    // Use GroupName as tag for specialized documents
                    return new[] { api.GroupName };
                }

                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                return new[] { controllerName ?? "Default" };
            });

            // Load XML comments for API doc
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
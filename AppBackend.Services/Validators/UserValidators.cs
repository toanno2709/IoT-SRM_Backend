using FluentValidation;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services.Validators
{
    /// <summary>
    /// Validator for CreateUserRequest
    /// </summary>
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required")
                .MaximumLength(255).WithMessage("Full name cannot exceed 255 characters")
                .Matches(@"^[\p{L}\s]+$").WithMessage("Full name can only contain letters and spaces");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

            RuleFor(x => x.Phone)
                .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage("Invalid Vietnamese phone number format")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("Role ID must be greater than 0")
                .LessThanOrEqualTo(4).WithMessage("Role ID must be between 1 and 4");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }
    }

    /// <summary>
    /// Validator for UpdateUserRequest
    /// </summary>
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.FullName)
                .MaximumLength(255).WithMessage("Full name cannot exceed 255 characters")
                .Matches(@"^[\p{L}\s]+$").WithMessage("Full name can only contain letters and spaces")
                .When(x => !string.IsNullOrWhiteSpace(x.FullName));

            RuleFor(x => x.Phone)
                .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage("Invalid Vietnamese phone number format")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.AvatarUrl)
                .Must(BeAValidUrl).WithMessage("Avatar URL must be a valid URL")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x.NewPassword)
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character")
                .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
        }

        private bool BeAValidUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    /// <summary>
    /// Validator for RegisterRequest
    /// </summary>
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required")
                .MaximumLength(255).WithMessage("Full name cannot exceed 255 characters")
                .Matches(@"^[\p{L}\s]+$").WithMessage("Full name can only contain letters and spaces");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

            RuleFor(x => x.Phone)
                .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage("Invalid Vietnamese phone number format")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit");
        }
    }
}

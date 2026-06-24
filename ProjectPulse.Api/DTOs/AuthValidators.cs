using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace ProjectPulse.Api.DTOs;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(email => new EmailAddressAttribute().IsValid(email.Trim()))
            .WithMessage("'Email' is not a valid email address.")
            .MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8); // súbelo si quieres
        RuleFor(x => x.DisplayName)
            .NotEmpty().MaximumLength(100);
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(email => new EmailAddressAttribute().IsValid(email.Trim()))
            .WithMessage("'Email' is not a valid email address.")
            .MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty();
    }
}

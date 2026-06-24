using FluentValidation;

namespace ProjectPulse.Api.DTOs;

// Validador para el body de POST /projects
public class ProjectCreateDtoValidator : AbstractValidator<ProjectCreateDto>
{
    public ProjectCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

// Validador para el body de PUT /projects/{id}
public class ProjectUpdateDtoValidator : AbstractValidator<ProjectUpdateDto>
{
    public ProjectUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

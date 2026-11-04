namespace Elsasa.Sis.Finance.Features.Payers.CreatePayer;

using FluentValidation;

/// <summary>
/// Validator for payer creation asserting name presence, email syntax, and phone formatting.
/// </summary>
public sealed class CreatePayerValidator : AbstractValidator<CreatePayerRequest>
{
    public CreatePayerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Payer name is required.")
            .MaximumLength(200).WithMessage("Payer name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email address is not in a valid format.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^[+0-9\s\-()]{7,30}$")
            .WithMessage("Phone number must contain at least 7 valid phone digits/symbols.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Type)
            .MaximumLength(64).WithMessage("Payer type must not exceed 64 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Type));

        RuleFor(x => x.IdentificationNumber)
            .MaximumLength(64).WithMessage("Identification number must not exceed 64 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber));

        RuleFor(x => x.BankAccountName)
            .MaximumLength(200).WithMessage("Bank account name must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BankAccountName));

        RuleFor(x => x.BankAccountNumber)
            .MaximumLength(100).WithMessage("Bank account number must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BankAccountNumber));

        RuleFor(x => x.BankName)
            .MaximumLength(100).WithMessage("Bank name must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BankName));
    }
}

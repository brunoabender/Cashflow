using FluentValidation;
using NUlid;

namespace Cashflow.Operations.Api.Features.CreateTransaction;

public class CreateTransactionValidator
{
    public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {

            RuleFor(x => x.IdempotencyKey)
                .NotNull().WithMessage("IdempotencyKey é obrigatório.")
                .Must(id => id != Ulid.MinValue).WithMessage("IdempotencyKey deve ser um ULID válido e não pode ser zerado.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("O valor da transação deve ser maior que zero.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Tipo de transação inválido.");
        }
    }
}

using Cashflow.SharedKernel.Enums;
using FluentValidation;

namespace Cashflow.Operations.Api.Features.CreateTransaction;

public class CreateTransactionValidator
{
    public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {
            RuleFor(x => x.IdempotencyKey)
                .NotNull().WithMessage("IdempotencyKey é obrigatório.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("O valor da transação deve ser maior que zero.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Tipo de transação inválido.");
        }
    }
}

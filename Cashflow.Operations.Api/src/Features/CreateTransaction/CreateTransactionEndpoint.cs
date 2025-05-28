using Cashflow.SharedKernel.Event;
using Cashflow.SharedKernel.Idempotency;
using Cashflow.SharedKernel.Messaging;
using FluentResults;
using FluentValidation;

namespace Cashflow.Operations.Api.Features.CreateTransaction
{
    public class CreateTransactionEndpoint(ILogger<CreateTransactionEndpoint> logger, IValidator<CreateTransactionRequest> validator)
    {
        private readonly ILogger<CreateTransactionEndpoint> _logger = logger;
        private readonly IValidator<CreateTransactionRequest> _validator = validator;

        public async Task<Result> Handle(
         CreateTransactionRequest request,
         IMessagePublisher publisher,
         IIdempotencyStore idempotencyStore)
        {
            _logger.LogInformation("Recebida requisição para criar transação: {IdempotencyKey}", request.IdempotencyKey);

            var validationResult = await _validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validação falhou: {Motivo}", validationResult.Errors);
                var result = Result.Fail("Erro de validação.");

                foreach (var error in validationResult.Errors)
                    result.WithError(error.ErrorMessage);
                return result;
            }

            if (await idempotencyStore.ExistsAsync(request.IdempotencyKey))
            {
                _logger.LogWarning("Transação duplicada detectada para ULID {Ulid}", request.IdempotencyKey);
                return Result.Fail("Requisição já processada anteriormente.");
            }

            var @event = new TransactionCreatedEvent(Guid.NewGuid(), request.Amount, request.Type, DateTime.UtcNow, request.IdempotencyKey);

            try
            {
                var result = await idempotencyStore.TryCreateAsync(@event.IdPotencyKey);

                if (!result)
                    return Result.Fail("Não foi possível colocar uma chave de idpotencia na transação.");

                await publisher.PublishAsync(@event);
                

                _logger.LogInformation("Transação registrada com sucesso: {TransactionId}", @event.Id);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar evento para transação {TransactionId}", @event.Id);
                return Result.Fail("Erro interno ao processar a transação.");
            }
        }

    }
}
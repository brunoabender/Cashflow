using Cashflow.Operations.Api.Features.CreateTransaction;
using Cashflow.SharedKernel.Idempotency;
using Cashflow.SharedKernel.Messaging;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cashflow.Operations.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(IMessagePublisher publisher, IIdempotencyStore idempotencyStore, ILogger<CreateTransactionEndpoint> logger, IValidator<CreateTransactionRequest> validator) : ControllerBase
{
    private readonly IMessagePublisher _publisher = publisher;
    private readonly IIdempotencyStore _idempotencyStore = idempotencyStore;
    private readonly ILogger<CreateTransactionEndpoint> _logger = logger;
    private readonly IValidator<CreateTransactionRequest> _validator = validator;

    [HttpPost]
    [Authorize(Policy = "Transacoes")]
    public async Task<IActionResult> Create(CreateTransactionRequest request)
    {
        var endpoint = new CreateTransactionEndpoint(_logger, _validator);
        var result = await endpoint.Handle(request, _publisher, _idempotencyStore);

        if (result.IsSuccess)
            return Accepted(new { message = "Transação criada com sucesso." });

        return BadRequest(result.Errors.Select(e => e.Message));
    }
}

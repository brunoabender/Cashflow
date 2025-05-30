using Cashflow.SharedKernel.Token;
using Microsoft.AspNetCore.Mvc;

namespace Cashflow.Reporting.Api.Controllers
{
    [ApiController]
    public class TokenController : Controller
    {
        //Não real
        //Sem autenticação para geração, só exibição
        [HttpGet("token/generate")]
        public IActionResult GenerateToken()
        {
            var token = new TokenGenerator();
            return Ok(token.Generate());
        }
    }
}

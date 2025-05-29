using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Cashflow.Operations.Api.Controllers
{
    [ApiController]
    [Route("api/")]
    public class TokenController : Controller
    {
        //Não real
        //Sem autenticação para geração, só exibição
        [HttpGet("[controller]/Generate")]
        public IActionResult GenerateToken()
        {
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("ChaveSecretaMasNesseCasoNaoÉPorqueEstaNoCodigo"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("scope", "transacoes:write")
        };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new { token = jwt });
        }
    }
}

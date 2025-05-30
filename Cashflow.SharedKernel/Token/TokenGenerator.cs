using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Cashflow.SharedKernel.Token
{
    public class TokenGenerator
    {
        public object Generate()
        {
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("ChaveSecretaMasNesseCasoNaoÉPorqueEstaNoCodigo"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim("scope", "transacoes:write"),
                new Claim("scope", "transacoes:read")
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return new { token = jwt };
        }
    }
}

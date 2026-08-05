using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PDOE.Infrastructure.Entities;

namespace PDOE.Gateway.Common;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generer(Utilisateur utilisateur);
}

public class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public (string Token, DateTime ExpiresAt) Generer(Utilisateur utilisateur)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var expirationMinutes = configuration.GetValue("Jwt:ExpirationMinutes", 480);
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // NameIdentifier = LoginAD (clé pivot utilisée partout via CurrentUser.Login), Role = Profil (pilote [Authorize(Roles=...)]).
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, utilisateur.LoginAD),
            new Claim(ClaimTypes.Role, utilisateur.Profil),
            new Claim(ClaimTypes.Name, $"{utilisateur.Prenom} {utilisateur.Nom}"),
            new Claim(ClaimTypes.Email, utilisateur.Email),
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

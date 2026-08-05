using System;
using Microsoft.Extensions.Logging;
using PDOE.Infrastructure.Ldap;


namespace PDOE.Gateway.Ldap;





public class BypassLdapAuthenticator : ILdapAuthenticator
{
	private readonly ILogger<BypassLdapAuthenticator> _logger;

	public BypassLdapAuthenticator(ILogger<BypassLdapAuthenticator> logger)
	{
		_logger = logger;
	}

	public Task<LdapBindResult> AuthentifierAsync(string login, string password, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Bypassing LDAP authentication for user {Login}. This should only be used in development or testing environments.", login);
        return Task.FromResult(new LdapBindResult(true, null));
    }
}

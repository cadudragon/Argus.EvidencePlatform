using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace Argus.EvidencePlatform.Api.Authentication;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddPlatformAuthentication(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(PlatformAuthenticationOptions.SectionName)
            .Get<PlatformAuthenticationOptions>() ?? new();

        var useMock = builder.Environment.IsEnvironment("Testing")
            || string.Equals(options.Mode, AuthenticationModes.Mock, StringComparison.OrdinalIgnoreCase);

        if (useMock && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException("Mock authentication is only allowed in Development or Testing.");
        }

        if (useMock)
        {
            builder.Services
                .AddOptions<MockAuthenticationOptions>()
                .Bind(builder.Configuration.GetSection($"{PlatformAuthenticationOptions.SectionName}:Mock"));

            builder.Services
                .AddAuthentication(authentication =>
                {
                    authentication.DefaultAuthenticateScheme = MockAuthenticationHandler.SchemeName;
                    authentication.DefaultChallengeScheme = MockAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(
                    MockAuthenticationHandler.SchemeName,
                    _ => { });

            return builder;
        }

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                if (!string.IsNullOrWhiteSpace(options.Authority))
                {
                    jwtOptions.Authority = options.Authority;
                }

                if (!string.IsNullOrWhiteSpace(options.Audience))
                {
                    jwtOptions.Audience = options.Audience;
                }

                jwtOptions.MapInboundClaims = false;
                jwtOptions.RequireHttpsMetadata = !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing");
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = !string.IsNullOrWhiteSpace(options.Audience),
                    ValidateIssuer = !string.IsNullOrWhiteSpace(options.Authority),
                    ValidateIssuerSigningKey = true
                };
            });

        return builder;
    }
}

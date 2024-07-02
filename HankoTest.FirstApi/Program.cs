using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Jose;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HankoTest.FirstApi;

public record JsonWebKeySet(
    JsonWebKey[] keys
);

public record JsonWebKey(
    string alg,
    string e,
    string kid,
    string kty,
    string n,
    string use
);

internal static class Program
{
    private static async Task<ValidatedTokenViewModel> ValidateJwt(string jwt)
    {
        List<SecurityKey> keys = await GetSigningKeys();

        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKeys = keys,
            RequireSignedTokens = true
        };

        JwtSecurityTokenHandler handler = new();
        handler.InboundClaimTypeMap.Clear();
        ClaimsPrincipal? user = handler.ValidateToken(jwt, parameters, out SecurityToken securityToken);

        JwtSecurityToken payload = handler.ReadJwtToken(jwt);

        return new ValidatedTokenViewModel
        {
            User = user,
            Token = securityToken,
            JwtPayload = HankoPayload.FromJwtPayload(payload.Payload)
        };
    }

    private static async Task<List<SecurityKey>> GetSigningKeys()
    {
        HttpClient client = new();
        JsonWebKeySet? keySet = await client.GetFromJsonAsync<JsonWebKeySet>("https://ac113bd9-81fe-494e-a715-0f58e6bac2ac.hanko.io/.well-known/jwks.json");

        JsonWebKey[] disco = keySet.keys;
        List<SecurityKey> keys = [];

        foreach (JsonWebKey webKey in disco)
        {
            byte[]? e = Base64Url.Decode(webKey.e);
            byte[]? n = Base64Url.Decode(webKey.n);

            RsaSecurityKey key = new(new RSAParameters { Exponent = e, Modulus = n })
            {
                KeyId = webKey.kid
            };

            keys.Add(key);
        }

        return keys;
    }

    private static async Task<string> ValidateJwtJose(string token)
    {
        HttpClient client = new();

        string keys = await client.GetStringAsync("https://ac113bd9-81fe-494e-a715-0f58e6bac2ac.hanko.io/.well-known/jwks.json");

        JwkSet jwks = JwkSet.FromJson(keys, JWT.DefaultSettings.JsonMapper);

        string? jwt = JWT.Decode(token, jwks, JwsAlgorithm.RS256);

        return jwt;
    }

    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        List<SecurityKey> keys = await GetSigningKeys();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = "https://ac113bd9-81fe-494e-a715-0f58e6bac2ac.hanko.io/.well-known/jwks.json";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKeys = keys,
                    RequireSignedTokens = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine(context.Exception);
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddTransient<AuthorizationHandler>();

        builder.Services.AddHttpClient("auth")
            .AddHttpMessageHandler<AuthorizationHandler>();

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHttpsRedirection();

        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        app.MapGet("/weatherforecast", (HttpContext _) =>
            {
                WeatherForecast[] forecast = Enumerable.Range(1, 5)
                    .Select(index =>
                        new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]))
                    .ToArray();

                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi()
            .RequireAuthorization();

        app.MapGet("/token", ValidateJwt)
            .WithName("ValidateToken")
            .WithOpenApi();

        app.MapGet("/token-jose", ValidateJwtJose)
            .WithName("ValidateTokenJose")
            .WithOpenApi();

        app.MapGet("/weatherforecast-two", ([FromServices] IHttpClientFactory httpClientFactory) =>
            {
                HttpClient client = httpClientFactory.CreateClient("auth");
                return client.GetStringAsync("https://localhost:7001/weatherforecast");
            })
            .WithName("GetWeatherForecastFromOtherApi")
            .WithOpenApi();

        await app.RunAsync();
    }

    private record ValidatedTokenViewModel
    {
        public ClaimsPrincipal? User { get; init; }
        public required SecurityToken Token { get; init; }
        public HankoPayload? JwtPayload { get; init; }
        public string? Payload { get; init; }
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
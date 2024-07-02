using System.IdentityModel.Tokens.Jwt;

namespace HankoTest.FirstApi;

public class HankoAudience
{
    /// <summary>
    ///     The audience for which the JWT was created. It specifies the intended recipient or system that should accept this
    ///     JWT.
    ///     When using Hanko Cloud, the aud will be your app URL.
    /// </summary>
    public required IEnumerable<string> AudienceValues { get; init; }
}

public class HankoUserEmail
{
    /// <summary>
    ///     The current primary email address of the user.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    ///     A boolean field indicating whether the email address is the primary email.
    ///     Currently, this field is redundant because only the primary email is included in the JWT.
    /// </summary>
    public required bool IsPrimary { get; init; }

    /// <summary>
    ///     A boolean field indicating whether the email address has been verified.
    /// </summary>
    public required bool IsVerified { get; init; }
}

public class HankoPayload
{
    /// <summary>
    ///     The audience for which the JWT was created.
    /// </summary>
    public required HankoAudience Audience { get; init; }

    /// <summary>
    ///     An object containing information about the user's email address.
    /// </summary>
    public required HankoUserEmail Email { get; init; }

    /// <summary>
    ///     The timestamp indicating when the JWT will expire.
    /// </summary>
    public required DateTime ExpirationTime { get; init; }

    /// <summary>
    ///     The timestamp indicating when the JWT was created.
    /// </summary>
    public required DateTime IssuedAt { get; init; }

    /// <summary>
    ///     The user ID.
    /// </summary>
    public required string Subject { get; init; }

    public static HankoPayload FromJwtPayload(JwtPayload jwtPayload) => new()
    {
        Audience = new HankoAudience
        {
            AudienceValues = (string[])jwtPayload["aud"]
        },
        Email = new HankoUserEmail
        {
            Address = (string)jwtPayload["email.address"],
            IsPrimary = (bool)jwtPayload["email.is_primary"],
            IsVerified = (bool)jwtPayload["email.is_verified"]
        },
        ExpirationTime = DateTimeOffset.FromUnixTimeSeconds((long)jwtPayload["exp"]).DateTime,
        IssuedAt = DateTimeOffset.FromUnixTimeSeconds((long)jwtPayload["iat"]).DateTime,
        Subject = (string)jwtPayload["sub"]
    };

    public Dictionary<string, object> ToDictionary() => new()
    {
        { "aud", Audience.AudienceValues },
        { "email.address", Email.Address },
        { "email.is_primary", Email.IsPrimary },
        { "email.is_verified", Email.IsVerified },
        { "exp", ((DateTimeOffset)ExpirationTime).ToUnixTimeSeconds() },
        { "iat", ((DateTimeOffset)IssuedAt).ToUnixTimeSeconds() },
        { "sub", Subject }
    };
}
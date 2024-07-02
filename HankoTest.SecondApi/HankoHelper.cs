using Microsoft.IdentityModel.Tokens;

namespace HankoTest.SecondApi;

public class HankoHelper
{
    public static async Task<IList<SecurityKey>> GetSigningKeys()
    {
        HttpClient client = new();
        string a = await client.GetStringAsync("https://ac113bd9-81fe-494e-a715-0f58e6bac2ac.hanko.io/.well-known/jwks.json");

        JsonWebKeySet keySet = JsonWebKeySet.Create(a);

        return keySet.GetSigningKeys();
    }
}
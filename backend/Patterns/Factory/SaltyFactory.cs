using HomemadeCookie.Api.Patterns.Factory.Products;

namespace HomemadeCookie.Api.Patterns.Factory;

public class SaltyFactory : ICookieFactory
{
    public Cookie CreateCookie(string type) => type switch
    {
        "PeanutButter" => new PeanutButterCookie(),
        _ => throw new ArgumentException($"Unknown cookie type '{type}' for Salty factory.")
    };
}

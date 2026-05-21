using HomemadeCookie.Api.Patterns.Factory.Products;

namespace HomemadeCookie.Api.Patterns.Factory;

public class ChocolateFactory : ICookieFactory
{
    public Cookie CreateCookie(string type) => type switch
    {
        "Chocolate" => new ChocolateChipsCookie(),
        "DarkChocolate" => new DarkChocolateCookie(),
        _ => throw new ArgumentException($"Unknown cookie type '{type}' for Chocolate factory.")
    };
}

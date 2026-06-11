using HomemadeCookie.Api.Patterns.Factory.Products;

namespace HomemadeCookie.Api.Patterns.Factory;

public class FruitFactory : ICookieFactory
{
    public Cookie CreateCookie(string type) => type switch
    {
        "Strawberry" => new StrawberryCookie(),
        "Orange" => new OrangeCookie(),
        _ => throw new ArgumentException($"Unknown cookie type '{type}' for Fruit factory.")
    };
}

using HomemadeCookie.Api.Patterns.Factory.Products;

namespace HomemadeCookie.Api.Patterns.Factory;

public class OatmealFactory : ICookieFactory
{
    public Cookie CreateCookie(string type) => type switch
    {
        "Oatmeal" => new OatmealCookie(),
        _ => throw new ArgumentException($"Unknown cookie type '{type}' for Oatmeal factory.")
    };
}

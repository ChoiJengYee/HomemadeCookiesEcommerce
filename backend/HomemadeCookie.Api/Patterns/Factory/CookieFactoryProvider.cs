namespace HomemadeCookie.Api.Patterns.Factory;

public static class CookieFactoryProvider
{
    public static ICookieFactory GetFactory(string factoryKey) => factoryKey switch
    {
        "Chocolate" => new ChocolateFactory(),
        "Fruit" => new FruitFactory(),
        "Oatmeal" => new OatmealFactory(),
        "Salty" => new SaltyFactory(),
        _ => throw new ArgumentException($"Unknown factory '{factoryKey}'.")
    };
}

namespace HomemadeCookie.Api.Patterns.Factory;

public interface ICookieFactory
{
    Cookie CreateCookie(string type);
}

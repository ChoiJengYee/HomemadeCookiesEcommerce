namespace HomemadeCookie.Api.Models;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    
    //store the incoming new password
    public string Password { get; set; } = string.Empty;
}
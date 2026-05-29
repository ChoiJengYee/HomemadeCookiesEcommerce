namespace HomemadeCookie.Api.Models;

public class UserEntity
{
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = "Customer";
}

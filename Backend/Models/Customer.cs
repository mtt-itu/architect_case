using System.Text.Json.Serialization;

namespace Backend.Models;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public List<Subscription> Subscriptions { get; set; } = [];
}

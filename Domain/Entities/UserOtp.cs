namespace Domain.Entities;


public class UserOtp
{
    public long Id { get; set; }
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
    public string Otp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}

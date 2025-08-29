using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Infrastructure.Services;

public class TwilioSmsSender : ISmsSender
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhoneNumber;

    public TwilioSmsSender(IConfiguration configuration)
    {
        _accountSid = configuration["Twilio:AccountSid"];
        _authToken = configuration["Twilio:AuthToken"];
        _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"];
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        TwilioClient.Init(_accountSid, _authToken);
        await MessageResource.CreateAsync(
            to: phoneNumber,
            from: _fromPhoneNumber,
            body: message);
    }
}

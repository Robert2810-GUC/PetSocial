using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface ISmsSender
{
    Task SendSmsAsync(string phoneNumber, string message);
}

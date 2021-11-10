using System.Threading.Tasks;

namespace TGSentry.Logic.Contract
{
    public interface INotificator
    {
        Task SendMessage(string message);
    }
}
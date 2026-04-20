
namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.AI
{
    public interface IIntentService
    {
        (string Intent, float Confidence) Predict(string text);
    }
}

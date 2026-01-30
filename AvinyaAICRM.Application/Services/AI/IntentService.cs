using AvinyaAICRM.Application.DTOs.AI;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.AI;
using Microsoft.ML;

namespace AvinyaAICRM.Application.Services.AI
{
    public class IntentService : IIntentService
    {
        private readonly PredictionEngine<IntentData, IntentPrediction> _engine;

        public IntentService(string modelPath)
        {
            var ml = new MLContext();
            var model = ml.Model.Load(modelPath, out _);

            _engine = ml.Model.CreatePredictionEngine<IntentData, IntentPrediction>(model);
        }

        public (string Intent, float Confidence) Predict(string text)
        {
            var result = _engine.Predict(new IntentData { Text = text });

            float max = result.Score.Max();
            float confidence = max / result.Score.Sum();

            return (result.Intent, confidence);
        }
    }
}

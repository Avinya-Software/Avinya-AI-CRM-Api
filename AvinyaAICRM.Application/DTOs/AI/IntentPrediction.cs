using Microsoft.ML.Data;

namespace AvinyaAICRM.Application.DTOs.AI
{

    public class IntentPrediction
    {
        [ColumnName("PredictedLabel")]
        public string Intent { get; set; }

        public float[] Score { get; set; }
    }

}

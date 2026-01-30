using Microsoft.ML.Data;

namespace AvinyaAICRM.Application.DTOs.AI
{

    public class IntentData
    {
        [LoadColumn(0)]
        public string Text { get; set; }

        [LoadColumn(1)]
        public string Label { get; set; }
    }


}

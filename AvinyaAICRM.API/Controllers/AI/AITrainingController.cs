using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.AI
{
    [ApiController]
    [Route("api/ai")]
    public class AITrainingController : ControllerBase
    {
        [HttpPost("train-intent")]
        public IActionResult TrainIntentModel()
        {
            var solutionRoot =
                Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;

            var dataPath = Path.Combine(
                solutionRoot,
                "AvinyaAICRM.Infrastructure",
                "AI",
                "Training",
                "intent-data.csv"
            );

            var modelDir = Path.Combine(
                solutionRoot,
                "AvinyaAICRM.Infrastructure",
                "AI",
                "Models"
            );

            if (!Directory.Exists(modelDir))
                Directory.CreateDirectory(modelDir);

            var modelPath = Path.Combine(modelDir, "intent-model.zip");

            IntentTrainer.Train(dataPath, modelPath);

            return Ok("Intent model trained successfully ✅");
        }
    }
}

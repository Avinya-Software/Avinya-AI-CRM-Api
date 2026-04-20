using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace AvinyaAICRM.API.Controllers.AI
{
    [ApiController]
    [Route("api/ai")]
    public class AITrainingController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public AITrainingController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("train-intent")]
        public IActionResult TrainIntentModel()
        {
            // wwwroot path
            var webRoot = _env.WebRootPath;

            // Training data
            var dataPath = Path.Combine(
                webRoot,
                "AI",
                "training",
                "intent-data.csv"
            );

            // Model directory
            var modelDir = Path.Combine(
                webRoot,
                "AI",
                "Model"
            );

            if (!Directory.Exists(modelDir))
                Directory.CreateDirectory(modelDir);

            var modelPath = Path.Combine(modelDir, "intent-model.zip");

            // Train model
            IntentTrainer.Train(dataPath, modelPath);

            return Ok("Intent model trained successfully ✅");
        }
    }
}
using Microsoft.ML;
using AvinyaAICRM.Application.DTOs.AI;

public class IntentTrainer
{
    public static void Train(string dataPath, string modelPath)
    {
        var ml = new MLContext(seed: 1);

        var data = ml.Data.LoadFromTextFile<IntentData>(
            dataPath,
            hasHeader: true,
            separatorChar: ',');

        var pipeline =
            // 🔥 STEP 1: Label (string) → Key
            ml.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label",
                inputColumnName: "Label")

            // 🔥 STEP 2: Text → Features
            .Append(ml.Transforms.Text.FeaturizeText(
                outputColumnName: "Features",
                inputColumnName: nameof(IntentData.Text)))

            // 🔥 STEP 3: Train classifier
            .Append(ml.MulticlassClassification.Trainers
                .SdcaMaximumEntropy())

            // 🔥 STEP 4: Key → string (prediction output)
            .Append(ml.Transforms.Conversion
                .MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(data);

        ml.Model.Save(model, data.Schema, modelPath);
    }

}

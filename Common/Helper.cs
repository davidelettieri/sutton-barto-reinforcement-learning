namespace Common;

public static class Helper
{
    public static SelectArmStrategy GetEpsilonStrategy(double epsilon)
    {
        return (arms, step) =>
       {
           if (Random.Shared.NextDouble() < epsilon)
           {
               return Random.Shared.Next(arms.Length);
           }

           return arms.MaxBy(p => p.EstimatedReward)!.Index;
       };
    }

    public static SelectArmStrategy GetUcbStrategy(double c)
    {
        return (arms, step) =>
        {
            if (arms.Length == 0)
            {
                throw new ArgumentException("Arms array cannot be empty", nameof(arms));
            }

            for (int i = 0; i < arms.Length; i++)
            {
                if (arms[i].SelectedCount == 0)
                {
                    return i;
                }
            }
            return arms
                .Select((arm, index) => (value: arm.EstimatedReward + c * Math.Sqrt(Math.Log(step + 1) / arm.SelectedCount), index))
                .MaxBy(t => t.value)
                .index;
        };
    }

    public static Arm[] InitializeArms(int numberOfArms)
    {
        var arms = new Arm[numberOfArms];
        for (int i = 0; i < numberOfArms; i++)
        {
            arms[i] = new Arm(i, 0, 0);
        }
        return arms;
    }

    public static readonly UpdateEstimatedReward SampleAverage = (currentEstimatedReward, reward, armSelectedCount) =>
        currentEstimatedReward + (reward - currentEstimatedReward) / armSelectedCount;
}

public delegate int SelectArmStrategy(Arm[] arms, int currentStep);

public record Arm(int Index, double EstimatedReward, int SelectedCount);

public record ExperimentResult(double[] AverageRewardsPerStep, double[] BestArmSelectionRate);

public delegate double UpdateEstimatedReward(double currentEstimatedReward, double reward, int armSelectedCount);

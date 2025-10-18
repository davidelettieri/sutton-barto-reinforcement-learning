using MathNet.Numerics.Distributions;

namespace Common;

public static class Helper
{
    public static SelectArmStrategy GetEpsilonStrategy(double epsilon)
    {
        return (arms, step) =>
       {
           if (arms.Length == 0)
           {
               throw new ArgumentException("Arms array cannot be empty", nameof(arms));
           }

           if (Random.Shared.NextDouble() < epsilon)
           {
               return Random.Shared.Next(arms.Length);
           }

           var maxReward = arms.Max(p => p.EstimatedReward);
           var bestArms = arms.Where(p => p.EstimatedReward == maxReward).ToArray();
           return bestArms[Random.Shared.Next(bestArms.Length)].Index;
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

            var ucbValues = arms
                .Select((arm, index) => (value: arm.EstimatedReward + c * Math.Sqrt(Math.Log(step + 1) / arm.SelectedCount), index))
                .ToArray();
            var maxValue = ucbValues.Max(t => t.value);
            var bestArms = ucbValues.Where(t => t.value == maxValue).ToArray();
            return bestArms[Random.Shared.Next(bestArms.Length)].index;
        };
    }

    public static Arm[] InitializeArms(int numberOfArms)
    {
        return Enumerable.Range(0, numberOfArms)
            .Select(i => new Arm(i, 0, 0))
            .ToArray();
    }

    public static readonly UpdateEstimatedReward SampleAverage = (currentEstimatedReward, reward, armSelectedCount) =>
        currentEstimatedReward + (reward - currentEstimatedReward) / armSelectedCount;

    public static ExperimentResult RunStationaryExperiment(int steps, int rounds, SelectArmStrategy strategy, UpdateEstimatedReward updateEstimatedReward)
    {
        // Setup: this variable will be used across all rounds.
        // Each arm will be identified by a number between 0 and 9.
        var numberOfArms = 10;
        var normal = new Normal(0, 1, Random.Shared);
        var sumOfRewards = new double[steps];
        var bestArmSelected = new int[steps];

        for (int round = 0; round < rounds; round++)
        {
            Console.WriteLine($"Round {round + 1} of {rounds}");
            // Action values for each arm are selected using a normal distribution with mean 0 and standard deviation 1.
            var qStarA = normal.Samples().Take(numberOfArms).ToArray();

            // Each arm reward is based on a normal distribution with mean equal to the action value for that arm and standard deviation 1.
            var rewardDistributions = qStarA
                .Select(x => new Normal(x, 1, Random.Shared))
                .ToArray();

            var arms = InitializeArms(numberOfArms);
            var bestArm = qStarA.Select((v, i) => (v, i)).MaxBy(el => el.v).i;

            for (int step = 0; step < steps; step++)
            {
                var selectedArm = strategy(arms, step);
                var reward = GetReward(rewardDistributions, selectedArm);
                var newSelectedCount = arms[selectedArm].SelectedCount + 1;
                arms[selectedArm] = arms[selectedArm] with
                {
                    SelectedCount = newSelectedCount,
                    EstimatedReward = updateEstimatedReward(
                        arms[selectedArm].EstimatedReward,
                        reward,
                        newSelectedCount)
                };

                if (bestArm == selectedArm)
                {
                    bestArmSelected[step]++;
                }

                sumOfRewards[step] += reward;
            }
        }

        double[] averageRewards = sumOfRewards.Select(i => i / rounds).ToArray();
        double[] bestArmSelectionRate =
            bestArmSelected.Select(Convert.ToDouble).Select(i => i / rounds).ToArray();

        return new(averageRewards, bestArmSelectionRate);
    }

    static double GetReward(Normal[] rewardDistributions, int arm) =>
        rewardDistributions[arm].Sample();
}

public delegate int SelectArmStrategy(Arm[] arms, int currentStep);

public record Arm(int Index, double EstimatedReward, int SelectedCount);

public record ExperimentResult(double[] AverageRewardsPerStep, double[] BestArmSelectionRate);

public delegate double UpdateEstimatedReward(double currentEstimatedReward, double reward, int armSelectedCount);

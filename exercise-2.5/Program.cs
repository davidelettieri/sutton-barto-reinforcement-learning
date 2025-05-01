using MathNet.Numerics.Distributions;
using ScottPlot;

// I want to run "experiments" with 10 arms to evaluate the performance of epsilon-greedy methods.
// An experiment consists of 2000 rounds, each round consists of 10000 steps.
const int rounds = 2000;
const int steps = 10000;

// Please check the ten-armed-testbed project for the simplest example of the k-armed bandit problem.

UpdateEstimatedReward sampleAverage = (currentEstimatedReward, reward, armSelectedCount) =>
    currentEstimatedReward + (reward - currentEstimatedReward) / armSelectedCount;

UpdateEstimatedReward constantStepSize(double alpha)
    => (currentEstimatedReward, reward, _) =>
        currentEstimatedReward + alpha * (reward - currentEstimatedReward);

Func<Dictionary<int, double>, int> GetEpsilonStrategy(double epsilon)
    => currentEstimatedRewards =>
    {
        if (Random.Shared.NextDouble() < epsilon)
        {
            return Convert.ToInt32(Random.Shared.NextInt64(currentEstimatedRewards.Count));
        }

        return currentEstimatedRewards.MaxBy(p => p.Value).Key;
    };

// Initialize the estimated reward dictionary.
Dictionary<int, double> InitializeEstimatedRewards(int numberOfArms)
{
    var dictionary = new Dictionary<int, double>();
    for (int i = 0; i < numberOfArms; i++)
    {
        dictionary[i] = 0;
    }

    return dictionary;
}


ExperimentResult RunExperiment(Func<Dictionary<int, double>, int> strategy, UpdateEstimatedReward updateEstimatedReward)
{
    // Setup: this variable will be used across all rounds.
    // Each arm will be identified by a number between 0 and 9.
    var numberOfArms = 10;
    // This variable will be used to update all the reward distributions at all steps
    var normal = new Normal(0, 0.01, Random.Shared);
    var sumOfRewards = new double[steps];
    var bestArmSelected = new int[steps];
    var rewardDistributions = new Normal[numberOfArms];

    for (int round = 0; round < rounds; round++)
    {
        Console.WriteLine($"Round {round + 1} of {rounds}");
        // All actions values start with zero value
        var qStarA = Enumerable.Repeat(0.0, 10).ToArray();

        // The initial estimates of the action values are set to 0.
        var estimatedRewards = InitializeEstimatedRewards(numberOfArms);

        var pickedArms = new Dictionary<int, int>();

        for (int step = 0; step < steps; step++)
        {
            // The random walk
            (qStarA, var bestArm) = UpdateQStarABestArm(qStarA, normal);
            for (int i = 0; i < qStarA.Length; i++)
            {
                rewardDistributions[i] = new Normal(qStarA[i], 1, Random.Shared);
            }
            var selectedArm = strategy(estimatedRewards);
            var reward = GetReward(rewardDistributions, selectedArm);
            if (pickedArms.TryGetValue(selectedArm, out var picked))
            {
                picked += 1;
            }
            else
            {
                picked = 1;
            }

            if (bestArm == selectedArm)
            {
                bestArmSelected[step]++;
            }

            sumOfRewards[step] += reward;
            pickedArms[selectedArm] = picked;
            estimatedRewards[selectedArm] = updateEstimatedReward(estimatedRewards[selectedArm], reward, picked);
        }
    }

    double[] averageRewards = sumOfRewards.Select(i => i / rounds).ToArray();
    double[] bestArmSelectionRate =
        bestArmSelected.Select(Convert.ToDouble).Select(i => i / rounds).ToArray();

    return new(averageRewards, bestArmSelectionRate);
}

var tenPercentExplorationStrategy = GetEpsilonStrategy(0.1);

var sampleAverageResult = RunExperiment(tenPercentExplorationStrategy, sampleAverage);
var constantStepSizeResult = RunExperiment(tenPercentExplorationStrategy, constantStepSize(0.1));


IPalette palette = new ScottPlot.Palettes.Category10();
var blue = palette.GetColor(0);
var red = palette.GetColor(3);
var dataX = Enumerable.Range(0, steps).Select(Convert.ToDouble).ToArray();

Plot averageRewardPlot = new();
var sampleAverageLine =
    averageRewardPlot.Add.ScatterLine(dataX, sampleAverageResult.AverageRewardsPerStep, blue);
sampleAverageLine.LegendText = "sample-average";

var constantStepSizeLine =
    averageRewardPlot.Add.ScatterLine(dataX, constantStepSizeResult.AverageRewardsPerStep, red);
constantStepSizeLine.LegendText = "constant-step";

averageRewardPlot.SavePng("average_reward.png", 1200, 800);

Plot bestArmSelectionRagePlot = new();
var sampleAverageLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, sampleAverageResult.BestArmSelectionRate, blue);
sampleAverageLineBestArmSelection.LegendText = "sample-average";
var constantStepSizeLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, constantStepSizeResult.BestArmSelectionRate, red);
constantStepSizeLineBestArmSelection.LegendText = "constant-step";

bestArmSelectionRagePlot.SavePng("best_arm_selection_rate.png", 1200, 800);

double GetReward(Normal[] rewardDistributions, int arm) =>
    rewardDistributions[arm].Sample();


(double[] qStarA, int bestArm) UpdateQStarABestArm(double[] doubles, Normal distribution)
{
    int bestArm = -1;
    double bestValue = double.MinValue;
    for (int i = 0; i < doubles.Length; i++)
    {
        doubles[i] += distribution.Sample();

        if (doubles[i] > bestValue)
        {
            bestValue = doubles[i];
            bestArm = i;
        }
    }

    return (doubles, bestArm);
}

// I'm defining a delegate to capture the signature of the reward estimate update strategy
delegate double UpdateEstimatedReward(double currentEstimatedReward, double reward, int armSelectedCount);

record ExperimentResult(double[] AverageRewardsPerStep, double[] BestArmSelectionRate);
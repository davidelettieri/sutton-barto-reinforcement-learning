using Common;
using MathNet.Numerics.Distributions;
using ScottPlot;
using static Common.Helper;

// I want to run "experiments" with 10 arms to evaluate the performance of epsilon-greedy methods.
// An experiment consists of 2000 rounds, each round consists of 10000 steps.
const int rounds = 2000;
const int steps = 10000;

// Please check the ten-armed-testbed project for the simplest example of the k-armed bandit problem.
UpdateEstimatedReward constantStepSize(double alpha)
    => (currentEstimatedReward, reward, _) =>
        currentEstimatedReward + alpha * (reward - currentEstimatedReward);

ExperimentResult RunExperiment(SelectArmStrategy strategy, UpdateEstimatedReward updateEstimatedReward)
{
    // Setup: this variable will be used across all rounds.
    // Each arm will be identified by a number between 0 and 9.
    var numberOfArms = 10;
    // This variable will be used to update all the reward distributions at all steps
    var normal = new Normal(0, 0.01, Random.Shared);
    var sumOfRewards = new double[steps];
    var bestArmSelected = new int[steps];
    var rewardDistributions = new Normal[numberOfArms];

    for (var round = 0; round < rounds; round++)
    {
        Console.WriteLine($"Round {round + 1} of {rounds}");
        // All actions values start with zero value
        var qStarA = Enumerable.Repeat(0.0, 10).ToArray();

        var arms = InitializeArms(numberOfArms);

        for (var step = 0; step < steps; step++)
        {
            // The random walk
            (qStarA, var bestArm) = UpdateQStarABestArm(qStarA, normal);
            for (var i = 0; i < qStarA.Length; i++)
            {
                rewardDistributions[i] = new Normal(qStarA[i], 1, Random.Shared);
            }
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

    var averageRewards = sumOfRewards.Select(i => i / rounds).ToArray();
    var bestArmSelectionRate =
        bestArmSelected.Select(Convert.ToDouble).Select(i => i / rounds).ToArray();

    return new(averageRewards, bestArmSelectionRate);
}

var tenPercentExplorationStrategy = GetEpsilonStrategy(0.1);

var sampleAverageResult = RunExperiment(tenPercentExplorationStrategy, SampleAverage);
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
    var bestArm = -1;
    var bestValue = double.MinValue;
    for (var i = 0; i < doubles.Length; i++)
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

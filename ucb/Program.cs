using Common;
using MathNet.Numerics.Distributions;
using ScottPlot;
using static Common.Helper;

// I want to run "experiments" with 10 arms to evaluate the performance of epsilon-greedy methods.
// An experiment consists of 2000 rounds, each round consists of 10000 steps.
const int rounds = 2000;
const int steps = 1000;

ExperimentResult RunExperiment(SelectArmStrategy strategy, UpdateEstimatedReward updateEstimatedReward)
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


var tenPercentEpsilonStrategy = GetEpsilonStrategy(0.1);
var ucbStrategy = GetUcbStrategy(2);

var greedyResult = RunExperiment(tenPercentEpsilonStrategy, SampleAverage);
var ucbResult = RunExperiment(ucbStrategy, SampleAverage);


IPalette palette = new ScottPlot.Palettes.Category10();
var blue = palette.GetColor(0);
var red = palette.GetColor(3);
var dataX = Enumerable.Range(0, steps).Select(Convert.ToDouble).ToArray();

Plot averageRewardPlot = new();
var sampleAverageLine =
    averageRewardPlot.Add.ScatterLine(dataX, greedyResult.AverageRewardsPerStep, blue);
sampleAverageLine.LegendText = "eps=0.1";

var constantStepSizeLine =
    averageRewardPlot.Add.ScatterLine(dataX, ucbResult.AverageRewardsPerStep, red);
constantStepSizeLine.LegendText = "ucb";

averageRewardPlot.SavePng("average_reward.png", 1800, 1200);

Plot bestArmSelectionRagePlot = new();
var sampleAverageLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, greedyResult.BestArmSelectionRate, blue);
sampleAverageLineBestArmSelection.LegendText = "eps=0.1";
var constantStepSizeLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, ucbResult.BestArmSelectionRate, red);
constantStepSizeLineBestArmSelection.LegendText = "ucb";

bestArmSelectionRagePlot.SavePng("best_arm_selection_rate.png", 1800, 1200);

double GetReward(Normal[] rewardDistributions, int arm) =>
    rewardDistributions[arm].Sample();
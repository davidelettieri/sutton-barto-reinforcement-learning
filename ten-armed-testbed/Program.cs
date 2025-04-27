using MathNet.Numerics.Distributions;
using ScottPlot;

// I want to run "experiments" with 10 arms to evaluate the performance of epsilon-greedy methods.
// An experiment consists of 2000 rounds, each round consists of 1000 steps.
const int rounds = 2000;
const int steps = 1000;

// I want to measure:
// 1. Average reward for each step (average reward for step 1, step 2, etc.)
// 2. Percentage of steps where the best arm was selected up to any step (percentage of steps where the best arm was selected after step 1, step 2, etc.)

// To measure 1, I'll keep an array sumOfRewards of length 1000, each element of the array will be the sum of rewards at that step for each round.
// At the end of all rounds, I'll divide the sum by 2000 to get the average reward for each step.

// To measure 2, I'll keep an array bestArmSelected of length 1000, each element of the array will be the number of times the best arm was selected at that step for each round.
// At the end of all rounds, I'll compute for each step "i" the percentage of times the best arm was selected up to that step using the following formula:

// We define an epsilon-greedy method as a function getting the estimated reward for each arm and returning the index of the arm to be selected.
// We accept epsilon as a parameter, note that with epsilon=0 we get the greedy strategy. Estimated reward is modeled as a dictionary.
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


ExperimentResult RunExperiment(Func<Dictionary<int, double>, int> strategy)
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
            .Select(x => new Normal(x, 1, new Random()))
            .ToArray();

        // The initial estimates of the action values are set to 0.
        var estimatedRewards = InitializeEstimatedRewards(numberOfArms);

        var pickedArms = new Dictionary<int, PickedArm>();
        var bestArm = qStarA.Select((v, i) => (v, i)).MaxBy(el => el.v).i;

        for (int step = 0; step < steps; step++)
        {
            var selectedArm = strategy(estimatedRewards);
            var reward = GetReward(rewardDistributions, selectedArm);
            if (pickedArms.TryGetValue(selectedArm, out var picked))
            {
                picked = new(picked.Count + 1, picked.TotalReward + reward);
            }
            else
            {
                picked = new(1, reward);
            }

            if (bestArm == selectedArm)
            {
                bestArmSelected[step]++;
            }

            sumOfRewards[step] += reward;
            pickedArms[selectedArm] = picked;
            estimatedRewards[selectedArm] = picked.TotalReward / picked.Count;
        }
    }

    double[] averageRewards = sumOfRewards.Select(i => i / rounds).ToArray();
    double[] bestArmSelectionRate =
        bestArmSelected.Select(Convert.ToDouble).Select(i => i / rounds).ToArray(); // new double[steps];

    // int total = 0;
    // for (int i = 0; i < steps; i++)
    // {
    //     total += bestArmSelected[i];
    //
    //     bestArmSelectionRate[i] = (double)total / (rounds * (i + 1));
    // }

    return new(averageRewards, bestArmSelectionRate);
}

var onePercentExplorationStrategy = GetEpsilonStrategy(0.01);
var tenPercentExplorationStrategy = GetEpsilonStrategy(0.1);
var greedyStrategy = GetEpsilonStrategy(0);

var tenPercentExperimentResult = RunExperiment(tenPercentExplorationStrategy);
var onePercentExperimentResult = RunExperiment(onePercentExplorationStrategy);
var greedyExperimentResultExperimentResult = RunExperiment(greedyStrategy);


IPalette palette = new ScottPlot.Palettes.Category10();
var blue = palette.GetColor(0);
var red = palette.GetColor(3);
var green = palette.GetColor(2);
var dataX = Enumerable.Range(0, steps).Select(Convert.ToDouble).ToArray();

Plot averageRewardPlot = new();
var tenPercentLineReward =
    averageRewardPlot.Add.ScatterLine(dataX, tenPercentExperimentResult.AverageRewardsPerStep, blue);
tenPercentLineReward.LegendText = "epsilon = 0.1";
var onePercentLineReward =
    averageRewardPlot.Add.ScatterLine(dataX, onePercentExperimentResult.AverageRewardsPerStep, red);
onePercentLineReward.LegendText = "epsilon = 0.01";
var greedyLineReward =
    averageRewardPlot.Add.ScatterLine(dataX, greedyExperimentResultExperimentResult.AverageRewardsPerStep, green);
greedyLineReward.LegendText = "greedy";

averageRewardPlot.SavePng("average_reward.png", 1200, 800);

Plot bestArmSelectionRagePlot = new();
var tenPercentLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, tenPercentExperimentResult.BestArmSelectionRate, blue);
tenPercentLineBestArmSelection.LegendText = "epsilon = 0.1";
var onePercentLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, onePercentExperimentResult.BestArmSelectionRate, red);
onePercentLineBestArmSelection.LegendText = "epsilon = 0.01";
var greedyLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, greedyExperimentResultExperimentResult.BestArmSelectionRate, green);
greedyLineBestArmSelection.LegendText = "greedy";

bestArmSelectionRagePlot.SavePng("best_arm_selection_rate.png", 1200, 800);

double GetReward(Normal[] rewardDistributions, int arm) =>
    rewardDistributions[arm].Sample();

record PickedArm(int Count, double TotalReward);

record ExperimentResult(double[] AverageRewardsPerStep, double[] BestArmSelectionRate);
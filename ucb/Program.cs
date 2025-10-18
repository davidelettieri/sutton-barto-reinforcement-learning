using ScottPlot;
using static Common.Helper;

// I want to run "experiments" with 10 arms to evaluate the performance of epsilon-greedy methods.
// An experiment consists of 2000 rounds, each round consists of 1000 steps.
const int rounds = 2000;
const int steps = 1000;

var tenPercentEpsilonStrategy = GetEpsilonStrategy(0.1);
var ucbStrategy = GetUcbStrategy(2);

var tenPercentResult = RunStationaryExperiment(steps, rounds, tenPercentEpsilonStrategy, SampleAverage);
var ucbResult = RunStationaryExperiment(steps, rounds, ucbStrategy, SampleAverage);


IPalette palette = new ScottPlot.Palettes.Category10();
var blue = palette.GetColor(0);
var red = palette.GetColor(3);
var dataX = Enumerable.Range(0, steps).Select(Convert.ToDouble).ToArray();

Plot averageRewardPlot = new();
var sampleAverageLine =
    averageRewardPlot.Add.ScatterLine(dataX, tenPercentResult.AverageRewardsPerStep, blue);
sampleAverageLine.LegendText = "eps=0.1";

var constantStepSizeLine =
    averageRewardPlot.Add.ScatterLine(dataX, ucbResult.AverageRewardsPerStep, red);
constantStepSizeLine.LegendText = "ucb";

averageRewardPlot.SavePng("average_reward.png", 1800, 1200);

Plot bestArmSelectionRagePlot = new();
var sampleAverageLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, tenPercentResult.BestArmSelectionRate, blue);
sampleAverageLineBestArmSelection.LegendText = "eps=0.1";
var constantStepSizeLineBestArmSelection =
    bestArmSelectionRagePlot.Add.ScatterLine(dataX, ucbResult.BestArmSelectionRate, red);
constantStepSizeLineBestArmSelection.LegendText = "ucb";

bestArmSelectionRagePlot.SavePng("best_arm_selection_rate.png", 1800, 1200);

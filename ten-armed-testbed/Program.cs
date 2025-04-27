using MathNet.Numerics.Distributions;

// I want to run "experiments" with 10 arms to evaluate the performance of epsilon-greedy methods.
// An experiment consists of 2000 rounds, each round consists of 1000 steps.

// I want to measure:
// 1. Average reward for each step (average reward for step 1, step 2, etc.)
// 2. Percentage of steps where the best arm was selected up to any step (percentage of steps where the best arm was selected after step 1, step 2, etc.)

// To measure 1, I'll keep an array sumOfRewards of length 1000, each element of the array will be the sum of rewards at that step for each round.
// At the end of all rounds, I'll divide the sum by 2000 * 1000 to get the average reward for each step.

// To measure 2, I'll keep an array bestArmSelected of length 1000, each element of the array will be the number of times the best arm was selected at that step for each round.
// At the end of all rounds, I'll compute for each step "i" the percentage of times the best arm was selected up to that step using the following formula:
// (SUM (over j <= i) of bestArmSelected[j]) / (2000 * i)
// The sum is counting the total number of times the best arm was selected up to step i.
// The (2000 * i) is counting the total number of selections we made up to step i e.g., for each step we made 2000 selections.  

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


void RunExperiment(Func<Dictionary<int, double>, int> strategy)
{
    // Setup: this variable will be used across all rounds.
    // Each arm will be identified by a number between 0 and 9.
    var rounds = 2000;
    var steps = 1000;
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
        var roundReward = 0.0;

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

            roundReward += reward;
            sumOfRewards[step] += roundReward;
            pickedArms[selectedArm] = picked;
            estimatedRewards[selectedArm] = picked.TotalReward / picked.Count;
        }
    }

    double[] dataX = Enumerable.Range(0, steps).Select(i => i + 1.0).ToArray();
    // double[] dataY = sumOfRewards.Select(i => i / (rounds * steps)).ToArray();
    double[] dataY2 = new double[steps];

    int total = 0;
    for (int i = 0; i < steps; i++)
    {
        total += bestArmSelected[i];

        dataY2[i] = (double)total / (rounds * (i + 1));
    }

    ScottPlot.Plot myPlot = new();
    // myPlot.Add.Scatter(dataX, dataY);
    myPlot.Add.Scatter(dataX, dataY2);

    myPlot.SavePng("quickstart.png", 800, 600);
}


var strategy = GetEpsilonStrategy(0.01);

RunExperiment(strategy);


double GetReward(Normal[] rewardDistributions, int arm) =>
    rewardDistributions[arm].Sample();


record PickedArm(int Count, double TotalReward);
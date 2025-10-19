var rows = 5;
var columns = 5;
var gw = new GridWorld(rows, columns, 0.9);
var stateValues = gw.ComputeStateValuesFunction();
Console.WriteLine("Value Function V(s):");
PrintMatrix(stateValues);

gw = new GridWorld(rows, columns, 0.9);
var optimalStateValues = gw.ComputeStateValuesFunctionWithOptimalPolicy();
Console.WriteLine("Optimal Value Function V(s):");
PrintMatrix(optimalStateValues);

void PrintMatrix(double[,] doubles)
{
    for (var i = 0; i < rows; i++)
    {
        for (var j = 0; j < columns; j++)
        {
            Console.Write($"{doubles[i, j],6:F1} ");
        }

        Console.WriteLine();
    }
}

public sealed class GridWorld
{
    private const double Tolerance = 1e-6;
    private readonly int _rows;
    private readonly int _columns;
    private readonly double _gamma;
    private readonly int _states;
    private readonly double[,] _stateValues;
    private readonly double[] _v;
    private readonly int _specialStateA;
    private readonly int _specialStateB;
    private readonly int _specialStateAPrime;
    private readonly int _specialStateBPrime;

    public GridWorld(int rows, int columns, double gamma)
    {
        _rows = rows;
        _columns = columns;
        _gamma = gamma;
        _states = rows * columns;
        _stateValues = new double[rows, columns];
        _v = new double[_states];

        _specialStateA = StateFromCoordinates(0, 1);
        _specialStateB = StateFromCoordinates(0, 3);
        _specialStateAPrime = StateFromCoordinates(4, 1);
        _specialStateBPrime = StateFromCoordinates(2, 3);
    }

    /// <summary>
    /// Corresponds to compute-V function in Sutton & Barto lisp code
    /// </summary>
    /// <returns>State value function as a matrix</returns>
    public double[,] ComputeStateValuesFunction()
        => Compute(ValueFunction);

    /// <summary>
    /// Corresponds to compute-V* function in Sutton & Barto lisp code
    /// </summary>
    /// <returns>State value function as a matrix</returns>
    public double[,] ComputeStateValuesFunctionWithOptimalPolicy()
        => Compute(OptimalValueFunction);

    private double[,] Compute(Func<int, double> valueFunction)
    {
        double delta;
        do
        {
            delta = 0.0;
            for (var state = 0; state < _states; state++)
            {
                var vOld = _v[state];
                var vNew = valueFunction(state);
                _v[state] = vNew;
                delta = Math.Max(delta, Math.Abs(vOld - vNew));
            }
        } while (delta > Tolerance);

        for (var state = 0; state < _states; state++)
        {
            var (x, y) = CoordinatesFromState(state);
            _stateValues[x, y] = _v[state];
        }

        return _stateValues;
    }

    private double ValueFunction(int state)
        => Enum.GetValues<Action>()
            .Select(a => FullBackup(state, a))
            .Average();

    private double OptimalValueFunction(int state)
        => Enum.GetValues<Action>()
            .Select(a => FullBackup(state, a))
            .Max();

    int StateFromCoordinates(int row, int col)
        => col + (row * _columns);

    (int row, int col) CoordinatesFromState(int state)
        => (state / _columns, state % _columns);

    bool OffGrid(int state, Action action)
    {
        var (row, col) = CoordinatesFromState(state);
        return action switch
        {
            Action.North => row <= 0,
            Action.East => col + 1 >= _columns,
            Action.South => row + 1 >= _rows,
            Action.West => col <= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(action), "Invalid action"),
        };
    }

    int NextState(int state, Action action)
    {
        if (state == _specialStateA)
        {
            return _specialStateAPrime;
        }

        if (state == _specialStateB)
        {
            return _specialStateBPrime;
        }

        if (OffGrid(state, action))
        {
            return state;
        }

        var (row, col) = CoordinatesFromState(state);
        return action switch
        {
            Action.East => StateFromCoordinates(row, col + 1),
            Action.South => StateFromCoordinates(row + 1, col),
            Action.West => StateFromCoordinates(row, col - 1),
            Action.North => StateFromCoordinates(row - 1, col),
            _ => throw new ArgumentOutOfRangeException(nameof(action), "Invalid action"),
        };
    }

    double FullBackup(int state, Action a)
    {
        var nextState = NextState(state, a);
        double reward;

        if (state == _specialStateA)
        {
            reward = 10;
        }
        else if (state == _specialStateB)
        {
            reward = 5;
        }
        // implicitly handles off-grid moves
        else if (nextState == state)
        {
            reward = -1;
        }
        else
        {
            reward = 0;
        }

        return reward + (_gamma * _v[nextState]);
    }
}

enum Action
{
    South = 0,
    East = 1,
    North = 2,
    West = 3
}

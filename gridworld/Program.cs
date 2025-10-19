const int Rows = 5;
const int Columns = 5;
const double Gamma = 0.9;
var states = Rows * Columns;
var aa = StateFromCoordinates(0, 1);
var bb = StateFromCoordinates(0, 3);
var aaprime = StateFromCoordinates(4, 1);
var bbprime = StateFromCoordinates(2, 3);
var v = new double[states];
var vv = new double[Rows, Columns];

ComputeV();

void ComputeV()
{
    double delta;
    do
    {
        delta = 0.0;
        for (var state = 0; state < states; state++)
        {
            var vOld = v[state];
            var vNew = Enum.GetValues<Action>()
                .Select(a => FullBackup(state, a))
                .Average();
            v[state] = vNew;
            delta = Math.Abs(vOld - v[state]);
        }
    } while (delta > 1e-6);

    for (var state = 0; state < states; state++)
    {
        var (x, y) = CoordinatesFromState(state);
        vv[x, y] = v[state];
    }
    
    Console.WriteLine("Value Function V(s):");
    for (var i = 0; i < Rows; i++)
    {
        for (var j = 0; j < Columns; j++)
        {
            Console.Write($"{vv[i, j],6:F2} ");
        }

        Console.WriteLine();
    }
}

void ComputeVStar()
{
    double delta;
    do
    {
        delta = 0.0;
        for (var state = 0; state < states; state++)
        {
            var vOld = v[state];
            var vNew = Enum.GetValues<Action>()
                .Select(a => FullBackup(state, a))
                .Max();
            v[state] = vNew;
            delta = Math.Abs(vOld - v[state]);
        }
    } while (delta > 1e-6);

    for (var state = 0; state < states; state++)
    {
        var (x, y) = CoordinatesFromState(state);
        vv[x, y] = v[state];
    }

    Console.WriteLine("Value Function V(s):");
    for (var i = 0; i < Rows; i++)
    {
        for (var j = 0; j < Columns; j++)
        {
            Console.Write($"{vv[i, j],6:F2} ");
        }
    }
}

double FullBackup(int state, Action a)
{
    double r;
    int nextState;

    if (state == aa)
    {
        r = 10.0;
        nextState = aaprime;
    }
    else if (state == bb)
    {
        r = 5.0;
        nextState = bbprime;
    }
    else if (OffGrid(state, a))
    {
        r = -1.0;
        nextState = state;
    }
    else
    {
        r = 0.0;
        nextState = NextState(state, a);
    }

    return r + (Gamma * v[nextState]);
}

static bool OffGrid(int state, Action action)
{
    var (row, col) = CoordinatesFromState(state);
    return action switch
    {
        Action.North => row <= 0,
        Action.East => (col + 1) >= Columns,
        Action.South => (row + 1) >= Rows,
        Action.West => col <= 0,
        _ => throw new ArgumentOutOfRangeException(nameof(action), "Invalid action"),
    };
}

static int NextState(int state, Action action)
{
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

static int StateFromCoordinates(int row, int col)
    => col + (row * Columns);

static (int row, int col) CoordinatesFromState(int state)
    => (state / Columns, state % Columns);

enum Action
{
    South = 0,
    East = 1,
    North = 2,
    West = 3
}

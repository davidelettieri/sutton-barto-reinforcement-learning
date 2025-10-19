const int Rows = 5;
const int Columns = 5;
const double Gamma = 0.9;
var states = Rows * Columns;
var aa = StateFromCoordinates(1, 0);
var bb = StateFromCoordinates(3, 0);
var aaprime = StateFromCoordinates(1, 4);
var bbprime = StateFromCoordinates(3, 2);
var v = new double[states];
var vv = new double[Rows, Columns];

ComputeV();

void ComputeV()
{
    double delta;
    do
    {
        delta = 0.0;
        for (int x = 0; x < states; x++)
        {
            double vOld = v[x];
            double vNew = Enum.GetValues<Action>()
                .Select(a => FullBackup(x, a))
                .Average();
            v[x] = vNew;
            delta = Math.Abs(vOld - v[x]);
        }
    } while (delta > 1e-6);

    for (int state = 0; state < states; state++)
    {
        var (x, y) = CoordinatesFromState(state);
        vv[x, y] = v[state];
    }

    Console.WriteLine("Value Function V(s):");
    for (int i = 0; i < Rows; i++)
    {
        for (int j = 0; j < Columns; j++)
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
        for (int x = 0; x < states; x++)
        {
            double vOld = v[x];
            double vNew = Enum.GetValues<Action>()
                .Select(a => FullBackup(x, a))
                .Max();
            v[x] = vNew;
            delta = Math.Abs(vOld - v[x]);
        }
    } while (delta > 1e-6);

    for (int state = 0; state < states; state++)
    {
        var (x, y) = CoordinatesFromState(state);
        vv[x, y] = v[state];
    }

    Console.WriteLine("Value Function V(s):");
    for (int i = 0; i < Rows; i++)
    {
        for (int j = 0; j < Columns; j++)
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
    var (x, y) = CoordinatesFromState(state);
    return action switch
    {
        Action.North => (y + 1) >= Rows,
        Action.East => (x + 1) >= Columns,
        Action.South => y <= 0,
        Action.West => x <= 0,
        _ => throw new ArgumentOutOfRangeException(nameof(action), "Invalid action"),
    };
}

static int NextState(int state, Action action)
{
    var (x, y) = CoordinatesFromState(state);
    return action switch
    {
        Action.North => StateFromCoordinates(x, y + 1),
        Action.East => StateFromCoordinates(x + 1, y),
        Action.South => StateFromCoordinates(x, y - 1),
        Action.West => StateFromCoordinates(x - 1, y),
        _ => throw new ArgumentOutOfRangeException(nameof(action), "Invalid action"),
    };
}

static int StateFromCoordinates(int x, int y)
    => y + (x * Columns);

static (int x, int y) CoordinatesFromState(int state)
    => (state / Columns, state % Columns);

enum Action
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

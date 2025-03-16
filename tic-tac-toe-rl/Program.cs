var playerX = TrainQPlayer(20000);
var humanPlayer = new HumanPlayer(PlayerSymbol.O);
Console.WriteLine("Begin human game!");
var humanGame = new State(new PlayerSymbol?[3, 3]);
IPlayer toMove = playerX;
do
{
    var move = toMove.GetMove(humanGame);
    humanGame = humanGame.WithMove(toMove.Symbol, move);
    humanGame.PrintBoard();
    toMove = toMove.Symbol == playerX.Symbol ? humanPlayer : playerX;
} while (humanGame.GetWinner() is null && humanGame.GetAvailableMoves().Count > 0);

var winner = humanGame.GetWinner();

if (winner == null)
{
    Console.WriteLine("No winner!");
}
else
{
    Console.WriteLine("The winner is {0}", winner);
}

QPlayer TrainQPlayer(int trainingIterations)
{
    var startState = new State(new PlayerSymbol?[3, 3]);

    var allStates = GetAllStates(startState, PlayerSymbol.X)
        .Distinct()
        .ToDictionary(p => p, p => p.GetWinner());

    var pX = new QPlayer(PlayerSymbol.X, allStates, 0.1);
    var pO = new QPlayer(PlayerSymbol.O, allStates, 0.5);

    Console.WriteLine("Begin training Q players!");
    for (int i = 0; i < trainingIterations; i++)
    {
        Console.WriteLine("Iteration {0}", i);
        var game = new State(new PlayerSymbol?[3, 3]);
        pX.Reset();
        pO.Reset();
        var toMove = pX;
        do
        {
            var move = toMove.Train(game);
            game = game.WithMove(toMove.Symbol, move);
            toMove = toMove.Symbol == pX.Symbol ? pO : pX;
        } while (game.GetWinner() is null && game.GetAvailableMoves().Count > 0);

        if (game.GetWinner() == PlayerSymbol.O || toMove.Symbol == PlayerSymbol.X)
        {
            pX.Learn(game);
        }

        if (game.GetWinner() == PlayerSymbol.X || toMove.Symbol == PlayerSymbol.O)
        {
            pO.Learn(game);
        }
    }

    Console.WriteLine("Q players trained!");

    return pX;

    IEnumerable<State> GetAllStates(State startState, PlayerSymbol toMove)
    {
        yield return startState;

        if (startState.GetWinner() is not null)
        {
            yield break;
        }

        var availableMoves = startState.GetAvailableMoves();

        foreach (var move in availableMoves)
        {
            var nextState = startState.WithMove(toMove, move);

            foreach (var states in GetAllStates(nextState, toMove == PlayerSymbol.O ? PlayerSymbol.X : PlayerSymbol.O))
            {
                yield return states;
            }
        }
    }
}

class State(PlayerSymbol?[,] positions) : IEquatable<State>
{
    public PlayerSymbol?[,] Positions { get; } = positions;

    public List<(int i, int j)> GetAvailableMoves()
        => Enumerable.Range(0, 3)
            .Select(i => Enumerable.Range(0, 3).Select(j => (i, j)))
            .SelectMany(a => a)
            .Where(c => Positions[c.i, c.j] is null)
            .ToList();

    public PlayerSymbol? GetWinner()
    {
        if (IsWinner(PlayerSymbol.X))
            return PlayerSymbol.X;
        if (IsWinner(PlayerSymbol.O))
            return PlayerSymbol.O;

        return null;
    }

    public void PrintBoard()
    {
        Console.WriteLine($"| {Positions[0, 0]} | {Positions[0, 1]} | {Positions[0, 2]} |");
        Console.WriteLine($"| {Positions[1, 0]} | {Positions[1, 1]} | {Positions[1, 2]} |");
        Console.WriteLine($"| {Positions[2, 0]} | {Positions[2, 1]} | {Positions[2, 2]} |");
    }

    public State WithMove(PlayerSymbol player, (int i, int j) move)
    {
        var p = Positions.Clone() as PlayerSymbol?[,];
        p![move.i, move.j] = player;
        return new(p);
    }

    private bool IsWinner(PlayerSymbol player)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Positions[i, 0] == player && Positions[i, 1] == player && Positions[i, 2] == player)
                return true;

            if (Positions[0, i] == player && Positions[1, i] == player && Positions[2, i] == player)
                return true;
        }

        if (Positions[0, 0] == player && Positions[1, 1] == player && Positions[2, 2] == player)
            return true;

        if (Positions[0, 2] == player && Positions[1, 1] == player && Positions[2, 0] == player)
            return true;

        return false;
    }

    public bool Equals(State? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Positions[i, j] != other.Positions[i, j])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((State)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            HashCode.Combine(Positions[0, 0], Positions[0, 1], Positions[0, 2]),
            HashCode.Combine(Positions[1, 0], Positions[1, 1], Positions[1, 2]),
            HashCode.Combine(Positions[2, 0], Positions[2, 1], Positions[2, 2]));
    }
}

enum PlayerSymbol
{
    X,
    O
}

internal interface IPlayer
{
    PlayerSymbol Symbol { get; }
    (int i, int j) GetMove(State state);
}

class QPlayer : IPlayer
{
    private readonly Dictionary<State, double> _qTable = new();
    private readonly double _explorationRate;
    private readonly double _learningRate = 0.5;
    private State? _lastState;

    public PlayerSymbol Symbol { get; }

    public QPlayer(PlayerSymbol symbol, Dictionary<State, PlayerSymbol?> states, double explorationRate)
    {
        Symbol = symbol;
        InitializeQTable(states);
        _explorationRate = explorationRate;
    }

    private void InitializeQTable(Dictionary<State, PlayerSymbol?> states)
    {
        foreach (var (state, winner) in states)
        {
            if (winner == Symbol)
            {
                _qTable[state] = 1;
            }
            else if (winner is not null)
            {
                _qTable[state] = 0;
            }
            else
            {
                _qTable[state] = 0.5;
            }
        }
    }

    public (int i, int j) Train(State state)
    {
        if (Random.Shared.NextDouble() < _explorationRate)
        {
            var move = state.GetAvailableMoves().OrderBy(_ => Random.Shared.NextDouble()).First();
            return move;
        }

        return GetMove(state);
    }

    public (int i, int j) GetMove(State state)
    {
        (int i, int j)? bestMove = null;
        double maxValue = double.MinValue;
        State? selectedNextState = null;

        foreach (var move in state.GetAvailableMoves().OrderBy(_ => Random.Shared.NextDouble()))
        {
            var nextPositions = (state.Positions.Clone() as PlayerSymbol?[,])!;
            nextPositions[move.i, move.j] = Symbol;
            var candidateState = new State(nextPositions);

            if (_qTable[candidateState] > maxValue)
            {
                bestMove = move;
                selectedNextState = candidateState;
                maxValue = _qTable[candidateState];
            }
        }

        if (bestMove is null || selectedNextState is null)
        {
            throw new Exception("No move available");
        }

        if (_lastState is not null)
        {
            _qTable[_lastState] += _learningRate * (_qTable[selectedNextState] - _qTable[_lastState]);
        }

        _lastState = selectedNextState;
        return bestMove.Value;
    }

    public void Learn(State state)
    {
        if (_lastState is not null)
        {
            _qTable[_lastState] += _learningRate * (_qTable[state] - _qTable[_lastState]);
        }
    }

    public void Reset()
    {
        _lastState = null;
    }
}

class HumanPlayer(PlayerSymbol symbol) : IPlayer
{
    public PlayerSymbol Symbol { get; } = symbol;

    public (int i, int j) GetMove(State state)
    {
        if (state.GetAvailableMoves().Count > 0)
        {
            Console.Write("Provide your move as i,j: ");
            var pos = Console.ReadLine()!.Split(',').Select(int.Parse).ToArray();
            var move = (pos[0], pos[1]);

            if (state.GetAvailableMoves().Contains(move))
            {
                return move;
            }
            else
            {
                Console.WriteLine("Invalid move");
                return GetMove(state);
            }
        }

        throw new InvalidOperationException("No move available");
    }
}
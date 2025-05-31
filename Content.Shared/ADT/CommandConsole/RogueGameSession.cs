using System.Text;

public sealed class RogueGameSession
{
    private Vector2i _playerPosition;
    public bool IsOver { get; private set; } = false;

    public string Start()
    {
        _playerPosition = new Vector2i(1, 1);
        return "Welcome to RogueSim!\n" + RenderMap();
    }

    public string ProcessInput(string input)
    {
        input = input.Trim().ToLowerInvariant();
        switch (input)
        {
            case "w": _playerPosition.Y = Math.Max(0, _playerPosition.Y - 1); break;
            case "s": _playerPosition.Y = Math.Min(9, _playerPosition.Y + 1); break;
            case "a": _playerPosition.X = Math.Max(0, _playerPosition.X - 1); break;
            case "d": _playerPosition.X = Math.Min(19, _playerPosition.X + 1); break;
            case "exit":
                IsOver = true;
                return "Exiting game...\n";
            default:
                return "Invalid command. Use WASD to move, or 'exit' to quit.";
        }

        return RenderMap();
    }

    private string RenderMap()
    {
        var sb = new StringBuilder();
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                if (_playerPosition.X == x && _playerPosition.Y == y)
                    sb.Append('@');
                else
                    sb.Append('#');
            }
            sb.Append('\n');
        }

        return sb.ToString();
    }
}

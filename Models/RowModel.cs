using System.Collections.Generic;

namespace PadelTournamentManager.Models;

public class RowModel
{
    public string Name { get; init; } = "";
    public Player Player { get; init; } = default!;
    public Dictionary<int, int> PointsByRound { get; } = new(); // round -> pts
    public Dictionary<int, bool> WinsByRound { get; } = new(); // round -> won?
    public Dictionary<int, bool> ByeByRound { get; } = new(); // round -> is bye?
    public int Total { get; set; }
    public int Wins { get; set; }
    public int Position { get; set; }
}
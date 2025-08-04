using System.Collections.Generic;

namespace PadelTournamentManager.Models;

public class Match
{
    public int Round { get; set; }

    public List<Player> Team1 { get; set; } = [];

    public List<Player> Team2 { get; set; } = [];

    public string CourtName { get; set; }

    public int? Team1Points { get; set; }

    public int? Team2Points { get; set; }

    public bool IsSkipper { get; set; }
}
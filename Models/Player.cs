using System.Collections.Generic;
using System.Linq;

namespace PadelTournamentManager.Models;

public class Player
{
    public Player(string name, int order)
    {
        Name = name;
        Order = order;
    }

    public string Name { get; set; }

    public int Order { get; set; }

    public bool IsEditing { get; set; }

    public string? EditName { get; set; }

    public List<Match> MatchHistory = [];

    public int Points => MatchHistory
                             .Where(m => m.Team1.Contains(this))
                             .Sum(m => m.Team1Points ?? 0)
                         +
                         MatchHistory
                             .Where(m => m.Team2.Contains(this))
                             .Sum(m => m.Team2Points ?? 0);

    public int Wins => MatchHistory.Count(m =>
        (m.Team1.Contains(this) && m.Team1Points > m.Team2Points) ||
        (m.Team2.Contains(this) && m.Team2Points > m.Team1Points));
}

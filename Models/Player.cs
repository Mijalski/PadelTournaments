using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

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

    public int Points => MatchHistory.Sum(m =>
        m.Team1.Any(p => string.Equals(p.Name, Name, StringComparison.OrdinalIgnoreCase))
            ? m.Team1Points.GetValueOrDefault()
            : m.Team2.Any(p => string.Equals(p.Name, Name, StringComparison.OrdinalIgnoreCase))
                ? m.Team2Points.GetValueOrDefault()
                : 0);

    public int Wins => MatchHistory.Count(m =>
        (m.Team1.Any(p => string.Equals(p.Name, Name, StringComparison.OrdinalIgnoreCase)) &&
         m.Team1Points.GetValueOrDefault() > m.Team2Points.GetValueOrDefault())
        ||
        (m.Team2.Any(p => string.Equals(p.Name, Name, StringComparison.OrdinalIgnoreCase)) &&
         m.Team2Points.GetValueOrDefault() > m.Team1Points.GetValueOrDefault()));
}

using System.Collections.Generic;

namespace PadelTournamentManager.Models;

public class Player
{
    public Player(string name, int order)
    {
        Name = name;
        Order = order;
    }

    public string Name { get; set; }

    public int Wins { get; set; } = 0;

    public int Points { get; set; } = 0;

    public int Order { get; set; }

    public bool IsEditing { get; set; }

    public string? EditName { get; set; }

    public List<Match> MatchHistory = new();
}
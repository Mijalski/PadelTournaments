using System.Collections.Generic;

namespace PadelTournamentManager.Models;

public class Match
{
    public int Round { get; set; }

    public List<Player> Team1 { get; set; } = new();

    public List<Player> Team2 { get; set; } = new();

    public string CourtName { get; set; }

    public int? Team1Points { get; set; }

    public int? Team2Points { get; set; }

    public void RecordResult(int team1Points, int team2Points)
    {
        Team1Points = team1Points;
        Team2Points = team2Points;

        if (team1Points > team2Points)
        {
            foreach (var p in Team1)
            {
                p.Wins++;
                p.Points += team1Points;
            }
            foreach (var p in Team2)
            {
                p.Points += team2Points;
            }
        }
        else if (team2Points > team1Points)
        {
            foreach (var p in Team2)
            {
                p.Wins++;
                p.Points += team2Points;
            }
            foreach (var p in Team1)
            {
                p.Points += team1Points;
            }
        }
        else
        {
            foreach (var p in Team1)
            {
                p.Points += team1Points;
            }
            foreach (var p in Team2)
            {
                p.Points += team2Points;
            }
        }
    }
}
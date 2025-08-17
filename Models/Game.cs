using PadelTournamentManager.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PadelTournamentManager.Models;

public class Game
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public TournamentType TournamentType { get; set; } = TournamentType.Mexicano;

    public TeamFormat TeamFormat { get; set; } = TeamFormat.Pairs;

    public ResultSorting ResultSorting { get; set; } = ResultSorting.WinsOverPoints;

    public FinalPairingOption FinalPairingOption { get; set; } = FinalPairingOption.OneTwoVsThreeFour;

    public ScoringType ScoringType { get; set; } = ScoringType.Points;

    public int ToPoints { get; set; } = 21;

    public List<Court> Courts { get; set; } = [];

    public List<Player> Players { get; set; } = [];

    public List<Match> Matches { get; set; } = [];

    public int CurrentRound { get; set; } = 0;

    public bool IsFinalRound { get; set; }

    public int FinalRound { get; set; } = 0;

    public List<RowModel> ComputeFinalRanking()
    {
        var map = new Dictionary<Player, RowModel>();

        foreach (var player in Players)
        {
            map[player] = new RowModel { Name = player.Name, Player = player };
        }

        foreach (var match in Matches)
        {
            var isBye = string.Equals(match.CourtName, "BYE", StringComparison.OrdinalIgnoreCase) || match.Team2.Count == 0;
            if (!isBye)
            {
                var t1win = match.Team1Points > match.Team2Points;
                var t2win = match.Team2Points > match.Team1Points;

                foreach (var p in match.Team1)
                {
                    AddFinalRoundStat(map[p], match.Round, match.Team1Points ?? 0, t1win);
                }

                foreach (var p in match.Team2)
                {
                    AddFinalRoundStat(map[p], match.Round, match.Team2Points ?? 0, t2win);
                }
            }
            else
            {
                foreach (var p in match.Team1)
                {
                    AddFinalRoundStat(map[p], match.Round, match.Team1Points ?? 0, false, true);
                }
            }
        }

        var rows = map.Values.ToList();

        rows = ResultSorting == ResultSorting.WinsOverPoints
            ? rows.OrderByDescending(r => r.Wins).ThenByDescending(r => r.Total).ToList()
            : rows.OrderByDescending(r => r.Total).ThenByDescending(r => r.Wins).ToList();

        for (int i = 0; i < rows.Count; i++)
            rows[i].Position = i + 1;

        return rows;
    }

    private static void AddFinalRoundStat(RowModel row, int round, int points, bool win, bool isBye = false)
    {
        row.Total += points;
        if (win)
        {
            row.Wins++;
        }
    }
}
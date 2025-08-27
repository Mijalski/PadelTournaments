using PadelTournamentManager.Models.Enums;
using System;
using System.Collections.Generic;

namespace PadelTournamentManager.Models;

public class SavedGame
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = "";

    public DateTime SavedAt { get; set; }

    public TournamentType TournamentType { get; set; }

    public ResultSorting ResultSorting { get; set; }

    public TeamFormat TeamFormat { get; set; }

    public FinalPairingOption FinalPairingOption { get; set; }

    public ScoringType ScoringType { get; set; }

    public int ToPoints { get; set; }

    public List<Player> Players { get; set; } = new();
    public List<Match> Matches { get; set; } = new();
    public List<Court> Courts { get; set; } = new();

    public bool IsFinalRound { get; set; }

    public int FinalRound { get; set; }

    public string Winner { get; set; } = default!;

    public bool AllowRerolls { get; set; } = false;

    public bool AllowRemovePlayers { get; set; } = false;
}

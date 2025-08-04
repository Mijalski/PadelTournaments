using PadelTournamentManager.Models.Enums;
using System.Collections.Generic;

namespace PadelTournamentManager.Models;

public class Game
{
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
}
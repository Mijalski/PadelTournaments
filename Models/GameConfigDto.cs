using PadelTournamentManager.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace PadelTournamentManager.Models;

public record GameConfigDto(
    TournamentType TournamentType,
    TeamFormat TeamFormat,
    ResultSorting ResultSorting,
    FinalPairingOption FinalPairingOption,
    ScoringType ScoringType,
    int ToPoints,
    List<string> Courts
)
{
    public const string GameConfigKey = "padel:game-config:v1";

    public static GameConfigDto FromGame(Game g) =>
        new(g.TournamentType, g.TeamFormat, g.ResultSorting, g.FinalPairingOption,
            g.ScoringType, g.ToPoints, g.Courts.Select(c => c.Name).ToList());

    public void ApplyTo(Game g)
    {
        g.TournamentType = TournamentType;
        g.TeamFormat = TeamFormat;
        g.ResultSorting = ResultSorting;
        g.FinalPairingOption = FinalPairingOption;
        g.ScoringType = ScoringType;
        g.ToPoints = ToPoints;

        // Replace courts with names from the config
        g.Courts = Courts.Select((name, i) => new Court(name, i)).ToList();
    }
}

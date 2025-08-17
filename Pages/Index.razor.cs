using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PadelTournamentManager.Models;
using PadelTournamentManager.Models.Enums;
using PadelTournamentManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PadelTournamentManager.Pages;

public partial class Index
{
    [Inject]
    IJSRuntime JSRuntime { get; set; }

    private ScorePicker _scorePicker = default!;
    private HistoryModal _historyModal = default!;
    SavedGamesModal? SavedGamesModalRef;
    private Game Game { get; set; } = new();
    IJSObjectReference? _storageModule;

    // Radio bindings (map bool <-> string for Blazor.Bootstrap RadioInput)
    private bool IsAmericano
    {
        get => Game.TournamentType == TournamentType.Americano; set
        {
            if (value)
            {
                Game.TournamentType = TournamentType.Americano;
            }
        }
    }
    private bool IsMexicano
    {
        get => Game.TournamentType == TournamentType.Mexicano; set
        {
            if (value)
            {
                Game.TournamentType = TournamentType.Mexicano;
            }
        }
    }

    private bool IsPointsThenWins
    {
        get => Game.ResultSorting == ResultSorting.PointsOverWins; set
        {
            if (value)
            {
                Game.ResultSorting = ResultSorting.PointsOverWins;
            }
        }
    }
    private bool IsWinsThenPoints
    {
        get => Game.ResultSorting == ResultSorting.WinsOverPoints; set
        {
            if (value)
            {
                Game.ResultSorting = ResultSorting.WinsOverPoints;
            }
        }
    }

    private bool IsPlayPointsOrGames
    {
        get => Game.ScoringType == ScoringType.GamesAndSets; set
        {
            if (value)
            {
                Game.ScoringType = ScoringType.GamesAndSets;
            }
        }
    }
    private bool IsPlayToPoints
    {
        get => Game.ScoringType == ScoringType.Points; set
        {
            if (value)
            {
                Game.ScoringType = ScoringType.Points;
            }
        }
    }

    private bool IsIndividual
    {
        get => Game.TeamFormat == TeamFormat.Individual; set
        {
            if (value)
            {
                Game.TeamFormat = TeamFormat.Individual;
            }
        }
    }
    private bool IsTeam
    {
        get => Game.TeamFormat == TeamFormat.Pairs; set
        {
            if (value)
            {
                Game.TeamFormat = TeamFormat.Pairs;
            }
        }
    }

    private string? NewCourtName { get; set; }
    private string? NewPlayerName { get; set; }

    bool ShowMatches => Game.Matches.Any();

    private static readonly Random _rng = new();
    private readonly Queue<Player> _skipQueue = new();

    async Task ShowMatchHistory()
    {
        if (SavedGamesModalRef is not null)
        {
            await SavedGamesModalRef.ShowAsync();
        }
    }

    void LoadGameFromHistory(SavedGame saved)
    {
        Game.Id = saved.Id;
        Game.TournamentType = saved.TournamentType;
        Game.ResultSorting = saved.ResultSorting;
        Game.TeamFormat = saved.TeamFormat;
        Game.FinalPairingOption = saved.FinalPairingOption;
        Game.ScoringType = saved.ScoringType;
        Game.ToPoints = saved.ToPoints;
        Game.IsFinalRound = saved.IsFinalRound;
        Game.FinalRound = saved.FinalRound;

        // Build new players
        Game.Players = saved.Players
            .Select(p => new Player(p.Name, p.Order))
            .ToList();

        // Build player lookup for consistent reference
        var lookup = Game.Players.ToDictionary(p => p.Name, p => p);

        // Fix match history references
        foreach (var player in saved.Players)
        {
            if (lookup.TryGetValue(player.Name, out var newPlayer))
            {
                newPlayer.MatchHistory = new();
                foreach (var match in player.MatchHistory)
                {
                    var fixedMatch = new Match
                    {
                        CourtName = match.CourtName,
                        Round = match.Round,
                        Team1Points = match.Team1Points,
                        Team2Points = match.Team2Points,
                        Team1 = match.Team1.Select(x => lookup[x.Name]).ToList(),
                        Team2 = match.Team2.Select(x => lookup[x.Name]).ToList()
                    };
                    newPlayer.MatchHistory.Add(fixedMatch);
                }
            }
        }

        // Fix all matches to point to the right Player instances
        Game.Matches = saved.Matches.Select(m => new Match
        {
            Round = m.Round,
            CourtName = m.CourtName,
            Team1Points = m.Team1Points,
            Team2Points = m.Team2Points,
            Team1 = m.Team1.Select(p => lookup[p.Name]).ToList(),
            Team2 = m.Team2.Select(p => lookup[p.Name]).ToList()
        }).ToList();

        Game.Courts = saved.Courts.ToList();
        Game.CurrentRound = Game.Matches.Any() ? Game.Matches.Max(m => m.Round) : 1;

        ReindexPlayers();
    }

    private bool NameExists(string proposed, Player? exclude = null) =>
        Game.Players.Any(x =>
            !ReferenceEquals(x, exclude) &&
            string.Equals(x.Name, proposed, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await EnsureStorageAsync();
        Game.Players = [new Player("Dominik P", 1), new Player("Hajzen", 2), new Player("Hubert", 3), new Player("Pan Krzysztof", 4),];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadConfigAsync(); // silently load if available
            StateHasChanged();
        }
    }

    void AddCourt()
    {
        var name = (NewCourtName ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            Game.Courts.Add(new Court(name, Game.Courts.Count + 1));
            NewCourtName = string.Empty;
        }
        _ = SaveConfigAsync();
    }

    void RemoveCourtByOrder(int order)
    {
        var found = Game.Courts.FirstOrDefault(x => x.Order == order);
        if (found is not null)
        {
            Game.Courts.Remove(found);
        }
        ReindexCourts(order);
        _ = SaveConfigAsync();
    }

    void EditCourtByOrder(int order)
    {
        var c = Game.Courts.FirstOrDefault(x => x.Order == order);
        if (c is null)
        {
            return;
        }
        c.EditName = c.Name;
        c.IsEditing = true;
        _ = SaveConfigAsync();
    }

    void SaveCourt(int order)
    {
        var c = Game.Courts.FirstOrDefault(x => x.Order == order);
        if (c is null)
        {
            return;
        }
        var newName = c.EditName?.Trim() ?? c.Name;
        if (!string.IsNullOrWhiteSpace(newName))
        {
            c.Name = newName;
        }

        c.IsEditing = false;
        c.EditName = null;
        ReindexPlayers();
        _ = SaveConfigAsync();
    }

    void CancelEditCourt(int order)
    {
        var c = Game.Courts.FirstOrDefault(x => x.Order == order);
        if (c is null)
        {
            return;
        }
        c.IsEditing = false;
        c.EditName = null;
    }

    void ReindexCourts(int order)
    {
        for (var i = order; i < Game.Courts.Count; i++)
        {
            Game.Courts[i].Order = i + 1;
        }
    }

    void AddPlayer()
    {
        var name = (NewPlayerName ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(name) && !NameExists(name))
        {
            Game.Players.Add(new Player(name, Game.Players.Count + 1));
            NewPlayerName = string.Empty;
        }
        ReindexPlayers();
    }

    void EditPlayer(string name)
    {
        var p = Game.Players.FirstOrDefault(x => x.Name == name);
        if (p is null)
        {
            return;
        }
        p.EditName = p.Name;
        p.IsEditing = true;
    }

    void SavePlayer(string name)
    {
        var p = Game.Players.FirstOrDefault(x => x.Name == name);
        if (p is null)
        {
            return;
        }
        var newName = p.EditName?.Trim() ?? p.Name;

        if (!string.IsNullOrWhiteSpace(newName) && !NameExists(newName, p))
        {
            p.Name = newName;
            p.IsEditing = false;
            p.EditName = null;
            ReindexPlayers();
        }
    }

    void CancelEditPlayer(string name)
    {
        var p = Game.Players.FirstOrDefault(x => x.Name == name);
        if (p is null)
        {
            return;
        }
        p.IsEditing = false;
        p.EditName = null;
    }

    void RemovePlayerById(string name)
    {
        var found = Game.Players.FirstOrDefault(x => x.Name == name);
        if (found is not null)
        {
            Game.Players.Remove(found);
        }

        ReindexPlayers();
    }

    void ReindexPlayers()
    {
        for (var i = 0; i < Game.Players.Count; i++)
        {
            Game.Players[i].Order = i + 1;
        }
    }

    void OnEnterUp(KeyboardEventArgs e, Action action)
    {
        if (e.Key == "Enter")
        {
            action();
        }
    }

    async Task NextRoundAsync()
    {
        if (Game.CurrentRound == 0)
        {
            _ = SaveConfigAsync();
        }
        Game.CurrentRound++;
        Game.FinalRound = 0;
        Game.IsFinalRound = false;
        // If there are current matches, require completion and commit them first
        if (Game.Matches.Count > 0)
        {
            if (!AllScoresFilled())
            {
                return; // guard: shouldn't be clickable, but double-protect
            }

            CommitCurrentRound();
        }

        // Generate the next round (keep your actual Americano/Mexicano logic here)
        GenerateNextRoundMatches();
        await SaveCurrentGameToHistoryAsync();

        StateHasChanged();
    }

    void GenerateNextRoundMatches()
    {
        // If matches already exist for this round and mode is not Mexicano, reuse
        if (Game.Matches.Any(m => m.Round == Game.CurrentRound) &&
            Game.TournamentType != TournamentType.Mexicano)
        {
            return;
        }
        Game.Matches.RemoveAll(m => m.Round == Game.CurrentRound);
        foreach (var p in Game.Players)
        {
            p.MatchHistory.RemoveAll(m => m.Round == Game.CurrentRound);
        }

        foreach (var p in Game.Players)
        {
            p.MatchHistory.RemoveAll(m => m.Round == Game.CurrentRound);
        }

        // Optional guard: finish latest round
        var latestRound = Game.Matches.Any() ? Game.Matches.Max(m => m.Round) : 0;
        var latestMatches = Game.Matches.Where(m => m.Round == latestRound).ToList();
        if (latestRound > 0 && latestMatches.Any() && latestMatches.Any(m => m.Team1Points + m.Team2Points == 0))
        {
            return;
        }

        // --- Round-robin skipper selection ---
        EnsureSkipQueueUpToDate();
        var cap = ComputeCapacity();
        if (cap.matches == 0)
        {
            return;
        }

        var skippers = TakeSkippers(cap.skipping);
        var selected = Game.Players.Where(p => !skippers.Contains(p)).ToList();

        var mexicanoByScore = Game.TournamentType == TournamentType.Mexicano && Game.CurrentRound > 3;

        // === Pair the selected players ===
        if (!mexicanoByScore)
        {
            // Americano (always random) or early Mexicano
            var pool = selected.OrderBy(_ => _rng.Next()).ToList();
            var p = 0;
            for (var c = 0; c < cap.matches; c++)
            {
                var m = new Match { Round = Game.CurrentRound, CourtName = Game.Courts[c].Name };

                if (Game.TeamFormat == TeamFormat.Pairs) // singles 1v1
                {
                    m.Team1.Add(pool[p++]);
                    m.Team2.Add(pool[p++]);
                }
                else // Individual -> doubles 2v2
                {
                    m.Team1.Add(pool[p++]); m.Team1.Add(pool[p++]);
                    m.Team2.Add(pool[p++]); m.Team2.Add(pool[p++]);
                }

                Game.Matches.Add(m);
            }
        }
        else
        {
            // Mexicano after 3 rounds: similar-level groups
            var ranked = selected
                .OrderByDescending(p => p.Points)
                .ThenByDescending(p => p.Wins)
                .ThenBy(p => p.Order)
                .ThenBy(p => p.Name)
                .ToList();

            var idx = 0;
            for (var c = 0; c < cap.matches; c++)
            {
                var m = new Match { Round = Game.CurrentRound, CourtName = Game.Courts[c].Name };

                if (Game.TeamFormat == TeamFormat.Pairs) // singles 1v1: neighbor pairs
                {
                    var p1 = ranked[idx++]; var p2 = ranked[idx++];
                    m.Team1.Add(p1);
                    m.Team2.Add(p2);
                }
                else // Individual -> doubles 2v2: quartets
                {
                    var p1 = ranked[idx + 0];
                    var p2 = ranked[idx + 1];
                    var p3 = ranked[idx + 2];
                    var p4 = ranked[idx + 3];
                    idx += 4;

                    var option14_23 = _rng.Next(2) == 0;
                    var left = option14_23 ? [p1, p4] : new[] { p1, p3 };
                    var right = option14_23 ? [p2, p3] : new[] { p2, p4 };

                    m.Team1.AddRange(left);
                    m.Team2.AddRange(right);
                }

                Game.Matches.Add(m);
            }
        }

        AwardSkipPointsAtCreation(Game.CurrentRound, skippers);
    }

    void CommitCurrentRound()
    {
        if (!Game.Matches.Any())
        {
            return;
        }

        var round = Game.Matches.Max(m => m.Round);
        var matchesThisRound = Game.Matches.Where(m => m.Round == round).ToList();

        // Regular per-match updates
        foreach (var m in matchesThisRound)
        {
            foreach (var p in m.Team1) { p.MatchHistory.Add(m); }
            foreach (var p in m.Team2) { p.MatchHistory.Add(m); }
        }
    }

    void AwardSkipPointsAtCreation(int round, IEnumerable<Player> skippers)
    {
        var skipPoints = (Game.ToPoints / 2) + 1;

        foreach (var p in skippers)
        {
            // Add a BYE match so history/leaderboard pick it up
            var bye = new Match
            {
                Round = round,
                CourtName = "BYE",
                Team1Points = skipPoints,
                Team2Points = 0,
                IsSkipper = true
            };
            bye.Team1.Add(p);           // Team2 left empty on purpose

            // Persist to both history and global matches
            p.MatchHistory.Add(bye);
            Game.Matches.Add(bye);
        }
    }

    private void OnScorePicked(ScorePicker.ScorePickArgs e)
    {
        var max = Math.Max(0, Game.ToPoints);
        var other = max - e.Selected;

        if (e.Team == 1)
        {
            e.Match.Team1Points = e.Selected;
            e.Match.Team2Points = other;
        }
        else
        {
            e.Match.Team2Points = e.Selected;
            e.Match.Team1Points = other;
        }

        StateHasChanged();
    }

    private async Task OpenHistory() => await _historyModal.ShowAsync();

    async Task ExitToStartAsync()
    {
        await SaveCurrentGameToHistoryAsync();
        Game = new Game();
        await LoadConfigAsync();
        StateHasChanged();
    }

    async Task FinalRoundAsync()
    {
        // Optional: mark round name/number
        Game.CurrentRound++;
        Game.IsFinalRound = true;
        // Commit the current visible round (push to histories & update stats)
        if (Game.Matches.Count > 0)
        {
            if (!AllScoresFilled())
            {
                return; // safety
            }

            CommitCurrentRound();
        }

        // Build the final pairing(s)
        GenerateFinalRoundMatches();
        await SaveCurrentGameToHistoryAsync();

        StateHasChanged();
    }

    private readonly Dictionary<Player, int> _finalPositions = new();

    private string NameFor(Match m, Player p)
    {
        if (Game.FinalRound == 0 || m.Round != Game.FinalRound)
        {
            return p.Name;
        }

        var ranking = Game.ComputeFinalRanking();
        var row = ranking.FirstOrDefault(r => ReferenceEquals(r.Player, p))
                  ?? ranking.FirstOrDefault(r => r.Player.Name == p.Name);

        return row is not null ? $"#{row.Position} {p.Name}" : p.Name;
    }

    private string NameFor(Player p)
    {
        if (Game.FinalRound == 0)
        {
            return p.Name;
        }

        var ranking = Game.ComputeFinalRanking();
        var row = ranking.FirstOrDefault(r => ReferenceEquals(r.Player, p))
                  ?? ranking.FirstOrDefault(r => r.Player.Name == p.Name);

        return row is not null ? $"#{row.Position} {p.Name}" : p.Name;
    }


    void GenerateFinalRoundMatches()
    {
        Game.Matches.RemoveAll(m => m.Round == Game.CurrentRound);
        foreach (var p in Game.Players)
        {
            p.MatchHistory.RemoveAll(m => m.Round == Game.CurrentRound);
        }

        var latestRound = Game.Matches.Any() ? Game.Matches.Max(m => m.Round) : 0;
        var latestRoundMatches = Game.Matches.Where(m => m.Round == latestRound).ToList();
        if (latestRound > 0 && latestRoundMatches.Any() && latestRoundMatches.Any(m => m.Team1Points + m.Team2Points == 0))
        {
            return;
        }

        var ranked = OrderPlayersForFinal(Game.Players).ToList();
        if (ranked.Count < 4)
        {
            return;
        }

        // Build final matches (your existing logic)
        var finalMatches = new List<Match>();

        if (Game.TeamFormat == TeamFormat.Individual)
        {
            var block = 0;
            for (var c = 0; c < Game.Courts.Count; c++)
            {
                var i = block * 4;
                if (i + 3 >= ranked.Count)
                {
                    break;
                }

                var (t1, t2) = Game.FinalPairingOption switch
                {
                    FinalPairingOption.OneTwoVsThreeFour => ([ranked[0 + i], ranked[1 + i]], [ranked[2 + i], ranked[3 + i]]),
                    FinalPairingOption.OneFourVsTwoThree => ([ranked[0 + i], ranked[3 + i]], [ranked[1 + i], ranked[2 + i]]),
                    FinalPairingOption.OneThreeVsTwoFour => ([ranked[0 + i], ranked[2 + i]], [ranked[1 + i], ranked[3 + i]]),
                    _ => (new[] { ranked[0 + i], ranked[1 + i] }, new[] { ranked[2 + i], ranked[3 + i] })
                };

                var m = new Match { Round = Game.CurrentRound, CourtName = Game.Courts[c].Name };
                m.Team1.AddRange(t1);
                m.Team2.AddRange(t2);
                finalMatches.Add(m);
                block++;
            }
        }
        else // TeamFormat.Pairs -> doubles blocks: (1&2) vs (3&4), (5&6) vs (7&8), ...
        {
            var block = 0;
            for (var c = 0; c < Game.Courts.Count; c++)
            {
                var i = block * 2;
                if (i + 1 >= ranked.Count)
                {
                    break;
                }

                var m = new Match { Round = Game.CurrentRound, CourtName = Game.Courts[c].Name };
                m.Team1.Add(ranked[i + 0]);
                m.Team2.Add(ranked[i + 1]);
                finalMatches.Add(m);
                block++;
            }
        }

        // Append the new matches
        foreach (var m in finalMatches)
        {
            Game.Matches.Add(m);
        }

        // === Build final positions ===
        _finalPositions.Clear();
        Game.FinalRound = Game.CurrentRound;

        // Players actually playing in the finals, in ranked order
        var finalists = finalMatches
            .SelectMany(m => m.Team1.Concat(m.Team2))
            .Distinct()
            .OrderBy(p => ranked.IndexOf(p))
            .ToList();

        // Players not in finals (skipping) – also keep their ranked order, but list them AFTER finalists
        var skippers = ranked.Where(p => !finalists.Contains(p)).ToList();

        // Assign positions: finalists first, then skippers
        var pos = 1;
        foreach (var p in finalists)
        {
            _finalPositions[p] = pos++;
        }

        foreach (var p in skippers)
        {
            _finalPositions[p] = pos++;
        }

        AwardSkipPointsAtCreation(Game.CurrentRound, skippers);
    }

    const string StorageKey = "saved-games";

    async Task<List<SavedGame>> LoadSavedGamesAsync()
    {
        var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
        return string.IsNullOrWhiteSpace(json)
            ? new()
            : JsonSerializer.Deserialize<List<SavedGame>>(json) ?? new();
    }

    async Task SaveCurrentGameToHistoryAsync()
    {
        var saved = await LoadSavedGamesAsync();

        var existing = saved.FirstOrDefault(g => g.Id == Game.Id);
        var now = DateTime.Now;

        var newSave = new SavedGame
        {
            Id = Game.Id,
            Name = $"{Game.TournamentType} {now:yyyy-MM-dd HH:mm}",
            SavedAt = now,
            TournamentType = Game.TournamentType,
            ResultSorting = Game.ResultSorting,
            TeamFormat = Game.TeamFormat,
            FinalPairingOption = Game.FinalPairingOption,
            ScoringType = Game.ScoringType,
            ToPoints = Game.ToPoints,
            Players = Game.Players.Select(p => new Player(p.Name, p.Order)
            {
                MatchHistory = [.. p.MatchHistory]
            }).ToList(),
            Matches = Game.Matches.ToList(),
            Courts = Game.Courts.ToList(),
            IsFinalRound = Game.IsFinalRound,
            FinalRound = Game.FinalRound
        };

        if (existing != null)
        {
            var index = saved.IndexOf(existing);
            saved[index] = newSave;
        }
        else
        {
            saved.Add(newSave);
        }

        var json = JsonSerializer.Serialize(saved);
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    IEnumerable<Player> OrderPlayersForFinal(IEnumerable<Player> players)
    {
        return Game.ResultSorting switch
        {
            ResultSorting.WinsOverPoints =>
                players.OrderByDescending(p => p.Wins)
                    .ThenByDescending(p => p.Points)
                    .ThenBy(p => p.Order)
                    .ThenBy(p => p.Name),

            ResultSorting.PointsOverWins =>
                players.OrderByDescending(p => p.Points)
                    .ThenByDescending(p => p.Wins)
                    .ThenBy(p => p.Order)
                    .ThenBy(p => p.Name),

            _ =>
                players.OrderByDescending(p => p.Points)
                    .ThenByDescending(p => p.Wins)
                    .ThenBy(p => p.Order)
                    .ThenBy(p => p.Name)
        };
    }

    bool AllScoresFilled()
        => Game.Matches.All(m => (m.Team1Points + m.Team2Points) > 0);

    IEnumerable<Match> CurrentRoundMatches()
        => Game.Matches.Where(m => m.Round == Game.CurrentRound);

    bool AllScoresFilledThisRound()
        => CurrentRoundMatches().Any() &&
           CurrentRoundMatches().All(m => (m.Team1Points + m.Team2Points) > 0);

    bool BaseReady()
        => Game.Courts.Count >= 1 && Game.Players.Count >= (Game.TeamFormat == TeamFormat.Pairs ? 2 : 4);

    bool CanGoNext
        => BaseReady() && (!CurrentRoundMatches().Any() || AllScoresFilledThisRound());

    bool CanGoFinal
        => BaseReady() && (!CurrentRoundMatches().Any() || AllScoresFilledThisRound()) && Game.CurrentRound > 0;


    async Task EnsureStorageAsync()
        => _storageModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./appStorage.js");

    async Task SaveConfigAsync()
    {
        await EnsureStorageAsync();
        var dto = GameConfigDto.FromGame(Game);
        var json = JsonSerializer.Serialize(dto);
        await _storageModule.InvokeVoidAsync("set", GameConfigDto.GameConfigKey, json);
    }

    async Task<bool> LoadConfigAsync()
    {
        await EnsureStorageAsync();
        var json = await _storageModule.InvokeAsync<string?>("get", GameConfigDto.GameConfigKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<GameConfigDto>(json);
            if (dto is null)
            {
                return false;
            }

            dto.ApplyTo(Game);

            // If you show the current round view, you might keep Game.CurrentRound; do not touch Matches/Players
            StateHasChanged();
            return true;
        }
        catch { return false; }
    }

    async Task ClearConfigAsync()
    {
        await EnsureStorageAsync();
        await _storageModule.InvokeVoidAsync("remove", GameConfigDto.GameConfigKey);
    }

    private IReadOnlyList<Player> SkippingPlayers => GetSkippingPlayers(Game.CurrentRound);

    private IReadOnlyList<Player> GetSkippingPlayers(int round)
    {
        var inRound = Game.Matches
            .Where(m => m.Round == round)
            .SelectMany(m => m.Team1.Concat(m.Team2))
            .ToHashSet();

        return Game.Players
            .Where(p => !inRound.Contains(p))
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Name)
            .ToList();
    }

    string CourtGridClasses(int count) => count switch
    {
        4 => "row-cols-1 row-cols-md-2",   // 2 by 2 on md+ screens
        6 => "row-cols-1 row-cols-lg-3",   // 3 per row on lg+ screens
        _ => "row-cols-1 row-cols-md-2 row-cols-xl-3" // default responsive
    };

    void EnsureSkipQueueUpToDate()
    {
        // Keep existing order for players that still exist, append any new players deterministically
        var current = Game.Players.ToList();

        // If counts match and all queue members still present, assume OK
        if (_skipQueue.Count == current.Count && _skipQueue.All(p => current.Contains(p)))
        {
            return;
        }

        // Rebuild: start with players that are currently in the queue (preserve order), keep only those still present
        var preserved = _skipQueue.Where(current.Contains).ToList();
        _skipQueue.Clear();
        foreach (var p in preserved)
        {
            _skipQueue.Enqueue(p);
        }

        // Append missing players by stable order (Order, Name)
        foreach (var p in current
            .Except(preserved)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Name))
        {
            _skipQueue.Enqueue(p);
        }

        // If queue ended empty (first run), seed it
        if (_skipQueue.Count == 0)
        {
            foreach (var p in current.OrderBy(p => p.Order).ThenBy(p => p.Name))
            {
                _skipQueue.Enqueue(p);
            }
        }
    }

    int PlayersPerMatch() => Game.TeamFormat == TeamFormat.Pairs ? 2 : 4;

    (int matches, int playing, int skipping) ComputeCapacity()
    {
        var courts = Game.Courts.Count;
        var n = Game.Players.Count;
        var ppm = PlayersPerMatch();

        if (courts <= 0 || n < ppm)
        {
            return (0, 0, n);
        }

        var m = Math.Min(courts, n / ppm);
        var playing = m * ppm;
        var skipping = Math.Max(0, n - playing);
        return (m, playing, skipping);
    }

    // Dequeue `count` skippers (round-robin) and rotate them to the back.
    // Guarantees all skippers are members of Game.Players.
    List<Player> TakeSkippers(int count)
    {
        EnsureSkipQueueUpToDate();

        var present = Game.Players.ToHashSet();
        var picked = new List<Player>(Math.Max(0, count));

        // We limit iterations to safety bound
        var safety = _skipQueue.Count * 2;
        while (picked.Count < count && safety-- > 0 && _skipQueue.Count > 0)
        {
            var p = _skipQueue.Dequeue();
            if (present.Contains(p))
            {
                picked.Add(p);
                _skipQueue.Enqueue(p); // rotate to the back after being picked to skip
            }
            else
            {
                // If player is no longer present, don't re-enqueue
            }
        }

        // If still short (added players just now), rebuild and retry once
        if (picked.Count < count)
        {
            EnsureSkipQueueUpToDate();
            while (picked.Count < count && _skipQueue.Count > 0)
            {
                var p = _skipQueue.Dequeue();
                picked.Add(p);
                _skipQueue.Enqueue(p);
            }
        }

        return picked;
    }

    bool CanGoBack => Game.CurrentRound > 1 && Game.CurrentRound == Game.Matches.Max(m => m.Round);

    void GoBackOneRound()
    {
        if (!CanGoBack)
        {
            return;
        }

        var round = Game.CurrentRound;

        foreach (var p in Game.Players)
        {
            p.MatchHistory.RemoveAll(m => m.Round == round);
        }

        Game.CurrentRound--;
    }
}
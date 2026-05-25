static class GameOps
{
    const int TableWidth = 83;

    static void TableHeader()
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(UI.P + $"  {"ID",-5} {"Title",-24} {"Genre",-14} {"Engine",-14} {"Status",-14} {"Year",-5}");
        Console.ResetColor();
        UI.Sep('─', TableWidth);
    }

    static void TableRow(Game g)
    {
        Console.Write(UI.P + $"  {g.Id,-5} {g.Title,-24} {g.Genre,-14} {g.Engine,-14} ");
        UI.WriteStatus(g.Status.PadRight(14));
        Console.WriteLine($" {g.Year,-5}");
    }

    static void PrintTable(List<Game> games)
    {
        if (games.Count == 0) { UI.Info("  No records found."); return; }
        TableHeader();
        foreach (var g in games) TableRow(g);
        Console.WriteLine();
        UI.Info($"  Total: {games.Count}");
    }

    public static void Add()
    {
        Console.Clear();
        UI.Header("Add Game");
        var games = Db.LoadGames();
        var g = new Game
        {
            Id     = Db.NextGameId(),
            Title  = UI.AskNonEmpty("  Title:   "),
            Genre  = UI.AskNonEmpty("  Genre:   "),
            Engine = UI.AskNonEmpty("  Engine:  "),
            Status = UI.AskNonEmpty("  Status:  "),
            Year   = UI.AskInt     ("  Year:    ")
        };
        games.Add(g);
        Db.SaveGames(games);
        UI.Success($"\n  Game #{g.Id} \"{g.Title}\" added.");
        UI.Pause();
    }

    public static void ViewAll()
    {
        Console.Clear();
        UI.Header("All Games");
        PrintTable(Db.LoadGames());
        UI.Pause();
    }

    public static void ViewById()
    {
        Console.Clear();
        UI.Header("Game by ID");
        var id  = UI.AskInt("  ID: ");
        var idx = Db.LoadGames().FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Game not found."); UI.Pause(); return; }

        var g = Db.LoadGames()[idx];
        UI.Sep('─', TableWidth);
        Console.WriteLine(UI.P + $"  ID:       {g.Id}");
        Console.WriteLine(UI.P + $"  Title:    {g.Title}");
        Console.WriteLine(UI.P + $"  Genre:    {g.Genre}");
        Console.WriteLine(UI.P + $"  Engine:   {g.Engine}");
        Console.Write    (UI.P +  "  Status:   ");
        UI.WriteStatus(g.Status);
        Console.WriteLine();
        Console.WriteLine(UI.P + $"  Year:     {g.Year}");
        UI.Sep('─', TableWidth);
        UI.Pause();
    }

    // Delete is always cascade — orphaned assets and builds serve no purpose
    public static void DeleteById()
    {
        Console.Clear();
        UI.Header("Delete Game");
        var id    = UI.AskInt("  Game ID: ");
        var games = Db.LoadGames();
        var idx   = games.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Game not found."); UI.Pause(); return; }

        var assets = Db.LoadAssets();
        var builds = Db.LoadBuilds();
        int aCount = assets.Count(a => a.GameId == id);
        int bCount = builds.Count(b => b.GameId == id);
        var title  = games[idx].Title;

        Console.WriteLine();
        Console.WriteLine(UI.P + $"  Game:     \"{title}\"");
        Console.WriteLine(UI.P + $"  Assets:   {aCount}");
        Console.WriteLine(UI.P + $"  Builds:   {bCount}");
        Console.WriteLine();
        UI.Error("  This will delete the game and all linked assets and builds!");
        var confirm = UI.Ask("  Confirm? (yes/no): ").Trim().ToLower();
        if (confirm != "yes")
        {
            UI.Info("  Cancelled.");
            UI.Pause();
            return;
        }

        games.RemoveAt(idx);
        assets.RemoveAll(a => a.GameId == id);
        builds.RemoveAll(b => b.GameId == id);
        Db.RenumberGames(games, assets, builds);
        Db.RenumberAssets(assets);
        Db.RenumberBuilds(builds);
        Db.SaveGames(games);
        Db.SaveAssets(assets);
        Db.SaveBuilds(builds);

        UI.Success($"\n  Deleted: 1 game, {aCount} asset(s), {bCount} build(s). IDs renumbered.");
        UI.Pause();
    }

    public static void Edit()
    {
        Console.Clear();
        UI.Header("Edit Game");
        var id    = UI.AskInt("  Game ID: ");
        var games = Db.LoadGames();
        var idx   = games.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Game not found."); UI.Pause(); return; }

        var g = games[idx];
        Console.WriteLine();
        UI.Info("  Leave blank and press Enter to keep the current value.");
        Console.WriteLine();

        g.Title  = UI.AskOrKeep    ("  Title:   ", g.Title);
        g.Genre  = UI.AskOrKeep    ("  Genre:   ", g.Genre);
        g.Engine = UI.AskOrKeep    ("  Engine:  ", g.Engine);
        g.Status = UI.AskOrKeep    ("  Status:  ", g.Status);
        g.Year   = UI.AskIntOrKeep ("  Year:    ", g.Year);

        games[idx] = g;
        Db.SaveGames(games);
        UI.Success($"\n  Game #{g.Id} \"{g.Title}\" updated.");
        UI.Pause();
    }

    public static void Search()
    {
        while (true)
        {
            var choice = UI.Menu("Search Games", new[]
            {
                "By genre",
                "By engine",
                "By status",
                "By year (range)"
            });
            if (choice == 0) return;

            Console.Clear();
            var games = Db.LoadGames();
            List<Game> result;

            switch (choice)
            {
                case 1:
                    var genre = UI.AskNonEmpty("  Genre (substring): ");
                    result = games.FindAll(g => g.Genre.Contains(genre, StringComparison.OrdinalIgnoreCase));
                    break;
                case 2:
                    var engine = UI.AskNonEmpty("  Engine (substring): ");
                    result = games.FindAll(g => g.Engine.Contains(engine, StringComparison.OrdinalIgnoreCase));
                    break;
                case 3:
                    var status = UI.AskNonEmpty("  Status (substring): ");
                    result = games.FindAll(g => g.Status.Contains(status, StringComparison.OrdinalIgnoreCase));
                    break;
                case 4:
                    int from = UI.AskInt("  Year from: ");
                    int to   = UI.AskInt("  Year to:   ");
                    result = games.FindAll(g => g.Year >= from && g.Year <= to);
                    break;
                default: return;
            }

            Console.Clear();
            UI.Header($"Search Results ({result.Count})");
            PrintTable(result);
            UI.Pause();
        }
    }

    public static void Stats()
    {
        Console.Clear();
        UI.Header("Game Statistics");
        var games = Db.LoadGames();
        if (games.Count == 0) { UI.Info("  No data."); UI.Pause(); return; }

        Console.WriteLine(UI.P + $"  Total games: {games.Count}");
        Console.WriteLine();

        Console.WriteLine(UI.P + "  By genre:");
        foreach (var grp in games.GroupBy(g => g.Genre).OrderByDescending(g => g.Count()))
            Console.WriteLine(UI.P + $"    {grp.Key,-24} {grp.Count()} title(s)");
        Console.WriteLine();

        Console.WriteLine(UI.P + "  By status:");
        foreach (var grp in games.GroupBy(g => g.Status).OrderByDescending(g => g.Count()))
        {
            Console.Write(UI.P + "    ");
            UI.WriteStatus(grp.Key.PadRight(24));
            Console.WriteLine($" {grp.Count()} title(s)");
        }
        Console.WriteLine();

        var wy = games.Where(g => g.Year > 0).ToList();
        if (wy.Count > 0)
        {
            Console.WriteLine(UI.P + $"  Average year: {wy.Average(g => g.Year):F0}");
            Console.WriteLine(UI.P + $"  Range:        {wy.Min(g => g.Year)} – {wy.Max(g => g.Year)}");
        }
        UI.Pause();
    }

    public static void Sort()
    {
        var choice = UI.Menu("Sort Games", new[]
        {
            "By title A → Z",
            "By title Z → A",
            "By year (ascending)",
            "By year (descending)"
        });
        if (choice == 0) return;

        var games = Db.LoadGames();
        List<Game> sorted = choice switch
        {
            1 => games.OrderBy(g => g.Title).ToList(),
            2 => games.OrderByDescending(g => g.Title).ToList(),
            3 => games.OrderBy(g => g.Year).ToList(),
            4 => games.OrderByDescending(g => g.Year).ToList(),
            _ => games
        };
        Console.Clear();
        UI.Header("Games (sorted)");
        PrintTable(sorted);
        UI.Pause();
    }

    public static void RunMenu()
    {
        while (true)
        {
            var choice = UI.Menu("Games", new[]
            {
                "Add game",
                "View all games",
                "View game by ID",
                "Edit game",
                "Delete game",
                "Search / Filter",
                "Statistics",
                "Sort"
            });
            switch (choice)
            {
                case 0: return;
                case 1: Add(); break;
                case 2: ViewAll(); break;
                case 3: ViewById(); break;
                case 4: Edit(); break;
                case 5: DeleteById(); break;
                case 6: Search(); break;
                case 7: Stats(); break;
                case 8: Sort(); break;
            }
        }
    }
}

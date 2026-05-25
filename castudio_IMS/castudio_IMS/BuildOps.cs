static class BuildOps
{
    const int TableWidth = 76;

    static int AskDate(string prompt)
    {
        while (true)
        {
            var s = UI.Ask(prompt);
            if (DateTime.TryParse(s, out var d))
                return d.Year * 10000 + d.Month * 100 + d.Day;
            UI.Error("  Invalid format. Use YYYY-MM-DD.");
        }
    }

    static string FmtDate(int d)
        => d == 0 ? "—" : $"{d / 10000:D4}-{d / 100 % 100:D2}-{d % 100:D2}";

    static void TableHeader()
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(UI.P + $"  {"ID",-5} {"Game",-5} {"Version",-12} {"Platform",-12} {"Status",-12} {"Date",-12}");
        Console.ResetColor();
        UI.Sep('─', TableWidth);
    }

    static void TableRow(Build b)
    {
        Console.Write(UI.P + $"  {b.Id,-5} {b.GameId,-5} {b.Version,-12} {b.Platform,-12} ");
        UI.WriteStatus(b.Status.PadRight(12));
        Console.WriteLine($" {FmtDate(b.BuildDate),-12}");
    }

    static void PrintTable(List<Build> builds)
    {
        if (builds.Count == 0) { UI.Info("  No records found."); return; }
        TableHeader();
        foreach (var b in builds) TableRow(b);
        Console.WriteLine();
        UI.Info($"  Total: {builds.Count}");
    }

    public static void Add()
    {
        Console.Clear();
        UI.Header("Add Build");
        var games = Db.LoadGames();
        if (games.Count == 0) { UI.Error("  Add at least one game first."); UI.Pause(); return; }

        var gameId = UI.AskInt("  Game ID:   ");
        if (!games.Exists(g => g.Id == gameId)) { UI.Error("  Game not found."); UI.Pause(); return; }

        var builds = Db.LoadBuilds();
        var b = new Build
        {
            Id        = Db.NextBuildId(),
            GameId    = gameId,
            Version   = UI.AskNonEmpty("  Version:   "),
            Platform  = UI.AskNonEmpty("  Platform:  "),
            Status    = UI.AskNonEmpty("  Status:    "),
            BuildDate = AskDate       ("  Date (YYYY-MM-DD): ")
        };
        builds.Add(b);
        Db.SaveBuilds(builds);
        UI.Success($"\n  Build #{b.Id} v{b.Version} added.");
        UI.Pause();
    }

    public static void ViewAll()
    {
        Console.Clear();
        UI.Header("All Builds");
        PrintTable(Db.LoadBuilds());
        UI.Pause();
    }

    public static void ViewById()
    {
        Console.Clear();
        UI.Header("Build by ID");
        var id  = UI.AskInt("  ID: ");
        var idx = Db.LoadBuilds().FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Build not found."); UI.Pause(); return; }

        var b = Db.LoadBuilds()[idx];
        UI.Sep('─', TableWidth);
        Console.WriteLine(UI.P + $"  ID:        {b.Id}");
        Console.WriteLine(UI.P + $"  Game ID:   {b.GameId}");
        Console.WriteLine(UI.P + $"  Version:   {b.Version}");
        Console.WriteLine(UI.P + $"  Platform:  {b.Platform}");
        Console.Write    (UI.P +  "  Status:    ");
        UI.WriteStatus(b.Status);
        Console.WriteLine();
        Console.WriteLine(UI.P + $"  Date:      {FmtDate(b.BuildDate)}");
        UI.Sep('─', TableWidth);
        UI.Pause();
    }

    public static void DeleteById()
    {
        Console.Clear();
        UI.Header("Delete Build");
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadBuilds();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Build not found."); }
        else
        {
            list.RemoveAt(idx);
            Db.RenumberBuilds(list);
            Db.SaveBuilds(list);
            UI.Success("  Build deleted. IDs renumbered.");
        }
        UI.Pause();
    }

    public static void Search()
    {
        while (true)
        {
            var choice = UI.Menu("Search Builds", new[]
            {
                "By platform",
                "By status",
                "By date (range)",
                "By game ID"
            });
            if (choice == 0) return;

            Console.Clear();
            var builds = Db.LoadBuilds();
            List<Build> result;

            switch (choice)
            {
                case 1:
                    var platform = UI.AskNonEmpty("  Platform (substring): ");
                    result = builds.FindAll(b => b.Platform.Contains(platform, StringComparison.OrdinalIgnoreCase));
                    break;
                case 2:
                    var status = UI.AskNonEmpty("  Status (substring): ");
                    result = builds.FindAll(b => b.Status.Contains(status, StringComparison.OrdinalIgnoreCase));
                    break;
                case 3:
                    int from = AskDate("  Date from (YYYY-MM-DD): ");
                    int to   = AskDate("  Date to   (YYYY-MM-DD): ");
                    result = builds.FindAll(b => b.BuildDate >= from && b.BuildDate <= to);
                    break;
                case 4:
                    var gid = UI.AskInt("  Game ID: ");
                    result = builds.FindAll(b => b.GameId == gid);
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
        UI.Header("Build Statistics");
        var builds = Db.LoadBuilds();
        if (builds.Count == 0) { UI.Info("  No data."); UI.Pause(); return; }

        Console.WriteLine(UI.P + $"  Total builds: {builds.Count}");
        Console.WriteLine();

        Console.WriteLine(UI.P + "  By platform:");
        foreach (var grp in builds.GroupBy(b => b.Platform).OrderByDescending(g => g.Count()))
            Console.WriteLine(UI.P + $"    {grp.Key,-24} {grp.Count()} build(s)");
        Console.WriteLine();

        Console.WriteLine(UI.P + "  By status:");
        foreach (var grp in builds.GroupBy(b => b.Status).OrderByDescending(g => g.Count()))
        {
            Console.Write(UI.P + "    ");
            UI.WriteStatus(grp.Key.PadRight(24));
            Console.WriteLine($" {grp.Count()} build(s)");
        }
        Console.WriteLine();

        var wd = builds.Where(b => b.BuildDate > 0).ToList();
        if (wd.Count > 0)
        {
            Console.WriteLine(UI.P + $"  First build:  {FmtDate(wd.Min(b => b.BuildDate))}");
            Console.WriteLine(UI.P + $"  Latest build: {FmtDate(wd.Max(b => b.BuildDate))}");
        }
        UI.Pause();
    }

    public static void Sort()
    {
        var choice = UI.Menu("Sort Builds", new[]
        {
            "By version A → Z",
            "By version Z → A",
            "By date (ascending)",
            "By date (descending)"
        });
        if (choice == 0) return;

        var builds = Db.LoadBuilds();
        List<Build> sorted = choice switch
        {
            1 => builds.OrderBy(b => b.Version).ToList(),
            2 => builds.OrderByDescending(b => b.Version).ToList(),
            3 => builds.OrderBy(b => b.BuildDate).ToList(),
            4 => builds.OrderByDescending(b => b.BuildDate).ToList(),
            _ => builds
        };
        Console.Clear();
        UI.Header("Builds (sorted)");
        PrintTable(sorted);
        UI.Pause();
    }

    public static void RunMenu()
    {
        while (true)
        {
            var choice = UI.Menu("Builds", new[]
            {
                "Add build",
                "View all builds",
                "View build by ID",
                "Delete by ID",
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
                case 4: DeleteById(); break;
                case 5: Search(); break;
                case 6: Stats(); break;
                case 7: Sort(); break;
            }
        }
    }
}

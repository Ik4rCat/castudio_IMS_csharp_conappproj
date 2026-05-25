static class AssetOps
{
    const int TableWidth = 80;

    static void TableHeader()
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(UI.P + $"  {"ID",-5} {"Game",-5} {"Name",-24} {"Type",-12} {"Author",-14} {"Size KB",-12}");
        Console.ResetColor();
        UI.Sep('─', TableWidth);
    }

    static void TableRow(Asset a)
        => Console.WriteLine(UI.P + $"  {a.Id,-5} {a.GameId,-5} {a.Name,-24} {a.Type,-12} {a.Author,-14} {a.FileSizeKb,-12}");

    static void PrintTable(List<Asset> assets)
    {
        if (assets.Count == 0) { UI.Info("  No records found."); return; }
        TableHeader();
        foreach (var a in assets) TableRow(a);
        Console.WriteLine();
        UI.Info($"  Total: {assets.Count}");
    }

    public static void Add()
    {
        Console.Clear();
        UI.Header("Add Asset");
        var games = Db.LoadGames();
        if (games.Count == 0) { UI.Error("  Add at least one game first."); UI.Pause(); return; }

        var gameId = UI.AskInt("  Game ID:  ");
        if (!games.Exists(g => g.Id == gameId)) { UI.Error("  Game not found."); UI.Pause(); return; }

        var assets = Db.LoadAssets();
        var a = new Asset
        {
            Id         = Db.NextAssetId(),
            GameId     = gameId,
            Name       = UI.AskNonEmpty("  Name:     "),
            Type       = UI.AskNonEmpty("  Type:     "),
            Author     = UI.AskNonEmpty("  Author:   "),
            FileSizeKb = UI.AskLong    ("  Size KB:  ")
        };
        assets.Add(a);
        Db.SaveAssets(assets);
        UI.Success($"\n  Asset #{a.Id} \"{a.Name}\" added.");
        UI.Pause();
    }

    public static void ViewAll()
    {
        Console.Clear();
        UI.Header("All Assets");
        PrintTable(Db.LoadAssets());
        UI.Pause();
    }

    public static void ViewById()
    {
        Console.Clear();
        UI.Header("Asset by ID");
        var id  = UI.AskInt("  ID: ");
        var idx = Db.LoadAssets().FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Asset not found."); UI.Pause(); return; }

        var a = Db.LoadAssets()[idx];
        UI.Sep('─', TableWidth);
        Console.WriteLine(UI.P + $"  ID:       {a.Id}");
        Console.WriteLine(UI.P + $"  Game ID:  {a.GameId}");
        Console.WriteLine(UI.P + $"  Name:     {a.Name}");
        Console.WriteLine(UI.P + $"  Type:     {a.Type}");
        Console.WriteLine(UI.P + $"  Author:   {a.Author}");
        Console.WriteLine(UI.P + $"  Size:     {a.FileSizeKb} KB  ({a.FileSizeKb / 1024.0:F2} MB)");
        UI.Sep('─', TableWidth);
        UI.Pause();
    }

    public static void DeleteById()
    {
        Console.Clear();
        UI.Header("Delete Asset");
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadAssets();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Asset not found."); }
        else
        {
            list.RemoveAt(idx);
            Db.RenumberAssets(list);
            Db.SaveAssets(list);
            UI.Success("  Asset deleted. IDs renumbered.");
        }
        UI.Pause();
    }

    public static void Search()
    {
        while (true)
        {
            var choice = UI.Menu("Search Assets", new[]
            {
                "By type",
                "By author",
                "By size (KB range)",
                "By game ID"
            });
            if (choice == 0) return;

            Console.Clear();
            var assets = Db.LoadAssets();
            List<Asset> result;

            switch (choice)
            {
                case 1:
                    var type = UI.AskNonEmpty("  Type (substring): ");
                    result = assets.FindAll(a => a.Type.Contains(type, StringComparison.OrdinalIgnoreCase));
                    break;
                case 2:
                    var author = UI.AskNonEmpty("  Author (substring): ");
                    result = assets.FindAll(a => a.Author.Contains(author, StringComparison.OrdinalIgnoreCase));
                    break;
                case 3:
                    long from = UI.AskLong("  Size from (KB): ");
                    long to   = UI.AskLong("  Size to   (KB): ");
                    result = assets.FindAll(a => a.FileSizeKb >= from && a.FileSizeKb <= to);
                    break;
                case 4:
                    var gid = UI.AskInt("  Game ID: ");
                    result = assets.FindAll(a => a.GameId == gid);
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
        UI.Header("Asset Statistics");
        var assets = Db.LoadAssets();
        if (assets.Count == 0) { UI.Info("  No data."); UI.Pause(); return; }

        long total = assets.Sum(a => a.FileSizeKb);
        Console.WriteLine(UI.P + $"  Total assets:     {assets.Count}");
        Console.WriteLine(UI.P + $"  Total size:       {total} KB  ({total / 1024.0:F2} MB)");
        Console.WriteLine(UI.P + $"  Average size:     {assets.Average(a => a.FileSizeKb):F0} KB");
        Console.WriteLine(UI.P + $"  Largest:          {assets.Max(a => a.FileSizeKb)} KB");
        Console.WriteLine(UI.P + $"  Smallest:         {assets.Min(a => a.FileSizeKb)} KB");
        Console.WriteLine();
        Console.WriteLine(UI.P + "  By type:");
        foreach (var grp in assets.GroupBy(a => a.Type).OrderByDescending(g => g.Count()))
            Console.WriteLine(UI.P + $"    {grp.Key,-22} {grp.Count()} item(s)   {grp.Sum(a => a.FileSizeKb)} KB");
        UI.Pause();
    }

    public static void Sort()
    {
        var choice = UI.Menu("Sort Assets", new[]
        {
            "By name A → Z",
            "By name Z → A",
            "By size (ascending)",
            "By size (descending)"
        });
        if (choice == 0) return;

        var assets = Db.LoadAssets();
        List<Asset> sorted = choice switch
        {
            1 => assets.OrderBy(a => a.Name).ToList(),
            2 => assets.OrderByDescending(a => a.Name).ToList(),
            3 => assets.OrderBy(a => a.FileSizeKb).ToList(),
            4 => assets.OrderByDescending(a => a.FileSizeKb).ToList(),
            _ => assets
        };
        Console.Clear();
        UI.Header("Assets (sorted)");
        PrintTable(sorted);
        UI.Pause();
    }

    public static void RunMenu()
    {
        while (true)
        {
            var choice = UI.Menu("Assets", new[]
            {
                "Add asset",
                "View all assets",
                "View asset by ID",
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

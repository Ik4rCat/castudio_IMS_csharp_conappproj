using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding  = Encoding.UTF8;
App.Run();

// ========================= STRUCTS =========================

struct Game
{
    public int    Id;
    public string Title;
    public string Genre;
    public string Engine;
    public string Status;
    public int    Year;
}

struct Asset
{
    public int    Id;
    public int    GameId;
    public string Name;
    public string Type;
    public string Author;
    public long   FileSizeKb;
}

struct Build
{
    public int    Id;
    public int    GameId;
    public string Version;
    public string Platform;
    public string Status;
    public int    BuildDate; // YYYYMMDD
}

// ========================= BINARY DB =========================

static class Db
{
    const string GamesFile  = "games.bin";
    const string AssetsFile = "assets.bin";
    const string BuildsFile = "builds.bin";

    static string ReadFixed(BinaryReader r, int size)
        => Encoding.UTF8.GetString(r.ReadBytes(size)).TrimEnd('\0');

    static void WriteFixed(BinaryWriter w, string? s, int size)
    {
        var str = s ?? "";
        while (Encoding.UTF8.GetByteCount(str) > size && str.Length > 0)
            str = str[..^1];
        var buf = new byte[size];
        var src = Encoding.UTF8.GetBytes(str);
        Array.Copy(src, buf, src.Length);
        w.Write(buf);
    }

    // ---- Games ----

    public static List<Game> LoadGames()
    {
        var list = new List<Game>();
        if (!File.Exists(GamesFile)) return list;
        using var r = new BinaryReader(File.OpenRead(GamesFile));
        while (r.BaseStream.Position < r.BaseStream.Length)
            list.Add(new Game {
                Id     = r.ReadInt32(),
                Title  = ReadFixed(r, 100),
                Genre  = ReadFixed(r, 60),
                Engine = ReadFixed(r, 60),
                Status = ReadFixed(r, 40),
                Year   = r.ReadInt32()
            });
        return list;
    }

    public static void SaveGames(List<Game> list)
    {
        using var w = new BinaryWriter(File.Create(GamesFile));
        foreach (var g in list)
        {
            w.Write(g.Id);
            WriteFixed(w, g.Title,  100);
            WriteFixed(w, g.Genre,   60);
            WriteFixed(w, g.Engine,  60);
            WriteFixed(w, g.Status,  40);
            w.Write(g.Year);
        }
    }

    public static int NextGameId()
    {
        var list = LoadGames();
        return list.Count == 0 ? 1 : list.Max(g => g.Id) + 1;
    }

    // ---- Assets ----

    public static List<Asset> LoadAssets()
    {
        var list = new List<Asset>();
        if (!File.Exists(AssetsFile)) return list;
        using var r = new BinaryReader(File.OpenRead(AssetsFile));
        while (r.BaseStream.Position < r.BaseStream.Length)
            list.Add(new Asset {
                Id         = r.ReadInt32(),
                GameId     = r.ReadInt32(),
                Name       = ReadFixed(r, 100),
                Type       = ReadFixed(r, 40),
                Author     = ReadFixed(r, 60),
                FileSizeKb = r.ReadInt64()
            });
        return list;
    }

    public static void SaveAssets(List<Asset> list)
    {
        using var w = new BinaryWriter(File.Create(AssetsFile));
        foreach (var a in list)
        {
            w.Write(a.Id);
            w.Write(a.GameId);
            WriteFixed(w, a.Name,   100);
            WriteFixed(w, a.Type,    40);
            WriteFixed(w, a.Author,  60);
            w.Write(a.FileSizeKb);
        }
    }

    public static int NextAssetId()
    {
        var list = LoadAssets();
        return list.Count == 0 ? 1 : list.Max(a => a.Id) + 1;
    }

    // ---- Builds ----

    public static List<Build> LoadBuilds()
    {
        var list = new List<Build>();
        if (!File.Exists(BuildsFile)) return list;
        using var r = new BinaryReader(File.OpenRead(BuildsFile));
        while (r.BaseStream.Position < r.BaseStream.Length)
            list.Add(new Build {
                Id        = r.ReadInt32(),
                GameId    = r.ReadInt32(),
                Version   = ReadFixed(r, 20),
                Platform  = ReadFixed(r, 40),
                Status    = ReadFixed(r, 40),
                BuildDate = r.ReadInt32()
            });
        return list;
    }

    public static void SaveBuilds(List<Build> list)
    {
        using var w = new BinaryWriter(File.Create(BuildsFile));
        foreach (var b in list)
        {
            w.Write(b.Id);
            w.Write(b.GameId);
            WriteFixed(w, b.Version,  20);
            WriteFixed(w, b.Platform, 40);
            WriteFixed(w, b.Status,   40);
            w.Write(b.BuildDate);
        }
    }

    public static int NextBuildId()
    {
        var list = LoadBuilds();
        return list.Count == 0 ? 1 : list.Max(b => b.Id) + 1;
    }
}

// ========================= UI =========================

static class UI
{
    public static void Pause()
    {
        Console.Write("\nPress any key...");
        Console.ReadKey(true);
        Console.WriteLine();
    }

    public static void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static void Sep(char c = '=', int len = 62)
        => Console.WriteLine(new string(c, len));

    public static void Header(string title)
    {
        Sep();
        Console.WriteLine($"  {title}");
        Sep();
    }

    public static string Ask(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? "";
    }

    public static string AskNonEmpty(string prompt)
    {
        while (true)
        {
            var s = Ask(prompt).Trim();
            if (s.Length > 0) return s;
            Error("  Field cannot be empty.");
        }
    }

    public static int AskInt(string prompt)
    {
        while (true)
        {
            var s = Ask(prompt);
            if (int.TryParse(s, out var v)) return v;
            Error("  Please enter a whole number.");
        }
    }

    public static long AskLong(string prompt)
    {
        while (true)
        {
            var s = Ask(prompt);
            if (long.TryParse(s, out var v) && v >= 0) return v;
            Error("  Please enter a non-negative whole number.");
        }
    }

    public static int Menu(string title, string[] options, string exitLabel = "Back")
    {
        while (true)
        {
            Console.Clear();
            Header(title);
            for (int i = 0; i < options.Length; i++)
                Console.WriteLine($"  {i + 1}. {options[i]}");
            Console.WriteLine($"  0. {exitLabel}");
            Sep('-');
            var s = Ask("  Choice: ");
            if (int.TryParse(s, out var v) && v >= 0 && v <= options.Length)
                return v;
            Error("  Invalid choice.");
            Console.ReadKey(true);
        }
    }
}

// ========================= GAMES =========================

static class GameOps
{
    static void TableHeader()
    {
        Console.WriteLine($"  {"ID",-5} {"Title",-24} {"Genre",-14} {"Engine",-14} {"Status",-14} {"Year",-5}");
        UI.Sep('-');
    }

    static void TableRow(Game g)
        => Console.WriteLine($"  {g.Id,-5} {g.Title,-24} {g.Genre,-14} {g.Engine,-14} {g.Status,-14} {g.Year,-5}");

    static void PrintTable(List<Game> games)
    {
        if (games.Count == 0) { Console.WriteLine("  No records found."); return; }
        TableHeader();
        foreach (var g in games) TableRow(g);
        Console.WriteLine($"\n  Total: {games.Count}");
    }

    public static void Add()
    {
        Console.Clear();
        UI.Header("Add Game");
        var games = Db.LoadGames();
        var g = new Game
        {
            Id     = Db.NextGameId(),
            Title  = UI.AskNonEmpty("  Title:  "),
            Genre  = UI.AskNonEmpty("  Genre:  "),
            Engine = UI.AskNonEmpty("  Engine: "),
            Status = UI.AskNonEmpty("  Status: "),
            Year   = UI.AskInt     ("  Year:   ")
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
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadGames();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0)
        {
            UI.Error("  Game not found.");
        }
        else
        {
            var g = list[idx];
            UI.Sep('-');
            Console.WriteLine($"  ID:     {g.Id}");
            Console.WriteLine($"  Title:  {g.Title}");
            Console.WriteLine($"  Genre:  {g.Genre}");
            Console.WriteLine($"  Engine: {g.Engine}");
            Console.WriteLine($"  Status: {g.Status}");
            Console.WriteLine($"  Year:   {g.Year}");
        }
        UI.Pause();
    }

    public static void DeleteById()
    {
        Console.Clear();
        UI.Header("Delete Game by ID");
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadGames();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Game not found."); }
        else
        {
            list.RemoveAt(idx);
            Db.SaveGames(list);
            UI.Success("  Game deleted.");
        }
        UI.Pause();
    }

    public static void CascadeDelete()
    {
        Console.Clear();
        UI.Header("Cascade Delete Game");
        var id    = UI.AskInt("  Game ID: ");
        var games = Db.LoadGames();
        var idx   = games.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Game not found."); UI.Pause(); return; }

        var title  = games[idx].Title;
        var assets = Db.LoadAssets();
        var builds = Db.LoadBuilds();
        int aCount = assets.Count(a => a.GameId == id);
        int bCount = builds.Count(b => b.GameId == id);

        Console.WriteLine($"\n  Game: \"{title}\"");
        Console.WriteLine($"  Assets to delete: {aCount},  Builds to delete: {bCount}");
        Console.Write("\n  Confirm? (yes/no): ");
        if ((Console.ReadLine() ?? "").Trim().ToLower() != "yes")
        {
            Console.WriteLine("  Cancelled.");
            UI.Pause();
            return;
        }

        games.RemoveAt(idx);
        Db.SaveGames(games);
        assets.RemoveAll(a => a.GameId == id);
        Db.SaveAssets(assets);
        builds.RemoveAll(b => b.GameId == id);
        Db.SaveBuilds(builds);

        UI.Success($"\n  Deleted: 1 game, {aCount} assets, {bCount} builds.");
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
        if (games.Count == 0) { Console.WriteLine("  No data."); UI.Pause(); return; }

        Console.WriteLine($"  Total games: {games.Count}");
        Console.WriteLine();
        Console.WriteLine("  By genre:");
        foreach (var grp in games.GroupBy(g => g.Genre).OrderByDescending(g => g.Count()))
            Console.WriteLine($"    {grp.Key,-24} {grp.Count()} title(s)");
        Console.WriteLine();
        Console.WriteLine("  By status:");
        foreach (var grp in games.GroupBy(g => g.Status).OrderByDescending(g => g.Count()))
            Console.WriteLine($"    {grp.Key,-24} {grp.Count()} title(s)");
        Console.WriteLine();
        var wy = games.Where(g => g.Year > 0).ToList();
        if (wy.Count > 0)
        {
            Console.WriteLine($"  Average year: {wy.Average(g => g.Year):F0}");
            Console.WriteLine($"  Range:        {wy.Min(g => g.Year)} – {wy.Max(g => g.Year)}");
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
                "Delete by ID",
                "Cascade delete (game + assets + builds)",
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
                case 5: CascadeDelete(); break;
                case 6: Search(); break;
                case 7: Stats(); break;
                case 8: Sort(); break;
            }
        }
    }
}

// ========================= ASSETS =========================

static class AssetOps
{
    static void TableHeader()
    {
        Console.WriteLine($"  {"ID",-5} {"Game",-5} {"Name",-24} {"Type",-12} {"Author",-14} {"Size KB",-12}");
        UI.Sep('-');
    }

    static void TableRow(Asset a)
        => Console.WriteLine($"  {a.Id,-5} {a.GameId,-5} {a.Name,-24} {a.Type,-12} {a.Author,-14} {a.FileSizeKb,-12}");

    static void PrintTable(List<Asset> assets)
    {
        if (assets.Count == 0) { Console.WriteLine("  No records found."); return; }
        TableHeader();
        foreach (var a in assets) TableRow(a);
        Console.WriteLine($"\n  Total: {assets.Count}");
    }

    public static void Add()
    {
        Console.Clear();
        UI.Header("Add Asset");
        var games = Db.LoadGames();
        if (games.Count == 0)
        {
            UI.Error("  Add at least one game first.");
            UI.Pause();
            return;
        }
        var gameId = UI.AskInt("  Game ID: ");
        if (!games.Exists(g => g.Id == gameId))
        {
            UI.Error("  Game not found.");
            UI.Pause();
            return;
        }
        var assets = Db.LoadAssets();
        var a = new Asset
        {
            Id         = Db.NextAssetId(),
            GameId     = gameId,
            Name       = UI.AskNonEmpty("  Name:    "),
            Type       = UI.AskNonEmpty("  Type:    "),
            Author     = UI.AskNonEmpty("  Author:  "),
            FileSizeKb = UI.AskLong    ("  Size KB: ")
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
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadAssets();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0)
        {
            UI.Error("  Asset not found.");
        }
        else
        {
            var a = list[idx];
            UI.Sep('-');
            Console.WriteLine($"  ID:      {a.Id}");
            Console.WriteLine($"  Game ID: {a.GameId}");
            Console.WriteLine($"  Name:    {a.Name}");
            Console.WriteLine($"  Type:    {a.Type}");
            Console.WriteLine($"  Author:  {a.Author}");
            Console.WriteLine($"  Size:    {a.FileSizeKb} KB");
        }
        UI.Pause();
    }

    public static void DeleteById()
    {
        Console.Clear();
        UI.Header("Delete Asset by ID");
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadAssets();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Asset not found."); }
        else
        {
            list.RemoveAt(idx);
            Db.SaveAssets(list);
            UI.Success("  Asset deleted.");
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
        if (assets.Count == 0) { Console.WriteLine("  No data."); UI.Pause(); return; }

        long total = assets.Sum(a => a.FileSizeKb);
        Console.WriteLine($"  Total assets:     {assets.Count}");
        Console.WriteLine($"  Total size:       {total} KB  ({total / 1024.0:F2} MB)");
        Console.WriteLine($"  Average size:     {assets.Average(a => a.FileSizeKb):F0} KB");
        Console.WriteLine($"  Largest:          {assets.Max(a => a.FileSizeKb)} KB");
        Console.WriteLine($"  Smallest:         {assets.Min(a => a.FileSizeKb)} KB");
        Console.WriteLine();
        Console.WriteLine("  By type:");
        foreach (var grp in assets.GroupBy(a => a.Type).OrderByDescending(g => g.Count()))
            Console.WriteLine($"    {grp.Key,-22} {grp.Count()} item(s)   {grp.Sum(a => a.FileSizeKb)} KB");
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

// ========================= BUILDS =========================

static class BuildOps
{
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
        Console.WriteLine($"  {"ID",-5} {"Game",-5} {"Version",-12} {"Platform",-12} {"Status",-12} {"Date",-12}");
        UI.Sep('-');
    }

    static void TableRow(Build b)
        => Console.WriteLine($"  {b.Id,-5} {b.GameId,-5} {b.Version,-12} {b.Platform,-12} {b.Status,-12} {FmtDate(b.BuildDate),-12}");

    static void PrintTable(List<Build> builds)
    {
        if (builds.Count == 0) { Console.WriteLine("  No records found."); return; }
        TableHeader();
        foreach (var b in builds) TableRow(b);
        Console.WriteLine($"\n  Total: {builds.Count}");
    }

    public static void Add()
    {
        Console.Clear();
        UI.Header("Add Build");
        var games = Db.LoadGames();
        if (games.Count == 0)
        {
            UI.Error("  Add at least one game first.");
            UI.Pause();
            return;
        }
        var gameId = UI.AskInt("  Game ID: ");
        if (!games.Exists(g => g.Id == gameId))
        {
            UI.Error("  Game not found.");
            UI.Pause();
            return;
        }
        var builds = Db.LoadBuilds();
        var b = new Build
        {
            Id        = Db.NextBuildId(),
            GameId    = gameId,
            Version   = UI.AskNonEmpty("  Version:  "),
            Platform  = UI.AskNonEmpty("  Platform: "),
            Status    = UI.AskNonEmpty("  Status:   "),
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
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadBuilds();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0)
        {
            UI.Error("  Build not found.");
        }
        else
        {
            var b = list[idx];
            UI.Sep('-');
            Console.WriteLine($"  ID:       {b.Id}");
            Console.WriteLine($"  Game ID:  {b.GameId}");
            Console.WriteLine($"  Version:  {b.Version}");
            Console.WriteLine($"  Platform: {b.Platform}");
            Console.WriteLine($"  Status:   {b.Status}");
            Console.WriteLine($"  Date:     {FmtDate(b.BuildDate)}");
        }
        UI.Pause();
    }

    public static void DeleteById()
    {
        Console.Clear();
        UI.Header("Delete Build by ID");
        var id   = UI.AskInt("  ID: ");
        var list = Db.LoadBuilds();
        var idx  = list.FindIndex(x => x.Id == id);
        if (idx < 0) { UI.Error("  Build not found."); }
        else
        {
            list.RemoveAt(idx);
            Db.SaveBuilds(list);
            UI.Success("  Build deleted.");
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
        if (builds.Count == 0) { Console.WriteLine("  No data."); UI.Pause(); return; }

        Console.WriteLine($"  Total builds: {builds.Count}");
        Console.WriteLine();
        Console.WriteLine("  By platform:");
        foreach (var grp in builds.GroupBy(b => b.Platform).OrderByDescending(g => g.Count()))
            Console.WriteLine($"    {grp.Key,-24} {grp.Count()} build(s)");
        Console.WriteLine();
        Console.WriteLine("  By status:");
        foreach (var grp in builds.GroupBy(b => b.Status).OrderByDescending(g => g.Count()))
            Console.WriteLine($"    {grp.Key,-24} {grp.Count()} build(s)");
        Console.WriteLine();
        var wd = builds.Where(b => b.BuildDate > 0).ToList();
        if (wd.Count > 0)
        {
            Console.WriteLine($"  First build:  {FmtDate(wd.Min(b => b.BuildDate))}");
            Console.WriteLine($"  Latest build: {FmtDate(wd.Max(b => b.BuildDate))}");
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

// ========================= APP =========================

static class App
{
    public static void Run()
    {
        while (true)
        {
            var choice = UI.Menu(
                "CAS Manager  —  Crazy Animals Studio",
                new[] { "Games", "Assets", "Builds" },
                "Exit");

            switch (choice)
            {
                case 0:
                    Console.Clear();
                    Console.WriteLine("  Goodbye!");
                    return;
                case 1: GameOps.RunMenu(); break;
                case 2: AssetOps.RunMenu(); break;
                case 3: BuildOps.RunMenu(); break;
            }
        }
    }
}

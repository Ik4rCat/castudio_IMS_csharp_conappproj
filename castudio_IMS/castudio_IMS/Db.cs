using System.Text;

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

    // ── Игры ───────────────────────────────────────────────────

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

    // ── Ассеты ─────────────────────────────────────────────────

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

    // ── Билды ──────────────────────────────────────────────────

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

    // ── Перенумерация ──────────────────────────────────────────

    // Перенумеровывает игры 1..N и обновляет GameId в ассетах и билдах
    public static void RenumberGames(List<Game> games, List<Asset> assets, List<Build> builds)
    {
        var idMap = new Dictionary<int, int>();
        for (int i = 0; i < games.Count; i++)
        {
            int oldId = games[i].Id;
            int newId = i + 1;
            idMap[oldId] = newId;
            var g = games[i]; g.Id = newId; games[i] = g;
        }
        for (int i = 0; i < assets.Count; i++)
        {
            if (idMap.TryGetValue(assets[i].GameId, out int nid))
            { var a = assets[i]; a.GameId = nid; assets[i] = a; }
        }
        for (int i = 0; i < builds.Count; i++)
        {
            if (idMap.TryGetValue(builds[i].GameId, out int nid))
            { var b = builds[i]; b.GameId = nid; builds[i] = b; }
        }
    }

    public static void RenumberAssets(List<Asset> assets)
    {
        for (int i = 0; i < assets.Count; i++)
        { var a = assets[i]; a.Id = i + 1; assets[i] = a; }
    }

    public static void RenumberBuilds(List<Build> builds)
    {
        for (int i = 0; i < builds.Count; i++)
        { var b = builds[i]; b.Id = i + 1; builds[i] = b; }
    }
}

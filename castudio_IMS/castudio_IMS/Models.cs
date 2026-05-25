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

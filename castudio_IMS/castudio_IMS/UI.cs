static class UI
{
    // Ширина контентной зоны внутри box-меню (между "│ " и " │")
    const int Inner = 54;

    static int LeftPad
    {
        get
        {
            try { return Math.Max(0, (Console.WindowWidth - Inner - 4) / 2); }
            catch { return 2; }
        }
    }

    // Публичный отступ — используется в ops-классах для выравнивания таблиц
    public static string P => new string(' ', LeftPad);

    // ── Box helpers ────────────────────────────────────────────

    static void BTop() => Console.WriteLine(P + "┌" + new string('─', Inner + 2) + "┐");
    static void BBot() => Console.WriteLine(P + "└" + new string('─', Inner + 2) + "┘");

    static void BDiv(char l = '├', char m = '─', char r = '┤')
        => Console.WriteLine(P + l + new string(m, Inner + 2) + r);

    // ── Публичный API ──────────────────────────────────────────

    // Заголовок для страниц контента (не box)
    public static void Header(string title)
    {
        int w;
        try { w = Console.WindowWidth; } catch { w = 80; }
        int pad = Math.Max(0, (w - title.Length) / 2);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(new string(' ', pad) + title);
        Console.ResetColor();
        Sep('═');
        Console.WriteLine();
    }

    // Box-меню по центру экрана
    public static int Menu(string title, string[] options, string exitLabel = "Назад")
    {
        while (true)
        {
            Console.Clear();

            // Заголовок
            BTop();
            int titlePad = Math.Max(0, (Inner - title.Length) / 2);
            string paddedTitle = new string(' ', titlePad) + title;
            Console.Write(P + "│ ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(paddedTitle);
            Console.ResetColor();
            Console.WriteLine(new string(' ', Math.Max(0, Inner - paddedTitle.Length)) + " │");
            BDiv('╞', '═', '╡');

            // Пункты меню
            for (int i = 0; i < options.Length; i++)
            {
                string numS = (i + 1).ToString();
                string rest = $". {options[i]}";
                int used = 2 + numS.Length + rest.Length; // 2 = indent

                Console.Write(P + "│ ");
                Console.Write("  ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(numS);
                Console.ResetColor();
                Console.Write(rest);
                Console.WriteLine(new string(' ', Math.Max(0, Inner - used)) + " │");
            }

            // Разделитель + пункт выхода
            BDiv();
            {
                string rest = $". {exitLabel}";
                int used = 2 + 1 + rest.Length;
                Console.Write(P + "│ ");
                Console.Write("  ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("0");
                Console.ResetColor();
                Console.Write(rest);
                Console.WriteLine(new string(' ', Math.Max(0, Inner - used)) + " │");
            }
            BBot();

            Console.Write(P + "  Choice: ");
            var s = Console.ReadLine() ?? "";
            if (int.TryParse(s, out var v) && v >= 0 && v <= options.Length)
                return v;
            Error("  Invalid choice.");
            Console.ReadKey(true);
        }
    }

    public static void Pause()
    {
        Console.WriteLine();
        Console.Write(P + "  Press any key...");
        Console.ReadKey(true);
        Console.WriteLine();
    }

    public static void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(P + msg);
        Console.ResetColor();
    }

    public static void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(P + msg);
        Console.ResetColor();
    }

    public static void Info(string msg)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(P + msg);
        Console.ResetColor();
    }

    public static void Sep(char c = '─', int len = -1)
    {
        if (len < 0) len = Inner + 4;
        Console.WriteLine(P + new string(c, len));
    }

    public static string Ask(string prompt)
    {
        Console.Write(P + prompt);
        return Console.ReadLine() ?? "";
    }

    // Если пользователь нажимает Enter без ввода — возвращает current
    public static string AskOrKeep(string prompt, string current)
    {
        Console.Write(P + $"{prompt}[{current}]: ");
        var s = (Console.ReadLine() ?? "").Trim();
        return s.Length > 0 ? s : current;
    }

    public static int AskIntOrKeep(string prompt, int current)
    {
        while (true)
        {
            Console.Write(P + $"{prompt}[{current}]: ");
            var s = (Console.ReadLine() ?? "").Trim();
            if (s.Length == 0) return current;
            if (int.TryParse(s, out var v)) return v;
            Error("  Please enter a whole number.");
        }
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
            Error("  Please enter a non-negative number.");
        }
    }

    // Цвет статуса по ключевым словам
    public static ConsoleColor StatusColor(string status)
    {
        var s = status.ToLowerInvariant();
        if (s.Contains("вышел") || s.Contains("выпущ") || s.Contains("готов") ||
            s.Contains("released") || s.Contains("done") || s.Contains("shipped"))
            return ConsoleColor.Green;
        if (s.Contains("отмен") || s.Contains("cancel") || s.Contains("закрыт") ||
            s.Contains("failed") || s.Contains("провал"))
            return ConsoleColor.Red;
        return ConsoleColor.Yellow;
    }

    public static void WriteStatus(string status)
    {
        Console.ForegroundColor = StatusColor(status);
        Console.Write(status);
        Console.ResetColor();
    }

    // ASCII-арт баннер при запуске
    public static void Banner()
    {
        Console.Clear();
        int w;
        try { w = Console.WindowWidth; } catch { w = 80; }

        string[] art = {
            " ██████╗  █████╗ ███████╗",
            "██╔════╝ ██╔══██╗██╔════╝",
            "██║      ███████║███████╗ ",
            "██║      ██╔══██║╚════██║",
            "╚██████╗ ██║  ██║███████║",
            " ╚═════╝ ╚═╝  ╚═╝╚══════╝",
        };

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        foreach (var line in art)
        {
            int pad = Math.Max(0, (w - line.Length) / 2);
            Console.WriteLine(new string(' ', pad) + line);
        }
        Console.ResetColor();

        Console.WriteLine();
        string sub = "Crazy Animals Studio  —  IMS";
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string(' ', Math.Max(0, (w - sub.Length) / 2)) + sub);
        Console.ResetColor();
        Console.WriteLine();
        Pause();
    }
}

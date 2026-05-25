static class App
{
    public static void Run()
    {
        UI.Banner();

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
                    UI.Success("\n  Goodbye!");
                    return;
                case 1: GameOps.RunMenu(); break;
                case 2: AssetOps.RunMenu(); break;
                case 3: BuildOps.RunMenu(); break;
            }
        }
    }
}

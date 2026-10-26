namespace SpaceMiner.Editor
{
    public static class LocalRefreshChecks
    {
        public static void Run()
        {
            ResearchSimulationValidation.Run();
            SettingsValidation.BuildChecks();
        }
    }
}

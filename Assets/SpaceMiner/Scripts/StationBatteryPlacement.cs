namespace SpaceMiner
{
    // Across the ring corridor from the command-room entrance, not across the entire station.
    public static class StationBatteryPlacement
    {
        public const int Axis = 0;
        public const int ActiveIndex = Axis * 2;
        public const int ReserveIndex = ActiveIndex + 1;
        public static bool Installed(int index) => index == ActiveIndex || index == ReserveIndex;
    }
}

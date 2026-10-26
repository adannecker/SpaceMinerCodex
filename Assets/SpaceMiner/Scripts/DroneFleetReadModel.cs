using UnityEngine;

namespace SpaceMiner
{
    // A transient view of the existing simulation, never an inventory or a saved fleet.
    public sealed class DroneFleetReadModel
    {
        public string Id, Name, Status, Tool, Activity, ActivityId, Destination, WaitReason, EtaLabel;
        public bool Internal, Operational, HasOnboardStores, HasActivity, HasEta, CanRecall;
        public int Index = -1;
        public float BatteryKwh, BatteryCapacityKwh, FuelLiters, FuelCapacityLiters;
        public float CargoKg, CargoCapacityKg, CargoVolumeM3, CargoFillFraction;
        public float SpeedMetersPerSecond, DistanceMeters, EtaSeconds, Progress;
        public Vector3 Position;
    }

    public sealed class DroneConsoleTarget
    {
        public readonly string Id, Name, Details;
        public DroneConsoleTarget(string id, string name, string details)
        { Id = id; Name = name; Details = details; }
    }

    public struct DroneConsoleLayout
    {
        public Rect Header, Domain, Fleet, Details, Orders, Footer, CompactNavigation;
        public bool Compact, Stacked;
        public static DroneConsoleLayout For(float width, float height, bool forceCompact = false)
        {
            float w = Mathf.Max(320, width), h = Mathf.Max(360, height);
            var result = new DroneConsoleLayout
            {
                Header = new Rect(16, 12, w - 32, 108),
                Domain = new Rect(16, 126, w - 32, 38),
                Footer = new Rect(20, h - 56, w - 40, 46),
                Compact = w < 1180 || forceCompact,
                Stacked = w < 720
            };
            float top = 176, bottom = h - 68;
            if (result.Stacked)
            {
                result.Fleet = new Rect(16, top, w - 32, 144);
                result.CompactNavigation = new Rect(16, top + 152, w - 32, 34);
                result.Details = result.Orders = new Rect(16, top + 196, w - 32, Mathf.Max(48, bottom - top - 196));
            }
            else
            {
                float left = Mathf.Clamp(w * .21f, 224, 304);
                result.Fleet = new Rect(16, top, left, Mathf.Max(48, bottom - top));
                float x = left + 28, remaining = w - x - 16;
                if (result.Compact)
                {
                    result.CompactNavigation = new Rect(x, top, remaining, 34);
                    result.Details = result.Orders = new Rect(x, top + 44, remaining, Mathf.Max(48, bottom - top - 44));
                }
                else
                {
                    float right = Mathf.Clamp(w * .30f, 338, 460);
                    result.Details = new Rect(x, top, remaining - right - 12, Mathf.Max(48, bottom - top));
                    result.Orders = new Rect(w - 16 - right, top, right, Mathf.Max(48, bottom - top));
                }
            }
            return result;
        }
    }
}

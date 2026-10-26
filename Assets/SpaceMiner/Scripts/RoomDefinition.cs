using System;
using UnityEngine;

namespace SpaceMiner
{
    [Serializable]
    public sealed class RoomKeepout
    {
        public Vector2 center, size;
        public float yaw;
        public string label;
        public RoomKeepout Copy() => new RoomKeepout { center = center, size = size, yaw = yaw, label = label };
    }

    /// <summary>Floor coordinates belong to the room's supplied frame, whose +Z points outward.</summary>
    [Serializable]
    public sealed class RoomDefinition
    {
        public string Id, Name;
        public bool IsAnnular = true;
        public float InnerRadius, OuterRadius, HalfAngle = 13.5f;
        public float MinX, MaxX, MinZ, MaxZ;
        public float WallMargin = .25f, ItemGap = .15f, MaximumHeight = 4.4f;
        public Vector3 MinimumItemSize = Vector3.one * .4f;
        public CommandRoomLayoutItem[] Defaults = Array.Empty<CommandRoomLayoutItem>();
        public RoomKeepout[] Keepouts = Array.Empty<RoomKeepout>();

        public RoomDefinition Copy()
        {
            var result = (RoomDefinition)MemberwiseClone();
            result.Defaults = new CommandRoomLayoutItem[Defaults?.Length ?? 0];
            for (int i = 0; i < result.Defaults.Length; i++) result.Defaults[i] = Defaults[i]?.Copy();
            result.Keepouts = new RoomKeepout[Keepouts?.Length ?? 0];
            for (int i = 0; i < result.Keepouts.Length; i++) result.Keepouts[i] = Keepouts[i]?.Copy();
            return result;
        }

        public Vector2[] FloorOutline()
        {
            if (!IsAnnular) return new[] { new Vector2(MinX, MinZ), new Vector2(MaxX, MinZ),
                new Vector2(MaxX, MaxZ), new Vector2(MinX, MaxZ) };
            const int steps = 32;
            var result = new Vector2[(steps + 1) * 2];
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(-HalfAngle, HalfAngle, (float)i / steps) * Mathf.Deg2Rad;
                result[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * OuterRadius;
                result[steps + 1 + i] = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * InnerRadius;
            }
            return result;
        }

        private void ViewBounds(out Vector2 low, out Vector2 high)
        {
            if (!IsAnnular) { low = new Vector2(MinX, MinZ); high = new Vector2(MaxX, MaxZ); return; }
            float angle = HalfAngle * Mathf.Deg2Rad;
            low = new Vector2(-OuterRadius * Mathf.Sin(angle), InnerRadius * Mathf.Cos(angle));
            high = new Vector2(OuterRadius * Mathf.Sin(angle), OuterRadius);
        }
        public Vector2 ViewCenter { get { ViewBounds(out var low, out var high); return (low + high) * .5f; } }
        // Full width/height, including two metres of drawing space on every side.
        public Vector2 ViewExtents { get { ViewBounds(out var low, out var high); return high - low + Vector2.one * 4; } }

        public static RoomDefinition CommandRoom() => new RoomDefinition
        {
            Id = "command-room", Name = "Kommandozentrale", InnerRadius = 38, OuterRadius = 54,
            HalfAngle = 13.5f, Defaults = CommandRoomLayout.CreateDefaults(),
            Keepouts = new[] { new RoomKeepout { center = new Vector2(0, 39.5f), size = new Vector2(4.2f, 3), label = "Eingangsbereich" } }
        };

        public static RoomDefinition Annular(string id, string name, float inner, float outer, float halfAngle,
            CommandRoomLayoutItem[] defaults, params RoomKeepout[] keepouts) => new RoomDefinition
        { Id = id, Name = name, IsAnnular = true, InnerRadius = inner, OuterRadius = outer, HalfAngle = halfAngle,
            Defaults = defaults, Keepouts = keepouts };

        public static RoomDefinition Rectangle(string id, string name, float minX, float maxX, float minZ, float maxZ,
            CommandRoomLayoutItem[] defaults, params RoomKeepout[] keepouts) => new RoomDefinition
        { Id = id, Name = name, IsAnnular = false, MinX = minX, MaxX = maxX, MinZ = minZ, MaxZ = maxZ,
            Defaults = defaults, Keepouts = keepouts };
    }
}

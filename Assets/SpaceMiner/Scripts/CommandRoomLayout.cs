using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    [Serializable]
    public sealed class CommandRoomLayoutItem
    {
        public string id, label;
        public Vector3 position, size;
        public float yaw;

        public CommandRoomLayoutItem Copy() => new CommandRoomLayoutItem
        { id = id, label = label, position = position, size = size, yaw = yaw };
    }

    // Marks individually movable blockout geometry so static mesh batching leaves it intact.
    public sealed class CommandRoomBlock : MonoBehaviour { public string id, roomId; }

    /// <summary>
    /// A prototype room plan, deliberately independent of campaign resources and savegames.
    /// Coordinates are relative to the station frame; position denotes the bottom centre.
    /// </summary>
    public sealed class CommandRoomLayout : MonoBehaviour
    {
        // This is a room-plan key, not the physical station battery. Keeping the key
        // preserves the user's existing position, dimensions and rotation in schema 1.
        public const string PowerConsolePlanId = "battery";
        public const float InnerRadius = 38, OuterRadius = 54, HalfAngle = 13.5f;
        public const float WallMargin = .25f, ItemGap = .15f;
        public const float MinimumSize = .4f, MaximumHeight = 4.4f;
        public const float EntryHalfWidth = 2.1f, EntryEnd = 41;
        private const int SchemaVersion = 1;

        [Serializable]
        private sealed class PlanFile
        {
            public int schema = SchemaVersion;
            public string room = "command-room";
            public CommandRoomLayoutItem[] items;
        }

        private sealed class Binding
        {
            public Transform root, body, label;
            public Vector3 labelScale;
            public float labelWidth;
            public Vector3 referenceSize;
            public bool authoredObject;
        }

        private CommandRoomLayoutItem[] items = CreateDefaults();
        private readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>();
        private Transform stationFrame;
        private string savedSignature, currentSignature;
        private string savePath;
        public RoomDefinition Definition { get; private set; } = RoomDefinition.CommandRoom();

        public event Action Changed;
        // Runtime systems may lock a working berth/cell without changing geometric constraints.
        // Return null/empty to accept, otherwise the explanation is shown by the editor.
        public Func<CommandRoomLayoutItem, string> EditGuard { get; set; }
        public IReadOnlyList<CommandRoomLayoutItem> Items => Snapshot();
        public string SavePath => savePath ?? DefaultPath(RoomId);
        public string RoomId => Definition.Id;
        public string RoomName => Definition.Name;
        public string DisplayName => RoomName;
        public Transform Frame => stationFrame != null ? stationFrame : transform;
        public Vector2 ViewCenter => Definition.ViewCenter;
        public Vector2 ViewExtents => Definition.ViewExtents;
        public Vector2[] FloorOutline() => Definition.FloorOutline();
        public string StartupNotice { get; private set; }
        public bool HasUnsavedChanges => savedSignature != currentSignature;

        public void Initialize(Transform frame, string overridePath = null)
            => InitializeForRoom(frame, RoomDefinition.CommandRoom(), overridePath);

        public void InitializeForRoom(Transform frame, RoomDefinition definition, string overridePath = null)
        {
            if (!ValidDefinition(definition, out string invalid)) throw new ArgumentException(invalid, nameof(definition));
            if (!ValidatePlan(definition.Defaults, definition, out invalid))
                throw new ArgumentException("Ungültiger Startgrundriss für " + definition.Id + ": " + invalid, nameof(definition));
            if (Definition.Id != definition.Id && bindings.Count > 0)
                throw new InvalidOperationException("Eine bereits gebundene Rauminstanz darf nicht den Raum wechseln.");
            Definition = definition.Copy();
            stationFrame = frame != null ? frame : transform;
            savePath = string.IsNullOrEmpty(overridePath) ? DefaultPath(RoomId) : Path.GetFullPath(overridePath);
            items = CreateDefaults(Definition);
            savedSignature = Signature(items, RoomId);
            currentSignature = savedSignature;
            StartupNotice = null;
            if (File.Exists(savePath) && !Load(out string reason)) StartupNotice = reason;
            ApplyBindings();
        }

        public CommandRoomLayoutItem Get(string id)
        {
            foreach (var item in items) if (item.id == id) return item.Copy();
            return null;
        }

        public CommandRoomLayoutItem[] Snapshot()
        {
            var result = new CommandRoomLayoutItem[items.Length];
            for (int i = 0; i < items.Length; i++) result[i] = items[i].Copy();
            return result;
        }

        public Transform BoundRoot(string id) => bindings.TryGetValue(id, out var binding) ? binding.root : null;

        /// <summary>Root and body use unit scale. The front display faces the root's local -Z.</summary>
        public void Register(string id, Transform root, Transform body, Transform frontLabel = null)
        {
            if (Get(id) == null) throw new ArgumentException("Unbekanntes Raumelement: " + id, nameof(id));
            if (root == null || body == null) throw new ArgumentNullException(root == null ? nameof(root) : nameof(body));
            var marker = root.GetComponent<CommandRoomBlock>() ?? root.gameObject.AddComponent<CommandRoomBlock>();
            marker.id = id; marker.roomId = RoomId;
            var binding = new Binding { root = root, body = body, label = frontLabel,
                labelScale = frontLabel != null ? frontLabel.localScale : Vector3.one };
            if (frontLabel != null)
            {
                var display = frontLabel.GetComponent<StationDisplay>();
                if (display != null) binding.labelWidth = display.PanelSize.x;
            }
            bindings[id] = binding;
            ApplyBinding(Get(id), binding);
        }

        /// <summary>
        /// Bind actual authored meshes/colliders without replacing them by a proxy. The wrapper
        /// uses the catalogue's original floor pose, so loading a plan does not recapture it.
        /// Register once, after authoring/relocation and before any static mesh batching.
        /// </summary>
        public Transform RegisterObject(string id, Transform authoredRoot)
        {
            if (authoredRoot == null) throw new ArgumentNullException(nameof(authoredRoot));
            if (bindings.ContainsKey(id)) throw new InvalidOperationException("Raumelement ist bereits gebunden: " + id);
            CommandRoomLayoutItem reference = null;
            foreach (var entry in Definition.Defaults) if (entry.id == id) { reference = entry; break; }
            if (reference == null) throw new ArgumentException("Unbekanntes Raumelement: " + id, nameof(id));
            var wrapper = new GameObject(reference.label + " / bewegliche Raumgruppe").transform;
            wrapper.SetParent(authoredRoot.parent, false);
            wrapper.SetPositionAndRotation(Frame.TransformPoint(reference.position), Frame.rotation * Quaternion.Euler(0, reference.yaw, 0));
            wrapper.localScale = Vector3.one;
            authoredRoot.SetParent(wrapper, true);
            var marker = wrapper.gameObject.AddComponent<CommandRoomBlock>(); marker.id = id; marker.roomId = RoomId;
            var binding = new Binding { root = wrapper, authoredObject = true, referenceSize = reference.size };
            bindings[id] = binding;
            ApplyBinding(Get(id), binding);
            return wrapper;
        }

        public bool Validate(CommandRoomLayoutItem candidate, out string reason)
        {
            if (candidate == null) { reason = "Kein Raumelement ausgewählt."; return false; }
            var replacement = Snapshot();
            bool found = false;
            for (int i = 0; i < replacement.Length; i++)
                if (replacement[i].id == candidate.id) { replacement[i] = candidate.Copy(); found = true; break; }
            if (!found) { reason = "Unbekanntes Raumelement: " + candidate.id; return false; }
            return ValidatePlan(replacement, Definition, out reason) && ValidateChanges(replacement, out reason);
        }

        public bool TrySet(CommandRoomLayoutItem candidate, out string reason)
        {
            if (!Validate(candidate, out reason)) return false;
            var replacement = Snapshot();
            for (int i = 0; i < replacement.Length; i++)
                if (replacement[i].id == candidate.id) { replacement[i] = candidate.Copy(); break; }
            Commit(replacement);
            return true;
        }

        public bool TryApply(CommandRoomLayoutItem[] snapshot, out string reason)
        {
            if (!ValidatePlan(snapshot, Definition, out reason) || !ValidateChanges(snapshot, out reason)) return false;
            Commit(snapshot);
            return true;
        }

        public bool TryResetDefaults(out string reason) => TryApply(CreateDefaults(Definition), out reason);
        public void ResetDefaults() { if (!TryResetDefaults(out string reason)) StartupNotice = reason; }

        private bool ValidateChanges(CommandRoomLayoutItem[] replacement, out string reason)
        {
            if (EditGuard != null)
                foreach (var candidate in replacement)
                {
                    var previous = Get(candidate.id);
                    if (previous != null && previous.position == candidate.position && previous.size == candidate.size
                        && Mathf.Abs(Mathf.DeltaAngle(previous.yaw, candidate.yaw)) < .001f) continue;
                    string rejected = EditGuard(candidate.Copy());
                    if (!string.IsNullOrEmpty(rejected)) { reason = rejected; return false; }
                }
            reason = null;
            return true;
        }

        private void Commit(CommandRoomLayoutItem[] replacement)
        {
            // Canonical ordering and labels make hand-edited plans deterministic and unambiguous.
            var defaults = CreateDefaults(Definition);
            for (int i = 0; i < defaults.Length; i++)
                foreach (var candidate in replacement)
                    if (candidate.id == defaults[i].id)
                    {
                        defaults[i].position = new Vector3(candidate.position.x, 0, candidate.position.z);
                        defaults[i].size = candidate.size;
                        defaults[i].yaw = NormalizeYaw(candidate.yaw);
                        break;
                    }
            items = defaults;
            currentSignature = Signature(items, RoomId);
            ApplyBindings();
            Physics.SyncTransforms();
            Changed?.Invoke();
        }

        private void ApplyBindings()
        {
            foreach (var item in items)
                if (bindings.TryGetValue(item.id, out var binding)) ApplyBinding(item, binding);
        }

        private void ApplyBinding(CommandRoomLayoutItem item, Binding binding)
        {
            if (binding.root == null || (!binding.authoredObject && binding.body == null)) return;
            var frame = stationFrame != null ? stationFrame : transform;
            binding.root.SetPositionAndRotation(frame.TransformPoint(item.position), frame.rotation * Quaternion.Euler(0, item.yaw, 0));
            binding.root.localScale = binding.authoredObject ? new Vector3(item.size.x / binding.referenceSize.x,
                item.size.y / binding.referenceSize.y, item.size.z / binding.referenceSize.z) : Vector3.one;
            if (binding.authoredObject) return;
            binding.body.localPosition = new Vector3(0, item.size.y * .5f, 0);
            binding.body.localRotation = Quaternion.identity;
            binding.body.localScale = item.size;
            if (binding.label == null) return;
            binding.label.localPosition = new Vector3(0, item.size.y * .62f, -item.size.z * .5f - .065f);
            binding.label.localRotation = Quaternion.identity;
            float labelFit = binding.labelWidth > .01f ? Mathf.Min(1, (item.size.x - .12f) / binding.labelWidth) : 1;
            labelFit = Mathf.Min(labelFit, item.size.y / .7f);
            binding.label.localScale = binding.labelScale * labelFit;
        }

        public bool Save(out string reason)
        {
            if (!ValidatePlan(items, Definition, out reason)) return false;
            string temporary = SavePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                File.WriteAllText(temporary, JsonUtility.ToJson(new PlanFile { room = RoomId, items = Snapshot() }, true));
                // Replacement and backup happen on the same filesystem. A failed write keeps the old plan.
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, SavePath + ".bak");
                else File.Move(temporary, SavePath);
                savedSignature = currentSignature;
                StartupNotice = null;
                reason = "Raumplan gespeichert.";
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                reason = "Raumplan konnte nicht gespeichert werden: " + exception.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        public bool Load(out string reason)
        {
            try
            {
                if (!File.Exists(SavePath)) { reason = "Noch kein gespeicherter Raumplan vorhanden."; return false; }
                if (new FileInfo(SavePath).Length > 1024 * 1024) { reason = "Raumplandatei ist zu groß."; return false; }
                var plan = JsonUtility.FromJson<PlanFile>(File.ReadAllText(SavePath));
                if (plan == null || plan.schema != SchemaVersion || plan.room != RoomId)
                { reason = "Raumplan hat ein unbekanntes Format."; return false; }
                if (!ValidatePlan(plan.items, Definition, out reason) || !ValidateChanges(plan.items, out reason))
                { reason = "Raumplan wurde nicht geladen: " + reason; return false; }
                Commit(plan.items);
                savedSignature = currentSignature;
                StartupNotice = null;
                reason = "Gespeicherten Raumplan geladen.";
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                reason = "Raumplan konnte nicht geladen werden: " + exception.Message;
                return false;
            }
        }

        public static bool ValidatePlan(CommandRoomLayoutItem[] plan, out string reason)
            => ValidatePlan(plan, RoomDefinition.CommandRoom(), out reason);

        public static bool ValidatePlan(CommandRoomLayoutItem[] plan, RoomDefinition definition, out string reason)
        {
            if (!ValidDefinition(definition, out reason)) return false;
            var defaults = definition.Defaults;
            if (plan == null || plan.Length != defaults.Length)
            { reason = "Der Plan muss alle " + defaults.Length + " Raumelemente enthalten."; return false; }
            var known = new HashSet<string>();
            foreach (var entry in defaults) known.Add(entry.id);
            var seen = new HashSet<string>();
            for (int i = 0; i < plan.Length; i++)
            {
                var item = plan[i];
                if (item == null || string.IsNullOrEmpty(item.id) || !known.Contains(item.id) || !seen.Add(item.id))
                { reason = "Raumplan enthält ein unbekanntes oder doppeltes Element."; return false; }
                if (!Finite(item.position) || !Finite(item.size) || !Finite(item.yaw))
                { reason = item.id + ": Position und Maße müssen endliche Zahlen sein."; return false; }
                if (Mathf.Abs(item.position.y) > .001f)
                { reason = item.id + ": Die Unterseite muss auf dem Boden liegen."; return false; }
                var minimum = Vector3.Max(Vector3.one * MinimumSize, definition.MinimumItemSize);
                if (item.size.x < minimum.x || item.size.z < minimum.z || item.size.y < minimum.y || item.size.y > definition.MaximumHeight)
                { reason = item.id + ": Mindestmaß (Breite/Höhe/Tiefe) " + minimum.x.ToString("0.#") + "/" + minimum.y.ToString("0.#")
                    + "/" + minimum.z.ToString("0.#") + " m, maximale Höhe " + definition.MaximumHeight.ToString("0.#") + " m."; return false; }
                var corners = Footprint(item);
                float sine = Mathf.Sin(definition.HalfAngle * Mathf.Deg2Rad), cosine = Mathf.Cos(definition.HalfAngle * Mathf.Deg2Rad);
                for (int c = 0; c < 4; c++)
                {
                    Vector2 p = corners[c], q = corners[(c + 1) % 4];
                    bool outside = definition.IsAnnular
                        ? p.magnitude > definition.OuterRadius - definition.WallMargin + .0001f
                            || p.y * sine - Mathf.Abs(p.x) * cosine < definition.WallMargin - .0001f
                            || NearestOrigin(p, q).magnitude < definition.InnerRadius + definition.WallMargin - .0001f
                        : p.x < definition.MinX + definition.WallMargin - .0001f || p.x > definition.MaxX - definition.WallMargin + .0001f
                            || p.y < definition.MinZ + definition.WallMargin - .0001f || p.y > definition.MaxZ - definition.WallMargin + .0001f;
                    if (outside)
                    { reason = item.id + ": Das Element ragt über die Raumgrenze oder in die Wand."; return false; }
                }
                foreach (var keepout in definition.Keepouts ?? Array.Empty<RoomKeepout>())
                    if (Overlap(corners, KeepoutFootprint(keepout), 0))
                    { reason = item.id + ": " + (string.IsNullOrEmpty(keepout.label) ? "Der markierte Bereich" : keepout.label) + " muss frei bleiben."; return false; }
                for (int other = 0; other < i; other++)
                    if (Overlap(corners, Footprint(plan[other]), definition.ItemGap))
                    { reason = item.id + ": Überschneidung mit " + plan[other].id + " (" + (definition.ItemGap * 100).ToString("0.#") + " cm Abstand erforderlich)."; return false; }
            }
            reason = null;
            return true;
        }

        public static Vector2[] KeepoutFootprint(RoomKeepout keepout) => Footprint(new CommandRoomLayoutItem
        { position = new Vector3(keepout.center.x, 0, keepout.center.y), size = new Vector3(keepout.size.x, 1, keepout.size.y), yaw = keepout.yaw });

        private static bool ValidDefinition(RoomDefinition definition, out string reason)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name))
            { reason = "Raumkennung und Name fehlen."; return false; }
            foreach (char character in definition.Id)
                if (!(character >= 'a' && character <= 'z') && !(character >= '0' && character <= '9') && character != '-')
                { reason = "Ungültige Raumkennung."; return false; }
            if (!Finite(definition.WallMargin) || definition.WallMargin < 0 || !Finite(definition.ItemGap) || definition.ItemGap < 0
                || !Finite(definition.MaximumHeight) || definition.MaximumHeight < MinimumSize)
            { reason = "Ungültige Raumabstände oder Raumhöhe."; return false; }
            if (!Finite(definition.MinimumItemSize) || definition.MinimumItemSize.x <= 0 || definition.MinimumItemSize.y <= 0
                || definition.MinimumItemSize.z <= 0 || Mathf.Max(MinimumSize, definition.MinimumItemSize.y) > definition.MaximumHeight)
            { reason = "Ungültige Mindestmaße für die vorhandenen Raumelemente."; return false; }
            if (definition.IsAnnular)
            {
                if (!Finite(definition.InnerRadius) || !Finite(definition.OuterRadius) || !Finite(definition.HalfAngle)
                    || definition.InnerRadius < 0 || definition.OuterRadius <= definition.InnerRadius + 2 * definition.WallMargin
                    || definition.HalfAngle <= 0 || definition.HalfAngle >= 90)
                { reason = "Ungültiger Ringsektor."; return false; }
            }
            else if (!Finite(definition.MinX) || !Finite(definition.MaxX) || !Finite(definition.MinZ) || !Finite(definition.MaxZ)
                || definition.MaxX <= definition.MinX + 2 * definition.WallMargin || definition.MaxZ <= definition.MinZ + 2 * definition.WallMargin)
            { reason = "Ungültiger rechteckiger Grundriss."; return false; }
            if (definition.Defaults == null || definition.Defaults.Length == 0 || definition.Defaults.Length > 256)
            { reason = "Ein Raum benötigt einen Katalog seiner vorhandenen Elemente."; return false; }
            var known = new HashSet<string>();
            foreach (var item in definition.Defaults)
                if (item == null || string.IsNullOrWhiteSpace(item.id) || string.IsNullOrWhiteSpace(item.label) || !known.Add(item.id))
                { reason = "Ungültiger oder doppelter Elementkatalog."; return false; }
            foreach (var keepout in definition.Keepouts ?? Array.Empty<RoomKeepout>())
                if (keepout == null || !Finite(keepout.center.x) || !Finite(keepout.center.y) || !Finite(keepout.size.x)
                    || !Finite(keepout.size.y) || !Finite(keepout.yaw) || keepout.size.x <= 0 || keepout.size.y <= 0)
                { reason = "Ungültiger geschützter Bereich."; return false; }
            reason = null;
            return true;
        }

        public static Vector2[] Footprint(CommandRoomLayoutItem item)
        {
            var result = new Vector2[4];
            var rotation = Quaternion.Euler(0, NormalizeYaw(item.yaw), 0);
            var signs = new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
            for (int i = 0; i < 4; i++)
            {
                var point = item.position + rotation * new Vector3(signs[i].x * item.size.x * .5f, 0, signs[i].y * item.size.z * .5f);
                result[i] = new Vector2(point.x, point.z);
            }
            return result;
        }

        public static Vector2[] RoomOutline()
        {
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

        private static Vector2 NearestOrigin(Vector2 p, Vector2 q)
        {
            Vector2 edge = q - p;
            return p + edge * Mathf.Clamp01(-Vector2.Dot(p, edge) / Mathf.Max(.000001f, edge.sqrMagnitude));
        }

        private static bool Overlap(Vector2[] a, Vector2[] b, float gap)
        {
            for (int polygon = 0; polygon < 2; polygon++)
            {
                var source = polygon == 0 ? a : b;
                for (int i = 0; i < 2; i++)
                {
                    Vector2 edge = source[(i + 1) % 4] - source[i];
                    Vector2 axis = new Vector2(-edge.y, edge.x).normalized;
                    float minA = float.PositiveInfinity, maxA = float.NegativeInfinity;
                    float minB = float.PositiveInfinity, maxB = float.NegativeInfinity;
                    foreach (var point in a) { float dot = Vector2.Dot(point, axis); minA = Mathf.Min(minA, dot); maxA = Mathf.Max(maxA, dot); }
                    foreach (var point in b) { float dot = Vector2.Dot(point, axis); minB = Mathf.Min(minB, dot); maxB = Mathf.Max(maxB, dot); }
                    if (maxA + gap <= minB + .0001f || maxB + gap <= minA + .0001f) return false;
                }
            }
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static float NormalizeYaw(float value) => (float)(((double)value % 360 + 360) % 360);
        private static string Signature(CommandRoomLayoutItem[] source, string room) => JsonUtility.ToJson(new PlanFile { room = room, items = source });

        public static CommandRoomLayoutItem[] CreateDefaults(RoomDefinition definition)
        {
            var result = new CommandRoomLayoutItem[definition.Defaults.Length];
            for (int i = 0; i < result.Length; i++) result[i] = definition.Defaults[i].Copy();
            return result;
        }

        public static CommandRoomLayoutItem[] CreateDefaults() => new[]
        {
            Item("main-console", "STEUERPULT", 0, 49, 4.4f, 1.2f, 2.2f, 0),
            Item("drones", "DROHNEN", -10.1f, 47, 1.6f, 1.3f, .8f, 270),
            Item("planning", "STATIONSPLANUNG", 10.1f, 47, 1.6f, 1.3f, .8f, 90),
            Item("research", "MIRA / TECHTREE", 9.1f, 43, 1.6f, 1.3f, .8f, 90),
            Item("production", "FERTIGUNG", -9.1f, 43, 1.6f, 1.3f, .8f, 270),
            Item("printer", "MATERIALDRUCKER", -7.2f, 40, 2.6f, 2.8f, 2, 270),
            Item("food", "NAHRUNGSSYNTHESE", 7.2f, 40, 2.4f, 2.4f, 1.7f, 90),
            Item(PowerConsolePlanId, "STROMVERSORGUNG", -9.65f, 45.1f, 1.1f, 2.4f, .8f, 270)
        };

        private static CommandRoomLayoutItem Item(string id, string label, float x, float z, float width, float height, float depth, float yaw)
            => new CommandRoomLayoutItem { id = id, label = label, position = new Vector3(x, 0, z), size = new Vector3(width, height, depth), yaw = yaw };

        private static string DefaultPath(string roomId)
        {
            // A standalone build in Builds/Windows shares the checked-out design plan with the editor.
            // Require project documentation as well as Assets so temporary build copies are skipped.
            // A distributed build without project sources falls back to the user's application data.
            var directory = new DirectoryInfo(Application.dataPath);
            for (int i = 0; directory != null && i < 6; i++, directory = directory.Parent)
                if (Directory.Exists(Path.Combine(directory.FullName, "Assets", "SpaceMiner"))
                    && File.Exists(Path.Combine(directory.FullName, "docs", "Spielidee.md")))
                    return Path.Combine(directory.FullName, "Design", "RoomPlans", roomId + "-layout.json");
            return Path.Combine(Application.persistentDataPath, "CommandRoomLayout", roomId + "-layout.json");
        }
    }
}

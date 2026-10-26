using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SpaceMiner
{
    // A metre-based blockout editor. It edits the live room through the layout model;
    // the drawing is an independent 2D view and never moves a gameplay camera.
    [DefaultExecutionOrder(-2150)]
    public sealed class CommandRoomPlanner : MonoBehaviour
    {
        public static CommandRoomPlanner Current { get; private set; }
        public static bool IsOpen { get; private set; }
        private static int closedFrame = -1;
        public static bool BlocksInput => IsOpen || closedFrame == Time.frameCount;
        public static bool OwnsScreenPoint(Vector3 mouse) => BlocksInput;

        public CommandRoomLayout Layout { get; private set; }
        private readonly List<CommandRoomLayout> rooms = new List<CommandRoomLayout>();
        private readonly Dictionary<string, string> roomSelections = new Dictionary<string, string>();
        public IReadOnlyList<CommandRoomLayout> Rooms => rooms;
        public string SelectedId { get; private set; }
        public Rect PlanViewport { get; private set; }
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        private readonly Stack<CommandRoomLayoutItem[]> undo = new Stack<CommandRoomLayoutItem[]>();
        private readonly Stack<CommandRoomLayoutItem[]> redo = new Stack<CommandRoomLayoutItem[]>();
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private readonly string[] fields = new string[6];
        private Vector2 itemScroll, inspectorScroll, pan;
        private float zoom = 1, metresToPixels = 20;
        private bool pointerActive, pointerResize, pointerRotate, panning, shiftSnap;
        public string RotationReference { get; private set; }
        private readonly List<Vector2[]> snapGuides = new List<Vector2[]>();
        private Vector2 pointerStart, panStart;
        private CommandRoomLayoutItem pointerItem, rejectedItem;
        private CommandRoomLayoutItem[] pointerSnapshot;
        private int resizeCorner;
        private CursorLockMode previousLock;
        private bool previousCursor;
        private string status = "Box wählen. Im Grundriss ziehen; an den gelben Ecken die Größe ändern.";
        private bool statusError;
        private GUIStyle boxLabel, fieldStyle;
        private float ButtonHeight => Mathf.Max(39, ui.Button.lineHeight + 16);

        private static GUIStyle FitButton(string caption, float width, GUIStyle source)
        {
            var style = new GUIStyle(source) { wordWrap = false, alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 5, 5) };
            var content = new GUIContent(caption);
            while (style.fontSize > 12 && style.CalcSize(content).x > width - 2) style.fontSize--;
            return style;
        }
        private string ShortLabel(string id)
        {
            if (id != null && id.StartsWith("berth-", StringComparison.Ordinal)) return "PLATZ " + id.Substring(6);
            switch (id)
            {
                case "main-console": return "STEUERPULT";
                case "drones": return "DROHNEN";
                case "planning": return "PLANUNG";
                case "research": return "TECHTREE";
                case "production": return "FERTIGUNG";
                case "printer": return "DRUCKER";
                case "food": return "NAHRUNG";
                case "battery": return "BATTERIE";
                case "repair-01": return "UMBAU 01";
                case "repair-02": return "UMBAU 02";
                case "tool-rack": return "WERKZEUGE";
                case "scrap-analysis": return "ANALYSE";
                case "scrap-bin": return "SCHROTT";
                case "bench-left": return "BANK LINKS";
                case "bench-right": return "BANK RECHTS";
                case "helper-dock": return "R-01 DOCK";
                case "repair-bench": return "REPARATUR";
                case "offline-facility": return "ANLAGE";
                case "carbon-store": return "C-LAGER";
                case "iron-store": return "Fe-LAGER";
                case "unknown-store": return "?-LAGER";
                default: return Layout?.Get(id)?.label ?? id;
            }
        }
        private static string RouteCaption(string label)
        {
            string lower = (label ?? "").ToLowerInvariant();
            if (lower.Contains("quer")) return "QUERGANG";
            if (lower.Contains("links")) return "GANG L";
            if (lower.Contains("rechts")) return "GANG R";
            if (lower.Contains("eingang")) return "EINGANG";
            if (lower.StartsWith("zugang") && lower.Contains("schleuse")) return "SCHLEUSE";
            if (lower.StartsWith("zugang")) return "ZUGANG";
            if (lower.Contains("fahrspur")) return "FAHRSPUR";
            return label;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStatics() { Current = null; IsOpen = false; closedFrame = -1; }
        private void Awake() { Current = this; }
        public void Configure(CommandRoomLayout layout)
            => ConfigureRooms(layout != null ? new[] { layout } : Array.Empty<CommandRoomLayout>());
        public void ConfigureRooms(IEnumerable<CommandRoomLayout> layouts)
        {
            if (pointerActive) EndPointerEdit(true);
            string currentId = Layout != null ? Layout.RoomId : null;
            if (Layout != null && SelectedId != null) roomSelections[Layout.RoomId] = SelectedId;
            rooms.Clear();
            if (layouts != null)
                foreach (var room in layouts)
                    if (room != null && !rooms.Exists(existing => existing.RoomId == room.RoomId)) rooms.Add(room);
            Layout = null; SelectedId = null; undo.Clear(); redo.Clear();
            if (rooms.Count == 0) { Array.Clear(fields, 0, fields.Length); return; }
            var selectedRoom = rooms.Find(room => room.RoomId == currentId) ?? rooms.Find(room => room.RoomId == "command-room") ?? rooms[0];
            SelectRoom(selectedRoom.RoomId);
        }
        public bool SelectRoom(string roomId)
        {
            var room = rooms.Find(candidate => candidate.RoomId == roomId);
            if (room == null) return false;
            if (Layout == room) return true;
            if (pointerActive) EndPointerEdit(true);
            if (Layout != null && SelectedId != null) roomSelections[Layout.RoomId] = SelectedId;
            Layout = room; SelectedId = null; undo.Clear(); redo.Clear();
            pan = Vector2.zero; zoom = 1; itemScroll = inspectorScroll = Vector2.zero;
            panning = false; rejectedItem = null; snapGuides.Clear(); RotationReference = null;
            string remembered = roomSelections.TryGetValue(room.RoomId, out string id) ? id : null;
            if (!string.IsNullOrEmpty(remembered) && room.Get(remembered) != null) Select(remembered);
            else if (room.Items.Count > 0) Select(room.Items[0].id);
            else Array.Clear(fields, 0, fields.Length);
            SetStatus(!string.IsNullOrEmpty(room.StartupNotice) ? room.StartupNotice : room.RoomName + " · Element wählen, verschieben, skalieren oder am grünen Ring drehen.", !string.IsNullOrEmpty(room.StartupNotice));
            return true;
        }
        public bool Open()
        {
            if (IsOpen) return true;
            if (Layout == null) ConfigureRooms(FindObjectsByType<CommandRoomLayout>(FindObjectsSortMode.None));
            if (Layout == null || StationOperations.TacticalViewActive || StartMenu.IsOpen || IntroSequence.BlocksGameplay || MemoryCinematic.IsPlaying ||
                SettingsMenu.IsOpen || SaveGameMenu.IsOpen || QuitMenu.BlocksInput || RingConstructionSimulator.IsOpen || TechTreeMenu.IsOpen) return false;
            StationOperations.Current?.ClosePanel();
            previousLock = Cursor.lockState; previousCursor = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            IsOpen = true; rejectedItem = null;
            if (!string.IsNullOrEmpty(Layout.StartupNotice)) SetStatus(Layout.StartupNotice, true);
            SyncFields(); return true;
        }
        public void Close()
        {
            if (!IsOpen) return;
            if (pointerActive) EndPointerEdit(true);
            IsOpen = false; closedFrame = Time.frameCount; panning = false;
            StationInteriorMode.Current?.RevalidateRoomPlanPosition();
            Cursor.lockState = previousLock; Cursor.visible = previousCursor;
        }
        private void OnDisable() { if (Current == this && IsOpen) Close(); }
        private void OnDestroy()
        {
            if (Current == this) { Close(); Current = null; }
            ui.Dispose();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7)) { if (IsOpen) Close(); else Open(); }
            if (!IsOpen) return;
            // These values are restored on close; the normal interior controller also
            // updates them when gameplay input is released on the following frame.
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (Input.GetKeyDown(KeyCode.Escape))
            { if (pointerActive) EndPointerEdit(true); else Close(); }
        }

        private static CommandRoomLayoutItem Copy(CommandRoomLayoutItem source)
            => source == null ? null : new CommandRoomLayoutItem { id = source.id, label = source.label, position = source.position, size = source.size, yaw = source.yaw };
        private static bool Same(CommandRoomLayoutItem a, CommandRoomLayoutItem b)
            => a != null && b != null && a.id == b.id && a.position == b.position && a.size == b.size && Mathf.Abs(Mathf.DeltaAngle(a.yaw, b.yaw)) < .001f;
        private static bool Same(CommandRoomLayoutItem[] a, CommandRoomLayoutItem[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (!Same(a[i], b[i])) return false;
            return true;
        }
        private void Remember(CommandRoomLayoutItem[] before)
        {
            if (Same(before, Layout.Snapshot())) return;
            // Small, fixed catalogue; bounded history avoids retaining unlimited edits.
            if (undo.Count >= 64)
            {
                var recent = undo.ToArray(); undo.Clear();
                for (int i = 62; i >= 0; i--) undo.Push(recent[i]);
            }
            undo.Push(before); redo.Clear();
        }
        public bool Select(string id)
        {
            if (Layout == null || Layout.Get(id ?? "") == null) return false;
            if (pointerActive) EndPointerEdit();
            SelectedId = id; rejectedItem = null; SyncFields(); return true;
        }
        public bool ApplyEdit(Vector3 position, Vector3 size, float yaw, out string reason)
        {
            var item = Layout?.Get(SelectedId ?? "");
            if (item == null) { reason = "Zuerst eine Box wählen."; SetStatus(reason, true); return false; }
            if (pointerActive) EndPointerEdit();
            var before = Layout.Snapshot(); item.position = position; item.size = size; item.yaw = yaw;
            if (!Layout.TrySet(item, out reason)) { rejectedItem = Copy(item); SetStatus(reason, true); return false; }
            Remember(before); rejectedItem = null; SyncFields(); SetStatus("" + item.label + " angepasst. Änderungen sind im Raum sichtbar."); return true;
        }
        public bool MoveSelected(Vector2 xz, out string reason)
        {
            var item = Layout?.Get(SelectedId ?? "");
            if (item == null) { reason = "Zuerst eine Box wählen."; return false; }
            return ApplyEdit(new Vector3(xz.x, item.position.y, xz.y), item.size, item.yaw, out reason);
        }
        public bool ResizeSelected(Vector3 size, out string reason)
        {
            var item = Layout?.Get(SelectedId ?? "");
            if (item == null) { reason = "Zuerst eine Box wählen."; return false; }
            return ApplyEdit(item.position, size, item.yaw, out reason);
        }
        public bool RotateSelected(float delta, out string reason)
            => RotateSelected(delta, ReadShift(), out reason);
        public bool RotateSelected(float delta, bool shift, out string reason)
        {
            var item = Layout?.Get(SelectedId ?? "");
            if (item == null) { reason = "Zuerst eine Box wählen."; return false; }
            snapGuides.Clear(); item.yaw += delta;
            if (shift) item = SnapRotation(item, out string _);
            bool accepted = ApplyEdit(item.position, item.size, item.yaw, out reason);
            if (accepted) SetStatus("Drehung · " + NormalizeAngle(item.yaw).ToString("0.#") + "°" + (shift ? " · " + RotationReference : ""));
            return accepted;
        }
        public bool Undo()
        {
            if (pointerActive) EndPointerEdit(true);
            if (Layout == null || undo.Count == 0) return false;
            var previous = undo.Peek(); var current = Layout.Snapshot();
            if (!Layout.TryApply(previous, out string reason)) { SetStatus(reason, true); return false; }
            undo.Pop(); redo.Push(current); rejectedItem = null; SyncFields(); SetStatus("Änderung zurückgenommen."); return true;
        }
        public bool Redo()
        {
            if (pointerActive) EndPointerEdit(true);
            if (Layout == null || redo.Count == 0) return false;
            var next = redo.Peek(); var current = Layout.Snapshot();
            if (!Layout.TryApply(next, out string reason)) { SetStatus(reason, true); return false; }
            redo.Pop(); undo.Push(current); rejectedItem = null; SyncFields(); SetStatus("Änderung wiederhergestellt."); return true;
        }
        public bool SaveLayout(out string reason)
        {
            if (pointerActive) EndPointerEdit();
            if (Layout == null) { reason = "Raumplan fehlt."; return false; }
            bool result = Layout.Save(out reason); SetStatus(result ? "Raumplan gespeichert: " + Layout.SavePath : reason, !result); return result;
        }
        public bool LoadLayout(out string reason)
        {
            if (pointerActive) EndPointerEdit(true);
            if (Layout == null) { reason = "Raumplan fehlt."; return false; }
            var before = Layout.Snapshot(); bool result = Layout.Load(out reason);
            if (result) { Remember(before); rejectedItem = null; SyncFields(); }
            SetStatus(result ? "Gespeicherten Raumplan geladen. Rückgängig bleibt möglich." : reason, !result); return result;
        }
        public void ResetLayout()
        {
            if (Layout == null) return;
            if (pointerActive) EndPointerEdit(true);
            var before = Layout.Snapshot();
            if (!Layout.TryResetDefaults(out string reason)) { SetStatus(reason, true); return; }
            Remember(before);
            rejectedItem = null; SyncFields(); SetStatus("Startgrundriss wiederhergestellt. Mit Rückgängig wiederherstellbar.");
        }
        private void SetStatus(string message, bool error = false) { status = message; statusError = error; }

        public Vector2 WorldToPlanScreen(Vector2 world)
        {
            Vector2 centre = Layout != null ? Layout.ViewCenter : Vector2.zero;
            return PlanViewport.center + pan + new Vector2(world.x - centre.x, -(world.y - centre.y)) * metresToPixels;
        }
        public Vector2 PlanScreenToWorld(Vector2 point)
        {
            Vector2 value = (point - PlanViewport.center - pan) / Mathf.Max(.001f, metresToPixels);
            Vector2 centre = Layout != null ? Layout.ViewCenter : Vector2.zero;
            return new Vector2(value.x + centre.x, centre.y - value.y);
        }
        private float BaseMetresToPixels
        {
            get
            {
                Vector2 extent = Layout != null ? Layout.ViewExtents : Vector2.one;
                return Mathf.Max(.01f, Mathf.Min(PlanViewport.width / Mathf.Max(1, extent.x), PlanViewport.height / Mathf.Max(1, extent.y + 2)));
            }
        }
        public Vector2[] GetItemPlanPolygon(string id)
        {
            var item = Layout?.Get(id ?? ""); if (item == null) return Array.Empty<Vector2>();
            Vector2[] corners = CommandRoomLayout.Footprint(item);
            for (int i = 0; i < corners.Length; i++) corners[i] = WorldToPlanScreen(corners[i]);
            return corners;
        }
        public Vector2 GetRotationHandlePlanPoint()
        {
            var item = Layout?.Get(SelectedId ?? ""); if (item == null) return Vector2.zero;
            Vector2 edge = FrontEdgePlanPoint(item);
            Vector2 centre = WorldToPlanScreen(new Vector2(item.position.x, item.position.z));
            // Keep the handle on the physical front axis, even when panning clips it.
            return edge + (edge - centre).normalized * 32;
        }
        private Vector2 FrontEdgePlanPoint(CommandRoomLayoutItem item)
        {
            Vector3 direction = Quaternion.Euler(0, item.yaw, 0) * Vector3.back;
            return WorldToPlanScreen(new Vector2(item.position.x + direction.x * item.size.z * .5f,
                item.position.z + direction.z * item.size.z * .5f));
        }
        private static bool Contains(CommandRoomLayoutItem item, Vector2 world)
        {
            var delta = Quaternion.Euler(0, -item.yaw, 0) * (new Vector3(world.x, 0, world.y) - item.position);
            return Mathf.Abs(delta.x) <= item.size.x * .5f && Mathf.Abs(delta.z) <= item.size.z * .5f;
        }
        public bool BeginPointerEdit(Vector2 guiPosition)
        {
            if (!IsOpen || Layout == null || !PlanViewport.Contains(guiPosition)) return false;
            if (pointerActive) EndPointerEdit();
            pointerResize = false; pointerRotate = false; resizeCorner = -1;
            var chosen = Layout.Get(SelectedId ?? "");
            if (chosen != null)
            {
                pointerRotate = Vector2.Distance(guiPosition, GetRotationHandlePlanPoint()) <= 13;
                if (!pointerRotate)
                {
                    var corners = GetItemPlanPolygon(chosen.id);
                    for (int i = 0; i < corners.Length; i++)
                        if (Vector2.Distance(guiPosition, corners[i]) <= 11) { resizeCorner = i; pointerResize = true; break; }
                }
            }
            if (!pointerResize && !pointerRotate)
            {
                chosen = null; Vector2 world = PlanScreenToWorld(guiPosition);
                for (int i = Layout.Items.Count - 1; i >= 0; i--)
                    if (Contains(Layout.Items[i], world)) { chosen = Layout.Items[i]; break; }
                if (chosen == null) return false;
                Select(chosen.id);
            }
            pointerItem = Copy(chosen); pointerSnapshot = Layout.Snapshot();
            pointerStart = PlanScreenToWorld(guiPosition); pointerActive = true; rejectedItem = null;
            if (Event.current != null) GUI.FocusControl(null); return true;
        }
        private static float Snapped(float value, bool shift) => shift ? Mathf.Round(value / .25f) * .25f : value;
        private static float NormalizeAngle(float angle) => Mathf.Repeat(angle, 360);
        private static bool ReadShift() => (Event.current != null && Event.current.shift) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public bool DragPointer(Vector2 guiPosition, out string reason)
            => DragPointer(guiPosition, ReadShift(), out reason);
        public bool MoveByDrag(Vector2 guiPosition, bool shift, out string reason) => DragPointer(guiPosition, shift, out reason);
        public bool DragPointer(Vector2 guiPosition, bool shift, out string reason)
        {
            if (!pointerActive || pointerItem == null) { reason = "Kein Element wird gezogen."; return false; }
            shiftSnap = shift; snapGuides.Clear();
            var item = Copy(pointerItem); Vector2 current = PlanScreenToWorld(guiPosition);
            if (pointerRotate)
            {
                Vector2 centre = new Vector2(pointerItem.position.x, pointerItem.position.z), start = pointerStart - centre, direction = current - centre;
                if (direction.magnitude * metresToPixels < 5 || start.sqrMagnitude < .00001f)
                { reason = "Zum Drehen etwas Abstand zum Mittelpunkt halten."; SetStatus(reason, true); return false; }
                float startAngle = Mathf.Atan2(start.x, start.y) * Mathf.Rad2Deg;
                float currentAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                item.yaw = pointerItem.yaw + Mathf.DeltaAngle(startAngle, currentAngle);
                if (shift) item = SnapRotation(item, out string _);
                else RotationReference = "Freie Drehung";
            }
            else if (!pointerResize)
            {
                Vector2 delta = current - pointerStart;
                item.position = new Vector3(pointerItem.position.x + delta.x, pointerItem.position.y, pointerItem.position.z + delta.y);
                if (shift) item = SnapPlacement(item);
            }
            else
            {
                Vector2[] footprint = CommandRoomLayout.Footprint(pointerItem);
                Vector2 anchor = footprint[(resizeCorner + 2) % 4];
                Vector3 local = Quaternion.Euler(0, -item.yaw, 0) * new Vector3(current.x - anchor.x, 0, current.y - anchor.y);
                Vector3 original = Quaternion.Euler(0, -item.yaw, 0) * new Vector3(footprint[resizeCorner].x - anchor.x, 0, footprint[resizeCorner].y - anchor.y);
                if (local.x * original.x <= 0 || local.z * original.z <= 0)
                { reason = "Die Ecke darf die gegenüberliegende Ecke nicht überqueren."; SetStatus(reason, true); return false; }
                float width = Snapped(Mathf.Abs(local.x), shift), depth = Snapped(Mathf.Abs(local.z), shift);
                item.size = new Vector3(width, item.size.y, depth);
                Vector3 centre = new Vector3(anchor.x, item.position.y, anchor.y) + Quaternion.Euler(0, item.yaw, 0) * new Vector3(Mathf.Sign(local.x) * width * .5f, 0, Mathf.Sign(local.z) * depth * .5f);
                item.position = centre;
            }
            if (!Layout.TrySet(item, out reason)) { rejectedItem = item; SetStatus(reason, true); return false; }
            rejectedItem = null; SyncFields();
            if (pointerRotate) SetStatus("DREHEN · " + NormalizeAngle(item.yaw).ToString("0.#") + "° · " + RotationReference + " · " + item.label);
            else SetStatus((shift ? "EINRASTEN" : "FREI") + " · " + (pointerResize ? "Größe ändern" : "Verschieben") + " · " + item.label + " · " + item.size.x.ToString("0.00") + " × " + item.size.z.ToString("0.00") + " m"); return true;
        }
        private CommandRoomLayoutItem SnapRotation(CommandRoomLayoutItem raw, out string reference)
        {
            const float captureAngle = 8, wallReach = 2.5f, neighbourReach = 3;
            CommandRoomLayoutItem best = null;
            float bestScore = float.PositiveInfinity, referenceYaw = 0;
            string bestLabel = null;
            Vector2 referenceOrigin = Vector2.zero;
            Vector2 centre = new Vector2(raw.position.x, raw.position.z);
            var corners = CommandRoomLayout.Footprint(raw);
            void Consider(float basis, float gap, string label, Vector2 origin, float guideYaw)
            {
                for (int quadrant = 0; quadrant < 4; quadrant++)
                {
                    float delta = Mathf.DeltaAngle(raw.yaw, basis + 90 * quadrant);
                    if (Mathf.Abs(delta) > captureAngle) continue;
                    var candidate = Copy(raw); candidate.yaw += delta;
                    if (!Layout.Validate(candidate, out _)) continue;
                    float score = Mathf.Abs(delta) + Mathf.Max(0, gap) * 2;
                    if (score >= bestScore) continue;
                    best = candidate; bestScore = score; bestLabel = label; referenceOrigin = origin; referenceYaw = guideYaw;
                }
            }
            foreach (PlanWall wall in RoomWalls())
            {
                float gap = float.PositiveInfinity;
                foreach (Vector2 corner in corners) gap = Mathf.Min(gap, Vector2.Dot(corner, wall.normal) - wall.offset);
                if (gap > wallReach) continue;
                Vector2 onWall = centre - wall.normal * (Vector2.Dot(centre, wall.normal) - wall.offset);
                Consider(wall.yaw, gap, wall.label, onWall, wall.yaw);
            }
            var definition = Layout.Definition;
            if (definition.IsAnnular)
            {
                float furthest = 0, nearest = float.PositiveInfinity;
                for (int i = 0; i < corners.Length; i++)
                {
                    furthest = Mathf.Max(furthest, corners[i].magnitude);
                    nearest = Mathf.Min(nearest, DistanceToSegment(Vector2.zero, corners[i], corners[(i + 1) % corners.Length]));
                }
                float polar = Mathf.Atan2(centre.x, centre.y) * Mathf.Rad2Deg;
                float outerGap = definition.OuterRadius - definition.WallMargin - furthest;
                float innerGap = nearest - definition.InnerRadius - definition.WallMargin;
                if (outerGap <= wallReach) Consider(polar, outerGap, "Außenwand-Tangente", centre.normalized * (definition.OuterRadius - definition.WallMargin), polar + 90);
                if (innerGap <= wallReach) Consider(polar, innerGap, "Innenwand-Tangente", centre.normalized * (definition.InnerRadius + definition.WallMargin), polar + 90);
            }
            foreach (var other in Layout.Items)
            {
                if (other.id == raw.id) continue;
                float gap = FootprintDistance(corners, CommandRoomLayout.Footprint(other));
                if (gap > neighbourReach) continue;
                Consider(other.yaw, gap, "Kante: " + ShortLabel(other.id), new Vector2(other.position.x, other.position.z), other.yaw);
            }
            if (best == null)
            {
                var grid = Copy(raw); grid.yaw = Mathf.Round(raw.yaw / 15) * 15;
                if (Layout.Validate(grid, out _)) { best = grid; bestLabel = "15°-Raster"; }
                else { best = raw; bestLabel = "Rasterposition blockiert"; }
            }
            else
            {
                Vector3 guide = Quaternion.Euler(0, referenceYaw, 0) * Vector3.forward;
                Vector2 along = new Vector2(guide.x, guide.z);
                snapGuides.Add(new[] { referenceOrigin - along * 2.5f, referenceOrigin + along * 2.5f });
            }
            Vector3 rotation = Quaternion.Euler(0, best.yaw, 0) * Vector3.forward;
            Vector2 axis = new Vector2(rotation.x, rotation.z), cross = new Vector2(-axis.y, axis.x);
            float span = Mathf.Max(best.size.x, best.size.z) * .75f + 1;
            snapGuides.Add(new[] { centre - axis * span, centre + axis * span });
            snapGuides.Add(new[] { centre - cross * span, centre + cross * span });
            reference = bestLabel; RotationReference = reference; return best;
        }
        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            return Vector2.Distance(point, a + edge * Mathf.Clamp01(Vector2.Dot(point - a, edge) / Mathf.Max(.000001f, edge.sqrMagnitude)));
        }
        private static float FootprintDistance(Vector2[] a, Vector2[] b)
        {
            float result = float.PositiveInfinity;
            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < b.Length; j++)
                {
                    result = Mathf.Min(result, DistanceToSegment(a[i], b[j], b[(j + 1) % b.Length]));
                    result = Mathf.Min(result, DistanceToSegment(b[j], a[i], a[(i + 1) % a.Length]));
                }
            return result;
        }
        private struct PlanWall
        {
            public Vector2 normal;
            public float offset, yaw;
            public string label;
        }
        private IEnumerable<PlanWall> RoomWalls()
        {
            var room = Layout.Definition;
            if (room.IsAnnular)
            {
                float angle = room.HalfAngle * Mathf.Deg2Rad;
                yield return new PlanWall { normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), offset = room.WallMargin, yaw = -room.HalfAngle, label = "Wand links" };
                yield return new PlanWall { normal = new Vector2(-Mathf.Cos(angle), Mathf.Sin(angle)), offset = room.WallMargin, yaw = room.HalfAngle, label = "Wand rechts" };
            }
            else
            {
                yield return new PlanWall { normal = Vector2.right, offset = room.MinX + room.WallMargin, yaw = 0, label = "Wand links" };
                yield return new PlanWall { normal = Vector2.left, offset = -room.MaxX + room.WallMargin, yaw = 0, label = "Wand rechts" };
                yield return new PlanWall { normal = Vector2.up, offset = room.MinZ + room.WallMargin, yaw = 90, label = "Wand hinten" };
                yield return new PlanWall { normal = Vector2.down, offset = -room.MaxZ + room.WallMargin, yaw = 90, label = "Wand vorne" };
            }
        }
        private CommandRoomLayoutItem SnapPlacement(CommandRoomLayoutItem raw)
        {
            // Shift uses the quarter-metre grid, then nearby edges with the model's
            // clearance. Every magnetic candidate is checked against the same model.
            const float threshold = .25f;
            var grid = Copy(raw); grid.position = new Vector3(Snapped(raw.position.x, true), raw.position.y, Snapped(raw.position.z, true));
            Bounds2(CommandRoomLayout.Footprint(raw), out Vector2 low, out Vector2 high);
            float dx = float.PositiveInfinity, dz = float.PositiveInfinity;
            foreach (var other in Layout.Items)
            {
                if (other.id == raw.id) continue;
                Bounds2(CommandRoomLayout.Footprint(other), out Vector2 a, out Vector2 b);
                // Align neighbouring matching edges. Clearance magnets only apply
                // when the boxes actually face each other along that axis; diagonal
                // neighbours must not pull a box sideways for an unnecessary gap.
                if (high.y >= a.y - 1 && low.y <= b.y + 1)
                    foreach (float d in new[] { a.x - low.x, b.x - high.x })
                        if (Mathf.Abs(d) <= threshold && Mathf.Abs(d) < Mathf.Abs(dx)) dx = d;
                if (high.y >= a.y && low.y <= b.y)
                    foreach (float d in new[] { a.x - Layout.Definition.ItemGap - high.x, b.x + Layout.Definition.ItemGap - low.x })
                        if (Mathf.Abs(d) <= threshold && Mathf.Abs(d) < Mathf.Abs(dx)) dx = d;
                if (high.x >= a.x - 1 && low.x <= b.x + 1)
                    foreach (float d in new[] { a.y - low.y, b.y - high.y })
                        if (Mathf.Abs(d) <= threshold && Mathf.Abs(d) < Mathf.Abs(dz)) dz = d;
                if (high.x >= a.x && low.x <= b.x)
                    foreach (float d in new[] { a.y - Layout.Definition.ItemGap - high.y, b.y + Layout.Definition.ItemGap - low.y })
                        if (Mathf.Abs(d) <= threshold && Mathf.Abs(d) < Mathf.Abs(dz)) dz = d;
            }
            var magnetic = Copy(grid);
            if (!float.IsPositiveInfinity(dx)) magnetic.position.x = raw.position.x + dx;
            if (!float.IsPositiveInfinity(dz)) magnetic.position.z = raw.position.z + dz;
            var chosen = Layout.Validate(magnetic, out _) ? magnetic : Layout.Validate(grid, out _) ? grid : raw;
            // Use the selected room's real side walls, including rectangular rooms.
            foreach (PlanWall wall in RoomWalls())
            {
                var corners = CommandRoomLayout.Footprint(raw); float distance = float.PositiveInfinity;
                foreach (Vector2 p in corners) distance = Mathf.Min(distance, Vector2.Dot(p, wall.normal));
                float adjustment = wall.offset - distance;
                if (Mathf.Abs(adjustment) > threshold) continue;
                var candidate = Copy(raw); candidate.position += new Vector3(wall.normal.x, 0, wall.normal.y) * adjustment;
                if (!Layout.Validate(candidate, out _)) continue;
                chosen = candidate;
                Vector2 onWall = Layout.ViewCenter - wall.normal * (Vector2.Dot(Layout.ViewCenter, wall.normal) - wall.offset), along = new Vector2(-wall.normal.y, wall.normal.x);
                float length = Layout.ViewExtents.magnitude;
                snapGuides.Add(new[] { onWall - along * length, onWall + along * length });
                break;
            }
            if (Layout.Definition.IsAnnular)
            {
                var rawCorners = CommandRoomLayout.Footprint(raw);
                Vector2 furthest = rawCorners[0], nearest = rawCorners[0];
                for (int i = 0; i < rawCorners.Length; i++)
                {
                    Vector2 p = rawCorners[i], q = rawCorners[(i + 1) % rawCorners.Length];
                    if (p.sqrMagnitude > furthest.sqrMagnitude) furthest = p;
                    Vector2 edge = q - p, close = p + edge * Mathf.Clamp01(-Vector2.Dot(p, edge) / Mathf.Max(.00001f, edge.sqrMagnitude));
                    if (close.sqrMagnitude < nearest.sqrMagnitude) nearest = close;
                }
                int circleIndex = 0;
                foreach (var pair in new[] { new Vector2(furthest.magnitude, Layout.Definition.OuterRadius - Layout.Definition.WallMargin), new Vector2(nearest.magnitude, Layout.Definition.InnerRadius + Layout.Definition.WallMargin) })
                {
                    Vector2 point = circleIndex++ == 0 ? furthest : nearest;
                    float adjustment = pair.y - pair.x; if (Mathf.Abs(adjustment) > threshold) continue;
                    var wall = Copy(raw); wall.position += new Vector3(point.x, 0, point.y).normalized * adjustment;
                    if (!Layout.Validate(wall, out _)) continue;
                    chosen = wall; Vector2 along = new Vector2(-point.y, point.x).normalized;
                    snapGuides.Add(new[] { point - along * 5, point + along * 5 });
                }
            }
            Bounds2(Layout.FloorOutline(), out Vector2 roomLow, out Vector2 roomHigh);
            if (!float.IsPositiveInfinity(dx) && Mathf.Abs(chosen.position.x - magnetic.position.x) < .001f)
                snapGuides.Add(new[] { new Vector2(low.x + dx, roomLow.y - 2), new Vector2(low.x + dx, roomHigh.y + 2) });
            if (!float.IsPositiveInfinity(dz) && Mathf.Abs(chosen.position.z - magnetic.position.z) < .001f)
                snapGuides.Add(new[] { new Vector2(roomLow.x - 2, low.y + dz), new Vector2(roomHigh.x + 2, low.y + dz) });
            return chosen;
        }
        private static void Bounds2(Vector2[] points, out Vector2 low, out Vector2 high)
        {
            low = high = points[0];
            foreach (Vector2 p in points) { low = Vector2.Min(low, p); high = Vector2.Max(high, p); }
        }
        public void EndPointerEdit(bool cancel = false)
        {
            if (!pointerActive) return;
            pointerActive = false;
            if (cancel)
            {
                if (!Layout.TryApply(pointerSnapshot, out string reason)) SetStatus(reason, true);
                else SetStatus("Ziehen abgebrochen.");
            }
            else
            {
                Remember(pointerSnapshot);
                var current = pointerRotate ? Layout.Get(SelectedId ?? "") : null;
                SetStatus(current != null ? "Drehung übernommen · " + current.yaw.ToString("0.#") + "° · " + RotationReference + ". Raumplan speichern übernimmt die Änderung dauerhaft." : "Anordnung angepasst. Zum dauerhaften Übernehmen Raumplan speichern.");
            }
            pointerItem = null; pointerSnapshot = null; rejectedItem = null; pointerRotate = false; snapGuides.Clear(); SyncFields();
        }

        private void SyncFields()
        {
            var item = Layout?.Get(SelectedId ?? ""); if (item == null) return;
            float[] values = { item.position.x, item.position.z, item.size.x, item.size.z, item.size.y, item.yaw };
            for (int i = 0; i < fields.Length; i++) fields[i] = values[i].ToString("0.##", CultureInfo.InvariantCulture);
        }
        private void ApplyFields()
        {
            float[] values = new float[6];
            for (int i = 0; i < values.Length; i++)
                if (!float.TryParse((fields[i] ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]) || float.IsNaN(values[i]) || float.IsInfinity(values[i]))
                { SetStatus("Bitte gültige Zahlen eingeben; Komma oder Punkt sind möglich.", true); return; }
            var item = Layout?.Get(SelectedId ?? ""); if (item == null) return;
            ApplyEdit(new Vector3(values[0], item.position.y, values[1]), new Vector3(values[2], values[4], values[3]), values[5], out _);
        }
        private void OnGUI()
        {
            if (!IsOpen || Layout == null) return;
            var oldMatrix = GUI.matrix; int oldDepth = GUI.depth; var oldColor = GUI.color;
            try
            {
                float scale = UiLayout.Scale(1280, 800); GUI.matrix = Matrix4x4.Scale(Vector3.one * scale); GUI.depth = -470;
                ui.Configure(SettingsStore.Current.Accessibility);
                GUI.tooltip = "";
                boxLabel = new GUIStyle(ui.Small) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(12 * SettingsStore.Current.Accessibility.TextScale), wordWrap = false };
                fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = Mathf.RoundToInt(16 * SettingsStore.Current.Accessibility.TextScale), padding = new RectOffset(7, 7, 4, 4) };
                float width = Screen.width / scale, height = Screen.height / scale;
                // Capture active gestures before any header/tab control can consume
                // their mouse release. Switching room explicitly cancels a gesture.
                if (pointerActive || panning) HandlePointerGesture(Event.current);
                SpaceMinerUi.Fill(new Rect(0, 0, width, height), new Color(.008f, .018f, .03f, 1));
                var heading = new GUIStyle(ui.Heading) { wordWrap = false };
                var headingText = new GUIContent("RAUMPLANUNG / " + Layout.RoomName.ToUpperInvariant());
                while (heading.fontSize > 18 && heading.CalcSize(headingText).x > width - 300) heading.fontSize--;
                GUI.Label(new Rect(24, 17, width - 300, 48), headingText, heading);
                if (GUI.Button(new Rect(width - 249, 18, 225, 46), "Schließen · F7", FitButton("Schließen · F7", 225, ui.Button))) { Close(); return; }
                GUI.Label(new Rect(24, 61, width - 48, 35), "Element wählen · gelbe Ecken: Größe · grüner Ring: Drehen · Shift: Einrasten", ui.Small);
                bool multipleRooms = rooms.Count > 1;
                if (multipleRooms)
                {
                    float tabWidth = (width - 36) / rooms.Count;
                    for (int i = 0; i < rooms.Count; i++)
                        if (ui.Tab(new Rect(18 + i * tabWidth, 99, tabWidth - 7, 42), rooms[i].RoomName, rooms[i] == Layout)) SelectRoom(rooms[i].RoomId);
                }
                float top = multipleRooms ? 155 : 112;
                Rect list = new Rect(18, top, 225, height - top - 76), inspector = new Rect(width - 292, top, 274, height - top - 76);
                PlanViewport = new Rect(257, top, width - 563, height - top - 76);
                metresToPixels = BaseMetresToPixels * zoom;
                DrawList(list); DrawInspector(inspector); DrawPlan();
                ui.Status(new Rect(24, height - 62, width - 48, 51), status, statusError ? SpaceMinerUi.Error : SpaceMinerUi.Cyan);
                HandleInput();
                ui.Tooltip(1, width, height);
            }
            finally { GUI.matrix = oldMatrix; GUI.depth = oldDepth; GUI.color = oldColor; }
        }
        private void DrawList(Rect panel)
        {
            ui.Panel(panel);
            float available = panel.width - 24;
            GUI.Label(new Rect(panel.x + 12, panel.y + 18, available, 30), "RAUMELEMENTE", ui.Small);
            GUI.Label(new Rect(panel.x + 12, panel.y + 51, available, 49), Layout.HasUnsavedChanges ? "● Plan nicht gespeichert" : "Plan gespeichert / unverändert", ui.Small);
            float footerY = panel.yMax - 14 - 3 * ButtonHeight - 12;
            Rect scrollRect = new Rect(panel.x + 12, panel.y + 108, available, Mathf.Max(85, footerY - panel.y - 118));
            var items = Layout.Items;
            float row = ButtonHeight + 10;
            itemScroll = GUI.BeginScrollView(scrollRect, itemScroll, new Rect(0, 0, available - 19, Mathf.Max(scrollRect.height, items.Count * row)), false, true);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                bool chosen = item.id == SelectedId;
                string caption = (chosen ? "► " : "") + item.label;
                if (GUI.Button(new Rect(0, i * row, available - 20, ButtonHeight + 4), caption, FitButton(caption, available - 20, chosen ? ui.Primary : ui.Button))) Select(item.id);
            }
            GUI.EndScrollView();
            GUI.enabled = CanUndo;
            if (GUI.Button(new Rect(panel.x + 12, footerY, available, ButtonHeight), "Rückgängig · Strg+Z", FitButton("Rückgängig · Strg+Z", available, ui.Button))) Undo();
            GUI.enabled = CanRedo;
            if (GUI.Button(new Rect(panel.x + 12, footerY + ButtonHeight + 6, available, ButtonHeight), "Wiederholen · Strg+Y", FitButton("Wiederholen · Strg+Y", available, ui.Button))) Redo();
            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.x + 12, footerY + 2 * (ButtonHeight + 6), available, ButtonHeight), "Startanordnung", FitButton("Startanordnung", available, ui.Button))) ResetLayout();
        }
        private void DrawInspector(Rect panel)
        {
            ui.Panel(panel); GUILayout.BeginArea(new Rect(panel.x + 12, panel.y + 17, panel.width - 24, panel.height - 30));
            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);
            var item = Layout.Get(SelectedId ?? "");
            GUILayout.Label(item?.label ?? "Box auswählen", ui.Text); GUILayout.Space(8);
            string[] labels = { "Position X / seitlich (m)", "Position Z / radial (m)", "Breite (m)", "Tiefe (m)", "Höhe (m)", "Drehung (°)" };
            GUI.enabled = item != null;
            for (int i = 0; i < fields.Length; i++)
            {
                GUILayout.Label(labels[i], ui.Small); GUI.SetNextControlName("room-plan-field-" + i);
                fields[i] = GUILayout.TextField(fields[i] ?? "", 16, fieldStyle, GUILayout.MinHeight(31 * SettingsStore.Current.Accessibility.TextScale)); GUILayout.Space(4);
            }
            float available = panel.width - 44;
            if (GUILayout.Button("Maße / Position anwenden", FitButton("Maße / Position anwenden", available, ui.Primary), GUILayout.Height(ButtonHeight + 3))) { ApplyFields(); GUI.FocusControl(null); }
            GUILayout.BeginHorizontal(); if (GUILayout.Button("↶ 15°", FitButton("↶ 15°", available / 2 - 5, ui.Button), GUILayout.Height(ButtonHeight))) RotateSelected(-15, out _); if (GUILayout.Button("15° ↷", FitButton("15° ↷", available / 2 - 5, ui.Button), GUILayout.Height(ButtonHeight))) RotateSelected(15, out _); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); if (GUILayout.Button("−90°", FitButton("−90°", available / 2 - 5, ui.Button), GUILayout.Height(ButtonHeight))) RotateSelected(-90, out _); if (GUILayout.Button("+90°", FitButton("+90°", available / 2 - 5, ui.Button), GUILayout.Height(ButtonHeight))) RotateSelected(90, out _); GUILayout.EndHorizontal();
            GUI.enabled = true; GUILayout.Space(9);
            GUILayout.Label("Frei platzieren · Shift halten:\n0,25-m-Raster / Kanten einrasten", ui.Small);
            if (GUILayout.Button("Raumplan speichern", FitButton("Raumplan speichern", available, ui.Primary), GUILayout.Height(ButtonHeight + 2))) SaveLayout(out _);
            if (GUILayout.Button("Gespeicherten Plan laden", FitButton("Gespeicherten Plan laden", available, ui.Button), GUILayout.Height(ButtonHeight + 2))) LoadLayout(out _);
            GUILayout.Space(10); GUILayout.Label("Linke Maus: verschieben\nGelbe Ecken: Größe ändern\nGrüner Ring: frei drehen\nShift: Raster / Kanten / Winkel\nMausrad: zoomen\nMittlere / rechte Maus: Plan ziehen\nEsc: Ziehen abbrechen / schließen", ui.Small);
            GUILayout.Label("Nur die Raumaufteilung wird gespeichert; Spielstände und Forschungen bleiben getrennt.", ui.Small);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private void HandleInput()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.control || e.command))
            {
                if (e.keyCode == KeyCode.Z) { if (e.shift) Redo(); else Undo(); e.Use(); }
                else if (e.keyCode == KeyCode.Y) { Redo(); e.Use(); }
                else if (e.keyCode == KeyCode.S) { SaveLayout(out _); e.Use(); }
            }
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && (GUI.GetNameOfFocusedControl() ?? "").StartsWith("room-plan-field-", StringComparison.Ordinal))
            { ApplyFields(); GUI.FocusControl(null); e.Use(); }
            if (e.type == EventType.ScrollWheel && PlanViewport.Contains(e.mousePosition))
            {
                Vector2 world = PlanScreenToWorld(e.mousePosition);
                zoom = Mathf.Clamp(zoom * Mathf.Pow(1.12f, -e.delta.y), .5f, 6); metresToPixels = BaseMetresToPixels * zoom;
                pan += e.mousePosition - WorldToPlanScreen(world); e.Use();
            }
            if (e.type == EventType.MouseDown && PlanViewport.Contains(e.mousePosition))
            {
                if (e.button == 0) { if (BeginPointerEdit(e.mousePosition)) e.Use(); }
                else if (e.button == 1 || e.button == 2) { panning = true; pointerStart = e.mousePosition; panStart = pan; GUI.FocusControl(null); e.Use(); }
            }
            HandlePointerGesture(e);
        }
        private void HandlePointerGesture(Event e)
        {
            if (e.type == EventType.MouseDrag)
            {
                if (pointerActive && e.button == 0) { DragPointer(e.mousePosition, out _); e.Use(); }
                else if (panning) { pan = panStart + e.mousePosition - pointerStart; e.Use(); }
            }
            if (e.type == EventType.MouseUp)
            { if (pointerActive && e.button == 0) { EndPointerEdit(); e.Use(); } if (panning) { panning = false; e.Use(); } }
        }
        private void DrawPlan()
        {
            SpaceMinerUi.Fill(PlanViewport, new Color(.018f, .035f, .052f)); SpaceMinerUi.Border(PlanViewport, new Color(.14f, .35f, .43f));
            GUI.BeginGroup(PlanViewport);
            try
            {
                Vector2 Local(Vector2 world) => WorldToPlanScreen(world) - PlanViewport.position;
                var outline = Layout.FloorOutline(); var points = new Vector2[outline.Length];
                for (int i = 0; i < points.Length; i++) points[i] = Local(outline[i]);
                FillPolygon(points, new Color(.052f, .10f, .13f));
                Color grid = new Color(.15f, .26f, .30f, .46f);
                Vector2 a = PlanScreenToWorld(PlanViewport.position), b = PlanScreenToWorld(new Vector2(PlanViewport.xMax, PlanViewport.yMax));
                int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x)), maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x));
                int minZ = Mathf.FloorToInt(Mathf.Min(a.y, b.y)), maxZ = Mathf.CeilToInt(Mathf.Max(a.y, b.y));
                for (int x = minX; x <= maxX; x++) Line(Local(new Vector2(x, minZ)), Local(new Vector2(x, maxZ)), x % 5 == 0 ? new Color(.24f, .42f, .46f, .55f) : grid);
                for (int z = minZ; z <= maxZ; z++) Line(Local(new Vector2(minX, z)), Local(new Vector2(maxX, z)), z % 5 == 0 ? new Color(.24f, .42f, .46f, .55f) : grid);
                for (int i = 0; i < points.Length; i++) Line(points[i], points[(i + 1) % points.Length], SpaceMinerUi.Cyan, 2);
                if (Layout.Definition.Keepouts != null)
                    foreach (var zone in Layout.Definition.Keepouts)
                    {
                        var footprint = CommandRoomLayout.Footprint(new CommandRoomLayoutItem { position = new Vector3(zone.center.x, 0, zone.center.y), size = new Vector3(zone.size.x, 1, zone.size.y), yaw = zone.yaw });
                        for (int i = 0; i < footprint.Length; i++) footprint[i] = Local(footprint[i]);
                        FillPolygon(footprint, new Color(.7f, .31f, .05f, .2f));
                        for (int i = 0; i < footprint.Length; i++) Line(footprint[i], footprint[(i + 1) % footprint.Length], SpaceMinerUi.Amber);
                        Bounds2(footprint, out Vector2 low, out Vector2 high);
                        string text = RouteCaption(zone.label);
                        var caption = new GUIStyle(boxLabel) { wordWrap = false };
                        float captionWidth = Mathf.Max(20, high.x - low.x - 8);
                        while (caption.fontSize > 10 && caption.CalcSize(new GUIContent(text)).x > captionWidth) caption.fontSize--;
                        bool vertical = high.y - low.y > (high.x - low.x) * 1.4f;
                        float captionY = vertical ? low.y + 15 : (low.y + high.y) * .5f - 14;
                        GUI.Label(new Rect(low.x + 4, captionY, captionWidth, 28), new GUIContent(text, zone.label), caption);
                    }
                foreach (var item in Layout.Items) DrawItem(item, false);
                if (rejectedItem != null) DrawItem(rejectedItem, true);
                foreach (Vector2[] guide in snapGuides) Line(Local(guide[0]), Local(guide[1]), SpaceMinerUi.Success, 2);
                var legend = new GUIStyle(boxLabel) { alignment = TextAnchor.UpperLeft };
                GUI.Label(new Rect(10, 7, PlanViewport.width - 20, 25), "GRUNDRISS · Raster 1 m · " + (Layout.Definition.IsAnnular ? "gebogener Raum" : "rechteckiger Raum"), legend);
                bool shift = pointerActive ? shiftSnap : (Event.current != null && Event.current.shift) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                GUI.Label(new Rect(10, 32, PlanViewport.width - 20, 25), pointerRotate ? "DREHEN · " + (shift ? RotationReference : "frei · Shift richtet aus") : shift ? "SHIFT · EINRASTEN (0,25 m / Kanten)" : "FREI · Shift zum Einrasten halten", legend);
                float scale = 2 * metresToPixels;
                Line(new Vector2(17, PlanViewport.height - 22), new Vector2(17 + scale, PlanViewport.height - 22), SpaceMinerUi.Cyan, 3);
                GUI.Label(new Rect(17, PlanViewport.height - 49, 90, 25), "2 Meter", ui.Small);
                if (GUI.Button(new Rect(PlanViewport.width - 137, PlanViewport.height - 52, 126, 41), "Einpassen", FitButton("Einpassen", 126, ui.Button))) { zoom = 1; pan = Vector2.zero; }
            }
            finally { GUI.EndGroup(); }
        }
        private Rect RectFromWorld(Vector2 a, Vector2 b)
        {
            a = WorldToPlanScreen(a) - PlanViewport.position; b = WorldToPlanScreen(b) - PlanViewport.position;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
        private void DrawItem(CommandRoomLayoutItem item, bool rejected)
        {
            bool selected = item.id == SelectedId;
            Color edge = rejected ? SpaceMinerUi.Error : selected ? SpaceMinerUi.Amber : SpaceMinerUi.Cyan;
            Vector2 centre = WorldToPlanScreen(new Vector2(item.position.x, item.position.z)) - PlanViewport.position;
            Vector2[] corners = CommandRoomLayout.Footprint(item);
            for (int i = 0; i < corners.Length; i++) corners[i] = WorldToPlanScreen(corners[i]) - PlanViewport.position;
            FillPolygon(corners, rejected ? new Color(.7f, .12f, .1f, .3f) : selected ? new Color(.36f, .25f, .1f, .92f) : new Color(.08f, .23f, .28f, .95f));
            for (int i = 0; i < corners.Length; i++) Line(corners[i], corners[(i + 1) % corners.Length], edge, 1.3f);
            // Footprint corners 0–1 are the physical -Z/front display face.
            Line(Vector2.Lerp(corners[0], corners[1], .08f), Vector2.Lerp(corners[0], corners[1], .92f), edge, 3);
            if (rejected) return;
            Bounds2(corners, out Vector2 low, out Vector2 high);
            float labelWidth = Mathf.Clamp(Mathf.Max(high.x - low.x, 96), 96, 170);
            var style = new GUIStyle(boxLabel); style.normal.textColor = selected ? Color.white : new Color(.77f, .93f, 1);
            string title = ShortLabel(item.id), dimensions = item.size.x.ToString("0.#") + " × " + item.size.z.ToString("0.#") + " m";
            while (style.fontSize > 11 && Mathf.Max(style.CalcSize(new GUIContent(title)).x, style.CalcSize(new GUIContent(dimensions)).x) > labelWidth - 4) style.fontSize--;
            GUI.Label(new Rect(Mathf.Clamp(centre.x - labelWidth * .5f, 2, PlanViewport.width - labelWidth - 2), centre.y - 24, labelWidth, 49), title + "\n" + dimensions, style);
            if (selected)
            {
                foreach (Vector2 corner in GetItemPlanPolygon(item.id))
                { Vector2 p = corner - PlanViewport.position; SpaceMinerUi.Fill(new Rect(p.x - 5, p.y - 5, 10, 10), edge); SpaceMinerUi.Border(new Rect(p.x - 6, p.y - 6, 12, 12), new Color(.025f, .04f, .06f)); }
                DrawRotationHandle(item);
            }
        }
        private void DrawRotationHandle(CommandRoomLayoutItem item)
        {
            Vector2 point = GetRotationHandlePlanPoint() - PlanViewport.position;
            Vector2 edge = FrontEdgePlanPoint(item) - PlanViewport.position;
            Line(edge, point, SpaceMinerUi.Success, 2);
            var circle = new Vector2[24];
            for (int i = 0; i < circle.Length; i++) { float angle = i * Mathf.PI * 2 / circle.Length; circle[i] = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 12; }
            FillPolygon(circle, new Color(.025f, .09f, .07f, 1));
            for (int i = 0; i < circle.Length; i++) Line(circle[i], circle[(i + 1) % circle.Length], SpaceMinerUi.Success, 2);
            var icon = new GUIStyle(boxLabel) { fontSize = 19, alignment = TextAnchor.MiddleCenter }; icon.normal.textColor = SpaceMinerUi.Success;
            GUI.Label(new Rect(point.x - 12, point.y - 13, 24, 26), "↻", icon);
            var caption = new GUIStyle(boxLabel); caption.normal.textColor = SpaceMinerUi.Success;
            GUI.Label(new Rect(Mathf.Clamp(point.x - 45, 2, PlanViewport.width - 92), Mathf.Clamp(point.y - 39, 3, PlanViewport.height - 25), 90, 24), "Drehen", caption);
        }
        private static void Line(Vector2 a, Vector2 b, Color color, float width = 1)
        {
            Vector2 delta = b - a; if (delta.sqrMagnitude < .001f) return;
            // Do not rotate GUI.matrix inside a GUI.BeginGroup: Unity's clip stack
            // then clips the rotated bounding box against the wrong coordinate space.
            // Axis-aligned spans are cheap; short unrotated spans rasterise diagonals.
            if (Mathf.Abs(delta.x) < .01f) { SpaceMinerUi.Fill(new Rect(a.x - width * .5f, Mathf.Min(a.y, b.y), width, Mathf.Abs(delta.y)), color); return; }
            if (Mathf.Abs(delta.y) < .01f) { SpaceMinerUi.Fill(new Rect(Mathf.Min(a.x, b.x), a.y - width * .5f, Mathf.Abs(delta.x), width), color); return; }
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) / 1.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
                SpaceMinerUi.Fill(new Rect(p.x - width * .5f, p.y - width * .5f, Mathf.Max(width, 1.6f), Mathf.Max(width, 1.6f)), color);
            }
        }
        private void FillPolygon(Vector2[] vertices, Color color)
        {
            if (Event.current.type != EventType.Repaint || vertices.Length < 3) return;
            // Scan conversion keeps the actual curved footprint filled while allowing
            // IMGUI's group to clip zoomed and panned edges without another camera.
            var hits = new List<float>(8);
            float low = PlanViewport.height, high = 0;
            foreach (Vector2 point in vertices) { low = Mathf.Min(low, point.y); high = Mathf.Max(high, point.y); }
            // Convert each physical pixel row exactly once. Fractional GUI-space
            // strips can round inward at 90% UI scale and leave visible dark gaps;
            // overlapping whole rows would instead stripe translucent previews.
            float pixelScale = Mathf.Max(.01f, Mathf.Abs(GUI.matrix.m11));
            int firstRow = Mathf.FloorToInt((PlanViewport.y + Mathf.Max(0, low)) * pixelScale);
            int lastRow = Mathf.CeilToInt((PlanViewport.y + Mathf.Min(PlanViewport.height, high)) * pixelScale);
            for (int row = firstRow; row < lastRow; row++)
            {
                float y = (row + .5f) / pixelScale - PlanViewport.y;
                hits.Clear();
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector2 a = vertices[i], b = vertices[(i + 1) % vertices.Length];
                    if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y)) hits.Add(a.x + (y - a.y) * (b.x - a.x) / (b.y - a.y));
                }
                hits.Sort();
                for (int i = 0; i + 1 < hits.Count; i += 2)
                {
                    float left = Mathf.Floor((PlanViewport.x + hits[i]) * pixelScale) / pixelScale - PlanViewport.x;
                    float right = Mathf.Ceil((PlanViewport.x + hits[i + 1]) * pixelScale) / pixelScale - PlanViewport.x;
                    SpaceMinerUi.Fill(new Rect(left, row / pixelScale - PlanViewport.y, right - left, 1 / pixelScale + .001f), color);
                }
            }
        }
    }
}

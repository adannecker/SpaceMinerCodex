using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public enum TacticalOverviewTab { Objects, Drones, Quests }
    [Flags] public enum TacticalObjectFilter { None = 0, Station = 1, Asteroids = 2, Wrecks = 4, All = 7 }

    public sealed class TacticalContact
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Kind { get; internal set; }
        public string Knowledge { get; internal set; }
        public Vector3 Position { get; internal set; }
        public float DiameterMeters { get; internal set; }
    }
    public sealed class TacticalContactPage
    {
        public IReadOnlyList<TacticalContact> Items { get; internal set; }
        public int TotalCount { get; internal set; }
        public int PageIndex { get; internal set; }
        public int PageCount { get; internal set; }
        public int PageSize { get; internal set; }
    }
    public sealed class TacticalDroneView
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Status { get; internal set; }
        public string Supplies { get; internal set; }
        public float Progress { get; internal set; }
        public Vector3 Position { get; internal set; }
    }
    public sealed class TacticalQuestView
    {
        public int Index { get; internal set; }
        public string Title { get; internal set; }
        public string Detail { get; internal set; }
        public float Progress { get; internal set; }
        public bool Complete { get; internal set; }
        public bool Current { get; internal set; }
    }

    // Read models and camera navigation only. Work remains at the physical terminals.
    public sealed partial class StationOperations
    {
        private struct TacticalReference { public TacticalObjectFilter Kind; public int Index; }
        private readonly List<TacticalReference> tacticalMatches = new List<TacticalReference>();
        private TacticalContactPage tacticalPage;
        private TacticalObjectFilter tacticalFilter = TacticalObjectFilter.All, tacticalIndexedFilter;
        private string tacticalSearch = "", tacticalIndexedSearch, tacticalSelectedObject;
        private int tacticalPageIndex, tacticalIndexedKnown = -1, tacticalIndexedElements = -1, tacticalIndexedCollected = -1;
        private bool tacticalIndexedScan;
        private StationCampaignState tacticalIndexedState;
        private AsteroidResource[] tacticalIndexedAsteroids;
        private float tacticalPageTime;
        private Vector2 tacticalScroll;
        private GUIStyle tacticalSearchStyle;
        private int tacticalSearchFont;
        private bool tacticalSearchFocused, tacticalRequestSearchFocus, tacticalReleaseSearchFocus;
        private Rect tacticalRenderedBounds, tacticalSearchBounds;
        public TacticalOverviewTab TacticalTab { get; private set; }
        private bool TacticalSearchVisible => Live && Scenario != null && !StationInteriorMode.IsInside && !PanelOpen
            && !ForeignModal && Scenario.Scanner.Dialogue == null && TacticalTab == TacticalOverviewTab.Objects
            && Camera.main != null && Camera.main.GetComponent<OrbitCamera>()?.ShowHud == true;
        public static bool TacticalTextInputActive => Current != null && Current.TacticalSearchVisible && Current.tacticalSearchFocused;
        public void FocusTacticalSearch()
        {
            SelectTacticalTab(TacticalOverviewTab.Objects);
            if (TacticalSearchVisible) { tacticalRequestSearchFocus = true; tacticalReleaseSearchFocus = false; }
        }
        public void ReleaseTacticalSearchFocus()
        {
            bool owned = tacticalSearchFocused || tacticalRequestSearchFocus;
            tacticalSearchFocused = tacticalRequestSearchFocus = false;
            if (owned)
            {
                // IMGUI retains its named focus across a skipped OnGUI/HUD pass.
                // Clear it in the same GUI context when the field is drawn again.
                tacticalReleaseSearchFocus = true;
                GUIUtility.keyboardControl = 0;
            }
        }
        internal void UpdateTacticalSearchInput()
            => HandleTacticalSearchInput(UiLayout.Point(Input.mousePosition), Input.GetKeyDown(KeyCode.Escape), Input.GetMouseButtonDown(0));
        internal bool HandleTacticalSearchInput(Vector2 point, bool escape, bool click)
        {
            if (!tacticalSearchFocused && !tacticalRequestSearchFocus) return false;
            if (TacticalSearchVisible && !escape && (!click || tacticalSearchBounds.Contains(point))) return false;
            ReleaseTacticalSearchFocus(); return true;
        }
        public string TacticalSelectedObject => tacticalSelectedObject;
        public int TacticalQueryRevision { get; private set; }
        public Rect TacticalOverviewBounds => new Rect(18, ResourcesRect.yMax + 12,
            Mathf.Min(420, UiLayout.Width - 36), Mathf.Max(140, OutsideVitalsRect.yMin - 18 - (ResourcesRect.yMax + 12)));
        public bool TacticalOwnsScreenPoint(Vector2 point) => !StationInteriorMode.IsInside && TacticalOverviewBounds.Contains(point);
        public string TacticalQuestHeader
        {
            get { int done = 0; for (int i = 0; i < questTitles.Length; i++) if (QuestDone(i)) done++; return "AUFGABEN · " + done + "/" + questTitles.Length; }
        }
        private bool TacticalNavigationAvailable => Live && Scenario != null && !StationInteriorMode.IsInside && Camera.main != null;
        public void SelectTacticalTab(TacticalOverviewTab tab)
        {
            if (!Enum.IsDefined(typeof(TacticalOverviewTab), tab)) return;
            if (TacticalTab != tab) { tacticalScroll = Vector2.zero; ReleaseTacticalSearchFocus(); }
            TacticalTab = tab;
        }
        public void RefreshTacticalContacts() { tacticalIndexedState = null; tacticalPage = null; }
        public void SetTacticalContactQuery(TacticalObjectFilter filter, string search = "")
        {
            tacticalFilter = filter & TacticalObjectFilter.All; tacticalSearch = search ?? "";
            tacticalPageIndex = 0; tacticalScroll = Vector2.zero; SelectTacticalTab(TacticalOverviewTab.Objects);
        }

        // Cache matching indices, not GUI rows or a second copy of asteroid domain data.
        // A new scan/query rebuilds the index once. A repaint materialises at most24 contacts.
        public TacticalContactPage GetTacticalContacts(TacticalObjectFilter filter = TacticalObjectFilter.All,
            string search = "", int page = 0, int pageSize = 8)
        {
            search = (search ?? "").Trim(); filter &= TacticalObjectFilter.All; pageSize = Mathf.Clamp(pageSize, 1, 24);
            int known = Scenario?.Scanner?.KnownCount ?? 0;
            bool scanned = Scenario?.Scanner?.FirstScanComplete ?? false;
            var asteroids = Scenario?.Asteroids;
            bool changed = tacticalIndexedState != State || tacticalIndexedAsteroids != asteroids
                || tacticalIndexedElements != State.elements.Count || tacticalIndexedCollected != State.collected.Count
                || tacticalIndexedKnown != known || tacticalIndexedScan != scanned || tacticalIndexedFilter != filter
                || !string.Equals(tacticalIndexedSearch, search, StringComparison.OrdinalIgnoreCase);
            if (changed)
            {
                tacticalMatches.Clear();
                if ((filter & TacticalObjectFilter.Station) != 0)
                {
                    if (TacticalSearchMatch("Raumstation", "Station", search)) tacticalMatches.Add(new TacticalReference { Kind = TacticalObjectFilter.Station, Index = -1 });
                    if (TacticalSearchMatch("Drohnenbay", "Außendock", search)) tacticalMatches.Add(new TacticalReference { Kind = TacticalObjectFilter.Station, Index = -2 });
                    foreach (var bay in BatteryBays)
                        if (TacticalSearchMatch("Akkubucht " + (bay.Index + 1) + " / " + bay.Caption, "Speicher", search))
                            tacticalMatches.Add(new TacticalReference { Kind = TacticalObjectFilter.Station, Index = -3 - bay.Index });
                    for (int i = 0; i < State.elements.Count; i++)
                    {
                        var element = State.elements[i];
                        if (element.kind == "Bergung" || element.kind == "Rohstoff" || WreckId(element.id)) continue;
                        if (TacticalSearchMatch(element.name, element.kind, search)) tacticalMatches.Add(new TacticalReference { Kind = TacticalObjectFilter.Station, Index = i });
                    }
                }
                if ((filter & TacticalObjectFilter.Asteroids) != 0 && asteroids != null)
                    for (int i = 0; i < asteroids.Length; i++)
                    {
                        var rock = asteroids[i];
                        // DebugVisibility is intentionally not a source of knowledge.
                        if (rock != null && rock.IsScanned && TacticalSearchMatch(rock.Info.DisplayName, "Asteroid", search))
                            tacticalMatches.Add(new TacticalReference { Kind = TacticalObjectFilter.Asteroids, Index = i });
                    }
                if ((filter & TacticalObjectFilter.Wrecks) != 0)
                    for (int i = 0; i < 5; i++)
                    {
                        var element = Element("debris-" + i);
                        if (element != null && IsDebrisKnown(element.id) && !State.collected.Contains(element.id)
                            && TacticalSearchMatch(element.name, "Wrackkontakt", search))
                            tacticalMatches.Add(new TacticalReference { Kind = TacticalObjectFilter.Wrecks, Index = i });
                    }
                tacticalIndexedState = State; tacticalIndexedAsteroids = asteroids;
                tacticalIndexedElements = State.elements.Count; tacticalIndexedCollected = State.collected.Count;
                tacticalIndexedKnown = known; tacticalIndexedScan = scanned; tacticalIndexedFilter = filter; tacticalIndexedSearch = search;
                tacticalPage = null; TacticalQueryRevision++;
            }
            int pages = Math.Max(1, (tacticalMatches.Count + pageSize - 1) / pageSize);
            page = Mathf.Clamp(page, 0, pages - 1);
            if (tacticalPage != null && tacticalPage.PageIndex == page && tacticalPage.PageSize == pageSize && Time.unscaledTime < tacticalPageTime + .25f)
                return tacticalPage;
            var items = new List<TacticalContact>(pageSize);
            for (int i = page * pageSize; i < Math.Min(tacticalMatches.Count, (page + 1) * pageSize); i++)
            {
                var reference = tacticalMatches[i];
                string id = reference.Kind == TacticalObjectFilter.Asteroids ? "asteroid-" + reference.Index
                    : reference.Kind == TacticalObjectFilter.Wrecks ? "debris-" + reference.Index
                    : reference.Index == -1 ? "station" : reference.Index == -2 ? "drone-bay"
                    : reference.Index <= -3 ? "battery-bay-" + (-3 - reference.Index) : State.elements[reference.Index].id;
                if (TryTacticalContact(id, out var contact)) items.Add(contact);
            }
            tacticalPageTime = Time.unscaledTime;
            return tacticalPage = new TacticalContactPage { Items = items.AsReadOnly(), TotalCount = tacticalMatches.Count,
                PageIndex = page, PageCount = pages, PageSize = pageSize };
        }
        private static bool TacticalSearchMatch(string name, string kind, string search) => search.Length == 0
            || (name ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || (kind ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        private bool TryTacticalContact(string id, out TacticalContact contact)
        {
            contact = null; if (Scenario == null || Layout == null || string.IsNullOrEmpty(id)) return false;
            if (id.StartsWith("asteroid-", StringComparison.Ordinal))
            {
                if (!int.TryParse(id.Substring(9), out int index) || index < 0 || index >= Scenario.Asteroids.Length) return false;
                var rock = Scenario.Asteroids[index]; if (rock == null || !rock.IsScanned) return false;
                contact = new TacticalContact { Id = id, Name = rock.Info.DisplayName, Kind = "Asteroid", Position = rock.transform.position,
                    DiameterMeters = rock.Info.DiameterMeters, Knowledge = rock.WaterIdentified
                        ? "Wasser " + rock.KnownWaterPercent.ToString("0") + " % · Rest unbekannt" : "Form / Ortung bekannt · Zusammensetzung unbekannt" };
                return true;
            }
            if (WreckId(id))
            {
                var root = DebrisRoot(id); var element = Element(id);
                if (root == null || element == null || !IsDebrisKnown(id) || State.collected.Contains(id)) return false;
                var bounds = new Bounds(root.position, Vector3.zero);
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) bounds.Encapsulate(collider.bounds);
                contact = new TacticalContact { Id = id, Name = element.name, Kind = "Wrackkontakt", Position = root.position,
                    DiameterMeters = Mathf.Max(1.5f, bounds.size.magnitude), Knowledge = "Form / Position bekannt · Masse und Inhalt unbekannt" };
                return true;
            }
            if (id == "station" || id == "drone-bay")
            {
                contact = new TacticalContact { Id = id, Name = id == "station" ? "Raumstation" : "Drohnenbay", Kind = "Station",
                    Position = Layout.transform.TransformPoint(id == "station" ? new Vector3(0, 2, 0) : RingWalkthrough.At(72, 30, 1)),
                    DiameterMeters = id == "station" ? 170 : 30, Knowledge = id == "station" ? "Gesamte Station" : "Außendock / Versorgung der Flotte" };
                return true;
            }
            var batteryBay = BatteryBay(id);
            if (batteryBay != null)
            {
                contact = new TacticalContact { Id = id, Name = "Akkubucht " + (batteryBay.Index + 1) + " / " + batteryBay.Caption,
                    Kind = "Speicher", Position = batteryBay.transform.TransformPoint(batteryBay.ModelBounds.center), DiameterMeters = 5,
                    Knowledge = batteryBay.Index == StationBatteryPlacement.ActiveIndex ? State.power.storedKwh.ToString("0.0") + " / " + State.power.capacityKwh.ToString("0") + " kWh / Stationsnetz"
                        : batteryBay.Installed ? "Installiert / elektrisch getrennt" : "Freier Einbauplatz" };
                return true;
            }
            var e = Element(id); if (e == null || e.kind == "Bergung" || e.kind == "Rohstoff") return false;
            Vector3 position;
            var block = RoomPlan?.BoundRoot(id);
            if (id == "battery") position = BatteryServicePoint;
            else if (block != null) position = block.position + Layout.transform.up;
            else position = TacticalStationPosition(e);
            float diameter = e.kind == "Ringsegment" ? 17 : e.id == "reactor" ? 12 : e.id.StartsWith("solar-", StringComparison.Ordinal) ? 30 : 24;
            contact = new TacticalContact { Id = id, Name = e.name, Kind = e.kind, Position = position, DiameterMeters = diameter,
                Knowledge = e.built ? "Zustand " + (e.condition * 100).ToString("0") + " % · " + e.use : "Ungebauter Anbauslot" };
            return true;
        }
        private Vector3 TacticalStationPosition(StationElementState element)
        {
            float radius = 56, angle = element.sector * 30;
            if (element.id.StartsWith("sector-", StringComparison.Ordinal)) radius = 33.5f;
            else if (element.id.StartsWith("expansion-", StringComparison.Ordinal)) { radius = 42; angle = ExpansionAngle(element); }
            else if (element.id.StartsWith("water-", StringComparison.Ordinal)) { radius = 46; angle = ExpansionAngle(element); }
            else if (element.id == "water") { radius = 46; angle = RingWalkthrough.StorageAngle(0); }
            else if (element.id.StartsWith("service-", StringComparison.Ordinal))
            { RingWalkthrough.ServiceBreak(int.Parse(element.id.Substring(8)), out float from, out float to); radius = (from + to) * .5f; }
            else if (element.id == "reactor") radius = 0;
            else if (element.id.StartsWith("solar-", StringComparison.Ordinal)) radius = 60;
            else if (element.id == "hall" || element.id == "command") radius = 46;
            else if (element.id == "helper") radius = 24.5f;
            return Layout.transform.TransformPoint(RingWalkthrough.At(radius, angle, 2));
        }
        public bool SelectTacticalObject(string id)
        {
            if (!TacticalNavigationAvailable || !TryTacticalContact(id, out _)) return false;
            tacticalSelectedObject = id; SelectTacticalTab(TacticalOverviewTab.Objects); return true;
        }
        public bool FocusTacticalContact(string id)
        {
            if (!TacticalNavigationAvailable || !TryTacticalContact(id, out var contact)) return false;
            var orbit = Camera.main.GetComponent<OrbitCamera>(); if (orbit == null) return false;
            if (id.StartsWith("asteroid-", StringComparison.Ordinal)) orbit.Focus(Scenario.Asteroids[int.Parse(id.Substring(9))].Info);
            else if (!orbit.FocusPoint(contact.Position, contact.DiameterMeters)) return false;
            tacticalSelectedObject = id; SelectTacticalTab(TacticalOverviewTab.Objects); return true;
        }
        public bool FocusTacticalDrone(string id)
        {
            if (!TacticalNavigationAvailable) return false;
            var orbit = Camera.main.GetComponent<OrbitCamera>(); if (orbit == null) return false;
            if (id == "R-01") { if (!State.helper || Layout.RepairDrone == null) return false; orbit.FocusPoint(Layout.RepairDrone.position, 1.2f); }
            else { var drone = TacticalDrone(id); if (drone == null || !drone.IsOperational) return false; orbit.Focus(drone.Info); }
            SelectTacticalTab(TacticalOverviewTab.Drones); return true;
        }
        public bool FollowTacticalDrone(string id)
        {
            if (!TacticalNavigationAvailable) return false;
            var orbit = Camera.main.GetComponent<OrbitCamera>(); if (orbit == null) return false;
            bool followed = id == "R-01" ? orbit.FollowInternalDrone(Layout.RepairDrone) : orbit.FollowDrone(TacticalDrone(id));
            if (followed) SelectTacticalTab(TacticalOverviewTab.Drones); return followed;
        }
        public void StopTacticalFollow() { if (TacticalNavigationAvailable) Camera.main.GetComponent<OrbitCamera>()?.StopFollow(); }
        private DroneAgent TacticalDrone(string id)
        {
            if (id == null || !id.StartsWith("drone-", StringComparison.Ordinal) || !int.TryParse(id.Substring(6), out int index)
                || index < 0 || index >= Scenario.Drones.Length) return null;
            return Scenario.Drones[index];
        }
        public IReadOnlyList<TacticalDroneView> GetTacticalDrones()
        {
            var result = new List<TacticalDroneView>(); if (Scenario == null) return result;
            for (int i = 0; i < Scenario.Drones.Length; i++)
            {
                var drone = Scenario.Drones[i]; if (drone == null || !drone.IsOperational) continue;
                var work = DroneWork(drone);
                result.Add(new TacticalDroneView { Id = "drone-" + i, Name = drone.Info.DisplayName,
                    Status = work == null ? drone.Status : StationRecipes.Get(work.kind).Name + " · " + work.status,
                    Progress = work == null ? drone.PhaseProgress : Mathf.Clamp01(work.elapsed / StationRecipes.Get(work.kind).Seconds), Position = drone.transform.position,
                    Supplies = "Akku " + drone.BatteryKwh.ToString("0.0") + "/" + drone.BatteryCapacityKwh.ToString("0") + " kWh · Wasser "
                        + drone.FuelLiters.ToString("0.0") + "/" + drone.FuelCapacityLiters.ToString("0") + " L\nFracht " + drone.CargoKg.ToString("0.0")
                        + " kg · " + drone.Speed.ToString("0.0") + " m/s · Ziel " + OrbitCamera.FormatDistance(drone.TargetDistance) });
            }
            if (State.helper && Layout.RepairDrone != null)
            {
                var work = State.orders.Find(order => order.started && !order.complete && order.actorId == "R-01");
                result.Add(new TacticalDroneView { Id = "R-01", Name = "R-01 · interne Drohne", Position = Layout.RepairDrone.position,
                    Status = work == null ? "Bereit an der internen Werkstatt" : StationRecipes.Get(work.kind).Name + " · " + work.status,
                    Progress = work == null ? 0 : Mathf.Clamp01(work.elapsed / StationRecipes.Get(work.kind).Seconds),
                    Supplies = "Stationsintern · Versorgung aus dem Stationsnetz" });
            }
            return result.AsReadOnly();
        }
        private float TacticalJobProgress(StationJobKind kind)
        {
            float progress = 0; foreach (var work in State.orders)
                if (work.kind == kind && !work.cancelled && !work.complete && work.started)
                    progress = Mathf.Max(progress, Mathf.Clamp01(work.elapsed / StationRecipes.Get(kind).Seconds));
            return progress;
        }
        public IReadOnlyList<TacticalQuestView> GetTacticalQuests()
        {
            var result = new List<TacticalQuestView>(); if (Scenario == null) return result;
            var kinds = new[] { StationJobKind.Diagnose, StationJobKind.Salvage, StationJobKind.ActivateHelper, StationJobKind.Carbon,
                StationJobKind.Food, StationJobKind.ActivateSecond, StationJobKind.Repair };
            for (int i = 0; i < questTitles.Length; i++)
            {
                bool done = QuestDone(i); float progress = done ? 1 : i < 2 ? 0 : TacticalJobProgress(kinds[i - 2]);
                string detail;
                if (i == 0) detail = done ? "Nahbereich gescannt · " + Scenario.Scanner.KnownCount + " Asteroiden bekannt"
                    : "Scanner " + (Scenario.Scanner.Charge * 100).ToString("0") + " % geladen · Scan am Stationspult auslösen";
                else if (i == 1)
                {
                    bool delivery = Scenario.Deliveries > 0; float water = Mathf.Clamp01(Scenario.WaterLiters / 40f);
                    if (!done) progress = .5f * ((delivery ? 1 : 0) + water);
                    detail = (delivery ? "✓ Erste Rohlieferung" : "○ Erste Rohlieferung fehlt") + "\n"
                        + Scenario.WaterLiters.ToString("0.0") + "/40 L sauberes Verbrauchswasser" + (done ? " · Ziel bereits gesichert" : " · Aufbereitung benötigt Strom");
                }
                else if (done) detail = "Abgeschlossen";
                else if (i == 2) detail = "Versorgungsdiagnose am Stationsplanungspult";
                else if (i == 3) detail = State.debrisBatches + "/1 Wrack geborgen · Auftrag am Drohnenpult";
                else if (i == 4) detail = "R-01 an der internen Werkstatt in Betrieb nehmen";
                else if (i == 5) detail = State.carbonBatches + "/1 Kohlenstoffprobe geborgen";
                else if (i == 6) detail = "Grundnahrung mit Wasser, Kohlenstoff und endlicher Kartusche herstellen";
                else if (i == 7) detail = "Drohne02 mit Teilen, R-01 und realer Stationsreserve instand setzen";
                else detail = "Reparaturrolle erforschen und ein Stationselement tatsächlich reparieren";
                result.Add(new TacticalQuestView { Index = i, Title = questTitles[i], Detail = detail, Progress = Mathf.Clamp01(progress), Complete = done, Current = i == CurrentQuest });
            }
            return result.AsReadOnly();
        }

        public void DrawTacticalOverview(Rect bounds)
        {
            if (StationInteriorMode.IsInside || Scenario == null) return;
            tacticalRenderedBounds = bounds;
            ui.Configure(SettingsStore.Current.Accessibility); ui.Panel(bounds);
            GUILayout.BeginArea(new Rect(bounds.x + 12, bounds.y + 10, bounds.width - 24, bounds.height - 20));
            GUILayout.Label("TAKTISCHE ÜBERSICHT", ui.Text);
            GUILayout.Label("Navigation / Beobachtung · Arbeitsaufträge am Drohnenpult", ui.Small);
            var tabs = GUILayoutUtility.GetRect(10, Mathf.Max(38, 38 * SettingsStore.Current.Accessibility.TextScale), GUILayout.ExpandWidth(true));
            float tabWidth = (tabs.width - 8) / 3;
            if (ui.Tab(new Rect(tabs.x, tabs.y, tabWidth, tabs.height), "Objekte", TacticalTab == TacticalOverviewTab.Objects)) SelectTacticalTab(TacticalOverviewTab.Objects);
            if (ui.Tab(new Rect(tabs.x + tabWidth + 4, tabs.y, tabWidth, tabs.height), "Drohnen", TacticalTab == TacticalOverviewTab.Drones)) SelectTacticalTab(TacticalOverviewTab.Drones);
            if (ui.Tab(new Rect(tabs.x + (tabWidth + 4) * 2, tabs.y, tabWidth, tabs.height), "Aufgaben", TacticalTab == TacticalOverviewTab.Quests)) SelectTacticalTab(TacticalOverviewTab.Quests);
            if (TacticalTab == TacticalOverviewTab.Objects) DrawTacticalObjects();
            else if (TacticalTab == TacticalOverviewTab.Drones) DrawTacticalDrones();
            else DrawTacticalQuests();
            GUILayout.EndArea();
        }
        private void DrawTacticalObjects()
        {
            var filters = GUILayoutUtility.GetRect(10, Mathf.Max(36, 34 * SettingsStore.Current.Accessibility.TextScale), GUILayout.ExpandWidth(true));
            float width = (filters.width - 8) / 3;
            TacticalFilterButton(new Rect(filters.x, filters.y, width, filters.height), "Station", TacticalObjectFilter.Station);
            TacticalFilterButton(new Rect(filters.x + width + 4, filters.y, width, filters.height), "Asteroiden", TacticalObjectFilter.Asteroids);
            TacticalFilterButton(new Rect(filters.x + (width + 4) * 2, filters.y, width, filters.height), "Wracks", TacticalObjectFilter.Wrecks);
            int font = Mathf.RoundToInt(16 * SettingsStore.Current.Accessibility.TextScale);
            if (tacticalSearchStyle == null || tacticalSearchFont != font)
            { tacticalSearchFont = font; tacticalSearchStyle = new GUIStyle(GUI.skin.textField) { fontSize = font, padding = new RectOffset(8, 8, 6, 6) }; }
            GUILayout.BeginHorizontal(); GUILayout.Label("Suche", ui.Small, GUILayout.Width(54));
            GUI.SetNextControlName("StationTacticalSearch");
            string search = GUILayout.TextField(tacticalSearch, 80, tacticalSearchStyle, GUILayout.MinHeight(font + 14));
            if (tacticalReleaseSearchFocus)
            {
                GUI.FocusControl(null); GUIUtility.keyboardControl = 0;
                if (Event.current.type == EventType.Repaint) tacticalReleaseSearchFocus = false;
                tacticalSearchFocused = false;
            }
            else
            {
                if (tacticalRequestSearchFocus) { GUI.FocusControl("StationTacticalSearch"); tacticalRequestSearchFocus = false; }
                tacticalSearchFocused = GUI.GetNameOfFocusedControl() == "StationTacticalSearch";
            }
            if (Event.current.type == EventType.Repaint)
            {
                tacticalSearchBounds = GUILayoutUtility.GetLastRect();
                tacticalSearchBounds.position += new Vector2(tacticalRenderedBounds.x + 12, tacticalRenderedBounds.y + 10);
            }
            GUILayout.EndHorizontal();
            if (search != tacticalSearch) { tacticalSearch = search; tacticalPageIndex = 0; tacticalScroll = Vector2.zero; }
            var page = GetTacticalContacts(tacticalFilter, tacticalSearch, tacticalPageIndex); tacticalPageIndex = page.PageIndex;
            GUILayout.Label(page.TotalCount + " bekannte Kontakte · keine ungescannten Ressourcen", ui.Small);
            tacticalScroll = GUILayout.BeginScrollView(tacticalScroll);
            if (page.TotalCount == 0) GUILayout.Label("Keine passenden Kontakte. Am Stationspult scannen oder Filter/Suche ändern.", ui.Small);
            foreach (var contact in page.Items)
            {
                GUILayout.Label(contact.Name + " · " + contact.Kind, ui.Text);
                GUILayout.Label(contact.Knowledge + "\nGröße " + OrbitCamera.FormatDistance(contact.DiameterMeters), ui.Small);
                if (GUILayout.Button((tacticalSelectedObject == contact.Id ? "✓ " : "") + "Fokus", ui.Button)) FocusTacticalContact(contact.Id);
                GUILayout.Space(7);
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal(); bool old = GUI.enabled; GUI.enabled = old && page.PageIndex > 0;
            if (GUILayout.Button("‹", ui.Button, GUILayout.Width(46))) { tacticalPageIndex--; tacticalScroll = Vector2.zero; }
            GUI.enabled = old; GUILayout.Label((page.PageIndex + 1) + " / " + page.PageCount, ui.Small);
            GUI.enabled = old && page.PageIndex + 1 < page.PageCount;
            if (GUILayout.Button("›", ui.Button, GUILayout.Width(46))) { tacticalPageIndex++; tacticalScroll = Vector2.zero; }
            GUI.enabled = old; GUILayout.EndHorizontal();
        }
        private void TacticalFilterButton(Rect bounds, string label, TacticalObjectFilter bit)
        {
            bool on = (tacticalFilter & bit) != 0;
            if (ui.Tab(bounds, label, on))
            { tacticalFilter ^= bit; tacticalPageIndex = 0; tacticalScroll = Vector2.zero; }
        }
        private void DrawTacticalDrones()
        {
            var orbit = Camera.main.GetComponent<OrbitCamera>();
            if (orbit != null && orbit.IsFollowing && GUILayout.Button("Folgen beenden · Kamera freigeben", ui.Primary)) StopTacticalFollow();
            var drones = GetTacticalDrones(); GUILayout.Label(drones.Count + " einsatzfähige Einheiten", ui.Small);
            tacticalScroll = GUILayout.BeginScrollView(tacticalScroll);
            foreach (var drone in drones)
            {
                GUILayout.Label(drone.Name, ui.Text); GUILayout.Label(drone.Status + "\n" + drone.Supplies, ui.Small);
                var bar = GUILayoutUtility.GetRect(10, 5, GUILayout.ExpandWidth(true)); SpaceMinerUi.Fill(bar, new Color(.07f, .14f, .19f)); SpaceMinerUi.Fill(new Rect(bar.x, bar.y, bar.width * drone.Progress, bar.height), SpaceMinerUi.Cyan);
                GUILayout.BeginHorizontal(); if (GUILayout.Button("Fokus", ui.Button)) FocusTacticalDrone(drone.Id);
                if (GUILayout.Button("Folgen", ui.Button)) FollowTacticalDrone(drone.Id); GUILayout.EndHorizontal(); GUILayout.Space(10);
            }
            if (drones.Count == 0) GUILayout.Label("Keine einsatzfähige Drohne. Instandsetzung am Stationspult planen.", ui.Small);
            GUILayout.EndScrollView();
        }
        private void DrawTacticalQuests()
        {
            GUILayout.Label(TacticalQuestHeader, ui.Text); tacticalScroll = GUILayout.BeginScrollView(tacticalScroll);
            foreach (var quest in GetTacticalQuests())
            {
                GUILayout.Label((quest.Complete ? "✓ " : quest.Current ? "→ " : "○ ") + quest.Title, ui.Text);
                GUILayout.Label(quest.Detail, ui.Small);
                var bar = GUILayoutUtility.GetRect(10, 7, GUILayout.ExpandWidth(true)); SpaceMinerUi.Fill(bar, new Color(.07f, .14f, .19f));
                SpaceMinerUi.Fill(new Rect(bar.x, bar.y, bar.width * quest.Progress, bar.height), quest.Complete ? SpaceMinerUi.Success : SpaceMinerUi.Cyan);
                GUILayout.Label((quest.Progress * 100).ToString("0") + " %", ui.Small); GUILayout.Space(10);
            }
            GUILayout.EndScrollView();
        }
    }
}

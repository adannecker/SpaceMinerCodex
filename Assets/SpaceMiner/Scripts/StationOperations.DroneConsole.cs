using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private Vector2 consoleFleetScroll, consoleDetailsScroll, consoleOrdersScroll;
        private int consoleDetailTab;
        private bool consoleShowFaults, consoleCompactOrders, consoleHistory, consoleChooseTarget;
        private string consoleTarget = "", consoleWaterTarget = "", consoleDraggedOrder;
        private string consoleTargetFilter = "";
        private int consoleJob;
        private const string ConsoleFilterControl = "DroneConsoleTargetFilter";
        private bool consoleFilterFocused, consoleReleaseFilterFocus;
        private string consoleObservedFocusedControl = "";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool consoleRequestFilterFocus;
        internal void RequestConsoleTargetFilterFocus() { consoleChooseTarget = true; consoleRequestFilterFocus = true; }
        internal bool ConsoleActualFilterFocus => consoleObservedFocusedControl == ConsoleFilterControl;
#endif
        private readonly string[] consoleDetailTitles = { "Übersicht", "Ausrüstung", "Diagnose" };
        public int ConsoleDetailTab { get => consoleDetailTab; set => consoleDetailTab = Mathf.Clamp(value, 0, 2); }
        public bool ConsoleCompactOrders { get => consoleCompactOrders; set => consoleCompactOrders = value; }
        public DroneConsoleLayout ConsoleLastLayout { get; private set; }
        public bool ConsoleDetailsVisible { get; private set; }
        public bool ConsoleOrdersVisible { get; private set; }
        public static bool DroneConsoleTextInputActive => Current != null && Current.Live && Current.panel == StationTerminalKind.Drones
            && Current.consoleChooseTarget && Current.ConsoleOrdersVisible && Current.consoleFilterFocused;
        private void ResetConsoleFilterFocus()
        {
            consoleFilterFocused = false; consoleObservedFocusedControl = ""; consoleReleaseFilterFocus = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            consoleRequestFilterFocus = false;
#endif
        }
        internal void ResetDroneConsoleTextInput() { consoleChooseTarget = false; ResetConsoleFilterFocus(); }
        private float ConsoleTextScale => SettingsStore.Current.Accessibility.TextScale;
        private float ConsoleButtonHeight => Mathf.Max(38, 36 * ConsoleTextScale);

        public void DrawDroneConsole(float w, float h)
        {
            if (consoleReleaseFilterFocus)
            { if (GUI.GetNameOfFocusedControl() == ConsoleFilterControl) GUI.FocusControl(null); consoleReleaseFilterFocus = false; }
            // UiLayout's fit preserves a wide virtual canvas even in a small window.
            // Reflow on physical width too; keep its existing single GUI scale.
            var layout = DroneConsoleLayout.For(w, h, Screen.width < 1300);
            ConsoleLastLayout = layout;
            ConsoleDetailsVisible = !layout.Compact || !consoleCompactOrders;
            ConsoleOrdersVisible = !layout.Compact || consoleCompactOrders;
            ui.Panel(new Rect(0, 0, w, h));
            DrawConsoleHeader(layout.Header);
            float half = (layout.Domain.width - 12) / 2;
            if (ui.Tab(new Rect(layout.Domain.x, layout.Domain.y, half, layout.Domain.height), "EXTERNE DROHNEN / " + Scenario.Drones.Length, !consoleInternal)) ConsoleSwitchDomain(false);
            if (ui.Tab(new Rect(layout.Domain.x + half + 12, layout.Domain.y, half, layout.Domain.height), "INTERNE DROHNEN / R-01", consoleInternal)) ConsoleSwitchDomain(true);
            DrawConsoleFleet(layout.Fleet);
            if (layout.Compact)
            {
                Rect n = layout.CompactNavigation;
                if (ui.Tab(new Rect(n.x, n.y, (n.width - 8) / 2, n.height), "DROHNENDETAILS", !consoleCompactOrders)) { consoleCompactOrders = false; ResetConsoleFilterFocus(); }
                if (ui.Tab(new Rect(n.x + (n.width + 8) / 2, n.y, (n.width - 8) / 2, n.height), "AUFTRÄGE", consoleCompactOrders)) { consoleCompactOrders = true; ResetConsoleFilterFocus(); }
            }
            if (ConsoleDetailsVisible) DrawConsoleDetails(layout.Details);
            if (ConsoleOrdersVisible) DrawConsoleOrders(layout.Orders);
            SpaceMinerUi.Fill(layout.Footer, new Color(.055f, .10f, .14f));
            GUI.Label(new Rect(layout.Footer.x + 10, layout.Footer.y + 3, layout.Footer.width - 20, layout.Footer.height - 6), new GUIContent(Message, Message), ui.Small);
            ui.Tooltip(1, w, h);
            if (Event.current.type == EventType.MouseUp) consoleDraggedOrder = null;
        }
        private void ConsoleSwitchDomain(bool inside)
        {
            if (consoleInternal == inside) return;
            SetConsoleDomain(inside); ConsoleSelectionChanged();
        }
        private void ConsoleSelectionChanged()
        {
            ResetConsoleFilterFocus();
            consoleDetailsScroll = consoleOrdersScroll = Vector2.zero; consoleTarget = ""; consoleWaterTarget = "";
            consoleChooseTarget = false; consoleTargetFilter = ""; consoleJob = 0; consoleDraggedOrder = null;
        }
        private void DrawConsoleHeader(Rect r)
        {
            float titleWidth = r.width - 144;
            var heading = new GUIStyle(ui.Heading) { wordWrap = false };
            while (heading.fontSize > 14 && heading.CalcSize(new GUIContent("STATION // DROHNENLEITSTAND")).x > titleWidth) heading.fontSize--;
            GUI.Label(new Rect(r.x + 4, r.y, titleWidth, 38), "STATION // DROHNENLEITSTAND", heading);
            if (ui.Tab(new Rect(r.xMax - 122, r.y, 118, 36), "E / Esc · Zu", false)) ClosePanel();
            string clock = Scenario.SimulationRate == 0 ? "SIMULATION PAUSIERT" : SettingsStore.Current.Gameplay.PauseInMenu ? "MENÜPAUSE · zum Arbeiten Leitstand schließen" : "LIVE · Simulation " + Scenario.SimulationRate.ToString("0") + "×";
            GUI.Label(new Rect(r.x + 4, r.y + 34, r.width - 8, 28), clock, ui.Small);
            var p = State.power;
            string[] stats = { "STATIONSENERGIE  " + p.storedKwh.ToString("0.0") + " / " + p.capacityKwh.ToString("0") + " kWh",
                "ERZEUGUNG / LAST  " + p.GenerationKw.ToString("0.00") + " / " + p.ConsumptionKw.ToString("0.00") + " kW",
                "WASSERRESERVE  " + Scenario.WaterLiters.ToString("0.0") + " L", "WERKSTATT  " + (IsRefitting(0) || IsRefitting(1) ? "01 belegt" : "01 frei") + " · 02 defekt" };
            float cell = (r.width - 12) / 4;
            for (int i = 0; i < stats.Length; i++)
            {
                var box = new Rect(r.x + i * (cell + 4), r.y + 68, cell, 38);
                SpaceMinerUi.Fill(box, new Color(.055f, .12f, .17f));
                var style = new GUIStyle(ui.Small) { wordWrap = true, fontSize = Mathf.Min(ui.Small.fontSize, 17) };
                GUI.Label(new Rect(box.x + 8, box.y + 3, box.width - 16, box.height - 6), stats[i], style);
            }
        }
        private void DrawConsoleFleet(Rect r)
        {
            ui.Panel(r); GUILayout.BeginArea(ConsoleInset(r));
            GUILayout.Label("FLOTTE", ui.Text);
            consoleFleetScroll = GUILayout.BeginScrollView(consoleFleetScroll);
            if (consoleInternal)
            {
                ConsoleFleetCard("R-01");
                GUILayout.Space(12); GUILayout.Label("Interne Logistik und Bestandsservice. Weitere interne Einheiten werden noch nicht simuliert.", ui.Small);
            }
            else
            {
                int live = 0; for (int i = 0; i < Scenario.Drones.Length; i++) if (CanUseDrone(i) && Scenario.Drones[i].IsOperational) live++;
                GUILayout.Label(live + " freigegeben · " + (Scenario.Drones.Length - live) + " ohne Einsatzfreigabe", ui.Small);
                ConsoleFleetCard("drone-0"); if (Scenario.Drones.Length > 1) ConsoleFleetCard("drone-1");
                string faultyLabel = ConsoleLastLayout.Compact ? " Defekte (" : " Weitere defekte Drohnen (";
                if (ConsoleButton((consoleShowFaults ? "▾" : "▸") + faultyLabel + Mathf.Max(0, Scenario.Drones.Length - 2) + ")")) consoleShowFaults = !consoleShowFaults;
                if (ConsoleDroneIndex(consoleSelectedDrone) > 1) consoleShowFaults = true;
                if (consoleShowFaults) for (int i = 2; i < Scenario.Drones.Length; i++) ConsoleFleetCard("drone-" + i);
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private void ConsoleFleetCard(string id)
        {
            var d = ConsoleDrone(id); if (d == null) return;
            bool chosen = consoleSelectedDrone == id;
            var style = new GUIStyle(chosen ? ui.Primary : ui.Button) { alignment = TextAnchor.MiddleLeft };
            string status = d.Internal ? State.helper ? "ONLINE" : "AUSGESCHALTET" : d.Operational ? d.Status.ToUpperInvariant() : "DEFEKT / GESPERRT";
            string stores = d.HasOnboardStores ? "Akku " + (100 * d.BatteryKwh / Mathf.Max(.001f, d.BatteryCapacityKwh)).ToString("0") + " %  ·  H₂O " + d.FuelLiters.ToString("0.0") + " L" : "Stationsversorgung";
            string text = d.Name + "  /  " + (d.Internal ? "INTERN" : "EXTERN") + "\n" + status + "\n" + stores + "\n" + (d.HasActivity ? d.Activity : d.Tool);
            if (GUILayout.Button(new GUIContent(text, d.Status), style, GUILayout.MinHeight(106 * ConsoleTextScale)))
            { SelectConsoleDrone(id); ConsoleSelectionChanged(); }
            GUILayout.Space(8);
        }
        private void DrawConsoleDetails(Rect r)
        {
            ui.Panel(r); var d = ConsoleDrone(consoleSelectedDrone); if (d == null) return;
            GUILayout.BeginArea(ConsoleInset(r));
            GUILayout.Label(d.Name + " // " + (d.Internal ? "INTERNE LOGISTIK" : "AUSSENDROHNE"), ui.Text);
            GUILayout.BeginHorizontal();
            for (int t = 0; t < consoleDetailTitles.Length; t++) if (GUILayout.Button(consoleDetailTitles[t], t == consoleDetailTab ? ui.Primary : ui.Button, GUILayout.MinHeight(ConsoleButtonHeight))) consoleDetailTab = t;
            GUILayout.EndHorizontal(); GUILayout.Space(8);
            consoleDetailsScroll = GUILayout.BeginScrollView(consoleDetailsScroll);
            if (consoleDetailTab == 0) DrawConsoleOverview(d);
            else if (consoleDetailTab == 1) DrawConsoleEquipment(d);
            else DrawConsoleDiagnostics(d);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private void DrawConsoleOverview(DroneFleetReadModel d)
        {
            ConsoleKicker(d.Operational ? "EINSATZFREIGABE" : "KEINE EINSATZFREIGABE", d.Operational ? SpaceMinerUi.Success : SpaceMinerUi.Amber);
            GUILayout.Label(d.Status, ui.Text); GUILayout.Space(10);
            DrawConsoleSchematic(d);
            if (d.HasOnboardStores)
            {
                ConsoleMeter("AKKU", d.BatteryKwh, d.BatteryCapacityKwh, "kWh", SpaceMinerUi.Cyan);
                ConsoleMeter("TREIBWASSER", d.FuelLiters, d.FuelCapacityLiters, "L", SpaceMinerUi.Cyan);
                ConsoleMeter("ROHFRACHT", d.CargoKg, d.CargoCapacityKg, "kg", SpaceMinerUi.Amber);
                GUILayout.Label("Boxfüllung " + (d.CargoFillFraction * 100).ToString("0.0") + " % · " + d.CargoVolumeM3.ToString("0.000") + " m³", ui.Small);
                var agent = Scenario.Drones[d.Index];
                GUILayout.Label("Geschwindigkeit  " + d.SpeedMetersPerSecond.ToString("0.00") + " m/s\nZielentfernung  " + d.DistanceMeters.ToString("0.0") + " m\nHeimatdock  " + (d.Index + 1).ToString("00"), ui.Small);
                if (d.Operational) GUILayout.Label("Sicherheitsreserve  " + agent.EnergyReserveKwh.ToString("0.00") + " kWh / " + agent.FuelReserveLiters.ToString("0.00") + " L", ui.Small);
            }
            else
            {
                GUILayout.Label("Versorgung über die Station. Für R-01 werden derzeit kein eigener Akku, Treibwassertank oder separates Frachtrauminventar simuliert.", ui.Small);
                GUILayout.Label("Arbeitsplatz: interne Werkstatt\nFähigkeiten: Greifen, interner Transport, Bestandsservice und Anlagenaufträge", ui.Small);
            }
            GUILayout.Space(12);
            if (d.HasActivity)
            {
                ConsoleKicker("AKTUELLER AUFTRAG", SpaceMinerUi.Cyan); GUILayout.Label(d.Activity, ui.Text);
                GUILayout.Label(d.Destination + "\n" + d.Status, ui.Small);
                ConsoleProgress(d.Progress);
                if (d.HasEta) GUILayout.Label(d.EtaLabel + WaterScenario.Duration(d.EtaSeconds), ui.Small);
                GUILayout.Label("Werkstattfahrt, Schleusen- und Versorgungswartezeit können die Gesamtdauer verlängern.", ui.Small);
                if (d.CanRecall && ConsoleButton("Sicher zurückrufen / abschließen")) RecallConsoleDrone(out _);
            }
            else GUILayout.Label("Kein aktiver Auftrag. Wähle rechts eine Tätigkeit und ein passendes Ziel.", ui.Small);
            if (!string.IsNullOrEmpty(d.WaitReason)) GUILayout.Label(d.WaitReason, ui.Small);
            if (d.Index == 1 && !State.second) DrawConsoleSecondActivation();
            if (d.Internal && !State.helper)
            {
                ConsoleRecipe(StationJobKind.ActivateHelper);
                if (ConsoleButton("R-01-Inbetriebnahme vormerken", true)) TryConsoleOrder(StationJobKind.ActivateHelper, "", out _);
            }
            if (d.Index >= 0 && d.Index <= 1 && Scenario.Drones[d.Index].IsReady && d.CargoKg > .0001f)
                if (ConsoleButton("Vorhandene Fracht zur Annahme liefern", true)) DeliverConsoleCargo(out _);
            if (ConsoleButton("Tätigkeit / Ziel wählen")) { consoleCompactOrders = true; consoleOrdersScroll = Vector2.zero; }
        }
        private void DrawConsoleSchematic(DroneFleetReadModel d)
        {
            Rect r = GUILayoutUtility.GetRect(40, 132, GUILayout.ExpandWidth(true));
            SpaceMinerUi.Fill(r, new Color(.035f, .08f, .12f)); SpaceMinerUi.Border(r, new Color(.15f, .38f, .48f));
            var style = new GUIStyle(ui.Small) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Min(ui.Small.fontSize, 16) };
            float block = (r.width - 40) / 3;
            string[] slots = d.Internal ? new[] { "BASISGREIFER", "R-01\nBESTANDSSERVICE", "STATIONSVERSORGUNG" } : new[] { d.Tool.ToUpperInvariant(), "AKKU / ANTRIEB\n" + (d.Operational ? "freigegeben" : "gesperrt"), "ROHFRACHT\n" + d.CargoKg.ToString("0.0") + " kg" };
            for (int i = 0; i < 3; i++)
            {
                Rect box = new Rect(r.x + 10 + i * (block + 10), r.y + 24, block, 80);
                SpaceMinerUi.Fill(box, new Color(.075f, .17f, .22f)); SpaceMinerUi.Border(box, d.Operational ? SpaceMinerUi.Cyan : SpaceMinerUi.Disabled);
                GUI.Label(new Rect(box.x + 4, box.y + 3, box.width - 8, box.height - 6), slots[i], style);
            }
        }
        private void DrawConsoleEquipment(DroneFleetReadModel d)
        {
            ConsoleKicker("WERKZEUG / FÄHIGKEITEN", SpaceMinerUi.Cyan);
            if (d.Internal)
            {
                GUILayout.Label("Basisgreifer und Bestandsservice", ui.Text);
                GUILayout.Label("Greifen und interner Transport sind Teil des vorhandenen R-01-Modells. Serviceaufträge nutzen die bestehenden Anlagen und Werkstattarme.", ui.Small);
                ConsoleRequirement("R-01 in Betrieb", State.helper);
                ConsoleRequirement("Reparaturverfahren erforscht", State.technologies.Contains("repair"));
                GUILayout.Space(12); ConsoleKicker("FORSCHUNG / SPÄTERER AUSBAU", SpaceMinerUi.Amber);
                GUILayout.Label("Scannerkopf, weitere Greifer und Fertigungsmodule: noch kein eigenständiges R-01-Steckplatz- oder Werkzeuglagermodell vorhanden. Diese Module sind noch nicht ausrüstbar.", ui.Small);
                bool prior = GUI.enabled; GUI.enabled = false; ConsoleButton("Neue interne Module · noch nicht verfügbar"); GUI.enabled = prior;
                return;
            }
            GUILayout.Label("Montiert: " + d.Tool, ui.Text);
            var planned = State.orders.Find(o => !o.complete && o.kind == StationJobKind.Refit && o.target.StartsWith(d.Index + ":", StringComparison.Ordinal));
            if (planned != null) GUILayout.Label("Umbauziel: " + ConsoleTargetName(planned.target) + "\n" + planned.status, ui.Small);
            GUILayout.Label("Eine gemeinsame Werkzeugaufnahme. Der Wechsel wird als echter Werkstattauftrag ausgeführt; das alte Werkzeug bleibt bis zum Abschluss montiert und geht danach ins Lager zurück.", ui.Small);
            string[] tools = { "Bohrer", "Greifer", "Laser" };
            string[] roles = { "Eisabbau, Kohlenstoffproben und erste Trümmerbergung", "Trümmer greifen und bergen", "Außenreparaturen nach erforschter Reparaturrolle" };
            for (int n = 0; n < tools.Length; n++)
            {
                GUILayout.Space(12); string tool = tools[n];
                int stock = n == 0 ? State.spareDrills : n == 1 ? State.spareGrippers : State.spareLasers;
                GUILayout.Label(tool + "  /  Bestand " + stock, ui.Text); GUILayout.Label(roles[n], ui.Small);
                bool unlocked = n != 2 || State.technologies.Contains("repair");
                if (!unlocked) GUILayout.Label("Gesperrt: Mira muss die Reparaturrolle erforschen.", ui.Small);
                bool previous = GUI.enabled; GUI.enabled = previous && d.Index <= 1 && CanUseDrone(d.Index) && d.Tool != tool && unlocked && stock > 0 && planned == null;
                if (ConsoleButton(d.Tool == tool ? "✓ Montiert" : "Umbau auf " + tool + " vormerken", d.Tool != tool)) TryConsoleRefit(tool, out _);
                GUI.enabled = previous;
            }
            GUILayout.Space(14); ConsoleRecipe(StationJobKind.Refit);
            ConsoleRequirement("R-01 verfügbar", State.helper); ConsoleRequirement("Werkstatt 01 versorgt", Powered(1) && Element("hall").condition >= .3f);
            GUILayout.Label("Servicezeit zuzüglich realer Werkstattfahrt und Schleusenzeiten. Platz 02 ist anfangs defekt.", ui.Small);
            var agent = Scenario.Drones[d.Index];
            if (!agent.CargoProfile.VolumeLimited && ConsoleButton("Alten Spielstand: 90-%-Auslegung am Dock übernehmen"))
                ApplyConsoleSizingAtDock(out _);
        }
        private void DrawConsoleDiagnostics(DroneFleetReadModel d)
        {
            ConsoleKicker("DIAGNOSE / FREIGABEN", SpaceMinerUi.Cyan);
            if (d.Internal)
            {
                ConsoleRequirement("R-01 in Betrieb", State.helper);
                ConsoleRequirement("Interne Werkstatt versorgt", Powered(1));
                GUILayout.Label("Werkstattzustand " + (Element("helper").condition * 100).ToString("0.0") + " % (Stationselement)", ui.Small);
                GUILayout.Label("Einzelsystemverschleiß, Wartungsdatum und eigener Speicher der internen Drohne sind noch nicht modelliert.", ui.Small);
                if (!State.helper && ConsoleButton("R-01-Inbetriebnahme vormerken", true)) TryConsoleOrder(StationJobKind.ActivateHelper, "", out _);
            }
            else
            {
                var agent = Scenario.Drones[d.Index];
                ConsoleRequirement("Einsatzfreigabe", d.Operational);
                ConsoleRequirement("Versorgung am Dock", Powered(1));
                ConsoleRequirement("Akku über Sicherheitsreserve", agent.BatteryKwh >= agent.EnergyReserveKwh);
                ConsoleRequirement("Treibwasser über Sicherheitsreserve", agent.FuelLiters >= agent.FuelReserveLiters);
                GUILayout.Label("Montiertes Werkzeug: " + d.Tool + "\nLeermasse " + agent.DryMassKg.ToString("0") + " kg · mechanisches Lastlimit " + agent.CargoCapacityKg.ToString("0") + " kg\nSchub " + agent.ThrustNewtons.ToString("0.0") + " N · Reisegeschwindigkeit " + agent.CruiseSpeed.ToString("0.0") + " m/s", ui.Small);
                GUILayout.Label("Die vorhandene Einsatzsperre ist verbindlich. Prozentwerte für einzelne Antriebs-, Sensor- oder Werkzeugschäden werden derzeit nicht separat simuliert.", ui.Small);
                if (d.Index == 1) DrawConsoleSecondActivation();
                if (d.Index > 1) GUILayout.Label("Für diese Einheit existiert noch kein freigegebener Instandsetzungsablauf. Die erste Kampagne schaltet zunächst Drohne 02 frei.", ui.Small);
                if (!string.IsNullOrEmpty(agent.ReturnReason)) GUILayout.Label("Letzte Rückkehrmeldung: " + agent.ReturnReason, ui.Small);
            }
            GUILayout.Space(10); ConsoleRequirement("Stationsversorgung diagnostiziert", State.diagnosed);
            if (!State.diagnosed && ConsoleButton("Versorgungsdiagnose vormerken")) TryConsoleOrder(StationJobKind.Diagnose, "", out _);
        }
        private void DrawConsoleSecondActivation()
        {
            GUILayout.Space(14); ConsoleKicker("DROHNE 02 / WIEDERINBETRIEBNAHME", SpaceMinerUi.Amber);
            if (State.second)
            {
                var second = Scenario.Drones[1];
                GUILayout.Label(second.IsReady ? "Instandsetzung abgeschlossen · einsatzbereit" : "Instandsetzung abgeschlossen · " + second.Status, ui.Text);
                GUILayout.Label("Dockversorgung aus echten Stationsbeständen: " + second.BatteryKwh.ToString("0.00") + " / " + second.BatteryCapacityKwh.ToString("0") + " kWh und " + second.FuelLiters.ToString("0.00") + " / " + second.FuelCapacityLiters.ToString("0") + " L.", ui.Small);
                return;
            }
            ConsoleRequirement("Versorgungsdiagnose", State.diagnosed); ConsoleRequirement("R-01 in Betrieb", State.helper);
            ConsoleRequirement("40 L Wasserreserve", Scenario.WaterLiters >= 40); ConsoleRequirement("6 kWh Stationsreserve", State.power.storedKwh >= 6);
            var active = State.orders.Find(o => o.kind == StationJobKind.ActivateSecond && !o.complete);
            ConsoleRequirement("2 geprüfte Ersatzteile" + (active != null && active.started ? " (reserviert)" : ""), State.inventory.parts.amount >= 2 || active != null && active.started);
            ConsoleRecipe(StationJobKind.ActivateSecond);
            GUILayout.Label("Aktueller Ablauf: R-01 setzt 02 instand. Danach lädt und betankt die Station sie tatsächlich am Dock. Ein eigener Notversorgungsflug vor der Reparatur ist noch nicht implementiert.", ui.Small);
            if (active != null) GUILayout.Label("Bestehender Auftrag: " + active.status, ui.Small);
            else if (ConsoleButton("Instandsetzung von 02 vormerken", true)) TryConsoleOrder(StationJobKind.ActivateSecond, "", out _);
        }
        private void DrawConsoleOrders(Rect r)
        {
            ui.Panel(r); GUILayout.BeginArea(ConsoleInset(r));
            GUILayout.Label("AUFTRÄGE // " + (consoleInternal ? "R-01" : "DROHNE " + (ConsoleDroneIndex(consoleSelectedDrone) + 1).ToString("00")), ui.Text);
            consoleOrdersScroll = GUILayout.BeginScrollView(consoleOrdersScroll);
            var drone = ConsoleDrone(consoleSelectedDrone);
            if (drone.HasActivity && drone.ActivityId != null && (drone.ActivityId.StartsWith("water:", StringComparison.Ordinal) || drone.ActivityId.StartsWith("phase:", StringComparison.Ordinal)))
            {
                ConsoleKicker(drone.CanRecall ? "AKTIVER EISFLUG" : "AKTUELLE DROHNENPHASE", SpaceMinerUi.Cyan); GUILayout.Label(drone.Activity + "\n" + drone.Destination + "\n" + drone.Status, ui.Small);
                ConsoleProgress(drone.Progress);
                if (drone.HasEta) GUILayout.Label(drone.EtaLabel + WaterScenario.Duration(drone.EtaSeconds), ui.Small);
                if (drone.CanRecall && ConsoleButton("Sichere Rückkehr anfordern")) RecallConsoleDrone(out _);
            }
            DrawConsoleOrderCards(false);
            GUILayout.Space(12); ConsoleKicker("NEUEN AUFTRAG PLANEN", SpaceMinerUi.Amber);
            if (consoleInternal) DrawConsoleInternalOrderForm(); else DrawConsoleExternalOrderForm();
            GUILayout.Space(16);
            if (ConsoleButton((consoleHistory ? "▾" : "▸") + " Abgeschlossene Aufträge")) consoleHistory = !consoleHistory;
            if (consoleHistory) DrawConsoleOrderCards(true);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private void DrawConsoleOrderCards(bool history)
        {
            var orders = ConsoleOrders(consoleSelectedDrone, history);
            bool any = false;
            foreach (var order in orders)
            {
                if (history != order.complete) continue; any = true;
                var recipe = StationRecipes.Get(order.kind);
                GUILayout.Space(8); GUILayout.BeginVertical(GUI.skin.box);
                var top = GUILayoutUtility.GetRect(40, 24 * ConsoleTextScale, GUILayout.ExpandWidth(true));
                GUI.Label(top, (order.started ? "▶ " : "≡ ") + recipe.Name, ui.Small);
                bool pending = !order.started && !order.complete && !order.cancelled;
                var e = Event.current;
                if (pending && e.type == EventType.MouseDown && e.button == 0 && top.Contains(e.mousePosition)) { consoleDraggedOrder = order.id; e.Use(); }
                if (consoleDraggedOrder == order.id) SpaceMinerUi.Fill(new Rect(top.x, top.yMax - 2, top.width, 2), SpaceMinerUi.Amber);
                GUILayout.Label(ConsoleTargetName(order.target) + "\n" + order.status, ui.Small);
                if (order.started || order.complete) ConsoleProgress(order.elapsed / recipe.Seconds);
                if (!order.complete)
                {
                    GUILayout.Label(order.started ? "Arbeitszeit ca. " + WaterScenario.Duration(Mathf.Max(0, recipe.Seconds - order.elapsed)) + " + Wege/Warten" : "Start beim nächsten ausführbaren freien Arbeitsplatz", ui.Small);
                    GUILayout.BeginHorizontal(); bool old = GUI.enabled; GUI.enabled = old && pending;
                    if (GUILayout.Button("↑", ui.Button, GUILayout.MinWidth(42), GUILayout.MinHeight(ConsoleButtonHeight))) MoveConsoleOrder(order.id, -1, out _);
                    if (GUILayout.Button("↓", ui.Button, GUILayout.MinWidth(42), GUILayout.MinHeight(ConsoleButtonHeight))) MoveConsoleOrder(order.id, 1, out _);
                    GUI.enabled = old && !order.cancelled;
                    if (GUILayout.Button(order.started ? "Rückruf" : "Abbrechen", ui.Button, GUILayout.MinHeight(ConsoleButtonHeight))) CancelConsoleOrder(order.id, out _);
                    GUI.enabled = old; GUILayout.EndHorizontal();
                }
                else if ((order.reserved == null || order.reserved.Length == 0) && ConsoleButton("Historieneintrag entfernen")) RemoveConsoleHistory(order.id, out _);
                GUILayout.EndVertical();
                Rect row = GUILayoutUtility.GetLastRect();
                if (pending && e.type == EventType.MouseUp && consoleDraggedOrder != null && row.Contains(e.mousePosition))
                { PlaceConsoleOrder(consoleDraggedOrder, order.id, out _); consoleDraggedOrder = null; e.Use(); }
            }
            if (!any) GUILayout.Label(history ? "Noch keine abgeschlossenen Einträge." : "Keine Arbeitsaufträge vorgemerkt.", ui.Small);
            if (!history) GUILayout.Label("≡ an der Überschrift ziehen oder ↑ / ↓: nur wartende Aufträge dieser Einheit priorisieren. Laufende Arbeit wird fortgesetzt.", ui.Small);
        }
        private void DrawConsoleExternalOrderForm()
        {
            int index = ConsoleDroneIndex(consoleSelectedDrone);
            if (index > 1 || !CanUseDrone(index))
            {
                GUILayout.Label("Außenaufträge gesperrt: zuerst instand setzen und am Dock versorgen.", ui.Small);
                if (index == 1) DrawConsoleSecondActivation();
                return;
            }
            string[] choices = { "Eis / Wasser", "Trümmer", "Kohlenstoff", "Reparatur", "Anbau" };
            int next = ui.Dropdown("TÄTIGKEIT", consoleJob, choices);
            if (next != consoleJob) { consoleJob = next; consoleTarget = ""; consoleChooseTarget = false; consoleTargetFilter = ""; }
            if (consoleJob == 0) { DrawConsoleWaterForm(index); return; }
            var kinds = new[] { StationJobKind.Salvage, StationJobKind.Carbon, StationJobKind.Repair, StationJobKind.Expand };
            StationJobKind kind = kinds[Mathf.Clamp(consoleJob - 1, 0, kinds.Length - 1)];
            DrawConsoleTargetForm(kind); ConsoleRecipe(kind);
            string why = ConsoleOrderWaitReason(kind, consoleTarget);
            if (why != "") GUILayout.Label("Voraussetzung / Wartegrund: " + why, ui.Small);
            bool old = GUI.enabled; GUI.enabled = old && !string.IsNullOrEmpty(consoleTarget);
            if (ConsoleButton("Auftrag vormerken", true)) TryConsoleOrder(kind, consoleTarget, out _); GUI.enabled = old;
        }
        private void DrawConsoleWaterForm(int index)
        {
            var targets = ConsoleWaterTargets();
            if (targets.Count == 0)
            {
                GUILayout.Label("Noch keine bestätigte Eisquelle verfügbar. Zuerst den Nahbereich am Scannerpult scannen; der Scan gewährt das Abbauwissen.", ui.Small); return;
            }
            if (!targets.Exists(t => t.Id == consoleWaterTarget)) consoleWaterTarget = targets[0].Id;
            ConsoleChooseTargets(targets, ref consoleWaterTarget);
            var source = Array.Find(Scenario.Asteroids, a => a.name == consoleWaterTarget); var d = Scenario.Drones[index];
            if (source != null)
            {
                float capacity = d.CargoCapacityFor(source);
                GUILayout.Label("Eine Ladung zur geschlossenen Roh-Eis-Annahme. Planbares Volumenlimit ca. " + capacity.ToString("0.0") + " kg · Schürfzeit ca. " + WaterScenario.Duration(capacity / Mathf.Max(.001f, d.EffectiveMiningRate)) + ".", ui.Small);
                GUILayout.Label("Quelle: " + source.RemainingRawKg.ToString("0") + " kg Rohmasse bekannt\nAnnahme derzeit frei: " + RawReceivingCapacity(source.WaterFraction).ToString("0.0") + " kg", ui.Small);
            }
            GUILayout.Label("Bohrer erforderlich. Der Flug wird beim Start auf Energie, Treibwasser, reale Annahmekapazität und sichere Rückkehr geprüft. Ein Eisflug ist ein sofortiger Einzelauftrag, keine zusätzliche Warteschlange.", ui.Small);
            bool prior = GUI.enabled; GUI.enabled = prior && d.IsReady && State.tools[index] == "Bohrer" && d.CargoKg <= .001f && RawReceivingCapacity(source.WaterFraction) > 0;
            bool startClicked = ConsoleButton("Eine Ladung Roh-Eis starten", true); GUI.enabled = prior;
            if (startClicked)
            {
                if (TryConsoleWaterOrder(consoleWaterTarget, out _) && !PanelOpen)
                {
                    // Dialogue/phase ownership changed during MouseUp. Finish this
                    // IMGUI event before drawing a layout based on the new state.
                    GUIUtility.ExitGUI();
                }
            }
            if (!d.IsReady) GUILayout.Label("Wartet: " + d.Status + ".", ui.Small);
            else if (d.CargoKg > .001f)
            { GUILayout.Label("Vorhandene Restfracht zuerst abgeben.", ui.Small); if (ConsoleButton("Restfracht zur Annahme liefern")) DeliverConsoleCargo(out _); }
            else if (State.tools[index] != "Bohrer") GUILayout.Label("Bohrer fehlt: im Reiter Ausrüstung einen Umbau vormerken.", ui.Small);
            else if (source != null && RawReceivingCapacity(source.WaterFraction) <= 0) GUILayout.Label("Rohannahme gesperrt, stromlos oder voll. Anlagenstatus am Fertigungspult prüfen.", ui.Small);
        }
        private void DrawConsoleInternalOrderForm()
        {
            var kinds = new[] { StationJobKind.Recycle, StationJobKind.PrintParts, StationJobKind.Food, StationJobKind.Oxygen, StationJobKind.Repair, StationJobKind.ActivateHelper, StationJobKind.ActivateSecond };
            var choices = new string[kinds.Length]; for (int n = 0; n < kinds.Length; n++) choices[n] = StationRecipes.Get(kinds[n]).Name;
            int next = ui.Dropdown("TÄTIGKEIT / ANLAGE", consoleJob, choices);
            if (next != consoleJob) { consoleJob = next; consoleTarget = ""; consoleChooseTarget = false; }
            var kind = kinds[Mathf.Clamp(consoleJob, 0, kinds.Length - 1)];
            if (kind == StationJobKind.Repair) DrawConsoleTargetForm(kind);
            else GUILayout.Label("Interner Transport und Anlagenarbeit über die vorhandene R-01. Material wird beim tatsächlichen Auftragsstart reserviert.", ui.Small);
            ConsoleRecipe(kind); string why = ConsoleOrderWaitReason(kind, consoleTarget);
            if (why != "") GUILayout.Label("Voraussetzung / Wartegrund: " + why, ui.Small);
            bool previous = GUI.enabled; GUI.enabled = previous && (kind != StationJobKind.Repair || consoleTarget != "");
            if (ConsoleButton("Internen Auftrag vormerken", true)) TryConsoleOrder(kind, kind == StationJobKind.Repair ? consoleTarget : "", out _); GUI.enabled = previous;
        }
        private void DrawConsoleTargetForm(StationJobKind kind)
        {
            var targets = ConsoleTargets(kind);
            if (targets.Count == 0)
            {
                consoleTarget = ""; GUILayout.Label(kind == StationJobKind.Salvage ? "Keine freien bekannten Wrackkontakte. Erst scannen; geborgene oder bereits zugewiesene Kontakte sind ausgeblendet." : "Keine bekannten verfügbaren Ziele für diese Tätigkeit.", ui.Small); return;
            }
            if (!targets.Exists(t => t.Id == consoleTarget)) consoleTarget = targets[0].Id;
            ConsoleChooseTargets(targets, ref consoleTarget);
        }
        private void ConsoleChooseTargets(List<DroneConsoleTarget> targets, ref string value)
        {
            string selectedTargetId = value;
            var chosen = targets.Find(t => t.Id == selectedTargetId);
            if (ConsoleButton("ZIEL: " + (chosen == null ? "wählen" : chosen.Name) + (consoleChooseTarget ? " ▴" : " ▾")))
            { consoleChooseTarget = !consoleChooseTarget; if (!consoleChooseTarget) ResetConsoleFilterFocus(); }
            if (chosen != null) GUILayout.Label(chosen.Details, ui.Small);
            if (!consoleChooseTarget) return;
            GUILayout.Label("Ziele filtern", ui.Small);
            GUI.SetNextControlName(ConsoleFilterControl);
            consoleTargetFilter = GUILayout.TextField(consoleTargetFilter, GUILayout.MinHeight(30 * ConsoleTextScale));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (consoleRequestFilterFocus && Event.current.type == EventType.Repaint) { GUI.FocusControl(ConsoleFilterControl); consoleRequestFilterFocus = false; }
#endif
            consoleObservedFocusedControl = GUI.GetNameOfFocusedControl();
            consoleFilterFocused = consoleObservedFocusedControl == ConsoleFilterControl;
            int shown = 0;
            foreach (var t in targets)
            {
                if (consoleTargetFilter != "" && (t.Name + " " + t.Details).IndexOf(consoleTargetFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                shown++;
                if (ConsoleButton(t.Name + "\n" + t.Details, t.Id == value)) { value = t.Id; consoleChooseTarget = false; ResetConsoleFilterFocus(); }
            }
            if (shown == 0) GUILayout.Label("Kein Ziel passt zum Filter.", ui.Small);
        }
        private void ConsoleRecipe(StationJobKind kind)
        {
            var recipe = StationRecipes.Get(kind);
            GUILayout.Label("ARBEIT  " + WaterScenario.Duration(recipe.Seconds) + " · " + recipe.PowerKw.ToString("0.##") + " kW\nEINSATZ  " + ConsoleRecipeMaterials(recipe.Input), ui.Small);
        }
        private void ConsoleRequirement(string label, bool met) => ConsoleKicker((met ? "✓ " : "○ ") + label, met ? SpaceMinerUi.Success : SpaceMinerUi.Amber);
        private void ConsoleKicker(string text, Color color)
        { var style = new GUIStyle(ui.Small); style.normal.textColor = color; GUILayout.Label(text, style); }
        private bool ConsoleButton(string text, bool primary = false)
        { return GUILayout.Button(text, primary ? ui.Primary : ui.Button, GUILayout.MinHeight(ConsoleButtonHeight)); }
        private void ConsoleMeter(string label, float amount, float capacity, string unit, Color color)
        {
            GUILayout.Space(8); GUILayout.Label(label + "  " + amount.ToString("0.0") + " / " + capacity.ToString("0.0") + " " + unit, ui.Small);
            ConsoleProgress(amount / Mathf.Max(.001f, capacity), color);
        }
        private void ConsoleProgress(float fraction) => ConsoleProgress(fraction, SpaceMinerUi.Cyan);
        private void ConsoleProgress(float fraction, Color color)
        {
            var r = GUILayoutUtility.GetRect(40, 8, GUILayout.ExpandWidth(true));
            SpaceMinerUi.Fill(r, new Color(.11f, .20f, .26f)); SpaceMinerUi.Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height), color);
            GUILayout.Space(6);
        }
        private static Rect ConsoleInset(Rect r) => new Rect(r.x + 12, r.y + 14, r.width - 24, r.height - 28);
    }
}

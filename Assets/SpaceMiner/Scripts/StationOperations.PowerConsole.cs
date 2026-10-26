using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private Vector2 powerDashboardScroll, powerListScroll;
        private string powerSelection = "battery-bay-0", powerModelSelection;
        private int powerCategory = 1;
        private Rect powerDashboardBounds;
        private float powerContentHeight = 900;
        private StationPowerModelView powerStationView, powerItemView;
        private readonly Dictionary<string, Rect> powerHitAreas = new Dictionary<string, Rect>();
        private GUIStyle powerValue, powerSmall, powerText;
        public bool PowerPanelOpen => Live && panel == StationTerminalKind.Power;
        public Rect PowerDashboardBounds => PowerPanelOpen ? powerDashboardBounds : default;
        public int PowerDashboardLastRenderedFrame { get; private set; } = -1;
        public string PowerSelection => powerSelection;
        internal int PowerPreviewMeshCount => powerStationView?.SharedMeshCount ?? 0;
        internal int PowerDetailMeshCount => powerItemView?.SharedMeshCount ?? 0;
        internal IReadOnlyDictionary<string, Rect> PowerHitAreas => powerHitAreas;
        internal void ScrollPowerDashboard(Vector2 position) => powerDashboardScroll = position;
        public bool OpenPowerConsole()
        {
            if (!Live || !StationInteriorMode.IsInside || ForeignModal || Scenario.Scanner.Dialogue != null) return false;
            var terminal = NearestTerminal(StationInteriorMode.Current.FeetPosition);
            if (terminal == null || terminal.kind != StationTerminalKind.Power) return false;
            RefreshSupplyState(); OpenTerminal(terminal); return PowerPanelOpen;
        }
        internal bool SelectPowerItem(string id)
        {
            bool battery = id != null && id.StartsWith("battery-bay-", StringComparison.Ordinal)
                && int.TryParse(id.Substring(12), out int index) && index >= 0 && index < 8;
            bool generator = id == "reactor" || id == "solar-a" || id == "solar-b";
            bool consumer = id != null && id.StartsWith("consumer:", StringComparison.Ordinal)
                && powerConsumers.Exists(item => "consumer:" + item.Id == id);
            if (!battery && !generator && !consumer) return false;
            powerCategory = battery ? 1 : generator ? 0 : 2;
            if (powerSelection != id) { powerSelection = id; powerDashboardScroll = Vector2.zero; powerContentHeight = 900; }
            return true;
        }
        private void DisposePowerModels()
        {
            powerStationView?.Dispose(); powerItemView?.Dispose();
            powerStationView = powerItemView = null; powerModelSelection = null; powerHitAreas.Clear();
        }
        private IEnumerable<Transform> ReactorModelRoots()
        {
            var old = transform.Find("Station Geometry");
            if (old != null) foreach (Transform child in old)
                if (child.name == "Shielded Reactor" || child.name.StartsWith("Reactor", StringComparison.Ordinal)) yield return child;
            if (Layout.Geometry != null) foreach (var child in Layout.Geometry.Root.GetComponentsInChildren<Transform>())
                if (child.name == "Reaktorkapsel / zweilagige Wabenabdeckung") yield return child;
        }
        private IEnumerable<Transform> PowerStationModelRoots()
        {
            // The station's actual architecture and installed equipment, not nearby salvage contacts.
            yield return Layout.Geometry.Root;
            foreach (Transform child in Layout.transform)
                if (child.name.StartsWith("Solar", StringComparison.Ordinal) || child.name.StartsWith("Campaign water storage", StringComparison.Ordinal)) yield return child;
            foreach (var bay in batteryBays) yield return bay.transform;
            foreach (var root in ReactorModelRoots()) yield return root;
        }
        private void PreparePowerModels()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (powerStationView == null) powerStationView = new StationPowerModelView(Layout.transform, PowerStationModelRoots(), true);
            if (powerModelSelection == powerSelection) return;
            powerItemView?.Dispose(); powerItemView = null; powerModelSelection = powerSelection;
            if (powerSelection.StartsWith("battery-bay-", StringComparison.Ordinal))
            {
                int index = int.Parse(powerSelection.Substring(12));
                if (index < batteryBays.Count) powerItemView = new StationPowerModelView(batteryBays[index].transform,
                    new[] { batteryBays[index].transform }, false);
            }
            else if (powerSelection == "reactor") powerItemView = new StationPowerModelView(Layout.transform, ReactorModelRoots(), false);
            else if (powerSelection == "solar-a" || powerSelection == "solar-b")
            {
                var root = powerSelection == "solar-a" ? SolarPanelA : SolarPanelB;
                if (root != null) powerItemView = new StationPowerModelView(root, new[] { root }, false);
            }
        }
        private static string PowerNumber(float value) => value.ToString(Mathf.Abs(value) < .01f && value != 0 ? "0.000" : "0.00") + " kW";
        private static string BatteryAmount(StationBatteryBayView bay) => !bay.Installed ? "Kein Akku eingesetzt"
            : !bay.ChargeKnown ? "Ladung unbekannt / Telemetrie aus" : bay.StoredKwh.ToString("0.0") + " / " + bay.CapacityKwh.ToString("0.0") + " kWh";
        private static void PowerPanel(Rect r, Color accent)
        {
            SpaceMinerUi.Fill(r, new Color(.018f, .039f, .061f, .98f));
            SpaceMinerUi.Border(r, new Color(.12f, .4f, .51f, .75f));
            SpaceMinerUi.Fill(new Rect(r.x + 1, r.y + 1, Mathf.Min(54, r.width - 2), 2), accent);
        }
        private float PowerLabel(float x, float y, float width, string text, GUIStyle style)
        {
            float height = Mathf.Ceil(style.CalcHeight(new GUIContent(text), Mathf.Max(20, width))) + 5;
            GUI.Label(new Rect(x, y, width, height), text, style); return y + height;
        }
        private static void PowerLine(Vector2 a, Vector2 b, Color color, float thickness = 2)
        {
            if (Event.current.type != EventType.Repaint) return;
            var matrix = GUI.matrix;
            try
            {
                var d = b - a; GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
                SpaceMinerUi.Fill(new Rect(a.x, a.y - thickness * .5f, d.magnitude, thickness), color);
            }
            finally { GUI.matrix = matrix; }
        }
        private void PowerBar(Rect r, float fraction, Color color)
        {
            SpaceMinerUi.Fill(r, new Color(.065f, .12f, .16f));
            SpaceMinerUi.Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height), color);
        }
        private void DrawPowerGraph(Rect r)
        {
            PowerPanel(r, SpaceMinerUi.Cyan);
            GUI.Label(new Rect(r.x + 14, r.y + 8, r.width - 20, 30), "ENERGIEVERLAUF / kW", powerSmall);
            var plot = new Rect(r.x + 55, r.y + 48, r.width - 75, r.height - 91);
            float max = .5f; var samples = PowerHistory.Samples;
            foreach (var sample in samples) max = Mathf.Max(max, sample.Generation, sample.Consumption);
            max = Mathf.Ceil(max * 2 + .1f) / 2;
            for (int line = 0; line <= 4; line++)
            {
                float y = plot.yMax - plot.height * line / 4;
                PowerLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), new Color(.13f, .23f, .29f), 1);
                GUI.Label(new Rect(r.x + 5, y - powerSmall.lineHeight / 2 - 2, 48, powerSmall.lineHeight + 8), (max * line / 4).ToString("0.0"), powerSmall);
            }
            double end = Math.Max(60, PowerHistory.Seconds), start = Math.Max(0, end - 1800), span = end - start;
            Vector2 Point(StationPowerHistory.Sample sample, bool generation) => new Vector2(plot.x + (float)((sample.Seconds - start) / span) * plot.width,
                plot.yMax - (generation ? sample.Generation : sample.Consumption) / max * plot.height);
            for (int i = 1; i < samples.Count; i++)
            {
                if (samples[i - 1].Seconds < start) continue;
                PowerLine(Point(samples[i - 1], true), Point(samples[i], true), SpaceMinerUi.Cyan);
                PowerLine(Point(samples[i - 1], false), Point(samples[i], false), SpaceMinerUi.Amber);
            }
            if (samples.Count == 1) foreach (bool generation in new[] { true, false })
            {
                var p = Point(samples[0], generation);
                SpaceMinerUi.Fill(new Rect(p.x - 3, p.y - 3, 6, 6), generation ? SpaceMinerUi.Cyan : SpaceMinerUi.Amber);
            }
            GUI.Label(new Rect(plot.x, plot.yMax + 3, plot.width, 30), "Letzte " + (span / 60).ToString("0") + " min Spielzeit", powerSmall);
            if (samples.Count < 2) GUI.Label(new Rect(plot.x + 16, plot.y + 10, plot.width - 22, 48), "Verlauf entsteht während der Simulation", powerSmall);
        }
        private void DrawPowerMetric(Rect r, string title, string value, string note, Color accent)
        {
            PowerPanel(r, accent);
            float y = PowerLabel(r.x + 12, r.y + 12, r.width - 24, title, powerSmall);
            var style = new GUIStyle(powerValue); style.normal.textColor = accent;
            while (style.fontSize > 17 && style.CalcSize(new GUIContent(value)).x > r.width - 24) style.fontSize--;
            y = PowerLabel(r.x + 12, y + 8, r.width - 24, value, style);
            PowerLabel(r.x + 12, y + 9, r.width - 24, note, powerSmall);
        }
        private void PowerModel(Rect r, StationPowerModelView view)
        {
            if (view == null) return;
            if (Event.current.type == EventType.Repaint) view.Render(Mathf.RoundToInt(r.width * UiLayout.Scale()), Mathf.RoundToInt(r.height * UiLayout.Scale()));
            if (view.Texture != null) GUI.DrawTexture(r, view.Texture, ScaleMode.StretchToFill, false);
        }
        private void MapItem(Rect map, string id, Vector3 world, string caption, Color color, float offsetY = 0, float offsetX = 0)
        {
            if (powerStationView?.Texture == null) return;
            var point = powerStationView.Project(world, map);
            float width = id.StartsWith("battery", StringComparison.Ordinal) ? 42 : 100;
            var r = new Rect(Mathf.Clamp(point.x - width / 2 + offsetX, map.x + 3, map.xMax - width - 3),
                Mathf.Clamp(point.y - 14 + offsetY, map.y + 4, map.yMax - 32), width, 28);
            if (id == "solar-a") { r.x = map.xMax - width - 8; r.y = map.center.y - 14; }
            if (id == "solar-b") { r.x = map.x + 8; r.y = map.center.y - 14; }
            r = FreePowerCallout(r, map);
            PowerLine(point, r.center, color, 1); powerHitAreas[id] = r;
            var button = new GUIStyle(powerSelection == id ? ui.Primary : ui.Button)
            { padding = new RectOffset(2, 2, 1, 1), alignment = TextAnchor.MiddleCenter, wordWrap = false, fontSize = powerSmall.fontSize };
            while (button.fontSize > 12 && button.CalcSize(new GUIContent(caption)).x > width - 4) button.fontSize--;
            if (GUI.Button(r, new GUIContent(caption, caption), button))
            { SelectPowerItem(id); SettingsUiAudio.Activate(); }
        }
        private Rect FreePowerCallout(Rect anchor, Rect viewport)
        {
            bool Clear(Rect candidate)
            {
                foreach (var other in powerHitAreas.Values) if (candidate.Overlaps(other)) return false;
                return true;
            }
            if (Clear(anchor)) return anchor;
            // Labels may move away from their projected point, retaining a leader line.
            // This also keeps all11 targets individually reachable in short, wide windows.
            for (int radius = 1; radius <= 12; radius++) for (int direction = 0; direction < 8; direction++)
            {
                float angle = direction * Mathf.PI / 4;
                var candidate = anchor;
                candidate.x = Mathf.Clamp(anchor.x + Mathf.Cos(angle) * radius * 32, viewport.x + 3, viewport.xMax - anchor.width - 3);
                candidate.y = Mathf.Clamp(anchor.y + Mathf.Sin(angle) * radius * 32, viewport.y + 3, viewport.yMax - anchor.height - 3);
                if (Clear(candidate)) return candidate;
            }
            return anchor;
        }
        private void DrawPowerStation(Rect r)
        {
            PowerPanel(r, SpaceMinerUi.Cyan);
            GUI.Label(new Rect(r.x + 14, r.y + 8, r.width - 28, 30), "STATION / ANLAGE AUSWÄHLEN", powerSmall);
            var map = new Rect(r.x + 5, r.y + 41, r.width - 10, r.height - 49);
            PowerModel(map, powerStationView); powerHitAreas.Clear();
            for (int i = 0; i < batteryBays.Count; i++)
            {
                var bay = batteryBays[i]; var data = CaptureBatteryBay(i);
                var local = Layout.transform.InverseTransformPoint(bay.transform.position);
                float offsetX = bay.Axis % 2 == 0 ? Mathf.Sign(local.x) * 26 : bay.Axis == 1 ? 26 : -26;
                MapItem(map, "battery-bay-" + i, bay.transform.TransformPoint(new Vector3(0, 2, 0)),
                    (i + 1).ToString("00"), data.Coupled ? SpaceMinerUi.Amber : data.Installed ? SpaceMinerUi.Cyan : SpaceMinerUi.Disabled,
                    bay.Axis % 2 == 0 ? (bay.Axis == 0 ? -16 : 16) : -Mathf.Sign(local.z) * 22, offsetX);
            }
            MapItem(map, "reactor", Layout.transform.TransformPoint(Vector3.up * 5), "REAKTOR", SpaceMinerUi.Cyan);
            if (SolarPanelA != null) MapItem(map, "solar-a", SolarPanelA.position, "SOLAR A", SpaceMinerUi.Cyan);
            if (SolarPanelB != null) MapItem(map, "solar-b", SolarPanelB.position, "SOLAR B", SpaceMinerUi.Cyan);
        }
        private void DrawPowerList(Rect r, StationGenerationSnapshot generation)
        {
            PowerPanel(r, SpaceMinerUi.Cyan);
            GUI.Label(new Rect(r.x + 12, r.y + 8, r.width - 24, 30), powerCategory == 0 ? "ERZEUGER / EINSPEISUNG"
                : powerCategory == 1 ? "AKKUPLÄTZE / 2 BESTÜCKT · 1 AKTIV" : "VERBRAUCHER / ZUGETEILTE LEISTUNG", powerSmall);
            int count = powerCategory == 0 ? 3 : powerCategory == 1 ? 8 : powerConsumers.Count;
            float width = r.width - 42, total = 0;
            var ids = new string[count]; var captions = new string[count]; var heights = new float[count];
            for (int i = 0; i < count; i++)
            {
                string id, caption;
                if (powerCategory == 0)
                {
                    var source = i == 0 ? generation.Reactor : i == 1 ? generation.SolarA : generation.SolarB;
                    id = source.Id; caption = source.Name + " / " + PowerNumber(source.OutputKw) + (source.Connected ? " · am Netz" : " · getrennt");
                }
                else if (powerCategory == 1)
                {
                    var bay = CaptureBatteryBay(i); id = "battery-bay-" + i;
                    caption = "PLATZ " + (i + 1).ToString("00") + " / " + bay.Caption + " · " + BatteryAmount(bay);
                }
                else
                {
                    var source = powerConsumers[i]; id = "consumer:" + source.Id;
                    caption = source.Name + " / " + PowerNumber(source.SuppliedKw);
                }
                ids[i] = id; captions[i] = caption; heights[i] = Mathf.Max(44, ui.Button.CalcHeight(new GUIContent(caption), width) + 8); total += heights[i] + 5;
            }
            powerListScroll = GUI.BeginScrollView(new Rect(r.x + 8, r.y + 43, r.width - 16, r.height - 49), powerListScroll, new Rect(0, 0, width, total));
            float rowY = 0;
            for (int i = 0; i < count; i++)
            {
                string id = ids[i];
                if (GUI.Button(new Rect(0, rowY, width, heights[i]), captions[i], powerSelection == id ? ui.Primary : ui.Button))
                { SelectPowerItem(id); SettingsUiAudio.Activate(); }
                rowY += heights[i] + 5;
            }
            GUI.EndScrollView();
        }
        private float DetailLabel(float y, float width, string caption, bool title = false)
            => PowerLabel(14, y, width - 28, caption, title ? powerText : powerSmall);
        private float DetailBar(float y, float width, string caption, float fraction, Color color)
        {
            y = DetailLabel(y, width, caption + "  " + (fraction * 100).ToString("0") + "%");
            PowerBar(new Rect(14, y, width - 28, 7), fraction, color); return y + 19;
        }
        private void DrawPowerDetails(Rect r, StationGenerationSnapshot generation)
        {
            PowerPanel(r, SpaceMinerUi.Amber); float width = r.width - 38;
            powerDashboardScroll = GUI.BeginScrollView(new Rect(r.x + 5, r.y + 7, r.width - 10, r.height - 14), powerDashboardScroll,
                new Rect(0, 0, width, powerContentHeight));
            float y = 10;
            if (powerSelection.StartsWith("consumer:", StringComparison.Ordinal))
            {
                var item = powerConsumers.Find(c => "consumer:" + c.Id == powerSelection);
                // An order may finish while this inspector is open.
                if (item.Id == null) { y = DetailLabel(y, width, "VERBRAUCHER / NICHT MEHR AKTIV", true); }
                else {
                    y = DetailLabel(y, width, item.Name.ToUpperInvariant(), true);
                    y = DetailLabel(y + 14, width, PowerNumber(item.SuppliedKw) + " / AKTUELLE LEISTUNG", true);
                    y = DetailLabel(y + 14, width, "Bedarf: " + PowerNumber(item.RequestedKw));
                    y = DetailLabel(y + 6, width, item.RequestedKw <= .0001f ? "Bereit / derzeit kein Bedarf"
                        : item.SuppliedKw < item.RequestedKw - .001f ? "Unterversorgt / Leistung nicht vollständig zugeteilt" : "Versorgung gedeckt");
                }
                y = DetailLabel(y + 20, width, "Die Liste folgt der tatsächlichen Stromzuteilung. Lebenserhaltung, Systeme, Scanner und Havarieverluste teilen sich die priorisierte Grundversorgung. Aufträge und Drohnenladung folgen danach.");
            }
            else
            {
                bool battery = powerSelection.StartsWith("battery-bay-", StringComparison.Ordinal);
                var bay = battery ? CaptureBatteryBay(int.Parse(powerSelection.Substring(12))) : null;
                var source = powerSelection == "solar-a" ? generation.SolarA : powerSelection == "solar-b" ? generation.SolarB : generation.Reactor;
                y = DetailLabel(y, width, battery ? "AKKUPLATZ " + (bay.Index + 1).ToString("00") + " / " + bay.Caption
                    : source.Name.ToUpperInvariant() + (source.Connected ? " / AM NETZ" : " / GETRENNT"), true);
                float modelHeight = 168, modelWidth = (width - 42) * .4f;
                PowerModel(new Rect(14, y + 5, modelWidth, modelHeight), powerItemView);
                float summaryX = modelWidth + 28, summaryWidth = width - summaryX - 14, summaryY = y + 8;
                if (battery)
                {
                    summaryY = PowerLabel(summaryX, summaryY, summaryWidth, BatteryAmount(bay), powerText);
                    if (bay.ChargeKnown)
                    {
                        summaryY = PowerLabel(summaryX, summaryY + 9, summaryWidth, "LADUNG " + (bay.ChargeFraction * 100).ToString("0") + "%", powerSmall);
                        PowerBar(new Rect(summaryX, summaryY, summaryWidth, 7), bay.ChargeFraction, SpaceMinerUi.Cyan); summaryY += 16;
                        summaryY = PowerLabel(summaryX, summaryY, summaryWidth, "BANKZUSTAND " + (bay.Condition * 100).ToString("0") + "%", powerSmall);
                        PowerBar(new Rect(summaryX, summaryY, summaryWidth, 7), bay.Condition, SpaceMinerUi.Success); summaryY += 17;
                    }
                }
                else
                {
                    summaryY = PowerLabel(summaryX, summaryY, summaryWidth, PowerNumber(source.OutputKw), powerValue);
                    summaryY = PowerLabel(summaryX, summaryY, summaryWidth, "Elektrische Einspeisung\nNennleistung " + PowerNumber(source.NominalKw), powerSmall);
                    summaryY = PowerLabel(summaryX, summaryY + 8, summaryWidth, "ANLAGENZUSTAND " + (source.Condition * 100).ToString("0") + "%", powerSmall);
                    PowerBar(new Rect(summaryX, summaryY, summaryWidth, 7), source.Condition, SpaceMinerUi.Success); summaryY += 17;
                }
                y = Mathf.Max(y + modelHeight + 17, summaryY);
                if (battery) y = DrawBatteryInspector(y, width, bay, generation.TotalOutputKw - State.power.ConsumptionKw);
                else
                {
                    if (source.Id != "reactor")
                    {
                        y = DetailLabel(y, width, source.Id == "solar-a" ? "Rechter Flügel / Sektor 3" : "Linker Flügel / Sektor 9");
                        y = DetailLabel(y, width, source.OrientationKnown ? "Winkel zur Hauptsonne: " + source.AngleDegrees.ToString("0.0") + "°" : "Sonnenrichtung nicht verfügbar");
                        y = DetailBar(y + 6, width, "AUSRICHTUNGSFAKTOR", source.Incidence01, SpaceMinerUi.Cyan);
                        y = DrawSolarOrientation(y, width, source);
                        y = DetailLabel(y, width, "Ausrichtungsfaktor ≠ PV-Wirkungsgrad.\nBegleitsonne: nicht bilanziert · Wirkungsgrad: keine Telemetrie\nSonnennachführung: nicht installiert");
                    }
                    else
                    {
                        y = DetailLabel(y + 10, width, "ERWEITERTE DIAGNOSE", true);
                        y = DetailLabel(y, width, "Kühlmitteltemperatur  —\nKühlkreis  —\nBrennstoffreserve  —\nSchutzsysteme  —");
                        y = DetailLabel(y + 6, width, "Temperatur-, Brennstoff- und Schutztelemetrie: keine Daten verfügbar.");
                    }
                }
            }
            y += 18;
            if (GUI.Button(new Rect(14, y, width - 28, 48), "Sektornetz / Stromkreise öffnen", ui.Button))
            { panel = StationTerminalKind.Utilities; panelScroll = Vector2.zero; DisposePowerModels(); }
            y += 62; if (Event.current.type == EventType.Repaint) powerContentHeight = y;
            GUI.EndScrollView();
        }
        private float DrawBatteryInspector(float y, float width, StationBatteryBayView bay, float net)
        {
            if (bay.ChargeKnown)
            {
                y = DetailLabel(y + 5, width, "BANKBILANZ " + (net >= 0 ? "+" : "") + PowerNumber(net), true);
                y = DetailLabel(y, width, StationPowerHistory.Forecast(bay.StoredKwh, bay.CapacityKwh, net) + " · bei gleicher Bilanz");
            }
            for (int i = 0; i < 4; i++)
            {
                string state = !bay.Installed ? "Leer" : !bay.Coupled ? "Aus · Ladung / Zustand unbekannt"
                    : (bay.ChargeFraction * 100).ToString("0") + "% Ladung · " + (bay.Condition * 100).ToString("0") + "% Bankzustand"
                    + (net < -.0001f ? " · ↓ entlädt" : net > .0001f && bay.ChargeFraction < .9999f ? " · ↑ lädt" : " · → stabil");
                y = DetailLabel(y + 7, width, "MODUL " + (i + 1).ToString("00") + " / " + state);
                if (bay.ChargeKnown) { PowerBar(new Rect(14, y, width - 28, 5), bay.ChargeFraction, SpaceMinerUi.Cyan); y += 9; }
            }
            if (bay.ChargeKnown)
            {
                y = DetailLabel(y + 9, width, "Vier Kassetten werden gemeinsam überwacht. Lade- und Zustandswerte stammen aus derselben Bank; keine unabhängige Einzelzellmessung.");
            }
            else y = DetailLabel(y + 15, width, bay.Installed ? "Reserve elektrisch getrennt. Keine zusätzliche nutzbare Energie; Inbetriebnahme ist ein eigener Ausbauauftrag."
                : "Freier Einbauplatz. Keine zusätzliche Speicherkapazität vorhanden.");
            return y;
        }
        private float DrawSolarOrientation(float y, float width, StationGeneratorView source)
        {
            if (!source.OrientationKnown) return y;
            float length = Mathf.Min(110, width * .23f), angle = source.AngleDegrees * Mathf.Deg2Rad;
            var center = new Vector2(width * .47f, y + length + 32);
            var normal = center + Vector2.right * length;
            var sun = center + new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * length;
            PowerLine(center + Vector2.up * 22, center + Vector2.down * 22, SpaceMinerUi.Cyan, 5);
            PowerLine(center, normal, SpaceMinerUi.Cyan);
            PowerLine(center, sun, SpaceMinerUi.Amber, 3);
            SpaceMinerUi.Fill(new Rect(sun.x - 4, sun.y - 4, 8, 8), SpaceMinerUi.Amber);
            GUI.Label(new Rect(normal.x - 25, normal.y + 4, 125, 28), "Panelnormale", powerSmall);
            GUI.Label(new Rect(sun.x - 25, sun.y - 27, 145, 28), "Hauptsonne", powerSmall);
            return y + length + 72;
        }
        private void DrawPowerConsole(float w, float h)
        {
            powerDashboardBounds = new Rect((UiLayout.Width - w) / 2, (UiLayout.Height - h) / 2, w, h);
            if (Event.current.type == EventType.Repaint) PowerDashboardLastRenderedFrame = Time.frameCount;
            powerSmall = new GUIStyle(ui.Small); powerText = new GUIStyle(ui.Text) { fontStyle = FontStyle.Bold };
            var title = new GUIStyle(ui.Heading); title.normal.textColor = SpaceMinerUi.Cyan;
            powerValue = new GUIStyle(ui.Heading) { fontSize = Mathf.RoundToInt(29 * SettingsStore.Current.Accessibility.TextScale) };
            SpaceMinerUi.Fill(new Rect(0, 0, w, h), new Color(.008f, .018f, .03f, .99f));
            GUI.Label(new Rect(20, 12, w - 170, 46), "SPACEMINER / STROMVERSORGUNG", title);
            GUI.Label(new Rect(20, 58, w - 160, 38), SettingsStore.Current.Gameplay.PauseInMenu
                ? "Menüpause · Verlauf und Simulation ruhen bis zum Schließen" : "LIVE / Anlagenstatus und tatsächliche Stromzuteilung", powerSmall);
            if (ui.Tab(new Rect(w - 120, 14, 100, 42), "E / Zu", false)) { ClosePanel(); return; }
            PreparePowerModels(); var generation = CaptureGeneration(); var active = CaptureBatteryBay(StationBatteryPlacement.ActiveIndex);
            float net = generation.TotalOutputKw - State.power.ConsumptionKw;
            float top = 104, graphHeight = Mathf.Max(184, 174 * SettingsStore.Current.Accessibility.TextScale);
            float graphWidth = (w - 56) * .39f, metricWidth = (w - graphWidth - 80) / 4;
            DrawPowerGraph(new Rect(20, top, graphWidth, graphHeight)); float x = graphWidth + 30;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "ERZEUGUNG", PowerNumber(generation.TotalOutputKw), PowerHistory.Trend(true), SpaceMinerUi.Cyan); x += metricWidth + 10;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "VERBRAUCH", PowerNumber(State.power.ConsumptionKw), PowerHistory.Trend(false), SpaceMinerUi.Amber); x += metricWidth + 10;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "BILANZ", (net >= 0 ? "+" : "") + PowerNumber(net),
                !State.power.LifePowered ? "Grundlast nicht gedeckt" : net < -.0001f ? "↓ entlädt" : net > .0001f ? active.ChargeFraction >= .9999f ? "Voll / Überschuss" : "↑ lädt" : "→ ausgeglichen", SpaceMinerUi.Amber); x += metricWidth + 10;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "SPEICHER " + (active.ChargeFraction * 100).ToString("0") + "%",
                active.StoredKwh.ToString("0.0") + " / " + active.CapacityKwh.ToString("0") + " kWh",
                StationPowerHistory.Forecast(active.StoredKwh, active.CapacityKwh, net) + "\nBei gleicher Bilanz", SpaceMinerUi.Cyan);
            float bodyY = top + graphHeight + 14, bodyHeight = h - bodyY - 82, leftWidth = (w - 52) * .46f;
            var map = new Rect(20, bodyY, leftWidth, bodyHeight * .66f);
            DrawPowerStation(map);
            DrawPowerList(new Rect(map.x, map.yMax + 10, map.width, bodyHeight - map.height - 10), generation);
            DrawPowerDetails(new Rect(map.xMax + 12, bodyY, w - map.xMax - 32, bodyHeight), generation);
            string[] tabs = { "ERZEUGER  3 / " + PowerNumber(generation.TotalOutputKw), "SPEICHER  8 Plätze · 2 bestückt / 24 kWh",
                "VERBRAUCHER  " + powerConsumers.Count + " / " + PowerNumber(State.power.ConsumptionKw) };
            float tabWidth = (w - 60) / 3;
            for (int i = 0; i < 3; i++) if (ui.Tab(new Rect(20 + i * (tabWidth + 10), h - 62, tabWidth, 47), tabs[i], powerCategory == i))
            {
                powerCategory = i; powerListScroll = Vector2.zero;
                SelectPowerItem(i == 0 ? "reactor" : i == 1 ? "battery-bay-" + StationBatteryPlacement.ActiveIndex : "consumer:life");
            }
        }
    }
}

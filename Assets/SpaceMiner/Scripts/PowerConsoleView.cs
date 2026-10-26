using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpaceMiner
{
    // Host observations/actions. Never owns campaign state or writes settings.
    internal sealed class PowerConsoleData
    {
        public StationGenerationSnapshot Generation;
        public StationPowerHistory History;
        public List<StationPowerConsumer> Consumers;
        public Func<int, StationBatteryBayView> Battery;
        public float ConsumptionKw;
        public float StorageFlowKw,CurtailedKw;
        public bool HasStorageFlow;
        public bool LifePowered;
        public AccessibilitySettings Accessibility;
        public string StatusLine, CloseLabel = "E / Zu";
        public Action Close, OpenUtilities;
        public Action<string> PrepareModels;
        public Action<Rect> DrawStation;
        public Func<StationPowerModelView> DetailModel;
    }
    // One renderer for both game and isolated studio.
    internal sealed class PowerConsoleView : IDisposable
    {
        private SpaceMinerUi ui = new SpaceMinerUi();
        private PowerConsoleData data;
        private Vector2 powerDashboardScroll, powerListScroll;
        private string powerSelection = "battery-bay-0";
        private int powerCategory = 1;
        private float powerContentHeight = 900;
        private GUIStyle powerValue, powerSmall, powerText;
        public GUIStyle Small => powerSmall;
        public string Selection => powerSelection;
        public int LastRenderedFrame { get; private set; } = -1;
        public void Scroll(Vector2 p) => powerDashboardScroll = p;
        public void SetData(PowerConsoleData value) => data = value;
        public void Dispose() { ui.Dispose(); ui = new SpaceMinerUi(); }
        internal bool SelectPowerItem(string id)
        {
            bool battery = id != null && id.StartsWith("battery-bay-", StringComparison.Ordinal)
                && int.TryParse(id.Substring(12), out int index) && index >= 0 && index < 8;
            bool generator = id == "reactor" || id == "solar-a" || id == "solar-b";
            bool star=id=="sun-primary"||id=="sun-companion";
            bool consumer = id != null && id.StartsWith("consumer:", StringComparison.Ordinal)
                && data.Consumers.Exists(item => "consumer:" + item.Id == id);
            if (!battery && !generator && !consumer && !star) return false;
            powerCategory = battery ? 1 : generator ? 0 : star?3:2;
            if (powerSelection != id) { powerSelection = id; powerDashboardScroll = Vector2.zero; powerContentHeight = 900; }
            return true;
        }
        private static string PowerNumber(float value) => value.ToString(Mathf.Abs(value) < .01f && value != 0 ? "0.000" : "0.00") + " kW";
        private static string BatteryAmount(StationBatteryBayView bay) => !bay.Installed ? "Kein Akku eingesetzt"
            : !bay.ChargeKnown ? "Ladung unbekannt / Telemetrie aus" : bay.StoredKwh.ToString("0.0") + " / " + bay.CapacityKwh.ToString("0.0") + " kWh";
        private void PowerPanel(Rect r, Color accent)
        {
            UiChrome.Panel(r, accent, data.Accessibility.HighContrast);
        }
        private float PowerLabel(float x, float y, float width, string text, GUIStyle style)
        {
            float height = Mathf.Ceil(style.CalcHeight(new GUIContent(text), Mathf.Max(20, width))) + 5;
            GUI.Label(new Rect(x, y, width, height), text, style); return y + height;
        }
        internal static void PowerLine(Vector2 a, Vector2 b, Color color, float thickness = 2)
        {
            if (Event.current.type != EventType.Repaint) return;
            // Axis-aligned strips keep GUI group clipping and UI scaling intact.
            // Rotating GUI.matrix inside nested groups shifts the group's origin.
            var d = b - a;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Min(Mathf.Abs(d.x), Mathf.Abs(d.y))));
            for (int i = 0; i < steps; i++)
            {
                var p = a + d * ((float)i / steps);
                var q = a + d * ((float)(i + 1) / steps);
                SpaceMinerUi.Fill(new Rect(Mathf.Min(p.x, q.x) - thickness * .5f,
                    Mathf.Min(p.y, q.y) - thickness * .5f,
                    Mathf.Abs(q.x - p.x) + thickness, Mathf.Abs(q.y - p.y) + thickness), color);
            }
        }
        private void PowerBar(Rect r, float fraction, Color color)
        {
            UiChrome.Progress(r, fraction, color);
        }
        private void DrawPowerGraph(Rect r)
        {
            PowerPanel(r, SpaceMinerUi.Cyan);
            GUI.Label(new Rect(r.x + 14, r.y + 8, r.width - 20, 30), "ERZEUGUNG / VERBRAUCH · kW", powerSmall);
            var plot = new Rect(r.x + 55, r.y + 48, r.width - 75, r.height - 91);
            float max = .5f; var samples = data.History.Samples;
            foreach (var sample in samples) max = Mathf.Max(max, sample.Generation, sample.Consumption);
            max = Mathf.Ceil(max * 2 + .1f) / 2;
            for (int line = 0; line <= 4; line++)
            {
                float y = plot.yMax - plot.height * line / 4;
                PowerLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), new Color(.13f, .23f, .29f), 1);
                GUI.Label(new Rect(r.x + 5, y - powerSmall.lineHeight / 2 - 2, 48, powerSmall.lineHeight + 8), (max * line / 4).ToString("0.0"), powerSmall);
            }
            double end = Math.Max(60, data.History.Seconds), start = Math.Max(0, end - 1800), span = end - start;
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
        internal static void PowerModel(Rect r, StationPowerModelView view)
        {
            if (view == null) return;
            if (Event.current.type == EventType.Repaint) view.Render(Mathf.RoundToInt(r.width * UiLayout.Scale()), Mathf.RoundToInt(r.height * UiLayout.Scale()));
            if (view.Texture != null) GUI.DrawTexture(r, view.Texture, ScaleMode.StretchToFill, false);
        }
        private void DrawPowerList(Rect r, StationGenerationSnapshot generation)
        {
            PowerPanel(r, SpaceMinerUi.Cyan);
            GUI.Label(new Rect(r.x + 12, r.y + 8, r.width - 24, 30), powerCategory == 0 ? "ERZEUGER / EINSPEISUNG"
                : powerCategory == 1 ? "AKKUPLÄTZE / 2 BESTÜCKT · 1 AKTIV" : powerCategory==3?"SONNEN / EINSTRAHLUNG AM STANDORT":"VERBRAUCHER / ZUGETEILTE LEISTUNG", powerSmall);
            int count = powerCategory == 0 ? 3 : powerCategory == 1 ? 8 : powerCategory==3?2:data.Consumers.Count;
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
                    var bay = data.Battery(i); id = "battery-bay-" + i;
                    caption = "PLATZ " + (i + 1).ToString("00") + " / " + bay.Caption + " · " + BatteryAmount(bay);
                }
                else if(powerCategory==3)
                {
                    var star=i==0?generation.Stars.Primary:generation.Stars.Companion;
                    id=i==0?"sun-primary":"sun-companion";
                    caption=(star.Name??"Stern nicht hinterlegt")+" / "+star.IrradianceWm2.ToString("0.0")+" W/m²";
                }
                else
                {
                    var source = data.Consumers[i]; id = "consumer:" + source.Id;
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
                var item = data.Consumers.Find(c => "consumer:" + c.Id == powerSelection);
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
            else if(powerSelection=="sun-primary"||powerSelection=="sun-companion")
            {
                y=DrawStarInspector(y,width,generation.Stars,powerSelection=="sun-primary");
            }
            else
            {
                bool battery = powerSelection.StartsWith("battery-bay-", StringComparison.Ordinal);
                var bay = battery ? data.Battery(int.Parse(powerSelection.Substring(12))) : null;
                var source = powerSelection == "solar-a" ? generation.SolarA : powerSelection == "solar-b" ? generation.SolarB : generation.Reactor;
                y = DetailLabel(y, width, battery ? "AKKUPLATZ " + (bay.Index + 1).ToString("00") + " / " + bay.Caption
                    : source.Name.ToUpperInvariant() + (source.Connected ? " / AM NETZ" : " / GETRENNT"), true);
                float modelHeight = 168, modelWidth = (width - 42) * .4f;
                PowerModel(new Rect(14, y + 5, modelWidth, modelHeight), data.DetailModel?.Invoke());
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
                    summaryY = PowerLabel(summaryX, summaryY, summaryWidth, "Verfügbare Einspeisung\nNennleistung " + PowerNumber(source.NominalKw), powerSmall);
                    summaryY = PowerLabel(summaryX, summaryY + 8, summaryWidth, "ANLAGENZUSTAND " + (source.Condition * 100).ToString("0") + "%", powerSmall);
                    PowerBar(new Rect(summaryX, summaryY, summaryWidth, 7), source.Condition, SpaceMinerUi.Success); summaryY += 17;
                }
                y = Mathf.Max(y + modelHeight + 17, summaryY);
                if (battery) y = DrawBatteryInspector(y, width, bay, data.HasStorageFlow?data.StorageFlowKw:generation.TotalOutputKw-data.ConsumptionKw);
                else
                {
                    if (source.Id != "reactor")
                    {
                        y = DetailLabel(y, width, source.Id == "solar-a" ? "Rechter Flügel / Sektor 3" : "Linker Flügel / Sektor 9");
                        y = DetailLabel(y, width, source.OrientationKnown ? "Winkel zur Hauptsonne: " + source.AngleDegrees.ToString("0.0") + "°" : "Sonnenrichtung nicht verfügbar");
                        y = DetailBar(y + 6, width, "AUSRICHTUNGSFAKTOR", source.Incidence01, SpaceMinerUi.Cyan);
                        y = DrawSolarOrientation(y, width, source);
                        y=DrawSolarContributions(y,width,source);
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
            { data.OpenUtilities?.Invoke(); }
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
            if(source.SolarDetails.Known)
            {
                float otherAngle=source.SolarDetails.Companion.AngleDegrees*Mathf.Deg2Rad;
                var other=center+new Vector2(Mathf.Cos(otherAngle),-Mathf.Sin(otherAngle))*length*.8f;
                PowerLine(center,other,SpaceMinerUi.Success,2);
                GUI.Label(new Rect(other.x-25,other.y+10,150,28),"Begleitstern",powerSmall);
            }
            return y + length + 72;
        }
        private float DrawSolarContributions(float y,float width,StationGeneratorView source)
        {
            var f=source.SolarDetails;
            if(!f.Known)return DetailLabel(y,width,"Sonnenphysik für diesen Testfall nicht hinterlegt.");
            y=DetailLabel(y,width,"PV-Fläche "+f.GeometricAreaM2.ToString("0.0")+" m² / elektrisch nutzbar "+f.EffectiveAreaM2.ToString("0.0")+" m²");
            y=DetailLabel(y,width,"Zellwirkungsgrad "+(f.Efficiency*100).ToString("0")+"% · Panel/MPPT-Faktor "+(f.NetFactor*100).ToString("0")+"%");
            y=DetailLabel(y+10,width,"BEITRAG JE SONNE",true);
            y=DetailLabel(y,width,"Hauptsonne A: "+PowerNumber(f.Primary.AvailableKw)+"\nWinkel "+f.Primary.AngleDegrees.ToString("0.0")+"° · frei "+(f.Primary.Visibility*100).ToString("0")+"%");
            y=DetailLabel(y+6,width,"Begleitstern B: "+PowerNumber(f.Companion.AvailableKw)+"\nWinkel "+f.Companion.AngleDegrees.ToString("0.0")+"° · frei "+(f.Companion.Visibility*100).ToString("0")+"%");
            y=DetailLabel(y+8,width,"Schatten: Stichprobe pro vorhandenem PV-Segment. Netztrennung verhindert die Einspeisung. Sonnennachführung: nicht installiert.");
            return y;
        }
        private float DrawStarInspector(float y,float width,BinaryStarView stars,bool primary)
        {
            if(!stars.Known)return DetailLabel(y,width,"Keine Sternparameter hinterlegt.",true);
            var star=primary?stars.Primary:stars.Companion;
            y=DetailLabel(y,width,star.Name.ToUpperInvariant(),true);
            y=DetailLabel(y+10,width,star.IrradianceWm2.ToString("0.0")+" W/m² am Standort",true);
            y=DetailLabel(y+12,width,"Ruhiger K-Zwerg / Referenzzustand\nOberfläche "+star.TemperatureK.ToString("0")+" K\nRadius "+star.RadiusSolar.ToString("0.00")+" Sonnenradien\nLeuchtkraft "+star.LuminositySolar.ToString("0.000")+" × Sonne\nStrahlungsleistung "+star.LuminosityWatts.ToString("0.000E+0")+" W");
            y=DetailLabel(y+14,width,"ENTFERNUNG",true);
            y=DetailLabel(y,width,"Energieentfernung "+star.DistanceAu.ToString("0.000")+" AE\n"+(star.DistanceAu*149.5978707).ToString("0.0")+" Mio. km physikalisch\nKulissenabstand "+star.DisplayDistanceKm.ToString("0.0")+" km");
            y=DetailLabel(y+12,width,"Die Kulisse bleibt verkürzt: 1.800 Darstellungs-km entsprechen für die Energiephysik einer AE. Lokale Stationsmaße bleiben in Metern.");
            y=DetailLabel(y+14,width,"BEIDE SONNEN",true);
            y=DetailLabel(y,width,"Zusammen "+stars.TotalIrradianceWm2.ToString("0.0")+" W/m² vor Panelprojektion. Entfernung, Winkel und Abschattung werden für jede Sonne einzeln berechnet.");
            y=DetailLabel(y+12,width,"Modelliert wird Energieeinstrahlung. UV-/Teilchendosis und Sonnenstürme sind nicht simuliert; dies ist keine Dosismessung.");
            return y;
        }
        public void Draw(float w, float h)
        {
            ui.Configure(data.Accessibility);
            if (Event.current.type == EventType.Repaint) LastRenderedFrame = Time.frameCount;
            powerSmall = new GUIStyle(ui.Small); powerText = new GUIStyle(ui.Text) { fontStyle = FontStyle.Bold };
            var title = new GUIStyle(ui.Heading); title.normal.textColor = SpaceMinerUi.Cyan;
            powerValue = new GUIStyle(ui.Heading) { fontSize = Mathf.RoundToInt(29 * data.Accessibility.TextScale) };
            ui.Panel(new Rect(0, 0, w, h));
            GUI.Label(new Rect(20, 12, w - 170, 46), "SPACEMINER / STROMVERSORGUNG", title);
            GUI.Label(new Rect(20, 58, w - 160, 38), data.StatusLine, powerSmall);
            if (ui.Tab(new Rect(w - 120, 14, 100, 42), data.CloseLabel, false)) { data.Close?.Invoke(); return; }
            data.PrepareModels?.Invoke(powerSelection); var generation = data.Generation; var active = data.Battery(StationBatteryPlacement.ActiveIndex);
            float net = data.HasStorageFlow?data.StorageFlowKw:generation.TotalOutputKw - data.ConsumptionKw;
            float top = 104, graphHeight = Mathf.Max(184, 174 * data.Accessibility.TextScale);
            float graphWidth = (w - 56) * .39f, metricWidth = (w - graphWidth - 80) / 4;
            DrawPowerGraph(new Rect(20, top, graphWidth, graphHeight)); float x = graphWidth + 30;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "VERFÜGBAR", PowerNumber(generation.TotalOutputKw), data.CurtailedKw>.001f?PowerNumber(data.CurtailedKw)+" abgeregelt":data.History.Trend(true), SpaceMinerUi.Cyan); x += metricWidth + 10;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "VERBRAUCH", PowerNumber(data.ConsumptionKw), data.History.Trend(false), SpaceMinerUi.Amber); x += metricWidth + 10;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "AKKUBILANZ", (net >= 0 ? "+" : "") + PowerNumber(net),
                !data.LifePowered ? "Grundlast nicht gedeckt" : net < -.0001f ? "↓ entlädt" : net > .0001f ? active.ChargeFraction >= .9999f ? "Voll / Überschuss" : "↑ lädt" : "→ ausgeglichen", SpaceMinerUi.Amber); x += metricWidth + 10;
            DrawPowerMetric(new Rect(x, top, metricWidth, graphHeight), "SPEICHER " + (active.ChargeFraction * 100).ToString("0") + "%",
                active.StoredKwh.ToString("0.0") + " / " + active.CapacityKwh.ToString("0") + " kWh",
                StationPowerHistory.Forecast(active.StoredKwh, active.CapacityKwh, net) + "\nBei gleicher Bilanz", SpaceMinerUi.Cyan);
            float bodyY = top + graphHeight + 14, bodyHeight = h - bodyY - 82, leftWidth = (w - 52) * .46f;
            var map = new Rect(20, bodyY, leftWidth, bodyHeight * .66f);
            data.DrawStation?.Invoke(map);
            DrawPowerList(new Rect(map.x, map.yMax + 10, map.width, bodyHeight - map.height - 10), generation);
            DrawPowerDetails(new Rect(map.xMax + 12, bodyY, w - map.xMax - 32, bodyHeight), generation);
            string[] tabs = { "ERZEUGER  3", "SPEICHER  8",
                "VERBRAUCHER  " + data.Consumers.Count,"SONNEN  2" };
            float tabWidth = (w - 70) / 4;
            for (int i = 0; i < 4; i++) if (ui.Tab(new Rect(20 + i * (tabWidth + 10), h - 62, tabWidth, 47), tabs[i], powerCategory == i))
            {
                powerCategory = i; powerListScroll = Vector2.zero;
                SelectPowerItem(i == 0 ? "reactor" : i == 1 ? "battery-bay-" + StationBatteryPlacement.ActiveIndex : i==3?"sun-primary":"consumer:life");
            }
        }
    }
}

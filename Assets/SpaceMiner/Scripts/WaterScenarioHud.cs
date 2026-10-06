using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    [RequireComponent(typeof(OrbitCamera))]
    public sealed class WaterScenarioHud : MonoBehaviour
    {
        private WaterScenario scenario;
        private OrbitCamera orbit;
        private Camera view;
        private DisplayModeController display;
        private float frameSeconds;
        private int frameCount;
        private float framesPerSecond;
        private GUIStyle title, text, muted, button, marker;
        private readonly List<Rect> markers = new List<Rect>();
        private readonly List<AsteroidResource> knownSources = new List<AsteroidResource>();
        private static readonly Color Cyan = new Color(0.35f, 0.85f, 0.95f);
        private float Scale => Mathf.Clamp(Mathf.Min(Screen.height / 900f, Screen.width / 1440f) * SettingsStore.Current.Interface.Scale, 0.35f, 1.5f);
        private float Width => Screen.width / Scale;
        private float Height => Screen.height / Scale;
        private Rect LeftTop => new Rect(18, 18, 310, 192);
        private Rect Quest => new Rect(18, 224, 310, 260);
        private Rect Controls => new Rect(18, Height - 154, 540, 136);
        private Rect Inspector => new Rect(Width - 358, 64, 340, 622);
        private Rect Clock => new Rect(Width - 358, Height - 164, 340, 146);
        private Rect Notice => new Rect(346, 18, Mathf.Max(140, Width - 722), 94);

        private void Start()
        {
            scenario = FindFirstObjectByType<WaterScenario>();
            orbit = GetComponent<OrbitCamera>();
            view = GetComponent<Camera>();
            display = GetComponent<DisplayModeController>();
            foreach (AsteroidResource source in scenario.Asteroids)
                if (source.WaterIdentified) knownSources.Add(source);
        }

        private void Update()
        {
            frameSeconds += Time.unscaledDeltaTime;
            frameCount++;
            if (frameSeconds < 0.5f) return;
            framesPerSecond = frameCount / frameSeconds;
            frameSeconds = 0;
            frameCount = 0;
        }

        public bool OwnsScreenPoint(Vector3 mouse)
        {
            Vector2 point = new Vector2(mouse.x, Screen.height - mouse.y) / Scale;
            if (IsPanel(point)) return true;
            foreach (Rect rect in markers) if (rect.Contains(point)) return true;
            return false;
        }

        private bool IsPanel(Vector2 point) => LeftTop.Contains(point) || Quest.Contains(point)
            || (SettingsStore.Current.Gameplay.ShowControlHints && Controls.Contains(point)) || Inspector.Contains(point) || Clock.Contains(point) || Notice.Contains(point);

        private void OnGUI()
        {
            if (IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput) return;
            if (scenario == null || orbit == null || !orbit.ShowHud) return;
            Styles();
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Scale);
            markers.Clear();
            DrawWorld(scenario.GetComponent<SpaceObject>(), "STATION", null);
            DrawWorld(scenario.Worker.Info, "DROHNE 01", scenario.Worker);
            foreach (AsteroidResource asteroid in knownSources)
                if (asteroid.WaterIdentified) DrawWorld(asteroid.Info, asteroid.Info.DisplayName + " · EIS", null);

            Panel(LeftTop);
            Label(34, 29, 275, 32, "SPACE MINER", title);
            Label(34, 64, 275, 22, "WASSER SICHERN", muted);
            Label(34, 95, 275, 24, "Stationstank   " + scenario.WaterLiters.ToString("0.0") + " / " + scenario.TankCapacityLiters.ToString("0") + " L");
            Bar(new Rect(34, 125, 278, 10), scenario.WaterLiters / scenario.TankCapacityLiters);
            Label(34, 146, 280, 22, "Reaktor " + scenario.ReactorPowerKw.ToString("0.0") + " kW  ·  Solar " + scenario.SolarPowerKw.ToString("0.0") + " kW", muted);
            Label(34, 172, 280, 24, "1 bereit  ·  1 wartet auf Ladung  ·  8 defekt", muted);

            Panel(Quest);
            Label(34, 238, 278, 24, "AUFTRAG: TANK BEFÜLLEN", muted);
            Label(34, 275, 278, 24, Tick(scenario.SourceAssigned) + "Eisquelle zuweisen");
            Label(34, 309, 278, 24, Tick(scenario.Deliveries > 0) + "Erste Wasserlieferung");
            Label(34, 343, 278, 24, Tick(scenario.QuestComplete) + "200 Liter Wasser sichern");
            Label(34, 379, 278, 44, scenario.Deliveries + " Lieferungen  ·  " + scenario.DeliveredLiters.ToString("0.0") + " L gewonnen\nDrohne 01: " + scenario.Worker.Status, muted);
            if (GUI.Button(new Rect(34, 435, 278, 32), "Drohne 01 auswählen", button)) orbit.Select(scenario.Worker.Info);

            if (SettingsStore.Current.Gameplay.ShowControlHints) {
            Panel(Controls);
            float cy = Controls.y;
            Label(34, cy + 12, 500, 20, "STEUERUNG  ·  Kamera " + OrbitCamera.FormatDistance(orbit.Distance), muted);
            Label(34, cy + 36, 508, 22, "Mausrad Zoom  ·  Shift + Rad Schnellzoom  ·  Rechtsziehen Drehen", muted);
            Label(34, cy + 59, 508, 22, "Mittelziehen Verschieben  ·  " + SettingsStore.Current.Controls.Bindings.Get(CameraAction.Forward) + "/" + SettingsStore.Current.Controls.Bindings.Get(CameraAction.Left) + "/" + SettingsStore.Current.Controls.Bindings.Get(CameraAction.Backward) + "/" + SettingsStore.Current.Controls.Bindings.Get(CameraAction.Right) + " Bewegen  ·  Shift Schnell", muted);
            Label(34, cy + 82, 508, 22, SettingsStore.Current.Controls.Bindings.Get(CameraAction.Reset) + " Startansicht  ·  " + SettingsStore.Current.Controls.Bindings.Get(CameraAction.Overview) + " Feldansicht  ·  " + SettingsStore.Current.Controls.Bindings.Get(CameraAction.Focus) + " Fokus  ·  Tab Drohne", muted);
            Label(34, cy + 105, 508, 22, "Leertaste Pause  ·  F11 Vollbild / Fenster  ·  H Anzeige  ·  Escape Schließen", muted);

            Panel(Inspector);
            }
            DrawInspector(Inspector.x + 16, Inspector.y + 16);
            Panel(Clock);
            float cx = Clock.x + 16, ty = Clock.y + 12;
            Label(cx, ty, 310, 20, scenario.SimulationRate == 0 ? "SIMULATION PAUSIERT" : "ZEITRAFFER  " + scenario.SimulationRate.ToString("0") + "×", muted);
            float[] rates = { 1, 20, 100, 500 };
            for (int i = 0; i < rates.Length; i++)
                if (GUI.Button(new Rect(cx + i * 77, ty + 30, 69, 27), rates[i] + "×", button)) scenario.SimulationRate = rates[i];
            if (GUI.Button(new Rect(cx, ty + 64, 300, 24), scenario.SimulationRate > 0 ? "Pause" : "Weiter (100×)", button))
                scenario.SimulationRate = scenario.SimulationRate > 0 ? 0 : 100;
            if (display != null)
            {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = display.CanSwitch;
                string label = display.IsFullscreen ? "Zum Fenster wechseln (F11)" : "Vollbild einschalten (F11)";
                if (GUI.Button(new Rect(cx, ty + 98, 300, 24), label, button)) display.Toggle();
                GUI.enabled = previousEnabled;
            }
            Panel(Notice);
            Label(Notice.x + 12, Notice.y + 10, Notice.width - 24, 54, scenario.Message, muted);
            Label(Notice.x + 12, Notice.y + 68, Notice.width - 24, 20,
                (GetComponent<SpiralBelt>() != null && GetComponent<SpiralBelt>().IsReady ? GetComponent<SpiralBelt>().AsteroidCount : scenario.Asteroids.Length).ToString("N0") + " Asteroiden  ·  " + framesPerSecond.ToString("0") + " FPS", muted);
            GUI.matrix = previous;
        }

        private void DrawInspector(float x, float y)
        {
            Label(x, y, 308, 22, "OBJEKTINFORMATION", muted);
            y += 34;
            SpaceObject selected = orbit.Selected;
            if (selected == null)
            {
                Label(x, y, 308, 80, "Wähle einen Asteroiden im Weltraum oder hier eine bekannte Eisquelle. Weise danach Drohne 01 den Tankauftrag zu.");
                y += 100;
                foreach (AsteroidResource source in knownSources)
                {
                    if (!source.WaterIdentified) continue;
                    if (GUI.Button(new Rect(x, y, 308, 36), source.Info.DisplayName + " · " + source.KnownWaterPercent.ToString("0") + "% Wasser", button)) orbit.Select(source.Info);
                    y += 46;
                }
                return;
            }
            Label(x, y, 308, 26, selected.DisplayName, title);
            y += 38;
            Label(x, y, 308, 23, "Größe  " + OrbitCamera.FormatDistance(selected.DiameterMeters), muted);
            y += 35;
            DroneAgent drone = selected.GetComponent<DroneAgent>();
            AsteroidResource asteroid = selected.GetComponent<AsteroidResource>();
            if (drone != null)
            {
                Label(x, y, 308, 25, "Status   " + drone.Status); y += 34;
                Label(x, y, 308, 24, "Batterie   " + drone.BatteryKwh.ToString("0.00") + " / " + drone.BatteryCapacityKwh + " kWh"); y += 27;
                Bar(new Rect(x, y, 308, 8), drone.BatteryKwh / drone.BatteryCapacityKwh); y += 22;
                Label(x, y, 308, 24, "Treibwasser   " + drone.FuelLiters.ToString("0.00") + " / " + drone.FuelCapacityLiters + " L"); y += 30;
                Label(x, y, 308, 24, "Ladung   " + drone.CargoKg.ToString("0.0") + " / " + drone.CargoCapacityKg + " kg"); y += 30;
                Label(x, y, 308, 24, "Geschwindigkeit   " + drone.Speed.ToString("0.00") + " m/s"); y += 30;
                Label(x, y, 308, 24, "Zielentfernung   " + OrbitCamera.FormatDistance(drone.TargetDistance)); y += 30;
                Label(x, y, 308, 24, "Ankunft in   " + (drone.IsFlying ? WaterScenario.Duration(drone.ArrivalSeconds) : "—")); y += 30;
                Label(x, y, 308, 24, "Abbau verbleibend   " + (drone.Phase == DronePhase.Mining ? WaterScenario.Duration(drone.MiningSecondsRemaining) : "—")); y += 30;
                Label(x, y, 308, 24, "Phase verbleibend   " + WaterScenario.Duration(drone.PhaseSecondsRemaining)); y += 30;
                Label(x, y, 308, 22, "Zeitangaben in Spielzeit", muted); y += 28;
                Bar(new Rect(x, y, 308, 9), drone.PhaseProgress); y += 22;
                Label(x, y, 308, 24, "Eisquelle   " + (drone.Target == null ? "—" : drone.Target.Info.DisplayName), muted); y += 30;
                if (!drone.IsOperational) { Label(x, y, 308, 42, "Havarieschaden. Eine Reparatur ist später möglich.", muted); y += 50; }
                if (drone.NeedsInitialCharge) { Label(x, y, 308, 60, "Funktionsfähig, aber ohne Batterieladung und Treibwasser. Die Freigabe folgt in einem späteren Spielschritt.", muted); y += 68; }
                if (drone.HasTankOrder && GUI.Button(new Rect(x, y, 308, 30), "Auftrag beenden und zurückkehren", button)) drone.ReturnToShip();
                y += 40;
            }
            else if (asteroid != null)
            {
                Label(x, y, 308, 24, "BEKANNTE ZUSAMMENSETZUNG", muted); y += 34;
                Label(x, y, 308, 24, "Wasser   " + asteroid.KnownWaterPercent.ToString("0") + "%"); y += 32;
                Bar(new Rect(x, y, 308, 10), asteroid.KnownWaterPercent / 100f); y += 24;
                Label(x, y, 308, 24, "Unbekannt   " + asteroid.UnknownPercent.ToString("0") + "%"); y += 36;
                Label(x, y, 308, 65, "Weitere Bestandteile erfordern bessere Sensoren und Forschung.", muted); y += 72;
                if (asteroid.WaterIdentified)
                {
                    Label(x, y, 308, 50, "Erreichbares Vorkommen\n" + asteroid.RemainingRawKg.ToString("0.0") + " kg Eisgemisch", muted); y += 65;
                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = scenario.Worker.IsReady && asteroid.CanMineWater && !scenario.QuestComplete;
                    if (GUI.Button(new Rect(x, y, 308, 42), "Drohne 01: Stationstank befüllen", button)) scenario.AssignTankOrder(asteroid);
                    GUI.enabled = oldEnabled;
                    y += 62;
                }
                else { Label(x, y, 308, 60, "Kein Wasser bestätigt. Hier kann noch kein Tankauftrag gestartet werden.", muted); y += 72; }
            }
            else
            {
                Label(x, y, 308, 80, "Beschädigte modulare Raumstation. Solarflächen und Reaktor versorgen die Ladestation."); y += 95;
            }
            if (GUI.Button(new Rect(x, y, 308, 30), drone != null ? "Fokus / Drohne folgen (F)" : "Objekt fokussieren (F)", button)) orbit.Focus(selected);
        }

        private void DrawWorld(SpaceObject item, string label, DroneAgent drone)
        {
            Vector3 point = view.WorldToScreenPoint(item.transform.position + Vector3.up * (item.DiameterMeters * 0.5f + 0.3f));
            if (point.z < view.nearClipPlane || point.z > view.farClipPlane || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) return;
            float x = point.x / Scale - 88, y = (Screen.height - point.y) / Scale - 24;
            Rect rect = new Rect(x, y, 176, drone != null ? 53 : 27);
            if (IsPanel(rect.center) || IsPanel(rect.min) || IsPanel(rect.max)) return;
            // Nearby objects converge to the same pixels in the belt overview.
            foreach (Rect existing in markers) if (existing.Overlaps(rect)) return;
            markers.Add(rect);
            if (GUI.Button(new Rect(x, y, 176, 26), label, marker)) orbit.Select(item);
            if (drone != null)
            {
                Label(x, y + 26, 176, 20, drone.Status + "  " + (drone.PhaseProgress * 100).ToString("0") + "%", muted);
                Bar(new Rect(x, y + 48, 176, 5), drone.PhaseProgress);
            }
        }

        private static string Tick(bool complete) => complete ? "✓  " : "○  ";
        private void Label(float x, float y, float w, float h, string value, GUIStyle style = null) => GUI.Label(new Rect(x, y, w, h), value, style ?? text);
        private static void Panel(Rect rect) => Fill(rect, new Color(0.018f, 0.035f, 0.055f, 0.94f));
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private static void Bar(Rect rect, float progress)
        {
            Fill(rect, new Color(0.13f, 0.2f, 0.25f));
            rect.width *= Mathf.Clamp01(progress); Fill(rect, Cyan);
        }
        private void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            muted = new GUIStyle(text) { fontSize = 12 };
            title.normal.textColor = new Color(0.83f, 0.94f, 1);
            text.normal.textColor = new Color(0.82f, 0.87f, 0.92f);
            muted.normal.textColor = new Color(0.57f, 0.7f, 0.78f);
            button = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true };
            marker = new GUIStyle(button); marker.normal.textColor = Cyan;
        }
    }
}

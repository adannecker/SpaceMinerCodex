using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // An empty scene with a UI host: no WaterScenario, campaign, room plans or save menu.
    public sealed class ConsoleStudio : MonoBehaviour
    {
        private readonly PowerConsoleView power = new PowerConsoleView();
        private readonly SpaceMinerUi chrome = new SpaceMinerUi();
        private readonly AccessibilitySettings accessibility = new AccessibilitySettings();
        private readonly StationPowerHistory history = new StationPowerHistory();
        private readonly List<StationPowerConsumer> consumers = new List<StationPowerConsumer>();
        private PowerConsoleData data;
        private int fixture;
        private bool toolbar = true, opened = true, utilities;
        private float stored = 12, uiScale = 1;
        private string error;
        private static readonly string[] Fixtures = { "Normalbetrieb", "Akku voll", "Strommangel / leer", "Lange Texte" };

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Application.logMessageReceived += OnLog;
            SetFixture(0);
        }
        private void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-consoleStudioCheck") >= 0)
                StartCoroutine(Check());
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) toolbar = !toolbar;
            if (Input.GetKeyDown(KeyCode.Escape)) { utilities = false; opened = true; toolbar = true; }
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) error = message + "\n" + stack;
        }
        private void SetFixture(int index)
        {
            fixture = index; stored = index == 1 ? 24 : index == 2 ? 0 : 12;
            var stars=BinaryStarEnergy.Observe(Vector3.zero);
            var a=new SolarFieldView(stars,Vector3.up,353.2672f,386.386f,.8f,1,1,.6f);
            var b=new SolarFieldView(stars,stars.Primary.Direction,375.3464f,386.386f,.7f,1,1,1);
            var generation = new StationGenerationSnapshot(
                StationGeneratorView.Reactor("reactor", "Reaktor", 2, .82f, index != 2),
                StationGeneratorView.SolarPhysical("solar-a","Solar A",.8f,false,Vector3.up,stars,a),
                StationGeneratorView.SolarPhysical("solar-b","Solar B",.7f,index!=2,stars.Primary.Direction,stars,b),stars);
            consumers.Clear();
            consumers.Add(new StationPowerConsumer("life", index == 3
                ? "Lebenserhaltung / Sauerstoffaufbereitung und Versorgung des beschädigten Stationssektors"
                : "Lebenserhaltung", .35f, index == 2 ? .1f : .35f));
            consumers.Add(new StationPowerConsumer("systems", "Stationssysteme", .4f, index == 2 ? 0 : .4f));
            consumers.Add(new StationPowerConsumer("charging", "Drohnen-Ladestation", .6f, index == 2 ? 0 : .6f));
            float consumption = 0; foreach (var item in consumers) consumption += item.SuppliedKw;
            history.Reset();
            for (int i = 0; i <= 60; i++) history.Observe(10, generation.TotalOutputKw,
                consumption * (.85f + .15f * Mathf.Cos(i * .2f)));
            float surplus=generation.TotalOutputKw-consumption;
            float storage=surplus>0?Mathf.Min(surplus,stored>=24?0:12):surplus;
            data = new PowerConsoleData {
                Generation = generation, History = history, Consumers = consumers,
                ConsumptionKw = consumption, LifePowered = index != 2, Accessibility = accessibility,
                HasStorageFlow=true,StorageFlowKw=storage,CurtailedKw=Mathf.Max(0,surplus-storage),
                Battery = i => new StationBatteryBayView(i, stored, 24, .62f, index != 2),
                StatusLine = "LAYOUTTEST / TESTDATEN · " + Fixtures[index] + " · keine Spielsimulation",
                CloseLabel = "Übersicht", Close = () => opened = false,
                OpenUtilities = () => utilities = true, DrawStation = DrawSchematic
            };
            power.SetData(data);
        }
        private void OnGUI()
        {
            UiLayout.Preview(uiScale);
            using var scope = new UiLayout.Scope(UiLayout.Scale());
            chrome.Configure(accessibility);
            float w = UiLayout.Width, h = UiLayout.Height, top = toolbar ? 116 : 0;
            SpaceMinerUi.Fill(new Rect(0, 0, w, h), new Color(.008f, .018f, .03f));
            if (toolbar) DrawToolbar(w);
            if (!opened)
            {
                GUI.Label(new Rect(28, top + 30, w - 56, 70), "KONSOLENSTUDIO / Stromversorgung", chrome.Heading);
                GUI.Label(new Rect(28, top + 100, w - 56, 70), "Weitere Konsolen sind noch nicht eingebunden. F1 blendet die Testleiste aus oder ein.", chrome.Text);
                if (GUI.Button(new Rect(28, top + 180, 350, 54), "Stromversorgung öffnen", chrome.Primary)) opened = true;
                return;
            }
            GUI.BeginGroup(new Rect(16, top + 16, w - 32, h - top - 32));
            power.Draw(w - 32, h - top - 32);
            GUI.EndGroup();
            if (utilities)
            {
                var r = new Rect((w - 650) / 2, (h - 240) / 2, 650, 240);
                chrome.Panel(r);
                GUI.Label(new Rect(r.x + 24, r.y + 24, 600, 110),
                    "Sektornetz ist noch nicht im Konsolenstudio eingebunden. Hier werden keine Spielaufträge oder Netzschaltungen ausgeführt.", chrome.Text);
                if (GUI.Button(new Rect(r.x + 24, r.y + 155, 600, 52), "Zurück zur Stromversorgung", chrome.Button)) utilities = false;
            }
        }
        private void DrawToolbar(float w)
        {
            GUI.Label(new Rect(20, 8, 680, 34), "KONSOLENSTUDIO / LAYOUTTEST", chrome.Heading);
            GUI.Label(new Rect(w - 390, 12, 370, 28), Screen.width + " × " + Screen.height + " · F1: volle UI", chrome.Small);
            for (int i = 0; i < Fixtures.Length; i++)
                if (chrome.Tab(new Rect(20 + i * 190, 46, 180, 32), Fixtures[i], fixture == i)) SetFixture(i);
            if (GUI.Button(new Rect(800, 46, 150, 32), "Schrift " + (accessibility.TextScale * 100).ToString("0") + "%", chrome.Button))
                accessibility.TextScale = accessibility.TextScale < 1.2f ? 1.4f : 1;
            if (GUI.Button(new Rect(960, 46, 150, 32), "UI " + (uiScale * 100).ToString("0") + "%", chrome.Button))
                uiScale = uiScale < 1 ? 1 : uiScale < 1.5f ? 1.5f : .75f;
            int[] widths = { 1280, 1600, 1920 }; int[] heights = { 720, 1000, 1080 };
            for (int i = 0; i < widths.Length; i++)
                if (GUI.Button(new Rect(20 + i * 190, 84, 180, 28), widths[i] + " × " + heights[i], chrome.Button))
                    Screen.SetResolution(widths[i], heights[i], FullScreenMode.Windowed);
            GUI.Label(new Rect(610, 84, w - 630, 28), "Stromversorgung · gemeinsamer Spiel-Renderer / Schema statt Stationsmodell", chrome.Small);
        }
        private void DrawSchematic(Rect r)
        {
            UiChrome.Panel(r, SpaceMinerUi.Cyan, accessibility.HighContrast);
            GUI.Label(new Rect(r.x + 14, r.y + 10, r.width - 28, 46), "TESTSCHEMA / KEINE STATIONSGEOMETRIE", chrome.Small);
            Vector2 center = new Vector2(r.center.x, r.center.y + 10);
            float radius = Mathf.Min(r.width * .27f, r.height * .28f);
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48, b = (i + 1) * Mathf.PI * 2 / 48;
                PowerConsoleView.PowerLine(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, SpaceMinerUi.Cyan);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                var pos = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                string id = "battery-bay-" + i;
                if (chrome.Tab(new Rect(pos.x - 24, pos.y - 15, 48, 30), (i + 1).ToString("00"), power.Selection == id)) power.SelectPowerItem(id);
            }
            if (chrome.Tab(new Rect(center.x - 60, center.y - 18, 120, 36), "REAKTOR", power.Selection == "reactor")) power.SelectPowerItem("reactor");
            for (int i = 0; i < 2; i++)
            {
                string id = i == 0 ? "solar-a" : "solar-b";
                if (chrome.Tab(new Rect(i == 0 ? r.x + 12 : r.xMax - 124, r.yMax - 45, 112, 32), "SOLAR " + (i == 0 ? "A" : "B"), power.Selection == id)) power.SelectPowerItem(id);
            }
        }
        private IEnumerator Check()
        {
            string dir = Path.GetFullPath("Logs/ConsoleStudio"); Directory.CreateDirectory(dir);
            int captures = 0;
            for (int i = 0; i < 4; i++)
            {
                toolbar = i == 0; SetFixture(i); accessibility.TextScale = i == 3 ? 1.4f : 1;
                Screen.SetResolution(i == 0 ? 1600 : 1280, i == 0 ? 1000 : 720, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.6f);
                string[] ids = { "battery-bay-0", "battery-bay-1", "battery-bay-7", "reactor", "solar-a", "solar-b", "consumer:life","sun-primary","sun-companion" };
                foreach (var id in ids)
                {
                    if (!power.SelectPowerItem(id)) error = "Selection failed: " + id;
                    yield return new WaitForEndOfFrame(); yield return null;
                    if (power.LastRenderedFrame < Time.frameCount - 2) error = "Shared renderer did not draw";
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir, i + "-" + id.Replace(':', '-') + ".png")); captures++;
                    yield return new WaitForEndOfFrame(); yield return null;
                }
            }
            yield return new WaitForSecondsRealtime(.3f);
            bool passed = error == null && FindFirstObjectByType<StationOperations>() == null && FindFirstObjectByType<WaterScenario>() == null;
            File.WriteAllText(Path.Combine(dir, "result.json"), "{\"passed\":" + (passed ? "true" : "false") + ",\"captures\":" + captures + ",\"error\":" + (error == null ? "null" : JsonUtility.ToJson(new Error { message = error })) + "}");
            Application.Quit(passed ? 0 : 1);
        }
        [Serializable] private sealed class Error { public string message; }
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            power.Dispose(); chrome.Dispose(); UiLayout.EndPreview();
        }
    }
}

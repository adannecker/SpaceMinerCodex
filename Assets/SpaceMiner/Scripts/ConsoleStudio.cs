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
        private int checks;
        private ConsoleStationPreview models;
        private static readonly string[] Fixtures = { "Normalbetrieb", "Akku voll", "Strommangel / leer", "Lange Texte" };

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Application.logMessageReceived += OnLog;
            models = new ConsoleStationPreview();
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
            var a=new SolarFieldSampler().Capture(models.SolarA,stars,.65f,1);
            var b=new SolarFieldSampler().Capture(models.SolarB,stars,.81f,1);
            var generation = new StationGenerationSnapshot(
                StationGeneratorView.Reactor("reactor", "Reaktor", 2, .82f, index != 2),
                StationGeneratorView.SolarPhysical("solar-a","Solar A",.65f,index!=2,models.SolarA.up,stars,a),
                StationGeneratorView.SolarPhysical("solar-b","Solar B",.81f,index!=2,models.SolarB.up,stars,b),stars);
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
                OpenUtilities = () => utilities = true, DrawStation = DrawSchematic,
                PrepareModels = id => { if(Event.current.type==EventType.Repaint)models.Select(id); },
                DetailModel = () => models.Detail
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
            GUI.Label(new Rect(610, 84, w - 630, 28), "Aktuelles Startmodell · gemeinsame Spielgeometrie / Testzustände", chrome.Small);
        }
        private void DrawSchematic(Rect r)
        {
            UiChrome.Panel(r, SpaceMinerUi.Cyan, accessibility.HighContrast);
            GUI.Label(new Rect(r.x+12,r.y+6,r.width-24,28),"STARTSTATION / ANLAGE AUSWÄHLEN",chrome.Small);
            var map=new Rect(r.x+5,r.y+37,r.width-10,r.height-79);
            PowerConsoleView.PowerModel(map,models.Station);
            void Item(string id,string caption,int slot,Color color)
            {
                if(models.Station.Texture==null)return;
                Vector2 point=models.Station.Project(models.Position(id),map);
                float w=id.StartsWith("battery")?42:96;
                var label=new Rect(slot%2==0?map.x+5:map.xMax-w-5,map.y+4,w,27);
                if(slot==10)label.x=map.center.x-w/2;
                else if(slot>=2)
                {
                    int index=slot-2;
                    label.x=map.x+8+index%4*(map.width-16)/4;
                    label.y=map.yMax-60+index/4*30;
                }
                PowerConsoleView.PowerLine(point,label.center,color,1);
                if(chrome.Tab(label,caption,power.Selection==id))power.SelectPowerItem(id);
            }
            Item("solar-b","SOLAR B",0,SpaceMinerUi.Cyan);Item("solar-a","SOLAR A",1,SpaceMinerUi.Cyan);
            for(int i=0;i<8;i++)Item("battery-bay-"+i,(i+1).ToString("00"),i+2,
                i==0?(fixture==2?SpaceMinerUi.Amber:SpaceMinerUi.Success):i==1?SpaceMinerUi.Amber:SpaceMinerUi.Disabled);
            Item("reactor","REAKTOR",10,SpaceMinerUi.Cyan);
            if(chrome.Tab(new Rect(r.x+12,r.yMax-35,100,28),"↶ Drehen",false))models.Station.Orbit(-20);
            if(chrome.Tab(new Rect(r.x+118,r.yMax-35,100,28),"Drehen ↷",false))models.Station.Orbit(20);
            if(chrome.Tab(new Rect(r.x+224,r.yMax-35,48,28),"+",false))models.Station.Zoom(.85f);
            if(chrome.Tab(new Rect(r.x+278,r.yMax-35,48,28),"−",false))models.Station.Zoom(1.18f);
        }
        private IEnumerator Check()
        {
            string dir = Path.GetFullPath("Logs/ConsoleStudio"); Directory.CreateDirectory(dir);
            int captures = 0;
            Verify(models.Station.SharedMeshCount>100,"Station must use authored game meshes");
            Verify(data.Generation.SolarA.SolarDetails.GeometricAreaM2>100,"Solar A must use its real PV cells");
            Verify(data.Generation.SolarB.SolarDetails.GeometricAreaM2>100,"Solar B must use its real PV cells");
            Verify(Vector3.Distance(models.SolarA.localPosition, RingWalkthrough.At(60,90,1.8f))<.01f,"Solar A location");
            Verify(Vector3.Distance(models.SolarB.localPosition, RingWalkthrough.At(60,270,1.8f))<.01f,"Solar B location");
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
                    Verify(power.Selection==id,"Selection retained: "+id);
                    if(id=="reactor"||id.StartsWith("solar")||id.StartsWith("battery"))
                        Verify(models.Detail!=null&&models.Detail.SharedMeshCount>0,"Real detail meshes: "+id);
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir, i + "-" + id.Replace(':', '-') + ".png")); captures++;
                    yield return new WaitForEndOfFrame(); yield return null;
                }
                foreach(string solar in new[]{"solar-a","solar-b"})
                {
                    power.SelectPowerItem(solar);
                    for(int page=1;page<3;page++)
                    {
                        power.SetSolarPage(page);
                        yield return new WaitForEndOfFrame();yield return null;
                        Verify(power.Selection==solar,"Solar tab retains its panel");
                        ScreenCapture.CaptureScreenshot(Path.Combine(dir,i+"-"+solar+"-page-"+page+".png"));captures++;
                        yield return new WaitForEndOfFrame();yield return null;
                    }
                }
                power.SelectPowerItem("sun-primary");
                for(int view=0;view<3;view++)
                {
                    power.SetCelestialPerspective(view);
                    yield return new WaitForEndOfFrame(); yield return null;
                    Verify(power.CelestialPerspective==view,"Celestial perspective "+view);
                    Verify(power.CelestialBodyCount==(view==2?11:2),"Both stars and existing planets");
                    Verify(power.CelestialTargetsDistinct,"Stars separately selectable at every scale");
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir,i+"-perspective-"+view+".png"));captures++;
                    yield return new WaitForEndOfFrame();yield return null;
                }
            }
            yield return new WaitForSecondsRealtime(.3f);
            bool passed = error == null && FindFirstObjectByType<StationOperations>() == null && FindFirstObjectByType<WaterScenario>() == null;
            File.WriteAllText(Path.Combine(dir, "result.json"), "{\"passed\":" + (passed ? "true" : "false") + ",\"checks\":"+checks+",\"stationMeshes\":"+models.Station.SharedMeshCount+",\"captures\":" + captures + ",\"error\":" + (error == null ? "null" : JsonUtility.ToJson(new Error { message = error })) + "}");
            Application.Quit(passed ? 0 : 1);
        }
        [Serializable] private sealed class Error { public string message; }
        private void Verify(bool condition,string message){checks++;if(!condition&&error==null)error=message;}
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            models?.Dispose();
            power.Dispose(); chrome.Dispose(); UiLayout.EndPreview();
        }
    }
}

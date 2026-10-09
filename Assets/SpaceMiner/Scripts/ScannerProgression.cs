using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Knowledge belongs to the simulation; debug visibility never grants knowledge.
    [DefaultExecutionOrder(100)]
    public sealed class ScannerProgression : MonoBehaviour
    {
        public float CapacityKwh = .1f;
        public float ChargePowerKw = 1.08f;
        public const float InitialRangeMeters = 10000f;
        public float EnergyKwh { get; private set; }
        public float Charge => EnergyKwh / CapacityKwh;
        public float RangeMeters { get; private set; }
        public bool FirstScanComplete { get; private set; }
        public bool AtConsole => StationInteriorMode.HasConsoleOpen;
        private MiraAvatar portrait;
        private readonly MiraVisorOverlay dialogueOverlay=new MiraVisorOverlay();
        public bool DebugVisibility { get; private set; }
        public bool Ready => EnergyKwh >= CapacityKwh - .0000001f;
        public int KnownCount { get; private set; }
        public string Dialogue { get; private set; }
        public string DialogueTitle { get; private set; }
        public bool VirtualView => !StationInteriorMode.IsInside && !StartMenu.IsOpen && !IntroSequence.IsPlaying;
        public bool ShowsUnknown => !VirtualView || DebugVisibility;
        private WaterScenario scenario;
        private OrbitCamera orbit;
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
        private Material contactMaterial;
        private bool deliveryReported, completionReported;
        private float appliedMode = -1;

        public void Initialize(WaterScenario owner)
        {
            scenario = owner;
            orbit = Camera.main.GetComponent<OrbitCamera>();
            var sorted = new List<AsteroidResource>(scenario.Asteroids);
            sorted.Sort((a, b) => Distance(a).CompareTo(Distance(b)));
            RangeMeters = InitialRangeMeters;
            ArrangeIntroCloud(sorted);
            contactMaterial = new Material(Shader.Find("Standard")) { name = "VR contact - unmapped surface", color = new Color(.21f, .47f, .55f) };
            contactMaterial.SetFloat("_Glossiness", 0);
            foreach (var asteroid in scenario.Asteroids)
                foreach (var renderer in asteroid.GetComponentsInChildren<Renderer>(true)) originals[renderer] = renderer.sharedMaterials;
            ResetProgress();
        }

        private float Distance(AsteroidResource asteroid) => Vector3.Distance(transform.position, asteroid.transform.position);

        private void ArrangeIntroCloud(List<AsteroidResource> sorted)
        {
            // Intro layout only; preserve the explicit spiral/million stress-test geometry.
            if(sorted.Count!=100 || Camera.main.GetComponent<SpiralBelt>()?.IsReady==true)return;
            var random=new System.Random(7301);
            for(int i=10;i<sorted.Count;i++)
            {
                var body=sorted[i];
                if(Distance(body)>RangeMeters)continue;
                Vector3 original=(body.transform.position-transform.position).normalized;
                bool placed=false;
                for(int attempt=0;attempt<2000;attempt++)
                {
                    Vector3 direction=attempt==0?original:new Vector3(
                        Mathf.Sign(original.x)*(.15f+(float)random.NextDouble()),
                        Mathf.Sign(original.y)*(.15f+(float)random.NextDouble()),
                        Mathf.Sign(original.z)*(.15f+(float)random.NextDouble())).normalized;
                    float radius=RangeMeters+body.Info.DiameterMeters*.5f+400+(float)random.NextDouble()*3000;
                    Vector3 position=transform.position+direction*radius;
                    if(radius>18000)continue;
                    bool clear=true;
                    foreach(var other in sorted)
                        if(other!=body && Vector3.Distance(position,other.transform.position)<(body.Info.DiameterMeters+other.Info.DiameterMeters)*.5f+400){clear=false;break;}
                    if(!clear)continue;
                    body.transform.position=position;placed=true;break;
                }
                if(!placed)throw new InvalidOperationException("Cannot place introductory cloud outside scanner range: "+body.name);
            }
            Physics.SyncTransforms();
        }

        public void ResetProgress()
        {
            EnergyKwh = CapacityKwh * .97f;
            FirstScanComplete = deliveryReported = completionReported = false;
            StationInteriorMode.Current?.CloseConsole();
            if(StationInteriorMode.Current?.Layout!=null) { StationInteriorMode.Current.Exit(true); StationInteriorMode.Current.Enter(true); }
            DebugVisibility = false;
            KnownCount = 0;
            foreach (var asteroid in scenario.Asteroids) { asteroid.IsScanned = false; asteroid.WaterIdentified = false; }
            Say("ERSTER AUFTRAG · UMGEBUNG SCANNEN", "Geh an die Konsole und führe einen Nahbereichsscan aus. Der Scanner ist zu 97 Prozent geladen. In zehn Sekunden ist er bereit. Die Aussenansicht ist ein virtueller Raum: Dort siehst du nur, was wir bereits gescannt haben.");
            RefreshVisibility();
        }

        public void Advance(float seconds)
        {
            EnergyKwh = Mathf.Min(CapacityKwh, EnergyKwh + Mathf.Max(0, seconds) * ChargePowerKw / 3600f);
            if (!deliveryReported && scenario.Deliveries > 0)
            {
                deliveryReported = true;
                Say("ERSTE LIEFERUNG", "Die erste Wasserlieferung ist angekommen. Gut gemacht. Drohne 01 kann ihre Flüge fortsetzen, bis unser Tank gefüllt ist.");
            }
            if (!completionReported && scenario.QuestComplete)
            {
                completionReported = true;
                Say("WASSERVERSORGUNG GESICHERT", "Unser Wassertank ist voll. Damit haben wir den nächsten Schritt geschafft. Für weitere Aufgaben brauchen wir neue Technik und verlässliche Messdaten.");
            }
        }

        public bool Scan()
        {
            if (!AtConsole || StationInteriorMode.Current?.NearConsole != true || !Ready || FirstScanComplete) return false;
            EnergyKwh = 0;
            int water = 0;
            foreach (var asteroid in scenario.Asteroids)
            {
                if (Distance(asteroid) > RangeMeters) continue;
                asteroid.IsScanned = true;
                // Introductory water sensor knows only the three authored starter deposits.
                asteroid.WaterIdentified = asteroid.IsStarterWaterSource && asteroid.WaterFraction > 0;
                asteroid.Info.Description = "Ortung und grobe Form · Oberfläche nicht kartiert";
                KnownCount++;
                if (asteroid.WaterIdentified) water++;
            }
            FirstScanComplete = true;
            StationInteractionAudio.Play(StationSound.ScanComplete);
            scenario.Notify("Scan abgeschlossen: " + KnownCount + " Kontakte, " + water + " Wasserquellen. Neuer Auftrag: Wasser sichern.");
            Say("SCAN ABGESCHLOSSEN · WASSER SICHERN", "Der Scan hat " + KnownCount + " Asteroiden erfasst. Bei " + water + " davon ist Wasser bestätigt. Ihre Oberflächen sind noch nicht kartiert. Öffne die virtuelle Aussenansicht, wähle eine Wasserquelle und beauftrage Drohne 01, unseren Stationstank zu füllen.");
            RefreshVisibility();
            return true;
        }

        public void OpenVirtualView() { StationInteriorMode.Current?.Exit(); RefreshVisibility(); }
        public void OpenConsole() { StationInteriorMode.Current?.OpenConsole(); orbit.Select(null); RefreshVisibility(); }
        public void ReturnToStation() { StationInteriorMode.Current?.Enter(); RefreshVisibility(); }
        public void SetDebugVisibility(bool value) { DebugVisibility = value; orbit.Select(null); RefreshVisibility(); }
        public void Say(string title, string text) { DialogueTitle = title; Dialogue = text; }
        public void DismissDialogue() { Dialogue = null; }
        public void Restore(float energy, bool scanned, bool console, bool firstDelivery, bool complete)
        {
            EnergyKwh = energy; FirstScanComplete = scanned;
            deliveryReported = firstDelivery; completionReported = complete; DebugVisibility = false;
            KnownCount = 0; foreach (var asteroid in scenario.Asteroids) if (asteroid.IsScanned) KnownCount++;
            Say("SPIELSTAND GELADEN", scanned ? "Unsere Scandaten und der Wasserauftrag sind wiederhergestellt. Wir können dort weitermachen, wo wir aufgehört haben." : "Wir sind wieder an der Station. Lade den Scanner und führe unseren ersten Scan aus.");
            RefreshVisibility();
        }
        public bool CanInspect(SpaceObject item) => item == null || item.GetComponent<AsteroidResource>() == null
            || item.GetComponent<AsteroidResource>().IsScanned || (VirtualView && DebugVisibility);

        public void RefreshVisibility()
        {
            if (scenario == null) return;
            foreach (var asteroid in scenario.Asteroids)
            {
                bool visible = ShowsUnknown || asteroid.IsScanned;
                foreach (var collider in asteroid.GetComponentsInChildren<Collider>(true)) collider.enabled = visible;
                foreach (var renderer in asteroid.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.forceRenderingOff = !visible;
                    if (!originals.TryGetValue(renderer, out var materials)) continue;
                    bool rough = VirtualView && !DebugVisibility && asteroid.IsScanned;
                    if (rough) { var contacts = new Material[materials.Length]; for (int i = 0; i < contacts.Length; i++) contacts[i] = contactMaterial; renderer.sharedMaterials = contacts; }
                    else renderer.sharedMaterials = materials;
                }
            }
            if (!CanInspect(orbit.Selected)) orbit.Select(null);
            appliedMode = (ShowsUnknown ? 1 : 0) + (VirtualView ? 2 : 0) + (DebugVisibility ? 4 : 0);
        }

        private void LateUpdate()
        {
            if (scenario == null) return;
            float mode = (ShowsUnknown ? 1 : 0) + (VirtualView ? 2 : 0) + (DebugVisibility ? 4 : 0);
            if (mode != appliedMode) RefreshVisibility();

        }

        private float Scale => UiLayout.Scale();
        private Rect DialogueRect
        {
            get {
                return MiraVisorOverlay.Bounds(Screen.width/Scale,Screen.height/Scale);
            }
        }
        public bool OwnsScreenPoint(Vector3 point)
        {
            Vector2 p = new Vector2(point.x, Screen.height - point.y) / Scale;
            return AtConsole || new Rect(346, 158, 640, 38).Contains(p) || (Dialogue != null && DialogueRect.Contains(p));
        }

        private void OnGUI()
        {
            if (scenario == null || StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.BlocksInput) return;
            ui.Configure(SettingsStore.Current.Accessibility);
            var previous = GUI.matrix; int depth = GUI.depth;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Scale); GUI.depth = -40;
            float width = Screen.width / Scale, height = Screen.height / Scale;
            bool initialEnabled = GUI.enabled;
            if (VirtualView)
            {
                if (GUI.Button(new Rect(346,158,245,38), "Zur Station",ui.Button)) ReturnToStation();
                bool debug=GUI.Toggle(new Rect(605,158,380,38),DebugVisibility,"Debug · alle Objekte sichtbar",ui.ToggleStyle);
                if(debug!=DebugVisibility)SetDebugVisibility(debug);
            }
            else if (!AtConsole)
            {
                string questTitle = FirstScanComplete ? "AUFTRAG · WASSER SICHERN" : "AUFTRAG · ERSTER SCAN";
                string questText = FirstScanComplete ? "Öffne am Pult die virtuelle Aussenansicht und beauftrage Drohne 01 mit einer Wasserquelle." : "Gehe zum Stationspult. Öffne es mit E und führe den Nahbereichsscan aus.";
                float titleHeight = ui.Small.CalcHeight(new GUIContent(questTitle), 274);
                float bodyHeight = ui.Text.CalcHeight(new GUIContent(questText), 274);
                float bodyTop = 36 + titleHeight + 16;
                float chargeTop = bodyTop + bodyHeight + 16;
                ui.Panel(new Rect(20,20,310,chargeTop + 34 - 20));
                GUI.Label(new Rect(38,36,274,titleHeight),questTitle,ui.Small);
                GUI.Label(new Rect(38,bodyTop,274,bodyHeight),questText,ui.Text);
                GUI.Label(new Rect(38,chargeTop,274,30),"Scanner " +(Charge*100).ToString("F1")+" %",ui.Small);
            }
            if (Dialogue != null)
            {
                GUI.enabled = initialEnabled;
                if(dialogueOverlay.Draw(DialogueRect,DialogueTitle,Dialogue,MiraAvatar.Mood.Focused,"STATIONSKANAL / MIRA","Verstanden"))DismissDialogue();
            }
            GUI.enabled = initialEnabled; GUI.depth = depth; GUI.matrix = previous;
        }

        public void DrawPortrait(Rect rect)
        {
            if(portrait==null) { portrait=gameObject.AddComponent<MiraAvatar>(); portrait.DialoguePortraits=Resources.Load<Texture2D>("Mira/MiraDialoguePortraits"); portrait.Motion=false; }
            portrait.Draw(rect);
        }
        private void OnDestroy() { dialogueOverlay.Dispose();ui.Dispose(); if (contactMaterial != null) Destroy(contactMaterial); }
    }
}

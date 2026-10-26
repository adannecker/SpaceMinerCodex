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
        public MiraDialogueModule DialogueModule { get; private set; }
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
            Say(MiraQuestDialogues.Quest(0).Cues[0]);
            // Initialization can run before the start menu has installed its modal state.
            // The normal visibility update resumes this clip when its panel can be shown.
            DialogueModule.SetPaused(true);
            RefreshVisibility();
        }

        public void Advance(float seconds)
        {
            if(scenario.Operations?.Live!=true||scenario.Operations.Powered(0)&&scenario.Operations.State.power.LifePowered)
                EnergyKwh = Mathf.Min(CapacityKwh, EnergyKwh + Mathf.Max(0, seconds) * ChargePowerKw / 3600f);
            if (!deliveryReported && scenario.Deliveries > 0)
            {
                deliveryReported = true;
                if (scenario.Operations?.Live != true) Say("ERSTE LIEFERUNG", scenario.UsesIcePlant
                    ? "Die erste Roh-Eislieferung wird übernommen. Der Brecher und der Verdichter bereiten sie für das Kaltlager vor. Sauberes Wasser entsteht erst im Aufbereiter. Prüfe die Anlage und ihre Stromversorgung am Lagerpult; ein weiterer Rohstoffflug ist ein eigener Auftrag."
                    : "Die erste Wasserlieferung ist angekommen. Gut gemacht. Drohne 01 kann ihre Flüge fortsetzen, bis unser Tank gefüllt ist.");
            }
            if (!completionReported && scenario.QuestComplete)
            {
                completionReported = true;
                if (scenario.Operations?.Live != true) Say("WASSERVERSORGUNG GESICHERT", "Unser Wassertank ist voll. Damit haben wir den nächsten Schritt geschafft. Für weitere Aufgaben brauchen wir neue Technik und verlässliche Messdaten.");
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
            scenario.Operations?.RefreshDebrisKnowledge();
            int wrecks = scenario.Operations?.KnownDebrisCount ?? 0;
            StationInteractionAudio.Play(StationSound.ScanComplete);
            scenario.Notify("Scan abgeschlossen: " + KnownCount + " Asteroiden, " + water + " Wasserquellen und " + wrecks + " unbekannte Wrackkontakte. Neuer Auftrag: Wasser sichern.");
            if (scenario.Operations?.Live == true) Say(MiraQuestDialogues.Quest(0).Cues[1]);
            else Say("SCAN ABGESCHLOSSEN · WASSER SICHERN", "Der Scan hat " + KnownCount + " Asteroiden erfasst. Bei " + water + " davon ist Wasser bestätigt. Ihre Oberflächen sind noch nicht kartiert. Zusätzlich haben wir " + wrecks + " unbekannte Teile im Nahbereich geortet. Deren Inhalt und Zusammensetzung kennen wir erst nach Bergung beziehungsweise Analyse. Öffne die virtuelle Aussenansicht, wähle eine Wasserquelle und beauftrage Drohne 01 mit einer Eislieferung. Im Lager wird das Rohmaterial angenommen; der Aufbereiter versorgt danach unseren Wassertank.");
            RefreshVisibility();
            return true;
        }

        public void OpenVirtualView() { StationInteriorMode.Current?.Exit(); RefreshVisibility(); }
        public void OpenConsole() { StationInteriorMode.Current?.OpenConsole(); orbit.Select(null); RefreshVisibility(); }
        public void ReturnToStation() { StationInteriorMode.Current?.Enter(); RefreshVisibility(); }
        public void SetDebugVisibility(bool value) { DebugVisibility = value; orbit.Select(null); RefreshVisibility(); }
        public void Say(string title, string text)
        {
            Say(new MiraDialogueLine { Heading = title, Text = text });
        }
        public void Say(MiraDialogueLine line)
        {
            if (DialogueModule == null) DialogueModule = MiraDialogueModule.Create(gameObject);
            DialogueModule.Play(line);
            DialogueTitle = line.Heading;
            Dialogue = line.Text ?? "";
            if (scenario.Operations?.Live == true) scenario.Operations.RememberMiraLine(line.Id);
            DialogueModule.SetPaused(!DialogueCanRender);
        }
        public void DismissDialogue() { Dialogue = null; DialogueModule?.Close(); }
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
            scenario.Operations?.RefreshDebrisKnowledge();
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
            if (DialogueModule != null) DialogueModule.SetPaused(!DialogueCanRender);
            float mode = (ShowsUnknown ? 1 : 0) + (VirtualView ? 2 : 0) + (DebugVisibility ? 4 : 0);
            if (mode != appliedMode) RefreshVisibility();

        }

        private float Scale => UiLayout.Scale();
        public Rect VirtualToolbarRect
        {
            get {
                var ops=scenario?.Operations;
                if(ops==null||!ops.Live)return new Rect(346,158,640,38);
                float x=ops.TacticalOverviewBounds.xMax+14;
                return new Rect(x,ops.HudResourceBounds.yMax+12,Mathf.Max(180,Mathf.Min(640,UiLayout.Width-x-18)),Mathf.Max(38,44*SettingsStore.Current.Accessibility.TextScale));
            }
        }
        private Rect DialogueRect
        {
            get {
                return MiraVisorOverlay.Bounds(Screen.width/Scale,Screen.height/Scale);
            }
        }
        // A station panel yields to Mira. It must not suppress the only UI that
        // can acknowledge her message, even during the panel's closing frame.
        public bool DialogueCanRender => scenario != null && Dialogue != null
            && !StartMenu.IsOpen && !IntroSequence.BlocksGameplay && !MemoryCinematic.IsPlaying
            && !SettingsMenu.IsOpen && !SaveGameMenu.BlocksInput && !QuitMenu.BlocksInput
            && !CommandRoomPlanner.BlocksInput && !TechTreeMenu.BlocksInput && !RingConstructionSimulator.BlocksInput;
        public int LastDialogueRenderedFrame { get; private set; } = -1;
        public bool OwnsScreenPoint(Vector3 point)
        {
            Vector2 p = new Vector2(point.x, Screen.height - point.y) / Scale;
            return AtConsole || (VirtualView && Dialogue == null && !MemoryCinematic.IsPlaying && VirtualToolbarRect.Contains(p))
                || (DialogueCanRender && DialogueRect.Contains(p));
        }

        private void OnGUI()
        {
            if (scenario == null || StartMenu.IsOpen || IntroSequence.BlocksGameplay || MemoryCinematic.IsPlaying
                || (SettingsMenu.BlocksInput && !DialogueCanRender)) return;
            ui.Configure(SettingsStore.Current.Accessibility);
            var previous = GUI.matrix; int depth = GUI.depth;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Scale); GUI.depth = -40;
            float width = Screen.width / Scale, height = Screen.height / Scale;
            bool initialEnabled = GUI.enabled;
            if (VirtualView && Dialogue == null)
            {
                Rect toolbar=VirtualToolbarRect;float returnWidth=Mathf.Min(245,(toolbar.width-14)*.4f);
                if (GUI.Button(new Rect(toolbar.x,toolbar.y,returnWidth,toolbar.height), "Zur Station [V]",ui.Button)) ReturnToStation();
                bool debug=GUI.Toggle(new Rect(toolbar.x+returnWidth+14,toolbar.y,toolbar.width-returnWidth-14,toolbar.height),DebugVisibility,"Debug · alle Objekte sichtbar",ui.ToggleStyle);
                if(debug!=DebugVisibility)SetDebugVisibility(debug);
            }
            else if (Dialogue == null && !AtConsole && scenario.Operations?.Live != true)
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
            if (DialogueCanRender)
            {
                GUI.enabled = initialEnabled;
                if (Event.current.type == EventType.Repaint) LastDialogueRenderedFrame = Time.frameCount;
                // Quest instructions remain readable even when cinematic subtitles are disabled.
                DialogueModule.Subtitles = true;
                if(DialogueModule.Draw(DialogueRect,"STATIONSKANAL / MIRA","Verstanden"))DismissDialogue();
            }
            GUI.enabled = initialEnabled; GUI.depth = depth; GUI.matrix = previous;
        }

        public void DrawPortrait(Rect rect)
        {
            if(portrait==null) { portrait=gameObject.AddComponent<MiraAvatar>(); portrait.DialoguePortraits=Resources.Load<Texture2D>("Mira/MiraDialoguePortraits"); portrait.Motion=false; }
            portrait.Draw(rect);
        }
        private void OnDisable() { DismissDialogue(); }
        private void OnDestroy() { ui.Dispose(); if (contactMaterial != null) Destroy(contactMaterial); }
    }
}

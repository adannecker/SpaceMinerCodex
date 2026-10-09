using System;
using System.Collections;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StartMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private readonly SpaceMinerUi ui = new SpaceMinerUi();
        private bool quitPrompt;
        private bool galleryOpen;
        private int galleryReturnedFrame = -1;
        private IntroSequence replayIntro;
        private Vector2 cinematicScroll, cutsceneScroll;
        private Camera menuCamera;
        private Transform scenery;
        private readonly Transform[] rocks = new Transform[7];
        private float sceneTime;
        private float originalFov;

        private void LateUpdate()
        {
            if (!IsOpen) return;
            if (menuCamera == null)
            {
                menuCamera = Camera.main;
                if (menuCamera == null) return;
                originalFov = menuCamera.fieldOfView;
                scenery = new GameObject("Start menu scenery (visual only)").transform;
                var root = GameObject.Find("Asteroids (1 unit = 1 metre)");
                if (root != null)
                {
                    var sources = root.GetComponentsInChildren<MeshFilter>();
                    for (int i = 0; i < rocks.Length && i < sources.Length; i++)
                    {
                        var source = sources[(i * 13) % sources.Length];
                        var renderer = source.GetComponent<MeshRenderer>();
                        if (source.sharedMesh == null || renderer == null) continue;
                        var rock = new GameObject("Backdrop asteroid " + i, typeof(MeshFilter), typeof(MeshRenderer));
                        rock.transform.SetParent(scenery);
                        rock.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                        rock.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                        float size = 9 + i * 2.5f;
                        rock.transform.localScale = Vector3.one * (size / Mathf.Max(.001f, source.sharedMesh.bounds.size.magnitude));
                        rocks[i] = rock.transform;
                    }
                }
            }
            sceneTime += Time.unscaledDeltaTime;
            float angle = (-28 + Mathf.Sin(sceneTime * .035f) * 12) * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Sin(angle) * 138, 65 + Mathf.Sin(sceneTime * .09f) * 3, -Mathf.Cos(angle) * 138);
            // Aim left of the station so its silhouette occupies the open right side.
            Vector3 target = new Vector3(-35, 0, 0);
            menuCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            OrbitCamera.ConfigureDepthRange(menuCamera, Vector3.Distance(position, target));
            menuCamera.fieldOfView = 48;
            for (int i = 0; i < rocks.Length; i++)
            {
                if (rocks[i] == null) continue;
                float phase = i * 2.4f + sceneTime * (.006f + i * .001f);
                rocks[i].position = new Vector3(Mathf.Cos(phase) * (68 + i * 8), -18 + i * 8, Mathf.Sin(phase) * (65 + i * 8));
                rocks[i].rotation = Quaternion.Euler(sceneTime * (1 + i * .2f), i * 47 + sceneTime * .7f, i * 21);
            }
        }

        private void ClearScenery()
        {
            if (scenery != null) { scenery.gameObject.SetActive(false); Destroy(scenery.gameObject); }
            if (menuCamera != null) { menuCamera.fieldOfView = originalFov; menuCamera.GetComponent<OrbitCamera>()?.ResetView(); }
            scenery = null;
            menuCamera = null;
        }

        private void Update()
        {
            if (!MemoryCinematic.IsPlaying && IsOpen && galleryOpen && galleryReturnedFrame != Time.frameCount && !SettingsMenu.IsOpen && Input.GetKeyDown(KeyCode.Escape)) galleryOpen = false;
        }

        public bool Replay(string id)
        {
            if (!IsOpen || SettingsMenu.IsOpen || MemoryCinematic.IsPlaying || !CinematicLibrary.IsAvailable(id)) return false;
            if (id == CinematicLibrary.FirstMemories)
            {
                var memory = GetComponent<MemoryCinematic>() ?? gameObject.AddComponent<MemoryCinematic>();
                memory.Finished -= ReturnFromMemory; memory.Finished += ReturnFromMemory;
                return memory.Play();
            }
            if (id != CinematicLibrary.MiraAwakening) return false;
            replayIntro = FindFirstObjectByType<IntroSequence>();
            if (replayIntro == null) return false;
            replayIntro.Finished += ReturnFromReplay;
            IsOpen = false;
            ClearScenery();
            replayIntro.PlayIntro(true);
            SettingsUiAudio.Activate();
            return true;
        }

        private void ReturnFromMemory() { galleryOpen = true; galleryReturnedFrame = Time.frameCount; }

        private void ReturnFromReplay()
        {
            replayIntro.Finished -= ReturnFromReplay;
            replayIntro = null;
            IsOpen = true;
            galleryOpen = true;
            galleryReturnedFrame = Time.frameCount;
        }

        private void DrawGallery(float width, float height)
        {
            Rect panel = new Rect(28, 28, width - 56, height - 56);
            ui.Panel(panel);
            GUI.Label(new Rect(56, 48, width - 112, 46), "CINEMATICS", ui.Heading);
            GUI.Label(new Rect(56, 100, width - 112, 62), "Erinnerungen und deine bereits erlebten Storysequenzen. Wähle eine Sequenz.", ui.Text);
            float columnWidth = (width - 136) / 2;
            DrawSequenceColumn(new Rect(56, 176, columnWidth, height - 290), "Cinematics", StorySequenceKind.Cinematic, ref cinematicScroll);
            DrawSequenceColumn(new Rect(80 + columnWidth, 176, columnWidth, height - 290), "Cutscenen", StorySequenceKind.Cutscene, ref cutsceneScroll);
            if (GUI.Button(new Rect(56, height - 84, 270, 44), "Zurück zum Startmenü", ui.Button)) { galleryOpen = false; SettingsUiAudio.Activate(); }
        }

        private void DrawSequenceColumn(Rect rect, string title, StorySequenceKind kind, ref Vector2 scroll)
        {
            ui.Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 18, rect.y + 14, rect.width - 36, rect.height - 28));
            GUILayout.Label(title, ui.Heading);
            GUILayout.Space(16);
            scroll = GUILayout.BeginScrollView(scroll);
            bool found = false;
            foreach (var entry in CinematicLibrary.Entries)
            {
                if (entry.Kind != kind || !CinematicLibrary.IsAvailable(entry.Id)) continue;
                found = true;
                GUILayout.Label(entry.Title, ui.Text);
                GUILayout.Label(entry.Description, ui.Small);
                GUILayout.Space(10);
                if (GUILayout.Button("Wiederansehen", ui.Button, GUILayout.Height(48))) Replay(entry.Id);
                GUILayout.Space(24);
            }
            if (!found) GUILayout.Label("Noch keine Sequenzen gesehen.\nSie erscheinen hier, sobald sie in deiner Story vorkommen.", ui.Text);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetState()
        {
            string[] args = Environment.GetCommandLineArgs();
            IsOpen = Array.IndexOf(args, "-spaceMinerSmokeTest") < 0
                && Array.IndexOf(args, "-scanSaveCheck") < 0
                && Array.IndexOf(args, "-settingsPreview") < 0
                && Array.IndexOf(args, "-configurationAudioCheck") < 0
                && Array.IndexOf(args, "-quitMenuCheck") < 0
                && Array.IndexOf(args, "-techTreeCheck") < 0;
        }

        public void StartDemo()
        {
            if (!IsOpen || SettingsMenu.IsOpen || SaveGameMenu.IsOpen) return;
            var saves = FindFirstObjectByType<SaveGameMenu>();
            if (saves != null && !saves.NewSession()) return;
            FindFirstObjectByType<WaterScenario>()?.ResetScenario();
            IsOpen = false;
            ClearScenery();
            FindFirstObjectByType<IntroSequence>()?.PlayIntro();
            SettingsUiAudio.Activate();
        }
        public void ResumeSavedGame()
        {
            IsOpen = false; galleryOpen = false; quitPrompt = false; ClearScenery();
            FindFirstObjectByType<IntroSequence>()?.Skip();
        }
        public void ReturnToStart()
        {
            FindFirstObjectByType<SaveGameMenu>()?.EndSession();
            StationInteriorMode.Current?.Exit(true);
            galleryOpen=false;quitPrompt=false;sceneTime=0;
            ClearScenery();Camera.main.GetComponent<OrbitCamera>()?.ResetView();IsOpen=true;
        }

        private void OnGUI()
        {
            if (!IsOpen || SettingsMenu.IsOpen || MemoryCinematic.IsPlaying || SaveGameMenu.IsOpen) return;
            Matrix4x4 matrix = GUI.matrix;
            int depth = GUI.depth;
            try
            {
                ui.Configure(SettingsStore.Current.Accessibility);
                GUI.depth = -190;
                float scale = UiLayout.Scale(960, 720);
                GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
                float width = Screen.width / scale, height = Screen.height / scale;
                if (galleryOpen) { DrawGallery(width, height); return; }
                SpaceMinerUi.Fill(new Rect(0, 0, width, height), new Color(.005f, .012f, .03f, .12f));
                // Deterministic star field, no per-frame random state or scene assets.
                for (int i = 0; i < 100; i++)
                {
                    float x = ((i * 137 + 43) % 997) / 997f * width;
                    float y = ((i * 251 + 71) % 991) / 991f * height;
                    SpaceMinerUi.Fill(new Rect(x, y, i % 7 == 0 ? 2 : 1, 1), new Color(.35f, .65f, .8f, .5f));
                }
                Rect panel = new Rect(28, (height - 510) / 2, 400, 510);
                ui.Panel(panel);
                var title = new GUIStyle(ui.Heading) { fontSize = 36, alignment = TextAnchor.MiddleLeft };
                var subtitle = new GUIStyle(ui.Text) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(panel.x + 28, panel.y + 38, 344, 70), "SPACE MINER", title);
                GUI.Label(new Rect(panel.x + 28, panel.y + 116, 344, 80), "Eine Station. Hundert Asteroiden.\nDein Anfang zwischen den Sternen.", subtitle);
                if (quitPrompt)
                {
                    GUI.Label(new Rect(panel.x + 28, panel.y + 210, 344, 60), "Spiel wirklich beenden?", subtitle);
                    if (GUI.Button(new Rect(panel.x + 28, panel.y + 290, 344, 55), "Beenden", ui.Primary)) Quit();
                    if (GUI.Button(new Rect(panel.x + 28, panel.y + 360, 344, 55), "Zurück", ui.Button)) quitPrompt = false;
                }
                else
                {
                    if (GUI.Button(new Rect(panel.x + 28, panel.y + 212, 344, 48), "Demo starten", ui.Primary)) StartDemo();
                    if (GUI.Button(new Rect(panel.x + 28, panel.y + 266, 344, 42), "Spielstand laden", ui.Button)) FindFirstObjectByType<SaveGameMenu>()?.OpenLoad();
                    if (GUI.Button(new Rect(panel.x + 28, panel.y + 314, 344, 42), "Cinematics", ui.Button)) { galleryOpen = true; SettingsUiAudio.Activate(); }
                    if (GUI.Button(new Rect(panel.x + 28, panel.y + 362, 344, 42), "Konfiguration", ui.Button)) { GetComponent<SettingsMenu>().Open(); SettingsUiAudio.Activate(); }
                    if (GUI.Button(new Rect(panel.x + 28, panel.y + 410, 344, 42), "Beenden", ui.Button)) { quitPrompt = true; SettingsUiAudio.Activate(); }
                }
                GUI.Label(new Rect(panel.x + 28, panel.y + 455, 344, 30), "DEMO  //  " + Application.version, new GUIStyle(ui.Small) { alignment = TextAnchor.MiddleLeft });
            }
            finally { GUI.matrix = matrix; GUI.depth = depth; }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy() { if (replayIntro != null) replayIntro.Finished -= ReturnFromReplay; IsOpen = false; ClearScenery(); ui.Dispose(); }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cinematicGalleryCheck") >= 0)
            {
                yield return GalleryCheck();
                yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-startMenuCheck") < 0) yield break;
            yield return new WaitForSecondsRealtime(1);
            try
            {
                if (!IsOpen || IntroSequence.IsPlaying || !SettingsMenu.PausesSimulation) throw new Exception("Start screen must hold gameplay and intro");
                GetComponent<SettingsMenu>().Open();
                if (!IsOpen || !SettingsMenu.IsOpen) throw new Exception("Configuration must retain start screen");
                GetComponent<SettingsMenu>().Cancel();
            }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); yield break; }
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../start-menu.png"));
            yield return new WaitForSecondsRealtime(.5f);
            StartDemo();
            if (IsOpen || !IntroSequence.IsPlaying) { Debug.LogError("Demo must start intro"); Application.Quit(1); yield break; }
            Debug.Log("START MENU CHECK PASSED");
            Application.Quit(0);
        }

        private bool GalleryGuard(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); return false; }
        }

        private IEnumerator GalleryCheck()
        {
            string previousPrefix = CinematicLibrary.PreferencePrefix;
            CinematicLibrary.PreferencePrefix = "SpaceMiner.Test.Gallery." + Guid.NewGuid() + ".";
            try
            {
                yield return new WaitForSecondsRealtime(1);
                galleryOpen = true;
                if (!GalleryGuard(() =>
                {
                    if (CinematicLibrary.IsSeen(CinematicLibrary.MiraAwakening) || Replay(CinematicLibrary.MiraAwakening)) throw new Exception("Unseen sequences must remain locked");
                    if (!SettingsMenu.PausesSimulation || IntroSequence.IsPlaying) throw new Exception("Gallery must hold gameplay");
                })) yield break;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../cinematic-gallery-empty.png"));
                yield return new WaitForSecondsRealtime(.4f);
                var intro = FindFirstObjectByType<IntroSequence>();
                if (!GalleryGuard(() =>
                {
                    intro.PlayIntro();
                    if (!CinematicLibrary.IsSeen(CinematicLibrary.MiraAwakening)) throw new Exception("Story appearance must persist unlock");
                    intro.Skip();
                })) yield break;
                yield return null;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, "../cinematic-gallery-unlocked.png"));
                yield return new WaitForSecondsRealtime(.4f);
                if (!GalleryGuard(() =>
                {
                    if (!Replay(CinematicLibrary.MiraAwakening) || IsOpen || !IntroSequence.IsPlaying) throw new Exception("Unlocked replay must start");
                    intro.Skip();
                    if (!IsOpen || !galleryOpen || IntroSequence.IsPlaying) throw new Exception("Skipped replay must return to gallery");
                })) yield break;
                yield return null;
                if (!GalleryGuard(() =>
                {
                    if (!Replay(CinematicLibrary.MiraAwakening)) throw new Exception("Repeated replay must work");
                    intro.AdvancePlayback(100000);
                    if (!IsOpen || !galleryOpen || IntroSequence.IsPlaying) throw new Exception("Completed replay must return to gallery");
                })) yield break;
                yield return null;
                if (!GalleryGuard(() =>
                {
                    if (scenery == null || menuCamera == null) throw new Exception("Backdrop must resume after replay");
                    galleryOpen = false;
                    StartDemo();
                    if (IsOpen || !IntroSequence.IsPlaying) throw new Exception("Normal demo must still start");
                })) yield break;
                Debug.Log("CINEMATIC GALLERY CHECK PASSED");
                Application.Quit(0);
            }
            finally
            {
                PlayerPrefs.DeleteKey(CinematicLibrary.PreferencePrefix + CinematicLibrary.MiraAwakening);
                PlayerPrefs.Save();
                CinematicLibrary.PreferencePrefix = previousPrefix;
            }
        }
#endif
    }
}

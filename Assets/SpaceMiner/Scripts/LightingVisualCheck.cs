using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    // Render the same stationary station while the observer surveys the system.
    // A camera move must not change the illumination of an unchanged object.
    public sealed class LightingVisualCheck : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-lightingVisualCheck") < 0) return;
            SceneManager.sceneLoaded += Loaded;
        }

        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Loaded;
            new GameObject("Lighting visual check").AddComponent<LightingVisualCheck>();
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var menu = FindFirstObjectByType<StartMenu>();
            var world = FindFirstObjectByType<RuinedWorld>();
            var orbit = FindFirstObjectByType<OrbitCamera>();
            var station = FindFirstObjectByType<StationVisual>();
            if (menu == null || world == null || orbit == null || station == null)
            { Debug.LogError("Lighting check requires the playable scene"); Application.Quit(1); yield break; }

            string folder = Path.GetFullPath("Logs/Lighting");
            Directory.CreateDirectory(folder);
            Capture(Camera.main, Path.Combine(folder, "start-menu-scene.png"));
            yield return new WaitForSecondsRealtime(.3f);
            menu.StartDemo();
            FindFirstObjectByType<IntroSequence>()?.Skip();
            yield return null;
            orbit.ResetView();
            yield return null;
            Capture(Camera.main, Path.Combine(folder, "gameplay-scene.png"));
            yield return new WaitForSecondsRealtime(.3f);

            // Isolate the station for a repeatable pixel comparison, keeping its real materials and shadows.
            const int probeLayer = 27;
            foreach (var renderer in station.GetComponentsInChildren<Renderer>()) renderer.gameObject.layer = probeLayer;
            var probe = new GameObject("Station lighting probe", typeof(Camera)).GetComponent<Camera>();
            probe.enabled = false;
            Vector3 position = new Vector3(-70, 45, -100);
            probe.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-position));
            probe.fieldOfView = 36;
            probe.nearClipPlane = .5f;
            probe.farClipPlane = 500;
            probe.cullingMask = 1 << probeLayer;
            probe.clearFlags = CameraClearFlags.SolidColor;
            probe.backgroundColor = Color.black;
            bool passed = true;
            foreach (int quality in new[] { 3, 5 })
            {
                QualitySettings.SetQualityLevel(quality);
                orbit.ResetView();
                yield return null;
                Color32[] reference = Capture(probe, Path.Combine(folder, "station-quality-" + quality + ".png"));
                int visiblePixels = 0;
                foreach (var pixel in reference) if (pixel.r + pixel.g + pixel.b > 30) visiblePixels++;
                if (visiblePixels < 1000) { Debug.LogError("Lighting probe has no visible station"); passed = false; }
                for (int sample = 0; sample < 8; sample++)
                {
                    if ((sample & 1) == 0) orbit.SolarOverview(RuinedWorld.SunPosition, world.OverviewRadius);
                    else { orbit.ResetView(); orbit.Orbit(new Vector2(sample * 35, sample * 2)); }
                    yield return null;
                    Color32[] current = Capture(probe, null);
                    long difference = 0;
                    for (int i = 0; i < current.Length; i++)
                        difference += Math.Abs(current[i].r - reference[i].r)
                            + Math.Abs(current[i].g - reference[i].g) + Math.Abs(current[i].b - reference[i].b);
                    double meanDifference = difference / (double)(current.Length * 3);
                    Debug.Log("LIGHTING SAMPLE quality=" + quality + " view=" + sample + " meanDifference=" + meanDifference.ToString("F6"));
                    if (meanDifference > .05) { Debug.LogError("Observer movement changed stationary station illumination"); passed = false; }
                }
            }
            // Render a red front plate 1 cm in front of a green back plate at station distance.
            // The back plate is deliberately drawn later; depth precision must still keep it hidden.
            var front = Plate("Front plate", Color.red, -.01f, 2000);
            var back = Plate("Back plate", Color.green, 0, 2001);
            probe.cullingMask = 1 << 26;
            probe.fieldOfView = 36;
            for (int sample = 0; sample < 24; sample++)
            {
                float distance = 140 + sample * .013f;
                probe.transform.SetPositionAndRotation(new Vector3(0, 0, -distance), Quaternion.identity);
                OrbitCamera.ConfigureDepth(probe, distance);
                Color32[] frame = Capture(probe, sample == 0 ? Path.Combine(folder, "thin-surfaces.png") : null);
                Color32 center = frame[300 * 800 + 400];
                if (center.r < 30 || center.g > center.r / 2)
                { Debug.LogError("Rear plate leaked through the front plate at distance " + distance); passed = false; }
            }
            Destroy(front); Destroy(back);
            Debug.Log(passed ? "LIGHTING VISUAL CHECK PASSED: stable station illumination with real shadow casters at two quality levels; 24 thin-surface depth samples"
                : "LIGHTING VISUAL CHECK FAILED");
            Application.Quit(passed ? 0 : 1);
        }

        static GameObject Plate(string name, Color color, float z, int queue)
        {
            var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plate.name = name;
            plate.layer = 26;
            plate.transform.position = new Vector3(0, 0, z);
            plate.transform.localScale = Vector3.one * 30;
            var material = new Material(Shader.Find("Standard")) { color = color, renderQueue = queue };
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
            var renderer = plate.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return plate;
        }

        static Color32[] Capture(Camera camera, string path)
        {
            var target = new RenderTexture(800, 600, 24);
            var pixels = new Texture2D(800, 600, TextureFormat.RGB24, false);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            float shadowDistance = QualitySettings.shadowDistance;
            try
            {
                // Hold the probe's shadow coverage constant while the gameplay camera changes zoom.
                QualitySettings.shadowDistance = 200;
                if (camera == Camera.main)
                {
                    var sky = FindFirstObjectByType<RuinedWorld>().SkyCamera;
                    var oldSkyTarget = sky.targetTexture;
                    try { sky.targetTexture = target; sky.Render(); }
                    finally { sky.targetTexture = oldSkyTarget; }
                }
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                pixels.Apply();
                if (path != null) File.WriteAllBytes(path, pixels.EncodeToPNG());
                return pixels.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
                QualitySettings.shadowDistance = shadowDistance;
                target.Release();
                Destroy(target);
                Destroy(pixels);
            }
        }
#endif
    }
}

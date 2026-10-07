using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    // Offline video frames sample the actual cinematic renderer, independent of wall-clock playback.
    public sealed class MemoryCinematicExport : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-cinematicExport") >= 0)
                SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Loaded;
            new GameObject("Cinematic video export").AddComponent<MemoryCinematicExport>();
        }
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            string output = Path.Combine(root, "outputs", "Cinematic");
            Directory.CreateDirectory(output);
            var args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-cinematicExport");
            if (flag + 1 >= args.Length) { Application.Quit(1); yield break; }
            var memory = gameObject.AddComponent<MemoryCinematic>();
            if (!memory.Play()) { Application.Quit(1); yield break; }
            memory.enabled = false;
            const int width = 1280, height = 720, fps = 30;
            var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo(args[flag + 1],
                "-y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p \"" + Path.Combine(output, "IntroCinematic-silent.mp4") + "\"")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true };
            process.Start();
            var target = new RenderTexture(width, height, 24);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var camera = memory.ExportCamera;
            camera.targetTexture = target;
            camera.aspect = width / (float)height;
            string timing = "scene,frames,voiceSeconds\n";
            for (int scene = 0; scene < MemoryCinematic.SceneCount; scene++)
            {
                int startFrame=Mathf.RoundToInt(memory.SceneStart(scene)*fps);
                int frames = Mathf.RoundToInt((memory.SceneStart(scene)+memory.SceneDuration(scene))*fps)-startFrame;
                timing += (scene + 1) + "," + frames + "," + memory.VoiceDuration(scene).ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "\n";
                for (int frame = 0; frame < frames; frame++)
                {
                    memory.SampleExport(scene, Mathf.Max(0,(startFrame+frame)/(float)fps-memory.SceneStart(scene)));
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    pixels.Apply();
                    var bytes = pixels.GetRawTextureData<byte>().ToArray();
                    process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                    if (frame == 90) File.WriteAllBytes(Path.Combine(output, "video-scene-" + (scene + 1) + ".png"), pixels.EncodeToPNG());
                    if (frame % 30 == 0) yield return null;
                }
                Debug.Log("VIDEO EXPORTED SCENE " + (scene + 1));
            }
            process.StandardInput.Close();
            process.WaitForExit();
            File.WriteAllText(Path.Combine(output, "video-timing.csv"), timing);
            Debug.Log("CINEMATIC VIDEO EXPORT " + (process.ExitCode == 0 ? "PASSED" : "FAILED"));
            Application.Quit(process.ExitCode);
        }
#endif
    }
}

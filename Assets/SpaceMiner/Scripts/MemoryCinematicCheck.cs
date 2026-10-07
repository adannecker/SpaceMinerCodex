using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SpaceMiner
{
    public sealed class MemoryCinematicCheck : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-memoryCinematicCheck")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-worldVisualCheck")>=0)SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode){SceneManager.sceneLoaded-=Loaded;new GameObject("Memory cinematic check").AddComponent<MemoryCinematicCheck>();}
        IEnumerator Start()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","outputs","Cinematic"));Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);
            var menu=FindFirstObjectByType<StartMenu>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-worldVisualCheck")<0) {
            if(menu==null||!CinematicLibrary.IsAvailable(CinematicLibrary.FirstMemories)||!menu.Replay(CinematicLibrary.FirstMemories)){Debug.LogError("Memory replay unavailable");Application.Quit(1);yield break;}
            var memory=menu.GetComponent<MemoryCinematic>();
            if(!memory.UsesContinuousNarration){Debug.LogError("Continuous narration missing");Application.Quit(1);yield break;}
            bool previousSubtitles=SettingsStore.Current.Accessibility.Subtitles;
            SettingsStore.Current.Accessibility.Subtitles=true;
            bool hasText=!string.IsNullOrEmpty(memory.CurrentSubtitle);
            SettingsStore.Current.Accessibility.Subtitles=false;
            bool hidden=string.IsNullOrEmpty(memory.CurrentSubtitle);
            SettingsStore.Current.Accessibility.Subtitles=previousSubtitles;
            if(!hasText||!hidden||Array.Find(CinematicLibrary.Entries,e=>e.Id==CinematicLibrary.FirstMemories).Kind!=StorySequenceKind.Cinematic){Debug.LogError("Subtitle or category check failed");Application.Quit(1);yield break;}
            for(int i=0;i<MemoryCinematic.SceneCount;i++){
                float timeout=Time.realtimeSinceStartup+30;
                while(MemoryCinematic.IsPlaying&&memory.SceneIndex<i&&Time.realtimeSinceStartup<timeout)yield return null;
                if(!MemoryCinematic.IsPlaying||memory.SceneIndex!=i){Debug.LogError("Missing memory scene "+i);Application.Quit(1);yield break;}
                yield return new WaitForSecondsRealtime(3);
                Capture(GameObject.Find("Memory camera").GetComponent<Camera>(),Path.Combine(folder,"scene-"+(i+1)+".png"));
            }
            while(MemoryCinematic.IsPlaying)yield return null;
            if(!StartMenu.IsOpen){Debug.LogError("Memory must return to menu");Application.Quit(1);yield break;}
            if(!menu.Replay(CinematicLibrary.FirstMemories)){Application.Quit(1);yield break;}
            memory.SampleExport(1,MemoryCinematic.DissolveSeconds/2);
            if(Mathf.Abs(memory.CrossDissolve-.5f)>.01f || memory.PageOpacity<.99f || MemoryCinematic.SceneGap>.2f){Debug.LogError("Cross dissolve check failed");Application.Quit(1);yield break;}
            Capture(memory.ExportCamera,Path.Combine(folder,"cross-dissolve-check.png"));
            memory.Stop();yield return null;
            if(MemoryCinematic.IsPlaying||!StartMenu.IsOpen){Application.Quit(1);yield break;}
            }
            var world=FindFirstObjectByType<RuinedWorld>();if(world==null||world.HomeMoon==null){Debug.LogError("Ruined world missing");Application.Quit(1);yield break;}
            if(world.transform.Find("Planet 9")==null || RuinedWorld.SunRadius<=RuinedWorld.HomeRadius*3){Debug.LogError("Solar system proportions missing");Application.Quit(1);yield break;}
            if(Mathf.Abs(Vector3.Distance(world.HomePosition,world.HomeMoon.position)-RuinedWorld.MoonDistance)>1 || Mathf.Abs(world.HomePosition.magnitude-RuinedWorld.StationDistance)>64 || Mathf.Abs(Vector3.Distance(world.HomePosition,RuinedWorld.SunPosition)-RuinedWorld.AstronomicalUnit)>64 || RenderSettings.sun==null){Debug.LogError("Astronomical distances or sunlight failed");Application.Quit(1);yield break;}
            Vector3 before=world.HomePosition;float orbitRadius=Vector3.Distance(before,RuinedWorld.SunPosition);world.AdvanceOrbits(600);
            if(Vector3.Distance(before,world.HomePosition)<.001f || Mathf.Abs(Vector3.Distance(world.HomePosition,RuinedWorld.SunPosition)-orbitRadius)>.05f){Debug.LogError("Orbit motion failed");Application.Quit(1);yield break;}world.AdvanceOrbits(-600);
            var orbit=FindFirstObjectByType<OrbitCamera>();menu.enabled=false;orbit.enabled=false;
            if(world.transform.Find("Companion sun")==null || RenderSettings.ambientLight.maxColorComponent<.2f || RenderSettings.sun.intensity<2){Debug.LogError("Binary lighting missing");Application.Quit(1);yield break;}
            orbit.ResetView();yield return null;Capture(Camera.main,Path.Combine(folder,"station-lighting.png"));
            orbit.Orbit(new Vector2(180,0));yield return null;Capture(Camera.main,Path.Combine(folder,"station-lighting-reverse.png"));
            orbit.LookAtCelestial(world.HomePosition-Camera.main.transform.position/1000f,12);yield return null;Capture(world.SkyCamera,Path.Combine(folder,"homeworld-telescope.png"));
            if(Vector3.Angle(Camera.main.transform.forward,(world.HomePosition-Camera.main.transform.position/1000f).normalized)>.02f || Camera.main.fieldOfView>13){Debug.LogError("Homeworld telescope failed");Application.Quit(1);yield break;}
            orbit.ResetView();if(Camera.main.fieldOfView<3){Debug.LogError("Station view restoration failed");Application.Quit(1);yield break;}
            orbit.SolarOverview(RuinedWorld.SunPosition,world.OverviewRadius);yield return null;
            var outer=world.transform.Find("Planet 9");var point=world.SkyCamera.WorldToViewportPoint(outer.position);
            if(point.z<=0||point.x<0||point.x>1||point.y<0||point.y>1){Debug.LogError("Solar overview does not frame outer orbit");Application.Quit(1);yield break;}
            Capture(world.SkyCamera,Path.Combine(folder,"solar-game-overview.png"));orbit.ResetView();yield return null;
            orbit.SolarOverview(RuinedWorld.SunPosition,world.OverviewRadius);yield return null;
            for(int i=0;i<PlanetLore.All.Length;i++)
            {
                if(!world.TrySelectPlanet(world.SkyCamera.WorldToScreenPoint(world.transform.Find("Planet "+(i+1)+(i==3?" - shattered homeworld":"")).position))||world.SelectedPlanet!=i){Debug.LogError("Planet picking failed "+i);Application.Quit(1);yield break;}
            }
            menu.StartDemo();FindFirstObjectByType<IntroSequence>().Skip();yield return null;
            orbit.SolarOverview(RuinedWorld.SunPosition,world.OverviewRadius);world.SelectPlanet(3);yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"planet-lore-ui.png"));yield return new WaitForSecondsRealtime(.3f);
            world.SelectPlanet(-1);orbit.ResetView();yield return null;
            world.enabled=false;var camera=world.SkyCamera;camera.nearClipPlane=.1f;Vector3 home=world.HomePosition;Vector3 position=home+RuinedWorld.HomeRadius*new Vector3(2.3f,2,4.4f);camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(home-position));camera.fieldOfView=55;
            yield return new WaitForSecondsRealtime(2);Capture(camera,Path.Combine(folder,"ruined-world.png"));yield return new WaitForSecondsRealtime(.5f);
            Vector3 sunView=RuinedWorld.SunPosition+RuinedWorld.SunRadius*new Vector3(0,.6f,-4.3f);camera.transform.SetPositionAndRotation(sunView,Quaternion.LookRotation(RuinedWorld.SunPosition-sunView));camera.fieldOfView=55;
            yield return new WaitForSecondsRealtime(1);Capture(camera,Path.Combine(folder,"sun.png"));yield return new WaitForSecondsRealtime(.5f);
            camera.transform.SetPositionAndRotation(RuinedWorld.SunPosition+new Vector3(0,RuinedWorld.AstronomicalUnit*50,-RuinedWorld.AstronomicalUnit*10),Quaternion.LookRotation(new Vector3(0,-RuinedWorld.AstronomicalUnit*50,RuinedWorld.AstronomicalUnit*10)));camera.farClipPlane=10000000000f;camera.fieldOfView=70;
            yield return new WaitForSecondsRealtime(.5f);Capture(camera,Path.Combine(folder,"solar-system.png"));
            camera.nearClipPlane=.1f;camera.transform.SetPositionAndRotation(Vector3.zero,Quaternion.LookRotation(world.HomePosition));camera.fieldOfView=48;
            yield return new WaitForSecondsRealtime(.5f);Capture(camera,Path.Combine(folder,"station-homeworld.png"));
            camera.transform.rotation=Quaternion.LookRotation(RuinedWorld.SunPosition);yield return new WaitForSecondsRealtime(.5f);Capture(camera,Path.Combine(folder,"station-sun.png"));
            Debug.Log(Array.IndexOf(Environment.GetCommandLineArgs(),"-worldVisualCheck")>=0?"SOLAR SYSTEM CHECK PASSED: nine planets, rings, orbit motion, homeworld and moon":"MEMORY CINEMATIC CHECK PASSED: twelve scenes, completion, replay, skip and solar system");Application.Quit(0);
        }
        static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(1440,900,24);var previous=camera.targetTexture;var active=RenderTexture.active;
            var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);
            try{
                if(camera==Camera.main){var sky=FindFirstObjectByType<RuinedWorld>().SkyCamera;var oldSky=sky.targetTexture;try{sky.targetTexture=target;sky.Render();}finally{sky.targetTexture=oldSky;}}
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1440,900),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(pixels);}
        }
#endif
    }
}

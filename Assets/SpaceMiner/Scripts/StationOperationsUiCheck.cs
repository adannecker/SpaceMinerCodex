#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class StationOperationsUiCheck:MonoBehaviour
    {
        private const string Folder="Logs/StationOperationsUI";
        private int checks;
        private void Approach(StationOperations ops,StationTerminalKind kind)
        {
            var terminal=Array.Find(ops.GetComponentsInChildren<StationTerminal>(),t=>t.kind==kind);
            if(terminal==null)throw new Exception("Missing real terminal "+kind);
            var feet=terminal.transform.TransformPoint(new Vector3(0,.04f,-1.55f));
            StationInteriorMode.Current.RestoreInterior(feet,false,terminal.transform.eulerAngles.y,0);
            Physics.SyncTransforms();
            Require(ops.NearestTerminal(StationInteriorMode.Current.FeetPosition)==terminal,kind+" reachable with unobstructed interaction");
        }
        private void Require(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("OPERATIONS UI OK: "+message);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stationOperationsUiCheck")>=0)new GameObject("Operations UI inspection").AddComponent<StationOperationsUiCheck>();}
        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"/"+name+".png"));yield return new WaitForSecondsRealtime(.4f);Guard(()=>{var pixels=new Texture2D(2,2);try{Require(pixels.LoadImage(File.ReadAllBytes(Folder+"/"+name+".png")),name+" screenshot readable");int bright=0;foreach(var p in pixels.GetPixels32())if(p.r>40||p.g>40||p.b>40)bright++;Require(bright>1000,name+" contains rendered image and UI");}finally{Destroy(pixels);}});}
        private bool Guard(Action action){try{action();return true;}catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());Debug.LogException(e);Application.Quit(1);return false;}}
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);File.Delete(Folder+"/result.json");File.Delete(Folder+"/error.txt");yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath(Folder+"/Saves");
            CinematicLibrary.PreferencePrefix="SpaceMiner.Test.StationOperationsUI.";
            FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();yield return null;yield return null;
            var ops=StationOperations.Current;var scenario=ops.Scenario;var mode=StationInteriorMode.Current;scenario.Scanner.DismissDialogue();scenario.SetSimulationRate(0);
            // Recreate the backbuffer: a hidden Windows launch at an unchanged size can capture black.
            Screen.SetResolution(1440,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.35f);
            Screen.SetResolution(1600,1000,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);
            if(!Guard(()=>{Require(ops.Live,"isolated operations UI active");Approach(ops,StationTerminalKind.Research);Require(ops.InteractionPrompt(mode.FeetPosition).Contains("TECHTREE"),"research E prompt at real approach position");}))yield break;
            yield return Capture("01-hud-und-e-hinweis");
            if(!Guard(()=>ops.OpenTerminal(ops.NearestTerminal(mode.FeetPosition))))yield break;yield return Capture("02-forschungsterminal");
            if(!Guard(()=>{Require(ops.PanelOpen&&SettingsMenu.BlocksInput,"terminal captures gameplay input");ops.ClosePanel();Approach(ops,StationTerminalKind.Drones);ops.OpenTerminal(ops.NearestTerminal(mode.FeetPosition));}))yield break;
            yield return Capture("03-drohnenverwaltung");
            if(!Guard(()=>{ops.ClosePanel();Approach(ops,StationTerminalKind.Planning);ops.OpenTerminal(ops.NearestTerminal(mode.FeetPosition));}))yield break;
            yield return Capture("04-stationsplanung");
            if(!Guard(()=>{ops.ClosePanel();Approach(ops,StationTerminalKind.Production);ops.OpenTerminal(ops.NearestTerminal(mode.FeetPosition));}))yield break;
            yield return Capture("05-produktion-und-lager");
            if(!Guard(()=>{ops.ClosePanel();SettingsStore.Current.Accessibility.TextScale=1.4f;Screen.SetResolution(1280,720,FullScreenMode.Windowed);}))yield break;yield return new WaitForSecondsRealtime(.5f);yield return Capture("06-grosse-schrift-1280");
            if(!Guard(()=>{Require(ops.HudResourceBounds.yMax<ops.HudTasksBounds.yMin,"resources and tasks do not overlap at enlarged text");ops.ExplainQuest(0);Require(scenario.Scanner.Dialogue!=null,"task opens actual Mira dialogue");}))yield break;
            yield return Capture("07-aufgabe-mit-mira");
            var display=FindFirstObjectByType<DisplayModeController>();Vector2Int native=DisplayModeController.MonitorResolution;
            if(!Guard(()=>{scenario.Scanner.DismissDialogue();SettingsStore.Current.Accessibility.TextScale=1;Require(display!=null,"display controller installed");display.SetFullscreen(true);}))yield break;yield return new WaitForSecondsRealtime(2);yield return Capture("08-vollbild");
            if(!Guard(()=>{Require(Screen.fullScreen&&Screen.width==native.x&&Screen.height==native.y,"real fullscreen controller selects native monitor framebuffer");display.SetFullscreen(false);}))yield break;yield return new WaitForSecondsRealtime(2);
            if(!Guard(()=>{Require(!Screen.fullScreen&&Screen.width==1280&&Screen.height==720,"fullscreen controller restores prior window dimensions");File.WriteAllText(Folder+"/result.json","{\"passed\":true,\"checks\":"+checks+",\"scope\":\"programmatic real terminal opens, UI captures, scaling, Mira and actual native fullscreen/window restore; no manual keyboard/mouse test\"}");}))yield break;
            Application.Quit(0);
        }
    }
}
#endif

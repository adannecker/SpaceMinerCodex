#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SpaceMiner
{
    public sealed class TouchConsoleCheck:MonoBehaviour
    {
        private int checks;private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-touchConsoleCheck")>=0)new GameObject("R04 acceptance").AddComponent<TouchConsoleCheck>();}
        private void Require(bool ok,string text){checks++;if(!ok)throw new Exception(text);Debug.Log("R04 OK "+text);}
        private void OnEnable()=>Application.logMessageReceived+=Observe;
        private void OnDisable()=>Application.logMessageReceived-=Observe;
        private void Observe(string text,string trace,LogType kind){if(kind==LogType.Exception){failed=true;File.WriteAllText("Logs/TouchConsole/error.txt",text+"\n"+trace);Application.Quit(1);}}
        private IEnumerator Start()
        {
            Directory.CreateDirectory("Logs/TouchConsole");yield return new WaitForSecondsRealtime(1);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath("Logs/TouchConsole/Saves");
            CinematicLibrary.PreferencePrefix="SpaceMiner.Test.TouchConsole."+Guid.NewGuid()+".";
            FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();yield return null;yield return null;
            var ops=StationOperations.Current;var mode=StationInteriorMode.Current;ops.Scenario.SetSimulationRate(0);ops.Scenario.Scanner.DismissDialogue();
            var visuals=ops.Layout.GetComponentsInChildren<TouchConsoleVisual>();Require(visuals.Length==6,"six real consoles including wide main");
            Require(visuals.Count(v=>v.Main)==1,"one wide main console");
            foreach(var v in visuals){
                var root=v.transform.parent;var box=root.Find("Blockoutbody").GetComponent<BoxCollider>();
                Require(v.GetComponentsInChildren<MeshFilter>().Length==3,"three material-batched R04 mesh groups / "+root.name);
                Require(!root.Find("Blockoutbody").GetComponent<Renderer>().enabled,"old box hidden / "+root.name);
                Require(Mathf.Abs(Vector3.Scale(box.size,box.transform.lossyScale).x-v.Width)<.01f,"real collider width / "+root.name);
                Require(Mathf.Abs(Vector3.Scale(box.size,box.transform.lossyScale).y-TouchConsoleVisual.Height)<.01f,"real collider height / "+root.name);
            }
            var main=visuals.First(v=>v.Main);var frame=main.transform;
            mode.RestoreInterior(frame.TransformPoint(new Vector3(0,.04f,-3.4f)),false,frame.eulerAngles.y,20);yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/TouchConsole/main.png"));
            var terminals=ops.GetComponentsInChildren<StationTerminal>();Require(terminals.Length==5,"five preserved functional terminals");
            foreach(var terminal in terminals){
                ops.ClosePanel();ops.OpenTerminal(terminal);Require(ops.PanelOpen,"terminal opens / "+terminal.label);ops.ClosePanel();
                var root=terminal.GetComponentInParent<CommandRoomBlock>().transform;var v=root.GetComponentInChildren<TouchConsoleVisual>();
                mode.RestoreInterior(v.transform.TransformPoint(new Vector3(0,.04f,-1.65f)),false,root.eulerAngles.y,27);
                yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();
                Require(ops.NearestTerminal(mode.FeetPosition)==terminal,"physical E approach / "+terminal.label);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/TouchConsole/"+terminal.kind+".png"));
            }
            yield return new WaitForSecondsRealtime(.5f);if(failed)yield break;
            File.WriteAllText("Logs/TouchConsole/result.json","{\"passed\":true,\"checks\":"+checks+"}");Debug.Log("R04 CONSOLE CHECK PASSED "+checks);Application.Quit(0);
        }
    }
}
#endif

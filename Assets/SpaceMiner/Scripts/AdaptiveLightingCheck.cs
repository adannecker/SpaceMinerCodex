#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace SpaceMiner
{
    public sealed class AdaptiveLightingCheck:MonoBehaviour
    {
        private int checks;private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-adaptiveLightingCheck")>=0)new GameObject("Adaptive light acceptance").AddComponent<AdaptiveLightingCheck>();}
        private void Require(bool ok,string label){checks++;if(!ok)throw new Exception(label);Debug.Log("LIGHT OK "+label);}
        private void OnEnable()=>Application.logMessageReceived+=Observe;
        private void OnDisable()=>Application.logMessageReceived-=Observe;
        private void Observe(string msg,string trace,LogType type){if(type==LogType.Exception){failed=true;Directory.CreateDirectory("Logs/AdaptiveLighting");File.WriteAllText("Logs/AdaptiveLighting/error.txt",msg+"\n"+trace);Application.Quit(1);}}
        private IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory("Logs/AdaptiveLighting");yield return new WaitForSecondsRealtime(1);
            File.Delete("Logs/AdaptiveLighting/error.txt");File.Delete("Logs/AdaptiveLighting/result.json");
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath("Logs/AdaptiveLighting/Saves");
            CinematicLibrary.PreferencePrefix="SpaceMiner.Test.AdaptiveLighting."+Guid.NewGuid()+".";
            FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();yield return null;yield return null;
            var ops=StationOperations.Current;var mode=StationInteriorMode.Current;ops.Scenario.SetSimulationRate(0);ops.Scenario.Scanner.DismissDialogue();
            Require(ops.Live,"real campaign simulation enabled for lighting acceptance");
            var root=ops.RoomPlan.BoundRoot("main-console").GetComponentInChildren<TouchConsoleVisual>().transform;
            mode.RestoreInterior(root.TransformPoint(new Vector3(0,.04f,-3.4f)),false,root.eulerAngles.y,20);
            yield return new WaitForSecondsRealtime(2);
            Require(ops.CurrentLightingZone=="command","physical occupant selects command room");
            Require(ops.LightingLevel("command")>.9f,"occupied room reaches work light");
            Require(ops.LightingLevel("workshop")<.16f,"other room remains dim");
            float workDemand=ops.LightingDemandKw;
            Require(workDemand>0&&workDemand<=StationOperations.LightingBudgetKw,"real demand stays within allocated systems budget");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/AdaptiveLighting/command-active.png"));
            float battery=ops.State.power.storedKwh;
            yield return new WaitForSecondsRealtime(.6f);Require(Mathf.Abs(battery-ops.State.power.storedKwh)<.00001f,"light updates during pause do not consume simulation energy");
            mode.Exit();yield return new WaitForSecondsRealtime(4.5f);
            Require(ops.CurrentLightingZone=="outside","outside camera leaves no occupied room");
            Require(ops.LightingLevel("command")<.16f,"vacated room dims after grace and fade");
            Require(ops.LightingDemandKw<workDemand,"idle station genuinely requests less electricity");
            float idleDemand=ops.LightingDemandKw;
            mode.RestoreInterior(ops.Layout.transform.TransformPoint(RingWalkthrough.At(46,30,.04f)),false,ops.Layout.transform.eulerAngles.y+30,12);
            yield return new WaitForSecondsRealtime(2);
            Require(ops.CurrentLightingZone=="workshop","workshop occupancy detected independently");
            Require(ops.LightingLevel("workshop")>.9f&&ops.LightingLevel("command")<.16f,"entering second room does not brighten first room");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/AdaptiveLighting/workshop-active.png"));
            var extra=new GameObject("Test preview camera").AddComponent<Camera>();extra.enabled=false;extra.transform.position=root.position+Vector3.up*2;
            ops.RefreshViewLighting(extra,true);Require(ops.CurrentLightingZone=="workshop","preview camera does not trigger remote occupancy");Destroy(extra.gameObject);
            float consumers=0;bool found=false;foreach(var consumer in ops.PowerConsumers){consumers+=consumer.SuppliedKw;if(consumer.Id=="lighting")found=true;}
            Require(found,"lighting appears as its own power consumer");Require(Mathf.Abs(consumers-ops.State.power.ConsumptionKw)<.0001f,"power dashboard sums the actual billed power");
            bool originalPower=ops.Element("sector-1").power;
            if(originalPower)ops.TogglePower(1);else ops.RefreshViewLighting(mode.GetComponent<Camera>(),true);
            Require(!ops.Powered(1),"workshop circuit is actually disconnected");
            bool lit=false;foreach(var lamp in ops.Layout.GetComponentsInChildren<Light>())if(lamp.name=="Adaptive Arbeitsleuchte"&&StationOperations.LightingZone(ops.Layout.transform.InverseTransformPoint(lamp.transform.position))=="workshop"&&lamp.enabled)lit=true;
            Require(!lit,"disconnected room lamps stay off immediately");if(originalPower)ops.TogglePower(1);
            yield return new WaitForSecondsRealtime(.5f);if(failed)yield break;
            File.WriteAllText("Logs/AdaptiveLighting/result.json","{\"passed\":true,\"checks\":"+checks+",\"commandDemandKw\":"+workDemand.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"idleDemandKw\":"+idleDemand.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            Debug.Log("ADAPTIVE LIGHTING CHECK PASSED "+checks);Application.Quit(0);
        }
    }
}
#endif

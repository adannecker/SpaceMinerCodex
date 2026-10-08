using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class ScanSaveCheck : MonoBehaviour
    {
        private int checks;
        private void Require(bool condition,string text){if(!condition)throw new Exception(text);checks++;}
        private IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-scanSaveCheck")<0)yield break;
            yield return null;
            var scenario=FindFirstObjectByType<WaterScenario>();var scanner=scenario.Scanner;var orbit=GetComponent<OrbitCamera>();
            GetComponent<IntroSequence>()?.Skip();scenario.SimulationRate=0;scenario.ResetScenario();
            yield return null;
            string directory=Path.Combine(Application.temporaryCachePath,"ScanSaveCheck-"+Guid.NewGuid().ToString("N"));
            try
            {
                Require(StationInteriorMode.IsInside && !scanner.AtConsole && Mathf.Abs(scanner.Charge-.97f)<.00001f,"start walking inside with 97 percent");
                scanner.OpenConsole(); Require(!scanner.AtConsole && !scanner.Scan(),"remote pult interaction rejected");
                Require(Resources.Load<Texture2D>("Mira/MiraDialoguePortraits")!=null,"Mira portrait available");
                Require(Array.TrueForAll(scenario.Asteroids,a=>!a.IsScanned&&!a.CanMineWater),"all contacts unknown");
                scanner.OpenVirtualView();
                var unknown=scenario.Asteroids[0];
                Require(unknown.GetComponent<Renderer>().forceRenderingOff && !unknown.GetComponent<Collider>().enabled,"unknown hidden and unpickable");
                orbit.Focus(unknown.Info);Require(orbit.Selected==null,"programmatic focus cannot reveal unknown");
                scanner.SetDebugVisibility(true);
                Require(!unknown.GetComponent<Renderer>().forceRenderingOff&&!unknown.CanMineWater,"debug reveals without working permission");
                Require(!scenario.AssignTankOrder(unknown),"unscanned order rejected");
                scanner.SetDebugVisibility(false);scanner.ReturnToStation();
                var interior=StationInteriorMode.Current; interior.RestoreInterior(interior.Layout.Console.position-interior.Room.forward*1.5f,false);scanner.OpenConsole();
                Require(!scanner.Scan(),"early scan rejected");
                scanner.Advance(9);Require(!scanner.Ready,"not charged before ten seconds");
                scanner.Advance(1);Require(scanner.Ready,"ready at ten seconds");
                Require(scanner.Scan()&&scanner.KnownCount==10,"first scan finds ten");
                var interiorSave=SaveGameStore.Capture(scenario,"Inside","Pult"); scanner.OpenVirtualView(); SaveGameStore.Apply(interiorSave,scenario);
                Require(StationInteriorMode.IsInside && scanner.AtConsole && Vector3.Distance(interiorSave.interiorFeet,StationInteriorMode.Current.FeetPosition)<.01f,"inside position and console restored");
                Require(Array.FindAll(scenario.Asteroids,a=>a.WaterIdentified).Length==3,"three confirmed water sources");
                Require(scanner.EnergyKwh==0&&!scanner.Scan(),"scan consumes energy, duplicate rejected");
                scanner.OpenVirtualView();scanner.DismissDialogue();
                Require(unknown.IsScanned && unknown.GetComponent<Renderer>().sharedMaterial.name.Contains("VR contact"),"unmapped contacts have no surface texture");
                Require(Array.TrueForAll(scenario.Asteroids,a=>a.IsScanned||a.GetComponent<Renderer>().forceRenderingOff),"only scanned bodies visible");
                var before=scanner.EnergyKwh;
                for(int i=0;i<20;i++)scenario.Mining.CompleteTrip();
                var source=Array.Find(scenario.Asteroids,a=>a.IsStarterWaterSource);
                Require(scenario.AssignTankOrder(source),"scanned source assignable");
                var baseline=SaveGameStore.Capture(scenario,"Teststation","Erster Scan; Drohne unterwegs.");
                string file=SaveGameStore.Write(baseline,false,directory);
                var saved=SaveGameStore.Read(file);
                Require(saved.name==baseline.name&&saved.comment==baseline.comment&&saved.throughput==1,"name, comment, research persisted");
                scenario.Advance(37);SaveGameStore.Apply(saved,scenario);
                Require(scenario.Worker.Phase==saved.drones[0].phase && scenario.Worker.PhaseElapsed==saved.drones[0].phaseSeconds,"running phase restored");
                Require(scanner.EnergyKwh==before&&scenario.Mining.Level==2,"scanner and research restored");
                string auto=SaveGameStore.Write(saved,true,directory);SaveGameStore.Write(saved,true,directory);
                Require(File.Exists(auto+".bak")&&SaveGameStore.Read(auto+".bak").name==saved.name,"previous autosave recoverable");
                var visited=new System.Collections.Generic.HashSet<DronePhase>();
                for(int i=0;i<30000&&scenario.Deliveries<1;i++)
                {
                    var phase=scenario.Worker.Phase;
                    if(visited.Add(phase))
                    {
                        var state=SaveGameStore.Capture(scenario,"Phase "+phase,"Resume check");
                        scenario.Advance(.25f);var expected=SaveGameStore.Capture(scenario,"Expected","");
                        SaveGameStore.Apply(state,scenario);scenario.Advance(.25f);
                        Require(scenario.Worker.Phase==expected.drones[0].phase &&
                            Vector3.Distance(scenario.Worker.transform.position,expected.drones[0].position)<.001f &&
                            Mathf.Abs(scenario.Worker.BatteryKwh-expected.drones[0].battery)<.00001f &&
                            Mathf.Abs(scenario.Worker.CargoKg-expected.drones[0].cargo)<.00001f,"phase resume matches uninterrupted "+phase);
                    }
                    else scenario.Advance(.25f);
                }
                Require(scenario.Deliveries==1 && visited.Contains(DronePhase.Mining)&&visited.Contains(DronePhase.Returning),"restored mining delivers once");
                var corrupted=SaveGameStore.Capture(scenario,"Broken","");corrupted.asteroids[0].id="missing";
                float water=scenario.WaterLiters;bool rejected=false;
                try{SaveGameStore.Apply(corrupted,scenario);}catch(IOException){rejected=true;}
                Require(rejected&&scenario.WaterLiters==water,"invalid save rejected before mutation");
                var menu=GetComponent<SaveGameMenu>();menu.StorageDirectoryOverride=directory;menu.NewSession();
                bool left=false;menu.Leave(()=>left=true);
                Require(SaveGameMenu.IsOpen&&SettingsMenu.PausesSimulation&&!left,"leave asks to save and pauses");
                Require(File.Exists(Path.Combine(directory,"autosave.json")),"leave creates autosave");
                menu.CancelDialog();Require(!left&&SaveGameMenu.BlocksInput,"cancel retains session and consumes frame");
                string autosavePath=Path.Combine(directory,"autosave.json");
                string previousAuto=File.ReadAllText(autosavePath);
                menu.AdvanceAutosave(59);Require(File.ReadAllText(autosavePath)==previousAuto,"no early autosave");
                scenario.RestoreSupply(scenario.WaterLiters+1,scenario.Deliveries,scenario.DeliveredLiters,scenario.SourceAssigned);
                menu.AdvanceAutosave(1);Require(SaveGameStore.Read(autosavePath).water==scenario.WaterLiters,"autosave at sixty seconds");
                Require(!menu.RequestApplicationExit()&&SaveGameMenu.IsOpen,"application close asks to save");
                menu.CancelDialog();
                scanner.ReturnToStation();var room=StationInteriorMode.Current; room.RestoreInterior(room.Layout.Console.position-room.Room.forward*1.5f,false); scanner.OpenConsole();scanner.DismissDialogue();scenario.SimulationRate=0;
                File.WriteAllText("Logs/scan-save-result.json","{\"passed\":true,\"checks\":"+checks+",\"rangeMeters\":"+scanner.RangeMeters.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
                Debug.Log("SCAN AND SAVE CHECK PASSED: "+checks+" checks; range "+scanner.RangeMeters+" m");
            }
            catch(Exception error){Debug.LogException(error);Application.Quit(1);yield break;}
            yield return new WaitForEndOfFrame();
            scenario.ResetScenario();
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture("Logs/scanner-console.png");
            yield return new WaitForSecondsRealtime(.5f);
            var consoleRoom=StationInteriorMode.Current; consoleRoom.RestoreInterior(consoleRoom.Layout.Console.position-consoleRoom.Room.forward*1.5f,false);scanner.OpenConsole();
            yield return new WaitForEndOfFrame(); Capture("Logs/station-main-console.png");
            scanner.Advance(10);scanner.Scan();scanner.DismissDialogue();
            scanner.OpenVirtualView();orbit.Overview();
            yield return new WaitForEndOfFrame();Capture("Logs/scanner-virtual.png");
            yield return new WaitForSecondsRealtime(.5f);
            GetComponent<SaveGameMenu>().OpenSave();
            yield return new WaitForEndOfFrame();Capture("Logs/save-dialog.png");
            yield return new WaitForSecondsRealtime(.5f);
            GetComponent<SaveGameMenu>().OpenLoad();
            yield return new WaitForEndOfFrame();Capture("Logs/load-dialog.png");
            yield return new WaitForSecondsRealtime(.5f);
            GetComponent<SaveGameMenu>().CancelDialog();
            Application.Quit(0);
        }
        private static void Capture(string path)
        {
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(path));
        }
    }
}

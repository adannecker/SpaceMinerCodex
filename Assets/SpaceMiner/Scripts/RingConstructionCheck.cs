#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class RingConstructionCheck : MonoBehaviour
    {
        private int checks;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-ringSimulatorCheck")>=0)
                new GameObject("Ring construction integration check").AddComponent<RingConstructionCheck>();
        }
        private void Require(bool value,string description)
        {
            checks++;
            if(value){Debug.Log("RING CHECK OK: "+description);return;}
            Debug.LogError("RING CHECK FAILED: "+description);Application.Quit(1);throw new Exception(description);
        }
        private static string Folder=>Path.GetFullPath("Logs/RingSimulator-Stacked-"+Screen.width+"x"+Screen.height);
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,name+".png"));yield return new WaitForSecondsRealtime(.2f);
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var args=Environment.GetCommandLineArgs();int width=1600,height=1000;
            int wi=Array.IndexOf(args,"-screen-width"),hi=Array.IndexOf(args,"-screen-height");
            if(wi>=0)int.TryParse(args[wi+1],out width);if(hi>=0)int.TryParse(args[hi+1],out height);
            var defaults=new SpaceMinerPlayerSettings();defaults.Graphics.DisplayMode=0;defaults.Graphics.Width=width;defaults.Graphics.Height=height;
            Require(SettingsStore.Save(defaults,out _),"isolated settings applied");
            CinematicLibrary.PreferencePrefix="SpaceMiner.Test.Ring."+Guid.NewGuid()+".";
            yield return new WaitForSecondsRealtime(.5f);Directory.CreateDirectory(Folder);
            Require(Screen.width==width&&Screen.height==height,"requested resolution");
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.Combine(Folder,"Saves");
            FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();
            yield return null;
            var scenario=FindFirstObjectByType<WaterScenario>();scenario.Scanner.DismissDialogue();
            var simulator=FindFirstObjectByType<RingConstructionSimulator>();
            Require(simulator!=null,"simulator installed in game scene");
            var model=new RingConstructionModel();
            Require(model.Rings[0].Complete&&model.Slots(0)==12,"base ring and four reserved crossings");
            int budget=model.ConstructionParts;
            Require(!model.Place(0,0,RingConstructionModel.Tool.Storage,out _)&&budget==model.ConstructionParts,"crossing rejects container without spending");
            Require(!model.Place(0,6,RingConstructionModel.Tool.Fabricator,out _),"production collision rejects placement");
            Require(model.Place(0,3,RingConstructionModel.Tool.Storage,out _),"outside storage accepted");
            Require(!model.Place(0,3,RingConstructionModel.Tool.Storage,out _),"queued footprint reserved immediately");
            Require(model.Place(0,3,RingConstructionModel.Tool.Separator,out _),"inside production shares sector with outside storage");
            Require(model.ConstructionParts==budget-35-60,"cost charged once per accepted job");
            var collision=new RingConstructionModel();var duplicateRing=new RingConstructionModel.Ring(16,32);Array.Fill(duplicateRing.Segments,true);collision.Rings.Add(duplicateRing);
            Require(!collision.Place(1,12,RingConstructionModel.Tool.Fabricator,out string overlap)&&overlap.Contains("Abstand"),"geometric overlap rejected across different ring indices");
            var corridor=new RingConstructionModel();var narrow=new RingConstructionModel.Ring(10,16);Array.Fill(narrow.Segments,true);corridor.Rings.Add(narrow);
            Require(!corridor.Place(1,1,RingConstructionModel.Tool.Separator,out string blocked)&&blocked.Contains("Übergang"),"adjacent footprint cannot intrude into reserved corridor");
            model.Tick(0);Require(!model.At(0,3,true).Built,"paused construction does not advance");
            model.Tick(10);Require(model.At(0,3,true).Built&&model.At(0,3,false).Built,"drone completes queued modules");
            Require(model.CapacityFor(RingConstructionModel.Product.Parts)==25&&model.CapacityFor(RingConstructionModel.Product.Ore)==25&&model.CapacityFor(RingConstructionModel.Product.Metal)==25,"new storage adds capacity only for assigned product");
            Require(model.AddRing(out _)&&model.Rings[1].Radius==model.Rings[0].Radius&&model.Rings[1].Height>model.Rings[0].Height,"second ring has same radius on higher level");
            Require(!model.AddRing(out _),"prototype limits expansion to two stacked rings");
            Require(!model.Place(1,2,RingConstructionModel.Tool.Storage,out _),"unbuilt floor rejects module");
            model.Tick(3);Require(model.Rings[1].Segments[0]&&!model.Rings[1].Segments[1],"ring grows one segment at a time");
            model.Tick(45);Require(model.Rings[1].Complete,"all expansion segments complete");
            Require(model.Place(1,2,RingConstructionModel.Tool.Storage,out _),"upper ring accepts storage directly above existing lower storage");
            Require(model.Place(1,2,RingConstructionModel.Tool.Fabricator,out _),"new ring accepts inside factory");
            model.Tick(10);Require(!model.AddRing(out _)&&model.Slots(1)==model.Slots(0),"each level adds an equal set of building slots");
            foreach(var a in model.Modules)foreach(var b in model.Modules){
                if(ReferenceEquals(a,b))continue;
                var pa=model.Position(a.Ring,a.Sector,a.Kind==RingConstructionModel.Tool.Storage?3.8f:-3.8f);
                var pb=model.Position(b.Ring,b.Sector,b.Kind==RingConstructionModel.Tool.Storage?3.8f:-3.8f);
                Require(Vector3.Distance(pa,pb)>3.4f,"sampled module footprints stay separated");
            }
            var chain=new RingConstructionModel();int total=chain.Ore+chain.Metal+chain.Products;chain.Tick(12);
            Require(chain.Ore<20&&chain.Products==0&&chain.Status(chain.At(0,13,false)).Contains("Lager"),"factory waits until its product has dedicated storage");
            Require(chain.Place(0,3,RingConstructionModel.Tool.Storage,out _,RingConstructionModel.Product.Parts),"build missing product storage");chain.Tick(200);
            Require(chain.Ore+chain.Metal+chain.Products==total,"processing conserves test inventory");
            chain.Tick(200);Require(chain.Ore==0&&chain.Metal==0&&chain.Products==total,"chain finishes and waits for resources");
            Require(chain.Status(chain.At(0,6,false)).Contains("fehlt"),"missing input reason visible");
            Require(chain.SupplyOre()&&!chain.SupplyOre(),"ore deliveries respect only ore storage capacity");
            chain.Tick(200);Require(chain.Products==chain.CapacityFor(RingConstructionModel.Product.Parts)&&chain.Status(chain.At(0,13,false)).Contains("voll"),"full product storage stalls factory");
            Require(chain.Place(0,5,RingConstructionModel.Tool.Storage,out _,RingConstructionModel.Product.Metal),"additional metal container accepted");chain.Tick(5);
            Require(chain.CapacityFor(RingConstructionModel.Product.Metal)==50&&chain.CapacityFor(RingConstructionModel.Product.Ore)==25&&chain.CapacityFor(RingConstructionModel.Product.Parts)==25,"metal storage does not increase other product capacities");
            simulator.Open();Require(RingConstructionSimulator.IsOpen&&SettingsMenu.BlocksInput&&SettingsMenu.PausesSimulation,"planning pauses campaign and blocks controls");
            var oldPause=SettingsStore.Current.Gameplay.PauseInMenu;SettingsStore.Current.Gameplay.PauseInMenu=false;
            Require(SettingsMenu.PausesSimulation,"planner pauses even when normal menus do not");SettingsStore.Current.Gameplay.PauseInMenu=oldPause;
            float water=scenario.WaterLiters;var worker=scenario.Worker.transform.position;
            yield return Capture("01-base-ring");
            Require(simulator.CheckPicking(),"camera ray selects inner and outer building slots");
            simulator.Model.AddRing(out _);simulator.SelectLevel(1,false);simulator.Model.Tick(24);
            yield return Capture("02-ring-under-construction");
            simulator.Model.Tick(24);simulator.Model.Place(1,2,RingConstructionModel.Tool.Storage,out _);simulator.Model.Place(1,3,RingConstructionModel.Tool.Separator,out _);simulator.Model.Place(1,6,RingConstructionModel.Tool.Fabricator,out _);simulator.Model.Tick(15);
            yield return Capture("03-two-rings-production");
            Require(simulator.CheckPicking(),"camera picks upper plane independently of lower plane");
            simulator.SelectLevel(0,true);yield return Capture("04-lower-level-only");Require(simulator.CheckPicking(),"lower plane remains selectable after expansion");
            simulator.SelectLevel(1,true);yield return Capture("05-upper-level-only");Require(simulator.CheckPicking(),"isolated upper plane remains selectable");
            Require(Mathf.Approximately(water,scenario.WaterLiters)&&worker==scenario.Worker.transform.position,"planner does not mutate campaign water or worker");
            var settings=FindFirstObjectByType<SettingsMenu>();settings.Open();Require(SettingsMenu.IsOpen&&!RingConstructionSimulator.IsOpen,"settings replaces planner");
            simulator.Open();Require(RingConstructionSimulator.IsOpen&&!SettingsMenu.IsOpen,"planner replaces settings");
            FindFirstObjectByType<TechTreeMenu>().Open();Require(TechTreeMenu.IsOpen&&!RingConstructionSimulator.IsOpen,"techtree replaces planner");
            simulator.Open();Require(!TechTreeMenu.IsOpen&&simulator.Model.Rings.Count==2,"reopen retains stacked station and closes techtree");
            simulator.Close();Require(RingConstructionSimulator.BlocksInput,"close consumes input frame");
            yield return null;Require(!RingConstructionSimulator.BlocksInput,"input released next frame");
            Debug.Log("RING SIMULATOR CHECK PASSED: "+checks+" checks");Application.Quit();
        }
    }
}
#endif

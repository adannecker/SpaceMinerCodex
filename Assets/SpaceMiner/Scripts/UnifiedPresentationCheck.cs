#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner
{
    // Opt-in integration checks of the real presenters and menu routes, no synthetic save data.
    public sealed class UnifiedPresentationCheck : MonoBehaviour
    {
        private int checks;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-presentationCheck")>=0)
                new GameObject("Unified presentation check").AddComponent<UnifiedPresentationCheck>();
        }
        private static string Folder=>Path.GetFullPath("Logs/Presentation-"+Screen.width+"x"+Screen.height);
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.25f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,name+".png"));
            yield return new WaitForSecondsRealtime(.2f);
        }
        private void Require(bool value,string message)
        {
            checks++;if(value)return;
            Debug.LogError("UNIFIED PRESENTATION CHECK FAILED: "+message);Application.Quit(1);throw new Exception(message);
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var args=Environment.GetCommandLineArgs();
            int width=1600,height=1000;
            int wi=Array.IndexOf(args,"-screen-width"),hi=Array.IndexOf(args,"-screen-height");
            if(wi>=0)int.TryParse(args[wi+1],out width);
            if(hi>=0)int.TryParse(args[hi+1],out height);
            var defaults=new SpaceMinerPlayerSettings();
            defaults.Graphics.DisplayMode=0;defaults.Graphics.Width=width;defaults.Graphics.Height=height;
            Require(SettingsStore.Save(defaults,out _),"isolated window settings apply");
            CinematicLibrary.PreferencePrefix="SpaceMiner.Test.Presentation."+Guid.NewGuid()+".";
            yield return new WaitForSecondsRealtime(.5f);
            Require(Screen.width==width&&Screen.height==height&&!Screen.fullScreen,"requested viewport applied");
            Directory.CreateDirectory(Folder);
            FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.Combine(Folder,"Saves");
            var start=FindFirstObjectByType<StartMenu>();var menu=FindFirstObjectByType<TechTreeMenu>();
            Require(start!=null&&menu!=null,"game components present");
            Require(!TechTreeMenu.ShortcutAllowed,"shortcut blocked on start menu");
            start.StartDemo();var intro=FindFirstObjectByType<IntroSequence>();intro.Skip();yield return null;
            var scenario=FindFirstObjectByType<WaterScenario>();scenario.Scanner.DismissDialogue();
            menu.ToggleFromShortcut();Require(TechTreeMenu.IsOpen&&menu.Presentation.PlayerPresentation,"shortcut opens replacement tree");
            Require(menu.Presentation.Available("drohnen-01")&&!menu.Presentation.Available("drohnen-02"),"known bright and unavailable dim");
            for(int tree=0;tree<11;tree++){
                menu.Presentation.PreviewOverview(tree);yield return Capture("tree-"+tree);
                Require(menu.Presentation.TreeRect.xMax<menu.Presentation.SidebarRect.xMin,"tree does not overlap sidebar "+tree);
            }
            menu.Presentation.Preview(5,"drohnen-02");yield return Capture("sidebar-locked");
            var p=menu.Presentation.Simulation.State("drohnen-02");p.evidence=true;p.hardware=true;
            Require(menu.Presentation.Available("drohnen-02"),"met prerequisites become available");
            p.evidence=p.hardware=false;
            menu.PreviewMiningForCheck(true);yield return Capture("sidebar-water-large");
            menu.Presentation.ScrollDetailsForCheck(350);yield return Capture("sidebar-water-scrolled");
            menu.Presentation.PreviewTheme=null;
            menu.ToggleFromShortcut();Require(!TechTreeMenu.IsOpen&&TechTreeMenu.BlocksInput,"shortcut close consumes frame");
            yield return null;Require(!TechTreeMenu.BlocksInput,"input released");
            var settings=FindFirstObjectByType<SettingsMenu>();settings.Open();
            menu.ToggleFromShortcut();Require(!TechTreeMenu.IsOpen&&SettingsMenu.IsOpen,"shortcut does not steal settings input");
            settings.Cancel();yield return null;
            bool subtitles=SettingsStore.Current.Accessibility.Subtitles;
            float textScale=SettingsStore.Current.Accessibility.TextScale;
            try{
                SettingsStore.Current.Accessibility.Subtitles=true;
                intro.PlayIntro(true);intro.AdvancePlayback(1.5f);yield return Capture("mira-awakening");
                menu.ToggleFromShortcut();Require(!TechTreeMenu.IsOpen,"shortcut blocked during awakening");
                SettingsStore.Current.Accessibility.Subtitles=false;yield return Capture("mira-awakening-no-subtitles");
                intro.Skip();yield return null;
                SettingsStore.Current.Accessibility.Subtitles=true;SettingsStore.Current.Accessibility.TextScale=1.4f;
                scenario.Scanner.Say("ERSTER AUFTRAG · UMGEBUNG SCANNEN","Geh an die Konsole und führe einen Nahbereichsscan aus. Der Scanner ist zu 97 Prozent geladen. In zehn Sekunden ist er bereit. Die Außenansicht ist ein virtueller Raum: Dort siehst du nur, was wir bereits gescannt haben.");
                yield return Capture("mira-quest-large");
                var r=MiraVisorOverlay.Bounds(UiLayout.Width,UiLayout.Height);var point=new Vector3(r.center.x*UiLayout.Scale(),Screen.height-r.center.y*UiLayout.Scale());
                Require(scenario.Scanner.OwnsScreenPoint(point),"visor dialogue blocks world clicks at scaled location");
                scenario.Scanner.DismissDialogue();SettingsStore.Current.Accessibility.TextScale=textScale;
                start.ReturnToStart();yield return null;
                Require(start.Replay(CinematicLibrary.FirstMemories),"memory cinematic starts from menu");
                var memory=start.GetComponent<MemoryCinematic>();
                yield return Capture("mira-memory");
                Require(!string.IsNullOrEmpty(memory.CurrentSubtitle),"cinematic subtitles present");
                menu.ToggleFromShortcut();Require(!TechTreeMenu.IsOpen,"shortcut blocked during cinematic");
                SettingsStore.Current.Accessibility.Subtitles=false;
                Require(string.IsNullOrEmpty(memory.CurrentSubtitle),"cinematic subtitles can be hidden");
                yield return Capture("mira-memory-no-subtitles");memory.Stop();yield return null;
                Require(StartMenu.IsOpen&&!MemoryCinematic.IsPlaying,"memory returns to menu");
            }finally{
                SettingsStore.Current.Accessibility.Subtitles=subtitles;SettingsStore.Current.Accessibility.TextScale=textScale;
            }
            Debug.Log("UNIFIED PRESENTATION CHECK PASSED: "+checks+" checks, 11 trees, fixed sidebar, availability, shortcuts, live water knowledge and three shared Mira dialogue contexts");
            Application.Quit();
        }
    }
}
#endif

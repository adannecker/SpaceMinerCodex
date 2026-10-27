#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    // Actual model, production driver and local recordings. No fabricated successful pose weights.
    public sealed class MiraExpressionCheck : MonoBehaviour
    {
        private const string Folder="Logs/MiraExpressions";
        private MiraDialogueModule module;
        private string host;
        private int checks,pictures;
        private bool failed;
        private int runtimeErrors,emptyAudioWarnings;
        private void OnEnable()=>Application.logMessageReceived+=Observe;
        private void OnDisable()=>Application.logMessageReceived-=Observe;
        private void Observe(string message,string stack,LogType type)
        {
            if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)runtimeErrors++;
            if(message.Contains("resource that is not a clip"))emptyAudioWarnings++;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-miraExpressionCheck")>=0)
                new GameObject("Mira expressive performance acceptance").AddComponent<MiraExpressionCheck>();
        }
        private void Require(bool condition,string message)
        {
            if(!condition)throw new Exception("MIRA EXPRESSION: "+message);
            checks++;
        }
        private bool Step(Action action)
        {
            if(failed)return false;
            try{action();return true;}
            catch(Exception e){failed=true;File.WriteAllText(Folder+"/"+host+"-error.txt",e.ToString());Debug.LogException(e);Application.Quit(1);return false;}
        }
        private float Weight(string name)
        {
            var face=module.Portrait.ActiveAvatar.Face;
            int index=face.sharedMesh.GetBlendShapeIndex(name);
            return index<0?0:face.GetBlendShapeWeight(index);
        }
        private IEnumerator Picture(string label)
        {
            yield return new WaitForEndOfFrame();
            Step(()=>{
                var target=module.Portrait.Texture;var previous=RenderTexture.active;
                var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                try
                {
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                    string file=Folder+"/"+host+"-"+label+".png";File.WriteAllBytes(file,image.EncodeToPNG());
                    Require(new FileInfo(file).Length>10000,"rendered portrait "+label);pictures++;
                }
                finally{RenderTexture.active=previous;Destroy(image);}
            });
        }
        private void Mood(MiraAvatar.Mood mood)
        {
            module.Play(new MiraDialogueLine{Heading="Mimikvergleich",Text="Isolierte Ausdrucksprobe",Mood=mood,Acting=new MiraDialogueActing{MoodIntensity=1}});
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            yield return new WaitForSecondsRealtime(1);
            var demo=FindFirstObjectByType<MiraDialogueDemo>();host=demo!=null?"demo":"game";
            if(!Step(()=>{
                if(demo!=null){demo.SetContinuous(false);module=demo.Dialogue;}
                else
                {
                    CinematicLibrary.PreferencePrefix="SpaceMiner.Test.MiraExpressions.";
                    FindFirstObjectByType<SaveGameMenu>().StorageDirectoryOverride=Path.GetFullPath(Folder+"/Saves");
                    FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();
                    var scanner=FindFirstObjectByType<WaterScenario>().Scanner;
                    scanner.Say(MiraDialogueScript.LoadAwakening().Cues[1]);
                    module=scanner.DialogueModule;
                    // Isolate the pause fixture from the scanner's host-level visibility/pause ownership.
                    scanner.enabled=false;
                }
                module.AutomaticClock=false;module.Portrait.SetMode(MiraVisualMode.HighModel);
                module.Portrait.HeadMotion=module.Portrait.BodyMotion=false;
                Require(module.Portrait.ActiveAvatar.Face.sharedMesh.blendShapeCount==21,"21 high-model expressions");
                Require(module.Portrait.Performance.HasExpressiveRig,"high-specific expressive rig");
                foreach(string shape in new[]{"SmileLeft","SmileRight","CheekRaiseLeft","CheekRaiseRight","EyeSmileLeft","EyeSmileRight","BrowLiftLeft","BrowLiftRight"})
                {
                    var mesh=module.Portrait.ActiveAvatar.Face.sharedMesh;
                    int index=mesh.GetBlendShapeIndex(shape);Require(index>=0,"shape "+shape);
                    var delta=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(index,0,delta,null,null);
                    var vertices=mesh.vertices;int affected=0;float maximum=0;bool clothingPreserved=true;
                    for(int i=0;i<delta.Length;i++)
                    {
                        maximum=Mathf.Max(maximum,delta[i].magnitude);
                        if(vertices[i].y<.15f&&delta[i].sqrMagnitude>=1e-12f)clothingPreserved=false;
                        if(delta[i].sqrMagnitude>1e-10f)affected++;
                    }
                    Require(affected>10&&maximum>.0005f&&maximum<.008f,"bounded useful deformation "+shape);
                    Require(clothingPreserved,"clothing preserved "+shape);
                }
                module.Portrait.ExpressionCoupling=0;Mood(MiraAvatar.Mood.Encouraging);
            }))yield break;
            yield return new WaitForSecondsRealtime(1.6f);
            if(!Step(()=>Require(Weight("CheekRaiseLeft")<.1f&&Weight("EyeSmileLeft")<.1f,"comparison zero removes all added expression")))yield break;
            yield return Picture("01-baseline");
            module.Portrait.ExpressionCoupling=1;
            yield return new WaitForSecondsRealtime(1.6f);
            if(!Step(()=>{
                Require(Weight("CheekRaiseLeft")>15&&Weight("CheekRaiseRight")>15,"warm smile raises both cheeks");
                Require(Weight("EyeSmileLeft")>5&&Weight("EyeSmileRight")>5,"warm smile engages lower eyelids");
                Require(Weight("BrowLiftLeft")>5,"expressive brows");
                Require(Mathf.Abs(Weight("SmileLeft")-Weight("SmileRight"))>.5f,"restrained smile asymmetry");
            }))yield break;
            yield return Picture("02-encouraging");
            foreach(var mood in new[]{MiraAvatar.Mood.Friendly,MiraAvatar.Mood.Concerned,MiraAvatar.Mood.Focused})
            {
                Mood(mood);yield return new WaitForSecondsRealtime(1.7f);
                if(!Step(()=>{
                    if(mood==MiraAvatar.Mood.Concerned)Require(Weight("BrowConcern")>40&&Weight("CheekRaiseLeft")<3,"concern replaces warm smile");
                    if(mood==MiraAvatar.Mood.Focused)Require(Weight("BrowFocus")>35&&Weight("SmileLeft")<2,"focus stays serious");
                    if(mood==MiraAvatar.Mood.Friendly)Require(Weight("CheekRaiseLeft")>10,"friendly coupled expression");
                }))yield break;
                yield return Picture("03-"+mood);
            }
            // Select the longest real rounded-lip interval, rather than synthesizing a test viseme.
            var script=MiraDialogueScript.LoadAwakening();MiraDialogueLine roundedLine=null;MiraFacePerformance.MouthCue rounded=null;
            foreach(var line in script.Cues)
            {
                var clip=Resources.Load<AudioClip>(line.Audio);var timing=module.Portrait.Performance.GetTiming(clip);
                if(timing?.mouthCues==null)continue;
                foreach(var cue in timing.mouthCues)if(cue.value=="F"&&(rounded==null||cue.end-cue.start>rounded.end-rounded.start)){rounded=cue;roundedLine=line;}
            }
            if(!Step(()=>{
                Require(rounded!=null&&rounded.end-rounded.start>.1f,"real usable rounded phoneme");
                module.Play(new MiraDialogueLine{Heading=roundedLine.Heading,Text=roundedLine.Text,Audio=roundedLine.Audio,Mood=MiraAvatar.Mood.Encouraging,Acting=new MiraDialogueActing{MoodIntensity=1}});
                module.Speech.time=rounded.start+.005f;
            }))yield break;
            yield return new WaitForSecondsRealtime(Mathf.Min(.14f,(rounded.end-rounded.start)*.65f));
            if(!Step(()=>Require(Weight("Round")>55&&Weight("Smile")<35,"rounded speech takes priority over smiling")))yield break;
            yield return Picture("04-rounded-speech");
            module.SetPaused(true);float at=module.Speech.time;
            yield return new WaitForSecondsRealtime(.5f);
            if(!Step(()=>Require(Mathf.Abs(module.Speech.time-at)<.02f&&Weight("JawOpen")<1,"pause preserves recording and rests mouth")))yield break;
            module.Portrait.Expressions=false;
            yield return new WaitForSecondsRealtime(1.7f);
            if(!Step(()=>Require(Weight("SmileLeft")<1&&Weight("CheekRaiseLeft")<1&&Weight("BrowLiftLeft")<1,"mimic control releases the complete expression")))yield break;
            module.Portrait.EyeMotion=false;
            yield return null;yield return null;
            if(!Step(()=>Require(Weight("EyeSmileLeft")==0&&module.Portrait.Performance.Blink==0,"eye control restores lids")))yield break;
            module.Portrait.SetMode(MiraVisualMode.LowModel);yield return null;yield return null;
            if(!Step(()=>Require(!module.Portrait.Performance.HasExpressiveRig&&module.Portrait.ActiveAvatar.Face.sharedMesh.blendShapeCount==13,"low model preserved")))yield break;
            if(!Step(()=>Require(runtimeErrors==0&&emptyAudioWarnings==0,"runtime and text-only audio clock stay clean")))yield break;
            File.WriteAllText(Folder+"/"+host+"-result.json","{\"passed\":true,\"checks\":"+checks+",\"pictures\":"+pictures+"}");
            Application.Quit(0);
        }
    }
}
#endif

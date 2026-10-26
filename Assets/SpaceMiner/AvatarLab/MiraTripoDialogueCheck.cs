using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiraTripoDialogueCheck : MonoBehaviour
    {
        private int checks, audioFrames, shapeFrames;
        private float maximumMouth, maximumWeight;
        private int timedClips;
        private float maximumHead,maximumGaze,maximumRound,maximumWide,maximumSmile,maximumConcern;
        private string Folder => Path.Combine(Environment.CurrentDirectory,"Logs/MiraTripoMotion");
        private IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-miraTripoCheck")<0)yield break;
            Directory.CreateDirectory(Folder);
            string result=Path.Combine(Folder,"result.json");if(File.Exists(result))File.Delete(result);
            var run=Run();
            while(true)
            {
                bool next;object current;
                try{next=run.MoveNext();current=next?run.Current:null;}
                catch(Exception error){Debug.LogException(error);Debug.LogError("MIRA TRIPO CHECK FAILED");Application.Quit(1);yield break;}
                if(!next)yield break;
                yield return current;
            }
        }
        private IEnumerator Run()
        {
            var preview=GetComponent<MiraDialoguePreview>();var avatar=GetComponent<MiraAvatar3D>();var driver=GetComponent<MiraAvatar>();
            float deadline=Time.realtimeSinceStartup+20;
            while(preview.CueCount==0 && Time.realtimeSinceStartup<deadline)yield return null;
            Require(preview.CueCount==16,"all sixteen original dialogue cues loaded");
            Require(preview.TripoPrototype && preview.Avatar3D==avatar,"new Tripo portrait bound to dialogue");
            Require(FindFirstObjectByType<WaterScenario>()==null && FindFirstObjectByType<RuinedWorld>()==null,"isolated avatar scene");
            Require(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"exactly one audio listener for actual speech output");
            Require(avatar.Face!=null && avatar.Face.sharedMesh.vertexCount>60000,"actual imported Tripo geometry");
            Require(avatar.Face.sharedMaterial.mainTexture!=null && avatar.Face.sharedMaterial.GetTexture("_BumpMap")!=null,"PBR color and normal textures assigned");
            int jaw=avatar.Face.sharedMesh.GetBlendShapeIndex("JawOpen");Require(jaw>=0,"prototype speech shape present");
            var mesh=avatar.Face.sharedMesh;var delta=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(jaw,0,delta,null,null);
            var vertices=mesh.vertices;float maximumDelta=0;int affected=0;
            for(int i=0;i<delta.Length;i++)
            {
                maximumDelta=Mathf.Max(maximumDelta,delta[i].magnitude);
                if(delta[i].sqrMagnitude>1e-10f){affected++;RequireFinite(delta[i]);if(vertices[i].y>.31f || vertices[i].y<.205f)throw new Exception("Speech deformation outside lower face");}
            }
            Require(affected>20 && maximumDelta<=.009f,"small finite deformation confined to lower face");
            Require(Mathf.Abs(avatar.JawWeight-100)<.01f,"Tripo strength explicitly configured");
            var performance=GetComponent<MiraFacePerformance>();
            Require(performance!=null && performance.Head!=null && avatar.EnhancedFace,"local head and facial performance enabled");
            CheckRigAndShapes(avatar,performance);
            bool capturedSpeech=false;int spoken=0,textOnly=0;
            for(int cue=0;cue<16;cue++)
            {
                preview.Play(cue);yield return null;
                if(preview.TextOnly)
                {
                    Require(cue==12 || cue==13,"only deliberately unvoiced scanner instructions use text");textOnly++;
                    float reading=preview.ReadingDuration;float textStart=Time.realtimeSinceStartup;
                    if(cue==12){ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"04-text-only-instruction.png"));yield return new WaitForEndOfFrame();yield return null;}
                    while(Time.realtimeSinceStartup-textStart<reading*.35f && preview.CurrentCue==cue)yield return null;
                    Require(preview.CurrentCue==cue,"unvoiced instruction remains for reading time");
                    float textDeadline=textStart+reading+4;
                    while(preview.CurrentCue==cue && Time.realtimeSinceStartup<textDeadline)yield return null;
                    Require(preview.CurrentCue==cue+1,"unvoiced instruction advances automatically");continue;
                }
                spoken++;
                Require(preview.Speech.clip!=null && preview.Speech.clip.samples>0,"voice clip "+cue+" available");
                var timing=performance.GetTiming(preview.Speech.clip);
                Require(timing?.mouthCues?.Length>1 && timing.audioSha256?.Length==64,"offline timing with source hash for voice "+cue);
                float end=0;
                foreach(var mouthCue in timing.mouthCues)
                {
                    Require(Mathf.Abs(mouthCue.start-end)<.001f && mouthCue.end>mouthCue.start && mouthCue.value.Length==1 && "ABCDEFGHX".Contains(mouthCue.value),"valid contiguous mouth timing");
                    end=mouthCue.end;
                }
                Require(Mathf.Abs(end-preview.Speech.clip.length)<.015f,"mouth timing covers actual clip duration");timedClips++;
                Require(preview.CurrentCue==cue && preview.Speech.isPlaying,"voice clip "+cue+" started");
                Debug.Log("MIRA TRIPO CUE "+cue+" / 16");
                float clipDeadline=Time.realtimeSinceStartup+preview.Speech.clip.length+8;
                bool pausedOnce=false;
                while(preview.CurrentCue==cue && preview.Speech.isPlaying && Time.realtimeSinceStartup<clipDeadline)
                {
                    audioFrames++;maximumMouth=Mathf.Max(maximumMouth,driver.Mouth);
                    float weight=avatar.Face.GetBlendShapeWeight(jaw);maximumWeight=Mathf.Max(maximumWeight,weight);
                    if(weight>.1f)shapeFrames++;
                    maximumHead=Mathf.Max(maximumHead,performance.HeadAngles.magnitude);
                    maximumGaze=Mathf.Max(maximumGaze,Weight(avatar,"LookLeft"),Weight(avatar,"LookRight"));
                    maximumRound=Mathf.Max(maximumRound,Weight(avatar,"Round"));maximumWide=Mathf.Max(maximumWide,Weight(avatar,"Wide"));
                    maximumSmile=Mathf.Max(maximumSmile,Weight(avatar,"Smile"));maximumConcern=Mathf.Max(maximumConcern,Weight(avatar,"BrowConcern"));
                    if(!capturedSpeech && driver.Mouth>.12f)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"01-speaking.png"));capturedSpeech=true;
                    }
                    if(cue==0 && !pausedOnce && preview.Speech.time>.6f)
                    {
                        preview.Pause();float held=preview.Speech.time;yield return new WaitForSecondsRealtime(.6f);
                        Require(preview.IsPaused && !preview.Speech.isPlaying,"pause stops voice");
                        Require(Mathf.Abs(preview.Speech.time-held)<.02f,"pause keeps audio position");
                        Require(driver.Mouth<.02f && avatar.Face.GetBlendShapeWeight(jaw)<2f,"pause returns mouth to resting shape");
                        Require(performance.CurrentViseme=="X","pause releases spoken viseme without advancing clip time");
                        ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"02-paused.png"));
                        yield return new WaitForEndOfFrame();yield return null;
                        preview.Pause();pausedOnce=true;
                        Require(!preview.IsPaused && preview.Speech.isPlaying,"resume continues conversation");
                    }
                    yield return null;
                }
                Require(Time.realtimeSinceStartup<clipDeadline,"voice clip "+cue+" completed without timeout");
            }
            preview.Speech.Stop();yield return new WaitForSecondsRealtime(.6f);
            Require(spoken==16 && textOnly==0,"current dialogue has sixteen recorded lines");
            Require(audioFrames>100 && shapeFrames>20 && maximumMouth>.07f && maximumWeight>2,"voice produces actual mesh animation");
            Require(driver.Mouth<.02f && avatar.Face.GetBlendShapeWeight(jaw)<2,"silence restores resting mouth");
            Require(timedClips==14 && performance.VisemeChanges>100 && maximumRound>30 && maximumWide>15,"recorded timing drives distinct rounded and wide speech shapes");
            Require(performance.Blinks>5 && performance.GazeChanges>5 && maximumGaze>10,"actual blinks and gaze shifts during conversation");
            Require(maximumHead>.5f && maximumHead<5 && maximumSmile>15 && maximumConcern>30,"subtle head movement and dialogue-dependent expressions");
            preview.Play(0);Require(preview.CurrentCue==0 && preview.Speech.isPlaying,"restart begins first cue");
            preview.Play(1);Require(preview.CurrentCue==1,"next dialogue selectable");preview.Play(0);Require(preview.CurrentCue==0,"previous dialogue selectable");
            preview.Speech.Stop();yield return new WaitForSecondsRealtime(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"03-resting.png"));yield return new WaitForEndOfFrame();yield return null;
            driver.Motion=false;performance.EyeMotion=false;performance.Expressions=false;
            yield return new WaitForSecondsRealtime(1.8f);
            Require(performance.HeadAngles.magnitude<.05f && performance.Blink==0 && Weight(avatar,"LookLeft")<1 && Weight(avatar,"LookRight")<1,"movement controls restore steady portrait");
            Require(Weight(avatar,"Smile")<1 && Weight(avatar,"BrowConcern")<1 && Weight(avatar,"BrowFocus")<1,"expression toggle returns to base expression");
            performance.DiagnosticPose=true;Set(avatar,"BlinkLeft",100);Set(avatar,"BlinkRight",100);
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"05-blinking.png"));yield return new WaitForEndOfFrame();yield return null;
            File.WriteAllText(Path.Combine(Folder,"result.json"),JsonUtility.ToJson(new MotionResult{checks=checks,spokenCues=spoken,textOnlyCues=textOnly,audioFrames=audioFrames,shapeFrames=shapeFrames,vertices=mesh.vertexCount,affectedVertices=affected,maximumShapeDeltaMeters=maximumDelta,maximumMouth=maximumMouth,maximumWeight=maximumWeight,timedClips=timedClips,visemeChanges=performance.VisemeChanges,blinks=performance.Blinks,gazeChanges=performance.GazeChanges,maximumHeadDegrees=maximumHead,maximumGazeWeight=maximumGaze,maximumRoundWeight=maximumRound,maximumWideWeight=maximumWide},true));
            Debug.Log("MIRA TRIPO CHECK PASSED: "+checks+" checks, 16 voice clips");Application.Quit(0);
        }
        private void Require(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
        private static float Weight(MiraAvatar3D avatar,string shape)=>avatar.Face.GetBlendShapeWeight(avatar.Face.sharedMesh.GetBlendShapeIndex(shape));
        private static void Set(MiraAvatar3D avatar,string shape,float weight)=>avatar.Face.SetBlendShapeWeight(avatar.Face.sharedMesh.GetBlendShapeIndex(shape),weight);
        private void CheckRigAndShapes(MiraAvatar3D avatar,MiraFacePerformance performance)
        {
            var mesh=avatar.Face.sharedMesh;var vertices=mesh.vertices;
            Require(mesh.blendShapeCount==13 && avatar.Face.bones.Length==2 && mesh.bindposes.Length==2,"thirteen shapes and two-bone bust rig");
            var deformation=new Vector3[vertices.Length];
            for(int shape=0;shape<mesh.blendShapeCount;shape++)
            {
                mesh.GetBlendShapeFrameVertices(shape,0,deformation,null,null);
                int active=0;
                for(int i=0;i<deformation.Length;i++)
                {
                    RequireFinite(deformation[i]);
                    if(deformation[i].sqrMagnitude<1e-10f)continue;
                    active++;
                    if(vertices[i].y<.20f || vertices[i].y>.49f || deformation[i].magnitude>.03f)throw new Exception("Expression moved outside bounded face region: "+mesh.GetBlendShapeName(shape));
                }
                Require(active>10,"nonempty bounded expression "+mesh.GetBlendShapeName(shape));
            }
            var original=performance.Head.localRotation;
            performance.Head.localRotation=Quaternion.identity;
            var baseline=new Mesh();avatar.Face.BakeMesh(baseline);var a=baseline.vertices;
            performance.Head.localRotation=Quaternion.Euler(2,6,1);
            var moved=new Mesh();avatar.Face.BakeMesh(moved);var b=moved.vertices;
            float shoulders=0,head=0;
            for(int i=0;i<vertices.Length;i++)
            {
                float distance=(a[i]-b[i]).magnitude;
                if(vertices[i].y<.2f)shoulders=Mathf.Max(shoulders,distance);
                if(vertices[i].y>.35f)head=Mathf.Max(head,distance);
            }
            performance.Head.localRotation=original;Destroy(baseline);Destroy(moved);
            Require(shoulders<.00001f && head>.008f && head<.06f,"head pivots on neck while shoulders remain fixed");
        }
        [Serializable] private sealed class MotionResult
        {
            public bool passed=true,isolatedScene=true,phoneticLipSync=true;
            public int checks,dialogueCues=16,spokenCues,textOnlyCues,audioFrames,shapeFrames,vertices,affectedVertices,timedClips,visemeChanges,blinks,gazeChanges;
            public float maximumShapeDeltaMeters,maximumMouth,maximumWeight,maximumHeadDegrees,maximumGazeWeight,maximumRoundWeight,maximumWideWeight;
        }
        private static void RequireFinite(Vector3 value){if(float.IsNaN(value.x)||float.IsNaN(value.y)||float.IsNaN(value.z)||float.IsInfinity(value.sqrMagnitude))throw new Exception("Non-finite shape");}
    }
}

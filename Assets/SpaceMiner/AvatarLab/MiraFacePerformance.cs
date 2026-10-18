using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Opt-in for the Tripo prototype. Old 2D / anatomical avatar demos keep their driver.
    [DefaultExecutionOrder(150)]
    public sealed class MiraFacePerformance : MonoBehaviour
    {
        [Serializable] public sealed class MouthCue { public float start,end; public string value; }
        [Serializable] public sealed class Timing { public MouthCue[] mouthCues; public string audioSha256; public float duration; }
        public MiraAvatar3D Avatar;
        public Transform Head;
        public bool EyeMotion=true, Expressions=true;
        public int Blinks { get; private set; }
        public int GazeChanges { get; private set; }
        public int VisemeChanges { get; private set; }
        public bool HasTiming { get; private set; }
        public string CurrentViseme { get; private set; }="X";
        public Vector3 HeadAngles { get; private set; }
        public float Blink { get; private set; }
        private readonly Dictionary<string,Timing> timings=new Dictionary<string,Timing>();
        private readonly Dictionary<string,int> shapes=new Dictionary<string,int>();
        private readonly System.Random random=new System.Random(1847);
        private float nextBlink=2.6f,blinkAt=-10,nextGaze=1.2f,nextHead=1.7f;
        private Vector2 gaze,targetGaze;
        private Vector3 headTarget;
        private float jaw,round,wide,close,smile,concern,focus;
        // Used only by the deterministic pose inspection in the editor/test player.
        internal bool DiagnosticPose;

        private float Range(float a,float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
        private void Awake()
        {
            if(Avatar==null || Avatar.Face==null)return;
            for(int i=0;i<Avatar.Face.sharedMesh.blendShapeCount;i++)shapes[Avatar.Face.sharedMesh.GetBlendShapeName(i)]=i;
        }
        public Timing GetTiming(AudioClip clip)
        {
            if(clip==null)return null;
            if(!timings.TryGetValue(clip.name,out var timing))
            {
                var file=Resources.Load<TextAsset>("MiraVisemes/"+clip.name);
                timing=file!=null?JsonUtility.FromJson<Timing>(file.text):null;
                if(timing!=null && Mathf.Abs(timing.duration-clip.length)>.03f)
                {
                    Debug.LogWarning("Mira timing duration differs from voice; regenerate MiraVisemes for "+clip.name);
                    timing=null;
                }
                timings.Add(clip.name,timing);
            }
            return timing;
        }
        public static string CueAt(Timing timing,float seconds)
        {
            if(timing?.mouthCues==null)return "X";
            // Audio position, rather than a scene clock, keeps pause/replay/seek synchronized.
            int low=0,high=timing.mouthCues.Length-1;
            while(low<=high)
            {
                int mid=(low+high)/2;var cue=timing.mouthCues[mid];
                if(seconds<cue.start)high=mid-1;else if(seconds>=cue.end)low=mid+1;else return cue.value;
            }
            return "X";
        }
        private void LateUpdate()
        {
            if(Avatar==null || Avatar.Driver==null || Avatar.Face==null || DiagnosticPose)return;
            var driver=Avatar.Driver;var speech=driver.Speech;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f),time=Time.unscaledTime;
            bool speaking=speech!=null && speech.isPlaying;
            var timing=GetTiming(speech!=null?speech.clip:null);HasTiming=timing?.mouthCues?.Length>0;
            string viseme=speaking && HasTiming?CueAt(timing,speech.time):"X";
            if(viseme!=CurrentViseme){CurrentViseme=viseme;VisemeChanges++;}
            float targetJaw=0,targetRound=0,targetWide=0,targetClose=.9f;
            if(speaking && HasTiming)
            {
                targetClose=0;
                switch(viseme)
                {
                    case "A": targetClose=1;break;
                    case "B": targetJaw=.16f;targetWide=.35f;break;
                    case "C": targetJaw=.55f;targetWide=.2f;break;
                    case "D": targetJaw=.95f;targetWide=.1f;break;
                    case "E": targetJaw=.40f;targetRound=.60f;break;
                    case "F": targetJaw=.22f;targetRound=1;break;
                    case "G": targetJaw=.12f;targetClose=.2f;break;
                    case "H": targetJaw=.50f;break;
                    default: targetClose=.9f;break;
                }
                float strength=Mathf.Clamp(driver.Sensitivity/12f,.4f,1.4f);
                targetJaw=Mathf.Min(1,targetJaw*strength);
                targetRound=Mathf.Min(1,targetRound*strength);targetWide=Mathf.Min(1,targetWide*strength);
            }
            else if(speaking) {targetJaw=driver.Mouth;targetClose=0;} // New recordings without offline timing still work.
            float blend=1-Mathf.Exp(-dt*25);
            jaw=Mathf.Lerp(jaw,targetJaw,blend);round=Mathf.Lerp(round,targetRound,blend);
            wide=Mathf.Lerp(wide,targetWide,blend);close=Mathf.Lerp(close,targetClose,blend);
            Set("JawOpen",jaw*Avatar.JawWeight);Set("Round",round*100);Set("Wide",wide*100);Set("LipClose",close*100);
            float desiredSmile=driver.CurrentMood==MiraAvatar.Mood.Encouraging?.48f:driver.CurrentMood==MiraAvatar.Mood.Friendly?.20f:0;
            smile=Mathf.Lerp(smile,Expressions?desiredSmile:0,1-Mathf.Exp(-dt*3));
            concern=Mathf.Lerp(concern,Expressions && driver.CurrentMood==MiraAvatar.Mood.Concerned?.6f:0,1-Mathf.Exp(-dt*3));
            focus=Mathf.Lerp(focus,Expressions && driver.CurrentMood==MiraAvatar.Mood.Focused?.5f:0,1-Mathf.Exp(-dt*3));
            Set("Smile",smile*100);Set("BrowConcern",concern*100);Set("BrowFocus",focus*100);
            if(time>=nextBlink){blinkAt=time;nextBlink=time+Range(2.8f,6.2f);if(EyeMotion)Blinks++;}
            float phase=time-blinkAt;
            Blink=EyeMotion?(phase<.085f?Mathf.SmoothStep(0,1,phase/.085f):1-Mathf.SmoothStep(0,1,(phase-.085f)/.15f)):0;
            Set("BlinkLeft",Blink*100);Set("BlinkRight",Blink*98);
            if(time>=nextGaze)
            {
                targetGaze=random.NextDouble()<.65?Vector2.zero:new Vector2(Range(-.65f,.65f),Range(-.35f,.35f));
                nextGaze=time+Range(1.5f,3.8f);if(EyeMotion)GazeChanges++;
            }
            gaze=Vector2.Lerp(gaze,EyeMotion?targetGaze:Vector2.zero,1-Mathf.Exp(-dt*18));
            float eyes=1-Blink;
            Set("LookLeft",Mathf.Max(0,gaze.x)*100*eyes);Set("LookRight",Mathf.Max(0,-gaze.x)*100*eyes);
            Set("LookUp",Mathf.Max(0,gaze.y)*100*eyes);Set("LookDown",Mathf.Max(0,-gaze.y)*100*eyes);
            if(time>=nextHead){headTarget=new Vector3(Range(-.65f,.65f),Range(-2,2),Range(-.7f,.7f));nextHead=time+Range(2.7f,5.5f);}
            var angles=headTarget+new Vector3((speaking?jaw*.9f:0)+Mathf.Sin(time*.9f)*.20f,gaze.x*.5f,Mathf.Sin(time*.55f)*.12f);
            HeadAngles=Vector3.Lerp(HeadAngles,driver.Motion?angles:Vector3.zero,1-Mathf.Exp(-dt*2.8f));
            if(Head!=null)Head.localRotation=Quaternion.Euler(HeadAngles);
        }
        private void Set(string shape,float weight)
        { if(shapes.TryGetValue(shape,out int index))Avatar.Face.SetBlendShapeWeight(index,Mathf.Clamp(weight,0,100)); }
    }
}

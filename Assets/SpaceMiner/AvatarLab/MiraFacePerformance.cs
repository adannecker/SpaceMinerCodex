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
        public Transform Torso, LeftShoulder, RightShoulder;
        public MiraDialogueActing Acting;
        public bool BodyMotion = true;
        public float MovementStrength = 1;
        public Vector3 TorsoAngles { get; private set; }
        public float ShoulderLift { get; private set; }
        public bool EyeMotion=true, Expressions=true;
        public float ExpressionCoupling=1;
        public bool HasExpressiveRig => shapes.ContainsKey("CheekRaiseLeft");
        public float CheekActivation {get;private set;}
        public float EyeSmileActivation {get;private set;}
        public float BrowLiftActivation {get;private set;}
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
        private float jaw,round,wide,close,smile,concern,focus,expressionAccent;
        private Vector3 torsoRest, leftRest, rightRest;
        private float actingWeight;
        // Used only by the deterministic pose inspection in the editor/test player.
        internal bool DiagnosticPose;

        private float Range(float a,float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
        private void Awake()
        {
            if(Avatar==null || Avatar.Face==null)return;
            if(Torso!=null)torsoRest=Torso.localPosition;
            if(LeftShoulder!=null)leftRest=LeftShoulder.localPosition;
            if(RightShoulder!=null)rightRest=RightShoulder.localPosition;
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
            float emotion=Acting!=null?Mathf.Clamp01(Acting.MoodIntensity):1;
            float natural=HasExpressiveRig?Mathf.Clamp01(ExpressionCoupling):0;
            float phraseTime=speech!=null&&speech.clip!=null?speech.time:0;
            float cadence=speaking?.5f+.5f*Mathf.Sin(phraseTime*2.1f):.25f;
            expressionAccent=Mathf.Lerp(expressionAccent,speaking?driver.Mouth:0,1-Mathf.Exp(-dt*5));
            float desiredSmile=(driver.CurrentMood==MiraAvatar.Mood.Encouraging?Mathf.Lerp(.48f,.66f,natural):
                driver.CurrentMood==MiraAvatar.Mood.Friendly?Mathf.Lerp(.20f,.36f,natural):0)*emotion;
            smile=Mathf.Lerp(smile,Expressions?desiredSmile:0,1-Mathf.Exp(-dt*3));
            concern=Mathf.Lerp(concern,Expressions && driver.CurrentMood==MiraAvatar.Mood.Concerned?.6f*emotion:0,1-Mathf.Exp(-dt*3));
            focus=Mathf.Lerp(focus,Expressions && driver.CurrentMood==MiraAvatar.Mood.Focused?.5f*emotion:0,1-Mathf.Exp(-dt*3));
            // Rounding and bilabial closures take precedence over expressive mouth corners.
            float smileGate=1-natural*Mathf.Clamp01(round*.78f+(speaking?close*.55f:0));
            Set("Smile",smile*100*smileGate);Set("BrowConcern",concern*100);Set("BrowFocus",focus*100);
            if(time>=nextBlink){blinkAt=time;nextBlink=time+Range(2.8f,6.2f);if(EyeMotion)Blinks++;}
            float phase=time-blinkAt;
            Blink=EyeMotion?(phase<.085f?Mathf.SmoothStep(0,1,phase/.085f):1-Mathf.SmoothStep(0,1,(phase-.085f)/.15f)):0;
            Set("BlinkLeft",Blink*100);Set("BlinkRight",Blink*98);
            float asymmetry=Mathf.Sin(phraseTime*.73f+.4f)*.10f;
            CheekActivation=smile*natural*(.70f+cadence*.12f);
            EyeSmileActivation=EyeMotion?CheekActivation*.65f*(1-Blink):0;
            BrowLiftActivation=Expressions?natural*Mathf.Clamp01(smile*.36f+concern*.12f+expressionAccent*.30f):0;
            Set("SmileLeft",smile*natural*smileGate*(.78f+asymmetry)*100);
            Set("SmileRight",smile*natural*smileGate*(.78f-asymmetry)*100);
            Set("CheekRaiseLeft",CheekActivation*(1+asymmetry)*100);
            Set("CheekRaiseRight",CheekActivation*(1-asymmetry)*100);
            Set("EyeSmileLeft",EyeSmileActivation*(1+asymmetry)*100);
            Set("EyeSmileRight",EyeSmileActivation*(1-asymmetry)*100);
            Set("BrowLiftLeft",BrowLiftActivation*(.85f+cadence*.2f)*100);
            Set("BrowLiftRight",BrowLiftActivation*(.7f+cadence*.15f)*100);
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
            actingWeight=Mathf.Lerp(actingWeight,Acting!=null?1:0,1-Mathf.Exp(-dt*3));
            Vector3 gesture=Vector3.zero; float shoulder=0;
            if(Acting?.Gestures!=null && speech!=null && speech.clip!=null)
                foreach(var cue in Acting.Gestures)
                {
                    float weight=cue.Weight(speech.time);
                    gesture+=cue.Head*weight;shoulder+=cue.Shoulder*weight;
                }
            // Authored head gestures follow recording time, independent of jaw amplitude.
            // The small gaze contribution settles later than the eye movement.
            var posture=driver.CurrentMood==MiraAvatar.Mood.Friendly?new Vector3(-.6f,0,1.2f):
                driver.CurrentMood==MiraAvatar.Mood.Concerned?new Vector3(1.1f,0,-.8f):
                driver.CurrentMood==MiraAvatar.Mood.Focused?new Vector3(1.5f,0,0):new Vector3(-.8f,0,-1);
            var acted=gesture+(Expressions?posture*emotion:Vector3.zero)+headTarget*.25f+new Vector3(0,gaze.x*2,0);
            // A small phrase accent complements the authored gestures; it never drives the head from each viseme.
            if(Expressions)acted+=natural*new Vector3(expressionAccent*.45f,0,smile*cadence*.35f);
            angles=Vector3.Lerp(angles,acted,actingWeight)*Mathf.Clamp(MovementStrength,0,1.5f);
            HeadAngles=Vector3.Lerp(HeadAngles,driver.Motion?angles:Vector3.zero,1-Mathf.Exp(-dt*2.8f));
            if(Head!=null)Head.localRotation=Quaternion.Euler(HeadAngles);
            UpdateBody(dt,time,emotion,shoulder,driver.CurrentMood);
        }
        private void UpdateBody(float dt,float time,float emotion,float gesture,MiraAvatar.Mood mood)
        {
            float strength=BodyMotion?actingWeight*Mathf.Clamp(MovementStrength,0,1.5f):0;
            float breathing=Mathf.Sin(time*1.25f);
            float tension=Expressions && mood==MiraAvatar.Mood.Concerned?.7f*emotion:
                Expressions && (mood==MiraAvatar.Mood.Friendly || mood==MiraAvatar.Mood.Encouraging)?-.3f*emotion:0;
            float lean=Expressions && mood==MiraAvatar.Mood.Focused?.7f*emotion:-.15f;
            var target=new Vector3(lean+breathing*.17f,Mathf.Sin(time*.31f)*.28f,Mathf.Sin(time*.43f)*.25f)*strength;
            float blend=1-Mathf.Exp(-dt*3);
            TorsoAngles=Vector3.Lerp(TorsoAngles,target,blend);
            ShoulderLift=Mathf.Lerp(ShoulderLift,(breathing*.0006f+(tension+gesture)*.0015f)*strength,blend);
            if(Torso!=null)
            {
                Torso.localRotation=Quaternion.Euler(TorsoAngles);
                Torso.localPosition=torsoRest+Vector3.up*(strength*breathing*.00035f);
            }
            if(LeftShoulder!=null)
            {
                LeftShoulder.localPosition=leftRest+Vector3.up*ShoulderLift;
                LeftShoulder.localRotation=Quaternion.Euler(0,0,-ShoulderLift*450);
            }
            if(RightShoulder!=null)
            {
                RightShoulder.localPosition=rightRest+Vector3.up*(ShoulderLift*.8f);
                RightShoulder.localRotation=Quaternion.Euler(0,0,ShoulderLift*380);
            }
        }
        private void Set(string shape,float weight)
        { if(shapes.TryGetValue(shape,out int index))Avatar.Face.SetBlendShapeWeight(index,Mathf.Clamp(weight,0,100)); }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        // Prototype allocation within the existing 180 W systems budget, not a fixture specification.
        public const float NonLightingSystemsKw=.09f, LightingBudgetKw=.09f, IdleLightingLevel=.12f;
        private readonly Dictionary<Light,float> lampRatings=new Dictionary<Light,float>();
        private readonly Dictionary<Light,Renderer> lampFaces=new Dictionary<Light,Renderer>();
        private readonly Dictionary<string,float> roomVisited=new Dictionary<string,float>();
        private float lastLightingTime;
        private MaterialPropertyBlock lampProperties;
        public string CurrentLightingZone {get;private set;}="outside";
        public float LightingDemandKw {get;private set;}
        internal float LightingLevel(string zone)
        {
            float level=0;int count=0;foreach(var lamp in lamps)if(LampZone(lamp)==zone){level+=lamp.intensity/lampRatings[lamp];count++;}
            return count>0?level/count:0;
        }
        internal static string LightingZone(Vector3 p)
        {
            float r=new Vector2(p.x,p.z).magnitude;
            int sector=(Mathf.RoundToInt(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg/30)+12)%12;
            if(r>=37.6f){if(sector==0)return "command";if(sector==1)return r<54?"workshop":"hangar";return "outer-"+sector;}
            if(r>=29.4f)return "ring-"+sector;
            if(r<8)return "reactor-service";
            return "inner-"+sector;
        }
        private string LampZone(Light lamp)=>LightingZone(Layout.transform.InverseTransformPoint(lamp.transform.position));
        private void UpdateAdaptiveLighting(Vector3 renderEye)
        {
            float now=Time.unscaledTime,dt=Mathf.Clamp(now-lastLightingTime,0,.5f);lastLightingTime=now;
            var mode=StationInteriorMode.Current;
            CurrentLightingZone=mode!=null&&StationInteriorMode.IsInside?LightingZone(Layout.transform.InverseTransformPoint(mode.FeetPosition)):"outside";
            if(CurrentLightingZone!="outside")roomVisited[CurrentLightingZone]=now;
            float total=0;
            foreach(var lamp in lamps){
                string zone=LampZone(lamp);
                bool occupied=zone==CurrentLightingZone||roomVisited.TryGetValue(zone,out float visited)&&now-visited<1.5f;
                float target=occupied?1:IdleLightingLevel;
                float level=lampRatings[lamp]>0?lamp.intensity/lampRatings[lamp]:0;
                level=Mathf.MoveTowards(level,target,dt*(occupied?1.7f:.55f));lamp.intensity=lampRatings[lamp]*level;
                if(Powered(lampSectors[lamp]))total+=level;
                if(lampFaces.TryGetValue(lamp,out var face)&&face!=null){
                    bool supplied=Powered(lampSectors[lamp])&&State.power.LifePowered;
                    if(lampProperties==null)lampProperties=new MaterialPropertyBlock();lampProperties.Clear();var color=face.sharedMaterial.color;
                    lampProperties.SetColor("_Color",color*(supplied?Mathf.Lerp(.12f,1,level):.04f));
                    lampProperties.SetColor("_EmissionColor",supplied?color*1.7f*level:Color.black);face.SetPropertyBlock(lampProperties);
                }
            }
            LightingDemandKw=lamps.Count>0?LightingBudgetKw*total/lamps.Count:0;
            lamps.Sort((a,b)=>{
                int active=(LampZone(b)==CurrentLightingZone?1:0).CompareTo(LampZone(a)==CurrentLightingZone?1:0);
                return active!=0?active:(a.transform.position-renderEye).sqrMagnitude.CompareTo((b.transform.position-renderEye).sqrMagnitude);
            });
            int count=0;
            foreach(var lamp in lamps){
                bool active=LampZone(lamp)==CurrentLightingZone;
                lamp.enabled=(lamp.transform.position-renderEye).sqrMagnitude<45*45&&count<12&&Powered(lampSectors[lamp])&&State.power.LifePowered;
                lamp.renderMode=active&&count<4?LightRenderMode.ForcePixel:LightRenderMode.ForceVertex;
                lamp.shadows=active&&count==0?LightShadows.Soft:LightShadows.None;
                if(lamp.enabled)count++;
            }
            // Refresh displayed allocation while paused, without advancing energy or jobs.
            if(Live)BalancePower(0);
        }
    }
}

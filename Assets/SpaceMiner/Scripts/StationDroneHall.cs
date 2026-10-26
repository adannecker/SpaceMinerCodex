using System;
using UnityEngine;

namespace SpaceMiner
{
    // The station supplies its existing visitor/controller; this module never owns camera or input.
    public sealed class StationDroneHall : MonoBehaviour
    {
        public Transform[] Portals, LeftLeaves, RightLeaves;
        private readonly Vector3[] left = new Vector3[3], right = new Vector3[3], axes = new Vector3[3];
        private readonly float[] amounts = { 1, 1, 1 };
        private readonly bool[] targets = { true, true, true };
        private readonly float[] travel=new float[3];
        private bool vacuumBoundary;
        internal void ConfigureVacuumBoundary()
        {
            vacuumBoundary=true;
            for(int i=0;i<3;i++){amounts[i]=i==1?1:0;targets[i]=i==1;Apply(i);}
        }
        internal Func<Vector3?> Visitor;
        internal float Amount(int index) => amounts[index];
        internal bool Target(int index) => targets[index];
        private void Awake()
        {
            for(int i=0;i<3;i++)
            {
                left[i]=LeftLeaves[i].localPosition;right[i]=RightLeaves[i].localPosition;
                axes[i]=(right[i]-left[i]).normalized;
                travel[i]=1.66f/LeftLeaves[i].parent.TransformVector(axes[i]).magnitude;Apply(i);
            }
        }
        private void Update()
        {
            if(SettingsMenu.BlocksInput || StartMenu.IsOpen || IntroSequence.BlocksGameplay || MemoryCinematic.IsPlaying)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            for(int i=0;i<3;i++)
            {
                if(!targets[i] && amounts[i]>.001f && Occupied(i))targets[i]=true;
                amounts[i]=Mathf.MoveTowards(amounts[i],targets[i]?1:0,dt*.55f);Apply(i);
            }
        }
        private void Apply(int i)
        {
            float distance=travel[i]*Mathf.SmoothStep(0,1,amounts[i]);
            LeftLeaves[i].localPosition=left[i]-axes[i]*distance;
            RightLeaves[i].localPosition=right[i]+axes[i]*distance;
        }
        private bool Occupied(int i)
        {
            Vector3? feet=Visitor?.Invoke();if(!feet.HasValue)return false;
            Vector3 delta=feet.Value-Portals[i].position;
            Vector3 side=LeftLeaves[i].parent.TransformDirection(axes[i]).normalized;
            return Mathf.Abs(Vector3.Dot(delta,side))<1.92f
                && Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(side,Vector3.up)))<.85f
                && delta.y> -2.1f && delta.y<1.9f;
        }
        internal int Nearest(Vector3 feet)
        {
            int best=-1;float distance=3;
            for(int i=0;i<3;i++)
            {
                Vector3 delta=feet-Portals[i].position;
                if(Mathf.Abs(delta.y+1.7f)>2)continue;
                delta.y=0;if(delta.magnitude<distance){distance=delta.magnitude;best=i;}
            }
            return best;
        }
        internal bool Request(int index,bool open)
        {
            if(vacuumBoundary)return false;
            if(!open && Occupied(index))return false;
            targets[index]=open;return true;
        }
        internal bool Interact(Vector3 feet)
        {
            int i=Nearest(feet);if(i<0)return false;Request(i,!targets[i]);return true;
        }
        internal string Prompt(Vector3 feet)
        {
            if(vacuumBoundary)return Nearest(feet)<0?"":"Außenbereich: Zweitür-Schleuse benutzen / E an der Schleusentür";
            int i=Nearest(feet);return i<0?"":Occupied(i)?"Durchgang freihalten / E Tor bedienen"
                :"E  Drohnentor "+(i+1).ToString("00")+(targets[i]?" schließen":" öffnen");
        }
    }
}

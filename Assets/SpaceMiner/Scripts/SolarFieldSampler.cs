using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Two rays per remaining cell, cached in real time even at fast simulation rates.
    internal sealed class SolarFieldSampler
    {
        private Transform panel;
        private readonly List<Transform> cells=new List<Transform>();
        private readonly RaycastHit[] hits=new RaycastHit[32];
        private float nextSample=-1,area,primaryVisibility=1,companionVisibility=1;
        private Vector3 position;private Quaternion rotation;private bool cast;
        internal int LastRayCount {get;private set;}
        internal void Invalidate()=>nextSample=-1;
        public SolarFieldView Capture(Transform root,BinaryStarView stars,float condition,float scale)
        {
            if(panel!=root)
            {
                panel=root;cells.Clear();area=0;
                if(root!=null)foreach(var t in root.GetComponentsInChildren<Transform>())
                    if(t.name.StartsWith("Solar cell "))
                    {
                        cells.Add(t);var size=t.lossyScale;area+=Mathf.Abs(size.x*size.z);
                    }
                Invalidate();
            }
            if(root==null)return default;
            if(nextSample<0||Time.unscaledTime>=nextSample||root.position!=position||root.rotation!=rotation)
            {
                position=root.position;rotation=root.rotation;nextSample=Time.unscaledTime+.5f;LastRayCount=0;
                Physics.SyncTransforms();
                primaryVisibility=Visible(stars.Primary.Direction);
                companionVisibility=Visible(stars.Companion.Direction);cast=true;
            }
            return new SolarFieldView(stars,root.up,area,BinaryStarEnergy.FullPanelAreaM2,condition,scale,
                cast?primaryVisibility:1,cast?companionVisibility:1);
        }
        private float Visible(Vector3 direction)
        {
            if(cells.Count==0)return 0;
            int clear=0;
            foreach(var cell in cells)
            {
                // Above the top face, ignoring this wing's own small structural trim.
                var origin=cell.TransformPoint(Vector3.up*.55f)+direction*.03f;
                int count=Physics.RaycastNonAlloc(origin,direction,hits,1000000,(1<<0)|(1<<25),QueryTriggerInteraction.Ignore);
                LastRayCount++;
                bool blocked=count==hits.Length; // Saturation is conservatively obscured.
                for(int j=0;j<count&&!blocked;j++)
                    if(!hits[j].transform.IsChildOf(panel))blocked=true;
                if(!blocked)clear++;
            }
            return (float)clear/cells.Count;
        }
    }
}

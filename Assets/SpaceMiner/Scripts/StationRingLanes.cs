using UnityEngine;

namespace SpaceMiner
{
    // Metre-scale ring traffic: inner pair clockwise (+angle), outer pair counterclockwise.
    public static class StationRingLanes
    {
        public const int Count=4;
        public const float InnerEdge=30.85f, Width=1.5f, Median=33.85f, OuterEdge=36.85f;
        // V02 bottom-contact centres measured at x=-0.176/+0.173m after 75cm scaling.
        public const float MagnetGauge=.35f, MagnetWidth=.08f;
        public static readonly float[] JunctionAngles={-60,-20,0,10,30,45,60};
        public static bool Junction(Vector3 local)
        {
            foreach(float angle in JunctionAngles)if(Mathf.Abs(Mathf.DeltaAngle(Angle(local),angle))<3.5f)return true;
            return false;
        }
        public static float Center(int lane)=>InnerEdge+(lane+.5f)*Width;
        public static int Direction(int lane)=>lane<2?1:-1;
        public static int NormalLane(int direction)=>direction>0?0:3;
        public static int PassingLane(int direction)=>direction>0?1:2;
        public static float Angle(Vector3 local)=>Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
        public static float Radius(Vector3 local)=>new Vector2(local.x,local.z).magnitude;
        public static bool OnRoad(Vector3 local)=>Radius(local)>InnerEdge+.2f&&Radius(local)<OuterEdge-.2f;
    }
}

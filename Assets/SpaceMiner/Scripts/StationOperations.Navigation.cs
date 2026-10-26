using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private string helperObstruction="";

        // The room-plan key "battery" now belongs to the power terminal. Repair
        // work reaches the installed bank across the corridor from the command entrance.
        public Vector3 BatteryServicePoint => Layout.Geometry.GetBatteryBayAnchor(StationBatteryPlacement.Axis,1)
            .TransformPoint(new Vector3(0,1.25f,.7f));

        private static bool IsBatteryRepair(StationWorkOrder order)
            =>order.kind==StationJobKind.Repair&&order.target=="battery";

        private Vector3[] BatteryRepairPath()
        {
            Vector3 home=HelperHomePoint(1);
            var points=new List<Vector3>{home,home+Vector3.up*.25f,
                Layout.transform.TransformPoint(RingWalkthrough.At(27,30,1.25f)),
                Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,30,1.25f))};
            // Follow the intact ring clockwise. Ten-degree chords stay inside the
            // pedestrian lane and avoid the destroyed sector at +90 degrees.
            for(int angle=20;angle>=0;angle-=10)
                points.Add(Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,angle,1.25f)));
            var anchor=Layout.Geometry.GetBatteryBayAnchor(StationBatteryPlacement.Axis,1);
            // Move sideways while still in the ring, then enter only the bay's
            // own service opening. The locked central reactor shutter is bypassed.
            points.Add(anchor.TransformPoint(new Vector3(0,1.25f,-5.4f)));
            points.Add(BatteryServicePoint);
            return points.ToArray();
        }
        private bool PlanWorkFlight(StationWorkOrder order,DroneAgent drone,out string reason)
        {
            reason="";
            try{
                var points=new List<Vector3>{drone.HomePosition,drone.HomePosition+Vector3.up*1.1f,drone.HomeApproachPoint,drone.HomeHoldPoint};
                points.AddRange(drone.Navigation!=null?drone.Navigation.Plan(drone.HomeHoldPoint,WorkTarget(order)):new[]{WorkTarget(order)});
                order.flightPath=points.ToArray();return true;
            }catch(Exception){reason="Kein sicherer Außenflugweg: Hindernis entfernen / anderen Auftrag wählen";return false;}
        }
        private static Vector3 PathPoint(Vector3[] path,float progress)
        {
            float length=0;for(int i=1;i<path.Length;i++)length+=Vector3.Distance(path[i-1],path[i]);
            float distance=Mathf.Clamp01(progress)*length;
            for(int i=1;i<path.Length;i++){float leg=Vector3.Distance(path[i-1],path[i]);if(distance<=leg)return Vector3.Lerp(path[i-1],path[i],leg<.0001f?1:distance/leg);distance-=leg;}
            return path[path.Length-1];
        }
        private Vector3 HelperPoint(StationWorkOrder order,float progress)
        {
            Vector3 a=HelperHomePoint(1),lift=a+Vector3.up*.25f;
            Vector3 entry=Layout.transform.TransformPoint(RingWalkthrough.At(27,30,1.25f));
            Vector3 ring=Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,30,1.25f));
            Vector3[] path;
            if(IsBatteryRepair(order))path=BatteryRepairPath();
            else if(order.kind==StationJobKind.Recycle||order.kind==StationJobKind.Refit||order.kind==StationJobKind.ActivateSecond)
                path=order.kind==StationJobKind.Recycle
                    ?new[]{a,lift,entry,ring,Layout.transform.TransformPoint(RingWalkthrough.At(42,30,1.25f)),Layout.transform.TransformPoint(RingWalkthrough.At(47,30,1.25f)),WorkshopServicePoint(order.kind)}
                    :new[]{a,lift,entry,ring,Layout.transform.TransformPoint(RingWalkthrough.At(42,30,1.25f)),WorkshopServicePoint(order.kind)};
            else{bool food=order.kind==StationJobKind.Food||order.kind==StationJobKind.Oxygen||order.target=="food";path=new[]{a,lift,entry,ring,Layout.transform.TransformPoint(RingWalkthrough.At(33.5f,15,1.25f)),Layout.transform.TransformPoint(new Vector3(0,1.25f,33.5f)),Layout.transform.TransformPoint(new Vector3(0,1.25f,42)),CommandServicePoint(food?"food":"printer")};}
            float trip=progress<.25f?progress*4:progress>.8f?(1-progress)*5:1;
            return PathPoint(path,trip);
        }
        private bool HelperPathClear(StationWorkOrder order,float before,float seconds)
        {
            if(IsBatteryRepair(order))return BatteryHelperPathClear(order,before,seconds);
            Vector3 from=HelperPoint(order,before/StationRecipes.Get(order.kind).Seconds),to=HelperPoint(order,order.elapsed/StationRecipes.Get(order.kind).Seconds);
            return HelperSegmentClear(from,to,seconds);
        }

        private bool BatteryHelperPathClear(StationWorkOrder order,float before,float seconds)
        {
            var path=BatteryRepairPath();
            float duration=StationRecipes.Get(order.kind).Seconds;
            float start=Mathf.Clamp01(before/duration),end=Mathf.Clamp01(order.elapsed/duration);
            // Include both changes of direction even when a time-lapse step spans
            // travelling, working and returning. A single old-to-new chord would
            // cut across the central void or omit an entire outward/return trip.
            float cursor=start;
            if(end<start)return BatteryTripClear(path,BatteryTrip(start),BatteryTrip(end),seconds);
            foreach(float stop in new[]{.25f,.8f,end})
            {
                if(stop<=cursor||stop>end)continue;
                if(!BatteryTripClear(path,BatteryTrip(cursor),BatteryTrip(stop),seconds))return false;
                cursor=stop;
            }
            helperObstruction="";return true;
        }

        private static float BatteryTrip(float progress)
            =>progress<.25f?progress*4:progress>.8f?(1-progress)*5:1;

        private bool BatteryTripClear(Vector3[] path,float start,float end,float seconds)
        {
            float length=0;for(int i=1;i<path.Length;i++)length+=Vector3.Distance(path[i-1],path[i]);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(end-start)*length/.4f));
            Vector3 from=PathPoint(path,start);
            for(int i=1;i<=steps;i++)
            {
                Vector3 to=PathPoint(path,Mathf.Lerp(start,end,(float)i/steps));
                if(!HelperSegmentClear(from,to,seconds))return false;
                from=to;
            }
            return true;
        }

        private bool HelperSegmentClear(Vector3 from,Vector3 to,float seconds)
        {
            if((to-from).sqrMagnitude<.000001f)return true;
            foreach(var gate in Layout.Geometry.PressureGates){
                var p=gate.transform.InverseTransformPoint(to);if(Mathf.Abs(p.x)<1.4f&&Mathf.Abs(p.z)<2&&gate.Opening<.98f){helperObstruction=gate.Label+(gate.Locked?" gesperrt":" öffnet");if(gate.Locked)return false;gate.Request(true,from-Vector3.up*.65f);gate.Advance(seconds,null);if(gate.Opening<.98f)return false;}
            }
            if(Physics.SphereCast(from,.28f,(to-from).normalized,out var hit,Vector3.Distance(from,to),1<<25,QueryTriggerInteraction.Ignore)){helperObstruction=hit.collider.name;return false;}helperObstruction="";return true;
        }
    }
}

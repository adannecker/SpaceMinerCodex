using System;
using UnityEngine;

namespace SpaceMiner
{
    // Optional saved travel state: old orders without this member begin a new safe journey.
    // Elapsed on the work order counts tool service only; transit and interlock waits do not.
    [Serializable] public sealed class WorkshopRefitState
    {
        public bool present;
        public int stage,waypoint,doorStage;
        public float halfLength,pivotHeight;
        public Vector3 position;
        public Quaternion rotation=Quaternion.identity;
        public StationAirlock.SavedState airlock;
        public static bool HasData(WorkshopRefitState state)=>state!=null&&state.present;
        private static bool EmptyPlaceholder(WorkshopRefitState state)
        {
            // JsonUtility expands absent inline classes into default objects. Accept only
            // that exact empty representation; a disabled marker must not hide travel data.
            var a=state.airlock;
            return state.stage==0&&state.waypoint==0&&state.doorStage==0&&state.halfLength==0&&state.pivotHeight==0
                &&state.position.x==0&&state.position.y==0&&state.position.z==0
                &&state.rotation.x==0&&state.rotation.y==0&&state.rotation.z==0&&(state.rotation.w==0||state.rotation.w==1)
                &&(a==null||a.phase==AirlockPhase.Idle&&!a.towardsRing&&a.habitatOpen==0&&a.ringOpen==0&&a.equalizing==0&&a.occupantHalfLength==0);
        }
        public static bool Valid(WorkshopRefitState state)
        {
            if(state==null)return true;
            if(!state.present)return EmptyPlaceholder(state);
            bool Finite(float x)=>ResourceContainer.Finite(x);
            int maximumWaypoint=state.stage==0?5:state.stage==2?2:state.stage==4?1:state.stage==6?4:0;
            return state.stage>=0&&state.stage<=7&&state.waypoint>=0&&state.waypoint<=8&&state.doorStage>=0&&state.doorStage<=2
                &&state.waypoint<=maximumWaypoint&&(state.doorStage==0||state.stage==1||state.stage==5)
                &&Finite(state.halfLength)&&state.halfLength>0&&state.halfLength<3.8f&&Finite(state.pivotHeight)&&state.pivotHeight>0&&state.pivotHeight<4.1f
                &&Finite(state.position.x)&&Finite(state.position.y)&&Finite(state.position.z)
                &&Finite(state.rotation.x)&&Finite(state.rotation.y)&&Finite(state.rotation.z)&&Finite(state.rotation.w)
                &&Mathf.Abs(1-(state.rotation.x*state.rotation.x+state.rotation.y*state.rotation.y+state.rotation.z*state.rotation.z+state.rotation.w*state.rotation.w))<.01f
                &&(state.airlock==null||StationAirlock.IsValid(state.airlock));
        }
    }
    public sealed partial class StationOperations
    {
        private DroneAgent RefitDrone(StationWorkOrder order)=>Scenario.Drones[int.Parse(order.target.Split(':')[0])];
        private Bounds RefitHull(DroneAgent drone)
        {
            var boxes=drone.GetComponent<TripoMiningDroneVisual>().FlightBoxes;var bounds=boxes[0];
            foreach(var box in boxes)bounds.Encapsulate(box);return bounds;
        }
        private Quaternion WorkshopOrientation=>Layout.transform.rotation*Quaternion.Euler(0,30,0);
        private Vector3 WorkshopPivot(WorkshopRefitState state,float x,float z)=>workshop.TransformPoint(new Vector3(x,state.pivotHeight,z));
        private Vector3 WorkshopLockPivot(StationWorkOrder order,float centreZ)
        {
            var door=Layout.Airlock;var hull=RefitHull(RefitDrone(order));
            var offset=Quaternion.Inverse(door.transform.rotation)*WorkshopOrientation*hull.center;
            return door.transform.TransformPoint(new Vector3(-offset.x,order.workshop.pivotHeight,centreZ-offset.z));
        }
        private Vector3 WorkshopAirlockPivot(StationWorkOrder order,bool outside)
        {
            var door=Layout.Airlock;var state=order.workshop;
            float z=outside?door.ExitZ-state.halfLength-.6f:door.EntranceZ+state.halfLength+.6f;
            return WorkshopLockPivot(order,z);
        }
        private bool WorkshopRefitReady(StationWorkOrder order,out string why)
        {
            why="";if(workshop==null||Layout.Airlock==null){why="Innenwerkstatt fehlt";return false;}
            var drone=RefitDrone(order);var hull=RefitHull(drone);var gate=Layout.Airlock;
            if(hull.size.x+.15f>gate.ClearWidth||hull.size.y+.45f>gate.ClearHeight||hull.size.z+.8f>gate.EntranceZ-gate.ExitZ){why="Drohne passt nicht durch die Werkstattschleuse";return false;}
            if(!Powered(1)||Element("hall").condition<.3f){why="Umbauplatz 01 ohne Versorgung / defekt";return false;}
            if(drone.BatteryKwh<drone.EnergyReserveKwh+.35f||drone.FuelLiters<drone.FuelReserveLiters+.25f){why="Reserve für Werkstattflug fehlt";return false;}
            return true;
        }
        private bool BeginWorkshopRefit(StationWorkOrder order)
        {
            var drone=RefitDrone(order);if(!drone.StartStationWork())return false;var bounds=RefitHull(drone);
            order.workshop=new WorkshopRefitState{present=true,position=drone.transform.position,rotation=drone.transform.rotation,
                halfLength=bounds.extents.z+.05f,pivotHeight=.32f-bounds.min.y};
            return true;
        }
        private Vector3[] WorkshopOutsideRoute(StationWorkOrder order)
        {
            var drone=RefitDrone(order);Vector3 outer=WorkshopAirlockPivot(order,true);
            Vector3 high=outer+Layout.transform.up*12;
            return new[]{drone.HomePosition,drone.HomePosition+Layout.transform.up*1.1f,drone.HomeApproachPoint,drone.HomeHoldPoint,high,outer};
        }
        private Vector3[] WorkshopInsideRoute(StationWorkOrder order)
        {
            var s=order.workshop;
            Vector3 target=primaryRepairCell!=null?primaryRepairCell.position+Layout.transform.up*s.pivotHeight:WorkshopPivot(s,-4.3f,8.1f);
            Vector3 local=workshop.InverseTransformPoint(target);
            return new[]{WorkshopAirlockPivot(order,false),workshop.TransformPoint(new Vector3(0,local.y,local.z)),target};
        }
        private bool MoveWorkshopDrone(StationWorkOrder order,Vector3 destination,float seconds,Quaternion orientation)
        {
            var state=order.workshop;var drone=RefitDrone(order);float remaining=Mathf.Max(0,seconds)*.85f;
            while(remaining>.0001f){
                float step=Mathf.Min(.18f,remaining);var next=Vector3.MoveTowards(state.position,destination,step);
                var nextRotation=Quaternion.RotateTowards(state.rotation,orientation,step*40);
                if(drone.Navigation!=null&&!drone.Navigation.PoseClear(next,nextRotation,out string obstacle)){
                    order.status="Wartet: Werkstattflug / "+obstacle;return false;
                }
                float moved=Vector3.Distance(state.position,next);state.position=next;state.rotation=nextRotation;remaining-=step;
                drone.SetStationWorkPose(next,step/.85f);drone.transform.rotation=state.rotation;
                if(moved<.00001f&&Quaternion.Angle(state.rotation,orientation)<.1f)break;
            }
            return Vector3.Distance(state.position,destination)<.008f&&Quaternion.Angle(state.rotation,orientation)<.1f;
        }
        private void CancelWorkshopRefit(StationWorkOrder order)
        {
            order.cancelled=true;var state=order.workshop;
            if(!WorkshopRefitState.HasData(state)){order.elapsed=StationRecipes.Get(order.kind).Seconds;return;}
            float duration=StationRecipes.Get(order.kind).Seconds;
            if(state.stage==3){
                float p=order.elapsed/duration,trip=p<.25f?p*4:p>.8f?(1-p)*5:1;
                order.elapsed=duration*(1-.2f*trip);
            }else if(state.stage<3)order.elapsed=duration;
            // An occupied pressure cycle always completes before reversing direction.
            if(state.stage==0){state.stage=6;state.waypoint=Mathf.Max(0,state.waypoint-1);}
            else if(state.stage==2){state.stage=4;state.waypoint=Mathf.Max(0,state.waypoint-1);}
            else if(state.stage==3){state.stage=4;state.waypoint=1;}
            order.status="Umbau abgebrochen / sichere Rückkehr";
        }
        private bool RefitPlayerInChamber()
        {
            if(!StationInteriorMode.IsInside)return false;
            var gate=Layout.Airlock;var p=gate.transform.InverseTransformPoint(StationInteriorMode.Current.FeetPosition);
            return Mathf.Abs(p.x)<gate.ChamberHalfWidth+.35f&&p.z>gate.ExitZ-1&&p.z<gate.EntranceZ+1;
        }
        private bool AdvanceWorkshopAirlock(StationWorkOrder order,float seconds,bool towardsOutside)
        {
            var state=order.workshop;var gate=Layout.Airlock;var drone=RefitDrone(order);
            var hull=RefitHull(drone);var centre=state.position+state.rotation*hull.center;
            order.status="Drohne / verriegelter Schleusentransit";
            // Gate safety follows the full hull centre, including asymmetric drill/cargo bounds.
            if(state.doorStage==0){
                if(RefitPlayerInChamber()){order.status="Wartet: Besucher in der Drohnenschleuse";return false;}
                if(gate.Phase!=AirlockPhase.Idle||!gate.RequestTransit(centre,state.halfLength)){order.status="Wartet: Drohnenschleuse belegt";return false;}
                state.doorStage=1;
            }
            if(gate.Phase==AirlockPhase.AwaitingEntry){
                var middle=WorkshopLockPivot(order,(gate.EntranceZ+gate.ExitZ)*.5f);
                MoveWorkshopDrone(order,middle,seconds,WorkshopOrientation);
            }
            else if(gate.Phase==AirlockPhase.AwaitingExit||gate.Phase==AirlockPhase.ClosingExit||gate.Phase==AirlockPhase.Idle){
                // The safety controller may start closing as soon as the trailing hull clears.
                // Continue to the farther navigation hold while that door closes behind it.
                if(MoveWorkshopDrone(order,WorkshopAirlockPivot(order,towardsOutside),seconds,WorkshopOrientation))state.doorStage=2;
            }
            // Keep the real hull safe even if the player's camera is outside the station.
            centre=state.position+state.rotation*hull.center;gate.Advance(seconds,centre);
            state.airlock=gate.CaptureState();
            if(!order.status.StartsWith("Wartet:",StringComparison.Ordinal))order.status=gate.Phase==AirlockPhase.Equalizing?"Drohne / Druckausgleich":"Drohne / verriegelter Schleusentransit";
            var local=gate.transform.InverseTransformPoint(centre);
            order.status+=" / "+gate.Phase+" / Rumpfmitte "+local.ToString("F2")+" / Halbmaß "+state.halfLength.ToString("F2");
            if(state.doorStage==2&&gate.Phase==AirlockPhase.Idle){state.doorStage=0;return true;}return false;
        }
        // Returns true only once the same vehicle has physically returned to its own cradle.
        private bool AdvanceWorkshopRefit(StationWorkOrder order,float seconds)
        {
            if(!WorkshopRefitState.HasData(order.workshop)){
                // Version-1 saves can contain an old refit reserved while the drone stayed docked.
                var drone=RefitDrone(order);drone.ResumeStationWork();var bounds=RefitHull(drone);
                order.workshop=new WorkshopRefitState{present=true,position=drone.transform.position,rotation=drone.transform.rotation,
                    halfLength=bounds.extents.z+.05f,pivotHeight=.32f-bounds.min.y};
                if(order.cancelled){order.workshop.stage=7;return true;}
            }
            var state=order.workshop;
            if(order.cancelled&&order.elapsed<StationRecipes.Get(order.kind).Seconds){
                float before=order.elapsed;order.elapsed=Mathf.Min(StationRecipes.Get(order.kind).Seconds,order.elapsed+seconds);
                if(!HelperPathClear(order,before,seconds))order.elapsed=before;else AnimateOrder(order,seconds);
            }
            if(state.stage==0){
                order.status="Drohne fliegt zur Werkstattschleuse";
                var path=WorkshopOutsideRoute(order);if(state.waypoint==0)state.waypoint=1;
                var orientation=state.waypoint>=4?WorkshopOrientation:RefitDrone(order).HomeRotation;
                if(MoveWorkshopDrone(order,path[state.waypoint],seconds,orientation)&&++state.waypoint>=path.Length){state.stage=1;state.waypoint=0;}
            }
            else if(state.stage==1){if(AdvanceWorkshopAirlock(order,seconds,false)){state.stage=order.cancelled?4:2;state.waypoint=order.cancelled?0:1;}}
            else if(state.stage==2){
                order.status="Drohne fährt in Umbauplatz 01";
                var path=WorkshopInsideRoute(order);
                var orientation=state.waypoint==path.Length-1&&primaryRepairCell!=null?primaryRepairCell.rotation:WorkshopOrientation;
                if(MoveWorkshopDrone(order,path[state.waypoint],seconds,orientation)&&++state.waypoint>=path.Length){state.stage=3;state.waypoint=0;}
            }
            else if(state.stage==3){
                if(!Powered(1)||!State.power.JobPowered){order.status="Umbauplatz 01 / wartet auf Strom";return false;}
                float before=order.elapsed;
                order.elapsed=Mathf.Min(StationRecipes.Get(order.kind).Seconds,order.elapsed+seconds);
                if(!HelperPathClear(order,before,seconds)){order.elapsed=before;order.status="Pausiert: R-01 zum Umbauplatz / "+helperObstruction;return false;}
                AnimateOrder(order,seconds);
                order.status="Umbauplatz 01 / Werkzeugwechsel durch R-01 und Roboterarme";
                if(order.elapsed>=StationRecipes.Get(order.kind).Seconds){state.stage=4;state.waypoint=1;}
            }
            else if(state.stage==4){
                order.status="Drohne verlässt Umbauplatz 01";
                var path=WorkshopInsideRoute(order);
                if(MoveWorkshopDrone(order,path[state.waypoint],seconds,WorkshopOrientation)&&--state.waypoint<0){state.stage=5;state.waypoint=0;}
            }
            else if(state.stage==5){if(AdvanceWorkshopAirlock(order,seconds,true)){state.stage=6;state.waypoint=4;}}
            else if(state.stage==6){
                order.status="Drohne kehrt zum Außendock zurück";
                var path=WorkshopOutsideRoute(order);var orientation=state.waypoint>=4?WorkshopOrientation:RefitDrone(order).HomeRotation;
                if(MoveWorkshopDrone(order,path[state.waypoint],seconds,orientation)&&--state.waypoint<0){state.stage=7;state.waypoint=0;}
            }
            if(state.stage==7){
                if(order.cancelled&&order.elapsed<StationRecipes.Get(order.kind).Seconds){order.status="Drohne am Dock / R-01 kehrt sicher zur internen Werkstatt zurück";return false;}
                order.elapsed=StationRecipes.Get(order.kind).Seconds;return true;
            }
            return false;
        }
        internal void RestoreWorkshopRefits()
        {
            foreach(var order in State.orders)if(order.kind==StationJobKind.Refit&&order.started&&!order.complete&&WorkshopRefitState.HasData(order.workshop)){
                var drone=RefitDrone(order);drone.ResumeStationWork();drone.SetStationWorkPose(order.workshop.position,0);drone.transform.rotation=order.workshop.rotation;
                if(order.workshop.doorStage>0&&order.workshop.airlock!=null)Layout.Airlock.RestoreState(order.workshop.airlock);
            }
        }
    }
}

using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationIndoorTraffic
    {
        // Reserve the whole conflict area, including its departure lane. There
        // is one FIFO for room turns, U-turns and through traffic: nobody may
        // park on a crossing while waiting to merge into an occupied lane.
        private long nextCrossingTicket;
        private const float CrossingHalfAngle=4.5f, HoldAngle=5.5f, RequestAngle=8;
        public int CompletedCrossings { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void PreparePassingConflict()
        {
            // A later-ticket overtaker reaches the hold line beside the slower
            // ticket holder. Merging before admission would trap both vehicles.
            for(int i=7;i<=8;i++)
            {
                var v=vehicles[i];v.NextStop=12;v.Junction=-1;v.CrossingGranted=false;v.crossingEntered=false;
                v.Passing=i==8;v.passTarget=i==8?vehicles[7]:null;
                var position=Snap(At(StationRingLanes.Center(i==8?1:0),i==8?52.2f:53));
                v.Agent.Warp(position);v.Agent.transform.position=position;v.Agent.nextPosition=position;v.previous=position;
                SetDestination(v);
            }
        }
#endif
        public string Diagnostic()
        {
            string result="";
            foreach(var v in vehicles)
                result+=v.Id+" stop="+v.NextStop+" pos="+Local(v)+" target="+layout.transform.InverseTransformPoint(v.Stops[v.NextStop])
                    +" J="+v.Junction+" ticket="+v.crossingTicket+" granted="+v.CrossingGranted+" entered="+v.crossingEntered
                    +" guided="+v.Guided+" passing="+v.Passing+" velocity="+v.Agent.velocity+" status="+v.Status+"\n";
            return result;
        }
        private Vector3 Local(Vehicle v)=>layout.transform.InverseTransformPoint(v.Agent.transform.position);
        private bool InCrossing(Vector3 p,int junction)
        {
            float r=StationRingLanes.Radius(p);
            return r>29.5f&&r<38f&&Mathf.Abs(Mathf.DeltaAngle(StationRingLanes.JunctionAngles[junction],StationRingLanes.Angle(p)))<CrossingHalfAngle;
        }
        private int RequestedCrossing(Vehicle v)
        {
            var p=Local(v);
            var end=layout.transform.InverseTransformPoint(v.Stops[v.NextStop]);
            var start=layout.transform.InverseTransformPoint(v.Stops[(v.NextStop+v.Stops.Length-1)%v.Stops.Length]);
            if(!v.Guided)
            {
                if(v.crossingDone||(!StationRingLanes.OnRoad(start)&&!StationRingLanes.OnRoad(end)))return -1;
                float angle=StationRingLanes.Angle(StationRingLanes.OnRoad(end)?end:start);
                for(int j=0;j<StationRingLanes.JunctionAngles.Length;j++)
                    if(Mathf.Abs(Mathf.DeltaAngle(angle,StationRingLanes.JunctionAngles[j]))<2)return j;
                return -1;
            }
            float here=StationRingLanes.Angle(p);
            float remaining=Mathf.Max(0,Mathf.DeltaAngle(here,StationRingLanes.Angle(end))*v.Direction);
            int nearest=-1;float distance=RequestAngle;
            for(int j=0;j<StationRingLanes.JunctionAngles.Length;j++)
            {
                float ahead=Mathf.DeltaAngle(here,StationRingLanes.JunctionAngles[j])*v.Direction;
                if(ahead>=-.5f&&ahead<=remaining+.5f&&ahead<=distance){distance=ahead;nearest=j;}
            }
            return nearest;
        }
        private void CoordinateCrossings()
        {
            foreach(var v in vehicles)
            {
                if(v.Junction<0||!v.CrossingGranted)continue;
                bool inside=InCrossing(Local(v),v.Junction);
                if(inside)v.crossingEntered=true;
                if(!v.crossingEntered||inside)continue;
                v.Junction=-1;v.CrossingGranted=false;v.crossingEntered=false;
                v.crossingDone=true;v.Crossings++;CompletedCrossings++;
            }
            foreach(var v in vehicles)
            {
                if(v.Junction>=0)continue;
                int requested=RequestedCrossing(v);
                if(requested<0)continue;
                v.Junction=requested;v.crossingTicket=++nextCrossingTicket;
            }
            for(int junction=0;junction<StationRingLanes.JunctionAngles.Length;junction++)
            {
                Vehicle first=null;bool occupied=false;
                foreach(var v in vehicles)
                {
                    if(v.Junction!=junction)continue;
                    if(v.CrossingGranted){occupied=true;break;}
                    if(first==null||v.crossingTicket<first.crossingTicket)first=v;
                }
                if(occupied||first==null)continue;
                // Native avoidance / the free-path-to-guide handover can leave
                // an unadmitted actor just inside the zone. Holding it there
                // while waiting for an empty zone would block every ticket.
                // Drain that existing occupant first; FIFO applies once empty.
                foreach(var v in vehicles)
                    if(v.Junction==junction&&InCrossing(Local(v),junction)
                        &&(!InCrossing(Local(first),junction)||v.crossingTicket<first.crossingTicket))first=v;
                bool clear=true;
                foreach(var other in vehicles)
                    if(other!=first&&InCrossing(Local(other),junction)){clear=false;break;}
                if(!clear)continue; // Existing traffic drains before the first admission.
                // Include the native departure point in the reservation. Through
                // traffic waiting outside the zone cannot obstruct this point.
                var end=first.Guided
                    ?At(StationRingLanes.Center(first.Passing?StationRingLanes.PassingLane(first.Direction):StationRingLanes.NormalLane(first.Direction)),
                        StationRingLanes.JunctionAngles[junction]+first.Direction*(CrossingHalfAngle+.5f))
                    :first.Stops[first.NextStop];
                foreach(var other in vehicles)
                    if(other!=first&&Vector3.Distance(end,other.Agent.transform.position)<1.2f){clear=false;break;}
                if(!clear)continue;
                first.CrossingGranted=true;first.crossingEntered=InCrossing(Local(first),junction);
            }
        }
        private bool CrossingHold(Vehicle v)
        {
            if(v.Junction<0||v.CrossingGranted)return false;
            if(!v.Guided)return true;
            float ahead=Mathf.DeltaAngle(StationRingLanes.Angle(Local(v)),StationRingLanes.JunctionAngles[v.Junction])*v.Direction;
            return ahead<=HoldAngle;
        }
    }
}

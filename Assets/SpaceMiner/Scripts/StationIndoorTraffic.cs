using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SpaceMiner
{
    // Opt-in visual driving trial. These actors never become campaign inventory or workers.
    public sealed partial class StationIndoorTraffic : MonoBehaviour
    {
        public sealed class Vehicle
        {
            public string Id, Route;
            public NavMeshAgent Agent;
            public Vector3[] Stops;
            public int NextStop, StopsReached, RoomVisits;
            public float Distance;
            public string Status;
            public bool Guided, Passing;
            public int Direction, Overtakes;
            public float CruiseSpeed;
            internal Vector3 previous;
            public int Junction = -1, Crossings;
            public bool CrossingGranted;
            internal long crossingTicket;
            internal bool crossingEntered, crossingDone;
            internal Vehicle passTarget;
        }
        private readonly List<Vehicle> vehicles = new List<Vehicle>();
        private readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        private readonly List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
        private StationInteriorLayout layout;
        private WaterScenario scenario;
        private NavMeshData data;
        private NavMeshDataInstance navigation;
        private NavMeshBuildSettings settings;
        private bool dirty, rebuilding;
        public bool Ready { get; private set; }
        public string Failure { get; private set; }
        public IReadOnlyList<Vehicle> Vehicles => vehicles;
        public int NavigationSourceCount => sources.Count;
        public const float ModelLength = IndoorDroneVisual.OverallLength;

        internal static void Install(WaterScenario scenario, StationInteriorLayout layout)
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-indoorTrafficPreview") < 0 && Array.IndexOf(args, "-stationIndoorTrafficCheck") < 0) return;
            var traffic = layout.gameObject.AddComponent<StationIndoorTraffic>();
            traffic.layout = layout; traffic.scenario = scenario;
        }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            try
            {
                settings = NavMesh.CreateSettings();
                settings.agentRadius = .48f; settings.agentHeight = .7f;
                settings.agentClimb = .12f; settings.agentSlope = 25;
                settings.overrideVoxelSize = true; settings.voxelSize = .1f;
                settings.overrideTileSize = true; settings.tileSize = 128; settings.minRegionArea = .4f;
                RefreshNavigation();
                if (data == null) throw new InvalidOperationException("Keine befahrbare Stationsfläche gefunden.");
                BuildVehicles();
                foreach (var room in scenario.Operations.RoomPlans) room.Changed += PlanChanged;
                Ready = vehicles.Count == 10;
            }
            catch (Exception error) { Failure = error.ToString(); Debug.LogException(error); }
        }
        private void PlanChanged() => dirty = true;
        private Vector3 At(float radius, float angle) => layout.transform.TransformPoint(RingWalkthrough.At(radius, angle));
        private Vector3 Room(string id, float x, float z)
        {
            foreach (var room in scenario.Operations.RoomPlans)
                if (room.RoomId == id) return room.Frame.TransformPoint(new Vector3(x, 0, z));
            throw new InvalidOperationException("Fahrprobe: Raum fehlt / " + id);
        }
        private void RefreshNavigation()
        {
            rebuilding = true; Physics.SyncTransforms(); sources.Clear(); markups.Clear();
            foreach (var gate in layout.Geometry.PressureGates)
            {
                if (gate.Locked) continue;
                foreach (var collider in gate.GetComponentsInChildren<Collider>())
                    if (collider.name == "Druckschott / Schiebefluegel")
                        markups.Add(new NavMeshBuildMarkup { root = collider.transform, ignoreFromBuild = true });
            }
            var bounds = new Bounds(layout.transform.position + Vector3.up * 2, new Vector3(220, 14, 220));
            NavMeshBuilder.CollectSources(bounds, 1 << 25, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            foreach (var vehicle in vehicles) vehicle.Agent.enabled = false;
            navigation.Remove(); if (data != null) Destroy(data);
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("Fahrprobe: Navigationsaufbau fehlgeschlagen.");
            navigation = NavMesh.AddNavMeshData(data);
            foreach (var vehicle in vehicles)
            {
                var position = Snap(vehicle.Agent.transform.position);
                vehicle.Agent.enabled = true; vehicle.Agent.Warp(position);
                SetDestination(vehicle);
            }
            rebuilding = false; dirty = false;
        }
        private Vector3 Snap(Vector3 desired)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = settings.agentTypeID, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(desired, out var hit, 1.3f, filter)
                || Mathf.Abs(layout.transform.InverseTransformPoint(hit.position).y) > .2f
                || !layout.ContainsPosition(hit.position + Vector3.up * .04f))
                throw new InvalidOperationException("Kein sicherer Fahrprobenpunkt: " + desired);
            return hit.position;
        }
        private void BuildVehicles()
        {
            var prefab = Resources.Load<GameObject>("IndoorDroneI75/I75Traffic");
            if (prefab == null) throw new InvalidOperationException("I-75-Fahrprobenmodell fehlt; IndoorDroneTrafficBuild vorbereiten.");
            float forward=StationRingLanes.Center(0),back=StationRingLanes.Center(3);
            var hall = new[] { At(forward,30), Room("drone-workshop",-.7f,40), Room("drone-workshop",-.7f,47.7f),
                Room("drone-workshop",.7f,47.7f), Room("drone-workshop",.7f,40), At(back,30), At(back,10), At(forward,10) };
            var inner = new[] { At(forward,30), Room("internal-workshop",-.7f,27), Room("internal-workshop",-.7f,24.6f),
                Room("internal-workshop",.7f,24.6f), Room("internal-workshop",.7f,27), At(forward,30), At(forward,45), At(back,45), At(back,30) };
            var command = new[] { At(forward,0), Room("command-room",-.7f,40), Room("command-room",-.7f,43.2f),
                Room("command-room",.7f,43.2f), Room("command-room",.7f,40), At(back,0), At(back,-20), At(forward,-20) };
            var corridor = new List<Vector3>();
            for (int angle=-60;angle<=60;angle+=10) corridor.Add(At(forward,angle));
            for (int angle=60;angle>=-60;angle-=10) corridor.Add(At(back,angle));
            for (int index=0;index<10;index++)
            {
                string route = index<3 ? "Außendrohnenwerkstatt" : index<5 ? "Interne Drohnenwerkstatt"
                    : index<7 ? "Kommandozentrale" : "Ringgang";
                var requested = index<3 ? hall : index<5 ? inner : index<7 ? command : corridor.ToArray();
                var stops = Array.ConvertAll(requested, Snap);
                int first = index<3 ? index*3 : index<5 ? (index==3?1:3) : index<7 ? (index-5)*3 : index==7?2:index==8?1:15;
                var node = new GameObject("I-"+(index+1).ToString("00")+" / Fahrprobe / "+route);
                node.SetActive(false); // Configure the custom agent type before its first OnEnable.
                node.layer = 26; node.transform.SetParent(transform, true); node.transform.position = stops[first];
                Instantiate(prefab, node.transform, false);
                var body = node.AddComponent<CapsuleCollider>(); body.radius=.28f; body.height=.6f; body.center=Vector3.up*.3f;
                var rigid = node.AddComponent<Rigidbody>(); rigid.isKinematic=true; rigid.useGravity=false;
                var agent = node.AddComponent<NavMeshAgent>(); agent.enabled=false; agent.agentTypeID=settings.agentTypeID;
                agent.radius=.48f; agent.height=.7f; agent.baseOffset=.08f; agent.speed=1;
                agent.acceleration=2; agent.angularSpeed=140; agent.stoppingDistance=.18f;
                agent.autoBraking=true; agent.autoTraverseOffMeshLink=false;
                agent.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.avoidancePriority=25+index*5; agent.enabled=true; node.SetActive(true);
                if(!agent.Warp(stops[first])||!agent.isOnNavMesh)
                    throw new InvalidOperationException("Fahrprobe: Agent konnte nicht sicher aufgesetzt werden / "+index);
                var vehicle = new Vehicle { Id="I-"+(index+1).ToString("00"), Route=route, Agent=agent,
                    Stops=stops, NextStop=(first+1)%stops.Length, previous=agent.transform.position, Status="Fahrprobe",
                    CruiseSpeed=index==7?.72f:index==8?1.4f:.92f+(index%3)*.11f };
                vehicles.Add(vehicle); SetDestination(vehicle);
            }
        }
        private void SetDestination(Vehicle vehicle)
        {
            if (!vehicle.Agent.isOnNavMesh) return;
            bool wasGuided=vehicle.Guided;
            var target=vehicle.Stops[vehicle.NextStop];
            var from=layout.transform.InverseTransformPoint(vehicle.Stops[(vehicle.NextStop+vehicle.Stops.Length-1)%vehicle.Stops.Length]);
            var to=layout.transform.InverseTransformPoint(target);
            float turn=Mathf.DeltaAngle(StationRingLanes.Angle(from),StationRingLanes.Angle(to));
            vehicle.Guided=StationRingLanes.OnRoad(from)&&StationRingLanes.OnRoad(to)
                &&Mathf.Abs(StationRingLanes.Radius(from)-StationRingLanes.Radius(to))<.3f&&Mathf.Abs(turn)>3;
            vehicle.Direction=vehicle.Guided?(turn>0?1:-1):0;
            // Free-moving room traffic must yield to rail-bound actors, whose
            // position is controlled by the guide instead of native side-stepping.
            vehicle.Agent.avoidancePriority=vehicle.Guided?0:25+vehicles.IndexOf(vehicle)*5;
            vehicle.Agent.updatePosition=!vehicle.Guided;vehicle.Agent.updateRotation=!vehicle.Guided;
            if(wasGuided&&!vehicle.Guided)vehicle.Agent.Warp(Snap(vehicle.Agent.transform.position));
            if(!vehicle.Guided){vehicle.Passing=false;vehicle.passTarget=null;}
            if(vehicle.Guided)
            {
                var position=layout.transform.InverseTransformPoint(vehicle.Agent.transform.position);
                float remaining=Mathf.DeltaAngle(StationRingLanes.Angle(position),StationRingLanes.Angle(to))*vehicle.Direction;
                bool continues=Continues(vehicle);
                if(!continues&&remaining<6&&(vehicle.Junction<0||vehicle.CrossingGranted)){vehicle.Passing=false;vehicle.passTarget=null;}
                int lane=vehicle.Passing?StationRingLanes.PassingLane(vehicle.Direction):StationRingLanes.NormalLane(vehicle.Direction);
                float step=Mathf.Min(Mathf.Max(remaining,0),5);
                target=Snap(At(StationRingLanes.Center(lane),StationRingLanes.Angle(position)+vehicle.Direction*step));
                if(!continues&&remaining<.6f)target=vehicle.Stops[vehicle.NextStop];
                vehicle.Agent.autoBraking=!continues&&remaining<3;
            }
            else vehicle.Agent.autoBraking=true;
            var path = new NavMeshPath();
            if (!vehicle.Agent.CalculatePath(target, path) || path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Fahrprobe: unvollständiger Weg / " + vehicle.Id + " / " + vehicle.NextStop);
            foreach (var point in path.corners)
                if (!layout.ContainsPosition(point + Vector3.up*.04f) || Mathf.Abs(layout.transform.InverseTransformPoint(point).y)>.2f)
                    throw new InvalidOperationException("Fahrprobe: Weg verlässt den befahrbaren Stationsboden / " + vehicle.Id);
            vehicle.Agent.SetPath(path);
            vehicle.crossingDone=false;
        }
        private float Ahead(Vehicle self,Vehicle other)
        {
            var a=layout.transform.InverseTransformPoint(self.Agent.transform.position);
            var b=layout.transform.InverseTransformPoint(other.Agent.transform.position);
            return Mathf.DeltaAngle(StationRingLanes.Angle(a),StationRingLanes.Angle(b))*Mathf.Deg2Rad*StationRingLanes.Radius(a)*self.Direction;
        }
        private bool Continues(Vehicle self)
        {
            var a=layout.transform.InverseTransformPoint(self.Stops[self.NextStop]);
            var b=layout.transform.InverseTransformPoint(self.Stops[(self.NextStop+1)%self.Stops.Length]);
            float turn=Mathf.DeltaAngle(StationRingLanes.Angle(a),StationRingLanes.Angle(b));
            return StationRingLanes.OnRoad(b)&&Mathf.Abs(StationRingLanes.Radius(a)-StationRingLanes.Radius(b))<.3f
                &&turn*self.Direction>3;
        }
        private bool LaneClear(Vehicle self,int lane)
        {
            foreach(var other in vehicles)
            {
                if(other==self)continue;
                var p=layout.transform.InverseTransformPoint(other.Agent.transform.position);
                if(Mathf.Abs(StationRingLanes.Radius(p)-StationRingLanes.Center(lane))>.9f)continue;
                float ahead=Ahead(self,other);if(ahead>-2.2f&&ahead<4)return false;
            }
            return true;
        }
        private void ChoosePassingLane(Vehicle self)
        {
            if(!self.Guided)return;
            // Keep a stable lane while traversing a reserved crossing.
            if(self.Junction>=0)return;
            if(self.Passing)
            {
                if(self.passTarget!=null)
                {
                    bool passed=Ahead(self,self.passTarget)<-2.2f;
                    var p=layout.transform.InverseTransformPoint(self.passTarget.Agent.transform.position);
                    bool leftLane=Mathf.Abs(StationRingLanes.Radius(p)-StationRingLanes.Center(StationRingLanes.NormalLane(self.Direction)))>.9f;
                    if((passed||leftLane)&&LaneClear(self,StationRingLanes.NormalLane(self.Direction)))
                    {self.Passing=false;self.passTarget=null;if(passed)self.Overtakes++;}
                }
                return;
            }
            foreach(var other in vehicles)
            {
                if(other==self||other.Passing)continue;
                if(other.Guided&&(other.Direction!=self.Direction||other.CruiseSpeed>self.CruiseSpeed*.86f))continue;
                if(!other.Guided)
                {
                    var p=layout.transform.InverseTransformPoint(other.Agent.transform.position);
                    if(Mathf.Abs(StationRingLanes.Radius(p)-StationRingLanes.Center(StationRingLanes.NormalLane(self.Direction)))>.7f)continue;
                }
                float ahead=Ahead(self,other);
                if(ahead<1||ahead>6||!LaneClear(self,StationRingLanes.PassingLane(self.Direction)))continue;
                self.Passing=true;self.passTarget=other;return;
            }
        }
        private bool GuideStepClear(Vehicle self,Vector3 from,Vector3 to)
        {
            foreach(var other in vehicles)
            {
                if(other==self)continue;
                var predicted=other.Agent.transform.position+(other.Guided?Vector3.zero:other.Agent.velocity*Time.deltaTime);
                if(Vector3.Distance(to,other.Agent.transform.position)<.95f||Vector3.Distance(to,predicted)<.95f)return false;
            }
            var delta=to-from;
            return delta.sqrMagnitude<.000001f||!Physics.SphereCast(from+Vector3.up*.3f,.3f,delta.normalized,out _,delta.magnitude,1<<25,QueryTriggerInteraction.Ignore);
        }
        private void DriveGuide(Vehicle vehicle,bool stop)
        {
            var agent=vehicle.Agent;agent.isStopped=true;
            var position=agent.transform.position;
            if(stop){agent.nextPosition=position;return;}
            ChoosePassingLane(vehicle);
            var local=layout.transform.InverseTransformPoint(position);
            var target=layout.transform.InverseTransformPoint(vehicle.Stops[vehicle.NextStop]);
            float angle=StationRingLanes.Angle(local),radius=StationRingLanes.Radius(local);
            float remaining=Mathf.Max(0,Mathf.DeltaAngle(angle,StationRingLanes.Angle(target))*vehicle.Direction);
            if(!Continues(vehicle)&&remaining<6&&(vehicle.Junction<0||vehicle.CrossingGranted)){vehicle.Passing=false;vehicle.passTarget=null;}
            int lane=vehicle.Passing?StationRingLanes.PassingLane(vehicle.Direction):StationRingLanes.NormalLane(vehicle.Direction);
            float rate=Mathf.Min(scenario.SimulationRate,100),seconds=Time.deltaTime*rate;
            float nextRadius=Mathf.MoveTowards(radius,StationRingLanes.Center(lane),.8f*seconds);
            float advance=Mathf.Min(remaining,vehicle.CruiseSpeed*seconds/Mathf.Max(1,radius)*Mathf.Rad2Deg);
            Vector3 candidate=At(nextRadius,angle+vehicle.Direction*advance);candidate.y=position.y;
            if(!GuideStepClear(vehicle,position,candidate))
            {
                // A following vehicle can change lane while waiting for forward clearance.
                candidate=At(nextRadius,angle);candidate.y=position.y;
                if(!GuideStepClear(vehicle,position,candidate)){vehicle.Status="Wartet";agent.nextPosition=position;return;}
            }
            var movement=candidate-position;
            agent.nextPosition=candidate;agent.transform.position=candidate;
            if(movement.sqrMagnitude>.00001f)
                agent.transform.rotation=Quaternion.RotateTowards(agent.transform.rotation,Quaternion.LookRotation(movement),140*seconds);
            vehicle.Status=vehicle.Passing?"Überholt":"Magnetspur";
        }
        private void Update()
        {
            if (!Ready || rebuilding || !string.IsNullOrEmpty(Failure)) return;
            try
            {
                if (dirty) RefreshNavigation();
                bool paused = StartMenu.IsOpen || IntroSequence.BlocksGameplay || SettingsMenu.PausesSimulation
                    || CommandRoomPlanner.BlocksInput || scenario.SimulationRate<=0;
                if(!paused) CoordinateCrossings();
                foreach (var vehicle in vehicles)
                {
                    var agent=vehicle.Agent; if (!agent.isOnNavMesh) continue;
                    bool stop=paused || CrossingHold(vehicle);
                    if (!stop && StationInteriorMode.IsInside)
                    {
                        var delta=agent.transform.position-StationInteriorMode.Current.FeetPosition; delta.y=0;
                        if (delta.sqrMagnitude<1.0f) stop=true;
                    }
                    foreach (var gate in layout.Geometry.PressureGates)
                    {
                        var p=gate.transform.InverseTransformPoint(agent.transform.position);
                        if (Mathf.Abs(p.x)>gate.ClearWidth*.5f+.35f || Mathf.Abs(p.z)>2.7f) continue;
                        if (gate.Locked) { stop=true; continue; }
                        if (!paused) gate.Request(true,agent.transform.position-Vector3.up*.08f);
                        if (gate.Opening<.98f) stop=true;
                    }
                    agent.isStopped=stop||vehicle.Guided;
                    agent.speed=vehicle.CruiseSpeed*Mathf.Min(scenario.SimulationRate,100);
                    vehicle.Distance+=Vector3.Distance(vehicle.previous,agent.transform.position);
                    vehicle.previous=agent.transform.position;
                    vehicle.Status=stop ? "Wartet" : vehicle.Passing?"Überholt":"Fährt";
                    if(vehicle.Guided)
                    {
                        DriveGuide(vehicle,stop);
                        if(stop)continue;
                        var delta=vehicle.Stops[vehicle.NextStop]-agent.transform.position;delta.y=0;
                        var here=layout.transform.InverseTransformPoint(agent.transform.position);
                        var end=layout.transform.InverseTransformPoint(vehicle.Stops[vehicle.NextStop]);
                        float angleLeft=Mathf.DeltaAngle(StationRingLanes.Angle(here),StationRingLanes.Angle(end))*vehicle.Direction;
                        if(delta.magnitude>.30f&&(!Continues(vehicle)||angleLeft>.45f))continue;
                    }
                    else
                    {
                        // A junction is a transfer area, not a parking spot on the
                        // through lane. Hand over before native avoidance waits for
                        // an exact centre point occupied by passing traffic.
                        var endpoint=layout.transform.InverseTransformPoint(vehicle.Stops[vehicle.NextStop]);
                        float arrival=StationRingLanes.OnRoad(endpoint)?1.15f:agent.stoppingDistance+.06f;
                        if(stop||agent.pathPending||agent.remainingDistance>arrival)continue;
                    }
                    vehicle.StopsReached++;
                    var local=layout.transform.InverseTransformPoint(agent.transform.position);
                    float radius=new Vector2(local.x,local.z).magnitude;
                    if (radius>39 || radius<28.5f) vehicle.RoomVisits++;
                    vehicle.NextStop=(vehicle.NextStop+1)%vehicle.Stops.Length; SetDestination(vehicle);
                }
            }
            catch(Exception error)
            {
                Failure=error.ToString(); Debug.LogException(error);
                foreach(var vehicle in vehicles) if(vehicle.Agent.isOnNavMesh) vehicle.Agent.isStopped=true;
            }
        }
        private void OnDestroy()
        {
            if (scenario!=null && scenario.Operations!=null)
                foreach(var room in scenario.Operations.RoomPlans) if(room!=null) room.Changed-=PlanChanged;
            navigation.Remove(); if(data!=null) Destroy(data);
            if(settings.agentTypeID!=0) NavMesh.RemoveSettings(settings.agentTypeID);
        }
    }
}

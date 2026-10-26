using UnityEngine;

namespace SpaceMiner
{
    // Campaign facade over the same metre-scale geometry used by the ring walkthrough.
    public sealed class StationInteriorLayout : MonoBehaviour
    {
        public const int Revision = 15;
        public Transform Habitat { get; private set; }
        public Transform Ring { get; private set; }
        public Transform Console { get; private set; }
        public Transform RepairDrone { get; private set; }
        public StationAirlock Airlock { get; private set; }
        public CommandRoomLayout RoomPlan { get; internal set; }
        public Vector3 SpawnPosition
        {
            get
            {
                if (RoomPlan == null) return transform.TransformPoint(new Vector3(0, .05f, RingWalkthrough.StartConsoleRadius - 2.8f));
                return TryCommandRoomSpawn(out var position, out _) ? position : transform.TransformPoint(new Vector3(0, .05f, 40));
            }
        }
        public float SpawnYaw => RoomPlan != null && TryCommandRoomSpawn(out _, out float heading) ? heading : transform.eulerAngles.y;
        internal RingWalkthrough Geometry { get; private set; }
        private bool TryCommandRoomSpawn(out Vector3 position, out float heading)
        {
            position = Vector3.zero; heading = transform.eulerAngles.y;
            var item = RoomPlan?.Get("main-console");
            var root = RoomPlan?.BoundRoot("main-console");
            if (item == null || root == null) return false;
            position = root.TransformPoint(new Vector3(0, .05f, -item.size.z * .5f - 1.1f));
            if (!HasStandingClearance(position) || OverlapsCommandBox(position)) return false;
            heading = root.eulerAngles.y;
            return true;
        }
        public void Build(StationVisual station)
        {
            if (Habitat != null) return;
            Geometry = new RingWalkthrough(transform);
            Ring = Geometry.Root.Find("Ring 1");
            Habitat = new GameObject("Internal drone workshop anchor").transform;
            Habitat.SetParent(transform, false);
            Habitat.localPosition = RingWalkthrough.At(24.5f, 30);
            Habitat.localRotation = Quaternion.Euler(0, 30, 0);
            Console = new GameObject("Command console interaction").transform;
            Console.SetParent(transform, false); Console.localPosition = new Vector3(0, 0, RingWalkthrough.StartConsoleRadius - 2);
            Airlock = Geometry.BayAirlock;
            var prefab=Resources.Load<GameObject>("IndoorDroneI75/I75Traffic");
            if(prefab==null)throw new System.InvalidOperationException("R03-Innendrohnenmodell fehlt; IndoorDroneTrafficBuild vorbereiten.");
            RepairDrone = new GameObject("Internal helper R-01").transform;
            RepairDrone.name = "Repair drone R-01 - offline"; RepairDrone.gameObject.layer = 25;
            RepairDrone.SetParent(Habitat, false); RepairDrone.localPosition = new Vector3(-3, .6f, 0);
            var helperVisual=Object.Instantiate(prefab,RepairDrone,false);
            // Existing work paths use a body-centre pivot; traffic uses a floor pivot.
            helperVisual.transform.localPosition=Vector3.down*IndoorDroneVisual.Height*.5f;
            helperVisual.GetComponent<IndoorDroneVisual>().SetOperational(false);
            foreach(var node in RepairDrone.GetComponentsInChildren<Transform>())node.gameObject.layer=25;
            foreach(var collider in RepairDrone.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var renderer in RepairDrone.GetComponentsInChildren<Renderer>())
                if(renderer.sharedMaterial!=null && renderer.sharedMaterial.name=="Drone status")
                {var block=new MaterialPropertyBlock();block.SetColor("_Color",Color.gray);block.SetColor("_EmissionColor",Color.black);renderer.SetPropertyBlock(block);}
            station.InstallStartLayout(this);
            CampaignDroneFleet.Install(station,this);
            StationOperations.Install(station,this);
            Physics.SyncTransforms();
            StationIndoorTraffic.Install(GetComponentInParent<WaterScenario>(),this);
        }
        // Same authored start geometry, without installing a campaign or reading a save.
        internal void BuildModelPreview(StationVisual station)
        {
            Geometry = new RingWalkthrough(transform);
            Ring = Geometry.Root.Find("Ring 1");
            station.InstallStartLayout(this);
        }
        // Floors of the built start modules only; future ports are capped by the ring wall.
        public bool ContainsPosition(Vector3 world)
        {
            if(GetComponentInParent<WaterScenario>()?.Operations?.ContainsExpansion(world)==true)return true;
            Vector3 p = transform.InverseTransformPoint(world);
            if (p.y < -.2f || p.y > .5f) return false;
            bool repairedBatteryAxis = GetComponentInParent<WaterScenario>()?.Operations?.Element("sector-3")?.condition >= .6f;
            if (Geometry != null && RingWalkthrough.ContainsBatteryBayFloor(p, repairedBatteryAxis)) return true;
            float r = new Vector2(p.x, p.z).magnitude;
            float a = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
            int sector = (Mathf.RoundToInt(a / 30) + 12) % 12;
            if (r >= 29.4f && r <= 37.6f) return !RingWalkthrough.DamagedRingSector(sector)||GetComponentInParent<WaterScenario>()?.Operations?.Element("sector-"+sector)?.condition>=.6f;
            float delta = Mathf.Abs(Mathf.DeltaAngle(a, sector * 30));
            if (sector==7 && r>=37.6f) return Mathf.Abs(Mathf.DeltaAngle(a,RingWalkthrough.StorageAngle(0)))<4.3f && r<53.5f;
            if (r >= 37.6f && delta < 13 && Geometry.OuterStages(sector) > 0)
                return r < Geometry.BuiltOuterRadius(sector) - .4f;
            if (Geometry.HasInnerArea(sector) && delta < 13 && r > RingWalkthrough.StartInnerRadius + .4f && r < 29.4f) return true;
            // The maintenance annulus and four 3-m-clear service corridors stay outside the sealed reactor capsule.
            if (r >= RingWalkthrough.ReactorRadius + .75f && r < RingWalkthrough.ServiceOuterRadius - .35f) return true;
            float nearestAxis = Mathf.Round(a / 90) * 90;
            Vector3 serviceLocal = Quaternion.Euler(0, -nearestAxis, 0) * p;
            int axis = (Mathf.RoundToInt(nearestAxis / 90) + 4) % 4;
            return Mathf.Abs(serviceLocal.x) <= 1.18f && RingWalkthrough.HasServiceFloor(axis, serviceLocal.z);
        }
        internal bool HasStandingClearance(Vector3 feet) => ContainsPosition(feet)
            && !Physics.CheckCapsule(feet + Vector3.up * .33f, feet + Vector3.up * 1.5f, .3f, 1 << 25, QueryTriggerInteraction.Ignore);
        internal bool OverlapsCommandBox(Vector3 feet)
        {
            foreach (var collider in Physics.OverlapCapsule(feet + Vector3.up * .33f, feet + Vector3.up * 1.5f,
                .27f, 1 << 25, QueryTriggerInteraction.Ignore))
                if (collider.GetComponentInParent<CommandRoomBlock>() != null) return true;
            return false;
        }
        private void OnDestroy() => Geometry?.Dispose();
    }
}

using UnityEngine;

namespace SpaceMiner
{
    // Campaign facade over the same metre-scale geometry used by the ring walkthrough.
    public sealed class StationInteriorLayout : MonoBehaviour
    {
        public const int Revision = 3;
        public Transform Habitat { get; private set; }
        public Transform Ring { get; private set; }
        public Transform Console { get; private set; }
        public Transform RepairDrone { get; private set; }
        public StationAirlock Airlock { get; private set; }
        public Vector3 SpawnPosition => transform.TransformPoint(new Vector3(0, .05f, 69.2f));
        public float SpawnYaw => transform.eulerAngles.y;
        internal RingWalkthrough Geometry { get; private set; }
        public void Build(StationVisual station)
        {
            if (Habitat != null) return;
            Geometry = new RingWalkthrough(transform);
            Ring = Geometry.Root.Find("Ring 1");
            Habitat = new GameObject("Habitat module").transform;
            Habitat.SetParent(transform, false);
            Habitat.localPosition = RingWalkthrough.At(46, 330);
            Habitat.localRotation = Quaternion.Euler(0, 330, 0);
            Console = new GameObject("Command console interaction").transform;
            Console.SetParent(transform, false); Console.localPosition = new Vector3(0, 0, 70);
            Airlock = Habitat.gameObject.AddComponent<StationAirlock>(); Airlock.ConfigureOpen(Habitat);
            RepairDrone = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            RepairDrone.name = "Repair drone R-01 - offline"; RepairDrone.gameObject.layer = 25;
            RepairDrone.SetParent(Habitat, false); RepairDrone.localPosition = new Vector3(-3, .6f, 0);
            RepairDrone.localScale = new Vector3(.38f, .22f, .34f);
            station.InstallStartLayout(this);
            Physics.SyncTransforms();
        }
        // Floors of the built start modules only; future ports are capped by the ring wall.
        public bool ContainsPosition(Vector3 world)
        {
            Vector3 p = transform.InverseTransformPoint(world);
            if (p.y < -.2f || p.y > .5f) return false;
            float r = new Vector2(p.x, p.z).magnitude;
            if (r >= 29.4f && r <= 37.6f) return true;
            float a = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
            int sector = (Mathf.RoundToInt(a / 30) + 12) % 12;
            float delta = Mathf.Abs(Mathf.DeltaAngle(a, sector * 30));
            if (r >= 37.6f && delta < 13 && Geometry.OuterStages(sector) > 0)
                return r < 38 + 16 * Geometry.OuterStages(sector) - .4f;
            return (sector == 1 || sector == 11) && delta < 11.5f && r > 9.5f && r < 29.4f;
        }
        internal bool HasStandingClearance(Vector3 feet) => ContainsPosition(feet)
            && !Physics.CheckCapsule(feet + Vector3.up * .33f, feet + Vector3.up * 1.5f, .3f, 1 << 25, QueryTriggerInteraction.Ignore);
        private void OnDestroy() => Geometry?.Dispose();
    }
}

using UnityEngine;

namespace SpaceMiner
{
    public enum AirlockPhase { Idle, OpeningEntry, AwaitingEntry, ClosingEntry, Equalizing, OpeningExit, AwaitingExit, ClosingExit }

    [DefaultExecutionOrder(100)]
    public sealed class StationAirlock : MonoBehaviour
    {
        [System.Serializable] public sealed class SavedState
        {
            public AirlockPhase phase;
            public bool towardsRing;
            public float habitatOpen, ringOpen, equalizing;
        }
        public SavedState CaptureState() => new SavedState { phase=Phase, towardsRing=TowardsRing,
            habitatOpen=progress[0], ringOpen=progress[1], equalizing=equalizing };
        public static bool IsValid(SavedState state) => state != null && System.Enum.IsDefined(typeof(AirlockPhase), state.phase)
            && state.habitatOpen>=0 && state.habitatOpen<=1 && state.ringOpen>=0 && state.ringOpen<=1
            && !(state.habitatOpen>0 && state.ringOpen>0) && state.equalizing>=0 && state.equalizing<=1.3f
            && (state.phase!=AirlockPhase.Idle && state.phase!=AirlockPhase.Equalizing || state.habitatOpen==0 && state.ringOpen==0);
        public void RestoreState(SavedState state)
        {
            Phase=state.phase;TowardsRing=state.towardsRing;
            progress[0]=state.habitatOpen;progress[1]=state.ringOpen;equalizing=state.equalizing;Apply();
        }
        public void RecoverLegacyOccupant(Vector3 feet)
        {
            ResetClosed();
            Vector3 p=habitat.InverseTransformPoint(feet);
            if(Mathf.Abs(p.x)>1.5f || p.z>HabitatDoorZ+.6f || p.z<RingDoorZ-.6f)return;
            // Old version-1 saves did not retain doors. Let an occupant leave safely.
            TowardsRing=true;
            Phase=Mathf.Abs(p.z-HabitatDoorZ)<.85f ? AirlockPhase.OpeningEntry : AirlockPhase.OpeningExit;
        }
        public const float HabitatDoorZ = -5.1f, RingDoorZ = -10.6f;
        public AirlockPhase Phase { get; private set; }
        public float HabitatOpen => progress[0];
        public float RingOpen => progress[1];
        public bool TowardsRing { get; private set; }
        public string Status => Phase == AirlockPhase.Idle ? "Schleuse bereit"
            : Phase == AirlockPhase.Equalizing ? "Druckausgleich …"
            : Phase == AirlockPhase.AwaitingEntry ? "Bitte in die Schleuse treten"
            : Phase == AirlockPhase.AwaitingExit ? (TowardsRing ? "Zum Stationsring gehen" : "Zum Wohnmodul gehen")
            : "Elektrische Schiebetür bewegt sich";
        private readonly Transform[,] leaves = new Transform[2, 2];
        private readonly float[] progress = new float[2];
        private Transform habitat;
        private Material indicator;
        private float equalizing;
        private int Entry => TowardsRing ? 0 : 1;
        private int Exit => 1 - Entry;
        private float DoorZ(int door) => door == 0 ? HabitatDoorZ : RingDoorZ;

        public void Configure(Transform room, Transform firstLeft, Transform firstRight, Transform secondLeft, Transform secondRight, Material statusMaterial)
        {
            habitat = room; indicator = statusMaterial;
            leaves[0, 0] = firstLeft; leaves[0, 1] = firstRight;
            leaves[1, 0] = secondLeft; leaves[1, 1] = secondRight;
            ResetClosed();
        }
        public bool CanRequest(Vector3 feet)
        {
            if (habitat == null || Phase != AirlockPhase.Idle) return false;
            Vector3 p = habitat.InverseTransformPoint(feet);
            return Mathf.Abs(p.x) < 1.35f && Mathf.Abs(p.y) < .4f
                && ((p.z > HabitatDoorZ && p.z - HabitatDoorZ < 1.9f)
                    || (p.z < RingDoorZ && RingDoorZ - p.z < 1.9f));
        }
        public bool Request(Vector3 feet)
        {
            if (!CanRequest(feet)) return false;
            TowardsRing = habitat.InverseTransformPoint(feet).z > HabitatDoorZ;
            Phase = AirlockPhase.OpeningEntry;
            Apply();
            return true;
        }
        public void ResetClosed()
        {
            Phase = AirlockPhase.Idle; progress[0] = progress[1] = 0; equalizing = 0;
            Apply();
        }
        private void Update()
        {
            if (StationInteriorMode.IsInside && !SettingsMenu.BlocksInput)
                Advance(Time.unscaledDeltaTime, StationInteriorMode.Current.FeetPosition);
        }
        public void Advance(float seconds, Vector3 feet)
        {
            if (habitat == null || seconds <= 0 || Phase == AirlockPhase.Idle) return;
            Vector3 p = habitat.InverseTransformPoint(feet);
            bool nearEntry = Mathf.Abs(p.z - DoorZ(Entry)) < .85f && Mathf.Abs(p.x) < 1.5f;
            bool nearExit = Mathf.Abs(p.z - DoorZ(Exit)) < .85f && Mathf.Abs(p.x) < 1.5f;
            switch (Phase)
            {
                case AirlockPhase.OpeningEntry:
                    progress[Exit] = 0;
                    progress[Entry] = Mathf.MoveTowards(progress[Entry], 1, seconds);
                    if (progress[Entry] == 1) Phase = AirlockPhase.AwaitingEntry;
                    break;
                case AirlockPhase.AwaitingEntry:
                    bool inChamber = p.z < HabitatDoorZ - .85f && p.z > RingDoorZ + .85f && Mathf.Abs(p.x) < 1.2f;
                    if (inChamber) Phase = AirlockPhase.ClosingEntry;
                    break;
                case AirlockPhase.ClosingEntry:
                    if (nearEntry) { Phase = AirlockPhase.OpeningEntry; break; }
                    progress[Entry] = Mathf.MoveTowards(progress[Entry], 0, seconds);
                    if (progress[Entry] == 0) { equalizing = 0; Phase = AirlockPhase.Equalizing; }
                    break;
                case AirlockPhase.Equalizing:
                    equalizing = Mathf.Min(1.2f, equalizing + seconds);
                    if (equalizing >= 1.2f) Phase = AirlockPhase.OpeningExit;
                    break;
                case AirlockPhase.OpeningExit:
                    progress[Entry] = 0;
                    progress[Exit] = Mathf.MoveTowards(progress[Exit], 1, seconds);
                    if (progress[Exit] == 1) Phase = AirlockPhase.AwaitingExit;
                    break;
                case AirlockPhase.AwaitingExit:
                    if (TowardsRing ? p.z < RingDoorZ - .85f : p.z > HabitatDoorZ + .85f)
                        Phase = AirlockPhase.ClosingExit;
                    break;
                case AirlockPhase.ClosingExit:
                    if (nearExit) { Phase = AirlockPhase.OpeningExit; break; }
                    progress[Exit] = Mathf.MoveTowards(progress[Exit], 0, seconds);
                    if (progress[Exit] == 0) Phase = AirlockPhase.Idle;
                    break;
            }
            Apply();
        }
        private void Apply()
        {
            for (int door = 0; door < 2; door++) for (int side = 0; side < 2; side++)
            {
                var leaf = leaves[door, side]; if (leaf == null) continue;
                float sign = side == 0 ? -1 : 1;
                leaf.localPosition = new Vector3(sign * (.52f + Mathf.SmoothStep(0, 1, progress[door]) * 1.15f), 1.2f, DoorZ(door));
            }
            if (indicator != null)
            {
                Color color = Phase == AirlockPhase.Equalizing ? new Color(1, .42f, .08f) : new Color(.08f, .7f, .65f);
                indicator.color = color; indicator.SetColor("_EmissionColor", color * .7f);
            }
            // The character moves after this component; moving door colliders must already match their meshes.
            Physics.SyncTransforms();
        }
    }
}

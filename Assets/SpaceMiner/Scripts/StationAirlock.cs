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
            public float habitatOpen, ringOpen, equalizing,occupantHalfLength;
        }
        public SavedState CaptureState() => new SavedState { phase=Phase, towardsRing=TowardsRing,
            habitatOpen=progress[0], ringOpen=progress[1], equalizing=equalizing,occupantHalfLength=occupantHalfLength };
        public static bool IsValid(SavedState state) => state != null && System.Enum.IsDefined(typeof(AirlockPhase), state.phase)
            && state.habitatOpen>=0 && state.habitatOpen<=1 && state.ringOpen>=0 && state.ringOpen<=1
            && !(state.habitatOpen>0 && state.ringOpen>0) && state.equalizing>=0 && state.equalizing<=1.3f && state.occupantHalfLength>=0 && state.occupantHalfLength<=3.85f
            && (state.phase!=AirlockPhase.Idle && state.phase!=AirlockPhase.Equalizing || state.habitatOpen==0 && state.ringOpen==0);
        public void RestoreState(SavedState state)
        {
            if (PermanentlyOpen) { ResetClosed(); return; }
            Phase=state.phase;TowardsRing=state.towardsRing;
            progress[0]=state.habitatOpen;progress[1]=state.ringOpen;equalizing=state.equalizing;occupantHalfLength=state.occupantHalfLength;Apply();
        }
        public void RecoverLegacyOccupant(Vector3 feet)
        {
            ResetClosed();
            if (PermanentlyOpen) return;
            Vector3 p=habitat.InverseTransformPoint(feet);
            if(Mathf.Abs(p.x)>ChamberHalfWidth-.1f || p.z>EntranceZ+.6f || p.z<ExitZ-.6f)return;
            // Old version-1 saves did not retain doors. Let an occupant leave safely.
            TowardsRing=true;
            Phase=Mathf.Abs(p.z-EntranceZ)<.85f ? AirlockPhase.OpeningEntry : AirlockPhase.OpeningExit;
        }
        public const float HabitatDoorZ = -5.1f, RingDoorZ = -10.6f;
        public float EntranceZ { get; private set; } = HabitatDoorZ;
        public float ExitZ { get; private set; } = RingDoorZ;
        public float ClearWidth { get; private set; } = 2.08f;
        public float ClearHeight { get; private set; } = 2.4f;
        public float ChamberHalfWidth { get; private set; } = 1.6f;
        private float occupantHalfLength;
        internal bool DroneTransit => occupantHalfLength>0 && Phase!=AirlockPhase.Idle;
        internal void ConfigureSize(float firstDoorZ,float secondDoorZ,float width,float height,float wallHalfWidth)
        {
            if(firstDoorZ<=secondDoorZ || width<=0 || height<=0 || wallHalfWidth<width*.5f)
                throw new System.ArgumentException("Invalid pressure chamber dimensions");
            EntranceZ=firstDoorZ;ExitZ=secondDoorZ;ClearWidth=width;ClearHeight=height;ChamberHalfWidth=wallHalfWidth;Apply();
        }
        public AirlockPhase Phase { get; private set; }
        public bool PermanentlyOpen { get; private set; }
        public void ConfigureOpen(Transform room)
        {
            habitat = room; PermanentlyOpen = true; ResetClosed();
        }
        public float HabitatOpen => progress[0];
        public float RingOpen => progress[1];
        public bool TowardsRing { get; private set; }
        private string firstLabel="Wohnmodul",secondLabel="Stationsring";
        internal void SetLabels(string first,string second) { firstLabel=first;secondLabel=second; }
        internal bool Nearby(Vector3 feet)
        { var p=habitat.InverseTransformPoint(feet);return Mathf.Abs(p.x)<ChamberHalfWidth+.4f && p.z>ExitZ-2.5f && p.z<EntranceZ+2.5f; }
        public string Status => PermanentlyOpen ? "Zugänge offen" : Phase == AirlockPhase.Idle ? "Schleuse bereit"
            : Phase == AirlockPhase.Equalizing ? "Druckausgleich …"
            : Phase == AirlockPhase.AwaitingEntry ? "Bitte in die Schleuse treten"
            : Phase == AirlockPhase.AwaitingExit ? "Weiter: "+(TowardsRing ? secondLabel : firstLabel)
            : "Elektrische Schiebetür bewegt sich";
        private readonly Transform[,] leaves = new Transform[2, 2];
        private readonly float[] progress = new float[2];
        private Transform habitat;
        private Material indicator;
        private float equalizing;
        private int Entry => TowardsRing ? 0 : 1;
        private int Exit => 1 - Entry;
        private float DoorZ(int door) => door == 0 ? EntranceZ : ExitZ;

        public void Configure(Transform room, Transform firstLeft, Transform firstRight, Transform secondLeft, Transform secondRight, Material statusMaterial)
        {
            habitat = room; indicator = statusMaterial;
            leaves[0, 0] = firstLeft; leaves[0, 1] = firstRight;
            leaves[1, 0] = secondLeft; leaves[1, 1] = secondRight;
            ResetClosed();
        }
        public bool CanRequest(Vector3 feet)
        {
            if (PermanentlyOpen || habitat == null || Phase != AirlockPhase.Idle) return false;
            Vector3 p = habitat.InverseTransformPoint(feet);
            return Mathf.Abs(p.x) < ClearWidth*.5f-.15f && Mathf.Abs(p.y) < .4f
                && ((p.z > EntranceZ && p.z - EntranceZ < 1.9f)
                    || (p.z < ExitZ && ExitZ - p.z < 1.9f));
        }
        public bool Request(Vector3 feet)
        {
            if (!CanRequest(feet)) return false;
            occupantHalfLength=0;
            TowardsRing = habitat.InverseTransformPoint(feet).z > EntranceZ;
            Phase = AirlockPhase.OpeningEntry;
            Apply();
            return true;
        }
        // The maintenance workflow supplies the centre of its full hull. Both leaves stay
        // interlocked until the rear of that hull is clear, not only its navigation pivot.
        internal bool RequestTransit(Vector3 centre,float halfLength)
        {
            if(PermanentlyOpen || habitat==null || Phase!=AirlockPhase.Idle || halfLength<0 || halfLength*2+.7f>EntranceZ-ExitZ)return false;
            var p=habitat.InverseTransformPoint(centre);
            if(Mathf.Abs(p.x)>ClearWidth*.5f-.2f || p.y<0 || p.y>ClearHeight)return false;
            bool first=p.z>EntranceZ && p.z-EntranceZ<halfLength+2;
            bool second=p.z<ExitZ && ExitZ-p.z<halfLength+2;
            if(!first&&!second)return false;
            occupantHalfLength=halfLength;TowardsRing=first;Phase=AirlockPhase.OpeningEntry;Apply();return true;
        }
        public void ResetClosed()
        {
            Phase = AirlockPhase.Idle; progress[0] = progress[1] = 0; equalizing = 0;occupantHalfLength=0;
            Apply();
        }
        private void Update()
        {
            if (!DroneTransit && StationInteriorMode.IsInside && !SettingsMenu.BlocksInput)
                Advance(Time.unscaledDeltaTime, StationInteriorMode.Current.FeetPosition);
        }
        public void Advance(float seconds, Vector3 feet)
        {
            if (habitat == null || seconds <= 0 || Phase == AirlockPhase.Idle) return;
            Vector3 p = habitat.InverseTransformPoint(feet);
            float clearance=occupantHalfLength>0?occupantHalfLength+.35f:.85f;
            bool nearEntry = Mathf.Abs(p.z - DoorZ(Entry)) < clearance && Mathf.Abs(p.x) < ChamberHalfWidth;
            bool nearExit = Mathf.Abs(p.z - DoorZ(Exit)) < clearance && Mathf.Abs(p.x) < ChamberHalfWidth;
            switch (Phase)
            {
                case AirlockPhase.OpeningEntry:
                    progress[Exit] = 0;
                    progress[Entry] = Mathf.MoveTowards(progress[Entry], 1, seconds);
                    if (progress[Entry] == 1) Phase = AirlockPhase.AwaitingEntry;
                    break;
                case AirlockPhase.AwaitingEntry:
                    bool inChamber = p.z < EntranceZ - clearance && p.z > ExitZ + clearance && Mathf.Abs(p.x) < ChamberHalfWidth-.4f;
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
                    if (TowardsRing ? p.z < ExitZ - clearance : p.z > EntranceZ + clearance)
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
                float leafHalf=ClearWidth*.25f;
                leaf.localPosition = new Vector3(sign * (leafHalf + Mathf.SmoothStep(0, 1, progress[door]) * (ClearWidth*.5f+.12f)), ClearHeight*.5f, DoorZ(door));
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

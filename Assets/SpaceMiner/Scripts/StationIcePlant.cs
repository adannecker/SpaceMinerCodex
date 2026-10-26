using System;
using System.Collections.Generic;

namespace SpaceMiner
{
    // Declared first process sizing, not a certified asteroid-processing design.
    // Every batch retains both water-potential (ice) and unidentified rest mass.
    public sealed class StationIcePlant
    {
        public const double IntakeCapacityKg = 1500, CrushedCapacityKg = 1500;
        public const double ColdCapacityM3 = 257.109766, ResidueCapacityKg = 45000;
        public const double PackingFraction = .75, IceDensityKgM3 = 917, RestDensityKgM3 = 2500;
        public const double CrusherPowerKw = .2, CompactorPowerKw = .2, ProcessorPowerKw = 1.5;
        public const double CrusherKwhPerKg = .0025, CompactorKwhPerKg = .0025, ProcessingKwhPerLiter = .20;
        public const double CleanBufferCapacityKg = .25;
        private const int MaxBatches = 128;
        private const double Epsilon = 1e-9;

        [Serializable] public sealed class Batch
        {
            public double waterKg, restKg;
            public double RawKg => waterKg + restKg;
            public double VolumeM3 => (waterKg / IceDensityKgM3 + restKg / RestDensityKgM3) / PackingFraction;
            public Batch() { }
            public Batch(double water, double rest) { waterKg = water; restKg = rest; }
        }
        [Serializable] public sealed class Cell
        {
            public List<Batch> batches;
            public double RawKg => Raw(batches);
            public double VolumeM3 => Volume(batches);
        }
        [Serializable] public sealed class SavedState
        {
            // Version zero is solely an absent Unity inline-object placeholder.
            public int version;
            public bool receivingEnabled, processingEnabled;
            public List<Batch> intake, crushed;
            public List<Cell> cells;
            public double residueKg, cleanWaterPendingKg, receivedWaterKg, receivedRestKg, exportedWaterKg, exportedRestKg;
            public double crusherEnergyKwh, compactorEnergyKwh, processingEnergyKwh;
        }
        public SavedState State { get; private set; }
        public double IntakeKg => Raw(State.intake);
        public double CrushedKg => Raw(State.crushed);
        public double ColdRawKg { get { double value = 0; foreach (var cell in State.cells) value += cell.RawKg; return value; } }
        public double ColdVolumeM3 { get { double value = 0; foreach (var cell in State.cells) value += cell.VolumeM3; return value; } }
        public double ResidueKg => State.residueKg;
        public double CleanWaterPendingKg => State.cleanWaterPendingKg;
        public double ReceivedWaterKg => State.receivedWaterKg;
        public double ReceivedRestKg => State.receivedRestKg;
        public double ExportedWaterKg => State.exportedWaterKg;
        public double ExportedRestKg => State.exportedRestKg;
        public bool ReceivingEnabled { get => State.receivingEnabled; set => State.receivingEnabled = value; }
        public bool ProcessingEnabled { get => State.processingEnabled; set => State.processingEnabled = value; }
        public static double CellCapacityM3(int index)
        {
            if (index < 0 || index > 2) throw new ArgumentOutOfRangeException(nameof(index));
            // Integral of the actual widening wedge, excluding the two partitions.
            double[] low = { 39.57, 43.40, 47.17 }, high = { 43.28, 47.05, 50.88 };
            double angle = 4.85 * Math.PI / 180;
            return .95 * 4.36 * (Math.Tan(angle) * (high[index] * high[index] - low[index] * low[index])
                - 2.06 / Math.Cos(angle) * (high[index] - low[index]));
        }
        public double GetCellRawKg(int index) => State.cells[index].RawKg;
        public double GetCellVolumeM3(int index) => State.cells[index].VolumeM3;
        public double EnergyKwh => State.crusherEnergyKwh + State.compactorEnergyKwh + State.processingEnergyKwh;
        public double RequestedKw { get; private set; }
        public double ActualPowerKw { get; private set; }
        public bool CrusherActive { get; private set; }
        public bool CompactorActive { get; private set; }
        public bool ProcessorActive { get; private set; }
        public string Status { get; private set; } = "Bereit / kalte Annahme";

        public StationIcePlant(SavedState saved = null)
        {
            if (!Valid(saved)) throw new ArgumentException("Ungültiger Eis-Anlagenzustand.");
            State = HasData(saved) ? Clone(saved) : CreateState();
        }
        public static SavedState CreateState()
        {
            var state = new SavedState { version = 1, receivingEnabled = true, processingEnabled = true,
                intake = new List<Batch>(), crushed = new List<Batch>(), cells = new List<Cell>() };
            for (int i = 0; i < 3; i++) state.cells.Add(new Cell { batches = new List<Batch>() });
            return state;
        }
        public SavedState Capture() => Clone(State);
        private static SavedState Clone(SavedState value) => UnityEngine.JsonUtility.FromJson<SavedState>(UnityEngine.JsonUtility.ToJson(value));
        public static bool HasData(SavedState value) => value != null && value.version == 1;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Nonnegative(double value) => Finite(value) && value >= 0;
        private static double Raw(List<Batch> batches) { double sum = 0; if (batches != null) foreach (var batch in batches) sum += batch.RawKg; return sum; }
        private static double Volume(List<Batch> batches) { double sum = 0; if (batches != null) foreach (var batch in batches) sum += batch.VolumeM3; return sum; }
        private static double Water(List<Batch> batches) { double sum = 0; foreach (var batch in batches) sum += batch.waterKg; return sum; }
        private static double Rest(List<Batch> batches) { double sum = 0; foreach (var batch in batches) sum += batch.restKg; return sum; }
        private static bool ValidBatches(List<Batch> batches)
        {
            if (batches == null || batches.Count > MaxBatches) return false;
            foreach (var batch in batches) if (batch == null || !Nonnegative(batch.waterKg) || !Nonnegative(batch.restKg)
                || !Finite(batch.RawKg) || batch.RawKg <= 0 || !Finite(batch.VolumeM3)) return false;
            return true;
        }
        public static bool Valid(SavedState state)
        {
            if (state == null) return true;
            var sums = new[] { state.residueKg, state.cleanWaterPendingKg, state.receivedWaterKg, state.receivedRestKg,
                state.exportedWaterKg, state.exportedRestKg, state.crusherEnergyKwh, state.compactorEnergyKwh, state.processingEnergyKwh };
            foreach (double sum in sums) if (!Nonnegative(sum)) return false;
            if (state.version == 0)
            {
                foreach (double sum in sums) if (sum != 0) return false;
                return !state.receivingEnabled && !state.processingEnabled && (state.intake == null || state.intake.Count == 0)
                    && (state.crushed == null || state.crushed.Count == 0) && (state.cells == null || state.cells.Count == 0);
            }
            if (state.version != 1 || !ValidBatches(state.intake) || !ValidBatches(state.crushed)
                || state.cells == null || state.cells.Count != 3 || Raw(state.intake) > IntakeCapacityKg + 1e-6
                || Raw(state.crushed) > CrushedCapacityKg + 1e-6 || state.residueKg > ResidueCapacityKg + 1e-6
                || state.cleanWaterPendingKg > CleanBufferCapacityKg + 1e-6) return false;
            double water = Water(state.intake) + Water(state.crushed), rest = Rest(state.intake) + Rest(state.crushed), coldRaw = 0;
            for (int i = 0; i < state.cells.Count; i++)
            {
                var cell = state.cells[i];
                if (cell == null || !ValidBatches(cell.batches) || cell.VolumeM3 > CellCapacityM3(i) + 1e-7) return false;
                water += Water(cell.batches); rest += Rest(cell.batches); coldRaw += cell.RawKg;
            }
            double produced = state.cleanWaterPendingKg + state.exportedWaterKg;
            double tolerance = 1e-5 + (state.receivedWaterKg + state.receivedRestKg) * 1e-10;
            return Finite(water + rest + produced + coldRaw)
                && Math.Abs(state.receivedWaterKg - water - produced) <= tolerance
                && Math.Abs(state.receivedRestKg - rest - state.residueKg - state.exportedRestKg) <= tolerance
                && Math.Abs(state.crusherEnergyKwh - (state.receivedWaterKg + state.receivedRestKg - Raw(state.intake)) * CrusherKwhPerKg) <= tolerance
                && Math.Abs(state.compactorEnergyKwh - (coldRaw + produced + state.residueKg + state.exportedRestKg) * CompactorKwhPerKg) <= tolerance
                && Math.Abs(state.processingEnergyKwh - produced * ProcessingKwhPerLiter) <= tolerance;
        }
        private static bool Compatible(Batch batch, double fraction) => Math.Abs(batch.waterKg / batch.RawKg - fraction) < 1e-10;
        private static bool CanAppend(List<Batch> batches, double fraction) => batches.Count < MaxBatches || batches.Exists(batch => Compatible(batch, fraction));
        private static void Append(List<Batch> batches, double water, double rest)
        {
            if (water + rest <= 0) return;
            double fraction = water / (water + rest); var existing = batches.Find(batch => Compatible(batch, fraction));
            if (existing == null) batches.Add(new Batch(water, rest)); else { existing.waterKg += water; existing.restKg += rest; }
        }
        private static Batch Remove(List<Batch> batches, double kg)
        {
            var first = batches[0]; double take = Math.Min(kg, first.RawKg), fraction = take / first.RawKg;
            var result = new Batch(first.waterKg * fraction, first.restKg * fraction);
            first.waterKg -= result.waterKg; first.restKg -= result.restKg;
            if (first.RawKg <= Epsilon) batches.RemoveAt(0);
            return result;
        }
        public double MaxReceivableRawKg(double waterFraction)
        {
            if (!State.receivingEnabled || !Finite(waterFraction) || waterFraction < 0 || waterFraction > 1
                || !CanAppend(State.intake, waterFraction)) return 0;
            return Math.Max(0, IntakeCapacityKg - IntakeKg);
        }
        public double Receive(double rawKg, double waterFraction)
        {
            if (!Finite(rawKg) || rawKg <= 0) return 0;
            double accepted = Math.Min(rawKg, MaxReceivableRawKg(waterFraction));
            if (accepted <= 0) return 0;
            double water = accepted * waterFraction, rest = accepted - water;
            Append(State.intake, water, rest); State.receivedWaterKg += water; State.receivedRestKg += rest;
            Status = "Roh-Eis gekoppelt übernommen"; return accepted;
        }
        private Cell Destination(Batch batch)
        {
            for (int i = 0; i < State.cells.Count; i++) if (State.cells[i].VolumeM3 < CellCapacityM3(i) - Epsilon
                && CanAppend(State.cells[i].batches, batch.waterKg / batch.RawKg)) return State.cells[i];
            return null;
        }
        private Batch ProcessSource()
        {
            foreach (var cell in State.cells) foreach (var batch in cell.batches) if (batch.waterKg > Epsilon
                && (batch.restKg <= Epsilon || ResidueKg < ResidueCapacityKg - Epsilon)) return batch;
            return null;
        }
        public double RequestedPowerKw(double tankSpaceLiters, bool waterConnected)
        {
            CrusherActive = State.receivingEnabled && State.intake.Count > 0 && CrushedKg < CrushedCapacityKg - Epsilon
                && CanAppend(State.crushed, State.intake[0].waterKg / State.intake[0].RawKg);
            CompactorActive = State.receivingEnabled && State.crushed.Count > 0 && Destination(State.crushed[0]) != null;
            ProcessorActive = State.processingEnabled && waterConnected && tankSpaceLiters > Epsilon
                && State.cleanWaterPendingKg < Math.Min(CleanBufferCapacityKg, tankSpaceLiters) - Epsilon
                && ProcessSource() != null;
            RequestedKw = (CrusherActive ? CrusherPowerKw : 0) + (CompactorActive ? CompactorPowerKw : 0) + (ProcessorActive ? ProcessorPowerKw : 0);
            return RequestedKw;
        }
        public void Advance(double seconds, bool powerGranted, bool waterConnected, double tankSpaceLiters)
        {
            if (!Finite(seconds) || seconds <= 0 || !Finite(tankSpaceLiters)) return;
            RequestedPowerKw(Math.Max(0, tankSpaceLiters), waterConnected); ActualPowerKw = 0;
            if (!powerGranted) { CrusherActive = CompactorActive = ProcessorActive = false; Status = "Pausiert / Stromversorgung"; return; }
            double before = EnergyKwh;
            // Snapshot stage demand: newly transferred material waits until the next step.
            bool crush = CrusherActive, compact = CompactorActive, process = ProcessorActive;
            if (process)
            {
                double waterBudget = Math.Min(ProcessorPowerKw * seconds / 3600 / ProcessingKwhPerLiter,
                    Math.Min(CleanBufferCapacityKg, tankSpaceLiters) - State.cleanWaterPendingKg);
                foreach (var cell in State.cells)
                {
                    for (int i = 0; i < cell.batches.Count && waterBudget > Epsilon;)
                    {
                        var batch = cell.batches[i]; if (batch.waterKg <= Epsilon) { i++; continue; }
                        double fraction = Math.Min(1, waterBudget / batch.waterKg);
                        if (batch.restKg > 0) fraction = Math.Min(fraction, (ResidueCapacityKg - State.residueKg) / batch.restKg);
                        // A dimensionless fraction may be tiny for a tens-of-tonnes
                        // batch while its water transfer is perfectly representable.
                        if (fraction <= 0) { i++; continue; }
                        double water = batch.waterKg * fraction, rest = batch.restKg * fraction;
                        batch.waterKg -= water; batch.restKg -= rest; State.cleanWaterPendingKg += water; State.residueKg += rest;
                        State.processingEnergyKwh += water * ProcessingKwhPerLiter; waterBudget -= water;
                        if (batch.RawKg <= Epsilon) cell.batches.RemoveAt(i); else i++;
                    }
                }
            }
            if (compact)
            {
                double budget = CompactorPowerKw * seconds / 3600 / CompactorKwhPerKg;
                while (budget > Epsilon && State.crushed.Count > 0)
                {
                    var batch = State.crushed[0]; var cell = Destination(batch); if (cell == null) break;
                    double kg = Math.Min(budget, (CellCapacityM3(State.cells.IndexOf(cell)) - cell.VolumeM3) * batch.RawKg / batch.VolumeM3);
                    if (kg <= Epsilon) break;
                    var moved = Remove(State.crushed, kg); Append(cell.batches, moved.waterKg, moved.restKg);
                    State.compactorEnergyKwh += moved.RawKg * CompactorKwhPerKg; budget -= moved.RawKg;
                }
            }
            if (crush)
            {
                double budget = CrusherPowerKw * seconds / 3600 / CrusherKwhPerKg;
                while (budget > Epsilon && State.intake.Count > 0 && CrushedKg < CrushedCapacityKg - Epsilon)
                {
                    var first = State.intake[0]; if (!CanAppend(State.crushed, first.waterKg / first.RawKg)) break;
                    var moved = Remove(State.intake, Math.Min(budget, CrushedCapacityKg - CrushedKg));
                    Append(State.crushed, moved.waterKg, moved.restKg); State.crusherEnergyKwh += moved.RawKg * CrusherKwhPerKg;
                    budget -= moved.RawKg;
                }
            }
            ActualPowerKw = (EnergyKwh - before) * 3600 / seconds;
            Status = ActualPowerKw > Epsilon ? "Anlage arbeitet / Roh-Eis bleibt kalt bis zur Wasseraufbereitung"
                : !waterConnected ? "Wasserleitung geschlossen" : tankSpaceLiters <= Epsilon ? "Verbrauchswassertank voll" : "Bereit / Material oder freies Ziel abwarten";
        }
        public double ExportCleanWater(double actualKg)
        {
            if (!Finite(actualKg) || actualKg <= 0) return 0;
            double exported = Math.Min(actualKg, State.cleanWaterPendingKg);
            State.cleanWaterPendingKg -= exported; State.exportedWaterKg += exported; return exported;
        }
        public double TransferResidueTo(ResourceContainer target, double requested)
        {
            if (target == null || target.resourceId != "unknown" || !target.Valid || !Finite(requested) || requested <= 0) return 0;
            double limit = Math.Min(requested, Math.Min(State.residueKg, target.Free));
            if (limit <= 0) return 0;
            // Credit only representable growth in the destination's float reservoir.
            float desired = (float)((double)target.amount + limit);
            if ((double)desired - target.amount > limit) desired -= Math.Abs(desired) * 1.2e-7f;
            float accepted = Math.Max(0, desired - target.amount); if (accepted <= 0) return 0;
            float before = target.amount; target.Add(accepted); double actual = (double)target.amount - before;
            State.residueKg -= actual; State.exportedRestKg += actual; return actual;
        }
    }
}

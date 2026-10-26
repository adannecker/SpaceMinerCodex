using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Session telemetry only. Never serialized as campaign stock or extrapolated into a fake past.
    public sealed class StationPowerHistory
    {
        public readonly struct Sample
        {
            public readonly double Seconds;
            public readonly float Generation, Consumption;
            public Sample(double seconds, float generation, float consumption)
            { Seconds = seconds; Generation = generation; Consumption = consumption; }
        }
        private readonly List<Sample> samples = new List<Sample>();
        public IReadOnlyList<Sample> Samples => samples;
        public double Seconds { get; private set; }
        public void Reset() { samples.Clear(); Seconds = 0; }
        public void Observe(float seconds, float generation, float consumption)
        {
            if (!ResourceContainer.Finite(seconds) || seconds < 0 || !ResourceContainer.Finite(generation)
                || !ResourceContainer.Finite(consumption)) return;
            Seconds += seconds;
            var point = new Sample(Seconds, generation, consumption);
            if (samples.Count == 0) { samples.Add(point); return; }
            var last = samples[samples.Count - 1];
            if (Seconds == last.Seconds) { samples[samples.Count - 1] = point; return; }
            // Bound both history and rendering work, even at large simulation rates.
            if (samples.Count > 1 && Math.Floor(Seconds / 10) == Math.Floor(last.Seconds / 10)
                && Mathf.Abs(generation - last.Generation) < .005f && Mathf.Abs(consumption - last.Consumption) < .005f)
            { samples[samples.Count - 1] = point; return; }
            samples.Add(point);
            while (samples.Count > 240 || samples.Count > 2 && samples[1].Seconds < Seconds - 1800) samples.RemoveAt(0);
        }
        public string Trend(bool generation)
        {
            if (samples.Count < 2) return "— noch kein Verlauf";
            var current = samples[samples.Count - 1];
            var previous = samples[Mathf.Max(0, samples.Count - 2)];
            float delta = generation ? current.Generation - previous.Generation : current.Consumption - previous.Consumption;
            return delta > .005f ? "↑ zunehmend" : delta < -.005f ? "↓ abnehmend" : "→ stabil";
        }
        public static double RemainingSeconds(float stored, float capacity, float netKw)
        {
            if (!ResourceContainer.Finite(stored) || !ResourceContainer.Finite(capacity) || !ResourceContainer.Finite(netKw)
                || capacity <= 0 || stored < 0 || stored > capacity || Mathf.Abs(netKw) < .0001f) return double.NaN;
            return (netKw > 0 ? capacity - stored : stored) / Math.Abs((double)netKw) * 3600;
        }
        public static string Forecast(float stored, float capacity, float netKw)
        {
            if (capacity <= 0) return "Keine nutzbare Kapazität";
            if (stored <= .0001f && netKw <= .0001f) return "Speicher leer";
            if (stored >= capacity - .0001f && netKw >= -.0001f) return "Speicher voll";
            double seconds = RemainingSeconds(stored, capacity, netKw);
            if (double.IsNaN(seconds)) return "Bilanz ausgeglichen";
            string duration = seconds >= 86400 ? (seconds / 86400).ToString("0.0") + " Tage" : seconds >= 3600 ? (seconds / 3600).ToString("0.0") + " h"
                : seconds >= 60 ? (seconds / 60).ToString("0") + " min" : "< 1 min";
            return (netKw > 0 ? "Voll in ca. " : "Leer in ca. ") + duration + " Spielzeit";
        }
    }

    public readonly struct StationPowerConsumer
    {
        public readonly string Id, Name;
        public readonly float RequestedKw, SuppliedKw;
        public StationPowerConsumer(string id, string name, float requested, float supplied)
        { Id = id; Name = name; RequestedKw = Mathf.Max(0, requested); SuppliedKw = Mathf.Max(0, supplied); }
    }
}

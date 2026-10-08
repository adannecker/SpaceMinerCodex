using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    [Serializable] public sealed class SavedAsteroid
    {
        public string id;
        public bool scanned, waterIdentified;
        public float remainingKg;
    }
    [Serializable] public sealed class SavedGame
    {
        public int version = 1;
        public string name, comment, savedAt;
        public float water, tankCapacity, delivered, scannerEnergy, scannerRange, simulationRate;
        public int deliveries, trips, throughput, efficiency, researchProgress;
        public int tripsPerLevel;
        public float improvement, reactorKw, solarKw, chargeKw;
        public MiningFocus researchFocus;
        public bool sourceAssigned, scanned, console;
        public bool hasInteriorState, insideStation;
        public Vector3 interiorFeet;
        public SavedAsteroid[] asteroids;
        public DroneAgent.SavedState[] drones;
        public string Summary => (scanned ? "Erster Scan abgeschlossen" : "Erster Scan offen") + " · " +
            "Wasserabbau Lv. " + (1 + throughput + efficiency) + " · " + researchProgress + "/" + tripsPerLevel + " Fahrten" +
            "\nTank " + water.ToString("0.0") + "/" + tankCapacity.ToString("0") + " L · " + deliveries + " Lieferungen";
    }

    public static class SaveGameStore
    {
        public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "Saves");
        public static SavedGame Capture(WaterScenario scenario, string name, string comment)
        {
            var state = new SavedGame {
                name=name, comment=comment, savedAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                water=scenario.WaterLiters, tankCapacity=scenario.TankCapacityLiters, delivered=scenario.DeliveredLiters,
                deliveries=scenario.Deliveries, sourceAssigned=scenario.SourceAssigned, simulationRate=scenario.SimulationRate,
                trips=scenario.Mining.CompletedTrips, throughput=scenario.Mining.ThroughputLevels, efficiency=scenario.Mining.EfficiencyLevels,
                researchProgress=scenario.Mining.TripsIntoLevel, researchFocus=scenario.Mining.Focus,
                tripsPerLevel=scenario.Mining.TripsPerLevel,improvement=scenario.Mining.ImprovementPerLevel,
                reactorKw=scenario.ReactorPowerKw,solarKw=scenario.SolarPowerKw,chargeKw=scenario.DroneChargePowerKw,
                scannerEnergy=scenario.Scanner.EnergyKwh, scannerRange=scenario.Scanner.RangeMeters,
                scanned=scenario.Scanner.FirstScanComplete, console=scenario.Scanner.AtConsole,
                hasInteriorState=true, insideStation=StationInteriorMode.IsInside, interiorFeet=StationInteriorMode.Current!=null?StationInteriorMode.Current.FeetPosition:Vector3.zero,
                asteroids=new SavedAsteroid[scenario.Asteroids.Length], drones=new DroneAgent.SavedState[scenario.Drones.Length]
            };
            for (int i=0;i<state.asteroids.Length;i++) {
                var asteroid=scenario.Asteroids[i];
                state.asteroids[i]=new SavedAsteroid { id=asteroid.name, scanned=asteroid.IsScanned, waterIdentified=asteroid.WaterIdentified, remainingKg=asteroid.RemainingRawKg };
            }
            for (int i=0;i<state.drones.Length;i++) state.drones[i]=scenario.Drones[i].CaptureState();
            return state;
        }
        public static string Write(SavedGame state, bool autosave=false, string directory=null)
        {
            directory=directory ?? DirectoryPath; Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,autosave ? "autosave.json" : "save-"+Guid.NewGuid().ToString("N")+".json");
            string temporary=path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            using(var writer=new StreamWriter(stream)) { writer.Write(JsonUtility.ToJson(state,true));writer.Flush();stream.Flush(true); }
            if(File.Exists(path)) File.Replace(temporary,path,path+".bak"); else File.Move(temporary,path);
            return path;
        }
        public static SavedGame Read(string path)
        {
            if(new FileInfo(path).Length>32*1024*1024)throw new IOException("Spielstand ist zu gross.");
            var state=JsonUtility.FromJson<SavedGame>(File.ReadAllText(path));
            if(state==null || state.version!=1 || state.asteroids==null || state.drones==null || string.IsNullOrWhiteSpace(state.name))
                throw new IOException("Ungültiger oder nicht unterstützter Spielstand.");
            return state;
        }
        private static void Numbers(object value)
        {
            if(value==null)throw new IOException("Unvollständiger Spielstand.");
            if(value is float number) { if(float.IsNaN(number)||float.IsInfinity(number))throw new IOException("Ungültiger Zahlenwert."); return; }
            Type type=value.GetType();
            if(type==typeof(string)||type.IsPrimitive||type.IsEnum)return;
            if(value is Array array) { foreach(var item in array)Numbers(item); return; }
            foreach(var field in type.GetFields()) { var item=field.GetValue(value);if(item!=null)Numbers(item); }
        }
        public static void Validate(SavedGame state, WaterScenario scenario)
        {
            Numbers(state);
            if(state.asteroids.Length!=scenario.Asteroids.Length || state.drones.Length!=scenario.Drones.Length)
                throw new IOException("Dieser Spielstand gehört zu einem anderen Asteroidenfeld.");
            if(state.tankCapacity<=0 || state.water<0 || state.water>state.tankCapacity || state.deliveries<0 || state.delivered<0 ||
                state.scannerEnergy<0 || state.scannerEnergy>scenario.Scanner.CapacityKwh || Mathf.Abs(state.scannerRange-scenario.Scanner.RangeMeters)>.01f ||
                state.throughput<0 || state.efficiency<0 || state.trips<0 || state.tripsPerLevel<1 || state.improvement<0 || state.improvement>1 ||
                state.chargeKw<=0 || state.reactorKw<0 || state.solarKw<0 || state.researchProgress<0 || state.researchProgress>=state.tripsPerLevel ||
                !Enum.IsDefined(typeof(MiningFocus),state.researchFocus)) throw new IOException("Ungültiger Fortschritt.");
            var ids=new HashSet<string>();
            foreach(var asteroid in state.asteroids) {
                var source=Array.Find(scenario.Asteroids,a=>a.name==asteroid.id);
                if(source==null || !ids.Add(asteroid.id) || asteroid.remainingKg<0 || asteroid.remainingKg>source.InitialRawKg ||
                    (asteroid.waterIdentified && (!asteroid.scanned || !source.IsStarterWaterSource)) || (!state.scanned && asteroid.scanned))
                    throw new IOException("Ungültige Asteroidendaten.");
            }
            foreach(var drone in state.drones) {
                if(drone.hardware==null || drone.hardware.Length!=12)throw new IOException("Fehlende Drohnenparameter.");
                for(int i=0;i<9;i++)if(drone.hardware[i]<=0)throw new IOException("Ungültige Drohnenparameter.");
                if(drone.hardware[9]<0 || drone.hardware[10]<0 || drone.hardware[11]<0 || drone.hardware[11]>.5f ||
                    drone.fuel>drone.hardware[1]+.001f || drone.battery>drone.hardware[2]+.001f || drone.cargo>drone.hardware[3]+.001f)
                    throw new IOException("Ungültige Drohnenversorgung.");
                if(!Enum.IsDefined(typeof(DronePhase),drone.phase) || drone.fuel<0 || drone.battery<0 || drone.cargo<0 || drone.speed<0 || drone.phaseSeconds<0)
                    throw new IOException("Ungültiger Drohnenzustand.");
                if(!string.IsNullOrEmpty(drone.target)) {
                    var source=Array.Find(state.asteroids,a=>a.id==drone.target);
                    if(source==null || !source.scanned || !source.waterIdentified) throw new IOException("Drohnenziel ist nicht gescannt.");
                }
                else if(drone.tankOrder) throw new IOException("Drohnenauftrag ohne Ziel.");
            }
        }
        public static void Apply(SavedGame state, WaterScenario scenario)
        {
            Validate(state,scenario);
            foreach(var asteroid in state.asteroids) {
                var source=Array.Find(scenario.Asteroids,a=>a.name==asteroid.id);
                source.IsScanned=asteroid.scanned;source.WaterIdentified=asteroid.waterIdentified;source.RestoreDeposit(asteroid.remainingKg);
            }
            scenario.TankCapacityLiters=state.tankCapacity;
            scenario.ReactorPowerKw=state.reactorKw;scenario.SolarPowerKw=state.solarKw;scenario.DroneChargePowerKw=state.chargeKw;
            scenario.Mining.TripsPerLevel=state.tripsPerLevel;scenario.Mining.ImprovementPerLevel=state.improvement;
            scenario.RestoreSupply(state.water,state.deliveries,state.delivered,state.sourceAssigned);
            scenario.Mining.Restore(state.trips,state.throughput,state.efficiency,state.researchProgress);scenario.Mining.Focus=state.researchFocus;
            if (state.hasInteriorState && state.insideStation) StationInteriorMode.Current?.RestoreInterior(state.interiorFeet,state.console);
            else if (state.console) StationInteriorMode.Current?.Enter(true);
            else StationInteriorMode.Current?.Exit();
            scenario.Scanner.Restore(state.scannerEnergy,state.scanned,state.console,state.deliveries>0,state.water>=state.tankCapacity-.001f);
            for(int i=0;i<state.drones.Length;i++)scenario.Drones[i].RestoreState(state.drones[i]);
            scenario.SetSimulationRate(Mathf.Clamp(state.simulationRate,0,500));
            scenario.Notify("Spielstand geladen: "+state.name);
        }
    }
}

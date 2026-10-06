using System;
using UnityEngine;
namespace SpaceMiner
{
    // Storage adapters know nothing about UI, simulation, or applying runtime options.
    public interface IPlayerSettingsStorage
    {
        SpaceMinerPlayerSettings Load();
        void Save(SpaceMinerPlayerSettings settings);
    }
    public sealed class LocalPlayerSettingsStorage : IPlayerSettingsStorage
    {
        private readonly string path;
        public LocalPlayerSettingsStorage(string path) { this.path = path ?? throw new ArgumentNullException(nameof(path)); }
        public SpaceMinerPlayerSettings Load() => PlayerSettingsFile.Read(path);
        public void Save(SpaceMinerPlayerSettings settings) => PlayerSettingsFile.Write(path, settings);
    }
    // Can be created against local files, test storage, or a future storage adapter.
    public sealed class PlayerSettingsService
    {
        private readonly IPlayerSettingsStorage storage;
        private readonly Action<SpaceMinerPlayerSettings> apply;
        public SpaceMinerPlayerSettings Current { get; private set; } = new SpaceMinerPlayerSettings();
        public PlayerSettingsService(IPlayerSettingsStorage storage, Action<SpaceMinerPlayerSettings> apply)
        { this.storage = storage ?? throw new ArgumentNullException(nameof(storage)); this.apply = apply ?? throw new ArgumentNullException(nameof(apply)); }
        public bool Load(out string error)
        {
            bool success = true;
            try { Current = storage.Load() ?? new SpaceMinerPlayerSettings(); error = null; }
            catch (Exception e) { Current = new SpaceMinerPlayerSettings(); error = e.Message; success = false; }
            Current.Validate(); apply(Current); return success;
        }
        public bool Save(SpaceMinerPlayerSettings draft, out string error)
        {
            if (draft == null) { error = "Kein Einstellungsentwurf vorhanden."; return false; }
            var candidate = draft.Copy(); candidate.Validate();
            try { storage.Save(candidate); }
            catch (Exception e) { error = "Speichern fehlgeschlagen: " + e.Message; return false; }
            Current = candidate; apply(Current); error = null; return true;
        }
    }
}

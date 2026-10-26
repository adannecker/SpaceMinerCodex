using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private readonly Dictionary<string, Vector3> wreckOrigins = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Quaternion> wreckRotations = new Dictionary<string, Quaternion>();
        private static bool WreckId(string id) => id != null && id.StartsWith("debris-", StringComparison.Ordinal);
        public int KnownDebrisCount
        {
            get { int count = 0; for (int i = 0; i < 5; i++) if (IsDebrisKnown("debris-" + i) && !State.collected.Contains("debris-" + i)) count++; return count; }
        }
        public bool IsDebrisKnown(string id) => !WreckId(id) || Scenario.Scanner.FirstScanComplete || State.collected.Contains(id)
            || State.orders.Exists(order => order.kind == StationJobKind.Salvage && order.target == id && order.started);
        internal Transform DebrisRoot(string id) => debris.TryGetValue(id, out var root) ? root : null;
        internal Vector3 DebrisOrigin(string id) => presentation.TransformPoint(debrisOrigins[id]);

        private void BuildWreckContacts()
        {
            var starting = new[] { new Vector3(-146, 18, 120), new Vector3(118, -36, -165), new Vector3(-220, 54, -78),
                new Vector3(72, 92, 208), new Vector3(168, -64, 65) };
            var rotations = new[] { new Vector3(24, 51, -17), new Vector3(-61, 133, 32), new Vector3(72, -38, 15),
                new Vector3(-28, 219, -47), new Vector3(48, 307, 69) };
            for (int i = 0; i < 5; i++)
            {
                string id = "debris-" + i; Vector3 origin = ClearWreckOrigin(starting[i], i);
                var root = Group("Unbekanntes Teil " + (i + 1).ToString("00"), presentation, origin, Quaternion.Euler(rotations[i]));
                debris[id] = root; debrisOrigins[id] = origin; wreckOrigins[id] = origin; wreckRotations[id] = root.localRotation;
                BuildWreckShape(root, i); Selectable(root, id);
                AddElement(id, "Unbekanntes Teil " + (i + 1).ToString("00"), "Bergung", "Zusammensetzung und Inhalt unbekannt", 1, .4f);
            }
        }

        private Vector3 ClearWreckOrigin(Vector3 starting, int index)
        {
            for (int attempt = 0; attempt < 160; attempt++)
            {
                float factor = 1 + attempt * .08f;
                Vector3 candidate = Quaternion.Euler(0, attempt * 37 + index * 3, 0) * starting * factor;
                candidate.y = starting.y + Mathf.Sin((attempt + index) * 1.7f) * 13;
                Vector3 world = presentation.TransformPoint(candidate); bool clear = true;
                foreach (var asteroid in Scenario.Asteroids)
                    if (Vector3.Distance(world, asteroid.transform.position) < asteroid.Info.DiameterMeters * .5f + 24) { clear = false; break; }
                if (!clear) continue;
                foreach (var other in wreckOrigins.Values) if (Vector3.Distance(candidate, other) < 65) { clear = false; break; }
                if (clear) return candidate;
            }
            throw new InvalidOperationException("Kein freier Ort für Trümmerkontakt " + index);
        }

        private void BuildWreckShape(Transform root, int variant)
        {
            // Thin broken skins and hollow-looking structural fragments are visual prototypes.
            // The existing finite recipe still recovers only its defined usable 12 kg + two parts.
            if (variant == 0)
            {
                BrokenSkin(root, "Kleines ausgerissenes Wandblech", 1.35f, 1.0f, new Vector3(0, 0, 0), 0);
                Beam(root, new Vector3(-.63f, -.08f, -.43f), new Vector3(-.63f, -.08f, .32f), .09f);
                BentEdge(root, new Vector3(.48f, .08f, .18f), new Vector3(.38f, .38f, .36f), .045f);
            }
            else if (variant == 1)
            {
                Beam(root, new Vector3(-1.55f, 0, -.8f), new Vector3(1.55f, 0, -.8f), .13f);
                Beam(root, new Vector3(-1.55f, 0, -.8f), new Vector3(-1.55f, .2f, .85f), .13f);
                Beam(root, new Vector3(1.55f, 0, -.8f), new Vector3(1.32f, .4f, .35f), .13f);
                BrokenSkin(root, "Gerissene Haut im offenen Rahmen", 2.5f, 1.2f, new Vector3(-.15f, -.03f, -.12f), 1);
                Beam(root, new Vector3(-1.4f, .05f, -.7f), new Vector3(.7f, .1f, .45f), .065f);
            }
            else if (variant == 2)
            {
                Beam(root, new Vector3(-2.05f, 0, -.28f), new Vector3(1.8f, .1f, -.28f), .14f);
                Beam(root, new Vector3(-2.05f, 0, .32f), new Vector3(.65f, -.15f, .32f), .14f);
                for (int i = 0; i < 4; i++) Beam(root, new Vector3(-1.8f + i * .9f, 0, -.28f), new Vector3(-1.45f + i * .9f, 0, .32f), .075f);
                BentEdge(root, new Vector3(1.6f, .08f, -.28f), new Vector3(2.1f, .48f, .05f), .11f);
                BrokenSkin(root, "Verbliebenes Schutzblech am Fachwerk", 1.15f, .75f, new Vector3(-1.1f, .12f, 0), 2);
            }
            else if (variant == 3)
            {
                BrokenSkin(root, "Großes abgeschältes Wabenpaneel", 3.25f, 2.1f, Vector3.zero, 3);
                BrokenSkin(root, "Abgehobene zweite Haut", 1.7f, 1.35f, new Vector3(-.48f, .23f, -.2f), 4);
                foreach (float x in new[] { -.95f, 0f, .85f }) Beam(root, new Vector3(x, -.18f, -.84f), new Vector3(x + .12f, -.18f, .76f), .075f);
                BentEdge(root, new Vector3(1.18f, .02f, -.45f), new Vector3(1.55f, .65f, -.2f), .035f);
            }
            else
            {
                Beam(root, new Vector3(-.7f, 0, -.7f), new Vector3(.72f, 0, -.7f), .12f);
                Beam(root, new Vector3(-.7f, 0, -.7f), new Vector3(-.65f, .8f, .48f), .12f);
                Beam(root, new Vector3(.72f, 0, -.7f), new Vector3(.56f, .65f, .35f), .12f);
                BrokenSkin(root, "Zerbrochene Eckkassette", 1.3f, 1.25f, new Vector3(0, .08f, -.1f), 5);
                Shape("Offene unbestimmte Baugruppe", root, new Vector3(-.2f, .35f, -.23f), new Vector3(.55f, .4f, .34f), graphite, true);
                Line("Abgerissenes isoliertes Kabel", root, new Vector3(-.4f, .35f, -.2f), new Vector3(-.1f, .87f, .47f), .04f, cableSurface);
            }
            for (int i = 0; i < 3; i++)
                Shape("Verbliebene Verschraubung", root, new Vector3((i - 1) * .3f, .045f, -.25f), new Vector3(.075f, .045f, .075f), graphite);
        }
        private void Beam(Transform root, Vector3 a, Vector3 b, float width)
        {
            var beam = Shape("Abgerissener Hohlprofilträger", root, (a + b) * .5f,
                new Vector3(width, Vector3.Distance(a, b), width), alloy, true);
            beam.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            Shape("Dunkles offenes Profilende", beam, new Vector3(0, .501f, 0), new Vector3(.62f, .018f, .62f), graphite);
        }
        private void BentEdge(Transform root, Vector3 a, Vector3 b, float width)
        { var edge = Shape("Aufgebogene Bruchkante", root, (a + b) * .5f, new Vector3(width, Vector3.Distance(a, b), width * 3), alloy, true); edge.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); }
        private void BrokenSkin(Transform root, string name, float width, float depth, Vector3 position, int style)
        {
            var points = new List<Vector2> { new Vector2(-.5f, -.5f), new Vector2(.36f, -.5f), new Vector2(.5f, -.23f),
                new Vector2(.42f, .17f), new Vector2(.15f, .5f), new Vector2(-.28f, .38f), new Vector2(-.5f, .11f) };
            for (int i = 0; i < points.Count; i++) points[i] = Vector2.Scale(points[i], new Vector2(width, depth));
            var mesh = HoneycombHull.Mesh(name, new List<List<Vector2>> { points }, (p, d) => new Vector3(p.x, d * .12f, p.y));
            ownedMeshes.Add(mesh); var plate = Group(name, root, position, Quaternion.Euler(style % 2 == 0 ? 0 : 9, style * 13, style % 3 == 0 ? -6 : 0)); plate.gameObject.layer = 25;
            plate.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = plate.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { style % 2 == 0 ? alloy : graphite, alloy };
            plate.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            for (int i = 0; i < points.Count - 1; i += 2)
            {
                Vector2 a = points[i], b = points[i + 1];
                Line("Sichtbare unregelmäßige Risskante", plate, new Vector3(a.x, .008f, a.y), new Vector3(b.x, .008f, b.y), .018f, graphite);
            }
        }

        public void RefreshDebrisKnowledge()
        {
            if (presentation == null) return;
            RefreshDebrisPresentation();
            if (WreckId(selected) && !IsDebrisKnown(selected)) selected = "command";
        }
        private void RefreshDebrisPresentation()
        {
            foreach (var pair in debris)
            {
                bool wreck = WreckId(pair.Key), collected = State.collected.Contains(pair.Key);
                if (wreck)
                {
                    Vector3 origin = wreckOrigins[pair.Key];
                    // Existing saved jobs retain their actual old pickup point and flight path.
                    var oldJob = State.orders.FindLast(order => order.kind == StationJobKind.Salvage && order.target == pair.Key
                        && order.started && order.flightPath != null && order.flightPath.Length > 0);
                    if (oldJob != null) origin = presentation.InverseTransformPoint(oldJob.flightPath[oldJob.flightPath.Length - 1]) - Vector3.up * 5;
                    debrisOrigins[pair.Key] = origin;
                    var info = Element(pair.Key);
                    if (info != null)
                    {
                        info.name = collected ? "Wrackfund " + (int.Parse(pair.Key.Substring(7)) + 1).ToString("00") + " / geborgen"
                            : "Unbekanntes Teil " + (int.Parse(pair.Key.Substring(7)) + 1).ToString("00");
                        info.use = collected ? "Geborgen: 12 kg Schrott und 2 geprüfte Ersatzteile. Stoffliche Untersuchung an der Schrottanalyse."
                            : "Ortung und grobe Form bekannt. Zusammensetzung, Masse und verwertbarer Inhalt unbekannt.";
                    }
                }
                pair.Value.gameObject.SetActive(!collected && (!wreck || IsDebrisKnown(pair.Key)));
                var active = State.orders.Find(order => order.started && !order.complete && !order.cancelled && order.target == pair.Key);
                if (active == null)
                { pair.Value.localPosition = debrisOrigins[pair.Key]; if (wreck) pair.Value.localRotation = wreckRotations[pair.Key]; }
                else if (wreck && active.flightPath != null && active.flightPath.Length > 0)
                {
                    // Loading while paused must restore the visible carried body immediately,
                    // independently of the drone pose being restored later by SaveGameStore.
                    float progress = Mathf.Min(.94f, active.elapsed / StationRecipes.Get(active.kind).Seconds);
                    if (progress >= .4f)
                    {
                        var drone = Scenario.Drones[active.actorId == "drone-1" ? 1 : 0];
                        float travel = progress > .7f ? (1 - progress) / .3f : 1;
                        pair.Value.position = PathPoint(active.flightPath, travel) + drone.transform.TransformVector(new Vector3(0, 2, 0));
                    }
                    else pair.Value.localPosition = debrisOrigins[pair.Key];
                }
            }
        }

        public bool SelectDebrisContact(string id)
        {
            if (!WreckId(id) || !debris.ContainsKey(id) || !IsDebrisKnown(id) || State.collected.Contains(id)) return false;
            selected = id; Camera.main.GetComponent<OrbitCamera>().Select(null); return true;
        }
        private bool DebrisMarker(string id, out Rect bounds)
        {
            bounds = default;
            if (!IsDebrisKnown(id) || State.collected.Contains(id) || !debris[id].gameObject.activeInHierarchy) return false;
            Vector3 screen = Camera.main.WorldToScreenPoint(debris[id].position); if (screen.z < 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return false;
            Vector2 point = UiLayout.Point(screen); bounds = new Rect(point.x - 72, point.y - 17, 144, 34);
            return !HudPanelOverlaps(bounds);
        }
        private bool OwnsDebrisMarker(Vector2 point)
        { for (int i = 0; i < 5; i++) if (DebrisMarker("debris-" + i, out var bounds) && bounds.Contains(point)) return true; return false; }
        private void DrawDebrisMarkers()
        {
            for (int i = 0; i < 5; i++)
            {
                string id = "debris-" + i; if (!DebrisMarker(id, out var bounds)) continue;
                if (ui.Tab(bounds, "◇ Teil " + (i + 1).ToString("00"), selected == id)) {SelectDebrisContact(id);SelectTacticalTab(TacticalOverviewTab.Objects);FocusTacticalContact(id);}
            }
        }
        private void DrawDebrisDetails(StationElementState item)
        {
            if (!IsDebrisKnown(item.id)) return;
            bool recovered = State.collected.Contains(item.id);
            GUILayout.Label(item.name, ui.Text); GUILayout.Label(item.use, ui.Small);
            if (!recovered)
            {
                float distance = Vector3.Distance(Scenario.Worker.transform.position, DebrisOrigin(item.id));
                GUILayout.Label("Wrackkontakt / Oberflächenform erfasst\nEntfernung Drohne 01: " + distance.ToString("0") + " m\nInhalt erst nach Bergung verfügbar; Schrott anschließend analysieren.", ui.Small);
                bool enabled = GUI.enabled; GUI.enabled = enabled && SourceAvailable(item.id);
                ActionButton(StationJobKind.Salvage, item.id); GUI.enabled = enabled;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private string consoleSelectedDrone = "drone-0", consoleLastExternal = "drone-0";
        private bool consoleInternal;
        public string ConsoleSelectedDroneId => consoleSelectedDrone;
        public bool ConsoleInternal => consoleInternal;
        public string R01HelperStatus
        {
            get
            {
                if (!State.helper) return "Ausgeschaltet / Inbetriebnahme erforderlich";
                var order = State.orders.Find(o => !o.complete && o.started && o.actorId == "R-01");
                return order == null ? "Bereit am internen Serviceplatz" : StationRecipes.Get(order.kind).Name + " · " + order.status;
            }
        }
        private int ConsoleDroneIndex(string id)
        {
            if (id == null || !id.StartsWith("drone-", StringComparison.Ordinal)) return -1;
            return int.TryParse(id.Substring(6), out int index) && index >= 0 && Scenario != null
                && Scenario.Drones != null && index < Scenario.Drones.Length && id == "drone-" + index ? index : -1;
        }
        public bool SelectConsoleDrone(string stableId)
        {
            if (stableId != "R-01" && ConsoleDroneIndex(stableId) < 0) return false;
            if (consoleSelectedDrone != stableId) ResetConsoleFilterFocus();
            consoleSelectedDrone = stableId; consoleInternal = stableId == "R-01";
            if (!consoleInternal) consoleLastExternal = stableId;
            return true;
        }
        public void SetConsoleDomain(bool internalDrones)
        {
            SelectConsoleDrone(internalDrones ? "R-01" : consoleLastExternal);
        }
        public bool ConsoleOrderBelongsTo(StationWorkOrder order, string id)
        {
            if (order == null) return false;
            if (id == "R-01") return order.kind == StationJobKind.ActivateHelper || Actor(order) == WorkActor.Internal;
            int index = ConsoleDroneIndex(id); if (index < 0) return false;
            if (order.kind == StationJobKind.Refit) return order.target.StartsWith(index + ":", StringComparison.Ordinal);
            if (order.kind == StationJobKind.ActivateSecond) return index == 1;
            return Actor(order) == WorkActor.External && order.actorId == id;
        }
        public List<StationWorkOrder> ConsoleOrders(string id, bool includeComplete = false)
        {
            return State.orders.FindAll(o => (includeComplete || !o.complete) && ConsoleOrderBelongsTo(o, id));
        }
        public DroneFleetReadModel ConsoleDrone(string id)
        {
            bool inside = id == "R-01"; int index = inside ? -1 : ConsoleDroneIndex(id);
            if (!inside && index < 0) return null;
            var active = State.orders.Find(o => o.started && !o.complete && ConsoleOrderBelongsTo(o, id));
            var view = new DroneFleetReadModel { Id = id, Internal = inside, Index = index, Name = inside ? "R-01" : "Drohne " + (index + 1).ToString("00") };
            if (inside)
            {
                view.Operational = State.helper; view.Status = R01HelperStatus; view.Tool = "Basisgreifer / Bestandsservice";
                view.Position = Layout.RepairDrone == null ? Vector3.zero : Layout.RepairDrone.position;
                view.WaitReason = State.helper ? "Stationsversorgung · kein eigener Akku/Tank simuliert" : "2 Ersatzteile und Inbetriebnahmeauftrag erforderlich";
            }
            else
            {
                var d = Scenario.Drones[index]; view.Operational = CanUseDrone(index) && d.IsOperational; view.Status = d.Status;
                view.Tool = State.tools[index]; view.Position = d.transform.position; view.HasOnboardStores = true;
                view.BatteryKwh = d.BatteryKwh; view.BatteryCapacityKwh = d.BatteryCapacityKwh;
                view.FuelLiters = d.FuelLiters; view.FuelCapacityLiters = d.FuelCapacityLiters;
                view.CargoKg = d.CargoKg; view.CargoCapacityKg = d.EffectiveCargoCapacityKg;
                view.CargoVolumeM3 = d.CargoVolumeM3; view.CargoFillFraction = d.CargoFillFraction;
                view.SpeedMetersPerSecond = d.Speed; view.DistanceMeters = d.TargetDistance;
                view.WaitReason = d.ReturnReason;
                if (active == null && (d.HasTankOrder || d.DeliveringExistingCargo || d.Phase != DronePhase.Ready && d.IsOperational))
                {
                    view.HasActivity = true; view.ActivityId = (d.HasTankOrder || d.DeliveringExistingCargo ? "water:" : "phase:") + id;
                    view.CanRecall = d.HasTankOrder || d.DeliveringExistingCargo;
                    view.Activity = d.DeliveringExistingCargo ? "Restladung zur Eisannahme liefern" : d.HasTankOrder ? "Eine Ladung Roh-Eis liefern" : d.Status;
                    view.Destination = d.Target == null ? "Eisannahme / Heimatdock" : d.Target.Info.DisplayName;
                    view.Progress = d.Phase == DronePhase.Unloading ? d.UnloadProgress : d.PhaseProgress;
                    view.EtaSeconds = d.PhaseSecondsRemaining; view.EtaLabel = "Aktuelle Phase noch ca. ";
                    if (d.Phase == DronePhase.Servicing)
                    {
                        float energyMissing = Mathf.Max(0, d.BatteryCapacityKwh - d.BatteryKwh);
                        float charging = Scenario.AllocatedChargeKw;
                        view.EtaSeconds = energyMissing <= .001f ? 0 : charging > .0001f ? energyMissing / charging * 3600 : float.PositiveInfinity;
                        if (d.FuelLiters < d.FuelCapacityLiters - .001f && (!WaterConnected(1) || Scenario.WaterLiters <= 0)) view.EtaSeconds = float.PositiveInfinity;
                        else view.EtaSeconds = Mathf.Max(view.EtaSeconds, d.FuelCapacityLiters - d.FuelLiters);
                        view.EtaLabel = "Ladezeit bei aktueller Zuteilung ca. ";
                    }
                    view.HasEta = ResourceContainer.Finite(view.EtaSeconds) && view.EtaSeconds > 0;
                }
            }
            if (active != null)
            {
                var recipe = StationRecipes.Get(active.kind); view.HasActivity = true; view.ActivityId = active.id;
                view.CanRecall = !active.cancelled; view.EtaLabel = "Arbeitszeit noch ca. ";
                view.Activity = recipe.Name; view.Status = active.status; view.Destination = ConsoleTargetName(active.target);
                view.Progress = Mathf.Clamp01(active.elapsed / recipe.Seconds); view.EtaSeconds = Mathf.Max(0, recipe.Seconds - active.elapsed);
                view.HasEta = true; view.WaitReason = active.status;
                view.DistanceMeters = Vector3.Distance(view.Position, WorkTarget(active));
            }
            return view;
        }
        public string ConsoleTargetName(string target)
        {
            if (string.IsNullOrEmpty(target)) return "Stationsanlage";
            var element = Element(target); if (element != null) return element.name;
            var source = Array.Find(Scenario.Asteroids, a => a.name == target);
            if (source != null) return source.Info.DisplayName;
            var bits = target.Split(':');
            return bits.Length == 2 && int.TryParse(bits[0], out int d) ? "Drohne " + (d + 1).ToString("00") + " → " + bits[1] : target;
        }
        public List<DroneConsoleTarget> ConsoleWaterTargets()
        {
            var result = new List<DroneConsoleTarget>(); int index = Mathf.Max(0, ConsoleDroneIndex(consoleSelectedDrone));
            var sources = new List<AsteroidResource>(Scenario.Asteroids);
            sources.Sort((a, b) => Vector3.Distance(Scenario.Drones[index].transform.position, a.transform.position).CompareTo(Vector3.Distance(Scenario.Drones[index].transform.position, b.transform.position)));
            foreach (var source in sources)
                if (source.CanMineWater) result.Add(new DroneConsoleTarget(source.name, source.Info.DisplayName,
                    (source.WaterFraction * 100).ToString("0") + " % Wasser · Rest unbekannt · " + Vector3.Distance(Scenario.Drones[index].transform.position, source.transform.position).ToString("0") + " m"));
            return result;
        }
        private static bool ConsoleInternalRepair(string target) => target == "printer" || target == "food" || target == "battery" || target == "scrap";
        public List<DroneConsoleTarget> ConsoleTargets(StationJobKind kind)
        {
            var result = new List<DroneConsoleTarget>();
            foreach (var element in State.elements)
            {
                bool valid = kind == StationJobKind.Salvage ? element.kind == "Bergung" && IsDebrisKnown(element.id) && SourceAvailable(element.id)
                    : kind == StationJobKind.Carbon ? element.kind == "Rohstoff" && State.diagnosed && SourceAvailable(element.id)
                    : kind == StationJobKind.Repair ? element.built && element.condition < .999f && element.kind != "Bergung" && element.kind != "Rohstoff" && ConsoleInternalRepair(element.id) == consoleInternal
                    : kind == StationJobKind.Expand && !element.built && element.id.StartsWith("expansion-", StringComparison.Ordinal) && !consoleInternal;
                if (!valid) continue;
                string detail = kind == StationJobKind.Salvage ? "Unbekannter Wrackkontakt · Inhalt erst nach Bergung"
                    : kind == StationJobKind.Carbon ? "Bestätigte Prozessprobe · endliche Quelle"
                    : element.built ? "Zustand " + (element.condition * 100).ToString("0") + " % · " + element.kind : "Freier Anbauslot";
                result.Add(new DroneConsoleTarget(element.id, element.name, detail));
            }
            return result;
        }
        // Pure prerequisites: unlike Ready(), this does not reserve actors or plan/mutate flight paths.
        public string ConsoleOrderWaitReason(StationJobKind kind, string target = "")
        {
            var recipe = StationRecipes.Get(kind); if (recipe == null) return "Unbekannte Tätigkeit";
            if (kind == StationJobKind.ActivateSecond && !State.diagnosed) return "Versorgungsdiagnose fehlt";
            if ((recipe.Actor == WorkActor.Internal || kind == StationJobKind.ResearchRepair || kind == StationJobKind.Repair && ConsoleInternalRepair(target)) && !State.helper) return "R-01 noch ausgeschaltet";
            if (recipe.Technology != "" && !State.technologies.Contains(recipe.Technology)) return "Forschung fehlt: " + (recipe.Technology == "repair" ? "Reparaturrolle" : "Modulmontage");
            if (kind == StationJobKind.ActivateSecond && Scenario.WaterLiters < 40) return "Stationsreserve unter 40 L Wasser";
            if (kind == StationJobKind.ActivateSecond && State.power.storedKwh < 6) return "Stationsakku unter 6 kWh";
            if (!State.inventory.CanConsume(recipe.Input)) return "Material fehlt: " + ConsoleRecipeMaterials(recipe.Input);
            if (!State.inventory.CanReceive(recipe.Output)) return "Ziellager voll";
            if (!Powered(kind == StationJobKind.Recycle || kind == StationJobKind.Refit ? 1 : 0)) return "Sektorstrom unterbrochen";
            if ((kind == StationJobKind.Food || kind == StationJobKind.Oxygen) && (!WaterConnected(0) || !WaterConnected(7))) return "Wasserleitung abgesperrt";
            if (kind == StationJobKind.Salvage && !Scenario.Scanner.FirstScanComplete) return "Nahbereichsscan fehlt";
            if (kind == StationJobKind.Carbon && !State.diagnosed) return "Versorgungsdiagnose fehlt";
            if (kind == StationJobKind.Refit)
            {
                if (!State.helper) return "R-01 noch ausgeschaltet";
                var bits = target.Split(':');
                if (bits.Length == 2 && int.TryParse(bits[0], out int index) && index >= 0 && index <= 1)
                {
                    if (!Scenario.Drones[index].IsReady) return "Drohne noch nicht frei am Dock";
                    if (!WorkshopRefitReady(new StationWorkOrder(kind, target), out string why)) return why;
                }
            }
            if (recipe.Actor == WorkActor.External && !(kind == StationJobKind.Repair && ConsoleInternalRepair(target)))
            {
                int i = ConsoleDroneIndex(consoleSelectedDrone);
                if (i < 0 || i > 1 || !CanUseDrone(i)) return "Drohne nicht für Außenaufträge freigegeben";
                if (!Scenario.Drones[i].IsReady || IsRefitting(i)) return "Drohne noch beschäftigt / lädt am Dock";
                if (kind == StationJobKind.Repair && State.tools[i] != "Laser") return "Laser erforderlich: Werkzeugumbau vormerken";
                if (kind == StationJobKind.Carbon && State.tools[i] != "Bohrer") return "Bohrer erforderlich: Werkzeugumbau vormerken";
                if (kind == StationJobKind.Salvage && State.tools[i] == "Laser") return "Bohrer oder Greifer erforderlich";
            }
            return "";
        }
        public static string ConsoleRecipeMaterials(ResourceAmount[] amounts)
        {
            if (amounts.Length == 0) return "keine";
            var names = new List<string>(); foreach (var a in amounts) names.Add(a.quantity.ToString("0.##") + " " + StationResources.Get(a.id).Unit + " " + StationResources.Get(a.id).Name);
            return string.Join(" · ", names);
        }
        private bool ConsoleResult(bool success, string text, out string reason)
        { reason = text; SetMessage(text); return success; }
        private bool ConsoleCommandAvailable(out string reason)
        {
            reason = DroneConsoleCanCommand ? "" : "Befehle nur am geöffneten Drohnenpult in der Station erteilen.";
            return reason == "";
        }
        public bool TryConsoleWaterOrder(string sourceId, out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            int i = ConsoleDroneIndex(consoleSelectedDrone);
            if (i < 0 || i > 1 || !CanUseDrone(i)) return ConsoleResult(false, "Nur eine freigegebene Außendrohne kann Eis liefern.", out reason);
            var source = Array.Find(Scenario.Asteroids, a => a.name == sourceId);
            if (source == null || !source.CanMineWater) return ConsoleResult(false, "Ziel ist keine gescannte, bestätigte und verfügbare Eisquelle.", out reason);
            bool accepted = Scenario.AssignTankOrder(source, i); reason = Scenario.Message; SetMessage(reason);
            // The mission acknowledgement belongs to Mira. Do not keep an invisible
            // station panel owning input and menu pause underneath that dialogue.
            if (accepted && Scenario.Scanner.Dialogue != null) ClosePanel();
            return accepted;
        }
        public bool TryConsoleRefit(string tool, out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            int i = ConsoleDroneIndex(consoleSelectedDrone);
            if (i < 0 || i > 1 || !CanUseDrone(i)) return ConsoleResult(false, "Nur freigegebene Außendrohnen besitzen die vorhandene Werkzeugaufnahme.", out reason);
            if (tool != "Bohrer" && tool != "Greifer" && tool != "Laser") return ConsoleResult(false, "Unbekanntes oder noch nicht erforschtes Werkzeug.", out reason);
            if (State.tools[i] == tool) return ConsoleResult(false, tool + " ist bereits montiert.", out reason);
            if (State.orders.Exists(o => !o.complete && o.kind == StationJobKind.Refit && o.target.StartsWith(i + ":", StringComparison.Ordinal))) return ConsoleResult(false, "Für diese Drohne ist bereits ein Werkzeugumbau vorgemerkt.", out reason);
            if (tool == "Laser" && !State.technologies.Contains("repair")) return ConsoleResult(false, "Mira muss zuerst die Reparaturrolle erforschen.", out reason);
            if ((tool == "Laser" ? State.spareLasers : tool == "Greifer" ? State.spareGrippers : State.spareDrills) <= 0) return ConsoleResult(false, "Kein " + tool + " im Werkzeugbestand.", out reason);
            return ConsoleEnqueue(StationJobKind.Refit, i + ":" + tool, out reason);
        }
        public bool TryConsoleOrder(StationJobKind kind, string target, out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            var recipe = StationRecipes.Get(kind); if (recipe == null || kind == StationJobKind.Refit) return ConsoleResult(false, "Werkzeugumbauten über die Ausrüstungsansicht wählen.", out reason);
            int i = ConsoleDroneIndex(consoleSelectedDrone);
            bool internalRepair = kind == StationJobKind.Repair && ConsoleInternalRepair(target);
            if (kind == StationJobKind.ActivateSecond && consoleSelectedDrone != "drone-1" && !consoleInternal) return ConsoleResult(false, "Drohne 02 auswählen, um ihre Instandsetzung zu planen.", out reason);
            bool internalAction = recipe.Actor == WorkActor.Internal || internalRepair || kind == StationJobKind.ActivateHelper;
            bool globalAction = kind == StationJobKind.Diagnose || recipe.Actor == WorkActor.Research || kind == StationJobKind.ActivateSecond;
            if (!globalAction && internalAction != consoleInternal) return ConsoleResult(false, "Diese Tätigkeit gehört zum anderen Drohnenbereich.", out reason);
            if (recipe.Actor == WorkActor.External && !internalRepair && (i < 0 || i > 1 || !CanUseDrone(i))) return ConsoleResult(false, "Diese Drohne ist nicht für Außenaufträge freigegeben.", out reason);
            if (kind == StationJobKind.Salvage || kind == StationJobKind.Carbon || kind == StationJobKind.Repair || kind == StationJobKind.Expand)
            {
                if (!ConsoleTargets(kind).Exists(t => t.Id == target)) return ConsoleResult(false, "Kein bekanntes, verfügbares Ziel für diese Tätigkeit ausgewählt.", out reason);
            }
            else if (!string.IsNullOrEmpty(target)) return ConsoleResult(false, "Diese Stationsaufgabe besitzt kein wählbares Außenziel.", out reason);
            if (kind == StationJobKind.ActivateHelper && State.helper || kind == StationJobKind.ActivateSecond && State.second || kind == StationJobKind.Diagnose && State.diagnosed)
                return ConsoleResult(false, "Diese Inbetriebnahme / Diagnose ist bereits abgeschlossen.", out reason);
            return ConsoleEnqueue(kind, target ?? "", out reason);
        }
        private bool ConsoleEnqueue(StationJobKind kind, string target, out string reason)
        {
            int before = State.orders.Count, previous = preferredDrone;
            int i = ConsoleDroneIndex(consoleSelectedDrone); if (i >= 0 && i <= 1) preferredDrone = i;
            bool accepted;
            try { accepted = Enqueue(kind, target); } finally { preferredDrone = previous; }
            if (!accepted) return ConsoleResult(false, State.orders.Count >= 80 ? "Auftragsliste voll." : "Auftrag bereits vorhanden, abgeschlossen oder Ziel nicht mehr verfügbar.", out reason);
            var order = State.orders[before]; string wait = ConsoleOrderWaitReason(kind, target);
            reason = StationRecipes.Get(kind).Name + " vorgemerkt." + (wait == "" ? " Start bei freiem Arbeitsplatz und verfügbarer Versorgung." : " Wartet: " + wait + ".");
            order.status = wait == "" ? "Vorgemerkt" : "Wartet: " + wait; SetMessage(reason); return true;
        }
        public bool MoveConsoleOrder(string id, int direction, out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            var pending = ConsoleOrders(consoleSelectedDrone).FindAll(o => !o.started && !o.cancelled);
            int from = pending.FindIndex(o => o.id == id), to = from + Math.Sign(direction);
            if (from < 0 || to < 0 || to >= pending.Count || direction == 0) return ConsoleResult(false, "Nur vorgemerkte Aufträge lassen sich innerhalb dieser Liste verschieben.", out reason);
            return PlaceConsoleOrder(id, pending[to].id, out reason);
        }
        public bool PlaceConsoleOrder(string id, string targetId, out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            var order = State.orders.Find(o => o.id == id); var target = State.orders.Find(o => o.id == targetId);
            if (id == targetId || order == null || target == null || order.started || target.started || order.complete || target.complete || order.cancelled || target.cancelled
                || !ConsoleOrderBelongsTo(order, consoleSelectedDrone) || !ConsoleOrderBelongsTo(target, consoleSelectedDrone))
                return ConsoleResult(false, "Laufende oder fremde Aufträge werden nicht verschoben.", out reason);
            bool success = MoveOrder(id, State.orders.IndexOf(target));
            return ConsoleResult(success, success ? "Priorität aktualisiert; laufende Arbeit wird sicher fortgesetzt." : "Priorität konnte nicht geändert werden.", out reason);
        }
        public bool CancelConsoleOrder(string id, out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            var order = State.orders.Find(o => o.id == id);
            if (order == null || order.complete || order.cancelled || !ConsoleOrderBelongsTo(order, consoleSelectedDrone)) return ConsoleResult(false, "Kein abbrechbarer Auftrag dieser Drohne.", out reason);
            Cancel(id); return ConsoleResult(true, order.started ? "Sicherer Abschluss / Rückkehr angefordert; Materialrückgabe erfolgt durch den Auftrag." : "Vorgemerkter Auftrag abgebrochen.", out reason);
        }
        public bool RemoveConsoleHistory(string id, out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            var order = State.orders.Find(o => o.id == id);
            if (order == null || !order.complete || order.reserved != null && order.reserved.Length > 0 || !ConsoleOrderBelongsTo(order, consoleSelectedDrone))
                return ConsoleResult(false, "Nur abgeschlossene Einträge ohne Materialreservierung dieser Drohne können entfernt werden.", out reason);
            bool removed = RemoveFinishedOrder(order.id);
            return ConsoleResult(removed, removed ? "Abgeschlossenen Historieneintrag entfernt; Ressourcen und Arbeitsergebnis bleiben erhalten." : "Historieneintrag ist noch nicht sicher abgeschlossen.", out reason);
        }
        public bool RecallConsoleDrone(out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            int index = ConsoleDroneIndex(consoleSelectedDrone); bool recalled = false;
            foreach (var order in ConsoleOrders(consoleSelectedDrone)) if (order.started && !order.complete && !order.cancelled) { Cancel(order.id); recalled = true; }
            if (index >= 0 && index <= 1)
            {
                var drone = Scenario.Drones[index];
                if (drone.HasTankOrder || drone.DeliveringExistingCargo) { drone.ReturnToShip(); recalled = true; }
                if (drone.ReturnReason != "") return ConsoleResult(recalled, drone.ReturnReason, out reason);
            }
            return ConsoleResult(recalled, recalled ? "Sichere Rückkehr / Abschluss angefordert." : "Kein aktiver Auftrag für einen Rückruf.", out reason);
        }
        public bool DeliverConsoleCargo(out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            int i = ConsoleDroneIndex(consoleSelectedDrone);
            if (i < 0 || i > 1) return ConsoleResult(false, "Außendrohne mit vorhandener Rohfracht auswählen.", out reason);
            bool ok = Scenario.Drones[i].DeliverExistingCargo(out reason); SetMessage(reason); return ok;
        }
        public bool ApplyConsoleSizingAtDock(out string reason)
        {
            if (!ConsoleCommandAvailable(out reason)) return false;
            int i = ConsoleDroneIndex(consoleSelectedDrone);
            if (i < 0 || i > 1) return ConsoleResult(false, "Eine der ersten beiden Außendrohnen auswählen.", out reason);
            bool ok = Scenario.Drones[i].ApplyNewSizingAtDock(out reason); SetMessage(reason); return ok;
        }
    }
}

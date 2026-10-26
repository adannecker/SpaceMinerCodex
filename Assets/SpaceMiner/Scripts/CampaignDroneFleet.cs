using System;
using UnityEngine;

namespace SpaceMiner
{
    // The approved imported hull and mining attachments, shared by the ten real campaign agents.
    public sealed class CampaignDroneFleet : MonoBehaviour
    {
        private WaterScenario scenario;
        private readonly TripoMiningDroneVisual[] visuals=new TripoMiningDroneVisual[10];
        private readonly MeshCollider[] cradles=new MeshCollider[10];
        private readonly Transform[] cradleRoots=new Transform[10],berthRoots=new Transform[10];
        private readonly Vector3[] berthScales=new Vector3[10];
        private bool refreshingBerths,navigationLayoutChanged;
        private MaintenanceLaserVisual receiver;
        internal int ModelCount { get; private set; }
        internal DroneFlightPlanner Navigator { get; private set; }
        internal Transform CradleRoot(int index)=>cradleRoots[index];
        internal Transform BerthRoot(int index)=>berthRoots[index];
        internal static void Install(StationVisual station,StationInteriorLayout layout)
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-tripoMiningShowcase")>=0 || Array.IndexOf(args,"-tripoMiningCheck")>=0)return;
            if(station.GetComponent<CampaignDroneFleet>()!=null)return;
            var fleet=station.gameObject.AddComponent<CampaignDroneFleet>();fleet.Build(station,layout);
        }
        private void Build(StationVisual station,StationInteriorLayout layout)
        {
            scenario=station.GetComponent<WaterScenario>();
            for(int i=0;i<visuals.Length;i++)
            {
                berthRoots[i]=layout.Geometry.CampaignBerths[i];berthScales[i]=berthRoots[i].lossyScale;
                var agent=scenario.Drones[i];var visual=agent.gameObject.AddComponent<TripoMiningDroneVisual>();visual.Build(agent);visuals[i]=visual;ModelCount++;
                Color blue=Color.Lerp(new Color(.21f,.29f,.44f),new Color(.27f,.35f,.49f),i/9f);
                foreach(var attachment in agent.GetComponentsInChildren<MaintenanceLaserVisual>())attachment.UseHullBlue(blue);
                var hull=visual.FlightBoxes[0];var selection=agent.gameObject.AddComponent<BoxCollider>();selection.center=hull.center;selection.size=hull.size;
                foreach(var renderer in agent.GetComponentsInChildren<Renderer>())
                    if(renderer.transform.parent!=null && renderer.sharedMaterial!=null && renderer.sharedMaterial.HasProperty("_MainTex") && renderer.sharedMaterial.mainTexture!=null)
                    {var block=new MaterialPropertyBlock();block.SetColor("_Color",Color.white*(.91f+i*.01f));renderer.SetPropertyBlock(block);}
                cradles[i]=BuildCradle(agent,layout,i);
                foreach(var renderer in agent.GetComponentsInChildren<Renderer>())renderer.gameObject.layer=26;
                if(i>0)agent.LockInactive();
            }
            var socket=station.TankSocket;
            foreach(Transform child in socket)child.gameObject.SetActive(false);
            var oldPipe=socket.parent.Find("Transfer pipe");if(oldPipe!=null)oldPipe.gameObject.SetActive(false);
            var receiver=new GameObject("Campaign sealed cargo receiver").transform;receiver.SetParent(socket,false);
            var transfer=receiver.gameObject.AddComponent<MaintenanceLaserVisual>();transfer.BuildCargoReceiver();transfer.UseHullBlue(new Color(.23f,.31f,.47f));
            this.receiver=transfer;
            foreach(var filter in receiver.GetComponentsInChildren<MeshFilter>())
                if(filter.name=="Wide unloading receiver side" || filter.name=="Enclosed unloading receiver floor" || filter.name=="Enclosed unloading receiver roof")
                {var collision=filter.gameObject.AddComponent<BoxCollider>();collision.center=filter.sharedMesh.bounds.center;collision.size=filter.sharedMesh.bounds.size;}
            visuals[0].ConfigureReceiver(transfer);scenario.Worker.ConfigureUnloading(40,200);
            Navigator=new DroneFlightPlanner(scenario.Worker.transform){HullBoxes=visuals[0].FlightBoxes,DockHullPoints=visuals[0].DockHullPoints,CradleCollider=cradles[0]};
            scenario.Worker.ConfigureNavigation(Navigator);Physics.SyncTransforms();
            visuals[1].ConfigureReceiver(transfer);
            scenario.Drones[1].ConfigureUnloading(40,200);
            scenario.Drones[1].ConfigureNavigation(new DroneFlightPlanner(scenario.Drones[1].transform){HullBoxes=visuals[1].FlightBoxes,DockHullPoints=visuals[1].DockHullPoints,CradleCollider=cradles[1]});
            for(int i=0;i<visuals.Length;i++){StationModelLod.Add(scenario.Drones[i].transform,"MaintenanceDrone");StationModelLod.Add(cradles[i].transform,"ChargingCradle");}
            foreach(var agent in scenario.Drones)foreach(var attachment in agent.GetComponentsInChildren<MaintenanceLaserVisual>())attachment.CombineStaticSurfaces();
            transfer.CombineStaticSurfaces();
            StationDockInfrastructure.Install(scenario,layout);
        }
        private MeshCollider BuildCradle(DroneAgent agent,StationInteriorLayout layout,int index)
        {
            foreach(var item in layout.Geometry.Root.GetComponentsInChildren<Transform>())
            {
                if(item.name=="Aussen-Ladebucht "+(index+1).ToString("00"))
                    foreach(Transform child in item)if(child.name!="Docktraeger" && child.GetComponent<TextMesh>()==null)child.gameObject.SetActive(false);
                if(item.name=="Dockaufnahme" && Vector3.Distance(item.position,agent.HomePosition-Vector3.up*.85f)<.1f)item.gameObject.SetActive(false);
            }
            var cradle=new GameObject("Imported campaign cradle "+(index+1).ToString("00")).transform;
            cradle.SetParent(layout.transform,true);cradle.SetPositionAndRotation(agent.HomePosition,agent.HomeRotation);
            var model=new GameObject("Normalised original charging cradle").transform;model.SetParent(cradle,false);
            Instantiate(Resources.Load<GameObject>("Tripo/ChargingCradle"),model,false);
            model.localScale=Vector3.one*(3.05f/TripoDockPreview.LocalBounds(model,cradle).size.x);
            var bounds=TripoDockPreview.LocalBounds(model,cradle);model.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var filter in cradle.GetComponentsInChildren<MeshFilter>())
            {var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;filter.gameObject.layer=30;}
            Physics.SyncTransforms();
            if(!Physics.Raycast(cradle.position+cradle.up*10,-cradle.up,out var hit,20,1<<30))throw new Exception("Campaign charging cradle floor missing");
            float floor=Vector3.Dot(hit.point-cradle.position,cradle.up);cradle.position+=cradle.up*(-.65f-floor+.015f);
            // The physical cradle follows the same authored berth anchor as the
            // drone's home pose; no second world-space delta is applied later.
            cradle.SetParent(berthRoots[index],true);cradleRoots[index]=cradle;
            return cradle.GetComponentInChildren<MeshCollider>();
        }
        internal void SetPlannedBerth(int index,Transform berth)
        {
            if(index<0||index>=visuals.Length)throw new ArgumentOutOfRangeException(nameof(index));
            if(berth==null)throw new ArgumentNullException(nameof(berth));
            var agent=scenario.Drones[index];
            bool changed=berthRoots[index]!=berth||(agent.HomePosition-berth.position).sqrMagnitude>.00000001f
                ||Quaternion.Angle(agent.HomeRotation,berth.rotation)>.001f||(berthScales[index]-berth.lossyScale).sqrMagnitude>.00000001f;
            if(!changed)return;
            if(cradleRoots[index].parent!=berth)
            {
                var local=cradleRoots[index].localPosition;var rotation=cradleRoots[index].localRotation;var scale=cradleRoots[index].localScale;
                cradleRoots[index].SetParent(berth,false);cradleRoots[index].localPosition=local;cradleRoots[index].localRotation=rotation;cradleRoots[index].localScale=scale;
            }
            berthRoots[index]=berth;berthScales[index]=berth.lossyScale;
            GetComponentInChildren<StationDockInfrastructure>()?.BindFeedToBerth(index,berth);
            // Bind the changed home with a fresh local planner, then invalidate the
            // other route caches after this batch without migrating their home poses.
            var navigation=new DroneFlightPlanner(agent.transform){HullBoxes=visuals[index].FlightBoxes,DockHullPoints=visuals[index].DockHullPoints,CradleCollider=cradles[index]};
            if(index==0)Navigator=navigation;
            agent.ConfigureNavigation(navigation);agent.RebindHome(berth);Physics.SyncTransforms();
            navigationLayoutChanged=true;
            if(!refreshingBerths)InvalidateFleetNavigation();
            GetComponentInChildren<StationDockInfrastructure>()?.Refresh();
        }
        private void InvalidateFleetNavigation()
        {
            if(!navigationLayoutChanged)return;
            // Every dock is an obstacle for every other route. Reset all existing
            // planners after a layout edit, without migrating unchanged home poses.
            // The room's edit guard forbids dock changes while any drone is away.
            for(int i=0;i<visuals.Length;i++)
            {
                var agent=scenario.Drones[i];if(visuals[i]==null||i>1&&agent.Navigation==null)continue;
                var navigation=new DroneFlightPlanner(agent.transform){HullBoxes=visuals[i].FlightBoxes,DockHullPoints=visuals[i].DockHullPoints,CradleCollider=cradles[i]};
                agent.ConfigureNavigation(navigation);if(i==0)Navigator=navigation;
            }
            navigationLayoutChanged=false;
        }
        internal void RefreshPlannedBerths()
        {
            if(scenario==null)return;
            refreshingBerths=true;
            try{for(int i=0;i<berthRoots.Length;i++)if(berthRoots[i]!=null)SetPlannedBerth(i,berthRoots[i]);}
            finally{refreshingBerths=false;InvalidateFleetNavigation();}
        }
        internal void Advance(float seconds)
        {
            if(!enabled || scenario==null)return;
            bool bound=false;for(int i=0;i<visuals.Length;i++)if(scenario.Drones[i].IsOperational){bool docked=scenario.Drones[i].Phase==DronePhase.Unloading;visuals[i].ConfigureReceiver(docked?receiver:null);if(docked)bound=true;visuals[i].Refresh(seconds);}if(!bound)receiver.PoseCargoCollar(0,false);
        }
        internal void ResetPresentation()
        {foreach(var visual in visuals)if(visual!=null){visual.ResetCollection();visual.Refresh(0);}if(receiver!=null)receiver.PoseCargoCollar(0,false);}
    }
}

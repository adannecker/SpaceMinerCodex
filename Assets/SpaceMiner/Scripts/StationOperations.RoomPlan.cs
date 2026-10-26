using UnityEngine;

namespace SpaceMiner
{
    public sealed partial class StationOperations
    {
        private static string CommandPlanId(StationTerminalKind kind)
        {
            switch(kind)
            {
                case StationTerminalKind.Drones:return "drones";
                case StationTerminalKind.Planning:return "planning";
                case StationTerminalKind.Research:return "research";
                case StationTerminalKind.Production:return "production";
                case StationTerminalKind.Power:return CommandRoomLayout.PowerConsolePlanId;
                default:throw new System.ArgumentOutOfRangeException(nameof(kind),kind,"Kein Pult im Kommandoraumplan.");
            }
        }

        public Transform PowerConsoleRoot => RoomPlan?.BoundRoot(CommandRoomLayout.PowerConsolePlanId);

        private Transform CreateCommandBlock(string id,string selectable)
        {
            var item=RoomPlan.Get(id);
            var root=Group(item.label+" / Planungsbox",presentation,Vector3.zero,Quaternion.identity);
            Selectable(root,selectable);
            var body=Shape("Blockoutbody",root,Vector3.zero,Vector3.one,alloy,true);
            var label=StationDisplay.Create(item.label,root,Vector3.zero,Quaternion.identity,
                .055f,signageFont,signageMaterial,graphite,cyan,25,3.2f);
            RoomPlan.Register(id,root,body,label.transform);
            if(id!="printer"&&id!="food")TouchConsoleVisual.Install(root,false).Bind(label,body,item.size);
            return root;
        }

        private void UpdateCommandInteractionPoints()
        {
            if(RoomPlan==null)return;
            var main=RoomPlan.Get("main-console");
            if(Layout.Console!=null)Layout.Console.localPosition=new Vector3(0,0,-main.size.z*.5f-.35f);
            RefreshTouchConsole("main-console");
            foreach(var terminal in terminals)
            {
                var block=terminal.GetComponentInParent<CommandRoomBlock>();
                if(block==null)continue;
                var item=RoomPlan.Get(block.id);
                terminal.transform.localPosition=new Vector3(0,0,-item.size.z*.5f-.12f);
                RefreshTouchConsole(block.id);
            }
        }
        private void RefreshTouchConsole(string id)
        {
            var root=RoomPlan.BoundRoot(id);var visual=root!=null?root.GetComponentInChildren<TouchConsoleVisual>():null;
            if(visual!=null)visual.Bind(root.GetComponentInChildren<StationDisplay>(),root.Find("Blockoutbody"),RoomPlan.Get(id).size);
        }

        private Vector3 CommandServicePoint(string id)
        {
            var item=RoomPlan?.Get(id);var root=RoomPlan?.BoundRoot(id);
            return item==null||root==null?Layout.transform.TransformPoint(new Vector3(0,1.25f,42))
                :root.TransformPoint(new Vector3(0,1.25f,-item.size.z*.5f-.65f));
        }
    }
}

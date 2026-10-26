using UnityEngine;

namespace SpaceMiner
{
    // The authored housing follows the plan root; changing width preserves structural side thickness.
    public sealed class TouchConsoleVisual : MonoBehaviour
    {
        public bool Main;
        public float Width => Main ? 4.4f : 1.5f;
        public const float Height = 1.14f, Depth = 1f;
        private StationDisplay source;
        private TextMesh frontText, screenText;
        private float refreshAt;
        public void Bind(StationDisplay display, Transform body, Vector3 footprint)
        {
            source=display;FitFootprint(footprint);
            if(frontText==null){
                frontText=Object.Instantiate(display.Front,transform);frontText.name="R04 Frontbeschriftung";frontText.transform.localPosition=new Vector3(0,.535f,-.507f);frontText.transform.localRotation=Quaternion.identity;frontText.transform.localScale=Vector3.one;frontText.characterSize=Main?.065f:.038f;
                screenText=Object.Instantiate(display.Front,transform);screenText.name="R04 Funktionsanzeige";screenText.transform.localPosition=new Vector3(0,.858f,-.002f);screenText.transform.localRotation=Quaternion.Euler(61.2f,0,0);screenText.transform.localScale=Vector3.one;screenText.characterSize=Main?.075f:.034f;
                foreach(var renderer in display.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            }
            body.GetComponent<Renderer>().enabled=false;
            var box=body.GetComponent<BoxCollider>();
            if(box!=null){box.size=new Vector3(Width/footprint.x,Height/footprint.y,Depth/footprint.z);box.center=new Vector3(0,(Height-footprint.y)*.5f/footprint.y,transform.localPosition.z/footprint.z);}
        }
        private void Update()
        {
            if(source==null||Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.25f;
            frontText.text=source.Caption;
            var ops=StationOperations.Current;
            if(ops==null){screenText.text=source.Caption;return;}
            var block=GetComponentInParent<CommandRoomBlock>();string id=block!=null?block.id:"";
            string detail=id==CommandRoomLayout.PowerConsolePlanId?
                "ERZEUGUNG  "+ops.State.power.GenerationKw.ToString("F2")+" kW\nVERBRAUCH  "+ops.State.power.ConsumptionKw.ToString("F2")+" kW":
                id=="drones"?"DROHNENLEITSTAND\n"+ops.Scenario.Drones.Length+" Drohnen im Bestand":
                id=="main-console"?"SCANNER / STATION\n"+(ops.Scenario.Scanner.FirstScanComplete?"Nahbereich erfasst":"Nahbereich noch unbekannt"):
                id=="production"?"FERTIGUNG\n"+ops.State.orders.Count+" Aufträge im Bestand":
                id=="planning"?"STATIONSPLANUNG\nReparatur / Ausbau":"MIRA / FORSCHUNG\nArchive und Technologie";
            screenText.text=source.Caption+"\n\n"+detail+"\n\nE  BEDIENEN";
            FitText(frontText,Main?3.4f:.80f,.055f);
            FitText(screenText,Main?3.6f:1.10f,.67f);
        }
        private static void FitText(TextMesh text,float width,float height)
        {
            text.characterSize=.01f;
            var bounds=text.GetComponent<Renderer>().localBounds.size;
            text.characterSize*=Mathf.Min(width/Mathf.Max(.001f,bounds.x),height/Mathf.Max(.001f,bounds.y));
        }
        public static TouchConsoleVisual Install(Transform root, bool main)
        {
            var prefab=Resources.Load<GameObject>(main?"TouchConsoleR04/Main":"TouchConsoleR04/Standard");
            if(prefab==null)throw new System.InvalidOperationException("R04-Pultmodell fehlt. TouchConsoleBuild vorbereiten.");
            var instance=Object.Instantiate(prefab,root,false);instance.name=main?"R04 Hauptpult / breit":"R04 Touchpult";
            var visual=instance.GetComponent<TouchConsoleVisual>();visual.Main=main;
            foreach(var t in instance.GetComponentsInChildren<Transform>())t.gameObject.layer=25;
            return visual;
        }
        public void FitFootprint(Vector3 footprint)
        {
            // Physical R04 stays at its authored height/depth. Place its front at the existing interaction edge.
            transform.localPosition=new Vector3(0,0,-footprint.z*.5f+Depth*.5f);
        }
    }
}

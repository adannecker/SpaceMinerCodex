using UnityEngine;

namespace SpaceMiner
{
    // Equal-pressure compartment bulkhead. Vacuum transitions use StationAirlock instead.
    public sealed class StationPressureGate : MonoBehaviour
    {
        private Transform[] leaves;
        private float width,height,opening,leafWidth;
        private bool telescopic;
        public bool Locked { get; private set; }
        public bool TargetOpen { get; private set; }
        public float Opening => opening;
        public float ClearWidth => width;
        public float ClearHeight => height;
        internal int LeafCount => leaves?.Length??0;
        public string Label { get; private set; }
        internal void SetSafetyLock(bool value){Locked=value;if(value)TargetOpen=false;}
        internal void Configure(float clearWidth,float clearHeight,bool locked,string label,Material metal,Material signal,bool compactNarrow=false)
        {
            // Four nested panels per side fit inside each narrow wall cassette. Eight
            // panels give the 8.95-m ring frame a genuine 7-m clear walking opening.
            // Narrow reactor passages can opt into compact stacks without the
            // wide ring frame's shoulder conversion changing their clear width.
            bool wideFrame=clearWidth>=4;
            telescopic=wideFrame||compactNarrow;
            width=wideFrame?clearWidth*.8f-.16f:clearWidth;
            height=clearHeight;Locked=locked;Label=label;
            leaves=new Transform[telescopic?8:2];leafWidth=width/leaves.Length+(telescopic?.02f:.035f);
            opening=locked?0:1;TargetOpen=!locked;
            // Solid jambs and lintel overlap the pressure backing. No unsealed edge remains
            // around a closed leaf even where a joint and a module meet different wall meshes.
            foreach(float side in new[]{-1f,1f})
                Frame("Druckschott / Dichtungszarge",new Vector3(side*(clearWidth*.5f+.07f),height*.5f,0),new Vector3(.22f,height+.12f,.4f),metal);
            Frame("Druckschott / geschlossener Sturz",new Vector3(0,height+.1f,0),new Vector3(clearWidth+.36f,.2f,.4f),metal);
            if(compactNarrow&&!wideFrame)foreach(float side in new[]{-1f,1f})
                Frame("Reaktorschott / gekapselte Teleskoptasche",new Vector3(side*(width*.5f+.25f),height*.5f,.14f),new Vector3(.5f,height+.16f,.96f),metal);
            // Fixed cassettes cover only the panel stacks, rather than closing most of
            // the traffic lanes. Their outer edges stay inside the original frame.
            float shoulder=(clearWidth-width)*.5f;
            if(shoulder>.02f)foreach(float sign in new[]{-1f,1f})
            {
                var fixedWall=GameObject.CreatePrimitive(PrimitiveType.Cube);fixedWall.name="Druckschott / feste Schulter";fixedWall.layer=25;
                fixedWall.transform.SetParent(transform,false);fixedWall.transform.localPosition=new Vector3(sign*(width*.5f+shoulder*.5f),height*.5f,0);
                fixedWall.transform.localScale=new Vector3(shoulder,height,.88f);fixedWall.GetComponent<Renderer>().sharedMaterial=metal;
            }
            for(int i=0;i<leaves.Length;i++)
            {
                var leaf=GameObject.CreatePrimitive(PrimitiveType.Cube);leaf.name="Druckschott / Schiebefluegel";
                leaf.layer=25;leaf.transform.SetParent(transform,false);leaf.transform.localScale=new Vector3(leafWidth,height+.08f,telescopic?.2f:.18f);
                leaf.GetComponent<Renderer>().sharedMaterial=metal;leaves[i]=leaf.transform;
                if(telescopic&&i%4!=0)continue;
                var strip=GameObject.CreatePrimitive(PrimitiveType.Cube);strip.name="Schottstatus / konstant";strip.layer=25;
                strip.transform.SetParent(leaf.transform,false);strip.transform.localPosition=new Vector3(i==0?.44f:-.44f,0,-.56f);
                strip.transform.localScale=telescopic?new Vector3(.025f/leafWidth,.65f/(height+.08f),.08f):new Vector3(.025f,.35f,.04f);strip.GetComponent<Renderer>().sharedMaterial=signal;
                Destroy(strip.GetComponent<Collider>());
            }
            Apply();
        }
        private void Frame(string name,Vector3 position,Vector3 size,Material material)
        {
            var node=GameObject.CreatePrimitive(PrimitiveType.Cube);node.name=name;node.layer=25;
            node.transform.SetParent(transform,false);node.transform.localPosition=position;node.transform.localScale=size;
            node.GetComponent<Renderer>().sharedMaterial=material;
        }
        internal bool Near(Vector3 feet)
        {
            var p=transform.InverseTransformPoint(feet);
            return Mathf.Abs(p.x)<width/2+.4f && Mathf.Abs(p.z)<2.1f && Mathf.Abs(p.y)<.6f;
        }
        internal bool Occupied(Vector3 feet)
        {
            var p=transform.InverseTransformPoint(feet);
            return Mathf.Abs(p.x)<width/2+.35f && Mathf.Abs(p.z)<.8f && Mathf.Abs(p.y)<.6f;
        }
        internal bool Request(bool open,Vector3 feet)
        {
            if(Locked || !open && Occupied(feet))return false;TargetOpen=open;return true;
        }
        internal string Prompt => Locked ? Label+" / GESPERRT – DRUCKVERLUST" : "E  "+Label+(TargetOpen?" schließen":" öffnen");
        private void Update()
        {
            if(SettingsMenu.BlocksInput || StartMenu.IsOpen || IntroSequence.BlocksGameplay)return;
            Advance(Mathf.Min(Time.unscaledDeltaTime,.05f),StationInteriorMode.IsInside?StationInteriorMode.Current.FeetPosition:(Vector3?)null);
        }
        internal void Advance(float seconds,Vector3? visitor)
        {
            if(!TargetOpen && !Locked && visitor.HasValue && Occupied(visitor.Value))TargetOpen=true;
            float next=Mathf.MoveTowards(opening,TargetOpen?1:0,seconds*.7f);
            if(Mathf.Approximately(next,opening))return;opening=next;Apply();Physics.SyncTransforms();
        }
        private void Apply()
        {
            for(int i=0;leaves!=null&&i<leaves.Length;i++)if(leaves[i]!=null)
            {
                if(!telescopic)
                    leaves[i].localPosition=new Vector3((i==0?-1:1)*(width*.25f+Mathf.SmoothStep(0,1,opening)*(width*.5f+.12f)),height/2,0);
                else
                {
                    int panel=i%4;float sign=i<4?-1:1;
                    float closed=(panel+.5f)*width/8,stack=width*.5f+leafWidth*.5f+.012f;
                    // Adjacent depth layers overlap by 4 cm at their sealed edges;
                    // the centre pair shares one plane, including the closed seam.
                    leaves[i].localPosition=new Vector3(sign*Mathf.Lerp(closed,stack,Mathf.SmoothStep(0,1,opening)),height/2,(panel-1.5f)*.16f);
                }
            }
        }
    }
}

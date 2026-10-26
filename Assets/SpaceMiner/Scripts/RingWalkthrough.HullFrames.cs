using UnityEngine;

namespace SpaceMiner
{
    internal sealed partial class RingWalkthrough
    {
        internal int HullSupportFrames { get; private set; }
        internal static bool PreserveHallInterior(string name)=>name.StartsWith("Drohnenhalle") || name.StartsWith("Waben Hallenfront");
        private void DeckSupportFrame(Transform parent,float ri,float ro,float a,float b,float inner,float outer)
        {
            float low=Mathf.Min(inner,outer)+.045f,high=Mathf.Max(inner,outer)-.045f;
            if(high<=low)return;
            var support=Child("Huellrahmen / Deck / geschlossener Rand",parent);HullSupportFrames++;
            Solid("Huellrahmen / innerer Stirnabschluss",support,ri-.42f,ri+.12f,a,b,low,high,frame,false);
            Solid("Huellrahmen / aeusserer Stirnabschluss",support,ro-.12f,ro+.42f,a,b,low,high,frame,false);
            int ribs=Mathf.Max(1,Mathf.CeilToInt((b-a)*Mathf.Deg2Rad*(ri+ro)*.5f/3));
            for(int i=0;i<=ribs;i++)
            {
                float angle=Mathf.Lerp(a,b,(float)i/ribs);
                Box("Abstandsrippe / beide Decklagen verbunden",support,At((ri+ro)/2,angle,(low+high)/2),new Vector3(.14f,high-low,ro-ri),dark,angle,false);
            }
            foreach(float radius in new[]{Mathf.Lerp(ri,ro,.33f),Mathf.Lerp(ri,ro,.67f)})
                Solid("Huellrahmen / Umfangsgurt",support,radius-.07f,radius+.07f,a,b,(low+high)/2-.07f,(low+high)/2+.07f,dark,false);
        }
        private void WallSupportFrame(Transform parent,float ri,float ro,float a,float b,float low,float high,float sign)
        {
            float pressure=(sign>0?ri:ro)-sign*.045f,shield=(sign>0?ro:ri)+sign*HoneycombHull.StandOff;
            float inside=Mathf.Min(pressure,shield)+.04f,outside=Mathf.Max(pressure,shield)-.04f;
            if(outside<=inside || high-low<.05f)return;
            var support=Child("Huellrahmen / Wand / angebundene Schutzlage",parent);HullSupportFrames++;
            foreach(float y in new[]{low+.07f,high-.07f})
                Solid("Huellrahmen / Wandabschluss",support,inside,outside,a,b,y-.07f,y+.07f,frame,false);
            int ribs=Mathf.Max(1,Mathf.CeilToInt((b-a)*Mathf.Deg2Rad*(ri+ro)*.5f/2.5f));
            for(int i=0;i<=ribs;i++)
            {
                float angle=Mathf.Lerp(a,b,(float)i/ribs);
                Box("Abstandsrippe / beide Wandlagen verbunden",support,At((inside+outside)/2,angle,(low+high)/2),new Vector3(.14f,high-low,outside-inside),dark,angle,false);
            }
        }
        private void BoxSupportFrame(Transform parent,Vector3 size,float sign,bool side,bool roof)
        {
            var support=Child("Huellrahmen / Seitenwand und Randabschluss",parent);HullSupportFrames++;
            if(side)
            {
                float inner=-sign*(size.x/2+.045f),outer=sign*(size.x/2+HoneycombHull.StandOff);
                float x=(inner+outer)/2,depth=Mathf.Abs(outer-inner)-.08f;
                foreach(float y in new[]{-size.y/2+.07f,size.y/2-.07f})
                    Box("Huellrahmen / Seitenabschluss",support,new Vector3(x,y,0),new Vector3(depth,.14f,size.z),frame,0,false);
                int ribs=Mathf.Max(1,Mathf.CeilToInt(size.z/3));
                for(int i=0;i<=ribs;i++)Box("Abstandsprofil Seitenhaut",support,new Vector3(x,0,Mathf.Lerp(-size.z/2,size.z/2,(float)i/ribs)),new Vector3(depth,size.y,.14f),dark,0,false);
            }
            else
            {
                float inner=roof?-size.y/2-.045f:size.y/2-.035f,outer=(roof?1:-1)*(size.y/2+HoneycombHull.StandOff);
                float y=(inner+outer)/2,height=Mathf.Abs(outer-inner)-.08f;
                foreach(float x in new[]{-size.x/2+.07f,size.x/2-.07f})Box("Huellrahmen / Deckrand",support,new Vector3(x,y,0),new Vector3(.14f,height,size.z),frame,0,false);
                int ribs=Mathf.Max(1,Mathf.CeilToInt(size.z/3));
                for(int i=0;i<=ribs;i++)Box("Abstandsprofil Deckhaut",support,new Vector3(0,y,Mathf.Lerp(-size.z/2,size.z/2,(float)i/ribs)),new Vector3(size.x,height,.14f),dark,0,false);
            }
        }
    }
}

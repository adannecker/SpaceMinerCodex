using UnityEngine;

namespace SpaceMiner
{
    internal sealed partial class RingWalkthrough
    {
        // Keep the authored wall panels, floor, equipment and side doors. The centre
        // opening is rebuilt, because scaling the whole prefab would distort the hall.
        private void RebuildDroneHallFront(Transform parent,float angle,float half)
        {
            foreach(var item in DetailedHall.GetComponentsInChildren<Transform>(true))
            {
                bool front=item.name.StartsWith("FrontBulkhead__")||item.name.StartsWith("FrontHeader__")
                    ||item.name.StartsWith("COL_Front__")||item.name.StartsWith("COL_Header__");
                bool centre=item==DetailedHall.Portals[1]||item.IsChildOf(DetailedHall.Portals[1]);
                if(!front&&!centre)continue;
                var renderer=item.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;
                foreach(var collider in item.GetComponents<Collider>())collider.enabled=false;
            }
            float previous=angle-half;
            for(int gate=-1;gate<=1;gate++)
            {
                float heading=angle+gate*6.5f,opening=gate==0?2.4f:1.7f,begin=heading-opening,end=heading+opening;
                Solid("Drohnenhallenfront / geschlossene Druckwand",parent,53.9f,54.15f,previous,begin,0,5.5f,hullA);
                Solid("Drohnenhallenfront / Portalsturz",parent,53.85f,54.2f,begin,end,gate==0?4.4f:3.4f,5.5f,hullB);
                previous=end;
            }
            Solid("Drohnenhallenfront / geschlossene Druckwand",parent,53.9f,54.15f,previous,angle+half,0,5.5f,hullA);
            var openingFrame=Child("Drohnen-Schleusenportal / volle Rumpfbreite",parent);
            openingFrame.localPosition=At(54,angle);openingFrame.localRotation=Quaternion.Euler(0,angle,0);
            foreach(float x in new[]{-2.47f,2.47f})
            {
                Box("Verankerter Drohnenportalpfosten",openingFrame,new Vector3(x,2.2f,0),new Vector3(.42f,4.4f,.65f),frame);
                Box("Drohnenportal Positionslicht",openingFrame,new Vector3(x,3.5f,.36f),new Vector3(.06f,.65f,.04f),cyan,0,false);
            }
            Box("Drohnenportal Quertraeger",openingFrame,new Vector3(0,4.59f,0),new Vector3(5.35f,.38f,.65f),frame);
        }

        private void BuildExteriorDeck(Transform parent,float angle)
        {
            var deck=Child("Aussendeck / vergroesserte Drohnenversorgung",parent);
            deck.localPosition=At(54,angle);deck.localRotation=Quaternion.Euler(0,angle,0);
            // Lines are connected to the hall service edge, then form separate row
            // manifolds. Every branch meets its own cradle quick disconnect.
            void Line(string name,Vector3 a,Vector3 b,float thickness,Material surface)
            {
                var part=Box(name,deck,(a+b)*.5f,new Vector3(thickness,Vector3.Distance(a,b),thickness),surface,0,false);
                part.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            }
            var waterSurface=Surface(new Color(.11f,.32f,.46f),.6f);
            var cableSurface=Surface(new Color(.31f,.24f,.1f),.35f);
            foreach(bool water in new[]{true,false})
            {
                float x=water?12.1f:11.85f,y=water?.19f:.11f,slope=Mathf.Tan(13.5f*Mathf.Deg2Rad);var surface=water?waterSurface:cableSurface;
                string medium=water?"Wasser":"Strom";
                Line("Hallenauslass / "+medium,new Vector3(x,3.5f,-1),new Vector3(x,y,0),water?.15f:.11f,surface);
                Line("Deckhauptleitung / "+medium,new Vector3(x,y,0),new Vector3(x+slope*(LowerBerthRadius-54),y,LowerBerthRadius-54),water?.15f:.11f,surface);
                foreach(float row in new[]{UpperBerthRadius-54,LowerBerthRadius-54})
                {
                    float z=row-1.15f;
                    float outerX=Mathf.Max(x+slope*z,2*BerthSpacing+2.1f);
                    Line("Reihenverteiler / "+medium,new Vector3(-2*BerthSpacing-2.1f,y,z),new Vector3(outerX,y,z),water?.12f:.09f,surface);
                    for(int i=-2;i<=2;i++)
                    {
                        float branch=i*BerthSpacing+BerthSupplyOffset;
                        float contactHeight=BerthHeight-(water?.5f:.6f);
                        Line("Einzelabgang / "+medium,new Vector3(branch,y,z),new Vector3(branch,contactHeight,z),water?.10f:.075f,surface);
                    }
                }
                Box("Abgesicherter Hallenanschluss / "+medium,deck,new Vector3(x,3.5f,-.8f),new Vector3(.5f,.6f,.45f),dark,0,false);
            }
            // The centre lane stays free between the large pressure chamber and both rows.
            foreach(float x in new[]{-3.4f,3.4f})
                Line("Freie Schleusenfahrgasse",new Vector3(x,.025f,6.3f),new Vector3(x,.025f,9.8f),.055f,cyan);
            foreach(float z in new[]{17.1f,21.4f})
                Line("Quergasse zwischen bodengleichen Dockreihen",new Vector3(-15,.025f,z),new Vector3(15,.025f,z),.055f,cyan);
            for(int i=0;i<3;i++)foreach(float side in new[]{-1f,1f})
            {
                float depth=BayOuterRadius-54,z=Mathf.Lerp(4,depth-2,i*.5f),x=side*(12.2f+z*.2f);
                Box("Deckrand / versenkte Positionsleuchte",deck,new Vector3(x,.2f,z),new Vector3(.24f,.12f,.55f),cyan,0,false);
                Box("Deckunterbau / radialer Tragsteg",deck,new Vector3(side*(2*BerthSpacing-i*2),-.55f,depth*.5f),new Vector3(.22f,.8f,depth),frame,0,false);
            }
            Sign("AUSSENDOCK / 10 MEDIENPLAETZE\nSCHLEUSENFAHRGASSE FREIHALTEN",deck,new Vector3(0,5.05f,6.2f),180,.04f);
        }
    }
}

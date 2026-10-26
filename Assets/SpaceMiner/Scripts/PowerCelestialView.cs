using UnityEngine;

namespace SpaceMiner
{
    // Read-only projections of the same celestial coordinates and orbit parameters as RuinedWorld.
    internal sealed class PowerCelestialView
    {
        public int Perspective { get; private set; }
        public int LastBodyCount { get; private set; }
        public Rect PrimaryHit { get; private set; }
        public Rect CompanionHit { get; private set; }
        private static readonly string[] Views = { "Stationsblick", "Doppelstern", "Ganzes System" };
        public void SetPerspective(int value) => Perspective = Mathf.Clamp(value, 0, 2);
        private Texture2D disc;
        private Texture2D Disc
        {
            get
            {
                if (disc != null) return disc;
                disc = new Texture2D(64,64,TextureFormat.RGBA32,false) { name = "Strompult / Sternscheibe" };
                for (int y=0;y<64;y++) for(int x=0;x<64;x++)
                {
                    float d = new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;
                    disc.SetPixel(x,y,new Color(1,1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.86f,1,d))));
                }
                disc.Apply(); return disc;
            }
        }
        public float Draw(float y, float width, BinaryStarView stars, string selected, SpaceMinerUi ui,
            GUIStyle small, System.Action<string> select)
        {
            if (!stars.Known) return y;
            float buttonWidth = (width-38)/3;
            for(int i=0;i<3;i++) if(ui.Tab(new Rect(14+i*(buttonWidth+5),y,buttonWidth,38),Views[i],Perspective==i)) SetPerspective(i);
            y+=46;
            var rect = new Rect(14,y,width-28,248);
            UiChrome.Panel(rect,SpaceMinerUi.Cyan,false);
            var plot = new Rect(rect.x+22,rect.y+30,rect.width-44,rect.height-62);
            Vector3 a=stars.Primary.Direction*(float)stars.Primary.DistanceAu;
            Vector3 b=stars.Companion.Direction*(float)stars.Companion.DistanceAu;
            Vector3 center, right, up;
            float extent;
            if(Perspective==0)
            {
                // Angular sky view: the station is the observer, not a third body at a fake distance.
                Vector3 forward=(a.normalized+b.normalized).normalized;
                right=Vector3.Cross(Vector3.up,forward).normalized; up=Vector3.Cross(forward,right).normalized;
                a=a.normalized;b=b.normalized;center=(a+b)*.5f;extent=Mathf.Max(.16f,(a-b).magnitude*.8f);
            }
            else if(Perspective==1)
            {
                right=(b-a).normalized;up=Vector3.Cross(Vector3.Cross(right,Vector3.up).normalized,right).normalized;
                center=(a+b)*.5f;extent=Mathf.Max(.1f,(a-b).magnitude*.75f);
            }
            else {center=a;right=Vector3.right;up=Vector3.forward;extent=RuinedWorld.OrbitDisplayRadius(8)/RuinedWorld.AstronomicalUnit*1.14f;}
            float scale=Mathf.Min(plot.width,plot.height)/(2*extent);
            Vector2 Project(Vector3 p)=>plot.center+new Vector2(Vector3.Dot(p-center,right),-Vector3.Dot(p-center,up))*scale;
            PowerConsoleView.PowerLine(new Vector2(plot.x,plot.center.y),new Vector2(plot.xMax,plot.center.y),new Color(.08f,.19f,.24f),1);
            PowerConsoleView.PowerLine(new Vector2(plot.center.x,plot.y),new Vector2(plot.center.x,plot.yMax),new Color(.08f,.19f,.24f),1);
            LastBodyCount=2;
            if(Perspective==2)
            {
                Vector3 observer=RuinedWorld.SunPosition/RuinedWorld.AstronomicalUnit-a;
                var world=Object.FindFirstObjectByType<RuinedWorld>();
                for(int i=0;i<9;i++)
                {
                    float radius=RuinedWorld.OrbitDisplayRadius(i)/RuinedWorld.AstronomicalUnit;
                    Vector2 previous=Project(a+Quaternion.Euler(RuinedWorld.OrbitInclination(i),0,0)*Vector3.right*radius);
                    for(int j=1;j<=72;j++)
                    {
                        float angle=j*Mathf.PI*2/72;
                        var point=Project(a+Quaternion.Euler(RuinedWorld.OrbitInclination(i),0,0)*new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
                        PowerConsoleView.PowerLine(previous,point,new Color(.12f,.28f,.34f),1);previous=point;
                    }
                    var position=(world!=null?world.PlanetDisplayPosition(i):RuinedWorld.ReferencePlanetPosition(i))/RuinedWorld.AstronomicalUnit-observer;
                    var p=Project(position);
                    DrawDisc(p,3.5f,SpaceMinerUi.Cyan);
                    GUI.Label(new Rect(p.x+5,p.y-9,30,20),(i+1).ToString("00"),small);
                    LastBodyCount++;
                }
                var station=Project(Vector3.zero);
                SpaceMinerUi.Fill(new Rect(station.x-3,station.y-3,6,6),Color.white);
                GUI.Label(new Rect(Mathf.Clamp(station.x-66,rect.x+6,rect.xMax-130),station.y+10,130,24),"Station",small);
            }
            DrawStar(Project(a),"sun-primary","A",new Color(1,.7f,.32f));
            DrawStar(Project(b),"sun-companion","B",new Color(1,.46f,.23f));
            void DrawStar(Vector2 p,string id,string caption,Color color)
            {
                float radius=Perspective==2?6:Perspective==0?13:20;
                DrawDisc(p,radius*1.7f,new Color(color.r,color.g,color.b,.12f));
                DrawDisc(p,radius,color);
                float labelX=Perspective==2?p.x+(id=="sun-primary"?-64:10):p.x-27;
                var button=new Rect(Mathf.Clamp(labelX,rect.x+3,rect.xMax-57),p.y-radius-34,54,28);
                if(id=="sun-primary")PrimaryHit=button;else CompanionHit=button;
                PowerConsoleView.PowerLine(p,button.center,color,1);
                if(ui.Tab(button,caption,selected==id))select(id);
            }
            GUI.Label(new Rect(rect.x+10,rect.y+5,rect.width-20,24),Perspective==0?"RICHTUNGEN AM STATIONSSTANDORT":Perspective==1?"BEIDE STERNE / RÄUMLICHE PROJEKTION":"NEUN PLANETENBAHNEN / STATIONSSTANDORT",small);
            GUI.Label(new Rect(rect.x+10,rect.yMax-28,rect.width-20,24),Perspective==0?"A / B auswählen · Sterne vergrößert":Perspective==1?"Abstände maßstäblich · Sternscheiben vergrößert":"Bahnen aus dem Spiel · Körper vergrößert",small);
            return rect.yMax+12;
        }
        private void DrawDisc(Vector2 p,float radius,Color color)
        {
            var previous=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),Disc);GUI.color=previous;
        }
        public void Dispose(){if(disc!=null)Object.Destroy(disc);disc=null;}
    }
}

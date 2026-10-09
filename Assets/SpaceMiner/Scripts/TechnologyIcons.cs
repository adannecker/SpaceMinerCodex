using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Locally drawn pictograms: no font-glyph dependency and no downloaded icon package.
    public sealed class TechnologyIcons : IDisposable
    {
        private readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
        private Color[] pixels;
        private const int Size=128;
        public Texture2D Get(string name)
        {
            if(cache.TryGetValue(name,out var found))return found;
            pixels=new Color[Size*Size];
            switch(name) {
                                case "laboratory":Path(7,53,57,53);Box(17,32,30,21);Path(17,32,19,23,25,16,32,13,39,16,45,23,47,32);Line(32,13,32,4);Box(26,38,12,15);Line(9,53,9,39);Line(55,53,55,39);break;
                case "chart":Box(10,7,44,50);FillBox(18,33,5,12);FillBox(28,25,5,20);FillBox(38,16,5,29);Line(17,50,45,50);break;
                case "optimization":Circle(20,39,11);Circle(20,39,4);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(20+11*Mathf.Cos(a),39+11*Mathf.Sin(a),20+15*Mathf.Cos(a),39+15*Mathf.Sin(a));}Path(32,26,52,7,52,18);Line(52,7,41,7);FillBox(37,41,5,14);FillBox(47,32,5,23);break;
                case "monitor":Monitor();Path(13,31,22,31,26,20,32,39,38,26,42,31,51,31);break;
                case "wrench":Path(40,8,34,16,38,25,47,29,55,23,52,36,40,40,19,58,8,48,29,28,26,16,30,7,31,19,37,23,43,18,40,8);Circle(15,49,3);break;
                case "cpu":Box(17,17,30,30);Box(24,24,16,16);for(int i=0;i<5;i++){float q=20+i*6;Line(q,8,q,17);Line(q,47,q,56);Line(8,q,17,q);Line(47,q,56,q);}break;
                case "fan":Circle(32,32,25);Circle(32,32,5);for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Polygon(32+8*Mathf.Cos(a),32+8*Mathf.Sin(a),32+21*Mathf.Cos(a+.2f),32+21*Mathf.Sin(a+.2f),32+20*Mathf.Cos(a+.8f),32+20*Mathf.Sin(a+.8f),32+8*Mathf.Cos(a+1),32+8*Mathf.Sin(a+1));}break;
                case "server":for(int i=0;i<3;i++){Box(9,7+i*18,46,13);FillBox(14,12+i*18,4,4);Line(25,13+i*18,48,13+i*18);}break;
                case "database":Database();break;
                case "analysis":Box(9,6,33,45);Line(16,15,34,15);Line(16,23,31,23);Line(16,31,26,31);Circle(40,38,12);Line(48,47,58,58);break;
                case "quadcopter":Drone(false);break;
                case "miningdrone":Drone(false);Path(34,36,48,47,52,54,56,50,49,43);break;
                case "repairdrone":Drone(false);Path(32,35,40,46,37,53,44,58,52,50,47,43,40,46);break;
                case "solar":Solar();break;
                case "solartrack":Solar();Circle(49,12,7);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(49+9*Mathf.Cos(a),12+9*Mathf.Sin(a),49+12*Mathf.Cos(a),12+12*Mathf.Sin(a));}Path(9,43,6,35,10,28);break;
                case "batteries":for(int i=0;i<3;i++){Box(5+i*20,17,14,36);Box(9+i*20,12,6,5);Path(13+i*20,25,9+i*20,35,15+i*20,35,11+i*20,45);}break;
                case "distribution":Box(24,5,16,17);Path(32,22,32,32,10,32,10,44);Path(32,32,54,32,54,44);Line(32,32,32,44);Box(4,44,12,12);Box(26,44,12,12);Box(48,44,12,12);break;
                case "fastcharge":Box(9,8,25,45);Box(15,4,13,4);Path(23,16,15,31,24,31,19,45);Path(37,42,39,30,45,24,53,25,59,31,60,42);Line(49,39,56,29);Circle(49,40,3);break;
                case "radiator":for(int i=0;i<7;i++)Box(8+i*7,24,4,30);for(int i=0;i<3;i++)Path(18+i*14,19,15+i*14,14,19+i*14,8,16+i*14,3);break;
                case "antenna":Path(9,17,29,37,43,21);Line(24,23,39,8);Circle(41,6,3);Path(29,37,29,52,16,52,43,52);Path(43,5,54,14,57,27);break;
                case "map":Path(6,13,23,7,41,14,58,7,58,50,41,57,23,50,6,57,6,13);Line(23,7,23,50);Line(41,14,41,57);Path(14,26,25,34,38,28,49,40);break;
                case "camera":Box(7,18,50,35);Path(18,18,22,10,39,10,44,18);Circle(33,35,12);Circle(33,35,7);FillBox(12,24,5,4);break;
                case "terrain":Path(5,51,23,20,33,36,43,12,60,51,5,51);Path(17,31,23,35,28,28);Line(6,58,58,58);break;
                case "sample":Box(13,12,11,39);Box(11,7,15,5);Box(39,12,11,39);Box(37,7,15,5);FillBox(15,33,7,16);FillBox(41,25,7,24);Line(7,56,57,56);break;
                case "spectrum":Box(5,7,54,49);for(int i=0;i<8;i++)Line(12+i*6,47,12+i*6,16+(i%3)*8);Line(10,51,54,51);break;
                case "microscope":Path(27,8,39,13,31,31,20,26,27,8);Path(36,17,46,25,48,37,41,48,20,48);Line(14,36,36,36);Line(31,48,31,56);Line(13,56,51,56);Circle(22,26,4);break;
                case "assembly":Path(10,53,10,44,26,44,26,37,35,31,29,23,41,13,50,20,40,31,35,31);Box(43,40,14,14);Line(5,58,59,58);Circle(28,42,4);Circle(36,29,4);break;
                case "furnace":Box(12,13,40,44);Box(21,31,22,20);Path(24,46,22,39,31,29,30,39,39,34,41,43,35,48);Box(18,5,9,8);break;
                case "press":Box(8,7,48,8);Line(13,15,13,54);Line(51,15,51,54);Box(26,15,12,17);Box(19,32,26,7);Box(24,45,16,8);Line(8,56,56,56);break;
                case "drill":Box(8,9,43,14);Box(41,23,10,32);Line(8,56,57,56);Path(17,23,17,34,23,44,29,34,29,23);Line(13,31,33,31);break;
                case "reactor":Box(12,16,40,33);Path(12,16,20,8,44,8,52,16);Line(32,4,32,45);Line(22,38,42,38);Path(12,34,5,34,5,55);Path(52,23,59,23,59,50);Line(20,49,20,58);Line(44,49,44,58);break;
                case "molecule":Circle(31,31,9);Circle(12,12,6);Circle(52,13,6);Circle(12,51,6);Circle(52,51,6);Line(17,17,25,25);Line(37,25,47,18);Line(25,37,17,46);Line(37,37,47,46);break;
                case "circuit":Box(6,9,52,46);Box(23,22,18,18);Path(23,27,14,27,14,16);Path(23,35,13,35,13,47);Path(41,27,49,27,49,17);Path(41,35,50,35,50,48);Circle(14,16,2);Circle(49,17,2);break;
                case "wafer":Circle(32,32,26);for(int i=0;i<5;i++){float q=16+i*8;Line(q,12,q,52);Line(12,q,52,q);}break;
                case "quarantine":Box(9,8,46,46);Path(32,17,15,46,49,46,32,17);Line(32,27,32,35);Circle(32,41,1);break;
                case "tank":Database();Line(10,53,10,59);Line(54,53,54,59);Line(32,5,32,1);break;
                case "gastank":Path(20,57,16,51,16,16,23,9,41,9,48,16,48,51,44,57,20,57);Box(25,4,14,5);Line(21,4,43,4);Line(17,21,47,21);break;
                case "cart":Box(8,19,39,27);Path(47,46,53,10,60,10);Circle(17,53,5);Circle(43,53,5);break;
                case "dock":Path(8,8,8,54,24,54,24,42);Path(56,8,56,54,40,54,40,42);Box(22,17,20,22);Line(32,4,32,17);break;
                case "watercycle":Path(32,17,22,31,21,40,28,47,36,47,43,40,42,31,32,17);Path(11,39,5,31,8,17,19,8,33,5,47,10,53,21);Path(53,12,53,21,44,18);Path(53,35,58,42,52,53,40,59,24,58,13,51);break;
                case "seal":Circle(32,32,25);Circle(32,32,14);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Circle(32+20*Mathf.Cos(a),32+20*Mathf.Sin(a),2);}break;
                case "thermometer":Circle(30,48,10);Path(24,39,24,11,28,6,33,6,37,11,37,40);Line(30,15,30,47);Line(42,17,51,17);Line(42,28,48,28);break;
                case "food":Path(8,22,14,13,50,13,56,22,50,52,14,52,8,22);Line(16,22,48,22);Path(25,32,32,29,40,32,32,39,25,32);break;
                case "blueprint":Box(8,7,48,50);Box(15,18,34,28);Line(25,18,25,46);Line(25,33,49,33);Line(12,11,12,53);Line(7,51,56,51);break;
                case "gauge":Circle(32,32,25);Path(14,45,14,35,20,22,32,18,44,22,50,35,50,45);Line(32,37,44,23);Circle(32,37,4);break;
                case "habitat":Box(7,18,50,31);Box(15,25,13,15);Box(36,25,13,15);Line(2,32,7,32);Line(57,32,62,32);Line(18,49,18,56);Line(46,49,46,56);break;
                case "dome":Path(7,49,7,35,11,23,20,14,32,10,44,14,53,23,57,35,57,49,7,49);Path(20,49,20,30,24,15);Path(44,49,44,30,40,15);Line(32,10,32,49);Line(7,35,57,35);Line(4,55,60,55);break;
                case "compass":Circle(32,32,25);Polygon(40,13,35,36,24,51,29,28,40,13);Line(32,2,32,9);Line(32,55,32,62);Line(2,32,9,32);Line(55,32,62,32);break;
                case "wreck":Path(7,44,19,18,32,26,45,9,56,28,48,53,33,47,21,57,7,44);Line(19,18,29,39);Line(32,26,41,40);Line(45,9,47,28);break;
                case "salvage":Path(5,50,5,17,30,17,44,8,55,18,42,34,35,31);Line(30,17,42,27);Path(48,27,48,44,41,50,35,44);Line(1,56,23,56);break;
                case "recycle":Path(21,15,29,6,39,8,48,22,54,18,52,34,38,29,44,25);Path(50,43,46,55,34,57,19,55,19,61,8,50,20,40,20,47);Path(8,40,5,29,12,20,20,10,14,8,29,5,32,20,26,17);break;
                case "disassemble":Box(7,7,18,18);Box(39,39,18,18);Path(29,29,43,15,53,17,57,10);Path(29,29,15,43,17,53,10,57);Line(23,41,41,23);break;
                case "sorting":Path(7,8,57,8,39,28,25,28,7,8);Path(32,28,32,38,13,38,13,48);Path(32,38,51,38,51,48);Box(5,48,16,11);Box(43,48,16,11);break;
                case "ingot":Path(7,39,19,23,48,23,58,39,48,48,17,48,7,39,58,39);Line(19,23,17,39);Line(48,23,48,39);break;
                case "coil":for(int i=0;i<5;i++){Circle(17+i*7,32,14);}Line(6,32,2,44);Line(58,32,62,20);break;
                case "glass":Path(16,6,49,6,45,56,20,56,16,6);Path(23,16,40,16,38,43,25,43,23,16);Line(23,43,40,25);break;
                case "powder":Path(8,14,56,14,51,55,13,55,8,14);Line(8,14,15,6);Line(56,14,49,6);Line(15,6,49,6);for(int i=0;i<9;i++)Circle(20+(i%3)*12,30+(i/3)*8,2);break;
                case "graphite":for(int i=0;i<3;i++){float q=i*12;Path(10+q,14,17+q,8,23+q,14,23+q,49,17+q,56,10+q,49,10+q,14);Line(17+q,18,17+q,49);}break;
                case "polymer":for(int i=0;i<4;i++){float x=11+i*14,y=i%2==0?21:43;Circle(x,y,7);if(i>0)Line(x-9,y+(i%2==0?10:-10),x-5,y+(i%2==0?5:-5));}break;
                case "electrolysis":Box(10,18,44,37);Line(10,40,54,40);Line(23,8,23,45);Line(41,8,41,45);Line(18,8,28,8);Line(36,8,46,8);Line(41,3,41,13);Circle(20,47,2);Circle(43,47,2);Circle(28,34,2);Circle(38,31,2);break;
                case "electrolyte":Box(15,13,34,44);Box(22,6,20,7);Path(15,38,23,34,32,39,42,33,49,38);Path(32,20,25,31,32,31,28,40,39,27,32,27);break;
                case "resin":Box(12,15,40,40);Box(19,7,26,8);Path(31,24,23,36,23,44,30,49,37,44,38,36,31,24);break;
                case "close":Line(16,16,48,48);Line(48,16,16,48);break;
                case "settings":
                    Circle(32,32,18);Circle(32,32,7);
                    for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;Line(32+18*Mathf.Cos(angle),32+18*Mathf.Sin(angle),32+25*Mathf.Cos(angle),32+25*Mathf.Sin(angle));}break;
                case "research":
                case "workflow":
                    Box(7,23,13,16);Box(43,5,13,16);Box(43,43,13,16);Line(20,31,32,31);Line(32,13,32,51);Line(32,13,43,13);Line(32,51,43,51);break;
                case "station":
                    Circle(32,32,15);Box(25,25,14,14);Line(5,32,17,32);Line(47,32,59,32);Box(2,21,7,22);Box(55,21,7,22);break;
                case "pickaxe":Line(14,54,43,15);Path(10,15,24,9,43,15,53,27);break;
                case "flask":Path(24,7,40,7);Path(27,7,27,25,13,50,16,55,48,55,51,50,37,25,37,7);Line(22,39,43,39);break;
                case "radar":Circle(32,32,23);Circle(32,32,14);Circle(32,32,4);Line(32,32,49,15);break;
                case "scan":Path(8,22,8,8,22,8);Path(42,8,56,8,56,22);Path(56,42,56,56,42,56);Path(22,56,8,56,8,42);Circle(30,29,11);Line(38,38,47,47);break;
                case "power":Path(36,5,17,34,30,34,24,58,48,25,34,25,36,5);break;
                case "cargo":Path(10,19,32,8,54,19,54,46,32,57,10,46,10,19,32,30,54,19);Line(32,30,32,57);Line(22,13,43,24);break;
                case "robot":Box(13,20,38,30);Line(32,20,32,10);Circle(32,7,3);Circle(23,32,3);Circle(41,32,3);Line(23,42,41,42);Line(7,28,7,43);Line(57,28,57,43);break;
                case "ice":for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;Vector2 v=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),normal=new Vector2(-v.y,v.x);Line(32,32,32+v.x*25,32+v.y*25);Vector2 point=new Vector2(32,32)+v*16;Line(point.x,point.y,point.x-v.x*7+normal.x*7,point.y-v.y*7+normal.y*7);Line(point.x,point.y,point.x-v.x*7-normal.x*7,point.y-v.y*7-normal.y*7);}break;
                case "water":
                case "autowater":Path(32,6,17,27,12,39,14,49,23,56,41,56,50,49,52,39,47,27,32,6);if(name=="autowater"){Path(22,40,27,34,40,34,44,39);Path(44,33,44,39,38,39);Path(42,45,37,51,24,51,20,46);}break;
                case "battery":Box(18,12,28,45);Box(26,6,12,6);Line(23,25,41,25);Line(23,34,41,34);Line(23,43,41,43);break;
                case "route":Circle(14,16,7);Circle(49,48,7);Path(14,23,14,36,32,36,32,20,49,20,49,41);break;
                case "tasks":for(int i=0;i<3;i++){float y=14+i*18;Path(8,y,12,y+4,19,y-5);Line(28,y,55,y);}break;
                case "filter":Path(8,10,56,10,38,32,38,52,26,58,26,32,8,10);break;
                case "gem":Path(17,9,47,9,58,25,32,57,6,25,17,9);Line(6,25,58,25);Path(17,9,24,25,32,57,40,25,47,9);break;
            }
            var texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false){name="UI icon "+name,hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
            texture.SetPixels(pixels);texture.Apply(false,true);cache.Add(name,texture);return texture;
        }
                private void FillBox(float x,float y,float w,float h){for(float q=y;q<y+h;q++)Line(x,q,x+w,q);}
        private void Polygon(params float[] points)
        {
            float min=64,max=0;
            for(int i=1;i<points.Length;i+=2){min=Mathf.Min(min,points[i]);max=Mathf.Max(max,points[i]);}
            for(float y=min;y<=max;y++){
                var hits=new List<float>();int count=points.Length/2;
                for(int i=0;i<count;i++){
                    int j=(i+1)%count;float ay=points[i*2+1],by=points[j*2+1];
                    if((ay<=y&&by>y)||(by<=y&&ay>y))hits.Add(points[i*2]+(y-ay)*(points[j*2]-points[i*2])/(by-ay));
                }
                hits.Sort();for(int i=1;i<hits.Count;i+=2)Line(hits[i-1],y,hits[i],y);
            }
        }
        private void Monitor(){Box(7,9,50,36);Line(32,45,32,54);Line(21,55,43,55);}
        private void Database(){Box(11,13,42,37);Ellipse(32,13,21,7);Ellipse(32,26,21,7);Ellipse(32,38,21,7);Ellipse(32,50,21,7);}
        private void Ellipse(float x,float y,float rx,float ry){for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;Line(x+rx*Mathf.Cos(a),y+ry*Mathf.Sin(a),x+rx*Mathf.Cos(b),y+ry*Mathf.Sin(b));}}
        private void Drone(bool unused){Box(24,24,16,16);for(int i=0;i<4;i++){float x=i%2==0?13:51,y=i<2?13:51;Circle(x,y,10);Line(32,32,x,y);}}
        private void Solar(){Path(15,17,49,17,56,45,8,45,15,17);for(int i=0;i<3;i++){float y=24+i*7;Line(13-i*2,y,51+i*2,y);}Line(24,17,21,45);Line(39,17,42,45);Line(32,45,32,55);Line(21,56,43,56);}
        private void Box(float x,float y,float w,float h)=>Path(x,y,x+w,y,x+w,y+h,x,y+h,x,y);
        private void Circle(float x,float y,float radius){for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;Line(x+radius*Mathf.Cos(a),y+radius*Mathf.Sin(a),x+radius*Mathf.Cos(b),y+radius*Mathf.Sin(b));}}
        private void Path(params float[] coords){for(int i=2;i<coords.Length;i+=2)Line(coords[i-2],coords[i-1],coords[i],coords[i+1]);}
        private void Line(float ax,float ay,float bx,float by)
        {
            int steps=Mathf.CeilToInt(Mathf.Max(Mathf.Abs(bx-ax),Mathf.Abs(by-ay))*4);
            for(int i=0;i<=steps;i++){float t=steps==0?0:(float)i/steps;int x=Mathf.RoundToInt(Mathf.Lerp(ax,bx,t)*2),y=Size-1-Mathf.RoundToInt(Mathf.Lerp(ay,by,t)*2);
                for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)if(x+dx>=0&&x+dx<Size&&y+dy>=0&&y+dy<Size)pixels[(y+dy)*Size+x+dx]=Color.white;}
        }
        public void Draw(Rect rect,string name,Color color){Color previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Get(name));GUI.color=previous;}
        public void Dispose(){foreach(var texture in cache.Values)UnityEngine.Object.Destroy(texture);cache.Clear();}
    }
}

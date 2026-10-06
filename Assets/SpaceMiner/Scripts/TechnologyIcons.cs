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
        private const int Size=64;
        public Texture2D Get(string name)
        {
            if(cache.TryGetValue(name,out var found))return found;
            pixels=new Color[Size*Size];
            switch(name) {
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
        private void Box(float x,float y,float w,float h)=>Path(x,y,x+w,y,x+w,y+h,x,y+h,x,y);
        private void Circle(float x,float y,float radius){for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;Line(x+radius*Mathf.Cos(a),y+radius*Mathf.Sin(a),x+radius*Mathf.Cos(b),y+radius*Mathf.Sin(b));}}
        private void Path(params float[] coords){for(int i=2;i<coords.Length;i+=2)Line(coords[i-2],coords[i-1],coords[i],coords[i+1]);}
        private void Line(float ax,float ay,float bx,float by)
        {
            int steps=Mathf.CeilToInt(Mathf.Max(Mathf.Abs(bx-ax),Mathf.Abs(by-ay))*2);
            for(int i=0;i<=steps;i++){float t=steps==0?0:(float)i/steps;int x=Mathf.RoundToInt(Mathf.Lerp(ax,bx,t)),y=Size-1-Mathf.RoundToInt(Mathf.Lerp(ay,by,t));
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(x+dx>=0&&x+dx<Size&&y+dy>=0&&y+dy<Size)pixels[(y+dy)*Size+x+dx]=Color.white;}
        }
        public void Draw(Rect rect,string name,Color color){Color previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Get(name));GUI.color=previous;}
        public void Dispose(){foreach(var texture in cache.Values)UnityEngine.Object.Destroy(texture);cache.Clear();}
    }
}

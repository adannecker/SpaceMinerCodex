using System;
using UnityEngine;

namespace SpaceMiner
{
    // One visor communication panel for live dialogue, awakening and story cinematics.
    public sealed class MiraVisorOverlay : IDisposable
    {
        private readonly SpaceMinerUi ui=new SpaceMinerUi();
        private Texture2D portraits;
        private Vector2 scroll;
        private string previousText;
        public static Rect Bounds(float width,float height)
        {
            float margin=28,w=Mathf.Min(1160,width-margin*2),h=Mathf.Min(288,height*.36f);
            return new Rect(margin,height-h-margin,w,h);
        }
        public struct Layout { public Rect Portrait,Title,Body,Footer; }
        public static Layout Arrange(Rect bounds)
        {
            float portraitSize=Mathf.Min(190,bounds.height-72);
            float textX=bounds.x+portraitSize+48;
            return new Layout{
                Portrait=new Rect(bounds.x+18,bounds.y+38,portraitSize,portraitSize),
                Title=new Rect(textX,bounds.y+18,bounds.xMax-textX-20,38),
                Body=new Rect(textX,bounds.y+64,bounds.xMax-textX-20,bounds.height-124),
                Footer=new Rect(textX,bounds.yMax-48,bounds.xMax-textX-20,32)
            };
        }
        public bool Draw(Rect bounds,string title,string text,MiraAvatar.Mood mood=MiraAvatar.Mood.Focused,string hint="",string action=null,bool showText=true,float alpha=1)
        {
            ui.Configure(SettingsStore.Current.Accessibility);
            if(portraits==null)portraits=Resources.Load<Texture2D>("Mira/MiraDialoguePortraits");
            if(previousText!=text){scroll=Vector2.zero;previousText=text;}
            var layout=Arrange(bounds);var color=GUI.color;
            try{
                GUI.color=new Color(1,1,1,alpha);
                Fill(bounds,new Color(.012f,.035f,.052f,.94f),alpha);
                Frame(bounds,new Color(.25f,.68f,.77f,.75f),alpha);
                var frame=new Rect(layout.Portrait.x-3,layout.Portrait.y-3,layout.Portrait.width+6,layout.Portrait.height+6);
                Fill(frame,new Color(.02f,.085f,.10f,.8f),alpha);Frame(frame,SpaceMinerUi.Cyan,alpha);
                if(portraits!=null){int cell=(int)mood;GUI.DrawTextureWithTexCoords(layout.Portrait,portraits,new Rect(cell%2*.5f,cell<2?.5f:0,.5f,.5f),true);}
                GUI.Label(new Rect(bounds.x+18,bounds.y+13,190,24),"MIRA  /  VERBINDUNG AKTIV",new GUIStyle(ui.Small){fontSize=12});
                var titleStyle=new GUIStyle(ui.Text){fontStyle=FontStyle.Bold};
                layout.Title.height=Mathf.Max(layout.Title.height,titleStyle.CalcHeight(new GUIContent(title),layout.Title.width));
                layout.Body.y=layout.Title.yMax+10;layout.Body.height=layout.Footer.y-layout.Body.y-14;
                GUI.Label(layout.Title,title,titleStyle);
                if(showText&&!string.IsNullOrEmpty(text)){
                    var style=new GUIStyle(ui.Text){fontSize=Mathf.RoundToInt(21*SettingsStore.Current.Accessibility.TextScale)};
                    float contentHeight=Mathf.Max(layout.Body.height,style.CalcHeight(new GUIContent(text),layout.Body.width-22)+8);
                    scroll=GUI.BeginScrollView(layout.Body,scroll,new Rect(0,0,layout.Body.width-22,contentHeight),false,false);
                    GUI.Label(new Rect(0,0,layout.Body.width-22,contentHeight),text,style);GUI.EndScrollView();
                }
                float actionWidth=string.IsNullOrEmpty(action)?0:166;
                GUI.Label(new Rect(layout.Footer.x,layout.Footer.y,layout.Footer.width-actionWidth-12,32),hint,ui.Small);
                if(actionWidth>0&&GUI.Button(new Rect(layout.Footer.xMax-actionWidth,layout.Footer.y,actionWidth,32),action,ui.Button)){SettingsUiAudio.Activate();return true;}
                return false;
            }finally{GUI.color=color;}
        }
        private static void Fill(Rect rect,Color color,float alpha){color.a*=alpha;SpaceMinerUi.Fill(rect,color);}
        private static void Frame(Rect rect,Color color,float alpha)
        {
            color.a*=alpha;SpaceMinerUi.Border(rect,new Color(color.r,color.g,color.b,color.a*.4f));
            const float arm=18,t=2;
            foreach(float x in new[]{rect.x,rect.xMax-arm})foreach(float y in new[]{rect.y,rect.yMax-t})SpaceMinerUi.Fill(new Rect(x,y,arm,t),color);
            foreach(float x in new[]{rect.x,rect.xMax-t})foreach(float y in new[]{rect.y,rect.yMax-arm})SpaceMinerUi.Fill(new Rect(x,y,t,arm),color);
        }
        public void Dispose()=>ui.Dispose();
    }
}

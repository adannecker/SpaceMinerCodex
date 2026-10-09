using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Shares the approved realistic atlas and assignments with the standalone preview.
    public sealed class ResearchMiniatures : IDisposable
    {
        [Serializable] public class Entry { public string id,texture; public int columns,rows,column,row; }
        [Serializable] public class Manifest { public Entry[] entries; }
        private readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
        private readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public int Count=>entries.Count;
        public ResearchMiniatures()
        {
            var manifest=JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("Research/miniatures").text);
            foreach(var entry in manifest.entries){
                entries.Add(entry.id,entry);
                if(textures.ContainsKey(entry.texture))continue;
                var original=Resources.Load<Texture2D>("Research/"+entry.texture);
                if(original==null)throw new InvalidOperationException("Research texture missing: "+entry.texture);
                var pixels=original.GetPixels32();
                for(int i=0;i<pixels.Length;i++){
                    var p=pixels[i];float gray=.2126f*p.r+.7152f*p.g+.0722f*p.b;
                    pixels[i]=new Color32((byte)Mathf.Lerp(gray,p.r,.28f),(byte)Mathf.Lerp(gray,p.g,.28f),(byte)Mathf.Lerp(gray,p.b,.28f),p.a);
                }
                var texture=new Texture2D(original.width,original.height,TextureFormat.RGBA32,false){name=entry.texture+"-muted",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                texture.SetPixels32(pixels);texture.Apply(false,true);textures.Add(entry.texture,texture);
            }
        }
        public void Draw(Rect rect,string id)
        {
            var e=entries[id];
            // Keep a safety inset inside each rendered atlas cell; irregular generated margins
            // must not reveal the top of the neighbouring miniature at large display sizes.
            GUI.DrawTextureWithTexCoords(rect,textures[e.texture],new Rect((e.column+.025f)/e.columns,1f-(e.row+1f-.12f)/e.rows,.95f/e.columns,.84f/e.rows),true);
        }
        public void Dispose(){foreach(var texture in textures.Values){if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);}textures.Clear();}
    }
}

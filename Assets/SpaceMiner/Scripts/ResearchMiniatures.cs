using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Shares the approved realistic atlas and assignments with the standalone preview.
    public sealed class ResearchMiniatures : IDisposable
    {
        [Serializable] public class Entry { public string id,texture; public int columns,rows,column,row,imageWidth,imageHeight,cropX,cropY,cropWidth,cropHeight; }
        [Serializable] public class Manifest { public Entry[] entries; }
        private readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
        private readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public int Count=>entries.Count;
        public ResearchMiniatures()
        {
            var manifest=JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("Research/miniatures").text);
            foreach(var entry in manifest.entries){
                if(entry.cropWidth<=0||entry.cropHeight<=0||entry.cropX<0||entry.cropY<0||entry.cropX+entry.cropWidth>entry.imageWidth||entry.cropY+entry.cropHeight>entry.imageHeight)
                    throw new InvalidOperationException("Research crop invalid: "+entry.id);
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
            var texture=textures[e.texture];
            // Explicit measured bounds exclude neighbouring rows. Fit without stretching.
            float scale=Mathf.Min(rect.width/e.cropWidth,rect.height/e.cropHeight);
            var fitted=new Rect(rect.center.x-e.cropWidth*scale*.5f,rect.center.y-e.cropHeight*scale*.5f,e.cropWidth*scale,e.cropHeight*scale);
            // Normalize against the source atlas even if Unity resizes the imported texture.
            GUI.DrawTextureWithTexCoords(fitted,texture,new Rect((float)e.cropX/e.imageWidth,1f-(float)(e.cropY+e.cropHeight)/e.imageHeight,(float)e.cropWidth/e.imageWidth,(float)e.cropHeight/e.imageHeight),true);
        }
        public void Dispose(){foreach(var texture in textures.Values){if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);}textures.Clear();}
    }
}

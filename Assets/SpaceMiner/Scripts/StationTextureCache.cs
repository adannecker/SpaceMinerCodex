using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Generated finishes are immutable texture data. Materials remain per movable assembly.
    internal static class StationTextureCache
    {
        private sealed class Entry {internal Texture2D[] Maps;internal int Users;}
        private static readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
        internal static Texture2D[] Borrow(string key){if(!entries.TryGetValue(key,out var entry))return null;entry.Users++;return entry.Maps;}
        internal static void Store(string key,Texture2D[] maps){entries.Add(key,new Entry{Maps=maps,Users=1});}
        internal static void Release(string key){if(!entries.TryGetValue(key,out var e))return;if(--e.Users>0)return;foreach(var t in e.Maps)if(t!=null)Object.Destroy(t);entries.Remove(key);}
    }
}

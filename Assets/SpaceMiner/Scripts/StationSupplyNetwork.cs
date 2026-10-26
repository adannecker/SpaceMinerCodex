using System.Collections.Generic;

namespace SpaceMiner
{
    // Twelve sector nodes form a redundant ring. Closed breakers/valves or broken sections sever edges.
    public sealed class StationSupplyNetwork
    {
        public bool Connected(IReadOnlyList<StationElementState> elements,int from,int to,bool water)
        {
            int mask=0;foreach(var e in elements)if(e.id.StartsWith("sector-")&&e.condition>=.3f&&(water?e.water:e.power))mask|=1<<e.sector;
            if((mask&(1<<from))==0||(mask&(1<<to))==0)return false;
            int seen=1<<from,wave=seen;
            for(int i=0;i<12;i++){if((seen&(1<<to))!=0)return true;wave=(((wave<<1)|(wave>>1)|((wave&1)<<11)|((wave&2048)>>11))&mask)&~seen;seen|=wave;if(wave==0)return false;}
            return false;
        }
    }
}

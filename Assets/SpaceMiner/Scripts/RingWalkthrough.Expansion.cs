using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SpaceMiner
{
    internal sealed partial class RingWalkthrough
    {
        internal readonly List<StationPressureGate> PressureGates=new List<StationPressureGate>();
        internal StationAirlock BayAirlock { get; private set; }
        internal int ExpansionSlots { get; private set; }
        internal int SideCouplings { get; private set; }
        private readonly HashSet<int> sealedDamageJoints=new HashSet<int>();
        private StationPressureGate Gate(Transform parent,string name,Vector3 position,float yaw,float width,float height,bool locked,bool compactNarrow=false)
        {
            foreach(var item in PressureGates)if(Vector3.Distance(item.transform.localPosition,position)<.1f)return item;
            var node=Child(name,parent);node.localPosition=position;node.localRotation=Quaternion.Euler(0,yaw,0);
            var gate=node.gameObject.AddComponent<StationPressureGate>();gate.Configure(width,height,locked,name,hullB,locked?amber:cyan,compactNarrow);
            PressureGates.Add(gate);return gate;
        }
        internal StationPressureGate NearestGate(Vector3 feet)
        {
            StationPressureGate best=null;float distance=float.PositiveInfinity;
            foreach(var gate in PressureGates)if(gate.Near(feet))
            {float d=Vector3.SqrMagnitude(gate.transform.position-feet);if(d<distance){best=gate;distance=d;}}
            return best;
        }
        private void BuildSegmentInterfaces(Transform parent,int sector)
        {
            float axis=sector*30, boundary=axis-15;
            bool unsafeJoint=DamagedRingSector(sector)||DamagedRingSector((sector+11)%12);
            Gate(parent,"Ringschott S"+(sector+1).ToString("00"),At(CentreRadius,boundary),boundary+90,8.95f,5.4f,unsafeJoint);
            if(OuterStages(sector)==0)for(int slot=0;slot<3;slot++)
            {
                // Pull the two outer mounting centres about one metre towards the segment centre.
                float a=axis+(slot-1)*8.5f;var mount=Child("Anbauslot S"+(sector+1).ToString("00")+" / "+(slot+1),parent);
                mount.localPosition=At(OuterRadius+.05f,a);mount.localRotation=Quaternion.Euler(0,a,0);ExpansionSlots++;
                foreach(float x in new[]{-1.55f,1.55f})Box("Integrierter Andocklaengstraeger",mount,new Vector3(x,2.25f,0),new Vector3(.45f,4.45f,.95f),frame,0,false);
                foreach(float y in new[]{.25f,4.25f})Box("Integrierter Andockquertraeger",mount,new Vector3(0,y,0),new Vector3(3.55f,.45f,.95f),frame,0,false);
                Box("Versenkte druckdichte Anschlusskappe",mount,new Vector3(0,2.25f,.31f),new Vector3(2.65f,3.55f,.24f),hullB,0,false);
                foreach(float x in new[]{-1.55f,1.55f})foreach(float y in new[]{.55f,2.25f,3.95f})
                {
                    Box("Verankerter Tragknoten",mount,new Vector3(x,y,.05f),new Vector3(.62f,.55f,1.15f),dark,0,false);
                    Box("Verriegelungsbacke",mount,new Vector3(x,y,.68f),new Vector3(.34f,.3f,.15f),frame,0,false);
                    foreach(float bx in new[]{-.11f,.11f})Box("Kupplungsbolzen",mount,new Vector3(x+bx,y,.79f),new Vector3(.07f,.08f,.08f),hullA,0,false);
                }
                Box("Gekappter Medienanschluss",mount,new Vector3(.8f,.8f,.55f),new Vector3(.35f,.3f,.35f),dark,0,false);
            }
            if(sector==0)Gate(parent,"Kommandoschott",At(38,axis),axis,7.65f,4.1f,false);
            if(sector==1)
            {
                Gate(parent,"Hallenschott",At(38,axis),axis,6.5f,4.1f,false);
                Gate(parent,"Werkstattschott innen",At(29,axis),axis,5.9f,4.1f,false);
                BuildBayAirlock(parent);
            }
            if(sector%3==0)Gate(parent,"Reaktorschott / "+axis,At(28.75f,axis),axis,2.9f,3.1f,sector!=0,true);
        }
        private void BuildStorageRingWall(Transform parent)
        {
            float opening=4.5f, axis=StorageAngle(0);
            Solid("Ringwand Lager links",parent,37.9f,38.1f,197.2f,axis-opening,0,5.5f,hullA);
            Solid("Ringwand Lager rechts",parent,37.9f,38.1f,axis+opening,222.8f,0,5.5f,hullA);
            Solid("Portalsturz",parent,37.9f,38.1f,axis-opening,axis+opening,4.3f,5.5f,frame);
            Gate(parent,"Wasserlager 01",At(38,axis),axis,5.7f,4.1f,false);
            for(int slot=1;slot<3;slot++)
            {
                float a=StorageAngle(slot);Gate(parent,"Wasserlager "+(slot+1).ToString("00")+" / HAVARIE",At(38.25f,a),a,3.15f,3.1f,true);
                Sign("LAGER "+(slot+1).ToString("00")+" / GESPERRT",parent,At(37.5f,a,3.6f),a,.035f);
            }
        }
        private void BuildResourceStorage(Transform parent,int sector)
        {
            float a=StorageAngle(0),begin=a-4.85f,end=a+4.85f;
            Solid("Wasserlager 01 Boden",parent,38,54,begin,end,-.35f,0,floor);ModuleCount++;
            var roof=Solid("Wasserlager 01 Decke",parent,38.2f,54,begin,end,5.5f,5.75f,hullB);roofs.Add(roof.gameObject);
            foreach(float side in new[]{begin,end})Box("Wasserlager 01 Seitenwand "+(side<a?"L":"R"),parent,At(46,side,2.75f),new Vector3(.2f,5.5f,16),hullA,side);
            // Real outer-front aperture for the existing sealed drone transfer fitting.
            Solid("Wasserlager Abschlusswand",parent,53.9f,54.1f,begin,a-1.15f,0,5.5f,hullA);
            Solid("Wasserlager Abschlusswand",parent,53.9f,54.1f,a+1.15f,end,0,5.5f,hullA);
            Solid("Wasserlager Abschlusswand",parent,53.9f,54.1f,a-1.15f,a+1.15f,0,2.55f,hullA);
            Solid("Wasserlager Abschlusswand",parent,53.9f,54.1f,a-1.15f,a+1.15f,4.05f,5.5f,hullA);
            var bins=Child("Eis-Wasserlager / Behaelter",parent);bins.localPosition=At(46,a);bins.localRotation=Quaternion.Euler(0,a,0);
            foreach(float z in new[]{-3.4f,3.4f})Box("Isolierter Eiscontainer",bins,new Vector3(-1.85f,.65f,z),new Vector3(1.2f,1.3f,1.8f),hullB);
            for(int slot=1;slot<3;slot++)BuildKeilWreck(parent,slot);
            foreach(float side in new[]{205f,215f})
            {
                for(int i=0;i<4;i++)
                {
                    Box("Seitliche Rahmenkupplung",parent,At(39+i*4.6f,side,5.85f),new Vector3(.9f,.45f,1.2f),frame,side,false);SideCouplings++;
                    Box("Kupplungsriegel",parent,At(39+i*4.6f,side,6.05f),new Vector3(.28f,.1f,.45f),dark,side,false);
                }
            }
        }
        private void TornPlate(Transform parent,string name,float ri,float ro,float a,float b,float y,int style)
        {
            // Irregular convex remnants, each pressure/protection layer with its own torn silhouette.
            for(int layer=0;layer<2;layer++)
            {
                float offset=layer==0?0:.38f;
                var polygon=new List<Vector2>{new Vector2(0,0),new Vector2(1,0),new Vector2(.97f,.54f),new Vector2(.79f,.68f),new Vector2(.85f,.81f),new Vector2(style==1?.67f:.85f,1),new Vector2(.48f,.79f),new Vector2(.18f,.87f),new Vector2(0,.6f)};
                if(layer==1){polygon[3]=new Vector2(.72f,.64f);polygon[5]=new Vector2(style==1?.55f:.73f,.93f);}
                var mesh=HoneycombHull.Mesh(name,new List<List<Vector2>>{polygon},(p,d)=>At(Mathf.Lerp(ri,ro,p.y),Mathf.Lerp(a,b,p.x),y+offset+d));meshes.Add(mesh);
                var piece=Child(name+(layer==0?" / Drucklage":" / Aussenschutz"),parent);
                piece.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;piece.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{layer==0?hullA:hullB,dark};
                roofs.Add(piece.gameObject);HoneycombSurfaces++;
            }
        }
        private void BuildKeilWreck(Transform parent,int slot)
        {
            float a=StorageAngle(slot);var wreck=Child("Lagerkeil "+(slot+1)+" / HAVARIERT",parent);DamagedStorageStages++;
            Solid("Erhaltenes Wrackbodenfeld",wreck,38.4f,slot==1?50:45,a-4.8f,a+4.8f,-.3f,-.03f,floor,false);
            TornPlate(wreck,"Aufgerissene Lagerdachhaut",38.5f,slot==1?49:44,a-4.8f,a+4.8f,5.5f,slot);
            if(slot==1)TornPlate(wreck,"Abgeschaelte aeussere Dachpartie",49.2f,54,a+1.1f,a+4.8f,5.85f,slot);
            else TornPlate(wreck,"Haengender Eckrest / befestigt",48,53.5f,a-4.8f,a-1.9f,4.7f,slot);
            foreach(float side in new[]{a-4.85f,a+4.85f})
            {
                Box("Erhaltener Seitenrahmen",wreck,At(46,side,.1f),new Vector3(.3f,.35f,16),frame,side,false);
                Box("Verformter Dachlaengstraeger",wreck,At(slot==1?45:43.5f,side,5.4f),new Vector3(.25f,.3f,slot==1?13:9),frame,side+(slot==1?1.5f:-4),false);
                for(int rib=0;rib<3;rib++)
                {
                    var post=Box("Geknickter Lagerrahmen",wreck,At(39.1f+rib*6,side,2.5f),new Vector3(.3f,4.9f,.32f),frame,side,false);
                    post.localRotation*=Quaternion.Euler(rib==0?0:slot==1?7:-16,0,rib==0?0:slot==1?4:22);
                }
                // Large wall remains, not an evenly scattered collection of rods.
                var wallPiece=Box("Lager Seitenwand "+(side<a?"L":"R")+" / aufgerissen",wreck,At(slot==1?42:40,side,2.3f),new Vector3(.18f,4.5f,slot==1?6.8f:3.2f),hullA,side,false);
                CladBox(wallPiece,wallPiece.name,wallPiece.localScale,wallPiece.localPosition,side);
            }
            for(int i=0;i<3;i++)Box("Gerissene befestigte Huelle",wreck,At(48+i*2,a+(slot==1?2.6f:-3.1f),5.3f-i*.4f),new Vector3(1.2f,.12f,2),hullB,a+i*(slot==1?9:-17),false);
            Sign("LAGER "+(slot+1).ToString("00")+" / DRUCKLOS",wreck,At(38.4f,a,4),a,.04f);
        }
        private void BuildDamagedRing(Transform parent,int sector)
        {
            float a=sector*30;var wreck=Child("Ring S"+(sector+1).ToString("00")+" / DRUCKLOS GESPERRT",parent);
            // Endpoint pads remain behind closed boundary bulkheads. The centre is physically absent.
            foreach(float edge in new[]{a-15,a+15})
            {
                // The healthy segment ends before its joint, so the bulkhead alone leaves a route
                // around both shoulders. Keep its entire transition enclosed, also after repair.
                BuildSealedDamageJoint(parent,edge);
                Gate(parent,"Not-Schott / Ringhavarie",At(CentreRadius,edge),edge+90,8.95f,5.5f,true);
            }
            TornPlate(wreck,"Ringhautriss",29.2f,37.8f,a-12,a+(sector==3?1:9),5.5f,sector==3?1:2);
            Solid("Ringboden / abgerissener Rest",wreck,sector==3?29:34,sector==3?33:38,a-12,a+5,-.55f,-.04f,floor,false);
            foreach(float r in new[]{29.2f,37.8f})
            {
                if (sector % 3 == 0 && r < 30)
                {
                    BuildBatteryBayDamagedRemnants(wreck,a);
                }
                else
                {
                    Solid("Offener Ringtraeger",wreck,r-.12f,r+.12f,a-14,a+14,.1f,.4f,frame,false);
                    Solid("Wandhaut / erhaltener Rest",wreck,r-.1f,r+.1f,a-14,a-7,0,4.8f,hullA,false);
                }
                for(int j=0;j<3;j++)
                {
                    if(sector%3==0&&r<30&&j!=1)continue; // The two old posts stood in the new bay apertures.
                    Box("Verformter Ringrahmen",wreck,At(r,a-9+j*9,2.5f),new Vector3(.3f,5,.35f),frame,a+j*13,false);
                }
            }
            Sign("HAVARIE / KEIN ZUTRITT",parent,At(CentreRadius,a-15.5f,3.5f),a+75,.08f);
        }
        private void BuildSealedDamageJoint(Transform parent,float edge)
        {
            if(!sealedDamageJoints.Add((Mathf.RoundToInt(edge)+360)%360))return;
            var collar=Child("Druckdichte Havarie-Ringkupplung / "+edge,parent);
            const float half=RingJointHalfAngle+.2f;
            Solid("Ring Wrackboden / Rest",collar,InnerRadius,OuterRadius,edge-half,edge+half,-.55f,0,floor);
            foreach(float radius in new[]{InnerRadius,OuterRadius})
                // Reach the normal coupling's inset backing as well as the nominal ring wall.
                Solid("Ringwand Havarie-Kupplung",collar,radius-.3f,radius+.3f,edge-half,edge+half,0,5.5f,hullB);
            var roof=Solid("Havarie-Kupplung Decke",collar,InnerRadius-.1f,OuterRadius+.1f,edge-half,edge+half,5.4f,5.75f,hullB);
            roofs.Add(roof.gameObject);
            foreach(float radius in new[]{InnerRadius+.15f,OuterRadius-.15f})
                Box("Havarie-Kupplung / geschlossener Schottrahmen",collar,At(radius,edge,2.75f),new Vector3(.36f,5.5f,.5f),frame,edge);
        }
        private void BuildBayAirlock(Transform parent)
        {
            if(DetailedHall==null)return;
            DetailedHall.ConfigureVacuumBoundary();
            var chamber=Child("Bay-Zugang / echte Zweituer-Schleuse",parent);
            chamber.localPosition=At(46.5f,30);chamber.localRotation=Quaternion.Euler(0,210,0);
            const float first=-5.1f,second=-13.5f,clearWidth=4.4f,clearHeight=4.1f,chamberHalfWidth=2.8f;
            float mid=(first+second)/2,length=first-second+.5f;
            foreach(float side in new[]{-1f,1f})Box("Schleusen Seitenwand",chamber,new Vector3(side*(chamberHalfWidth+.1f),2.75f,mid),new Vector3(.2f,5.5f,length),hullA);
            var ceiling=Box("Servicegang Decke",chamber,new Vector3(0,5.625f,mid),new Vector3(6,.25f,length),hullB);roofs.Add(ceiling.gameObject);
            Box("Schleusenkammer Boden",chamber,new Vector3(0,-.05f,mid),new Vector3(5.8f,.1f,length),floor);
            var status=Surface(new Color(.08f,.7f,.65f),.1f,true);
            var leaves=new Transform[4];int index=0;
            foreach(float z in new[]{first,second})
            {
                foreach(float side in new[]{-1f,1f})
                {
                    // Fixed pressure pockets fully cover both retracted full-size leaves.
                    // Their outer volume projects from the narrow connecting chamber.
                    Box("Schleuse Portalpfosten / geschlossene Tuerkassette",chamber,new Vector3(side*3.4f,clearHeight*.5f,z),new Vector3(2.4f,clearHeight,.5f),frame);
                    leaves[index++]=Box("Schleuse / druckdichte Schiebetuer",chamber,new Vector3(side*clearWidth*.25f,clearHeight*.5f,z),new Vector3(clearWidth*.5f+.035f,clearHeight+.08f,.2f),hullB);
                }
                Box("Schleuse Portalsturz",chamber,new Vector3(0,4.925f,z),new Vector3(9.2f,1.65f,.5f),frame);
                Box("Schleusenzyklus Status",chamber,new Vector3(0,4.35f,z-.3f),new Vector3(1.4f,.1f,.06f),status,0,false);
            }
            Box("Schleusenkammer Leuchte",chamber,new Vector3(0,5.38f,mid),new Vector3(2.2f,.08f,5),white,0,false);
            BayAirlock=chamber.gameObject.AddComponent<StationAirlock>();BayAirlock.ConfigureSize(first,second,clearWidth,clearHeight,chamberHalfWidth);BayAirlock.Configure(chamber,leaves[0],leaves[1],leaves[2],leaves[3],status);
            BayAirlock.SetLabels("Drohnenhalle","Außendock / Vakuum");
            Sign("E / AUSSENSCHLEUSE\nHALLE – AUSSENDOCK",parent,At(51.3f,30,4.85f),30,.035f);
            Sign("E / SCHLEUSE ZUR HALLE",parent,At(60.3f,30,4.85f),210,.035f);
        }
    }
}

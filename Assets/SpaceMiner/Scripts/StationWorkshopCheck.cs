#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SpaceMiner
{
    // Focused acceptance of the photographed station defects. Uses the real campaign
    // colliders, CharacterController, terminals, displays and shared utility network.
    public sealed class StationWorkshopCheck : MonoBehaviour
    {
        private const string Folder="Logs/StationWorkshop";
        private int checks;
        private StationInteriorMode mode;
        private StationInteriorLayout layout;
        private StationOperations operations;
        private WaterScenario scenario;
        private readonly List<string> pictures=new List<string>();
        [Serializable] private sealed class Result
        {
            public bool passed;
            public int checks,layout;
            public string[] pictures;
            public string scope="Closed seams, fall recovery, real terminal approaches, display geometry, shared workshop/dock utilities, physical refit transit, disk save/load and cancellation. Explicit later-game helper/research/water/power fixtures; no manual keyboard or frame-rate claim.";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stationWorkshopCheck")>=0)
                new GameObject("Workshop and station enclosure acceptance").AddComponent<StationWorkshopCheck>();
        }
        private void Require(bool condition,string label)
        {
            checks++;if(!condition)throw new Exception("WORKSHOP: "+label);
            Debug.Log("WORKSHOP OK: "+label);
        }
        private bool Guard(Action action)
        {
            try{action();return true;}
            catch(Exception error){File.WriteAllText(Folder+"/error.txt",error.ToString());Debug.LogException(error);Application.Quit(1);return false;}
        }
        private Vector3 World(Vector3 local)=>layout.transform.TransformPoint(local);
        private Vector3 LocalFeet=>layout.transform.InverseTransformPoint(mode.FeetPosition);
        // Authored node names contain literal slashes; Transform.Find treats them as paths.
        private static Transform DirectChild(Transform parent,string name)
        {
            if(parent==null)return null;
            foreach(Transform child in parent)if(child.name==name)return child;
            return null;
        }
        private static Transform Descendant(Transform parent,string name)
        {
            if(parent==null)return null;
            foreach(var child in parent.GetComponentsInChildren<Transform>(true))if(child.name==name)return child;
            return null;
        }
        private static Bounds MeshBoundsIn(MeshFilter filter,Transform frame)
        {
            var vertices=filter.sharedMesh.vertices;var first=frame.InverseTransformPoint(filter.transform.TransformPoint(vertices[0]));
            var bounds=new Bounds(first,Vector3.zero);
            foreach(var vertex in vertices)bounds.Encapsulate(frame.InverseTransformPoint(filter.transform.TransformPoint(vertex)));
            return bounds;
        }
        private static List<Bounds> ConnectedMeshBounds(MeshFilter filter,Transform frame)
        {
            // The imported material batch contains both cabinets. Rejoin coincident
            // vertices across normal/UV seams, then measure its two actual connected
            // bodies rather than mistaking a single box's opposite faces for two banks.
            var vertices=filter.sharedMesh.vertices;var points=new Vector3[vertices.Length];var parent=new int[vertices.Length];
            var canonical=new Dictionary<Vector3Int,int>();
            int Find(int index){while(parent[index]!=index){parent[index]=parent[parent[index]];index=parent[index];}return index;}
            void Join(int a,int b){a=Find(a);b=Find(b);if(a!=b)parent[b]=a;}
            for(int i=0;i<vertices.Length;i++)parent[i]=i;
            for(int i=0;i<vertices.Length;i++)
            {
                points[i]=frame.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                var key=new Vector3Int(Mathf.RoundToInt(points[i].x*10000),Mathf.RoundToInt(points[i].y*10000),Mathf.RoundToInt(points[i].z*10000));
                if(canonical.TryGetValue(key,out int previous))Join(i,previous);else canonical.Add(key,i);
            }
            var triangles=filter.sharedMesh.triangles;
            for(int i=0;i<triangles.Length;i+=3){Join(triangles[i],triangles[i+1]);Join(triangles[i+1],triangles[i+2]);}
            var groups=new Dictionary<int,Bounds>();
            for(int i=0;i<points.Length;i++)
            {
                int root=Find(i);if(!groups.TryGetValue(root,out var bounds))bounds=new Bounds(points[i],Vector3.zero);
                else bounds.Encapsulate(points[i]);groups[root]=bounds;
            }
            return new List<Bounds>(groups.Values);
        }
        private void CheckBenchShape(Transform wrapper,Transform authored,string side)
        {
            Bounds tabletop=default,drawers=default,handles=default;bool hasTop=false,hasDrawers=false,hasHandles=false;
            var cabinets=new List<Bounds>();
            foreach(var filter in authored.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null||filter.sharedMesh.vertexCount==0)continue;
                if(filter.name.StartsWith("Workbench__"))
                {var bounds=MeshBoundsIn(filter,wrapper);if(hasTop)tabletop.Encapsulate(bounds);else{tabletop=bounds;hasTop=true;}}
                if(filter.name.StartsWith("ToolCabinet__"))cabinets.AddRange(ConnectedMeshBounds(filter,wrapper));
                if(filter.name.StartsWith("ToolDrawer__"))
                {var bounds=MeshBoundsIn(filter,wrapper);if(hasDrawers)drawers.Encapsulate(bounds);else{drawers=bounds;hasDrawers=true;}}
                if(filter.name.StartsWith("DrawerHandle__"))
                {var bounds=MeshBoundsIn(filter,wrapper);if(hasHandles)handles.Encapsulate(bounds);else{handles=bounds;hasHandles=true;}}
            }
            Require(hasTop&&hasDrawers&&hasHandles,"bench retains physical tabletop, drawer fronts and handles / "+side);
            Require(Mathf.Abs(tabletop.size.x-3.1f)<.05f&&Mathf.Abs(tabletop.size.z-1)<.05f,
                "bench tabletop spans planned width on X and depth on Z / "+side+" / "+tabletop.size);
            Require(tabletop.max.y>.85f&&tabletop.max.y<.96f&&tabletop.center.y>.8f&&tabletop.center.y<.91f,
                "bench worktop stands at human working height rather than the top of its service rails / "+side+" / "+tabletop);
            Require(cabinets.Count==2,"authored bench contains two distinct connected cabinet bodies / "+side+" / count="+cabinets.Count);
            cabinets.Sort((a,b)=>a.center.x.CompareTo(b.center.x));var left=cabinets[0];var right=cabinets[1];
            Require(left.center.x<-.85f&&right.center.x>.85f&&right.center.x-left.center.x>1.8f&&Mathf.Abs(left.center.z-right.center.z)<.05f,
                "two support banks are separated across width rather than stacked behind each other / "+side+" / "+left.center+" / "+right.center);
            Require(left.max.x<-.4f&&right.min.x>.4f,"real opening between the two cabinet banks remains under tabletop / "+side);
            Require(Mathf.Abs(left.max.y-tabletop.min.y)<.06f&&Mathf.Abs(right.max.y-tabletop.min.y)<.06f,
                "both cabinet banks actually support the tabletop / "+side);
            Require(drawers.center.z<-.4f&&drawers.center.z>-.5f&&handles.center.z<drawers.center.z-.01f,
                "drawer fronts and projecting handles face the planned negative-Z front / "+side+" / drawerZ="+drawers.center.z+" / handleZ="+handles.center.z);
        }
        private void At(float radius,float angle,float heading)
        {
            mode.RestoreInterior(World(RingWalkthrough.At(radius,angle,.05f)),false,layout.transform.eulerAngles.y+heading,0);
            Physics.SyncTransforms();
        }
        private void Walk(float seconds)
        {
            for(float elapsed=0;elapsed<seconds;elapsed+=.02f)mode.Walk(Vector2.up,.02f);
        }
        private string PassageObstruction(StationPressureGate gate,Vector3 lower,Vector3 upper,float radius,RaycastHit first)
        {
            var report=new System.Text.StringBuilder();
            report.AppendLine("Gate: "+gate.Label+"; radius="+radius+"; lower="+lower+"; upper="+upper);
            report.AppendLine("First: "+first.collider?.name+"; distance="+first.distance+"; point="+first.point+"; normal="+first.normal);
            var affected=new List<Collider>(Physics.OverlapCapsule(lower,upper,radius,1<<25,QueryTriggerInteraction.Ignore));
            foreach(var hit in Physics.CapsuleCastAll(lower,upper,radius,gate.transform.forward,2.2f,1<<25,QueryTriggerInteraction.Ignore))
            {report.AppendLine("Sweep: "+hit.collider.name+"; distance="+hit.distance+"; point="+hit.point+"; normal="+hit.normal);if(!affected.Contains(hit.collider))affected.Add(hit.collider);}
            foreach(var collider in affected)
            {
                string path=collider.name;for(var parent=collider.transform.parent;parent!=null;parent=parent.parent)path=parent.name+"/"+path;
                report.AppendLine("Collider: "+path+"; centre="+collider.bounds.center+"; size="+collider.bounds.size+"; local="+collider.transform.localPosition+"; scale="+collider.transform.localScale);
            }
            File.WriteAllText(Folder+"/blocked-wide-passage.txt",report.ToString());Debug.LogError(report.ToString());
            Render("00-blocked-wide-passage",layout.transform.InverseTransformPoint(gate.transform.TransformPoint(new Vector3(0,2.4f,-5.5f))),
                layout.transform.InverseTransformPoint(gate.transform.TransformPoint(new Vector3(0,1.8f,0))),true,88);
            return first.collider?.name+"; distance="+first.distance+"; point="+first.point+"; normal="+first.normal;
        }
        private void CheckWideBulkheads()
        {
            int wide=0,recoveries=mode.FallRecoveries;
            foreach(var gate in layout.Geometry.PressureGates)
            {
                if(!gate.Label.StartsWith("Ringschott S")||gate.Locked)continue;
                wide++;
                Require(gate.ClearWidth>=6.99f&&gate.LeafCount==8,"healthy ring bulkhead has seven metres of real clear width and eight telescoping leaves / "+gate.Label);
                Require(gate.Request(true,gate.transform.position+gate.transform.forward*3),"healthy bulkhead opens / "+gate.Label);
                gate.Advance(2,null);Physics.SyncTransforms();
                foreach(float x in new[]{-3.15f,-1.55f,0,1.55f,3.15f})
                    Require(!Physics.Raycast(gate.transform.TransformPoint(new Vector3(x,1.6f,-1.1f)),gate.transform.forward,2.2f,1<<25,QueryTriggerInteraction.Ignore),
                        "full-width passage ray is clear / "+gate.Label+" / x="+x);
                foreach(float x in new[]{-3.1f,3.1f})
                {
                    Vector3 lower=gate.transform.TransformPoint(new Vector3(x,.37f,-1.1f));
                    Vector3 upper=gate.transform.TransformPoint(new Vector3(x,1.54f,-1.1f));
                    bool blocked=Physics.CapsuleCast(lower,upper,.3f,gate.transform.forward,out var obstruction,2.2f,1<<25,QueryTriggerInteraction.Ignore);
                    string obstacle=blocked?PassageObstruction(gate,lower,upper,.3f,obstruction):"none";
                    Require(!blocked,"human capsule clears outer walking lane through actual joint / "+gate.Label+" / "+x+" / "+obstacle);
                    Vector3 feet=gate.transform.TransformPoint(new Vector3(x,.04f,-1.2f));
                    Require(layout.ContainsPosition(feet)&&layout.HasStandingClearance(feet),"wide lane has real supported standing room / "+gate.Label+" / "+x);
                    mode.RestoreInterior(feet,false,gate.transform.eulerAngles.y,0);Walk(.45f);
                    Require(gate.transform.InverseTransformPoint(mode.FeetPosition).z>.7f,
                        "CharacterController traverses widened outer lane without shoulder collision / "+gate.Label+" / "+x);
                }
                foreach(float x in new[]{-1.65f,1.65f})
                {
                    Vector3 lower=gate.transform.TransformPoint(new Vector3(x,1.65f,-1.1f));
                    Vector3 upper=gate.transform.TransformPoint(new Vector3(x,3.2f,-1.1f));
                    bool blocked=Physics.CapsuleCast(lower,upper,1.5f,gate.transform.forward,out var obstruction,2.2f,1<<25,QueryTriggerInteraction.Ignore);
                    string obstacle=blocked?PassageObstruction(gate,lower,upper,1.5f,obstruction):"none";
                    Require(!blocked,"two independent three-metre drone capsule lanes clear joint / "+gate.Label+" / "+x+" / "+obstacle);
                }
                foreach(var leaf in gate.GetComponentsInChildren<Collider>())if(leaf.name=="Druckschott / Schiebefluegel")
                {
                    float left=gate.transform.InverseTransformPoint(leaf.bounds.min).x,right=gate.transform.InverseTransformPoint(leaf.bounds.max).x;
                    // Bounds corners above are world-axis aligned, so use the authored
                    // local panel extents for the actual ring-frame containment check.
                    float outer=Mathf.Abs(leaf.transform.localPosition.x)+leaf.transform.localScale.x*.5f;
                    Require(outer<4.475f,"retracted panel stack stays inside original 8.95-m ring frame / "+gate.Label+" / "+left+".."+right);
                }
            }
            Require(wide>=8,"multiple healthy ring-sector bulkheads use the widened mechanism");
            Require(mode.FallRecoveries==recoveries,"all widened-lane controller walks stay on supported ring floor");
            foreach(var gate in layout.Geometry.PressureGates)if(gate.Locked)
            {
                Require(!gate.Request(true,gate.transform.position+gate.transform.forward*3)&&gate.Opening==0,
                    "damaged bulkhead retains its locked closed state / "+gate.Label);
                Require(Physics.Raycast(gate.transform.TransformPoint(new Vector3(0,1.6f,-.9f)),gate.transform.forward,1.8f,1<<25,QueryTriggerInteraction.Ignore),
                    "closed damaged bulkhead retains real collision across its centre seam / "+gate.Label);
            }
        }
        internal static int CheckRoadOnly(StationInteriorLayout layout,StationInteriorMode mode)
        {
            var root=new GameObject("Focused ring-road acceptance");root.SetActive(false);
            try
            {
                var check=root.AddComponent<StationWorkshopCheck>();check.enabled=false;check.layout=layout;check.mode=mode;
                check.CheckWideBulkheads();check.CheckRingMarkings();return check.checks;
            }
            finally { Destroy(root); }
        }
        private void CheckRingMarkings()
        {
            const string prefix="Bodenmarkierung / durchgehend / ";
            var strips=new MeshFilter[RingWalkthrough.SegmentCount,7];
            var points=new Vector3[RingWalkthrough.SegmentCount,7][];
            int count=0;bool transverse=false;
            foreach(var filter in layout.Geometry.Root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.name=="Fugenlinie")transverse=true;
                if(!filter.name.StartsWith(prefix))continue;
                Require(int.TryParse(filter.name.Substring(prefix.Length),out int number)&&number>=1&&number<=7,
                    "continuous floor guidance retains a distinct lane boundary");
                int line=number-1;var vertices=filter.sharedMesh.vertices;var local=new Vector3[vertices.Length];
                for(int i=0;i<vertices.Length;i++)local[i]=layout.Geometry.Root.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                var middle=(local[local.Length/2]+local[local.Length/2+1])*.5f;
                int sector=((Mathf.RoundToInt(Mathf.Atan2(middle.x,middle.z)*Mathf.Rad2Deg/30)%12)+12)%12;
                Require(!RingWalkthrough.DamagedRingSector(sector)&&strips[sector,line]==null,
                    "exactly one painted boundary occupies each healthy ring sector / "+sector+" / "+line);
                strips[sector,line]=filter;points[sector,line]=local;count++;
                float radius=new Vector2(local[0].x,local[0].z).magnitude;
                bool smooth=local.Length>=6&&local.Length%2==0&&filter.GetComponent<Collider>()==null;
                for(int i=0;i<local.Length;i+=2)
                {
                    float r=new Vector2(local[i].x,local[i].z).magnitude,r2=new Vector2(local[i+1].x,local[i+1].z).magnitude;
                    smooth&=Mathf.Abs(r-radius)<.003f&&Mathf.Abs(r2-r-.05f)<.003f
                        &&Mathf.Abs(local[i].y-.012f)<.003f&&Mathf.Abs(local[i+1].y-.012f)<.003f;
                    if(i==0)continue;
                    float before=Mathf.Atan2(local[i-2].x,local[i-2].z)*Mathf.Rad2Deg,angle=Mathf.Atan2(local[i].x,local[i].z)*Mathf.Rad2Deg;
                    float advance=Mathf.DeltaAngle(before,angle);smooth&=advance>0&&advance<.27f;
                }
                Require(smooth&&ConnectedMeshBounds(filter,layout.Geometry.Root).Count==1,
                    "actual paint is one flat constant-radius arc without zigzags, gaps or reversed segments / "+sector+" / "+line);
            }
            Require(count==70&&!transverse,"ten healthy sectors retain seven lane boundaries without cyan transverse seam bars");
            for(int sector=0;sector<12;sector++)
            {
                if(RingWalkthrough.DamagedRingSector(sector))continue;
                for(int line=0;line<7;line++)Require(strips[sector,line]!=null,"healthy sector has every continuous lane boundary / "+sector+" / "+line);
                float Radius(int line)=>new Vector2(points[sector,line][0].x,points[sector,line][0].z).magnitude;
                Require(Radius(1)-Radius(0)>.59f&&Radius(2)-Radius(1)>.19f&&Radius(3)-Radius(2)>1.49f&&Radius(4)-Radius(3)>1.49f&&Radius(5)-Radius(4)>1.49f&&Radius(6)-Radius(5)>1.49f,
                    "paint preserves pedestrian strip, buffer and four 1.5m drone lanes / "+sector);
                int next=(sector+1)%12;if(RingWalkthrough.DamagedRingSector(next))continue;
                for(int line=0;line<7;line++)
                {
                    var current=points[sector,line];var following=points[next,line];
                    Vector3 outgoing=current[current.Length-2]-current[current.Length-4],incoming=following[2]-following[0];
                    Require(Vector3.Distance(current[current.Length-2],following[0])<.003f
                        &&Vector3.Distance(current[current.Length-1],following[1])<.003f&&Vector3.Angle(outgoing,incoming)<.55f,
                        "both painted edges join across actual sector boundary with a smooth tangent / "+sector+" / "+line);
                }
            }
            foreach(var gate in layout.Geometry.PressureGates)
            {
                if(gate.Locked||!gate.Label.StartsWith("Ringschott S"))continue;
                bool clear=true;var seen=new bool[7];
                for(int sector=0;sector<12;sector++)for(int line=0;line<7;line++)
                {
                    if(points[sector,line]==null)continue;
                    foreach(var point in points[sector,line])
                    {
                        var local=gate.transform.InverseTransformPoint(layout.Geometry.Root.TransformPoint(point));
                        if(Mathf.Abs(local.z)>.45f||Mathf.Abs(local.x)>5)continue;
                        seen[line]=true;clear&=Mathf.Abs(local.x)<gate.ClearWidth*.5f-.03f;
                    }
                }
                Require(clear&&Array.TrueForAll(seen,value=>value),"all seven real painted strips remain inside open bulkhead cassettes / "+gate.Label);
            }
        }
        private void CheckClosedJoints()
        {
            int recoveries=mode.FallRecoveries;
            foreach(float boundary in new[]{75f,135f})
            {
                float healthySide=boundary==75?-1:1,angle=boundary+healthySide*1.4f;
                foreach(bool inner in new[]{true,false})
                {
                    float radius=inner?30.1f:36.9f;
                    Vector3 from=World(RingWalkthrough.At(radius,angle,1.6f));
                    Vector3 radial=layout.transform.TransformDirection(RingWalkthrough.At(inner?-1:1,angle));
                    Require(Physics.Raycast(from,radial,out var wallHit,2.5f,1<<25,QueryTriggerInteraction.Ignore),
                        "damage joint "+boundary+" has actual "+(inner?"inner":"outer")+" sidewall collision before the gate");
                    Require(wallHit.distance<2,"joint sidewall meets the existing wall backing / "+boundary+" / "+inner);
                    At(radius,angle,angle+(inner?180:0));Walk(.8f);
                    float r=new Vector2(LocalFeet.x,LocalFeet.z).magnitude;
                    Require(inner?r>29.4f:r<37.6f,"CharacterController cannot leave radially around bulkhead / "+boundary+" / "+inner);
                    mode.Look(new Vector2(Mathf.DeltaAngle(mode.ViewYaw,layout.transform.eulerAngles.y+boundary-healthySide*90),0));
                    Walk(1.2f);
                    float reachedAngle=Mathf.Atan2(LocalFeet.x,LocalFeet.z)*Mathf.Rad2Deg;
                    Require(healthySide*Mathf.DeltaAngle(boundary,reachedAngle)>.1f&&LocalFeet.y>-.15f,
                        "walking along the sealed shoulder cannot bypass locked damage gate / "+boundary+" / "+inner);
                }
                Require(Physics.Raycast(World(RingWalkthrough.At(33.5f,angle,4.8f)),Vector3.up,1.1f,1<<25,QueryTriggerInteraction.Ignore),
                    "joint ceiling physically closes the visible gap / "+boundary);
                At(33.5f,angle,boundary-healthySide*90);Walk(1);
                float reached=Mathf.Atan2(LocalFeet.x,LocalFeet.z)*Mathf.Rad2Deg;
                Require(healthySide*Mathf.DeltaAngle(boundary,reached)>.1f,"closed gate blocks the centre route / "+boundary);
            }
            Require(mode.FallRecoveries==recoveries,"all blocked-joint walks stay on floor without relying on fall recovery");
        }
        private void CheckFallRecovery()
        {
            At(33.5f,330,60);mode.Walk(Vector2.zero,.02f);
            var controller=typeof(StationInteriorMode).GetField("walker",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(mode) as CharacterController;
            Require(controller!=null,"real walker available for an explicit below-deck fault injection");
            Vector3 safe=mode.FeetPosition;int before=mode.FallRecoveries;
            mode.Vitals.Stamina=47;mode.Vitals.Health=83;mode.Vitals.Hunger=12;mode.Vitals.Thirst=19;mode.Vitals.Fatigue=8;
            float water=scenario.WaterLiters,battery=operations.State.power.storedKwh,yaw=mode.ViewYaw;
            controller.enabled=false;controller.transform.position=safe-layout.transform.up*4;controller.enabled=true;Physics.SyncTransforms();
            Require(mode.RecoverFallenWalker()&&mode.FallRecoveries==before+1,"below-deck fall returns once to supported ground");
            Require(Vector3.Distance(mode.FeetPosition,safe)<.015f,"fall recovery returns to last local floor rather than the command spawn");
            Require(mode.Vitals.Stamina==47&&mode.Vitals.Health==83&&mode.Vitals.Hunger==12&&mode.Vitals.Thirst==19&&mode.Vitals.Fatigue==8,
                "fall recovery preserves all five player values");
            Require(scenario.WaterLiters==water&&operations.State.power.storedKwh==battery&&mode.ViewYaw==yaw,
                "fall recovery preserves resources and viewing direction");
            Require(!mode.RecoverFallenWalker()&&mode.FallRecoveries==before+1,"supported walker is not repeatedly recovered");
            mode.Exit(true);controller.transform.position=safe-Vector3.up*4;
            Require(!mode.RecoverFallenWalker()&&mode.FallRecoveries==before+1,"outside camera mode never invokes walking recovery");
            mode.RestoreInterior(safe,false,yaw,0);mode.Vitals.Reset();
        }
        private void CheckDisplayGeometry()
        {
            var boards=layout.GetComponentsInChildren<StationDisplay>(true);
            Require(boards.Length>=30,"station labels are physical display objects");
            foreach(var board in boards)
            {
                Require(board.Front!=null&&board.Back!=null&&board.Front.text==board.Caption&&board.Back.text==board.Caption,
                    "both display faces retain the same caption / "+board.Caption);
                Require(board.PanelSize.x>=.39f&&board.PanelSize.x<=3.35f&&board.PanelSize.y>=.21f&&board.PanelSize.y<=.65f,
                    "display panel has bounded readable dimensions / "+board.Caption);
                Require(Vector3.Dot(board.Front.transform.forward,board.Back.transform.forward)<-.999f
                    &&board.Front.transform.localScale.x>0&&board.Back.transform.localScale.x>0,
                    "reverse caption is separately rotated and never mirrored / "+board.Caption);
                Require(board.Front.transform.localPosition.z<-.04f&&board.Back.transform.localPosition.z>.04f,
                    "letters lie on opposite sides of the opaque display / "+board.Caption);
                Vector3 letters=board.Front.GetComponent<Renderer>().localBounds.size;
                Require(letters.x<=board.PanelSize.x+.03f&&letters.y<=board.PanelSize.y+.03f,
                    "caption fits the display instead of crossing a doorway / "+board.Caption);
                var housing=DirectChild(board.transform,"Display / Gehäuse");
                Require(housing!=null&&housing.GetComponent<Renderer>().sharedMaterial.renderQueue<=2500,
                    "opaque housing prevents seeing backwards lettering through the panel / "+board.Caption);
            }
        }
        private void CheckCommandApproaches()
        {
            var all=layout.GetComponentsInChildren<StationTerminal>();int command=0;
            foreach(var terminal in all)
            {
                // This fixture tests the four command-room desks at 1.55 m.
                // The separate plant acceptance walks its actual 1 m service aisle.
                if(terminal.kind==StationTerminalKind.Utilities||terminal==operations.IcePlantTerminal)continue;
                command++;Vector3 approach=terminal.transform.TransformPoint(new Vector3(0,.04f,-1.55f));
                Require(layout.ContainsPosition(approach)&&layout.HasStandingClearance(approach),"wall terminal has actual standing space / "+terminal.label);
                mode.RestoreInterior(approach,false,terminal.transform.eulerAngles.y,0);Physics.SyncTransforms();
                Require(Vector3.Distance(mode.FeetPosition,approach)<.01f,"terminal approach accepted without a spawn fallback / "+terminal.label);
                Require(operations.NearestTerminal(mode.FeetPosition)==terminal,"real visibility and proximity select intended terminal / "+terminal.label);
                Require(operations.InteractionPrompt(mode.FeetPosition).StartsWith("E")&&operations.InteractionPrompt(mode.FeetPosition).Contains(terminal.label),
                    "nearby terminal gives its own E interaction prompt / "+terminal.label);
                operations.OpenTerminal(terminal);Require(operations.PanelOpen,"terminal opens its campaign panel / "+terminal.label);operations.ClosePanel();
            }
            Require(command==4,"four command wall terminals are individually usable");
        }
        private void CheckWorkshopGeometry()
        {
            Require(operations.WorkshopRoot!=null&&operations.WorkshopCellCount==2&&operations.WorkshopArmCount==4,
                "two complete repair cells have left and right articulated floor robots");
            Require(operations.FunctionalWorkshopCells==1,"exactly one workshop cell starts supplied and functional");
            int changers=0,stocks=0,supplies=0;bool scrap=false;
            CommandRoomLayout workshopPlan=null;foreach(var room in operations.RoomPlans)if(room.RoomId=="drone-workshop")workshopPlan=room;
            Require(workshopPlan!=null,"actual workshop object catalogue exists");
            var analysis=workshopPlan.BoundRoot("scrap-analysis");var bin=workshopPlan.BoundRoot("scrap-bin");
            Require(analysis!=null&&bin!=null,"analysis and scrap bin have separate live planning wrappers");
            foreach(var t in operations.GetComponentsInChildren<Transform>(true))
            {
                if(t.name.StartsWith("Automatischer Werkzeugwechsel"))
                {
                    changers++;Require(DirectChild(t,"Greifwerkzeug")!=null&&DirectChild(t,"Schweißwerkzeug")!=null&&DirectChild(t,"Reparatursonde")!=null,
                        "each robot has three separate interchangeable tool mounts / "+changers);
                }
                if(t.name=="Bodenversorgung / Strom Wasser Daten")
                {
                    supplies++;Require(DirectChild(t,"Wassersteigleitung")!=null&&DirectChild(t,"Geschützter Stromkanal")!=null&&DirectChild(t,"Bodendurchführung")!=null,
                        "repair cell receives separate floor water and electrical risers / "+supplies);
                }
                if(t.name.StartsWith("Bohrer / Bestand ")||t.name.StartsWith("Greifer / Bestand ")||t.name.StartsWith("Laser / Bestand "))
                {stocks++;Require(t.gameObject.activeInHierarchy,"initial physical stock mount is filled / "+t.name);}
                if(t==bin)
                {
                    scrap=true;var cabinet=Descendant(analysis,"Analyseschrank");var originalBin=Descendant(bin,"Unidentifizierter Schrott / geschlossener Behälter");
                    Require(cabinet!=null&&originalBin!=null&&cabinet.gameObject.activeInHierarchy&&originalBin.gameObject.activeInHierarchy,
                        "planning wrappers retain the actual authored analysis cabinet and scrap container");
                    var cabinetCollider=cabinet.GetComponent<Collider>();var binBody=Descendant(originalBin,"Geschlossener Annahmebehälter");
                    var binCollider=binBody!=null?binBody.GetComponent<Collider>():null;
                    Require(cabinetCollider!=null&&binCollider!=null&&cabinetCollider.enabled&&binCollider.enabled,
                        "actual analysis cabinet and scrap container retain solid collision");
                    Require(Vector3.Distance(cabinetCollider.bounds.center,binCollider.bounds.center)<5,
                        "separately planned unidentified scrap store remains beside the real wall analysis machine");
                }
            }
            Require(changers==4&&supplies==2&&stocks==9&&scrap,"four tool changers, two floor supplies and three of each stock tool exist");
            foreach(string side in new[]{"left","right"})
            {
                var bench=workshopPlan.BoundRoot("bench-"+side);
                var authored=Descendant(bench,side=="left"?"Originale Wandwerkbank links":"Originale Wandwerkbank rechts");
                var marker=bench!=null?bench.GetComponent<CommandRoomBlock>():null;
                Require(marker?.id=="bench-"+side&&marker.roomId=="drone-workshop"&&authored!=null&&authored.gameObject.activeInHierarchy,
                    "stable planning wrapper retains its real split original wall workbench / "+side);
                foreach(string part in new[]{"Workbench__","ToolCabinet__","ToolDrawer__","DrawerHandle__","BenchTools__","UtilityStands__"})
                {
                    bool found=false;foreach(var filter in authored.GetComponentsInChildren<MeshFilter>())
                        if(filter.name.StartsWith(part)&&filter.sharedMesh!=null&&filter.sharedMesh.vertexCount>0&&filter.GetComponent<Renderer>()!=null)found=true;
                    Require(found,"original wall furniture still contains its authored visible mesh / "+side+" / "+part);
                }
                CheckBenchShape(bench,authored,side);
            }
            operations.TogglePower(1);Require(operations.FunctionalWorkshopCells==0,"isolating hall power disables its only functional cell");operations.TogglePower(1);
            Require(operations.FunctionalWorkshopCells==1,"reconnecting hall power restores the same first cell");
        }
        private void CheckDockInfrastructure()
        {
            var infrastructure=layout.GetComponentInChildren<StationDockInfrastructure>();
            Require(infrastructure!=null&&infrastructure.FeedCount==scenario.Drones.Length&&infrastructure.FeedCount==10,
                "all ten exterior berths own physical water and charging supplies");
            Require(infrastructure.RuptureCount==5&&infrastructure.GetComponentsInChildren<LineRenderer>(true).Length==5,
                "five attached external rupture sites have visible arc geometry");
            var feeds=DirectChild(infrastructure.transform,"Zehn getrennte Lade- und Wasseranschluesse");
            Require(feeds!=null,"exterior utility manifold exists");
            infrastructure.Refresh();
            var fleet=scenario.GetComponent<CampaignDroneFleet>();float? firstFloor=null;
            for(int i=0;i<scenario.Drones.Length;i++)
            {
                var feed=infrastructure.FeedRoot(i);
                Require(feed!=null&&Vector3.Distance(feed.position,scenario.Drones[i].HomePosition)<.01f,
                    "each supply follows its actual cradle / "+(i+1));
                Require(feed.IsChildOf(fleet.BerthRoot(i))&&fleet.CradleRoot(i).IsChildOf(fleet.BerthRoot(i)),
                    "physical supply and actual imported cradle share the same movable berth anchor / "+(i+1));
                Vector3 home=Quaternion.Euler(0,-30,0)*layout.transform.InverseTransformPoint(scenario.Drones[i].HomePosition);
                Require(Mathf.Abs(home.y-1)<.002f&&Mathf.Abs(home.x-(i%5-2)*6.6f)<.002f&&Mathf.Abs(home.z-(i<5?79:67.5f))<.002f,
                    "all ten homes stand at the same deck level in the two widened rows / "+(i+1));
                Require(Physics.Raycast(scenario.Drones[i].HomePosition+layout.transform.up*10,-layout.transform.up,out var cradleFloor,20,1<<30,QueryTriggerInteraction.Ignore),
                    "real imported cradle supports each same-level berth / "+(i+1));
                float floor=Vector3.Dot(cradleFloor.point-layout.transform.position,layout.transform.up);
                if(!firstFloor.HasValue)firstFloor=floor;
                Require(Mathf.Abs(floor-firstFloor.Value)<.005f,"all physical cradle platforms share the same floor level / "+(i+1));
                Require(DirectChild(feed,"Wasserabsperrblock")!=null&&DirectChild(feed,"Elektrischer Lasttrenner")!=null,
                    "water and power can be isolated separately at each berth / "+(i+1));
                Require(infrastructure.FeedConnected(i)==(i==0),"only operational drone01 is connected at launch / "+(i+1));
                if(i>0)Require(!infrastructure.FeedPowered(i)&&!infrastructure.FeedWatered(i),"inactive drone receives no fictitious supply / "+(i+1));
            }
            Quaternion bayHeading=layout.transform.rotation*Quaternion.Euler(0,30,0);
            foreach(float x in new[]{-9.9f,-3.3f,3.3f,9.9f})
                Require(!Physics.CheckBox(World(Quaternion.Euler(0,30,0)*new Vector3(x,2.2f,73)),new Vector3(1.55f,2,9),bayHeading,(1<<25)|(1<<30),QueryTriggerInteraction.Ignore),
                    "full-size drone longitudinal aisle remains physically clear / "+x);
            Require(!Physics.CheckBox(World(Quaternion.Euler(0,30,0)*new Vector3(0,2.2f,73.25f)),new Vector3(15,2,1.75f),bayHeading,(1<<25)|(1<<30),QueryTriggerInteraction.Ignore),
                "cross-aisle between same-level dock rows remains physically clear");
            Require(infrastructure.FeedPowered(0)&&infrastructure.FeedWatered(0),"active first berth draws from live station supply");
            float water=scenario.WaterLiters;
            operations.TogglePower(1);infrastructure.Refresh();Require(!infrastructure.FeedPowered(0),"hall breaker disconnects berth charging");operations.TogglePower(1);
            operations.ToggleWater(1);infrastructure.Refresh();Require(!infrastructure.FeedWatered(0),"hall valve disconnects berth water");operations.ToggleWater(1);infrastructure.Refresh();
            Require(infrastructure.FeedPowered(0)&&infrastructure.FeedWatered(0)&&scenario.WaterLiters==water,
                "reconnection restores supply without creating or consuming stored water");
            infrastructure.RefreshDamage(.05f);
            Require(infrastructure.ArcVisible(0)&&infrastructure.DamageVisible(0),"damaged live exterior cable has an actual visible short arc");
            operations.TogglePower(3);infrastructure.RefreshDamage(.05f);
            Require(!infrastructure.ArcVisible(0)&&infrastructure.DamageVisible(0),"local exterior circuit isolation suppresses sparks and preserves the physical damage");
            operations.TogglePower(3);infrastructure.RefreshDamage(.05f);
            Require(infrastructure.ArcVisible(0),"reconnecting the damaged branch restores its arc");
            // Presentation-only repaired-condition fixture. Do not run campaign repair or
            // RefreshPresentation while this temporary value is installed.
            var damaged=operations.Element("sector-3");float original=damaged.condition;damaged.condition=1;
            infrastructure.RefreshDamage(.05f);
            Require(!infrastructure.ArcVisible(0)&&!infrastructure.DamageVisible(0),"repaired exterior site has neither torn cover nor discharge");
            damaged.condition=original;infrastructure.RefreshDamage(.05f);
        }
        private StationWorkOrder UntilRefit(string id,Func<StationWorkOrder,bool> reached,string label,int limit=1500)
        {
            StationWorkOrder order=null;
            for(int elapsed=0;elapsed<limit;elapsed++)
            {
                order=operations.State.orders.Find(o=>o.id==id);
                if(order!=null&&reached(order))return order;
                Require(!(layout.Airlock.HabitatOpen>0&&layout.Airlock.RingOpen>0),"real refit airlock leaves remain interlocked / "+label+" / "+elapsed);
                scenario.Advance(1);
            }
            throw new Exception("WORKSHOP: timed out "+label+" / "+order?.status+" / "+JsonUtility.ToJson(order?.workshop));
        }
        private void WaitForReady()
        {
            for(int elapsed=0;elapsed<1500&&!scenario.Worker.IsReady;elapsed++)scenario.Advance(1);
            Require(scenario.Worker.IsReady&&!scenario.Worker.HasTankOrder,"same physical drone is free and actually supplied before the next workshop trip");
        }
        private void CheckPhysicalRefit()
        {
            var baseline=SaveGameStore.Capture(scenario,"Vor Werkstatt-Abnahme","Explizite Testressourcen folgen");
            // Explicit later-game fixture: this acceptance isolates workshop transport,
            // interlock and persistence from completing the preceding economy quests.
            operations.State.helper=true;operations.State.diagnosed=true;
            if(!operations.State.technologies.Contains("repair"))operations.State.technologies.Add("repair");
            scenario.DepositWater(60);operations.State.power.storedKwh=20;operations.RefreshSupplyState();
            mode.Exit(true);WaitForReady();
            int drills=operations.State.spareDrills,grippers=operations.State.spareGrippers,lasers=operations.State.spareLasers;
            string tool=operations.State.tools[0];
            Require(operations.Enqueue(StationJobKind.Refit,"0:Greifer"),"queue actual hall refit using the unlocked-helper fixture");
            var pending=operations.State.orders[operations.State.orders.Count-1];string id=pending.id;
            var entering=UntilRefit(id,o=>o.workshop!=null&&o.workshop.stage==1&&o.workshop.doorStage==1,"incoming cycle reached");
            Require(entering.started&&scenario.Worker.Phase==DronePhase.StationWork&&operations.IsRefitting(0),"travel reserves that same active exterior drone");
            Require(entering.workshop.airlock!=null&&entering.workshop.airlock.phase!=AirlockPhase.Idle,"incoming cycle has persistent occupied-airlock state");
            Vector3 before=scenario.Worker.transform.position;Quaternion rotation=scenario.Worker.transform.rotation;
            var chamberBefore=layout.Airlock.CaptureState();
            var captured=SaveGameStore.Capture(scenario,"Drohne im Schleusentransit","Außenkamera / aktive Werkstattfahrt");
            Require(!captured.insideStation,"transit save uses the outside camera case");
            string path=SaveGameStore.Write(captured,false,Path.GetFullPath(Folder+"/Saves"));
            SaveGameStore.Apply(SaveGameStore.Read(path),scenario);scenario.SetSimulationRate(0);scenario.Scanner.DismissDialogue();Physics.SyncTransforms();
            var restored=operations.State.orders.Find(o=>o.id==id);
            Require(WorkshopRefitState.HasData(restored.workshop)&&restored.workshop.stage==1&&restored.workshop.doorStage==1,"disk save restores the active incoming workshop stage");
            Require(Vector3.Distance(scenario.Worker.transform.position,before)<.01f&&Quaternion.Angle(scenario.Worker.transform.rotation,rotation)<.1f,
                "outside-camera load retains the real in-transit hull position and orientation");
            Require(layout.Airlock.Phase==chamberBefore.phase&&Mathf.Abs(layout.Airlock.HabitatOpen-chamberBefore.habitatOpen)<.001f
                &&Mathf.Abs(layout.Airlock.RingOpen-chamberBefore.ringOpen)<.001f,
                "optional workshop airlock snapshot survives an outside-camera disk save");
            var working=UntilRefit(id,o=>o.workshop!=null&&o.workshop.stage==3&&o.elapsed>=3,"same hull reaches working cell");
            Vector3 target=operations.WorkshopRoot.TransformPoint(new Vector3(-4.3f,working.workshop.pivotHeight,8.1f));
            Require(Vector3.Distance(scenario.Worker.transform.position,target)<.02f&&Vector3.Distance(scenario.Worker.transform.position,scenario.Worker.HomePosition)>15,
                "the imported exterior drone physically occupies repair cell01 rather than remaining in its cradle");
            Require(scenario.Worker.Navigation.PoseClear(scenario.Worker.transform.position,scenario.Worker.transform.rotation,out string obstruction),
                "full imported hull has real collision clearance in its working cell / "+obstruction);
            var cell=Descendant(operations.WorkshopRoot,"Umbauplatz 01 / betriebsbereit");
            var leftLift=DirectChild(DirectChild(cell,"Roboter links / Bodenaufnahme"),"Ausfahrbarer Roboterhub");
            var rightLift=DirectChild(DirectChild(cell,"Roboter rechts / Bodenaufnahme"),"Ausfahrbarer Roboterhub");
            Require(leftLift!=null&&rightLift!=null&&leftLift.localPosition.y>.1f&&rightLift.localPosition.y>.1f,
                "both floor robots extend during actual tool service");
            Render("08-aktive-drohne-im-umbauplatz",RingWalkthrough.At(41,30,2.7f),RingWalkthrough.At(46.1f,24.5f,1.8f),true,73);
            operations.Cancel(id);
            var cancelled=UntilRefit(id,o=>o.complete,"cancelled service returns to its own dock");
            Require(cancelled.cancelled&&!operations.IsRefitting(0)&&Vector3.Distance(scenario.Worker.transform.position,scenario.Worker.HomePosition)<.01f,
                "cancelled service returns the same hull and releases its reservation");
            Require(operations.State.tools[0]==tool&&operations.State.spareDrills==drills&&operations.State.spareGrippers==grippers&&operations.State.spareLasers==lasers,
                "service cancellation preserves mounted tool and all three stock counts");
            WaitForReady();Require(operations.Enqueue(StationJobKind.Refit,"0:Greifer"),"queue complete physical refit after cancelled return");
            string completeId=operations.State.orders[operations.State.orders.Count-1].id;
            UntilRefit(completeId,o=>o.workshop!=null&&o.workshop.stage==3,"second physical refit reaches cell01");
            var complete=UntilRefit(completeId,o=>o.complete,"completed refit physically returns");
            Require(!complete.cancelled&&operations.State.tools[0]=="Greifer"&&operations.State.spareGrippers==grippers-1&&operations.State.spareDrills==drills+1
                &&operations.State.spareLasers==lasers,"completed refit exchanges exactly one stock tool and returns the previous drill");
            Require(Vector3.Distance(scenario.Worker.transform.position,scenario.Worker.HomePosition)<.01f&&!operations.IsRefitting(0),"successful refit releases the real vehicle back at its own cradle");
            WaitForReady();Require(operations.Enqueue(StationJobKind.Refit,"0:Laser"),"queue separate incoming-cycle cancellation");
            string cycleId=operations.State.orders[operations.State.orders.Count-1].id;
            UntilRefit(cycleId,o=>o.workshop!=null&&o.workshop.stage==1&&o.workshop.doorStage==1,"third incoming interlock reached");
            operations.Cancel(cycleId);
            var cycleCancelled=UntilRefit(cycleId,o=>o.complete,"cancellation while entering safely finishes the occupied cycle and returns");
            Require(cycleCancelled.cancelled&&layout.Airlock.Phase==AirlockPhase.Idle&&operations.State.tools[0]=="Greifer"
                &&operations.State.spareLasers==lasers&&operations.State.spareGrippers==grippers-1,
                "mid-cycle cancellation preserves tools and leaves the real chamber safely idle");
            SaveGameStore.Apply(baseline,scenario);scenario.SetSimulationRate(0);scenario.Scanner.DismissDialogue();
            Require(operations.State.tools[0]==tool&&!operations.State.helper&&operations.State.orders.Count==0,
                "test-only unlock and resource fixtures are removed by restoring the pre-test save");
        }
        private void Render(string name,Vector3 eye,Vector3 target,bool inside,float fov=65)
        {
            var camera=new GameObject("Workshop inspection camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.position=World(eye);camera.transform.LookAt(World(target));camera.fieldOfView=fov;camera.nearClipPlane=.06f;camera.farClipPlane=1500;
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;camera.targetTexture=rt;
            operations.RefreshViewLighting(camera,inside);
            try
            {
                camera.Render();RenderTexture.active=rt;
                var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);
                try
                {
                    pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();
                    var samples=pixels.GetPixels32();int low=765,high=0;
                    for(int i=0;i<samples.Length;i+=997){int light=samples[i].r+samples[i].g+samples[i].b;low=Math.Min(low,light);high=Math.Max(high,light);}
                    Require(high>45&&high-low>20,"direct scene render contains lit geometry / "+name);
                    string file=name+".png";File.WriteAllBytes(Folder+"/"+file,pixels.EncodeToPNG());pictures.Add(file);
                }
                finally{Destroy(pixels);}
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(camera.gameObject);operations.RefreshViewLighting(Camera.main,StationInteriorMode.IsInside);}
        }
        private void CaptureGeometry()
        {
            Render("01-kommando-wandterminals",new Vector3(0,2.6f,40.5f),new Vector3(0,1.8f,48),true,82);
            Render("02-drohnenhalle-zwei-umbauplaetze",RingWalkthrough.At(40.4f,30,2.4f),RingWalkthrough.At(47,30,1.6f),true,78);
            Render("03-halle-wandmoebel-und-schrott",RingWalkthrough.At(50,30,2.4f),RingWalkthrough.At(42.5f,30,1.7f),true,82);
            Render("04-geschlossenes-havarie-gelenk",RingWalkthrough.At(33.5f,68,1.8f),RingWalkthrough.At(33.5f,75,2.5f),true,68);
            Render("05-groesseres-aussendock",RingWalkthrough.At(104,25,32),RingWalkthrough.At(72,30,1.5f),false,59);
            Render("09-bodengleiche-dockreihen-und-fahrgassen",RingWalkthrough.At(92,30,4.2f),RingWalkthrough.At(69,30,1.1f),false,88);
            Render("10-breites-ringschott",RingWalkthrough.At(33.5f,36,2.3f),RingWalkthrough.At(33.5f,45,2.2f),true,82);
            foreach(var room in operations.RoomPlans)if(room.RoomId=="drone-workshop")
                foreach(string side in new[]{"left","right"})
                {
                    var bench=room.BoundRoot("bench-"+side);
                    Render(side=="left"?"11-originalwerkbank-links-vorne":"12-originalwerkbank-rechts-vorne",
                        layout.transform.InverseTransformPoint(bench.TransformPoint(new Vector3(0,1.5f,-.9f))),
                        layout.transform.InverseTransformPoint(bench.TransformPoint(new Vector3(0,.75f,0))),true,100);
                }
            var signs=Array.FindAll(layout.GetComponentsInChildren<StationDisplay>(),d=>d.Caption=="DROHNENDECK");
            Require(signs.Length==2,"wall header has separately mounted displays on both physical sides");
            for(int side=0;side<signs.Length;side++)Render(side==0?"06-display-vorderseite":"07-display-rueckseite",
                layout.transform.InverseTransformPoint(signs[side].transform.TransformPoint(new Vector3(0,0,-2.1f))),
                layout.transform.InverseTransformPoint(signs[side].transform.position),true,80);
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);File.Delete(Folder+"/result.json");File.Delete(Folder+"/error.txt");
            yield return new WaitForSecondsRealtime(1);
            if(!Guard(()=>
            {
                var saves=FindFirstObjectByType<SaveGameMenu>();Require(saves!=null,"save facade exists");
                saves.StorageDirectoryOverride=Path.GetFullPath(Folder+"/Saves");
                CinematicLibrary.PreferencePrefix="SpaceMiner.Test.StationWorkshop."+Guid.NewGuid()+".";
                Require(SettingsStore.PathName.StartsWith(Path.GetFullPath(Folder),StringComparison.OrdinalIgnoreCase),"settings are isolated for this acceptance");
                FindFirstObjectByType<StartMenu>().StartDemo();FindFirstObjectByType<IntroSequence>().Skip();
            }))yield break;
            yield return null;yield return null;
            if(!Guard(()=>
            {
                mode=StationInteriorMode.Current;layout=mode.Layout;scenario=FindFirstObjectByType<WaterScenario>();operations=scenario.Operations;
                scenario.SetSimulationRate(0);scenario.Scanner.DismissDialogue();
                Require(operations!=null&&operations.Live,"current workshop campaign is active");
                Require(StationInteriorLayout.Revision==14,"cold-storage geometry declares a new interior-position migration revision");
                Physics.SyncTransforms();CheckWideBulkheads();CheckRingMarkings();CheckClosedJoints();CheckFallRecovery();CheckDisplayGeometry();CheckWorkshopGeometry();CheckDockInfrastructure();
                CheckCommandApproaches();
            }))yield break;
            // Open/close consumes a frame by design. Let its normal input boundary expire
            // before restoring the player and taking direct, UI-free scene renders.
            yield return null;yield return null;
            if(!Guard(CheckPhysicalRefit))yield break;
            yield return null;yield return null;
            if(!Guard(()=>
            {
                mode.RestoreInterior(layout.SpawnPosition,false,layout.SpawnYaw,0);CaptureGeometry();
                File.WriteAllText(Folder+"/result.json",JsonUtility.ToJson(new Result{passed=true,checks=checks,layout=StationInteriorLayout.Revision,pictures=pictures.ToArray()},true));
                Debug.Log("STATION WORKSHOP CHECK PASSED: "+checks+" checks; "+pictures.Count+" direct scene renders");
            }))yield break;
            Application.Quit(0);
        }
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Independent metre-scale hard-surface tool. Built meshes have no primitive colliders.
    public sealed class MaintenanceLaserVisual : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Texture2D> textures = new List<Texture2D>();
        private Material navy, steel, graphite, copper, warning, lens;
        private Texture2D finish, normal;
        private Texture2D hullAlbedo, hullMetal, hullNormal;
        public Transform Aperture { get; private set; }
        public int TriangleCount { get; private set; }
        public const float CassetteHalfWidth = .18f, CassetteFront = .84f, StowedHead = 1.09f, ExtendedHead = 1.72f;
        private Transform middleSleeve, piston;
        public Transform Rotor { get; private set; }
        public Transform MiningTip { get; private set; }
        private Transform armUpper, armLower, palm;
        private readonly Transform[] fingers=new Transform[2];
        private Transform anchorOuter, anchorInner, anchorFoot;
        public Transform GripPoint { get; private set; }
        public const float ArmLength=1.30f;
        public const float CargoWidth=1.32f, CargoDepth=1.64f, CargoHeight=.90f;
        public const float MaxCargoGateOpening=.90f;
        public Transform CargoLid { get; private set; }
        public Transform CargoGate { get; private set; }
        private readonly Transform[] collarRings=new Transform[7];
        private Transform collarLip;
        public float CollarExtension { get; private set; }
        public bool CollarSealed { get; private set; }
        public Vector3 CollarContactPoint=>collarLip.TransformPoint(new Vector3(0,0,.012f));

        public void UseHullBlue(Color colour)
        {
            if(hullAlbedo==null) MakeHullPaint(colour);
            foreach(var material in materials)
                if(material==navy || material.name=="Cassette / worn blue-grey hull enamel")
                {
                    material.color=Color.white; material.mainTexture=hullAlbedo;
                    material.SetTexture("_MetallicGlossMap",hullMetal); material.EnableKeyword("_METALLICGLOSSMAP");
                    material.SetFloat("_GlossMapScale",1); material.SetFloat("_Metallic",.25f);
                    material.SetTexture("_BumpMap",hullNormal); material.SetFloat("_BumpScale",.12f);
                }
        }

        // The imported hull uses an atlas; new panels need their own matching paint maps.
        private void MakeHullPaint(Color colour)
        {
            const int size=512;
            hullAlbedo=new Texture2D(size,size,TextureFormat.RGBA32,true,false) { name="Mining / worn hull-blue panel albedo" };
            hullMetal=new Texture2D(size,size,TextureFormat.RGBA32,true,true) { name="Mining / paint and exposed metal roughness" };
            hullNormal=new Texture2D(size,size,TextureFormat.RGBA32,true,true) { name="Mining / enamel grain and scratched panel normal" };
            var albedo=new Color[size*size]; var metal=new Color[size*size]; var bump=new Color[size*size];
            float seed=0; foreach(char c in gameObject.name) seed=(seed*31+c)%997;
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float u=x/(float)(size-1),v=y/(float)(size-1);
                float cloud=Mathf.PerlinNoise(u*7+seed,v*7+13);
                float grain=Mathf.PerlinNoise(x*.43f+seed,y*.71f);
                float fleck=Mathf.PerlinNoise(x*.12f+seed,y*.15f+47);
                float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));
                float border=1-Mathf.SmoothStep(.015f,.085f,edge);
                bool chip=edge<.026f && fleck>.50f+edge*6;
                bool scratch=(y%113==31 && x%197>36 && x%197<94) ||
                    (Mathf.Abs((x+y*3)%367-73)<2 && fleck>.67f && edge>.08f);
                float panelEdge=Mathf.Min(Mathf.Min(Mathf.Abs(u-.055f),Mathf.Abs(u-.945f)),Mathf.Min(Mathf.Abs(v-.055f),Mathf.Abs(v-.945f)));
                bool seam=panelEdge<.0025f;
                Color paint=colour*(.79f+cloud*.36f+(grain-.5f)*.10f);
                paint=Color.Lerp(paint,new Color(.07f,.09f,.12f),border*(.12f+cloud*.20f));
                float wear=chip?1:scratch?.65f:0;
                paint=Color.Lerp(paint,new Color(.43f,.48f,.53f)*(.7f+grain*.45f),wear);
                if(seam) paint*=.48f;
                albedo[y*size+x]=new Color(paint.r,paint.g,paint.b,1);
                float metallic=Mathf.Lerp(.22f,.82f,wear),smoothness=Mathf.Lerp(.27f+cloud*.24f,.61f,wear);
                metal[y*size+x]=new Color(metallic,metallic,metallic,seam?.18f:smoothness);
                bump[y*size+x]=new Color(.5f+(grain-.5f)*.13f,.5f+(fleck-.5f)*.07f+(seam?-.12f:0),1,1);
            }
            hullAlbedo.SetPixels(albedo); hullMetal.SetPixels(metal); hullNormal.SetPixels(bump);
            foreach(var texture in new[]{hullAlbedo,hullMetal,hullNormal})
            { texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Trilinear; texture.anisoLevel=8; texture.Apply(true,true); textures.Add(texture); }
        }

        public void Build()
        {
            MakeMaterials();
            BuildHead();
        }
        private void MakeMaterials()
        {
            MakeFinish();
            navy = Finish("Laser / worn navy enamel", new Color(.10f, .145f, .19f), .5f, .42f);
            steel = Finish("Laser / machined steel edges", new Color(.48f, .53f, .57f), .88f, .7f);
            graphite = Finish("Laser / graphite ceramic", new Color(.035f, .045f, .055f), .18f, .28f);
            copper = Finish("Laser / coolant fittings", new Color(.42f, .24f, .11f), .78f, .58f);
            warning = Finish("Laser / amber safety marks", new Color(.93f, .5f, .07f), .3f, .4f);
            lens = Finish("Laser / recessed cyan optics", new Color(.05f, .42f, .54f), .5f, .92f);
            lens.EnableKeyword("_EMISSION"); SetWelding(false);
        }
        private void BuildHead()
        {
            Housing("Bevelled main laser casing", new Vector2(.27f, .255f), .12f, -.17f, .036f, navy, steel);
            Lathe("Rear quick-release socket", Vector3.zero, new[] {
                P(.055f,.20f), P(.082f,.20f), P(.097f,.188f), P(.097f,.14f), P(.087f,.126f), P(.065f,.126f), P(.055f,.14f), P(.055f,.20f) }, steel);
            Lathe("Rear vibration isolator", Vector3.zero, new[] { P(.064f,.222f), P(.078f,.222f), P(.083f,.21f), P(.083f,.19f), P(.064f,.19f), P(.064f,.222f) }, graphite);
            Lathe("Collimator barrel", Vector3.zero, new[] { P(.079f,-.13f), P(.095f,-.15f), P(.095f,-.28f), P(.075f,-.30f), P(.055f,-.30f), P(.055f,-.13f), P(.079f,-.13f) }, graphite);
            // Separate chamfered cooling discs and visible slots between them.
            for (int i = 0; i < 7; i++)
            {
                float z = -.18f - i * .018f;
                Lathe("Machined cooling fin " + i, Vector3.zero, new[] {
                    P(.081f,z), P(.112f,z), P(.118f,z-.003f), P(.118f,z-.008f), P(.112f,z-.011f), P(.081f,z-.011f), P(.081f,z) }, steel);
            }
            Lathe("Focus adjustment knurled collar", Vector3.zero, new[] { P(.055f,-.31f), P(.09f,-.31f), P(.101f,-.32f), P(.101f,-.36f), P(.089f,-.37f), P(.055f,-.37f), P(.055f,-.31f) }, navy);
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20;
                var rib = Housing("Focus grip ridge", new Vector2(.008f,.012f), -.321f, -.359f, .002f, steel, steel);
                rib.localPosition = new Vector3(Mathf.Cos(a) * .1f, Mathf.Sin(a) * .1f, 0);
                rib.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90);
            }
            Lathe("Tapered ceramic nozzle shield", Vector3.zero, new[] {
                P(.055f,-.36f), P(.083f,-.36f), P(.086f,-.373f), P(.063f,-.452f), P(.058f,-.467f), P(.038f,-.467f), P(.032f,-.452f), P(.038f,-.37f), P(.055f,-.36f) }, graphite);
            Lathe("Polished aperture rim", Vector3.zero, new[] { P(.036f,-.46f), P(.056f,-.46f), P(.059f,-.465f), P(.059f,-.479f), P(.053f,-.485f), P(.036f,-.485f), P(.032f,-.478f), P(.032f,-.467f), P(.036f,-.46f) }, steel);
            Lathe("Recessed optical glass", Vector3.zero, new[] { P(0,-.47f), P(.032f,-.47f), P(.034f,-.474f), P(.032f,-.477f), P(0,-.477f) }, lens);
            Aperture = new GameObject("Laser beam emission aperture").transform;
            Aperture.SetParent(transform, false); Aperture.localPosition = new Vector3(0, 0, -.487f);

            for (int side = -1; side <= 1; side += 2)
            {
                var cover = Housing("Removable side service cover", new Vector2(.018f,.175f), .071f, -.123f, .007f, navy, steel);
                cover.localPosition = new Vector3(side * .139f, 0, 0);
                for (int i = 0; i < 5; i++)
                {
                    var vent = Housing("Recessed side cooling slot", new Vector2(.003f,.09f), .04f-i*.027f, .027f-i*.027f, .001f, graphite, graphite);
                    vent.localPosition = new Vector3(side * .149f, 0, 0);
                }
                // Fasteners on the front mounting flange face the same direction as the lens.
                foreach (float y in new[] { -.087f, .087f }) Bolt(new Vector3(side * .092f, y, -.174f));
                var stripe = Housing("Amber tool hazard strip", new Vector2(.013f,.13f), .095f, .081f, .003f, warning, graphite);
                stripe.localPosition = new Vector3(side * .142f, 0, 0);
                for (int i = 0; i < 4; i++)
                {
                    var tick = Housing("Safety strip dark division", new Vector2(.014f,.012f), .098f, .095f, .002f, graphite, graphite);
                    tick.localPosition = new Vector3(side * .142f, -.046f + i * .03f, 0);
                }
                Tube("Rigid copper coolant loop", new[] { new Vector3(side*.055f,.1f,.18f), new Vector3(side*.094f,.147f,.13f), new Vector3(side*.104f,.158f,-.03f), new Vector3(side*.07f,.12f,-.175f) }, .011f, copper);
                Tube("Braided power return", new[] { new Vector3(side*.045f,-.1f,.2f), new Vector3(side*.09f,-.148f,.12f), new Vector3(side*.10f,-.15f,-.04f), new Vector3(side*.055f,-.095f,-.16f) }, .013f, graphite);
                Lathe("Coolant compression fitting", new Vector3(side*.055f,.1f,0), new[] { P(.011f,.18f), P(.023f,.18f), P(.023f,.15f), P(.011f,.15f), P(.011f,.18f) }, steel, 12);
            }
            var rail = Housing("Top accessory rail", new Vector2(.09f,.018f), .09f, -.11f, .005f, graphite, steel);
            rail.localPosition = new Vector3(0, .143f, 0);
            for (int i = 0; i < 6; i++)
            {
                var tooth = Housing("Accessory rail tooth", new Vector2(.10f,.018f), .07f-i*.029f, .058f-i*.029f, .004f, steel, steel);
                tooth.localPosition = new Vector3(0,.158f,0);
            }
            var indicator = Housing("Cyan operating status inset", new Vector2(.035f,.009f), -.02f, -.075f, .003f, lens, graphite);
            indicator.localPosition = new Vector3(0,.131f,0);
        }

        public void BuildMiningHead()
        {
            MakeMaterials();
            Housing("Bevelled mining motor casing",new Vector2(.31f,.28f),.12f,-.14f,.03f,navy,steel);
            Lathe("Mining quick-release isolator",Vector3.zero,new[] { P(.055f,.222f),P(.083f,.222f),P(.083f,.12f),P(.055f,.12f),P(.055f,.222f) },graphite);
            for(int i=0;i<5;i++)
                Lathe("Motor cooling ring",Vector3.zero,new[] { P(.09f,-.12f-i*.018f),P(.13f,-.12f-i*.018f),P(.13f,-.13f-i*.018f),P(.09f,-.13f-i*.018f),P(.09f,-.12f-i*.018f) },steel);
            foreach(float side in new[] { -1f,1f })
            {
                var cover=Housing("Mining motor service panel",new Vector2(.014f,.19f),.08f,-.11f,.006f,navy,steel);
                cover.localPosition=new Vector3(side*.16f,0,0);
                for(int i=0;i<5;i++)
                {
                    var vent=Housing("Motor vent",new Vector2(.005f,.11f),.056f-i*.03f,.043f-i*.03f,.001f,graphite,graphite);
                    vent.localPosition=new Vector3(side*.169f,0,0);
                }
                foreach(float y in new[] { -.10f,.10f }) Bolt(new Vector3(side*.11f,y,-.143f));
                Tube("Armoured motor power loop",new[] { new Vector3(side*.06f,.11f,.18f),new Vector3(side*.12f,.17f,.12f),new Vector3(side*.12f,.17f,-.06f),new Vector3(side*.06f,.11f,-.15f) },.012f,graphite);
                var mark=Housing("Mining safety inset",new Vector2(.014f,.09f),.10f,.083f,.003f,warning,graphite);
                mark.localPosition=new Vector3(side*.168f,0,0);
            }
            Lathe("Bearing and dust seal",Vector3.zero,new[] { P(.045f,-.20f),P(.095f,-.20f),P(.105f,-.22f),P(.105f,-.265f),P(.08f,-.28f),P(.045f,-.28f),P(.045f,-.20f) },graphite);
            Rotor=new GameObject("Independent rotating auger and carbide cutters").transform; Rotor.SetParent(transform,false);
            int first=transform.childCount;
            Lathe("Machined drill chuck",Vector3.zero,new[] { P(0,-.245f),P(.09f,-.245f),P(.10f,-.26f),P(.10f,-.30f),P(.055f,-.33f),P(0,-.33f) },steel,48);
            Lathe("Conical auger core",Vector3.zero,new[] { P(0,-.30f),P(.052f,-.30f),P(.045f,-.48f),P(.025f,-.62f),P(0,-.68f) },graphite,48);
            BuildSpiralFlight();
            for(int i=0;i<10;i++)
            {
                float t=i/9f,a=t*Mathf.PI*5;
                var tooth=Housing("Replaceable carbide cutting insert",new Vector2(.025f,.035f),.015f,-.015f,.004f,steel,graphite);
                tooth.localPosition=new Vector3(Mathf.Cos(a)*Mathf.Lerp(.10f,.034f,t),Mathf.Sin(a)*Mathf.Lerp(.10f,.034f,t),Mathf.Lerp(-.32f,-.645f,t));
                tooth.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
            }
            var parts=new List<Transform>();
            for(int i=first;i<transform.childCount;i++) parts.Add(transform.GetChild(i));
            foreach(var part in parts) part.SetParent(Rotor,false);
            MiningTip=new GameObject("Actual drill contact tip").transform; MiningTip.SetParent(Rotor,false); MiningTip.localPosition=new Vector3(0,0,-.68f);
        }
        private void BuildSpiralFlight()
        {
            const int steps=160; var vertices=new List<Vector3>(); var indices=new List<int>(); var uv=new List<Vector2>();
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps,a=t*Mathf.PI*5,z=Mathf.Lerp(-.305f,-.645f,t),outer=Mathf.Lerp(.112f,.037f,t),inner=Mathf.Lerp(.051f,.022f,t);
                Vector3 radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                foreach(var p in new[] { radial*inner+new Vector3(0,0,z-.006f),radial*outer+new Vector3(0,0,z-.006f),radial*outer+new Vector3(0,0,z+.006f),radial*inner+new Vector3(0,0,z+.006f) })
                { vertices.Add(p); uv.Add(new Vector2(t*5,p.magnitude)); }
                if(i==steps) continue;
                for(int edge=0;edge<4;edge++)
                { int k=i*4+edge,n=i*4+(edge+1)%4; indices.AddRange(new[] { k,n,n+4,k,n+4,k+4 }); }
            }
            indices.AddRange(new[] { 0,2,1,0,3,2,steps*4,steps*4+1,steps*4+2,steps*4,steps*4+2,steps*4+3 });
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            Render("Continuous thick helical auger flight",mesh,new[] { steel });
        }
        public void BuildCargoCartridge()
        {
            MakeMaterials();
            float halfWidth=CargoWidth*.5f, halfDepth=CargoDepth*.5f;
            Housing("Large cartridge armoured base",new Vector2(CargoWidth,.065f),halfDepth,-halfDepth,.018f,navy,steel);
            foreach(float side in new[] { -1f,1f })
            {
                foreach(float y in new[] { .04f,CargoHeight })
                {
                    var cross=Housing("Bolted cartridge cross rail",new Vector2(CargoWidth,.045f),.025f,-.025f,.009f,navy,steel);
                    cross.localPosition=new Vector3(0,y,side*halfDepth);
                    var rail=Housing("Long cartridge structural rail",new Vector2(.045f,.045f),halfDepth,-halfDepth,.009f,navy,steel);
                    rail.localPosition=new Vector3(side*halfWidth,y,0);
                }
                foreach(float z in new[] { -halfDepth,halfDepth })
                {
                    var post=Housing("Cartridge corner pillar",new Vector2(.055f,CargoHeight-.02f),.028f,-.028f,.010f,navy,steel);
                    post.localPosition=new Vector3(side*(halfWidth-.02f),CargoHeight*.5f+.01f,z);
                    foreach(float y in new[] { .10f,CargoHeight-.06f }) Bolt(new Vector3(side*(halfWidth-.02f),y,z-.030f));
                }
                var shoe=Housing("Painted roof mounting saddle",new Vector2(.14f,.12f),.65f,-.65f,.012f,navy,steel);
                shoe.localPosition=new Vector3(side*.38f,-.075f,0);
            }
            CargoLid=new GameObject("Front-hinged cargo lid / clears rear sliding gate").transform; CargoLid.SetParent(transform,false);
            CargoLid.localPosition=new Vector3(0,CargoHeight+.055f,halfDepth);
            int first=transform.childCount;
            Housing("Bevelled blue lid armour",new Vector2(CargoWidth+.06f,.065f),CargoDepth+.03f,-.03f,.018f,navy,steel);
            var hatch=Housing("Inset lid service panel",new Vector2(CargoWidth-.18f,.018f),CargoDepth-.16f,.16f,.008f,navy,graphite); hatch.localPosition=Vector3.up*.044f;
            foreach(float x in new[] { -.45f,.45f })
            {
                var rib=Housing("Lid reinforcing rib",new Vector2(.025f,.035f),CargoDepth-.12f,.12f,.006f,navy,steel); rib.localPosition=new Vector3(x,.057f,0);
                foreach(float z in new[] { .20f,CargoDepth-.20f })
                {
                    var screw=new GameObject("Lid captive screw").transform; screw.SetParent(transform,false);
                    screw.localPosition=new Vector3(x,.06f,z); screw.localRotation=Quaternion.Euler(90,0,0); BoltOn(screw);
                }
            }
            Tube("Recessed cargo lid handle",new[] { new Vector3(-.12f,.055f,1.32f),new Vector3(-.12f,.14f,1.32f),new Vector3(.12f,.14f,1.32f),new Vector3(.12f,.055f,1.32f) },.015f,graphite);
            ReparentParts(first,CargoLid);
            foreach(float x in new[] { -.44f,.44f })
            {
                var hinge=Lathe("Sealed cargo lid hinge axle",Vector3.zero,new[] { P(0,-.12f),P(.065f,-.12f),P(.075f,-.09f),P(.075f,.09f),P(.065f,.12f),P(0,.12f) },steel,48);
                hinge.localPosition=new Vector3(x,CargoHeight+.055f,halfDepth); hinge.localRotation=Quaternion.Euler(0,90,0);
                var plate=Housing("Blue hinge bracket",new Vector2(.25f,.14f),.035f,-.035f,.012f,navy,steel); plate.localPosition=new Vector3(x,CargoHeight-.015f,halfDepth+.025f);
                var latch=Housing("Rear lid latch",new Vector2(.13f,.12f),.04f,-.04f,.012f,navy,steel); latch.localPosition=new Vector3(x,CargoHeight,-halfDepth+.045f);
            }
            PoseCargoLid(0);
        }
        public void PoseCargoLid(float opening) { CargoLid.localRotation=Quaternion.Euler(0,180,0)*Quaternion.Euler(-110*Mathf.Clamp01(opening),0,0); }

        public void BuildCargoGate()
        {
            MakeMaterials();
            foreach(float side in new[]{-1f,1f})
            {
                var rail=Housing("Compact rear sliding gate side guide",new Vector2(.075f,CargoHeight+.045f),.085f,-.07f,.012f,navy,steel);
                rail.localPosition=new Vector3(side*(CargoWidth*.5f+.005f),0,0);
                var channel=Housing("Recessed vertical gate guide channel",new Vector2(.024f,CargoHeight-.04f),-.071f,-.078f,.004f,graphite,steel);
                channel.localPosition=Vector3.right*(side*(CargoWidth*.5f+.005f));
                foreach(float y in new[]{-.35f,.35f}) Bolt(new Vector3(side*(CargoWidth*.5f+.005f),y,-.083f));
            }
            // The seal stays on the cartridge. Moving it with the wall would foul the top lid.
            foreach(float side in new[]{-1f,1f})
            {
                var seal=Housing("Rear wall side compression seal",new Vector2(.025f,CargoHeight-.04f),.078f,.045f,.004f,graphite,graphite);
                seal.localPosition=Vector3.right*(side*(CargoWidth*.5f-.045f));
                var crossSeal=Housing("Rear wall horizontal compression seal",new Vector2(CargoWidth-.06f,.025f),.078f,.045f,.004f,graphite,graphite);
                crossSeal.localPosition=Vector3.up*(side*(CargoHeight*.5f-.015f));
            }
            CargoGate=new GameObject("Upward sliding rear cargo wall").transform; CargoGate.SetParent(transform,false);
            int first=transform.childCount;
            Housing("Armoured sliding discharge wall",new Vector2(CargoWidth-.025f,CargoHeight),.045f,-.045f,.016f,navy,steel);
            foreach(float x in new[]{-.43f,.43f})
            {
                var rib=Housing("Discharge gate reinforcement",new Vector2(.06f,CargoHeight-.21f),-.046f,-.069f,.010f,steel,graphite); rib.localPosition=Vector3.right*x;
                foreach(float y in new[]{-.29f,.29f}) Bolt(new Vector3(x,y,-.077f));
            }
            var lockBar=Housing("Rear docking seal lower bar",new Vector2(CargoWidth-.12f,.07f),-.045f,-.08f,.01f,graphite,steel); lockBar.localPosition=Vector3.down*(CargoHeight*.5f-.07f);
            ReparentParts(first,CargoGate);
        }
        public void PoseCargoGate(float opening) { CargoGate.localPosition=Vector3.up*(Mathf.Clamp(opening,0,MaxCargoGateOpening)*CargoHeight); }

        public void BuildCargoReceiver()
        {
            MakeMaterials();
            foreach(float side in new[]{-1f,1f})
            {
                var wall=Housing("Wide unloading receiver side",new Vector2(.12f,1.12f),-.2f,-3.3f,.015f,navy,steel); wall.localPosition=Vector3.right*(side*.85f);
                var rim=Housing("Rectangular docking seal crosspiece",new Vector2(1.82f,.11f),.07f,-.15f,.016f,graphite,steel); rim.localPosition=Vector3.up*(side*.55f);
                foreach(float y in new[]{-.42f,.42f}) Bolt(new Vector3(side*.85f,y,.08f));
            }
            var floor=Housing("Enclosed unloading receiver floor",new Vector2(1.82f,.10f),-.2f,-3.3f,.012f,steel,graphite); floor.localPosition=Vector3.down*.58f;
            var roof=Housing("Enclosed unloading receiver roof",new Vector2(1.82f,.08f),-.2f,-3.3f,.012f,navy,steel); roof.localPosition=Vector3.up*.55f;
            for(int i=0;i<8;i++)
            {
                var roller=Lathe("Receiver transport roller",Vector3.zero,new[]{P(0,-.65f),P(.045f,-.65f),P(.045f,.65f),P(0,.65f)},steel,24);
                roller.localPosition=new Vector3(0,-.43f,-.24f-i*.36f); roller.localRotation=Quaternion.Euler(0,90,0);
            }
            for(int i=0;i<collarRings.Length;i++)
            {
                var ring=new GameObject("Flexible rectangular collar bellows fold "+i).transform; ring.SetParent(transform,false);
                int first=transform.childCount;
                foreach(float side in new[]{-1f,1f})
                {
                    var vertical=Housing("Bellows rubber vertical band",new Vector2(.12f,1.04f),.012f,-.012f,.006f,graphite,graphite); vertical.localPosition=Vector3.right*(side*.67f);
                    var horizontal=Housing("Bellows rubber horizontal band",new Vector2(1.34f,.12f),.012f,-.012f,.006f,graphite,graphite); horizontal.localPosition=Vector3.up*(side*.46f);
                }
                ReparentParts(first,ring); collarRings[i]=ring;
            }
            collarLip=new GameObject("Moving suction collar lip").transform; collarLip.SetParent(transform,false);
            int lipFirst=transform.childCount;
            foreach(float side in new[]{-1f,1f})
            {
                var vertical=Housing("Blue collar face clamp",new Vector2(.075f,1.07f),.01f,-.03f,.009f,navy,steel); vertical.localPosition=Vector3.right*(side*.75f);
                var horizontal=Housing("Collar face compression gasket",new Vector2(1.50f,.09f),.012f,-.015f,.008f,graphite,graphite); horizontal.localPosition=Vector3.up*(side*.46f);
                var pad=Housing("Suction seating strip",new Vector2(.10f,.89f),.012f,-.012f,.005f,graphite,graphite); pad.localPosition=Vector3.right*(side*.665f);
                var pump=Housing("Collar vacuum actuator",new Vector2(.13f,.24f),.0f,-.15f,.014f,navy,steel); pump.localPosition=new Vector3(side*.82f,-.24f,0);
                var status=Housing("Collar seal status lamp",new Vector2(.045f,.09f),.015f,.01f,.004f,lens,graphite); status.localPosition=new Vector3(side*.82f,-.24f,0);
            }
            ReparentParts(lipFirst,collarLip); PoseCargoCollar(0,false);
        }
        public void PoseCargoCollar(float extension,bool sealedContact)
        {
            CollarExtension=Mathf.Clamp01(extension); CollarSealed=sealedContact && CollarExtension>.999f;
            float tip=Mathf.Lerp(-.34f,-.097f,CollarExtension);
            collarLip.localPosition=Vector3.forward*tip;
            for(int i=0;i<collarRings.Length;i++)
            {
                collarRings[i].localPosition=Vector3.forward*Mathf.Lerp(-.52f,tip,i/(float)(collarRings.Length-1));
                collarRings[i].localScale=new Vector3(1,1,Mathf.Lerp(.9f,2.4f,CollarExtension));
            }
            lens.SetColor("_EmissionColor",CollarSealed?new Color(.05f,.7f,.9f):new Color(.65f,.24f,.015f));
        }
        public void BuildCargoMounts(Vector3[] roofPoints)
        {
            MakeMaterials();
            foreach(float z in new[]{-.53f,.43f})
            {
                var beam=Housing("Common bolted cargo and anchor crossmember",new Vector2(2.03f,.10f),z+.07f,z-.07f,.016f,navy,steel); beam.localPosition=Vector3.up*1.70f;
                foreach(float x in new[]{-.85f,.85f})
                {
                    var post=Housing("Anchor rail load-bearing hull-frame riser",new Vector2(.15f,.29f),z+.075f,z-.075f,.014f,navy,steel); post.localPosition=new Vector3(x,1.86f,0);
                    foreach(float y in new[]{1.74f,1.96f}) Bolt(new Vector3(x,y,z-.085f));
                }
            }
            foreach(var point in roofPoints)
            {
                var pad=Housing("Original hull roof bolted mounting foot",new Vector2(.28f,.04f),point.z+.15f,point.z-.15f,.012f,steel,graphite); pad.localPosition=new Vector3(point.x,point.y+.012f,0);
                float height=1.67f-point.y;
                var leg=Housing("Structural standoff from hull to cargo frame",new Vector2(.14f,height),point.z+.08f,point.z-.08f,.013f,navy,steel); leg.localPosition=new Vector3(point.x,point.y+height*.5f,0);
                foreach(float dx in new[]{-.10f,.10f})
                {
                    var fastener=new GameObject("Cargo hull anchor screw").transform; fastener.SetParent(transform,false);
                    fastener.localPosition=point+new Vector3(dx,.04f,-.10f); fastener.localRotation=Quaternion.Euler(90,0,0); BoltOn(fastener);
                }
            }
        }

        // Articulated collection tool, sharing the drill's bevels, fasteners and PBR finish.
        public void BuildMiningArm()
        {
            MakeMaterials();
            Housing("Manipulator bolted shoulder housing",new Vector2(.27f,.32f),.25f,-.25f,.028f,navy,steel);
            var mount=Housing("Manipulator hull mounting bridge",new Vector2(.42f,.25f),.17f,-.17f,.018f,navy,steel);
            mount.localPosition=new Vector3(.22f,0,0);
            foreach(float y in new[] { -.115f,.115f }) foreach(float z in new[] { -.14f,.14f })
            {
                var bolt=new GameObject("Shoulder hull screw").transform; bolt.SetParent(transform,false);
                bolt.localPosition=new Vector3(.40f,y,z); bolt.localRotation=Quaternion.Euler(0,90,0); BoltOn(bolt);
            }
            armUpper=ArmLink("Shoulder / upper articulated link");
            armLower=ArmLink("Elbow / forearm articulated link");
            palm=new GameObject("Servo wrist and collecting jaws").transform; palm.SetParent(transform,false);
            int first=transform.childCount;
            Lathe("Wrist sealed rotary bearing",Vector3.zero,new[] { P(.03f,-.12f),P(.10f,-.12f),P(.115f,-.09f),P(.115f,.025f),P(.095f,.05f),P(.03f,.05f),P(.03f,-.12f) },steel,48);
            Housing("Wrist servo cover",new Vector2(.23f,.15f),.10f,.025f,.02f,navy,steel);
            ReparentParts(first,palm);
            for(int i=0;i<2;i++)
            {
                fingers[i]=new GameObject("Independent collecting jaw "+i).transform; fingers[i].SetParent(palm,false);
                first=transform.childCount;
                Housing("Jaw knuckle armour",new Vector2(.065f,.095f),.12f,-.025f,.012f,navy,steel);
                Tube("Curved machined collecting finger",new[] { new Vector3(0,0,.07f),new Vector3(0,0,.27f),new Vector3(i==0?.045f:-.045f,0,.36f),new Vector3(i==0?.065f:-.065f,0,.37f) },.024f,steel);
                var pad=Housing("Replaceable serrated gripping pad",new Vector2(.035f,.065f),.28f,.18f,.007f,graphite,steel);
                pad.localPosition=new Vector3(i==0?.018f:-.018f,0,0);
                foreach(float y in new[] { -.033f,.033f }) Bolt(new Vector3(0,y,.125f));
                ReparentParts(first,fingers[i]);
            }
            GripPoint=new GameObject("Held sample centre between jaws").transform; GripPoint.SetParent(palm,false); GripPoint.localPosition=Vector3.forward*.25f;
        }
        private Transform ArmLink(string label)
        {
            var link=new GameObject(label).transform; link.SetParent(transform,false); int first=transform.childCount;
            Housing("Chamfered removable link cover",new Vector2(.135f,.13f),ArmLength-.09f,.09f,.025f,navy,steel);
            foreach(float side in new[] { -1f,1f })
            {
                var rail=Housing("Manipulator structural side rail",new Vector2(.028f,.16f),ArmLength-.05f,.05f,.007f,steel,graphite);
                rail.localPosition=new Vector3(side*.085f,0,0);
                foreach(float z in new[] { .13f,ArmLength-.13f }) Bolt(new Vector3(side*.075f,.06f,z));
            }
            Tube("Protected hydraulic line",new[] { new Vector3(0,-.09f,.04f),new Vector3(0,-.13f,.25f),new Vector3(0,-.13f,ArmLength-.25f),new Vector3(0,-.09f,ArmLength-.04f) },.018f,graphite);
            var pivot=Lathe("Joint bearing / bronze dust seal",Vector3.zero,new[] { P(.04f,-.11f),P(.12f,-.11f),P(.135f,-.09f),P(.135f,.09f),P(.12f,.11f),P(.04f,.11f),P(.04f,-.11f) },steel,48);
            pivot.localRotation=Quaternion.Euler(0,90,0);
            var mark=Housing("Manipulator safety inset",new Vector2(.07f,.008f),.42f,.25f,.003f,warning,graphite); mark.localPosition=Vector3.up*.071f;
            ReparentParts(first,link); return link;
        }
        private void ReparentParts(int first,Transform parent)
        {
            var parts=new List<Transform>(); for(int i=first;i<transform.childCount;i++) parts.Add(transform.GetChild(i));
            foreach(var part in parts) part.SetParent(parent,false);
        }
        public void PoseMiningArm(Vector3 grip,Quaternion orientation,float opening,bool folded)
        {
            Vector3 wrist=grip-orientation*GripPoint.localPosition;
            float reach=Mathf.Clamp(wrist.magnitude,.04f,ArmLength*2-.01f);
            Vector3 direction=wrist.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(folded?Vector3.back:Vector3.left,direction).normalized;
            Vector3 elbow=direction*(reach*.5f)+bend*Mathf.Sqrt(ArmLength*ArmLength-reach*reach*.25f);
            wrist=direction*reach;
            armUpper.SetLocalPositionAndRotation(Vector3.zero,Quaternion.LookRotation(elbow,Vector3.up));
            armLower.SetLocalPositionAndRotation(elbow,Quaternion.LookRotation(wrist-elbow,Vector3.up));
            palm.SetLocalPositionAndRotation(wrist,orientation);
            for(int i=0;i<2;i++) fingers[i].localPosition=new Vector3((i==0?-1:1)*opening*.5f,0,0);
        }
        public void BuildAnchorRail()
        {
            MakeMaterials();
            foreach(float side in new[] { -1f,1f })
            {
                var shell=Housing("Long stabiliser actuator side plate",new Vector2(.035f,.25f),.74f,-.74f,.010f,navy,steel); shell.localPosition=Vector3.right*(side*.105f);
                var cap=Housing("Long stabiliser actuator roof / floor",new Vector2(.23f,.035f),.74f,-.74f,.010f,navy,steel); cap.localPosition=Vector3.up*(side*.11f);
            }
            foreach(float z in new[] { -.55f,.48f })
            {
                var clamp=Housing("Cargo-frame stabiliser mounting saddle",new Vector2(.36f,.055f),z+.065f,z-.065f,.010f,navy,steel); clamp.localPosition=Vector3.down*.15f;
                foreach(float x in new[] { -.14f,.14f }) Bolt(new Vector3(x,-.12f,z-.068f));
            }
            Lathe("Hollow actuator front guide",Vector3.zero,new[] { P(.071f,.79f),P(.115f,.79f),P(.115f,.72f),P(.071f,.72f),P(.071f,.79f) },steel,48);
            anchorOuter=Lathe("Stabiliser nested outer sleeve",Vector3.zero,new[] { P(.048f,0),P(.068f,0),P(.068f,-1.05f),P(.048f,-1.05f),P(.048f,0) },steel,48);
            anchorInner=Lathe("Stabiliser nested inner piston",Vector3.zero,new[] { P(.025f,0),P(.043f,0),P(.043f,-1.05f),P(.025f,-1.05f),P(.025f,0) },graphite,48);
            anchorFoot=new GameObject("Articulated contact foot with gripping hooks").transform; anchorFoot.SetParent(transform,false);
            int first=transform.childCount;
            Lathe("Asteroid contact foot",Vector3.zero,new[] { P(0,0),P(.13f,0),P(.15f,.02f),P(.15f,.055f),P(0,.055f) },graphite,48);
            foreach(float side in new[] { -1f,1f })
                Tube("Curved asteroid anchor hook",new[] { new Vector3(side*.10f,0,-.06f),new Vector3(side*.21f,0,-.02f),new Vector3(side*.19f,0,.12f),new Vector3(side*.10f,0,.13f) },.022f,steel);
            ReparentParts(first,anchorFoot); PoseAnchorRail(.82f,Quaternion.identity);
        }
        public void PoseAnchorRail(float end,Quaternion footOrientation)
        {
            anchorOuter.localPosition=Vector3.forward*Mathf.Lerp(.72f,end,.5f);
            anchorInner.localPosition=Vector3.forward*end;
            anchorFoot.SetLocalPositionAndRotation(Vector3.forward*end,footOrientation);
        }
        public void BuildCargoPort()
        {
            MakeMaterials();
            Lathe("Transfer connector neck",Vector3.zero,new[] { P(.055f,.18f),P(.075f,.18f),P(.075f,.03f),P(.055f,.03f),P(.055f,.18f) },navy);
            Lathe("Machined transfer mating flange",Vector3.zero,new[] { P(.055f,.05f),P(.10f,.05f),P(.11f,.04f),P(.11f,.01f),P(.095f,0),P(.055f,0),P(.055f,.05f) },steel);
            Lathe("Cargo port front seal",Vector3.zero,new[] { P(.053f,.007f),P(.081f,.007f),P(.081f,0),P(.053f,0),P(.053f,.007f) },graphite);
            for(int i=0;i<6;i++) { float a=i*Mathf.PI/3; Bolt(new Vector3(Mathf.Cos(a)*.09f,Mathf.Sin(a)*.09f,.012f)); }
        }

        // Same hard-surface finishes as the head, but a genuinely hollow cassette.
        // Coordinates are metres along the drone's +Z axis; the head remains outside the mouth.
        public void BuildCassette(float rearMountX, float frontMountX)
        {
            MakeMaterials();
            var hullPaint = Finish("Cassette / worn blue-grey hull enamel", new Color(.32f,.40f,.52f), .62f, .48f);
            Transform Panel(string label, Vector2 size, float rear, float front, Vector3 position, Material face)
            {
                var panel = Housing(label, size, front, rear, .014f, face, steel);
                panel.localPosition = position; return panel;
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Panel("Separate cassette side armour", new Vector2(.04f,.34f), -.12f,.84f, new Vector3(side*.16f,0,0), hullPaint);
                Panel("Cassette top / bottom armour", new Vector2(.36f,.04f), -.12f,.84f, new Vector3(0,side*.17f,0), hullPaint);
                Panel("Open mouth side flange", new Vector2(.045f,.34f), .805f,.86f, new Vector3(side*.16f,0,0), steel);
                Panel("Open mouth upper / lower flange", new Vector2(.32f,.035f), .805f,.86f, new Vector3(0,side*.155f,0), steel);
                for (int i=0;i<7;i++)
                    Panel("Recessed cassette service vent", new Vector2(.004f,.10f), .04f+i*.071f,.069f+i*.071f,
                        new Vector3(side*.181f,-.02f,0), graphite);
            }
            Panel("Closed actuator rear cover", new Vector2(.35f,.34f), -.15f,-.12f, Vector3.zero, hullPaint);
            Panel("Removable top maintenance hatch", new Vector2(.26f,.015f), -.045f,.65f, new Vector3(0,.199f,0), hullPaint);
            foreach(float x in new[] { -.11f,.11f }) foreach(float z in new[] { .01f,.59f })
            {
                var fastener = new GameObject("Top hatch captive screw").transform; fastener.SetParent(transform,false);
                fastener.localPosition = new Vector3(x,.21f,z); fastener.localRotation = Quaternion.Euler(90,0,0);
                BoltOn(fastener);
            }
            int mountIndex=0;
            foreach(float hullX in new[] { rearMountX, frontMountX })
            {
                float z = mountIndex++ == 0 ? .08f : .58f;
                // Saddle meets the sampled original hull; stand-offs bridge the real gap to the cassette.
                Panel("Bolted hull saddle plate", new Vector2(.022f,.56f), z-.095f,z+.095f, new Vector3(hullX,0,0), steel);
                Panel("Elastomer hull isolation pad", new Vector2(.014f,.50f), z-.085f,z+.085f, new Vector3(hullX+.017f,0,0), graphite);
                float end=-.19f, start=hullX+.024f;
                Panel("Structural mounting stand-off",new Vector2(Mathf.Max(.01f,end-start),.24f),z-.06f,z+.06f,
                    new Vector3((start+end)*.5f,0,0), steel);
                foreach(float y in new[] { -.23f,.23f })
                {
                    var fastener = new GameObject("Hull mounting hex bolt").transform; fastener.SetParent(transform,false);
                    fastener.localPosition = new Vector3(hullX+.03f,y,z); fastener.localRotation = Quaternion.Euler(0,-90,0);
                    BoltOn(fastener);
                }
                Panel("Cassette mounting band", new Vector2(.025f,.37f),z-.045f,z+.045f,new Vector3(.19f,0,0),steel);
            }
            // Circular guide has an actual open bore. Nested sleeves are hollow, rather than intersecting rods.
            Lathe("Actuator internal fixed barrel",Vector3.zero,new[] {
                P(.077f,.795f),P(.106f,.795f),P(.106f,-.08f),P(.077f,-.08f),P(.077f,.795f) },graphite);
            Lathe("Machined open guide bushing",Vector3.zero,new[] {
                P(.083f,.86f),P(.12f,.86f),P(.126f,.851f),P(.126f,.805f),P(.12f,.795f),P(.083f,.795f),P(.083f,.86f) },steel);
            Lathe("Recessed piston wiper seal",Vector3.zero,new[] {
                P(.076f,.857f),P(.084f,.857f),P(.084f,.826f),P(.076f,.826f),P(.076f,.857f) },graphite);
            middleSleeve=Lathe("Hollow telescopic outer sleeve",Vector3.zero,new[] {
                P(.055f,0),P(.074f,0),P(.074f,-.62f),P(.055f,-.62f),P(.055f,0) },steel);
            piston=Lathe("Polished telescopic inner piston",Vector3.zero,new[] {
                P(.027f,0),P(.052f,0),P(.052f,-.62f),P(.027f,-.62f),P(.027f,0) },steel);
            Panel("Small tool safety marker",new Vector2(.045f,.007f),.70f,.78f,new Vector3(0,.194f,0),warning);
            Panel("Recessed cassette status light",new Vector2(.035f,.007f),.65f,.68f,new Vector3(0,.195f,0),lens);
            PoseCassette(0);
        }
        private void BoltOn(Transform parent)
        {
            // Bolt() builds on this root, so reparent its two new pieces without changing their local geometry.
            int first=transform.childCount; Bolt(Vector3.zero);
            var pieces=new List<Transform>();
            for(int i=first;i<transform.childCount;i++) pieces.Add(transform.GetChild(i));
            foreach(var piece in pieces) piece.SetParent(parent,false);
        }
        public void PoseCassette(float extension)
        {
            middleSleeve.localPosition=Vector3.forward*Mathf.Lerp(.81f,1.20f,extension);
            piston.localPosition=Vector3.forward*(Mathf.Lerp(StowedHead,ExtendedHead,extension)-.222f);
        }
        internal bool StowedSleevesContained()
        {
            foreach(var sleeve in new[] { middleSleeve,piston }) foreach(var v in sleeve.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                Vector3 p=transform.InverseTransformPoint(sleeve.TransformPoint(v));
                if(p.z<-.12f || p.z>CassetteFront+.03f || Mathf.Abs(p.x)>.14f || Mathf.Abs(p.y)>.15f) return false;
            }
            return true;
        }
        public void SetWelding(bool value)
        {
            if (lens != null) lens.SetColor("_EmissionColor", new Color(.08f,.7f,1) * (value ? 2 : .15f));
        }
        private static Vector2 P(float radius, float z) => new Vector2(radius, z);
        private Material Finish(string label, Color color, float metalness, float smoothness)
        {
            var material = new Material(Shader.Find("Standard")) { name = label, color = color, mainTexture = finish };
            var metalMap = new Texture2D(64,64,TextureFormat.RGBA32,true,true) { name = label + " / metal and roughness" };
            var pixels = new Color[64*64];
            for(int y=0;y<64;y++) for(int x=0;x<64;x++)
            {
                float n=Mathf.PerlinNoise(x*.4f,y*.8f);
                pixels[y*64+x]=new Color(metalness,metalness,metalness,.72f+n*.25f);
            }
            metalMap.SetPixels(pixels); metalMap.Apply(true,true); textures.Add(metalMap);
            material.SetTexture("_MetallicGlossMap", metalMap); material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_GlossMapScale", smoothness); material.SetFloat("_Metallic", metalness);
            material.SetTexture("_BumpMap", normal); material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale", .18f);
            material.SetFloat("_Glossiness", smoothness); materials.Add(material); return material;
        }
        private void MakeFinish()
        {
            const int size = 256;
            finish = new Texture2D(size,size,TextureFormat.RGBA32,true,false) { name = "Laser brushed finish / fine wear" };
            normal = new Texture2D(size,size,TextureFormat.RGBA32,true,true) { name = "Laser micro-brushed normal" };
            textures.Add(finish); textures.Add(normal);
            var colors = new Color[size*size]; var normals = new Color[size*size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x*.17f,y*.85f);
                bool scratch = (y % 43 == 0 && x % 97 < 34) || (y % 71 == 2 && x > 160);
                float v = .78f + noise*.16f + (scratch ? .09f : 0);
                colors[y*size+x] = new Color(v,v,v,1);
                normals[y*size+x] = new Color(.5f + (noise-.5f)*.035f,.5f + Mathf.Sin(y*2.1f)*.025f,1,1);
            }
            finish.SetPixels(colors); normal.SetPixels(normals);
            foreach (var texture in textures) { texture.wrapMode = TextureWrapMode.Repeat; texture.anisoLevel = 4; texture.Apply(true,true); }
        }
        private Transform Render(string label, Mesh mesh, Material[] surface)
        {
            mesh.name = label; mesh.RecalculateBounds(); meshes.Add(mesh); TriangleCount += mesh.triangles.Length / 3;
            var go = new GameObject(label); go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = surface;
            return go.transform;
        }
        private Transform Lathe(string label, Vector3 offset, Vector2[] profile, Material material, int sides = 64)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (int i = 0; i < profile.Length; i++)
            {
                Vector2 tangent = profile[Mathf.Min(i+1,profile.Length-1)] - profile[Mathf.Max(i-1,0)];
                Vector2 n = new Vector2(-tangent.y,tangent.x).normalized;
                for (int j = 0; j <= sides; j++)
                {
                    float a = j * Mathf.PI * 2 / sides;
                    vertices.Add(offset + new Vector3(Mathf.Cos(a)*profile[i].x,Mathf.Sin(a)*profile[i].x,profile[i].y));
                    normals.Add(new Vector3(Mathf.Cos(a)*n.x,Mathf.Sin(a)*n.x,n.y)); uv.Add(new Vector2(j/(float)sides,profile[i].y*5));
                    if (i == profile.Length-1 || j == sides) continue;
                    int k = i*(sides+1)+j; indices.Add(k); indices.Add(k+sides+1); indices.Add(k+sides+2);
                    indices.Add(k); indices.Add(k+sides+2); indices.Add(k+1);
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0); mesh.RecalculateTangents();
            return Render(label,mesh,new[] { material });
        }
        private Transform Housing(string label, Vector2 size, float back, float front, float bevel, Material face, Material edge)
        {
            float x=size.x*.5f, y=size.y*.5f, c=Mathf.Min(bevel,Mathf.Min(x,y)*.5f);
            var outline = new[] { new Vector2(x-c,y),new Vector2(x,y-c),new Vector2(x,-y+c),new Vector2(x-c,-y),new Vector2(-x+c,-y),new Vector2(-x,-y+c),new Vector2(-x,y-c),new Vector2(-x+c,y) };
            float rim = Mathf.Min(bevel*.35f,(back-front)*.2f);
            float[] z = { back,back-rim,front+rim,front }; float[] scale = { .94f,1,1,.94f };
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var faces = new List<int>(); var edges = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e, List<int> triangles)
            {
                int k=vertices.Count; vertices.AddRange(new[] { a,b,d,e }); uv.AddRange(new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right });
                triangles.AddRange(new[] { k,k+1,k+2,k,k+2,k+3 });
            }
            for (int s=0;s<3;s++) for (int i=0;i<8;i++)
            {
                int j=(i+1)%8; Vector2 a=outline[i]*scale[s],b=outline[j]*scale[s],d=outline[j]*scale[s+1],e=outline[i]*scale[s+1];
                Quad(new Vector3(a.x,a.y,z[s]),new Vector3(b.x,b.y,z[s]),new Vector3(d.x,d.y,z[s+1]),new Vector3(e.x,e.y,z[s+1]),s!=1 || i%2==0 ? edges : faces);
            }
            for (int s=0;s<=3;s+=3) for (int i=0;i<8;i++)
            {
                int j=(i+1)%8; Vector2 a=outline[i]*scale[s],b=outline[j]*scale[s];
                int k=vertices.Count;
                vertices.Add(new Vector3(0,0,z[s]));
                vertices.Add(new Vector3(a.x,a.y,z[s])); vertices.Add(new Vector3(b.x,b.y,z[s]));
                uv.AddRange(new[] { new Vector2(.5f,.5f),new Vector2(a.x/size.x+.5f,a.y/size.y+.5f),new Vector2(b.x/size.x+.5f,b.y/size.y+.5f) });
                faces.AddRange(s==0 ? new[] { k,k+2,k+1 } : new[] { k,k+1,k+2 });
            }
            var mesh = new Mesh { subMeshCount=2 }; mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(faces,0); mesh.SetTriangles(edges,1);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); return Render(label,mesh,new[] { face,edge });
        }
        private void Bolt(Vector3 position)
        {
            Lathe("Recessed hex fastener", position, new[] { P(0,0),P(.013f,0),P(.016f,-.003f),P(.016f,-.012f),P(.012f,-.015f),P(0,-.015f) }, steel, 6);
            var slot = Housing("Hex fastener inset slot",new Vector2(.012f,.003f),-.0155f,-.016f,.0005f,graphite,graphite);
            slot.localPosition=position;
        }
        private void Tube(string label, Vector3[] p, float radius, Material material)
        {
            const int steps=24,sides=10; var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var indices=new List<int>();
            for (int i=0;i<=steps;i++)
            {
                float t=i/(float)steps,u=1-t;
                Vector3 centre=u*u*u*p[0]+3*u*u*t*p[1]+3*u*t*t*p[2]+t*t*t*p[3];
                Vector3 tangent=(3*u*u*(p[1]-p[0])+6*u*t*(p[2]-p[1])+3*t*t*(p[3]-p[2])).normalized;
                Vector3 a=Vector3.Cross(tangent,Vector3.right).normalized,b=Vector3.Cross(tangent,a);
                for (int j=0;j<=sides;j++)
                {
                    float phi=j*Mathf.PI*2/sides; vertices.Add(centre+(Mathf.Cos(phi)*a+Mathf.Sin(phi)*b)*radius);
                    uv.Add(new Vector2(j/(float)sides,t*3));
                    if(i==steps || j==sides) continue; int k=i*(sides+1)+j;
                    indices.AddRange(new[] { k,k+1,k+sides+2,k,k+sides+2,k+sides+1 });
                }
            }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            Render(label,mesh,new[] { material });
        }
        private void OnDestroy()
        {
            foreach(var mesh in meshes) if(mesh!=null) Destroy(mesh);
            foreach(var material in materials) if(material!=null) Destroy(material);
            foreach(var texture in textures) if(texture!=null) Destroy(texture);
        }
    }
}

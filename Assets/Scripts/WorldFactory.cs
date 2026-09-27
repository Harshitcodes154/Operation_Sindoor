using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sindoor {
    public static class WorldFactory {
        public static Material metal, dark, glass, sand, concrete, green, orange, white, glow, red;
        static readonly Dictionary<string,Material> palette = new Dictionary<string,Material>();
        public static void Initialize() {
            metal=Mat("Airframe / graphite",new Color(.29f,.34f,.35f),.55f);
            dark=Mat("Carbon",new Color(.025f,.04f,.048f),.25f);
            glass=Mat("Canopy",new Color(.08f,.2f,.23f),.85f);
            sand=Mat("Sandstone",new Color(.45f,.39f,.29f));
            concrete=Mat("Concrete",new Color(.32f,.34f,.33f));
            green=Mat("Flight suit",new Color(.16f,.21f,.15f));
            orange=Mat("Saffron",new Color(1,.39f,.12f));
            white=Mat("Ivory",new Color(.85f,.85f,.76f));
            glow=Mat("Runway lamps",new Color(.39f,.81f,.86f),0,3);
            red=Mat("Warning lamps",new Color(1,.13f,.06f),0,3);
        }
        public static Material Mat(string name, Color c, float metallic=0, float emission=0) {
            if(palette.TryGetValue(name,out var old)) return old;
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name; m.color=c;
            m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",metallic*.75f+.1f); m.SetFloat("_Cull",0); m.enableInstancing=true;
            if(emission>0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",c*emission); }
            palette[name]=m; return m;
        }
        public static GameObject Shape(string name, PrimitiveType kind, Transform parent, Vector3 pos, Vector3 scale, Material mat) {
            var g=GameObject.CreatePrimitive(kind); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat; if(Application.isPlaying)Object.Destroy(g.GetComponent<Collider>());else Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        public static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m) => Shape(n,PrimitiveType.Cube,p,pos,size,m);
        public static GameObject Ball(string n,Transform p,Vector3 pos,Vector3 size,Material m) => Shape(n,PrimitiveType.Sphere,p,pos,size,m);
        static GameObject MeshObject(string name,Transform parent,Vector3[] vertices,int[] indices,Material material) {
            var g=new GameObject(name);g.transform.SetParent(parent,false); var mesh=new Mesh{name=name}; mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;return g;
        }
        static void Fin(Transform parent,string name,Vector3[] outline,float thickness,Material material) {
            int n=outline.Length;var v=new Vector3[n*2];for(int i=0;i<n;i++){v[i]=outline[i]+Vector3.up*thickness;v[i+n]=outline[i]-Vector3.up*thickness;}
            var t=new List<int>();for(int i=1;i<n-1;i++){t.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});}
            for(int i=0;i<n;i++){int j=(i+1)%n;t.AddRange(new[]{i,i+n,j,j,j+n,i+n});}MeshObject(name,parent,v,t.ToArray(),material);
        }
        public static Transform Aircraft(Transform parent,string name,bool hostile=false) { return VisualAssetLibrary.InstantiateVisual(hostile?VisualAssetLibrary.Current.adversary:VisualAssetLibrary.Current.kestrel,parent,name); }
        static Transform LegacyAircraft(Transform parent,string name,bool hostile=false) {
            var root=new GameObject(name).transform;root.SetParent(parent,false);
            var skin=hostile?Mat("Adversary",new Color(.32f,.29f,.26f),.35f):metal;
            // Elliptical station loft; +Z is the nose. Original fictional Kestrel airframe.
            float[] zs={-7.5f,-6,-3,0,3,5,7.8f,9.8f}, ws={.85f,1.2f,1.55f,1.35f,1,.8f,.4f,.025f}, hs={.65f,.85f,.9f,.9f,.8f,.7f,.4f,.025f};
            var vs=new List<Vector3>();var ts=new List<int>();int ring=16;
            for(int s=0;s<zs.Length;s++) for(int r=0;r<ring;r++){float a=r*Mathf.PI*2/ring;vs.Add(new Vector3(Mathf.Cos(a)*ws[s],Mathf.Sin(a)*hs[s],zs[s]));}
            for(int s=0;s<zs.Length-1;s++)for(int r=0;r<ring;r++){int a=s*ring+r,b=s*ring+(r+1)%ring;ts.AddRange(new[]{a,b,a+ring,b,b+ring,a+ring});}
            MeshObject("Sculpted fuselage",root,vs.ToArray(),ts.ToArray(),skin);
            foreach(int side in new[]{-1,1}) {
                Fin(root,"Swept delta wing",new[]{new Vector3(side*.7f,0,2),new Vector3(side*7,0,-3.7f),new Vector3(side*7,0,-5),new Vector3(side*1.1f,0,-4)},.1f,skin);
                Fin(root,"Canard",new[]{new Vector3(side*.7f,.35f,4.5f),new Vector3(side*3,.35f,2.5f),new Vector3(side*1,.35f,2.2f)},.065f,skin);
                Box("Intake",root,new Vector3(side*1.1f,-.35f,1.3f),new Vector3(.8f,.9f,2.3f),dark);
                var tail=new GameObject("Canted vertical stabilizer").transform;tail.SetParent(root,false);tail.localPosition=new Vector3(side*.7f,.45f,-5.3f);tail.localRotation=Quaternion.Euler(0,0,side*72);
                Fin(tail,"Tail blade",new[]{new Vector3(0,0,2),new Vector3(side*2.8f,0,-.8f),new Vector3(side*2.8f,0,-2.2f),new Vector3(0,0,-1.6f)},.09f,skin);
                var nozzle=Shape("Exhaust nozzle",PrimitiveType.Cylinder,root,new Vector3(side*.63f,-.15f,-7.2f),new Vector3(1,.6f,1),dark);nozzle.transform.localRotation=Quaternion.Euler(90,0,0);
                var flame=Ball("Afterburner",root,new Vector3(side*.63f,-.15f,-8),new Vector3(.7f,.7f,2.4f),Mat("Exhaust light",new Color(.35f,.52f,1),0,3));
                Ball("Navigation light",root,new Vector3(side*6.9f,.13f,-4.5f),Vector3.one*.15f,side<0?red:glow);
                for(int w=0;w<2;w++) {
                    Box("Pylon",root,new Vector3(side*(2.7f+w*1.9f),-.4f,-1.8f),new Vector3(.14f,.7f,1.1f),dark);
                    Ball("Fictional missile",root,new Vector3(side*(2.7f+w*1.9f),-.8f,-1.1f),new Vector3(.25f,.25f,3.2f),white);
                }
                if(!hostile){ var r=Shape("Roundel",PrimitiveType.Cylinder,root,new Vector3(side*3.7f,.13f,-2.4f),new Vector3(1.1f,.012f,1.1f),orange);
                    Shape("Roundel white",PrimitiveType.Cylinder,root,r.transform.localPosition+Vector3.up*.03f,new Vector3(.73f,.013f,.73f),white);
                    Shape("Roundel green",PrimitiveType.Cylinder,root,r.transform.localPosition+Vector3.up*.06f,new Vector3(.4f,.014f,.4f),green); }
            }
            Ball("Tinted canopy",root,new Vector3(0,1.05f,3),new Vector3(1.5f,1.35f,3.8f),glass);
            Box("Canopy spine",root,new Vector3(0,1.63f,2.8f),new Vector3(.07f,.1f,3.4f),dark);
            var gear=new GameObject("Landing gear").transform;gear.SetParent(root,false);
            for(int i=0;i<3;i++){float x=i==0?0:i==1?-1.5f:1.5f,z=i==0?5:-2;Box("Strut",gear,new Vector3(x,-1.6f,z),new Vector3(.13f,1.9f,.13f),metal);var wheel=Shape("Wheel",PrimitiveType.Cylinder,gear,new Vector3(x,-2.55f,z),new Vector3(.8f,.18f,.8f),dark);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}
            return root;
        }
        public static Transform Cockpit(Camera camera) { return VisualAssetLibrary.InstantiateVisual(VisualAssetLibrary.Current.cockpit,camera.transform,"Cockpit interior"); }
        static Transform LegacyCockpit(Camera camera) {
            var root=new GameObject("Cockpit interior").transform;root.SetParent(camera.transform,false);
            Box("Instrument coaming",root,new Vector3(0,-.64f,1.2f),new Vector3(2.8f,.45f,1.1f),dark);
            for(int i=-1;i<=1;i++) { Box("MFD bezel",root,new Vector3(i*.66f,-.47f,.75f),new Vector3(.52f,.33f,.12f),metal);
                Box("MFD glass",root,new Vector3(i*.66f,-.46f,.679f),new Vector3(.46f,.265f,.01f),Mat("MFD emission",new Color(.035f,.19f,.16f),0,.6f)); }
            for(int s=-1;s<=1;s+=2){ var rail=Box("Canopy rail",root,new Vector3(s*1.12f,-.15f,1.2f),new Vector3(.055f,.055f,3.5f),dark);rail.transform.localRotation=Quaternion.Euler(-14,s*7,0); }
            Box("HUD projector",root,new Vector3(0,-.36f,.9f),new Vector3(.35f,.08f,.3f),metal);
            return root;
        }
        public static Transform Pilot(Transform parent,Vector3 pos,string name,bool officer=false) { var p=VisualAssetLibrary.InstantiateVisual(officer?VisualAssetLibrary.Current.officer:VisualAssetLibrary.Current.pilot,parent,name);p.localPosition=pos;return p; }
        static Transform LegacyPilot(Transform parent,Vector3 pos,string name,bool officer=false) {
            var p=new GameObject(name).transform;p.SetParent(parent,false);p.localPosition=pos;
            var suit=officer?Mat("Officer uniform",new Color(.19f,.28f,.34f)):green;
            Ball("Torso",p,new Vector3(0,1.13f,0),new Vector3(.55f,.72f,.32f),suit);
            Ball("Helmet",p,new Vector3(0,1.72f,0),new Vector3(.4f,.44f,.4f),officer?Mat("Skin",new Color(.5f,.32f,.2f)):white);
            if(officer){Ball("Officer cap",p,new Vector3(0,1.91f,0),new Vector3(.44f,.15f,.45f),suit);Box("Cap peak",p,new Vector3(0,1.86f,.22f),new Vector3(.35f,.03f,.19f),dark);Ball("Nose",p,new Vector3(0,1.71f,.205f),new Vector3(.08f,.1f,.07f),Mat("Skin",new Color(.5f,.32f,.2f)));for(int s=-1;s<=1;s+=2)Ball("Eye",p,new Vector3(s*.08f,1.77f,.19f),new Vector3(.035f,.025f,.015f),dark);}
            if(!officer){Ball("Visor",p,new Vector3(0,1.76f,.15f),new Vector3(.36f,.19f,.17f),glass);Ball("Oxygen mask",p,new Vector3(0,1.61f,.2f),new Vector3(.23f,.17f,.15f),dark);}
            for(int s=-1;s<=1;s+=2){var leg=Shape(s<0?"Leg L":"Leg R",PrimitiveType.Capsule,p,new Vector3(s*.15f,.5f,0),new Vector3(.23f,.48f,.24f),suit);
                Box("Boot",p,new Vector3(s*.15f,.08f,.06f),new Vector3(.24f,.18f,.4f),dark);
                Shape(s<0?"Arm L":"Arm R",PrimitiveType.Capsule,p,new Vector3(s*.36f,1.13f,0),new Vector3(.18f,.35f,.2f),suit);
                Ball("Glove",p,new Vector3(s*.36f,.81f,0),Vector3.one*.18f,dark);
                Box("Harness",p,new Vector3(s*.13f,1.16f,.16f),new Vector3(.065f,.55f,.045f),dark);
                Box("Rank",p,new Vector3(s*.23f,1.42f,.07f),new Vector3(.12f,.03f,.15f),white);
            }return p;
        }
        public static TextMesh Text(Transform parent,string words,Vector3 pos,float size,Color color,Vector3 euler=default) {
            var g=new GameObject(words).transform;g.SetParent(parent,false);g.localPosition=pos;g.localEulerAngles=euler;
            var t=g.gameObject.AddComponent<TextMesh>();t.text=words;t.fontSize=64;t.characterSize=size*.1f;t.anchor=TextAnchor.MiddleCenter;t.color=color;return t;
        }
        public static float Height(float x,float z) {
            float flat=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(500,1700,Mathf.Abs(x)));
            float baseZone=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1500,3000,Mathf.Abs(z+4500)));
            float h=35+Mathf.PerlinNoise(x*.00023f+14,z*.00023f+12)*600+Mathf.PerlinNoise(x*.0006f+8,z*.0006f+4)*180;
            return h*(1-flat*.97f)*(1-baseZone*.85f);
        }
        public static Transform Build() {
            var root=new GameObject("Ashva theatre / original procedural art").transform;
            VisualEnvironment.Terrain(root);BuildBase(root);BuildBriefing(root);BuildFlag(root,new Vector3(100,0,-4700));VisualEnvironment.UpgradeBase(root);VisualEnvironment.DressTerrain(root);root.gameObject.AddComponent<AtmospherePresentation>();
            var rng=new System.Random(71);
            var cloudMaterial=CloudMaterial();
            for(int i=0;i<40;i++){float x=(float)(rng.NextDouble()-.5)*23000,z=(float)(rng.NextDouble()-.5)*23000;
                var cloud=Shape("Soft cloud bank",PrimitiveType.Quad,root,new Vector3(x,2200+(float)rng.NextDouble()*900,z),new Vector3(1400+(float)rng.NextDouble()*1000,550,1),cloudMaterial);cloud.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;cloud.gameObject.AddComponent<CloudBillboard>();}
            var water=Box("Distant water",root,new Vector3(0,-14,17000),new Vector3(80000,5,16000),Mat("Ocean",new Color(.15f,.27f,.3f),.65f));
            for(int i=0;i<20;i++){float x=1800+(float)rng.NextDouble()*800,z=500+(float)rng.NextDouble()*1000;Box("Protected civilian settlement",root,new Vector3(x,Height(x,z)+8,z),new Vector3(22,16,22),sand);}
            VisualEnvironment.DetailStructures(root);
            return root;
        }
        static Material CloudMaterial(){
            var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);tex.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<128;y++)for(int x=0;x<128;x++){float u=x/127f,v=y/127f;float a=0;for(int c=0;c<5;c++){float cx=.22f+c*.14f,cy=.45f+Mathf.Sin(c*2)*.12f;float d=Mathf.Pow((u-cx)/(.18f+c*.01f),2)+Mathf.Pow((v-cy)/(.26f+c*.015f),2);a=Mathf.Max(a,Mathf.Clamp01(1-d));}a=Mathf.SmoothStep(0,1,a)*(.65f+.35f*Mathf.PerlinNoise(u*12,v*12))*.7f;tex.SetPixel(x,y,new Color(.88f,.92f,.94f,a));}tex.Apply();
            var m=new Material(Shader.Find("Sindoor/SoftCloud"));m.name="Soft procedural cloud";m.SetTexture("_BaseMap",tex);return m;
        }
        static void BuildTerrain(Transform root) {
            const int n=161;var v=new Vector3[n*n];var uv=new Vector2[v.Length];var tris=new List<int>[4];for(int m=0;m<4;m++)tris[m]=new List<int>();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){int i=z*n+x;float wx=(x-(n-1)/2f)*220,wz=(z-(n-1)/2f)*220;v[i]=new Vector3(wx,Height(wx,wz)-2,wz);uv[i]=new Vector2(x*.2f,z*.2f);}
            for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int i=z*n+x;int m=v[i].y>450?3:v[i].y>260?2:v[i].y>70?1:0;tris[m].AddRange(new[]{i,i+n,i+1,i+1,i+n,i+n+1});}
            var colors=new Color[v.Length];for(int i=0;i<v.Length;i++){float noise=Mathf.PerlinNoise(v[i].x*.002f+21,v[i].z*.002f+30);Color low=Color.Lerp(new Color(.21f,.26f,.18f),new Color(.36f,.31f,.23f),noise);colors[i]=Color.Lerp(low,new Color(.5f,.46f,.38f),Mathf.InverseLerp(150,650,v[i].y));}
            var mesh=new Mesh{name="Theatre terrain"};mesh.vertices=v;mesh.uv=uv;mesh.colors=colors;var all=new List<int>();for(int m=0;m<4;m++)all.AddRange(tris[m]);mesh.triangles=all.ToArray();mesh.RecalculateNormals();
            var g=new GameObject("Valleys and ridgelines");g.transform.SetParent(root);g.AddComponent<MeshFilter>().sharedMesh=mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("Sindoor/TheatreTerrain"));
        }
        static void BuildBase(Transform root) {
            Box("Airfield apron",root,new Vector3(0,0,-4500),new Vector3(900,1,3000),concrete);
            Box("Runway 00",root,new Vector3(0,1,-4500),new Vector3(90,1,2400),dark);
            Box("Taxiway",root,new Vector3(180,1,-4700),new Vector3(45,1,1900),dark);
            for(int z=-5630;z<-3400;z+=95){Box("Centreline",root,new Vector3(0,1.55f,z),new Vector3(2,.04f,38),white);
                for(int s=-1;s<=1;s+=2){Ball("Runway edge",root,new Vector3(s*48,2,z),new Vector3(1.4f,.5f,1.4f),glow);}}
            for(int i=-4;i<=4;i++){Box("Threshold stripes",root,new Vector3(i*8,1.6f,-5580),new Vector3(4,.04f,65),white);}
            for(int z=-6400;z<-5700;z+=70){for(int i=-2;i<=2;i++)Ball("Approach light",root,new Vector3(i*8,2,z),Vector3.one*2,orange);}
            Text(root,"00",new Vector3(0,1.6f,-5490),18,Color.white,new Vector3(90,0,0));
            for(int i=0;i<4;i++){
                float z=-4900+i*290;Box("Hangar slab",root,new Vector3(300,1,z),new Vector3(170,2,180),concrete);
                Box("Hangar back",root,new Vector3(375,31,z),new Vector3(5,60,165),metal);
                for(int s=-1;s<=1;s+=2)Box("Hangar wall",root,new Vector3(300,31,z+s*82),new Vector3(150,60,4),metal);
                Box("Hangar roof",root,new Vector3(300,63,z),new Vector3(160,5,170),dark);
                Box("Hangar light",root,new Vector3(280,58,z),new Vector3(65,.2f,2),glow);
                Text(root,"ASHVA  /  "+(i+1).ToString("00"),new Vector3(221,48,z),2.8f,Color.white,new Vector3(0,-90,0));
                var jet=Aircraft(root,"Parked Kestrel");jet.position=new Vector3(255,3,z);jet.rotation=Quaternion.Euler(0,-90,0);
                for(int c=0;c<4;c++){var p=Pilot(root,new Vector3(250+c*3,1,z+13),"Ground crew");p.localScale=Vector3.one*1.2f;}
                var truck=Box("Service truck",root,new Vector3(200,4,z+65),new Vector3(5,5,11),green);
                Box("Truck cab",root,new Vector3(200,5,z+59),new Vector3(5,6,4),metal);
                for(int s=-1;s<=1;s+=2)for(int w=-1;w<=1;w+=2)Ball("Wheel",root,new Vector3(200+s*2.5f,2,z+65+w*4),new Vector3(1.1f,2.2f,2.2f),dark);
            }
            Box("Control tower",root,new Vector3(-170,32,-4400),new Vector3(22,64,22),concrete);
            Box("Tower control room",root,new Vector3(-170,68,-4400),new Vector3(40,12,35),glass);
            Box("Tower roof",root,new Vector3(-170,76,-4400),new Vector3(44,3,39),dark);
            var radar=new GameObject("Rotating radar").transform;radar.SetParent(root);radar.position=new Vector3(-250,22,-4800);
            Box("Radar mast",root,new Vector3(-250,11,-4800),new Vector3(3,22,3),metal);
            Box("Radar panel",radar,Vector3.zero,new Vector3(18,8,1),green);radar.gameObject.AddComponent<RadarMotion>();
            Box("Barracks",root,new Vector3(-270,12,-4600),new Vector3(100,24,60),sand);
            for(int i=0;i<9;i++)Box("Lit windows",root,new Vector3(-312+i*10,15,-4569),new Vector3(4,5,.2f),glow);
            BuildFlag(root,new Vector3(-90,0,-4600));
        }
        public static readonly Vector3 Room=new Vector3(450,2,-5300);
        static void BuildBriefing(Transform root) {
            var room=new GameObject("Briefing room").transform;room.SetParent(root);room.position=Room;
            Box("Floor",room,new Vector3(0,-.1f,0),new Vector3(20,.2f,24),dark);
            Box("Back wall",room,new Vector3(0,3,8),new Vector3(20,6,.2f),metal);
            Box("Sidewall",room,new Vector3(-10,3,0),new Vector3(.2f,6,24),metal);
            Box("Right wall",room,new Vector3(10,3,0),new Vector3(.2f,6,24),metal);Box("Ceiling",room,new Vector3(0,6,0),new Vector3(20,.2f,24),dark);Box("Front wall",room,new Vector3(0,3,-12),new Vector3(20,6,.2f),metal);
            for(int i=-1;i<=1;i++)Box("Ceiling light strip",room,new Vector3(i*5,5.85f,1),new Vector3(.2f,.1f,15),glow);
            Box("Map display",room,new Vector3(0,3.2f,7.8f),new Vector3(10,4,.1f),Mat("Map screen",new Color(.028f,.13f,.17f),0,.6f));
            for(int i=-4;i<=4;i++){Box("Map grid",room,new Vector3(i,3.2f,7.72f),new Vector3(.012f,3.6f,.015f),glow);}
            for(int i=0;i<4;i++)Box("Map grid",room,new Vector3(0,1.7f+i,7.72f),new Vector3(9,.015f,.015f),glow);
            Text(room,"ASHVA  /  SECTOR BRIEFING",new Vector3(0,5.45f,7.65f),.5f,Color.white);
            Text(room,"KESAR CORRIDOR\n\n+     VANA RIDGE     +\n\nFICTIONAL THEATRE  /  0500",new Vector3(0,3.2f,7.6f),.42f,new Color(.6f,1,.85f));
            Box("Table",room,new Vector3(0,.9f,0),new Vector3(5,.15f,7),sand);
            for(int i=0;i<4;i++) { Box("Chair",room,new Vector3(-3,.5f,-2+i*1.7f),new Vector3(.6f,1,.6f),dark);Pilot(room,new Vector3(-3,.1f,-2+i*1.7f),"Briefing officer",true); }
            var light=new GameObject("Briefing light").AddComponent<Light>();light.transform.SetParent(room);light.transform.localPosition=new Vector3(0,5,2);light.type=LightType.Point;light.range=20;light.intensity=5;light.color=new Color(.65f,.8f,1);light.shadows=LightShadows.None;
        }
        public static void BuildFlag(Transform root,Vector3 pos) {
            Shape("Flagpole",PrimitiveType.Cylinder,root,pos+Vector3.up*12,new Vector3(.15f,12,.15f),metal);
            var tex=new Texture2D(192,128);var pixels=new Color[192*128];
            for(int y=0;y<128;y++)for(int x=0;x<192;x++) {Color c=y>85?new Color(1,.48f,.15f):y<43?new Color(.06f,.42f,.23f):Color.white;
                float dx=x-96,dy=y-64,r=Mathf.Sqrt(dx*dx+dy*dy);float a=Mathf.Atan2(dy,dx);
                if((r>15&&r<17)||(r<16&&Mathf.Abs(Mathf.Sin(a*12))*r<.7f)||r<2)c=new Color(.02f,.08f,.33f);pixels[y*192+x]=c;}
            tex.SetPixels(pixels);tex.Apply();var mat=Mat("Indian flag",Color.white);mat.mainTexture=tex;mat.SetFloat("_Cull",0);
            var v=new Vector3[34];var uv=new Vector2[34];var t=new List<int>();for(int i=0;i<17;i++){float x=i/16f;v[i*2]=new Vector3(x*8,0,0);v[i*2+1]=new Vector3(x*8,-5,0);uv[i*2]=new Vector2(x,1);uv[i*2+1]=new Vector2(x,0);if(i<16){int a=i*2;t.AddRange(new[]{a,a+1,a+2,a+2,a+1,a+3});}}
            var f=MeshObject("Tricolour",root,v,t.ToArray(),mat);f.GetComponent<MeshFilter>().sharedMesh.uv=uv;f.transform.position=pos+Vector3.up*23;f.AddComponent<FlagMotion>();
        }
    }
    public class RadarMotion:MonoBehaviour{void Update(){transform.Rotate(0,25*Time.deltaTime,0);}}
    public class CloudBillboard:MonoBehaviour{void LateUpdate(){if(OperationGame.Instance&&OperationGame.Instance.cam)transform.rotation=Quaternion.LookRotation(transform.position-OperationGame.Instance.cam.transform.position);}}
    public class FlagMotion:MonoBehaviour{
        Mesh mesh;Vector3[] original,vertices;
        void Start(){mesh=GetComponent<MeshFilter>().mesh;original=mesh.vertices;vertices=(Vector3[])original.Clone();}
        void Update(){for(int i=0;i<vertices.Length;i++){vertices[i]=original[i];vertices[i].z=Mathf.Sin(original[i].x*.8f-Time.time*2)*original[i].x*.075f;}mesh.vertices=vertices;mesh.RecalculateNormals();}
    }
}

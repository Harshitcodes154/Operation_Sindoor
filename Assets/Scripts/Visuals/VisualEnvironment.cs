using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sindoor {
    public static class VisualEnvironment {
        static Material terrainMaterial;
        // Render-surface contact only; this never changes the gameplay terrain/collision function.
        public static float SurfaceHeight(Vector3 p){
            if(Mathf.Abs(p.x-450)<10&&Mathf.Abs(p.z+5300)<12)return 1.9f;
            if(p.x>225&&p.x<375&&p.z>-4983&&p.z<-3947)return 1.3f;
            if(Mathf.Abs(p.x)<45&&p.z>-5700&&p.z<-3300||Mathf.Abs(p.x-180)<22.5f&&p.z>-5650&&p.z<-3750)return 1.5f;
            if(Mathf.Abs(p.x)<450&&p.z>-6000&&p.z<-3000)return .5f;
            return WorldFactory.Height(p.x,p.z)-2;
        }
        public static void DetailStructures(Transform root){
            var a=VisualAssetLibrary.Current;var visibility=root.GetComponent<SceneryVisibility>();var items=new List<Transform>();foreach(Transform t in root)items.Add(t);
            foreach(var t in items){
                GameObject prefab=null;Vector3 offset=Vector3.zero;
                if(t.name=="Control tower"){prefab=a.controlTower;offset=Vector3.down*32;}
                if(t.name=="Barracks"){prefab=a.barracks;offset=Vector3.down*12;}
                if(t.name=="Protected civilian settlement"){prefab=a.building;offset=Vector3.down*8;}
                if(prefab){t.GetComponent<Renderer>().enabled=false;var replacement=VisualAssetLibrary.InstantiateVisual(prefab,root,"Detailed "+t.name);replacement.position=t.position+offset;visibility.Register(replacement);}
                else if(t.name=="Tower control room"||t.name=="Tower roof"||t.name=="Lit windows")t.GetComponent<Renderer>().enabled=false;
            }
            var radar=root.Find("Rotating radar");if(radar){
                foreach(var r in radar.GetComponentsInChildren<Renderer>())r.enabled=false;var b=new VisualBatch();
                b[a.alloy].Box(Vector3.zero,new Vector3(18.5f,8.5f,.8f));
                for(int y=-3;y<=3;y++)for(int x=-8;x<=8;x++)b[a.rubber].Box(new Vector3(x,y,.5f),new Vector3(.87f,.8f,.18f));
                b[a.alloy].Tube(new Vector3(-7,-3,-1),new Vector3(0,-4,-3),.14f,.14f,12);b[a.alloy].Tube(new Vector3(7,-3,-1),new Vector3(0,-4,-3),.14f,.14f,12);b.Attach(radar,"Radar visual array");
            }
            var room=root.Find("Briefing room");if(room){
                var b=new VisualBatch();for(int x=-9;x<10;x+=2){b[a.fabric].Box(new Vector3(x,3,7.85f),new Vector3(1.94f,5.6f,.1f));}
                for(int i=0;i<4;i++){
                    float z=-2+i*1.7f;b[a.fabric].Box(new Vector3(-3,.62f,z),new Vector3(.68f,.13f,.66f));b[a.fabric].Box(new Vector3(-3,1.02f,z-.28f),new Vector3(.68f,.8f,.12f));
                    foreach(int s in new[]{-1,1})b[a.alloy].Tube(new Vector3(-3+s*.25f,.57f,z-.21f),new Vector3(-3+s*.25f,.02f,z-.21f),.022f,.022f,8);
                }
                b.Attach(room,"Briefing interior details");
                var floor=room.Find("Floor");if(floor){var material=new Material(a.concrete);material.SetTextureScale("_BaseMap",new Vector2(5,6));floor.GetComponent<Renderer>().sharedMaterial=material;}
            }
            root.gameObject.AddComponent<VisualContactPresentation>();
        }
        public static void Terrain(Transform parent){
            var a=VisualAssetLibrary.Current;terrainMaterial=new Material(Shader.Find("Sindoor/TheatreTerrain")){name="Layered theatre ground"};
            terrainMaterial.SetTexture("_Rock",a.soil.GetTexture("_BaseMap"));terrainMaterial.SetTexture("_Grass",a.foliage.GetTexture("_BaseMap"));terrainMaterial.SetTexture("_Normal",a.soil.GetTexture("_BumpMap"));
            const int tiles=16;const float size=2200;
            for(int z=0;z<tiles;z++)for(int x=0;x<tiles;x++){
                var tile=new GameObject("Terrain tile "+x+" / "+z).transform;tile.SetParent(parent,false);tile.position=new Vector3((x-8)*size,0,(z-8)*size);var lods=new List<LOD>();
                for(int level=0;level<3;level++){
                    int n=level==0?33:level==1?17:9;var mesh=GroundMesh(tile.position,size,n);var g=new GameObject("Terrain LOD"+level);g.transform.SetParent(tile,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=terrainMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
                    lods.Add(new LOD(level==0?.19f:level==1?.07f:.001f,new Renderer[]{renderer}));
                }
                var lod=tile.gameObject.AddComponent<LODGroup>();lod.SetLODs(lods.ToArray());lod.RecalculateBounds();
            }
        }
        static Mesh GroundMesh(Vector3 origin,float size,int n){
            var v=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float xx=x*size/(n-1),zz=z*size/(n-1),wx=origin.x+xx,wz=origin.z+zz;v.Add(new Vector3(xx,WorldFactory.Height(wx,wz)-2,zz));
                normals.Add(new Vector3(WorldFactory.Height(wx-2,wz)-WorldFactory.Height(wx+2,wz),4,WorldFactory.Height(wx,wz-2)-WorldFactory.Height(wx,wz+2)).normalized);uv.Add(new Vector2(wx*.015f,wz*.015f));
            }
            for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int i=z*n+x;triangles.AddRange(new[]{i,i+n,i+1,i+1,i+n,i+n+1});}
            // Skirts hide cracks where adjacent tiles select different LOD levels.
            for(int edge=0;edge<4;edge++)for(int i=0;i<n-1;i++){
                int a=edge==0?i:edge==1?i*n+n-1:edge==2?(n-1)*n+(n-1-i):(n-1-i)*n;
                int b=edge==0?a+1:edge==1?a+n:edge==2?a-1:a-n;int k=v.Count;
                v.Add(v[a]);v.Add(v[b]);v.Add(v[a]-Vector3.up*45);v.Add(v[b]-Vector3.up*45);for(int j=0;j<4;j++){uv.Add(Vector2.zero);normals.Add(Vector3.up);}triangles.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});
            }
            var mesh=new Mesh{name="Terrain / "+n};mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        public static void UpgradeBase(Transform root){
            var a=VisualAssetLibrary.Current;var remove=new List<GameObject>();
            foreach(Transform t in root){if(t.name.StartsWith("Hangar ")||t.name.StartsWith("ASHVA  /  ")||t.name=="Service truck"||t.name=="Truck cab"||t.name=="Wheel")remove.Add(t.gameObject);}
            foreach(var obj in remove){if(Application.isPlaying)Object.Destroy(obj);else Object.DestroyImmediate(obj);}
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
                if(renderer.sharedMaterial==WorldFactory.concrete)renderer.sharedMaterial=a.concrete;
                if(renderer.name=="Runway 00"||renderer.name=="Taxiway")renderer.sharedMaterial=a.asphalt;
                if(renderer.name=="Airfield apron"||renderer.name=="Runway 00"||renderer.name=="Taxiway"){
                    var material=new Material(renderer.sharedMaterial);material.name=renderer.name+" / physical surface scale";Vector3 scale=renderer.transform.localScale;material.SetTextureScale("_BaseMap",new Vector2(scale.x/8,scale.z/8));renderer.sharedMaterial=material;
                }
            }
            for(int i=0;i<4;i++){
                var hangar=VisualAssetLibrary.InstantiateVisual(a.hangar,root,"Maintenance hangar "+(i+1));hangar.position=new Vector3(300,1,-4900+i*290);
                var truck=VisualAssetLibrary.InstantiateVisual(a.truck,root,"Utility vehicle "+(i+1));truck.position=new Vector3(200,1.5f,-4835+i*290);truck.rotation=Quaternion.Euler(0,90,0);
                WorldFactory.Text(root,"ASHVA / H"+(i+1).ToString("00"),new Vector3(224,26,-4900+i*290),1.3f,new Color(.85f,.87f,.8f),new Vector3(0,90,0));
            }
            var b=new VisualBatch();
            // Taxi centreline, hold-short bars, drainage, tire marks and edge fixtures stay on the existing airfield.
            for(int z=-5600;z<-3900;z+=30){b[WorldFactory.orange].Box(new Vector3(180,1.54f,z),new Vector3(.22f,.012f,21));}
            for(int i=0;i<18;i++){
                float z=-5410+i*17;b[WorldFactory.dark].Box(new Vector3(-5+i%3*5,1.568f,z),new Vector3(.36f,.006f,Mathf.Lerp(9,23,(i%5)/4f)));
            }
            for(int z=-5500;z<-3400;z+=160)foreach(int s in new[]{-1,1}){b[a.alloy].Box(new Vector3(s*63,1,z),new Vector3(.8f,.2f,5));}
            // Perimeter, blast barriers, fueling area and service facilities.
            for(int z=-5700;z<-3200;z+=70){b[a.alloy].Tube(new Vector3(-420,1,z),new Vector3(-420,4,z),.07f,.05f,8);b[a.alloy].Tube(new Vector3(-420,3.5f,z),new Vector3(-420,3.5f,z+70),.025f,.025f,6);}
            for(int i=0;i<12;i++){b[a.concrete].Box(new Vector3(-345+i*6,2.5f,-5200),new Vector3(5,3,1.5f));}
            for(int i=0;i<3;i++){
                Vector3 p=new Vector3(-290+i*40,4,-5050);b[a.alloy].Tube(p+Vector3.back*12,p+Vector3.forward*12,4,4,32);
                b[a.rubber].Tube(p+new Vector3(0,-2,12),p+new Vector3(0,-2,25),.13f,.13f,10);b[a.concrete].Box(p+new Vector3(0,-3,0),new Vector3(11,1,28));
            }
            for(int i=0;i<6;i++){
                float z=-5300+i*320;b[a.alloy].Tube(new Vector3(95,1,z),new Vector3(95,16,z),.12f,.06f,12);b[WorldFactory.glow].Box(new Vector3(95,16,z),new Vector3(1.8f,.16f,.9f));
                var lamp=new GameObject("Apron floodlight").AddComponent<Light>();lamp.transform.SetParent(root,false);lamp.transform.localPosition=new Vector3(95,15.5f,z);lamp.type=LightType.Spot;lamp.transform.rotation=Quaternion.Euler(75,90,0);lamp.spotAngle=105;lamp.range=75;lamp.intensity=8;lamp.color=new Color(1,.85f,.64f);lamp.shadows=LightShadows.None;
            }
            b.Attach(root,"Airbase infrastructure");
            WorldFactory.Text(root,"ASHVA AIR STATION\nAUTHORIZED PERSONNEL",new Vector3(-205,4,-4600),.65f,Color.white);
            // Communications mast, bracing and dish reflectors.
            var mast=new VisualBatch();Vector3 centre=new Vector3(-295,0,-4780);
            foreach(int s in new[]{-1,1})mast[a.alloy].Tube(centre+new Vector3(s*4,1,0),centre+new Vector3(s*.7f,54,0),.18f,.08f,10);
            for(int j=0;j<9;j++){float y=j*6;mast[a.alloy].Tube(centre+new Vector3(-4+y*.06f,y,0),centre+new Vector3(4-(y+6)*.06f,y+6,0),.09f,.09f,8);}
            mast[WorldFactory.red].Ellipsoid(centre+Vector3.up*55,Vector3.one*.35f,10,6);mast.Attach(root,"Communications tower");
            // Local static geometry batches retain Unity frustum and shadow culling.
        }
        public static void DressTerrain(Transform root){
            var a=VisualAssetLibrary.Current;var random=new System.Random(1947);var visibility=root.gameObject.AddComponent<SceneryVisibility>();
            foreach(var lod in root.GetComponentsInChildren<LODGroup>())if(lod.name.StartsWith("Maintenance hangar")||lod.name.StartsWith("Utility vehicle"))visibility.Register(lod.transform);
            for(int i=0;i<180;i++){
                float x=(float)(random.NextDouble()-.5)*14000,z=(float)(random.NextDouble()-.5)*17000;
                if(Mathf.Abs(x)<500||Mathf.Abs(x)<700&&z<-2700)continue;
                bool tree=i%3!=0;var p=VisualAssetLibrary.InstantiateVisual(tree?a.tree:a.rock,root,tree?"Native tree cluster":"Exposed rock");p.position=new Vector3(x,WorldFactory.Height(x,z)-2,z);p.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);p.localScale=Vector3.one*(tree?1.1f+(float)random.NextDouble()*1.3f:2+(float)random.NextDouble()*4);visibility.Register(p);
            }
            // A fictional service road follows the same analytic terrain used by gameplay.
            var road=new VisualBatch();
            for(int i=0;i<85;i++){
                float z=-6400+i*90,x=-850+Mathf.Sin(i*.12f)*120,zz=z+90,xx=-850+Mathf.Sin((i+1)*.12f)*120;
                float y=WorldFactory.Height(x,z)-1.6f,yy=WorldFactory.Height(xx,zz)-1.6f;
                road[a.asphalt].Quad(new Vector3(x-7,y,z),new Vector3(xx-7,yy,zz),new Vector3(xx+7,yy,zz),new Vector3(x+7,y,z),new Vector2(1,8));
            }
            road.Attach(root,"Service road");
            var bridge=new VisualBatch();bridge[a.concrete].Box(new Vector3(-850,WorldFactory.Height(-850,500)+1,500),new Vector3(17,2,85));foreach(int s in new[]{-1,1})bridge[a.alloy].Box(new Vector3(-850+s*8,WorldFactory.Height(-850,500)+2.5f,500),new Vector3(.2f,1,85));bridge.Attach(root,"Service bridge");
        }
    }
}

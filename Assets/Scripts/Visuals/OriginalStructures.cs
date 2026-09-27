using System.Collections.Generic;
using UnityEngine;

namespace Sindoor {
    public static class OriginalStructures {
        public static Transform Build(string kind,VisualAssetLibrary a){
            var root=new GameObject(kind).transform;var lods=new List<LOD>();
            for(int level=0;level<3;level++){
                var group=new GameObject("Visual Model / LOD"+level).transform;group.SetParent(root,false);var b=new VisualBatch();
                if(kind=="Control tower")Tower(b,a,level);else if(kind=="Relay")Relay(b,a,level);else Building(b,a,level,kind=="Barracks");
                lods.Add(new LOD(level==0?.1f:level==1?.025f:.002f,b.Attach(group,kind)));
            }
            var lod=root.gameObject.AddComponent<LODGroup>();lod.SetLODs(lods.ToArray());lod.RecalculateBounds();return root;
        }
        static void Tower(VisualBatch b,VisualAssetLibrary a,int level){
            b[a.concrete].Box(new Vector3(0,32,0),new Vector3(22,64,22));b[a.concrete].Box(new Vector3(0,62,0),new Vector3(39,2,34));
            b[a.rubber].Box(new Vector3(0,75,0),new Vector3(44,1.6f,39));
            foreach(int s in new[]{-1,1}){
                b[a.canopy].Box(new Vector3(s*19,68.5f,0),new Vector3(.14f,11,34));b[a.canopy].Box(new Vector3(0,68.5f,s*17),new Vector3(38,11,.14f));
                if(level<2){for(int x=-18;x<=18;x+=6)b[a.alloy].Box(new Vector3(x,68.5f,s*17.1f),new Vector3(.25f,11,.3f));for(int z=-12;z<=12;z+=6)b[a.alloy].Box(new Vector3(s*19.1f,68.5f,z),new Vector3(.3f,11,.25f));}
                if(level==0){for(int y=9;y<58;y+=9){b[a.canopy].Box(new Vector3(s*11.05f,y,0),new Vector3(.08f,3,2));b[a.concrete].Box(new Vector3(0,y-2,0),new Vector3(22.4f,.25f,22.4f));}
                    b[a.alloy].Tube(new Vector3(s*21,62,17.7f),new Vector3(s*21,64,17.7f),.09f,.09f,8);b[a.alloy].Tube(new Vector3(-21,64,s*17.7f),new Vector3(21,64,s*17.7f),.07f,.07f,8);
                }
            }
            if(level==0){b[a.rubber].Box(new Vector3(0,2,11.05f),new Vector3(2.5f,4,.12f));b[a.alloy].Tube(new Vector3(0,76,0),new Vector3(0,89,0),.14f,.05f,12);b[WorldFactory.red].Ellipsoid(new Vector3(0,89,0),Vector3.one*.2f,12,8);b[a.alloy].Box(new Vector3(8,76,4),new Vector3(5,1,3));}
        }
        static void Building(VisualBatch b,VisualAssetLibrary a,int level,bool barracks){
            float width=barracks?100:22,depth=barracks?60:22,height=barracks?24:16;
            b[a.concrete].Box(new Vector3(0,height/2,0),new Vector3(width,height,depth));b[a.rubber].Box(new Vector3(0,height+.12f,0),new Vector3(width+.8f,.35f,depth+.8f));
            if(level==2)return;
            foreach(int s in new[]{-1,1})for(int floor=0;floor<2;floor++)for(float x=-width/2+4;x<width/2-2;x+=barracks?8:6){
                b[a.canopy].Box(new Vector3(x,4+floor*(height/2),s*(depth/2+.03f)),new Vector3(2.5f,3.5f,.06f));
                if(level==0){b[a.alloy].Box(new Vector3(x,4+floor*(height/2),s*(depth/2+.08f)),new Vector3(.1f,3.6f,.09f));b[a.concrete].Box(new Vector3(x,2.2f+floor*(height/2),s*(depth/2+.2f)),new Vector3(3,.15f,.55f));}
            }
            if(level==0){
                b[a.rubber].Box(new Vector3(0,2.1f,depth/2+.08f),new Vector3(2.4f,4.2f,.1f));b[a.cladding].Box(new Vector3(0,4.5f,depth/2+1),new Vector3(5,.14f,2.4f));
                for(int i=0;i<(barracks?6:2);i++){b[a.alloy].Box(new Vector3(-width*.3f+i*(barracks?12:6),height+.65f,0),new Vector3(3,1,2));b[a.alloy].Tube(new Vector3(-width*.3f+i*(barracks?12:6),height+1,0),new Vector3(-width*.3f+i*(barracks?12:6),height+2,0),.3f,.3f,12);}
                for(int step=0;step<3;step++)b[a.concrete].Box(new Vector3(0,step*.17f,depth/2+1.5f-step*.35f),new Vector3(3.4f,.18f,1.1f));
            }
        }
        static void Relay(VisualBatch b,VisualAssetLibrary a,int level){
            b[a.concrete].Box(new Vector3(0,-7.7f,0),new Vector3(44,.6f,37));b[a.cladding].Box(Vector3.zero,new Vector3(42,16,35));b[a.rubber].Box(new Vector3(0,8.2f,0),new Vector3(43,.4f,36));
            foreach(int s in new[]{-1,1})b[a.alloy].Tube(new Vector3(s*2,8,0),new Vector3(s*.3f,52,0),.23f,.1f,level==0?12:6);
            b[a.alloy].Ellipsoid(new Vector3(0,48,0),new Vector3(10,5.4f,1.2f),level==0?32:level==1?16:8,level==0?18:8);
            b[WorldFactory.red].Ellipsoid(new Vector3(0,56,0),Vector3.one*.3f,8,6);
            if(level<2){
                for(int y=9;y<50;y+=5)b[a.alloy].Tube(new Vector3(-2+(y-8)*.038f,y,0),new Vector3(2-(y-3)*.038f,y+5,0),.09f,.09f,6);
                b[a.rubber].Box(new Vector3(14,-3,-17.6f),new Vector3(3,8,.15f));b[a.concrete].Box(new Vector3(-14,-5,-21),new Vector3(7,6,6));b[a.alloy].Tube(new Vector3(-14,-2,-21),new Vector3(-14,3,-21),.22f,.22f,10);
            }
            if(level==0){
                for(int i=0;i<12;i++)b[a.rubber].Box(new Vector3(-16+i*1.4f,1,-17.53f),new Vector3(.55f,3,.1f));
                for(int i=0;i<5;i++)b[a.alloy].Tube(new Vector3(-10+i*5,6,17.7f),new Vector3(-10+i*5,-7,17.7f),.07f,.07f,8);
                b[a.alloy].Tube(new Vector3(0,48,-1),new Vector3(0,48,-5),.15f,.1f,12);
            }
        }
    }
}

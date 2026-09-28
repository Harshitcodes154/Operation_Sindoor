using System.Collections.Generic;
using UnityEngine;

namespace Sindoor {
    public static class OriginalProps {
        public static Transform Build(Transform parent,string type,VisualAssetLibrary a){
            var root=new GameObject(type).transform;root.SetParent(parent,false);var lods=new List<LOD>();
            for(int detail=0;detail<3;detail++){
                var t=new GameObject("LOD"+detail).transform;t.SetParent(root,false);var b=new VisualBatch();
                if(type=="Hangar")Hangar(b,a,detail);else if(type=="Utility truck")Truck(b,a,detail);else if(type=="Tree")Tree(b,a,detail);else Rock(b,a,detail);
                lods.Add(new LOD(detail==0?.09f:detail==1?.025f:.002f,b.Attach(t,type)));
            }
            var lod=root.gameObject.AddComponent<LODGroup>();lod.SetLODs(lods.ToArray());lod.RecalculateBounds();return root;
        }
        static void Hangar(VisualBatch b,VisualAssetLibrary a,int level){
            b[a.concrete].Box(new Vector3(0,.15f,0),new Vector3(150,.3f,165));b[a.cladding].Box(new Vector3(73,14,0),new Vector3(2,28,165));
            foreach(int s in new[]{-1,1}){b[a.cladding].Box(new Vector3(0,14,s*81.5f),new Vector3(148,28,2));b[a.alloy].Box(new Vector3(-72,13,s*63),new Vector3(1.2f,26,36));}
            // Segmented arched roof, real structural depth, clerestory windows.
            int panels=level==0?20:10;
            for(int i=0;i<panels;i++){
                float z=-82.5f+i*165f/panels,zz=z+165f/panels,y=28+Mathf.Sin((z+82.5f)/165*Mathf.PI)*13,yy=28+Mathf.Sin((zz+82.5f)/165*Mathf.PI)*13;
                b[a.cladding].Quad(new Vector3(-75,y,z),new Vector3(-75,yy,zz),new Vector3(75,yy,zz),new Vector3(75,y,z),new Vector2(4,30));
            }
            if(level<2)for(int i=0;i<9;i++){
                float x=-68+i*17;b[a.alloy].Box(new Vector3(x,14,-79),new Vector3(.45f,28,.6f));b[a.alloy].Box(new Vector3(x,14,79),new Vector3(.45f,28,.6f));b[a.alloy].Tube(new Vector3(x,28,-79),new Vector3(x,40,0),.24f,.24f,8);b[a.alloy].Tube(new Vector3(x,40,0),new Vector3(x,28,79),.24f,.24f,8);
                b[WorldFactory.glow].Box(new Vector3(x,31,0),new Vector3(.23f,.06f,18));
            }
            if(level==0){for(int i=0;i<12;i++){b[a.canopy].Box(new Vector3(-63+i*11.5f,24,-82.6f),new Vector3(6,2.8f,.08f));}for(int s=-1;s<=1;s+=2)for(int i=0;i<10;i++)b[WorldFactory.orange].Box(new Vector3(-74.2f,1.4f,s*(46+i*3.4f)),new Vector3(.12f,2.8f,.32f));}
        }
        static void Truck(VisualBatch b,VisualAssetLibrary a,int level){
            b[a.rubber].Box(new Vector3(0,.68f,0),new Vector3(2.4f,.25f,6.5f));b[a.enemyPaint].Box(new Vector3(0,1.53f,1.72f),new Vector3(2.36f,1.7f,2.1f));b[a.enemyPaint].Box(new Vector3(0,1.25f,-1.42f),new Vector3(2.37f,1.16f,3.8f));
            b[a.canopy].Box(new Vector3(0,2.03f,2.79f),new Vector3(2.12f,.61f,.023f));
            foreach(int s in new[]{-1,1}){
                b[a.canopy].Box(new Vector3(s*1.189f,2.02f,1.82f),new Vector3(.024f,.63f,1.16f));
                foreach(float z in new[]{-2.13f,-.6f,1.92f}){b[a.rubber].Tube(new Vector3(s*1.1f,.56f,z),new Vector3(s*1.39f,.56f,z),.55f,.55f,level==0?24:level==1?12:6);b[a.alloy].Tube(new Vector3(s*1.4f,.56f,z),new Vector3(s*1.42f,.56f,z),.28f,.28f,level==0?16:level==1?8:5);}
                b[WorldFactory.glow].Box(new Vector3(s*.81f,1.23f,2.795f),new Vector3(.36f,.23f,.04f));
                if(level==0){b[a.alloy].Tube(new Vector3(s*1.18f,1.92f,2.28f),new Vector3(s*1.54f,2.13f,2.28f),.025f,.025f,8);b[a.rubber].Box(new Vector3(s*1.54f,2.13f,2.28f),new Vector3(.14f,.27f,.08f));}
            }
            b[a.alloy].Box(new Vector3(0,.89f,2.94f),new Vector3(2.53f,.18f,.22f));b[a.rubber].Box(new Vector3(0,1.42f,2.802f),new Vector3(1.06f,.43f,.04f));
            if(level==0)for(int i=0;i<8;i++)b[a.alloy].Box(new Vector3(-.43f+i*.125f,1.42f,2.834f),new Vector3(.025f,.4f,.015f));
        }
        static void Tree(VisualBatch b,VisualAssetLibrary a,int level){
            var bark=WorldFactory.Mat("Tree bark",new Color(.2f,.16f,.11f));b[bark].Tube(Vector3.zero,new Vector3(.3f,8,0),.36f,.12f,level==0?14:7);
            int branches=level==0?11:level==1?7:4;
            for(int i=0;i<branches;i++){
                float angle=i*2.39996f;Vector3 end=new Vector3(Mathf.Cos(angle)*(1.3f+i%3*.45f),5+i*.36f,Mathf.Sin(angle)*(1.3f+i%3*.45f));
                if(level<2)b[bark].Tube(new Vector3(.15f,4+i*.31f,0),end,.07f,.022f,6);
                b[a.foliage].RoughEllipsoid(end,new Vector3(1.65f,1.08f,1.48f),level==0?20:8,level==0?12:5,.85f);
            }
        }
        static void Rock(VisualBatch b,VisualAssetLibrary a,int level){
            b[a.soil].RoughEllipsoid(new Vector3(0,.7f,0),new Vector3(2.3f,1.5f,1.7f),level==0?22:level==1?12:7,level==0?16:7,1.1f);
            if(level<2)b[a.soil].RoughEllipsoid(new Vector3(1.2f,.3f,.7f),new Vector3(1.4f,.8f,1.2f),12,8,.9f);
        }
        public static Transform Cockpit(Transform parent,VisualAssetLibrary a){
            var root=new GameObject("Detailed cockpit").transform;root.SetParent(parent,false);var b=new VisualBatch();
            b[a.rubber].Box(new Vector3(0,-.66f,1.1f),new Vector3(2.62f,.36f,.9f));
            for(int i=-1;i<=1;i++){
                float x=i*.66f;b[a.alloy].Box(new Vector3(x,-.46f,.78f),new Vector3(.55f,.37f,.11f));b[a.rubber].Box(new Vector3(x,-.46f,.716f),new Vector3(.47f,.284f,.02f));
                b[WorldFactory.Mat("Avionics phosphor",new Color(.018f,.083f,.058f),0,.8f)].Box(new Vector3(x,-.46f,.703f),new Vector3(.431f,.25f,.008f));
                for(int s=-1;s<=1;s+=2)for(int button=0;button<5;button++)b[a.rubber].Box(new Vector3(x+s*.249f,-.35f-button*.052f,.70f),new Vector3(.025f,.023f,.027f));
                for(int k=0;k<6;k++)b[a.alloy].Tube(new Vector3(x-.19f+k*.075f,-.63f,.72f),new Vector3(x-.19f+k*.075f,-.63f,.688f),.01f,.01f,10);
            }
            for(int s=-1;s<=1;s+=2){
                b[a.alloy].Tube(new Vector3(s*.94f,-.16f,.4f),new Vector3(s*.78f,.42f,1.8f),.032f,.025f,16);
                b[a.rubber].Box(new Vector3(s*.93f,-.78f,.55f),new Vector3(.33f,.1f,1.1f),Quaternion.Euler(-8,0,s*10));
                for(int i=0;i<12;i++)b[a.alloy].Tube(new Vector3(s*.88f,-.70f,.17f+i*.066f),new Vector3(s*.88f,-.67f,.17f+i*.066f),.007f,.007f,8);
            }
            b[a.alloy].Box(new Vector3(0,-.31f,1.01f),new Vector3(.35f,.069f,.24f));b[a.canopy].Box(new Vector3(0,-.10f,1.12f),new Vector3(.48f,.31f,.007f),Quaternion.Euler(-8,0,0));
            b.Attach(root,"Cockpit instruments");root.gameObject.AddComponent<CockpitPresentation>();return root;
        }
    }
}

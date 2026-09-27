using System.Collections.Generic;
using UnityEngine;

namespace Sindoor {
    public static class OriginalCharacters {
        public static Transform Build(Transform parent,bool officer,VisualAssetLibrary a){
            var root=new GameObject(officer?"Officer visual rig":"Pilot visual rig").transform;root.SetParent(parent,false);
            var high=new GameObject("Visual Model").transform;high.SetParent(root,false);var b=new VisualBatch();
            var fabric=officer?WorldFactory.Mat("Officer wool",new Color(.16f,.21f,.25f)):a.fabric;
            var skin=WorldFactory.Mat("Skin / warm medium",new Color(.38f,.235f,.155f),0);
            var webbing=WorldFactory.Mat("Harness nylon",new Color(.22f,.25f,.18f),0);
            // Anatomical volumes under the flight suit; seams and equipment carry close-range detail.
            b[fabric].Ellipsoid(new Vector3(0,1.27f,0),new Vector3(.255f,.305f,.155f),32,22);
            b[fabric].Ellipsoid(new Vector3(0,1.02f,0),new Vector3(.225f,.17f,.155f),28,16);
            b[skin].Tube(new Vector3(0,1.49f,0),new Vector3(0,1.63f,0),.075f,.07f,20);
            b[skin].Ellipsoid(new Vector3(0,1.72f,.015f),new Vector3(.101f,.14f,.108f),32,24);
            if(officer){
                b[skin].Ellipsoid(new Vector3(0,1.70f,.119f),new Vector3(.024f,.034f,.028f),20,12);
                b[skin].Ellipsoid(new Vector3(0,1.62f,.069f),new Vector3(.071f,.054f,.074f),24,16);
                b[a.rubber].Tube(new Vector3(-.033f,1.655f,.125f),new Vector3(.033f,1.655f,.125f),.0025f,.0025f,8);
                for(int s=-1;s<=1;s+=2){
                    b[WorldFactory.white].Ellipsoid(new Vector3(s*.037f,1.748f,.107f),new Vector3(.02f,.009f,.008f),12,8);
                    b[a.rubber].Ellipsoid(new Vector3(s*.037f,1.748f,.114f),new Vector3(.007f,.007f,.004f),10,6);
                    b[a.rubber].Tube(new Vector3(s*.022f,1.77f,.109f),new Vector3(s*.057f,1.772f,.102f),.003f,.003f,8);
                }
                b[fabric].Ellipsoid(new Vector3(0,1.84f,0),new Vector3(.126f,.048f,.133f),32,14);
                b[a.rubber].Ellipsoid(new Vector3(0,1.815f,.127f),new Vector3(.11f,.009f,.077f),24,8);
                b[WorldFactory.orange].Box(new Vector3(0,1.842f,.126f),new Vector3(.023f,.023f,.005f));
            }else{
                var helmet=WorldFactory.Mat("Helmet composite",new Color(.49f,.5f,.44f),.18f);
                b[helmet].Ellipsoid(new Vector3(0,1.748f,-.005f),new Vector3(.137f,.158f,.139f),40,26);
                b[WorldFactory.Mat("Smoked pilot visor",new Color(.022f,.038f,.045f),.72f)].Ellipsoid(new Vector3(0,1.771f,.091f),new Vector3(.128f,.078f,.089f),36,18);
                b[a.rubber].Tube(new Vector3(-.12f,1.81f,.052f),new Vector3(.12f,1.81f,.052f),.013f,.013f,16);
                b[a.rubber].Ellipsoid(new Vector3(0,1.665f,.129f),new Vector3(.081f,.064f,.062f),24,16);
                b[a.alloy].Box(new Vector3(0,1.647f,.187f),new Vector3(.058f,.027f,.012f));
                for(int i=0;i<15;i++){
                    float t=i/14f,t2=(i+1)/14f;Vector3 p=new Vector3(.034f+t*.09f,1.636f-t*.37f,.17f+Mathf.Sin(t*Mathf.PI)*.09f),q=new Vector3(.034f+t2*.09f,1.636f-t2*.37f,.17f+Mathf.Sin(t2*Mathf.PI)*.09f);
                    b[a.rubber].Tube(p,q,.019f,.019f,10);b[a.alloy].Tube(p,p+(q-p)*.14f,.02f,.02f,10);
                }
                for(int s=-1;s<=1;s+=2){b[a.rubber].Ellipsoid(new Vector3(s*.126f,1.743f,0),new Vector3(.028f,.048f,.047f),20,12);b[a.alloy].Tube(new Vector3(s*.08f,1.674f,.155f),new Vector3(s*.131f,1.71f,.026f),.007f,.007f,10);}
            }
            for(int s=-1;s<=1;s+=2){
                b[webbing].Box(new Vector3(s*.127f,1.29f,.145f),new Vector3(.048f,.43f,.019f),Quaternion.Euler(0,0,-s*7));
                b[a.alloy].Box(new Vector3(s*.133f,1.18f,.163f),new Vector3(.057f,.039f,.013f));
                b[fabric].Box(new Vector3(s*.13f,1.36f,.157f),new Vector3(.117f,.097f,.024f));
                b[a.alloy].Tube(new Vector3(s*.19f,1.46f,.06f),new Vector3(s*.22f,1.46f,-.06f),.009f,.009f,8);
            }
            b[a.rubber].Box(new Vector3(0,1.06f,.142f),new Vector3(.37f,.035f,.026f));b[a.alloy].Box(new Vector3(0,1.06f,.165f),new Vector3(.054f,.034f,.012f));
            b[a.alloy].Tube(new Vector3(0,1.13f,.164f),new Vector3(0,1.48f,.147f),.003f,.003f,8);
            b.Attach(high,"Uniform and head");
            foreach(int s in new[]{-1,1}){
                var leg=new GameObject(s<0?"Leg L":"Leg R").transform;leg.SetParent(root,false);leg.localPosition=new Vector3(s*.116f,.98f,0);var l=new VisualBatch();
                l[fabric].Ellipsoid(new Vector3(0,-.205f,0),new Vector3(.11f,.26f,.115f),24,16);l[fabric].Ellipsoid(new Vector3(0,-.584f,.012f),new Vector3(.079f,.226f,.089f),24,16);
                l[fabric].Ellipsoid(new Vector3(0,-.399f,.016f),new Vector3(.093f,.086f,.105f),24,14);
                l[a.rubber].Ellipsoid(new Vector3(0,-.823f,.042f),new Vector3(.083f,.118f,.141f),24,16);l[a.rubber].Box(new Vector3(0,-.929f,.053f),new Vector3(.171f,.037f,.287f));
                l[fabric].Box(new Vector3(s*.077f,-.19f,.052f),new Vector3(.037f,.17f,.104f));
                for(int i=0;i<5;i++)l[webbing].Tube(new Vector3(-.05f,-.78f-i*.022f,.122f),new Vector3(.05f,-.78f-i*.022f,.122f),.003f,.003f,8);
                l.Attach(leg,"Articulated leg");
                var arm=new GameObject(s<0?"Arm L":"Arm R").transform;arm.SetParent(root,false);arm.localPosition=new Vector3(s*.246f,1.448f,0);var upper=new VisualBatch();
                upper[fabric].Ellipsoid(new Vector3(s*.037f,-.124f,0),new Vector3(.082f,.171f,.084f),24,16);upper.Attach(arm,"Upper sleeve");
                var forearm=new GameObject("Elbow").transform;forearm.SetParent(arm,false);forearm.localPosition=new Vector3(s*.035f,-.276f,0);forearm.localRotation=Quaternion.Euler(-9,0,0);var lower=new VisualBatch();
                lower[fabric].Ellipsoid(new Vector3(0,-.105f,0),new Vector3(.064f,.135f,.07f),24,16);lower[a.rubber].Ellipsoid(new Vector3(0,-.247f,.016f),new Vector3(.052f,.081f,.034f),24,16);
                for(int i=0;i<4;i++)lower[a.rubber].Tube(new Vector3(-.033f+i*.021f,-.27f,.032f),new Vector3(-.033f+i*.021f,-.324f,.026f),.009f,.007f,10);
                lower[a.rubber].Tube(new Vector3(s*.04f,-.23f,.018f),new Vector3(s*.064f,-.275f,.048f),.014f,.012f,12);lower.Attach(forearm,"Glove and forearm");
                // Small tricolour sleeve patch.
                if(s<0){var patch=new VisualBatch();for(int stripe=0;stripe<3;stripe++)patch[stripe==0?WorldFactory.orange:stripe==1?WorldFactory.white:WorldFactory.green].Box(new Vector3(-.079f,-.11f+(.02f-stripe*.019f),.025f),new Vector3(.009f,.018f,.074f));patch.Attach(arm,"Sleeve patch");}
            }
            // One low-detail fallback, with the articulated high-detail rig retained for close cinematics.
            var low=new GameObject("LOD1 distant silhouette").transform;low.SetParent(root,false);var lb=new VisualBatch();lb[fabric].Ellipsoid(new Vector3(0,1.22f,0),new Vector3(.25f,.34f,.16f),10,7);lb[a.rubber].Ellipsoid(new Vector3(0,1.75f,0),new Vector3(.13f,.15f,.14f),10,7);
            foreach(int s in new[]{-1,1}){lb[fabric].Tube(new Vector3(s*.12f,.95f,0),new Vector3(s*.12f,.12f,0),.1f,.075f,8);lb[fabric].Tube(new Vector3(s*.27f,1.45f,0),new Vector3(s*.29f,.86f,0),.078f,.052f,8);}
            var lowRenderers=lb.Attach(low,"Silhouette");var all=new List<Renderer>(root.GetComponentsInChildren<Renderer>());foreach(var r in lowRenderers)all.Remove(r);
            var lod=root.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.035f,all.ToArray()),new LOD(.003f,lowRenderers)});lod.RecalculateBounds();root.gameObject.AddComponent<CharacterPresentation>();return root;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Sindoor {
    public static class OriginalAircraft {
        public static Transform Build(Transform parent,bool hostile,VisualAssetLibrary a){
            var root=new GameObject(hostile?"Raven / fictional opposing aircraft":"Kestrel F.1 / original aircraft").transform;root.SetParent(parent,false);
            var visual=new GameObject("Visual Model").transform;visual.SetParent(root,false);var lods=new List<LOD>();
            for(int level=0;level<3;level++){
                var group=new GameObject("LOD"+level).transform;group.SetParent(visual,false);
                var b=new VisualBatch();var paint=hostile?a.enemyPaint:a.airframe;int radial=level==0?48:level==1?24:12,steps=level==0?4:level==1?2:1;
                b[paint].Loft(new[]{-7.7f,-6.8f,-5,-2.5f,0,2.8f,4.9f,6.7f,8.6f,9.8f},new[]{.88f,1.28f,1.51f,1.6f,1.33f,.96f,.77f,.54f,.25f,.018f},new[]{.64f,.78f,.86f,.9f,.91f,.8f,.67f,.48f,.25f,.018f},new[]{-.07f,-.04f,0,0,0,0,-.025f,-.07f,-.13f,-.17f},radial,steps);
                int wingSteps=level==0?14:level==1?7:3,chord=level==0?20:level==1?10:4;
                foreach(int side in new[]{-1,1}){
                    float span=hostile?6.4f:7.1f;
                    b[paint].Airfoil(new Vector3(side*.85f,.03f,2.5f),new Vector3(side*1.2f,.02f,-5.2f),new Vector3(side*span,.12f,hostile?-2.1f:-3.4f),new Vector3(side*span,.08f,-5.35f),.9f,wingSteps,chord);
                    b[paint].Airfoil(new Vector3(side*.48f,.18f,5.9f),new Vector3(side*.76f,.25f,4.1f),new Vector3(side*2.7f,.21f,4.8f),new Vector3(side*2.7f,.24f,4.1f),.27f,steps+2,chord/2);
                    // Canted vertical surfaces are lofted in their own coordinates, not thin primitive triangles.
                    var fin=new VisualMesh();fin.Airfoil(new Vector3(0,0,1.8f),new Vector3(0,0,-2.1f),new Vector3(side*.9f,2.7f,-.5f),new Vector3(side*1.1f,2.7f,-2.05f),.25f,steps+2,chord/2);
                    var finRoot=new GameObject("Canted stabilizer "+side).transform;finRoot.SetParent(group,false);finRoot.localPosition=new Vector3(side*.85f,.65f,-5.1f);fin.Attach(finRoot,"Rudder",paint);
                    b[a.alloy].Tube(new Vector3(side*.69f,-.1f,-6.9f),new Vector3(side*.69f,-.1f,-8.2f),.6f,.48f,radial,false);
                    b[a.rubber].Tube(new Vector3(side*.69f,-.1f,-8.17f),new Vector3(side*.69f,-.1f,-7.55f),.44f,.39f,radial,true);
                    // Tapered inlet shoulders and recessed throat replace the solid rectangular pods.
                    var inlet=new GameObject("Intake "+side).transform;inlet.SetParent(group,false);inlet.localPosition=new Vector3(side*1.04f,-.34f,0);
                    var intake=new VisualBatch();intake[paint].Loft(new[]{-.9f,.5f,1.9f,2.5f},new[]{.2f,.37f,.4f,.37f},new[]{.24f,.45f,.44f,.39f},new[]{0f,0f,0f,0f},level==0?24:12,3);
                    intake[a.rubber].Ellipsoid(new Vector3(0,0,2.35f),new Vector3(.32f,.345f,.025f),level==0?24:12,8);
                    intake.Attach(inlet,"Inlet lip and throat");
                    if(level<2){
                        for(int p=0;p<12;p++){
                            float angle=p*Mathf.PI*2/12;Vector3 outward=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                            b[a.alloy].Box(new Vector3(side*.69f,-.1f,-7.7f)+outward*.54f,new Vector3(.09f,.045f,.94f),Quaternion.Euler(0,0,angle*Mathf.Rad2Deg));
                        }
                        for(int h=0;h<3;h++){
                            float x=side*(2.25f+h*1.55f),z=-1.4f-h*.62f;b[a.alloy].Box(new Vector3(x,-.37f,z),new Vector3(.14f,.58f,1.5f));
                            MissileMesh(b,new Vector3(x,-.79f,z+.3f),a,level==0?20:10,.92f);
                        }
                        for(int l=0;l<4;l++)b[a.rubber].Box(new Vector3(side*.91f,.54f,-3+l*.33f),new Vector3(.42f,.02f,.07f),Quaternion.Euler(0,0,-side*22));
                        b[a.alloy].Tube(new Vector3(side*.42f,-.02f,6.1f),new Vector3(side*.42f,-.02f,7.1f),.025f,.016f,8);
                    }
                    if(level==0){
                        for(int panel=0;panel<7;panel++)b[a.alloy].Box(new Vector3(side*(1.72f+panel*.64f),.145f,-4.65f),new Vector3(.02f,.014f,.53f));
                        b[a.rubber].Tube(new Vector3(side*1.02f,.02f,3.2f),new Vector3(side*1.02f,.02f,4.1f),.057f,.049f,12);
                        if(!hostile){
                            WingRoundel(b,side);
                        }
                    }
                    b[side<0?WorldFactory.red:WorldFactory.glow].Ellipsoid(new Vector3(side*span,.17f,-4.6f),Vector3.one*.065f,8,4);
                }
                // Canopy, sill, instrument coaming and visible seat.
                b[a.rubber].Ellipsoid(new Vector3(0,.84f,3.2f),new Vector3(.77f,.22f,2.12f),radial,12);
                b[a.canopy].Ellipsoid(new Vector3(0,1.03f,3.15f),new Vector3(.73f,.68f,1.97f),radial,level==0?20:10);
                if(level<2){
                    for(int frame=0;frame<16;frame++){
                        float z=1.22f+frame*3.86f/16,zz=1.22f+(frame+1)*3.86f/16;
                        float y=1.03f+.68f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((z-3.15f)/1.97f,2))),yy=1.03f+.68f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((zz-3.15f)/1.97f,2)));
                        b[paint].Tube(new Vector3(0,y,z),new Vector3(0,yy,zz),.028f,.028f,8);
                    }
                    b[a.rubber].Box(new Vector3(0,1.02f,2.65f),new Vector3(.48f,.83f,.36f),Quaternion.Euler(-12,0,0));b[a.fabric].Box(new Vector3(0,1.25f,2.89f),new Vector3(.4f,.54f,.05f));
                }
                b.Attach(group,"Airframe");lods.Add(new LOD(level==0?.14f:level==1?.045f:.003f,group.GetComponentsInChildren<Renderer>()));
            }
            var lod=visual.gameObject.AddComponent<LODGroup>();lod.SetLODs(lods.ToArray());lod.RecalculateBounds();
            var gear=new GameObject("Landing gear").transform;gear.SetParent(visual,false);
            for(int i=0;i<3;i++){
                float x=i==0?0:i==1?-1.45f:1.45f,z=i==0?5.05f:-2.3f;
                var strut=new GameObject(i==0?"Nose strut":i==1?"Main strut L":"Main strut R").transform;strut.SetParent(gear,false);strut.localPosition=new Vector3(x,-.65f,z);var gb=new VisualBatch();
                gb[a.alloy].Tube(Vector3.zero,new Vector3(0,-1.75f,0),.067f,.075f,16);gb[a.alloy].Tube(new Vector3(0,-.45f,-.65f),new Vector3(0,-1.52f,0),.04f,.04f,12);
                gb[hostile?a.enemyPaint:a.airframe].Box(new Vector3(.23f,-.6f,0),new Vector3(.025f,.8f,1.05f),Quaternion.Euler(0,0,12));gb.Attach(strut,"Suspension and door");
                var wheel=new GameObject("Wheel").transform;wheel.SetParent(strut,false);wheel.localPosition=new Vector3(0,i==0?-1.92f:-1.85f,0);var wb=new VisualBatch();float radius=i==0?.36f:.43f;
                wb[a.rubber].Tube(new Vector3(-.18f,0,0),new Vector3(.18f,0,0),radius,radius,32);
                wb[a.alloy].Tube(new Vector3(-.19f,0,0),new Vector3(.19f,0,0),.2f,.2f,20);
                foreach(int side in new[]{-1,1})for(int bolt=0;bolt<6;bolt++){float theta=bolt*Mathf.PI/3;wb[a.rubber].Ellipsoid(new Vector3(side*.195f,Mathf.Sin(theta)*.13f,Mathf.Cos(theta)*.13f),Vector3.one*.027f,8,6);}
                wb.Attach(wheel,"Wheel and hub");
            }
            // Keep the existing cinematic entry path clear and give the climb visible support.
            var ladder=new GameObject("Boarding ladder").transform;ladder.SetParent(root,false);var stepsMesh=new VisualBatch();
            foreach(float x in new[]{-1.98f,-1.22f})stepsMesh[a.alloy].Tube(new Vector3(x,-1.45f,3.3f),new Vector3(x,1.35f,3.3f),.027f,.027f,10);
            for(int rung=0;rung<9;rung++)stepsMesh[a.alloy].Tube(new Vector3(-1.98f,-1.4f+rung*.31f,3.3f),new Vector3(-1.22f,-1.4f+rung*.31f,3.3f),.028f,.028f,10);
            stepsMesh.Attach(ladder,"Boarding steps");ladder.gameObject.SetActive(false);
            var attachments=new GameObject("Attachments").transform;attachments.SetParent(root,false);
            Anchor(attachments,"CockpitCamera",new Vector3(0,1.5f,4.5f));Anchor(attachments,"CannonMuzzle",new Vector3(0,-.5f,9));Anchor(attachments,"MissileLaunch",new Vector3(0,-1,8));
            Anchor(attachments,"ExhaustL",new Vector3(-.69f,-.1f,-8.2f));Anchor(attachments,"ExhaustR",new Vector3(.69f,-.1f,-8.2f));
            var presentation=root.gameObject.AddComponent<AircraftPresentation>();presentation.visualRoot=visual;presentation.gear=gear;
            return root;
        }
        static void Anchor(Transform root,string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(root,false);t.localPosition=position;}
        static Vector3 WingSurface(float x,float z){
            float span=Mathf.Clamp01((Mathf.Abs(x)-.85f)/6.25f),chord=0;
            for(int k=0;k<4;k++){float leading=Mathf.Lerp(2.5f,-3.4f,span),trailing=Mathf.Lerp(-5.2f,-5.35f,span);chord=Mathf.Clamp01((z-leading)/(trailing-leading));span=Mathf.Clamp01((Mathf.Abs(x)-.85f-.35f*chord)/(6.25f-.35f*chord));}
            float profile=4.5f*(.2969f*Mathf.Sqrt(chord)-.126f*chord-.3516f*chord*chord+.2843f*chord*chord*chord-.1036f*Mathf.Pow(chord,4))*(1-span*.72f);
            float y=Mathf.Lerp(Mathf.Lerp(.03f,.12f,span),Mathf.Lerp(.02f,.08f,span),chord)+profile+.018f;
            return new Vector3(x,y,z);
        }
        static void WingRoundel(VisualBatch b,int side){
            float[] radius={.48f,.32f,.17f,0};Material[] colors={WorldFactory.orange,WorldFactory.white,WorldFactory.green};
            for(int ring=0;ring<3;ring++)for(int i=0;i<48;i++){
                float a=i*Mathf.PI/24,c=(i+1)*Mathf.PI/24;
                Vector3 p=WingSurface(side*4.5f+Mathf.Cos(a)*radius[ring],-3.95f+Mathf.Sin(a)*radius[ring]),q=WingSurface(side*4.5f+Mathf.Cos(c)*radius[ring],-3.95f+Mathf.Sin(c)*radius[ring]);
                Vector3 r=WingSurface(side*4.5f+Mathf.Cos(a)*radius[ring+1],-3.95f+Mathf.Sin(a)*radius[ring+1]),s=WingSurface(side*4.5f+Mathf.Cos(c)*radius[ring+1],-3.95f+Mathf.Sin(c)*radius[ring+1]);
                b[colors[ring]].Quad(p,r,s,q,Vector2.one);
            }
        }
        static void MissileMesh(VisualBatch b,Vector3 offset,VisualAssetLibrary a,int detail,float scale=1){
            b[WorldFactory.white].Tube(offset+Vector3.back*1.48f*scale,offset+Vector3.forward*.92f*scale,.125f*scale,.125f*scale,detail);
            b[a.alloy].Tube(offset+Vector3.forward*.92f*scale,offset+Vector3.forward*1.52f*scale,.125f*scale,.008f,detail);
            b[a.rubber].Tube(offset+Vector3.back*1.5f*scale,offset+Vector3.back*1.45f*scale,.13f*scale,.13f*scale,detail);
            for(int i=0;i<4;i++){
                var rot=Quaternion.Euler(0,0,i*90);var p=offset+rot*new Vector3(0,.24f*scale,-1.05f*scale);
                b[a.alloy].Box(p,new Vector3(.022f,.39f,.54f)*scale,rot);
            }
        }
        public static Transform Missile(Transform parent,VisualAssetLibrary a){var t=new GameObject("Missile visual").transform;t.SetParent(parent,false);var b=new VisualBatch();MissileMesh(b,Vector3.zero,a,24,1);b.Attach(t,"Missile");return t;}
    }
}

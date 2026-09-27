using UnityEngine;
using UnityEngine.Rendering;

namespace Sindoor {
    public static class VisualEffects {
        static Material smokeMaterial,fireMaterial;
        public static Material Smoke {get{Initialize();return smokeMaterial;}}
        static void Initialize(){
            if(smokeMaterial)return;
            var a=VisualAssetLibrary.Current;var shader=Shader.Find("Sindoor/Particle");
            smokeMaterial=new Material(shader){name="Soft smoke particles"};smokeMaterial.SetTexture("_BaseMap",a.smoke);smokeMaterial.SetFloat("_Additive",0);smokeMaterial.renderQueue=3000;
            fireMaterial=new Material(shader){name="Emissive fire particles"};fireMaterial.SetTexture("_BaseMap",a.smoke);fireMaterial.SetFloat("_Additive",1);fireMaterial.renderQueue=3001;
        }
        public static ParticleSystem Emitter(Transform parent,string name,bool fire){
            Initialize();var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localRotation=Quaternion.Euler(0,180,0);var p=obj.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main;main.loop=true;main.playOnAwake=false;main.maxParticles=fire?80:70;main.startLifetime=fire?.22f:3;main.startSpeed=fire?12:2;main.startSize=fire?.42f:1.1f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=fire?7:17;shape.radius=fire?.22f:.3f;
            var emission=p.emission;emission.rateOverTime=0;
            var color=p.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(fire?new[]{new GradientColorKey(new Color(.6f,.74f,1)*2,0),new GradientColorKey(new Color(1,.42f,.08f)*1.5f,.3f),new GradientColorKey(new Color(.4f,.11f,.03f),1)}:new[]{new GradientColorKey(new Color(.1f,.12f,.14f),0),new GradientColorKey(new Color(.25f,.27f,.28f),1)},new[]{new GradientAlphaKey(fire?.8f:.45f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=p.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,fire?.7f:.4f),new Keyframe(1,fire?.06f:5)));
            var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=fire?fireMaterial:smokeMaterial;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return p;
        }
        public static Transform Missile(Transform parent){
            var root=VisualAssetLibrary.InstantiateVisual(VisualAssetLibrary.Current.missile,parent,"Guided missile");
            var tail=new GameObject("Motor").transform;tail.SetParent(root,false);tail.localPosition=Vector3.back*1.5f;var p=Emitter(tail,"Rocket exhaust",true);var e=p.emission;e.rateOverTime=30;p.Play();return root;
        }
        public static GameObject Impact(Transform parent){var root=new GameObject("Pooled layered impact");root.transform.SetParent(parent,false);var fx=root.AddComponent<LayeredImpact>();fx.Initialize();return root;}
    }
    public sealed class LayeredImpact : MonoBehaviour {
        ParticleSystem fire,smoke,sparks;float until;Light flash;
        public void Initialize(){
            fire=VisualEffects.Emitter(transform,"Flash and fireball",true);smoke=VisualEffects.Emitter(transform,"Rolling smoke",false);sparks=VisualEffects.Emitter(transform,"Sparks and debris",true);
            foreach(var p in new[]{fire,smoke,sparks}){var s=p.shape;s.shapeType=ParticleSystemShapeType.Sphere;s.radius=.25f;var m=p.main;m.loop=false;m.duration=4;m.maxParticles=p==smoke?24:40;}
            flash=new GameObject("Impact flash").AddComponent<Light>();flash.transform.SetParent(transform,false);flash.type=LightType.Point;flash.color=new Color(1,.39f,.12f);flash.shadows=LightShadows.None;flash.enabled=false;
        }
        public void Play(Vector3 position,float radius){
            transform.position=position;gameObject.SetActive(true);bool large=radius>5;until=Time.time+(large?4.5f:.8f);
            fire.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var fm=fire.main;fm.startLifetime=large?.85f:.12f;fm.startSize=new ParticleSystem.MinMaxCurve(radius*.18f,radius*.5f);fm.startSpeed=radius*.6f;
            var sm=smoke.main;sm.startLifetime=large?3.7f:.6f;sm.startSize=radius*.25f;sm.startSpeed=radius*.19f;
            var sp=sparks.main;sp.startLifetime=large?1.3f:.3f;sp.startSize=large?.12f:.025f;sp.startSpeed=new ParticleSystem.MinMaxCurve(radius, radius*2);sp.gravityModifier=.45f;
            fire.Emit(large?22:5);smoke.Emit(large?18:3);sparks.Emit(large?25:7);flash.enabled=large;flash.range=radius*2;flash.intensity=large?12:0;
        }
        void Update(){if(flash.enabled){flash.intensity=Mathf.MoveTowards(flash.intensity,0,Time.deltaTime*70);if(flash.intensity<=0)flash.enabled=false;}if(Time.time>until)gameObject.SetActive(false);}
    }
}

using UnityEngine;

namespace Sindoor {
    public sealed class AircraftPresentation : MonoBehaviour {
        public Transform visualRoot,gear;
        ParticleSystem[] exhaust;
        ParticleSystem damageSmoke;
        float gearBlend=1;
        bool visible=true;
        Transform boardingLadder;
        Transform[] struts,wheels;Vector3 previousPosition;float wheelAngle,compression;
        void Start(){
            previousPosition=transform.position;
            if(gear){struts=new[]{gear.Find("Nose strut"),gear.Find("Main strut L"),gear.Find("Main strut R")};wheels=new Transform[3];for(int i=0;i<3;i++)wheels[i]=struts[i]?struts[i].Find("Wheel"):null;}
            boardingLadder=transform.Find("Boarding ladder");
            var a=transform.Find("Attachments");if(!a)return;
            exhaust=new[]{VisualEffects.Emitter(a.Find("ExhaustL"),"Turbine exhaust",true),VisualEffects.Emitter(a.Find("ExhaustR"),"Turbine exhaust",true)};
            damageSmoke=VisualEffects.Emitter(a.Find("ExhaustL"),"Damage smoke",false);damageSmoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void SetVisible(bool value){visible=value;if(!value){if(exhaust!=null)foreach(var p in exhaust)if(p)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(damageSmoke)damageSmoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}}
        void LateUpdate(){
            var game=OperationGame.Instance;if(!game||!gear)return;
            bool player=game.ship==transform;
            if(boardingLadder)boardingLadder.gameObject.SetActive(player&&game.mode==Mode.Cinematic&&(game.shot==6||game.shot==7));
            bool onGround=player?(game.stage==Stage.Startup||game.stage==Stage.Taxi||game.stage==Stage.Land||game.ship.position.y<20):transform.position.y<WorldFactory.Height(transform.position.x,transform.position.z)+28;
            float dt=game.enabled&&game.mode==Mode.Paused?0:Time.deltaTime;
            gearBlend=Mathf.MoveTowards(gearBlend,onGround?1:0,dt*.5f);float folded=1-Mathf.SmoothStep(0,1,gearBlend);gear.localScale=Vector3.one;gear.gameObject.SetActive(gearBlend>.001f);
            if(struts!=null)for(int i=0;i<3;i++)if(struts[i])struts[i].localRotation=i==0?Quaternion.Euler(-95*folded,0,0):Quaternion.Euler(0,0,(i==1?-88:88)*folded);
            float surface=VisualEnvironment.SurfaceHeight(transform.position);
            compression=CharacterPresentation.Damp(compression,onGround?Mathf.Clamp(surface-transform.position.y+2.93f,0,1.5f):0,12,dt);gear.localPosition=Vector3.up*compression;
            Vector3 movement=transform.position-previousPosition;previousPosition=transform.position;
            if(onGround&&movement.sqrMagnitude<100&&dt>0)wheelAngle=Mathf.Repeat(wheelAngle+Vector3.Dot(movement,transform.forward)/.43f*Mathf.Rad2Deg,360);
            if(wheels!=null)for(int i=0;i<wheels.Length;i++)if(wheels[i])wheels[i].localRotation=Quaternion.Euler(wheelAngle*(i==0?.43f/.36f:1),0,0);
            bool engine=visible&&(player?game.engineOn:!onGround)&&game.mode!=Mode.Menu&&game.mode!=Mode.Credits;
            if(exhaust!=null)foreach(var p in exhaust){
                if(!p)continue;var emission=p.emission;emission.rateOverTime=engine?(player?25+game.throttle*70:45):0;
                var main=p.main;main.startSpeed=player?4+game.throttle*14:9;main.startLifetime=player?.12f+game.throttle*.14f:.18f;
                if(engine&&!p.isPlaying)p.Play();
            }
            if(player&&damageSmoke){bool smoking=visible&&game.health<35&&game.mode==Mode.Flight;var e=damageSmoke.emission;e.rateOverTime=smoking?12:0;if(smoking&&!damageSmoke.isPlaying)damageSmoke.Play();}
        }
    }
}

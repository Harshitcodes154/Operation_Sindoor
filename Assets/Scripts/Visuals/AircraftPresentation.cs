using UnityEngine;

namespace Sindoor {
    public sealed class AircraftPresentation : MonoBehaviour {
        public Transform visualRoot,gear;
        ParticleSystem[] exhaust;
        ParticleSystem damageSmoke;
        float gearBlend=1;
        bool visible=true;
        Transform boardingLadder;
        void Start(){
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
            gearBlend=Mathf.MoveTowards(gearBlend,onGround?1:0,Time.deltaTime*.65f);gear.localScale=new Vector3(1,Mathf.Max(.001f,gearBlend),1);gear.gameObject.SetActive(gearBlend>.01f);
            float surface=VisualEnvironment.SurfaceHeight(transform.position);
            gear.localPosition=Vector3.up*(onGround?Mathf.Clamp(surface-transform.position.y+2.93f,0,1.5f):0);
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

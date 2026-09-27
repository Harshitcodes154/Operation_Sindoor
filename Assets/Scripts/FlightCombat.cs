using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sindoor {
    public sealed class Contact {
        public Transform body;public Allegiance side;public string name,state="PATROL";public float health=100,speed=150,fireAt,decisionAt,phase;public Vector3 desired;public bool dead;
    }
    public sealed class MissileBody {public Transform body;public Contact target;public bool hostile;public float life,speed=520;public Vector3 direction;public TrailRenderer trail;}
    public partial class OperationGame {
        float cannonAt,missileAt,flareAt,warningUntil,damageAt,stallTime;float friendlyHealth=100;
        readonly System.Collections.Generic.List<TransientEffect> effectPool=new System.Collections.Generic.List<TransientEffect>();
        int landingLeg;
        public float heading => (ship.eulerAngles.y+360)%360;
        public float AGL => ship.position.y-GroundHeight(ship.position);
        static float GroundHeight(Vector3 p)=>Mathf.Abs(p.x)<440&&p.z>-6100&&p.z<-2900?1.5f:WorldFactory.Height(p.x,p.z)-2;
        void TickFlight(float dt){
            alert="";var p=save.settings;var pad=Controls.Pad;
            if(Controls.Bind(p.assist)||(pad!=null&&pad.leftShoulder.wasPressedThisFrame)){assist=!assist;Toast(assist?"ROUTE ASSIST ENGAGED":"MANUAL FLIGHT");}
            if(Controls.Down(Key.C)||(pad!=null&&pad.rightStickButton.wasPressedThisFrame))chase=!chase;
            if(Controls.Down(Key.Tab)||(pad!=null&&pad.selectButton.wasPressedThisFrame))radarOpen=!radarOpen;
            if(engineOn){float input=Controls.Axis(Key.W,Key.S);if(pad!=null)input+=pad.dpad.y.ReadValue();throttle=Mathf.Clamp01(throttle+input*dt*.28f);}
            bool burner=engineOn&&fuel>0&&(Controls.Held(Key.LeftShift)||(pad!=null&&pad.leftStickButton.isPressed));
            float yaw=Controls.Axis(Key.E,Key.Q),roll=Controls.Axis(Key.A,Key.D),pitch=Controls.Axis(Key.UpArrow,Key.DownArrow);
            bool manual=Mathf.Abs(yaw)+Mathf.Abs(roll)+Mathf.Abs(pitch)>.1f;
            if(pad!=null){var stick=pad.leftStick.ReadValue();float rudder=pad.rightStick.x.ReadValue();pitch+=stick.y;roll-=stick.x;yaw+=rudder;manual|=stick.sqrMagnitude>.05f||Mathf.Abs(rudder)>.1f;}
            if(p.mouseFlight&&Mouse.current!=null&&Mouse.current.rightButton.isPressed){var d=Mouse.current.delta.ReadValue();pitch-=d.y*p.sensitivity*.035f*(p.invertMouse?-1:1);yaw+=d.x*p.sensitivity*.035f;manual|=d.sqrMagnitude>.01f;}
            if(assist&&manual){assist=false;Toast("MANUAL CONTROL");}
            float desiredSpeed=engineOn&&fuel>0?35+throttle*235+(burner?110:0):0;
            if(stage==Stage.Startup)desiredSpeed=0;
            bool ground=stage==Stage.Startup||stage==Stage.Taxi||(stage==Stage.Takeoff&&ship.position.y<5);
            if(assist&&engineOn){
                Vector3 destination=waypoint;throttle=.76f;desiredSpeed=215;
                if(stage==Stage.Takeoff){destination=Launch+new Vector3(0,speed<65?3:700,4000);throttle=1;desiredSpeed=245;}
                if(stage==Stage.Return){destination=Approach;desiredSpeed=Mathf.Min(220,100+Vector3.Distance(ship.position,Approach)*.03f);}
                if(stage==Stage.Land){
                    if(landingLeg==0){destination=new Vector3(0,420,-10100);desiredSpeed=140;if(Vector3.Distance(ship.position,destination)<500)landingLeg=1;}
                    else if(landingLeg==1){destination=new Vector3(0,360,-7700);desiredSpeed=100;if(Vector3.Distance(ship.position,destination)<450&&Mathf.Abs(Mathf.DeltaAngle(heading,0))<30)landingLeg=2;}
                    else{float z=ship.position.z;destination=new Vector3(0,Mathf.Max(3,(-5450-z)*.07f),z+350);desiredSpeed=82;}
                    waypoint=destination;throttle=.2f;
                }
                if(stage==Stage.Taxi){desiredSpeed=0;throttle=0;destination=ship.position+Vector3.forward*100;}
                if(stage==Stage.Identify||stage==Stage.Combat||stage==Stage.Strike){if(target==null||target.dead){target=ClosestTarget();lockProgress=0;}if(target!=null){destination=target.body.position+target.body.forward*target.speed*.8f;float range=Vector3.Distance(ship.position,target.body.position);if(target.side==Allegiance.Ground&&range<700)destination=ship.position+ship.forward*1400+Vector3.up*600;desiredSpeed=target.side==Allegiance.Ground?150:Mathf.Clamp(target.speed+(range-500)*.06f,115,240);}else destination=ship.position+ship.forward*1500;}
                if(stage!=Stage.Land&&stage!=Stage.Taxi&&stage!=Stage.Takeoff)destination.y=Mathf.Max(destination.y,GroundHeight(ship.position+ship.forward*400)+250);
                Vector3 dir=(destination-ship.position).normalized;
                Quaternion look=Quaternion.LookRotation(dir,Vector3.up);ship.rotation=Quaternion.RotateTowards(ship.rotation,look,dt*38);ground=stage==Stage.Taxi||(stage==Stage.Takeoff&&speed<65);
            }else if(engineOn&&speed>15){
                float handling=(.45f+.55f*health/100)*Mathf.Clamp(speed/80,.25f,1);
                ship.Rotate(Mathf.Clamp(pitch,-1,1)*dt*45*handling,Mathf.Clamp(yaw,-1,1)*dt*28*handling,Mathf.Clamp(roll,-1,1)*dt*80*handling,Space.Self);
                // Banking contributes a coordinated turn; pitch and roll remain player controlled.
                float bank=Mathf.DeltaAngle(0,ship.eulerAngles.z);ship.Rotate(Vector3.up,-Mathf.Sin(bank*Mathf.Deg2Rad)*dt*22,Space.World);
                if(!manual){var e=ship.eulerAngles;e.z=Mathf.LerpAngle(e.z,0,dt*.3f);ship.eulerAngles=e;}
            }
            speed=Mathf.MoveTowards(speed,desiredSpeed,dt*(desiredSpeed>speed?26:stage==Stage.Taxi?22:18));
            Vector3 movement=ship.forward*speed*dt;
            if(!ground&&speed<65){movement.y-=(65-speed)*.45f*dt;alert="LOW AIRSPEED / INCREASE THROTTLE";stallTime+=dt;}else stallTime=0;
            ship.position+=movement;
            if(ground){ship.position=new Vector3(ship.position.x,3,ship.position.z);if(stage==Stage.Takeoff&&speed>65&&ship.forward.y>.025f)ship.position+=Vector3.up*3;}
            if(stage==Stage.Land&&assist&&ship.position.z>-5600&&ship.position.y<13)ship.position=new Vector3(ship.position.x,3,ship.position.z);
            fuel=Mathf.Max(0,fuel-dt*(engineOn?(burner?.09f:.023f):0));
            if(stage!=Stage.Startup&&stage!=Stage.Taxi&&!(stage==Stage.Takeoff&&ship.position.y<6)&&!(stage==Stage.Land&&Mathf.Abs(ship.position.x)<43&&speed<105&&ship.position.z>-5700&&ship.position.z<-3300)){
                if(ship.position.y<GroundHeight(ship.position)+2){Effect(ship.position,20);Fail("AIRFRAME LOST / TERRAIN COLLISION");return;}
            }
            if(!ground&&AGL<130&&stage!=Stage.Land){alert="TERRAIN / PULL UP";audioDirector.Cue("alarm");}
            if(health<35){alert="CRITICAL DAMAGE / CONTROLS DEGRADED";audioDirector.Cue("alarm");}
            if(Time.time<warningUntil){alert="MISSILE INBOUND / X COUNTERMEASURES";audioDirector.Cue("alarm");}
            if(stage==Stage.Startup||stage==Stage.Taxi)return;
            if(Controls.Bind(p.lockTarget)||(pad!=null&&pad.buttonWest.wasPressedThisFrame))SelectTarget();
            if(target!=null&&!target.dead){
                Vector3 diff=target.body.position-ship.position;float angle=Vector3.Angle(ship.forward,diff);float distance=diff.magnitude;
                float cone=p.difficulty==2?24:38;
                if(distance<6500&&angle<cone)lockProgress=Mathf.Min(1,lockProgress+dt/(p.difficulty==0?.65f:1.3f));else lockProgress=Mathf.Max(0,lockProgress-dt*1.5f);
            }else{target=null;lockProgress=0;}
            if(Controls.Bind(p.missile)||(pad!=null&&pad.buttonSouth.wasPressedThisFrame))FireMissile();
            if(Controls.Bind(p.fire,true)||(pad!=null&&pad.rightTrigger.ReadValue()>.4f))FireCannon();
            if(Controls.Bind(p.counter)||(pad!=null&&pad.buttonEast.wasPressedThisFrame))Countermeasures();
        }
        Contact ClosestTarget(){Contact best=null;float cost=float.MaxValue;foreach(var c in contacts){if(c.dead||c.side==Allegiance.Friendly)continue;float d=Vector3.Distance(ship.position,c.body.position);if(d>9000)continue;float s=d+Vector3.Angle(ship.forward,c.body.position-ship.position)*35;if(s<cost){cost=s;best=c;}}return best;}
        void SelectTarget(){
            var valid=contacts.FindAll(c=>!c.dead&&c.side!=Allegiance.Friendly&&Vector3.Distance(ship.position,c.body.position)<9000);if(valid.Count==0){Toast("NO CONTACTS IN RANGE");return;}
            int index=target==null?-1:valid.IndexOf(target);target=valid[(index+1)%valid.Count];lockProgress=0;audioDirector.Cue("lock");
        }
        public bool FireMissile(){
            if(Time.time<missileAt)return false;
            if(missiles<=0){Toast("MISSILES DEPLETED / USE CANNON");return false;}
            if(target==null||target.dead||lockProgress<1){Toast("HOLD TARGET IN LOCK CIRCLE");return false;}
            if(target.side==Allegiance.Unknown||target.side==Allegiance.Friendly){Toast("FIRE INHIBITED / IDENTIFICATION REQUIRED");return false;}
            missiles--;shotsFired++;missileAt=Time.time+.85f;LaunchMissile(ship.position+ship.forward*8-ship.up,ship.forward,target,false);audioDirector.Cue("launch");return true;
        }
        public void FireCannon(){
            if(Time.time<cannonAt||cannon<=0)return;cannonAt=Time.time+.075f;cannon--;shotsFired++;audioDirector.Cue("gun");
            Contact best=null;float range=1800;foreach(var c in contacts){if(c.dead||c.side==Allegiance.Friendly||c.side==Allegiance.Unknown)continue;var d=c.body.position-ship.position;float angle=Vector3.Angle(ship.forward,d);if(d.magnitude<range&&angle<4.5f){best=c;range=d.magnitude;}}
            Vector3 end=best!=null?best.body.position:ship.position+ship.forward*1500;Tracer(ship.position+ship.forward*9-ship.up*.5f,end);
            if(best!=null){hits++;DamageContact(best,10);}
        }
        public void Countermeasures(){
            if(Time.time<flareAt||flares<=0)return;flares--;flareAt=Time.time+2.5f;warningUntil=0;audioDirector.Cue("launch");
            foreach(var p in projectiles)if(p.hostile&&Vector3.Distance(p.body.position,ship.position)<2300){p.hostile=false;p.target=null;p.direction=(p.body.forward-ship.right*.7f-Vector3.up*.4f).normalized;p.life=Mathf.Max(p.life,6);}
            for(int i=0;i<6;i++)Effect(ship.position-ship.forward*8+ship.right*(i-2.5f)*9,3);
            Toast("CHAFF / FLARE DISPENSED");
        }
        void LaunchMissile(Vector3 pos,Vector3 direction,Contact destination,bool hostile){
            if(projectiles.Count>=32)return;
            var body=VisualEffects.Missile(transform);body.position=pos;body.rotation=Quaternion.LookRotation(direction);
            var trail=body.gameObject.AddComponent<TrailRenderer>();trail.time=3;trail.startWidth=.65f;trail.endWidth=3;trail.minVertexDistance=5;trail.sharedMaterial=VisualEffects.Smoke;trail.textureMode=LineTextureMode.Stretch;trail.startColor=new Color(.75f,.78f,.79f,.48f);trail.endColor=new Color(.65f,.69f,.72f,0);trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            projectiles.Add(new MissileBody{body=body,target=destination,hostile=hostile,direction=direction,trail=trail,speed=hostile?360:560});
            if(hostile){warningUntil=Time.time+7;Radio("VEER-2","Missile launch! Break and use countermeasures!");}
        }
        void TickMissiles(float dt){
            for(int i=projectiles.Count-1;i>=0;i--){var p=projectiles[i];p.life+=dt;bool remove=p.life>11;Vector3 old=p.body.position;Vector3 aim=p.hostile?ship.position:p.target!=null&&!p.target.dead?p.target.body.position:old+p.direction*1000;
                Vector3 wanted=(aim-old).normalized;p.direction=Vector3.RotateTowards(p.direction,wanted,dt*(p.hostile?1.15f:2.8f),0);Vector3 next=old+p.direction*p.speed*dt;p.body.position=next;p.body.rotation=Quaternion.LookRotation(p.direction);
                if((p.hostile||p.target!=null&&!p.target.dead)&&DistanceToSegment(aim,old,next)<(p.hostile?22:30)){
                    if(p.hostile)DamagePlayer(save.settings.difficulty==0?12:save.settings.difficulty==1?23:32,"MISSILE HIT");else{hits++;DamageContact(p.target,70);}
                    Effect(aim,12);remove=true;
                }
                if(next.y<GroundHeight(next)){Effect(next,8);remove=true;}
                if(remove){Destroy(p.body.gameObject);projectiles.RemoveAt(i);}
            }
        }
        static float DistanceToSegment(Vector3 point,Vector3 a,Vector3 b){Vector3 d=b-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,d)/Mathf.Max(.001f,d.sqrMagnitude));return Vector3.Distance(point,a+d*t);}
        public Contact SpawnOne(Allegiance side,Vector3 pos,string label=null){
            var body=side==Allegiance.Ground?new GameObject("Military relay").transform:WorldFactory.Aircraft(contactRoot,"Radar contact",side==Allegiance.Hostile||side==Allegiance.Unknown);
            body.SetParent(contactRoot);body.position=pos;body.rotation=Quaternion.Euler(0,side==Allegiance.Friendly?heading:heading+65,0);
            if(side==Allegiance.Ground){body.position=new Vector3(pos.x,GroundHeight(pos)+8,pos.z);WorldFactory.Box("Relay shelter",body,Vector3.zero,new Vector3(42,16,35),WorldFactory.metal);WorldFactory.Box("Antenna mast",body,new Vector3(0,24,0),new Vector3(2,48,2),WorldFactory.dark);WorldFactory.Ball("Relay dish",body,new Vector3(0,48,0),new Vector3(22,12,5),WorldFactory.white);WorldFactory.Ball("Beacon",body,new Vector3(0,56,0),Vector3.one*3,WorldFactory.red);}
            var c=new Contact{body=body,side=side,name=label??(side==Allegiance.Friendly?"VEER-2":side==Allegiance.Unknown?"UNKNOWN 01":side==Allegiance.Ground?"VANA RELAY":"BANDIT "+(contacts.Count+1).ToString("00")),speed=side==Allegiance.Ground?0:side==Allegiance.Unknown?110:135,health=side==Allegiance.Ground?100:mission==0?60:100,phase=contacts.Count*1.7f,fireAt=Time.time+15+contacts.Count*3};contacts.Add(c);return c;
        }
        void SpawnFriendly(){friendlyHealth=100;SpawnOne(Allegiance.Friendly,ship.position-ship.forward*130+ship.right*90+Vector3.up*30,"VEER-2");}
        void SpawnThreats(){for(int i=0;i<Mission.All[mission].hostiles;i++)SpawnOne(Allegiance.Hostile,ship.position+ship.forward*(1600+i*400)+ship.right*((i%2==0?1:-1)*(300+i*150))+Vector3.up*(80+i*40));
            for(int i=0;i<Mission.All[mission].installations;i++)SpawnOne(Allegiance.Ground,new Vector3(-450+i*850,0,3500+i*700),"VANA "+(i+1));}
        void TickContacts(float dt){
            foreach(var c in contacts){if(c.dead||c.side==Allegiance.Ground)continue;
                float distance=Vector3.Distance(c.body.position,ship.position);
                if(c.side==Allegiance.Friendly){c.state=friendlyHealth<25?"RETREAT":health<35?"DEFEND":"FORMATION";Vector3 anchor=ship.position-ship.forward*(friendlyHealth<25?450:150)+ship.right*100+ship.up*35;if(stage==Stage.Startup)anchor=Launch+new Vector3(130,3,100);c.body.position=Vector3.Lerp(c.body.position,anchor,dt*1.2f);c.body.rotation=Quaternion.Slerp(c.body.rotation,ship.rotation,dt*2);c.speed=speed;
                    if(stage==Stage.Combat&&target!=null&&target.side==Allegiance.Hostile&&!target.dead&&Time.time>c.fireAt&&Vector3.Distance(c.body.position,target.body.position)<2200){c.state="ENGAGE";c.fireAt=Time.time+6;Tracer(c.body.position,target.body.position);DamageContact(target,12);}
                    continue;}
                if(Time.time>c.decisionAt){c.decisionAt=Time.time+.25f;
                    bool threatened=target==c&&lockProgress>.7f;
                    if(c.side==Allegiance.Unknown){c.state="PATROL";c.desired=Zone+new Vector3(Mathf.Sin(Time.time*.05f+c.phase)*1500,120,Mathf.Cos(Time.time*.05f+c.phase)*1500);}
                    else if(c.health<30){c.state="RETREAT";c.desired=ship.position+(c.body.position-ship.position).normalized*2300+Vector3.up*250;}
                    else if(threatened&&distance<1800){c.state="EVADE";c.desired=c.body.position+c.body.forward*800+c.body.right*Mathf.Sin(Time.time*.5f+c.phase)*1000+Vector3.up*150;}
                    else if(distance<500){c.state="BREAK";c.desired=c.body.position+c.body.forward*1100+c.body.right*900+Vector3.up*200;}
                    else if(distance>2600){c.state="INTERCEPT";c.desired=ship.position+ship.forward*speed*3;}
                    else{c.state="ATTACK";c.desired=ship.position+ship.forward*speed*1.3f+ship.right*Mathf.Sin(Time.time*.2f+c.phase)*400;}
                    c.desired.y=Mathf.Clamp(c.desired.y,GroundHeight(c.body.position+c.body.forward*400)+250,2600);
                }
                Vector3 direction=c.desired-c.body.position;if(direction.sqrMagnitude>1)c.body.rotation=Quaternion.RotateTowards(c.body.rotation,Quaternion.LookRotation(direction),dt*(c.state=="EVADE"?30:22));c.body.position+=c.body.forward*c.speed*dt;
                if(c.body.position.y<GroundHeight(c.body.position)+80)c.body.position+=Vector3.up*dt*90;
                if(c.side==Allegiance.Hostile&&mission!=0&&Time.time>c.fireAt&&distance<4500&&stage!=Stage.Land&&stage!=Stage.Taxi&&stage!=Stage.Return){c.fireAt=Time.time+(save.settings.difficulty==0?25:save.settings.difficulty==1?18:12)+c.phase;LaunchMissile(c.body.position+c.body.forward*12,(ship.position-c.body.position).normalized,null,true);}
                if(c.side==Allegiance.Hostile&&mission!=0&&Time.time>damageAt+22&&distance<1800&&stage==Stage.Combat){damageAt=Time.time;friendlyHealth=Mathf.Max(0,friendlyHealth-(save.settings.difficulty+1)*3);if(friendlyHealth<30)Radio("VEER-2","Taking damage. I need support!");if(friendlyHealth<=0)Fail("FORMATION LOST / WINGMAN DOWN");}
            }
        }
        public void DamageContact(Contact c,float amount){if(c==null||c.dead)return;c.health-=amount;Effect(c.body.position,3);if(c.health>0)return;c.dead=true;Effect(c.body.position,24);c.body.gameObject.SetActive(false);score+=c.side==Allegiance.Ground?750:500;if(c.side==Allegiance.Ground)groundKills++;else kills++;if(target==c){target=null;lockProgress=0;}Radio("VEER-2",c.side==Allegiance.Ground?"Designated relay disabled.":"Splash confirmed. Good work, Veer-1.");audioDirector.Cue("boom");}
        public void DamagePlayer(float amount,string reason){health=Mathf.Max(0,health-amount);Toast(reason);if(health<=0){Effect(ship.position,28);Fail("AIRFRAME LOST / CRITICAL DAMAGE");}}
        void UpdateCamera(float dt){
            cockpit.gameObject.SetActive(!chase);ShowShip(chase);Vector3 desired=chase?ship.position-ship.forward*38+ship.up*12:ship.position+ship.up*1.5f+ship.forward*4.5f;
            cam.transform.position=chase?Vector3.Lerp(cam.transform.position,desired,1-Mathf.Exp(-dt*6)):desired;
            Quaternion rot=chase?Quaternion.LookRotation(ship.position+ship.forward*90-cam.transform.position,ship.up):ship.rotation;
            cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,rot,1-Mathf.Exp(-dt*10));cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,chase?68:72,dt*3);
        }
        void Effect(Vector3 pos,float size){
            if(save.settings.effects==0&&size<5)return;
            TransientEffect fx=null;foreach(var item in effectPool)if(!item.gameObject.activeSelf){fx=item;break;}
            if(fx==null){if(effectPool.Count>=64)return;var g=VisualEffects.Impact(transform);fx=g.AddComponent<TransientEffect>();effectPool.Add(fx);}
            fx.Play(pos,size,size>5?.7f:.15f);
        }
        void Tracer(Vector3 a,Vector3 b){var g=new GameObject("Cannon tracer");g.transform.SetParent(transform);var l=g.AddComponent<LineRenderer>();l.positionCount=2;l.SetPosition(0,a);l.SetPosition(1,b);l.startWidth=.35f;l.endWidth=.15f;l.sharedMaterial=WorldFactory.glow;Destroy(g,.055f);}
        void CaptureCheckpoint(){
            checkpoint=new MissionCheckpoint{mission=mission,stage=stage,pos=ship.position,rotation=ship.rotation,waypoint=waypoint,speed=speed,throttle=throttle,health=health,fuel=fuel,time=missionTime,kills=kills,groundKills=groundKills,missiles=missiles,cannon=cannon,flares=flares,score=score,shots=shotsFired,hits=hits,engineOn=engineOn,routeLeg=routeLeg};
            foreach(var c in contacts)if(!c.dead)checkpoint.contacts.Add(new ContactSnapshot{side=c.side,pos=c.body.position,rot=c.body.rotation,health=c.health,name=c.name});
        }
        public void RestoreCheckpoint(){
            if(checkpoint==null){StartFlight();return;}var cp=checkpoint;ClearCombat();Time.timeScale=1;mode=Mode.Flight;menuPage="";mission=cp.mission;SetStage(cp.stage);ship.position=cp.pos;ship.rotation=cp.rotation;waypoint=cp.waypoint;speed=cp.speed;throttle=cp.throttle;health=Mathf.Max(50,cp.health);fuel=Mathf.Max(25,cp.fuel);missionTime=cp.time;kills=cp.kills;groundKills=cp.groundKills;missiles=cp.missiles;cannon=cp.cannon;flares=cp.flares;score=cp.score;shotsFired=cp.shots;hits=cp.hits;engineOn=cp.engineOn;assist=false;
            foreach(var s in cp.contacts){var c=SpawnOne(s.side,s.pos,s.name);c.health=s.health;c.body.rotation=s.rot;}if(!contacts.Exists(c=>c.side==Allegiance.Friendly))SpawnFriendly();
            routeLeg=cp.routeLeg;warningUntil=0;missileAt=cannonAt=flareAt=0;friendlyHealth=100;checkpoint=cp;Radio("ASHVA CONTROL","Checkpoint restored. You have control, Veer-1.");
        }
    }
    public class TransientEffect:MonoBehaviour {public void Play(Vector3 pos,float size,float duration){GetComponent<LayeredImpact>().Play(pos,size);}}
}

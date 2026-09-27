using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sindoor {
    public partial class OperationGame : MonoBehaviour {
        public static OperationGame Instance;
        public const string Disclaimer="THIS GAME IS A FICTIONALIZED INTERPRETATION INSPIRED BY PUBLICLY REPORTED EVENTS. CHARACTERS, LOCATIONS, MISSIONS, DIALOGUE AND GAMEPLAY SCENARIOS HAVE BEEN FICTIONALIZED FOR ENTERTAINMENT.";
        public Mode mode=Mode.Menu;
        public Stage stage;
        public SaveData save;
        public Transform world,ship,cockpit,pilot,commander;
        public Camera cam;
        public Light sun;
        public AudioDirector audioDirector;
        public int mission, shot, kills, groundKills, score, shotsFired, hits, missiles=12, cannon=900, flares=18;
        public float speed, throttle, health=100, fuel=100, lockProgress, missionTime, stageTime, shotTime, totalTime;
        public bool assist, engineOn, radarOpen=true, chase, returning, testing;
        public Vector3 waypoint;
        public string objective, radioSpeaker="ASHVA CONTROL", radioText="", alert="", menuPage="", failReason="";
        float radioUntil, toastUntil;
        string toast="";
        int lastShot=-1;
        [NonSerialized] public List<Contact> contacts=new List<Contact>();
        [NonSerialized] public List<MissileBody> projectiles=new List<MissileBody>();
        [NonSerialized] public Contact target;
        Transform contactRoot;
        MissionCheckpoint checkpoint;
        Renderer[] airframeRenderers;
        bool? airframeVisible;
        int routeLeg;
        public static readonly Vector3 Launch=new Vector3(0,3,-5450), Zone=new Vector3(0,850,2000), Approach=new Vector3(0,230,-7900);
        static readonly Vector3[][] PatrolRoutes={
            new[]{new Vector3(-2000,800,-800),new Vector3(3000,1000,3500),new Vector3(-3000,950,7000),Zone},
            new[]{new Vector3(-4500,1100,-1000),new Vector3(5000,1200,5000),new Vector3(-4000,1000,9500),Zone},
            new[]{new Vector3(-1800,750,-1500),new Vector3(-1200,650,1000),new Vector3(800,550,4500),new Vector3(-1600,750,7500),Zone},
            new[]{Zone},new[]{Approach}
        };
        public string evidencePath;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Instance==null)new GameObject("Operation Sindoor / game director").AddComponent<OperationGame>();}
        void Awake(){
            if(Instance!=null){Destroy(gameObject);return;} Instance=this;DontDestroyOnLoad(gameObject);
            bool assetReview=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--asset-review");
            testing=assetReview||Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--campaign-test");
            evidencePath=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"../Artifacts":"../../../Artifacts"));
            save=testing?new SaveData():Saves.Read();WorldFactory.Initialize();world=WorldFactory.Build();DontDestroyOnLoad(world.gameObject);
            contactRoot=new GameObject("Active contacts").transform;contactRoot.SetParent(transform);
            ship=WorldFactory.Aircraft(transform,"KESTREL / VEER-1");ship.position=new Vector3(55,3,-5030);
            airframeRenderers=ship.GetComponentsInChildren<Renderer>();
            cam=new GameObject("Flight and cinematic camera").AddComponent<Camera>();cam.transform.SetParent(transform);cam.nearClipPlane=.08f;cam.farClipPlane=24000;cam.fieldOfView=62;cam.gameObject.AddComponent<AudioListener>();
            var cameraData=cam.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            cockpit=WorldFactory.Cockpit(cam);cockpit.gameObject.SetActive(false);
            sun=new GameObject("Sun").AddComponent<Light>();sun.transform.SetParent(transform);sun.type=LightType.Directional;sun.intensity=1.35f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(18,-35,0);sun.color=new Color(1,.74f,.52f);
            var volume=new GameObject("Cinematic grade").AddComponent<Volume>();volume.transform.SetParent(transform);volume.isGlobal=true;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom=volume.profile.Add<Bloom>();bloom.intensity.Override(.22f);bloom.threshold.Override(1.2f);
            var tone=volume.profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
            var vignette=volume.profile.Add<Vignette>();vignette.intensity.Override(.2f);vignette.smoothness.Override(.4f);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.57f,.65f);RenderSettings.ambientEquatorColor=new Color(.3f,.34f,.35f);RenderSettings.ambientGroundColor=new Color(.18f,.17f,.14f);
            var skyShader=Shader.Find("Skybox/Procedural");if(skyShader){RenderSettings.skybox=new Material(skyShader);RenderSettings.skybox.SetFloat("_AtmosphereThickness",1.25f);RenderSettings.skybox.SetFloat("_SunSize",.025f);}
            RenderSettings.sun=sun;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.000045f;RenderSettings.fogColor=new Color(.48f,.49f,.48f);
            pilot=WorldFactory.Pilot(transform,new Vector3(52,1,-5020),"Squadron Leader Aarya Sen / VEER-1");
            commander=WorldFactory.Pilot(transform,WorldFactory.Room+new Vector3(2,0,5.5f),"Wing Commander Dev Rao",true);commander.rotation=Quaternion.Euler(0,180,0);
            audioDirector=gameObject.AddComponent<AudioDirector>();audioDirector.Initialize(save.settings);ApplySettings(!testing);SetWeather(0);
            string scene=SceneManager.GetActiveScene().name;
            if(scene.StartsWith("Mission")&&int.TryParse(scene.Substring(7),out var m)){mission=Mathf.Clamp(m-1,0,4);StartFlight();}
            else if(scene=="Intro"||scene=="Briefing"||scene=="AircraftSelection"){BeginMission(0,scene=="Intro");}
            else if(scene=="Ending"){mode=Mode.Tribute;shotTime=0;}
            else if(scene=="Credits")mode=Mode.Credits;
            if(assetReview)gameObject.AddComponent<AssetReview>();else if(testing)StartCoroutine(CampaignValidation());
        }
        void Update(){
            float dt=Mathf.Min(Time.deltaTime,testing?.2f:.05f);totalTime+=dt;
            bool rebinding=binding!="";TickInterface();
            audioDirector.Mix(engineOn?throttle:0,mode==Mode.Flight,stage==Stage.Combat||stage==Stage.Strike);
            if(!rebinding&&(Controls.Down(Key.Escape)||(Controls.Pad!=null&&Controls.Pad.startButton.wasPressedThisFrame))){
                if(mode==Mode.Flight){mode=Mode.Paused;Time.timeScale=0;Cursor.visible=true;}
                else if(mode==Mode.Paused){if(menuPage!="")CloseMenuPage();else Resume();}
                else if(mode==Mode.Menu&&menuPage!=""){CloseMenuPage();}
                else if(mode==Mode.Credits){ToMenu();}
            }
            if(mode==Mode.Flight){missionTime+=dt;stageTime+=dt;TickFlight(dt);if(mode!=Mode.Flight)return;TickContacts(dt);TickMissiles(dt);TickMission(dt);UpdateCamera(dt);}
            else if(mode==Mode.Cinematic)TickCinematic(dt);
            else if(mode==Mode.Menu||mode==Mode.Credits){MenuCamera(dt);}
            else if(mode==Mode.Tribute){shotTime+=dt;TributeCamera(dt);if(shotTime>30||(shotTime>3&&Controls.Confirm)){mode=Mode.Credits;menuPage="";}}
            else if(mode==Mode.Results||mode==Mode.Failed){cam.transform.RotateAround(ship.position,Vector3.up,dt*4);cam.transform.LookAt(ship.position);}
            if(mode!=Mode.Flight&&mode!=Mode.Paused)ShowShip(true);
        }
        public void BeginMission(int index,bool opening=false){
            Time.timeScale=1;mission=Mathf.Clamp(index,0,4);save.currentMission=mission;menuPage="";ClearCombat();SetWeather(mission);
            mode=Mode.Cinematic;shot=opening?0:3;shotTime=0;lastShot=-1;engineOn=false;chase=false;cockpit.gameObject.SetActive(false);
            ship.position=Launch;ship.rotation=Quaternion.identity;pilot.gameObject.SetActive(true);
            if(!testing)TrySave();
        }
        public void StartFlight(){
            ClearCombat();mode=Mode.Flight;Time.timeScale=1;menuPage="";health=100;fuel=100;speed=0;throttle=0;engineOn=false;assist=false;chase=false;returning=false;
            missiles=mission==3?8:12;cannon=900;flares=mission==3?12:18;kills=groundKills=score=shotsFired=hits=0;missionTime=0;lockProgress=0;target=null;warningUntil=0;damageAt=Time.time;missileAt=cannonAt=flareAt=0;
            ship.position=Launch;ship.rotation=Quaternion.identity;pilot.gameObject.SetActive(false);SetWeather(mission);
            if(mission==3){ship.position=new Vector3(0,1000,4000);ship.rotation=Quaternion.Euler(0,180,0);speed=180;throttle=.7f;engineOn=true;SetStage(Stage.Combat);SpawnThreats();CaptureCheckpoint();}
            else if(mission==4){ship.position=Approach+new Vector3(0,120,-1800);speed=130;throttle=.45f;engineOn=true;SetStage(Stage.Return);}
            else SetStage(Stage.Startup);
            SpawnFriendly();CaptureCheckpoint();cam.transform.position=ship.position-ship.forward*35+Vector3.up*12;Cursor.visible=true;
        }
        public void SetStage(Stage next){
            stage=next;stageTime=0;target=null;lockProgress=0;
            switch(next){
                case Stage.Startup:objective="START ENGINE  /  PRESS ENTER";Radio("ASHVA CONTROL","Veer-1, checklist complete. Press ENTER to start the engine.");break;
                case Stage.Takeoff:objective="TAKE OFF  /  CLIMB TO 180 M";waypoint=Launch+new Vector3(0,600,3500);Radio("ASHVA CONTROL","Cleared for takeoff. W raises throttle. At 220 km/h, hold DOWN ARROW to raise the nose. H enables route assist.");break;
                case Stage.Transit:routeLeg=0;objective=mission==2?"FOLLOW THE VALLEY  /  STAY BELOW 1,200 M":"REACH THE PATROL ZONE";waypoint=PatrolRoutes[mission][0];Radio("VEER-2",mission==2?"Hold the valley corridor. The settlement east of us is protected. Only marked military relays are cleared.":"On your wing. Follow the diamond. Mouse steers, A/D roll, Q/E yaw. H toggles route assist.");break;
                case Stage.Identify:objective="IDENTIFY THE UNKNOWN CONTACT  /  F";Radio("ASHVA CONTROL","Unknown contact ahead. Face the contact and press F. Hold it in the lock circle to identify.");break;
                case Stage.Combat:objective=mission==0?"DESTROY THE TRAINING DRONE":"PROTECT THE FORMATION  /  CLEAR HOSTILES";Radio("VEER-2",mission==0?"Contact identified: an uncrewed training drone. F selects. Keep the contact ahead, then R launches. SPACE fires cannon; X releases countermeasures.":"Hostiles confirmed. Protect the formation. R for missile; X for countermeasures. H can help you turn toward a selected contact.");break;
                case Stage.Strike:objective="DISABLE BOTH MARKED RELAYS";waypoint=new Vector3(0,600,3600);Radio("COMMAND","Vana One and Vana Two are designated military relays. Fire only on the marked targets. Civilian areas are off limits.");break;
                case Stage.Egress:objective="EXIT THE OBJECTIVE AREA";waypoint=new Vector3(-1000,750,-800);Radio("VEER-2","Objectives confirmed. Turn southwest; follow the exit marker.");break;
                case Stage.Return:objective="RETURN TO ASHVA  /  APPROACH FIX";waypoint=Approach;returning=true;Radio("ASHVA CONTROL","Veer-1, return to base. Approach runway 00 from the south. H enables the guided approach.");break;
                case Stage.Land:landingLeg=0;objective="LAND ON RUNWAY 00  /  180–360 KM/H";waypoint=new Vector3(0,420,-10100);Radio("ASHVA CONTROL","Runway is clear. Fly the approach circuit and align north. Touch down below 360 km/h. H guides the landing; you may take over at any time.");break;
                case Stage.Taxi:objective="TAXI AND SHUT DOWN";waypoint=new Vector3(0,3,-4750);Radio("ASHVA CONTROL","Welcome home, Veer-1. Hold S to brake. Press ENTER once stopped to shut down.");break;
            }
            Toast("OBJECTIVE UPDATED");if(next!=Stage.Startup&&next!=Stage.Takeoff)CaptureCheckpoint();
        }
        void TickMission(float dt){
            if(stage==Stage.Startup&&Controls.Confirm){engineOn=true;SetStage(Stage.Takeoff);}
            if(stage==Stage.Takeoff&&ship.position.y>180&&ship.position.z>-4200){SetStage(Stage.Transit);}
            if(stage==Stage.Transit){
                if(mission==2&&ship.position.y>1200){alert="DETECTION RISK / DESCEND BELOW 1,200 M";if(stageTime>15&&contacts.Count<4){SpawnOne(Allegiance.Hostile,ship.position+ship.forward*2000+Vector3.up*200);Radio("VEER-2","We have been detected. An interceptor is inbound.");}}
                if(Vector3.Distance(ship.position,waypoint)<900){
                    if(routeLeg+1<PatrolRoutes[mission].Length){routeLeg++;waypoint=PatrolRoutes[mission][routeLeg];objective=(mission==2?"VALLEY CORRIDOR":"PATROL ROUTE")+"  /  FIX "+(routeLeg+1)+" OF "+PatrolRoutes[mission].Length;Radio("VEER-2",mission==0?(routeLeg==1?"Keep your turns smooth. Bank with A or D, then use pitch to hold the horizon.":"For a burst of speed, hold LEFT SHIFT. Watch your fuel. C changes the view."):mission==2?"Corridor checkpoint reached. Hold below twelve hundred metres. We stay clear of the settlement.":"Formation steady. Moving to the next patrol fix.");CaptureCheckpoint();}
                    else if(mission==0){SpawnOne(Allegiance.Unknown,ship.position+ship.forward*1700);SetStage(Stage.Identify);}
                    else {SpawnThreats();SetStage(mission==2?Stage.Strike:Stage.Combat);CaptureCheckpoint();}}
            }
            if(stage==Stage.Identify&&target!=null&&lockProgress>=1){target.side=Allegiance.Hostile;SetStage(Stage.Combat);}
            if(stage==Stage.Strike&&groundKills>=Mission.All[mission].installations){SetStage(Stage.Combat);}
            if(stage==Stage.Combat&&kills>=Mission.All[mission].hostiles){SetStage(mission==2?Stage.Egress:Stage.Return);}
            if(stage==Stage.Egress&&Vector3.Distance(ship.position,waypoint)<950){SetStage(Stage.Return);}
            if(stage==Stage.Return&&Vector3.Distance(ship.position,Approach)<650){SetStage(Stage.Land);}
            if(stage==Stage.Land&&ship.position.y<7&&Mathf.Abs(ship.position.x)<43&&ship.position.z>-5700&&ship.position.z<-3300&&speed<105){ship.position=new Vector3(ship.position.x,3,ship.position.z);ship.rotation=Quaternion.Euler(0,ship.eulerAngles.y,0);SetStage(Stage.Taxi);}
            if(stage==Stage.Land&&ship.position.z>-3250){Radio("ASHVA CONTROL","Go around. Return to the approach fix and try again.");SetStage(Stage.Return);}
            if(stage==Stage.Taxi&&speed<4&&(Controls.Confirm||assist)){engineOn=false;CompleteMission();}
            if(fuel<=0&&engineOn){throttle=0;alert="FUEL DEPLETED";}
            if(Vector3.Distance(ship.position,Vector3.zero)>23000){Radio("ASHVA CONTROL","The mission boundary is behind you. Return to the assigned corridor.");assist=true;}
        }
        public void CompleteMission(){
            score+=Mathf.RoundToInt(health*10)+Mathf.Max(0,1800-(int)missionTime);save.completed|=1<<mission;save.unlocked=Mathf.Max(save.unlocked,Mathf.Min(5,mission+2));save.currentMission=Mathf.Min(4,mission+1);save.bestScores[mission]=Mathf.Max(save.bestScores[mission],score);if(!testing)TrySave();
            if(mission==4){mode=Mode.Cinematic;shot=20;shotTime=0;lastShot=-1;pilot.gameObject.SetActive(true);}else{mode=Mode.Results;audioDirector.Cue("success");}
        }
        public void Fail(string why){if(mode!=Mode.Flight)return;failReason=why;mode=Mode.Failed;cockpit.gameObject.SetActive(false);ShowShip(true);cam.transform.position=ship.position-ship.forward*35+Vector3.up*12;cam.transform.LookAt(ship.position);audioDirector.Cue("boom");}
        public void Resume(){mode=Mode.Flight;menuPage="";Time.timeScale=1;}
        public void ToMenu(){mode=Mode.Menu;Time.timeScale=1;menuPage="";ClearCombat();engineOn=false;ship.position=new Vector3(55,3,-5030);ship.rotation=Quaternion.identity;cockpit.gameObject.SetActive(false);ShowShip(true);pilot.gameObject.SetActive(true);pilot.position=ship.position+new Vector3(-5,-2,6);}
        public void Radio(string speaker,string words){radioSpeaker=speaker;radioText=words;radioUntil=Time.time+Mathf.Max(7,words.Length*.06f);audioDirector.Cue("radio");}
        public void Toast(string text){toast=text;toastUntil=Time.time+3;audioDirector.Cue("ui");}
        void TrySave(){try{Saves.Write(save);}catch(Exception e){Debug.LogWarning("Save failed: "+e.Message);toast="SAVE UNAVAILABLE / CHECK DISK ACCESS";toastUntil=Time.time+6;}}
        void ClearCombat(){foreach(var c in contacts)if(c.body)Destroy(c.body.gameObject);contacts.Clear();foreach(var p in projectiles)if(p.body)Destroy(p.body.gameObject);projectiles.Clear();target=null;}
        void ShowShip(bool visible){if(airframeVisible==visible)return;airframeVisible=visible;foreach(var r in airframeRenderers)r.enabled=visible;var presentation=ship.GetComponent<AircraftPresentation>();if(presentation)presentation.SetVisible(visible);}
        public void ApplySettings(bool display=true){
            var p=save.settings;QualitySettings.vSyncCount=p.vsync?1:0;Application.targetFrameRate=60;QualitySettings.shadowDistance=p.shadows==0?0:p.shadows==1?180:450;QualitySettings.globalTextureMipmapLimit=p.textures;cam.farClipPlane=Mathf.Clamp(p.viewDistance,9000,26000);
            var urp=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;if(urp){urp.renderScale=p.quality==0?.75f:p.quality==1?1:1.15f;urp.msaaSampleCount=p.antialiasing==0?1:p.antialiasing==1?2:4;urp.shadowDistance=QualitySettings.shadowDistance;}
            if(display){int[] widths={1280,1600,1920};int i=Mathf.Clamp(p.resolution,0,2);Screen.SetResolution(widths[i],widths[i]*9/16,p.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}
        }
        void SetWeather(int i){
            var m=Mission.All[i];cam.backgroundColor=m.sky;RenderSettings.fogColor=m.sky;RenderSettings.fogDensity=i==3?.00009f:.000045f;
            sun.color=i==3?new Color(.65f,.76f,.86f):i==2?new Color(1,.63f,.39f):new Color(1,.85f,.65f);sun.intensity=i==3?.7f:1.4f;sun.transform.rotation=Quaternion.Euler(i==2?12:i==0?16:35,-35,0);
            if(RenderSettings.skybox){RenderSettings.skybox.SetColor("_SkyTint",i==3?new Color(.35f,.4f,.47f):new Color(.48f,.52f,.58f));RenderSettings.skybox.SetFloat("_Exposure",i==3?.65f:1.1f);}
        }
    }
    [Serializable] public class ContactSnapshot {public Allegiance side;public Vector3 pos;public Quaternion rot;public float health;public string name;}
    [Serializable] public class MissionCheckpoint {
        public int mission,kills,groundKills,missiles,cannon,flares,score,shots,hits,routeLeg;public Stage stage;public Vector3 pos,waypoint;public Quaternion rotation;public float speed,throttle,health,fuel,time;public bool engineOn;
        public List<ContactSnapshot> contacts=new List<ContactSnapshot>();
    }
}

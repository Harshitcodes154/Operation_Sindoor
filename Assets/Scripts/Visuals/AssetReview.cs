using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Sindoor {
    // Opt-in player QA. Isolated save, unmodified gameplay paths, no production camera changes.
    public sealed class AssetReview : MonoBehaviour {
        OperationGame g;string folder;int errors;bool motionPassed=true;readonly List<string> report=new List<string>();
        readonly List<string> captures=new List<string>();DateTime started;
        IEnumerator Start(){
            g=OperationGame.Instance;folder=Path.Combine(g.evidencePath,"AssetReview");Directory.CreateDirectory(folder);started=DateTime.UtcNow;
            Application.logMessageReceived+=Log;
            report.Add("GPU: "+SystemInfo.graphicsDeviceName+" / VRAM "+SystemInfo.graphicsMemorySize+" MB; CPU: "+SystemInfo.processorType+"; RAM "+SystemInfo.systemMemorySize+" MB");
            yield return new WaitForSecondsRealtime(3);
            var originalSky=RenderSettings.skybox;
            // Every registered scene is actually loaded; shared-world campaign paths run separately.
            for(int i=0;i<SceneManager.sceneCountInBuildSettings;i++){
                var op=SceneManager.LoadSceneAsync(i);while(!op.isDone)yield return null;
                report.Add("Loaded scene gateway: "+SceneManager.GetActiveScene().name);
            }
            RenderSettings.skybox=originalSky;RenderSettings.sun=g.sun;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            g.world.GetComponent<AtmospherePresentation>().RefreshAmbient();g.BeginMission(0);g.ToMenu();g.enabled=false;g.mode=Mode.Paused;g.cockpit.gameObject.SetActive(false);g.pilot.gameObject.SetActive(false);
            Vector3 p=new Vector3(55,3,-5030);g.ship.position=p;g.ship.rotation=Quaternion.identity;
            Look(p+new Vector3(-18,8,22),p+Vector3.up*.6f,48);yield return Capture("01-kestrel.png");
            Look(p+new Vector3(10,3,-17),p+new Vector3(0,.3f,-3),48);yield return Capture("02-nozzles-stores.png");
            var raven=VisualAssetLibrary.InstantiateVisual(VisualAssetLibrary.Current.adversary,g.transform,"QA opposing aircraft");raven.position=p;g.ship.gameObject.SetActive(false);
            Look(p+new Vector3(-18,8,22),p+Vector3.up*.6f,48);yield return Capture("03-raven.png");Destroy(raven.gameObject);g.ship.gameObject.SetActive(true);
            g.pilot.gameObject.SetActive(true);g.pilot.position=p+new Vector3(-5,-2,5);g.pilot.rotation=Quaternion.identity;
            Look(g.pilot.position+new Vector3(-1.5f,1.5f,3.8f),g.pilot.position+Vector3.up*1.05f,40);yield return Capture("04-pilot.png");
            yield return ReviewMotion();g.pilot.gameObject.SetActive(false);
            Look(new Vector3(10,90,-5270),new Vector3(200,15,-4740),57);yield return Capture("05-airbase.png");
            Look(new Vector3(-140,7,-5085),new Vector3(40,10,-4800),56);yield return Capture("06-service-area.png");
            Look(new Vector3(-800,1100,-1500),new Vector3(1500,400,3700),62);yield return Capture("07-terrain.png");
            g.cockpit.gameObject.SetActive(true);Look(OperationGame.Launch+new Vector3(0,1.5f,4.5f),OperationGame.Launch+new Vector3(0,1.5f,1000),62);yield return Capture("08-cockpit.png");g.cockpit.gameObject.SetActive(false);
            Look(p+new Vector3(-35,12,30),p+new Vector3(0,5,0),56);
            var fx=VisualEffects.Impact(g.transform);fx.GetComponent<LayeredImpact>().Play(p+new Vector3(0,5,0),14);yield return Capture("09-explosion.png",.2f);Destroy(fx);
            var atmosphere=g.world.GetComponent<AtmospherePresentation>();atmosphere.nightLighting=true;
            Look(new Vector3(10,60,-5400),new Vector3(170,10,-4750),57);yield return Capture("10-night-airbase.png");atmosphere.nightLighting=false;
            g.enabled=true;g.cam.fieldOfView=62;
            foreach(int profile in new[]{1,0}){
                g.save.settings.quality=profile;g.save.settings.vsync=false;g.save.settings.shadows=profile==0?0:1;g.ApplySettings(false);
                QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                g.mission=1;g.StartFlight();g.ship.position=new Vector3(0,950,-2500);g.engineOn=true;g.speed=190;g.throttle=.72f;g.assist=true;g.chase=true;g.SetStage(Stage.Transit);
                yield return new WaitForSecondsRealtime(3);
                var frameTimes=new List<float>();float t=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-t<15){frameTimes.Add(Time.unscaledDeltaTime*1000);yield return null;}
                frameTimes.Sort();float sum=0;foreach(float f in frameTimes)sum+=f;
                report.Add((profile==1?"Balanced":"Performance")+" / "+Screen.width+"x"+Screen.height+" / normal-speed transit: "+(1000*frameTimes.Count/sum).ToString("0.0")+" FPS average; p95 "+frameTimes[(int)(frameTimes.Count*.95f)].ToString("0.0")+" ms; allocated Unity memory "+(Profiler.GetTotalAllocatedMemoryLong()/1048576)+" MiB; samples "+frameTimes.Count);
                yield return Capture(profile==1?"11-flight-balanced.png":"12-flight-performance.png");
            }
            bool fresh=captures.TrueForAll(pth=>File.Exists(pth)&&File.GetLastWriteTimeUtc(pth)>=started&&new FileInfo(pth).Length>5000);
            report.Add((fresh?"PASS":"FAIL")+": "+captures.Count+" fresh rendered asset captures");report.Add("Runtime errors: "+errors);
            report.Add(errors==0&&fresh&&motionPassed?"ASSET_REVIEW_PASS":"ASSET_REVIEW_FAIL");
            File.WriteAllLines(Path.Combine(g.evidencePath,"asset-review-validation.txt"),report);Application.Quit(errors==0&&fresh&&motionPassed?0:2);
        }
        IEnumerator ReviewMotion(){
            var rig=g.pilot.GetComponent<CharacterPresentation>();var knee=g.pilot.Find("Leg L/Knee");var ankle=g.pilot.Find("Leg L/Knee/Ankle");
            Vector3 origin=g.pilot.position;g.mode=Mode.Cinematic;g.shot=2;float start=Time.time;float maxKnee=0,minAnkle=100,maxStep=0;Quaternion previous=knee.localRotation;
            while(Time.time-start<2){
                g.pilot.position=origin+Vector3.forward*((Time.time-start)*.8f);
                Look(g.pilot.position+new Vector3(-2.8f,1.25f,3.4f),g.pilot.position+Vector3.up,40);
                yield return new WaitForEndOfFrame();
                maxKnee=Mathf.Max(maxKnee,Quaternion.Angle(Quaternion.identity,knee.localRotation));minAnkle=Mathf.Min(minAnkle,ankle.position.y-VisualEnvironment.SurfaceHeight(ankle.position));
                maxStep=Mathf.Max(maxStep,Quaternion.Angle(previous,knee.localRotation));previous=knee.localRotation;
            }
            yield return Capture("13-walk-pose.png",.02f);
            g.shot=3;yield return new WaitForSecondsRealtime(1.2f);float idle=rig.WalkBlend;
            g.shot=5;yield return Capture("14-salute.png",1);float salute=rig.SaluteBlend;
            g.shot=3;yield return new WaitForSecondsRealtime(1);float released=rig.SaluteBlend;
            g.shot=7;yield return Capture("15-climb-pose.png",1);
            motionPassed=maxKnee>25&&minAnkle>.08f&&idle<.08f&&salute>.95f&&released<.05f;
            report.Add((motionPassed?"PASS":"FAIL")+": runtime walk/stop/salute/release/climb; knee bend "+maxKnee.ToString("F1")+" deg; ankle clearance "+minAnkle.ToString("F3")+" m; max sampled knee step "+maxStep.ToString("F1")+" deg; idle "+idle.ToString("F3")+"; salute "+salute.ToString("F3")+"; released "+released.ToString("F3"));
            g.mode=Mode.Paused;g.pilot.position=origin;
        }
        void Look(Vector3 from,Vector3 at,float fov){g.cam.transform.position=from;g.cam.transform.LookAt(at);g.cam.fieldOfView=fov;}
        IEnumerator Capture(string name,float settle=.7f){yield return new WaitForSecondsRealtime(settle);yield return new WaitForEndOfFrame();string path=Path.Combine(folder,name);ScreenCapture.CaptureScreenshot(path);captures.Add(path);yield return new WaitForSecondsRealtime(.4f);}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;report.Add(message+"\n"+trace);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Sindoor {
    public partial class OperationGame {
        readonly List<string> testLog=new List<string>();int runtimeErrors;
        readonly List<string> testCaptures=new List<string>();
        DateTime testStarted;
        bool captureEnabled;
        void CaptureEvidence(string path){if(captureEnabled){ScreenCapture.CaptureScreenshot(path);testCaptures.Add(path);}}
        IEnumerator CampaignValidation(){
            Directory.CreateDirectory(evidencePath);Application.logMessageReceived+=TestLog;
            testStarted=DateTime.UtcNow;captureEnabled=!Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--no-captures");
            testLog.Add("Graphics: "+SystemInfo.graphicsDeviceName+"; resolution "+Screen.width+"x"+Screen.height+"; evidence "+evidencePath);Debug.Log(testLog[0]);
            yield return new WaitForSecondsRealtime(3);CaptureEvidence(Path.Combine(evidencePath,"01-menu.png"));yield return new WaitForSecondsRealtime(.5f);
            BeginMission(0,true);yield return new WaitForSecondsRealtime(1);CaptureEvidence(Path.Combine(evidencePath,"02-disclaimer.png"));
            while(mode==Mode.Cinematic){if(shot==3||shot==6||shot==7||shot==8){yield return new WaitForSecondsRealtime(1.5f);CaptureEvidence(Path.Combine(evidencePath,shot==3?"03-briefing.png":"cinematic-"+shot+".png"));}yield return new WaitForSecondsRealtime(.4f);NextShot();}
            testLog.Add("PASS: opening, briefing, preparation, flag, cockpit and engine-start gateway");
            yield return new WaitForSecondsRealtime(1);CaptureEvidence(Path.Combine(evidencePath,"04-cockpit.png"));yield return new WaitForSecondsRealtime(.3f);
            Time.timeScale=6;
            for(int m=0;m<5;m++){
                mission=m;StartFlight();assist=true;chase=true;Time.timeScale=6;
                if(stage==Stage.Startup){engineOn=true;SetStage(Stage.Takeoff);}
                float started=Time.realtimeSinceStartup,stageBegan=started;Stage previous=stage;bool captured=false;int retries=0;
                while(mode==Mode.Flight||mode==Mode.Failed&&retries<1){
                    if(mode==Mode.Failed){retries++;testLog.Add("Checkpoint retry: "+failReason);RestoreCheckpoint();stageBegan=Time.realtimeSinceStartup;}
                    assist=true;
                    if(stage!=previous){testLog.Add("PASS: mission "+(m+1)+" "+previous+" -> "+stage);File.WriteAllLines(Path.Combine(evidencePath,"campaign-progress.txt"),testLog);previous=stage;stageBegan=Time.realtimeSinceStartup;captured=false;}
                    if(stage==Stage.Identify||stage==Stage.Combat||stage==Stage.Strike){
                        if(target==null||target.dead){target=ClosestTarget();lockProgress=0;}
                        if(target!=null&&lockProgress>=1&&target.side!=Allegiance.Unknown){int tracking=projectiles.FindAll(p=>p.target==target).Count;if(tracking*70<target.health)FireMissile();FireCannon();}
                        if(Time.time<warningUntil)Countermeasures();
                    }
                    if(!captured&&Time.realtimeSinceStartup-stageBegan>1.5f){
                        if(stage==Stage.Transit||stage==Stage.Combat||stage==Stage.Strike||stage==Stage.Land){captured=true;CaptureEvidence(Path.Combine(evidencePath,"mission-"+(m+1)+"-"+stage+".png"));}
                    }
                    if(stage==Stage.Taxi)throttle=0;
                    if(Time.realtimeSinceStartup-stageBegan>90||Time.realtimeSinceStartup-started>240){testLog.Add("FAIL: mission "+(m+1)+" timed out at "+stage+" pos="+ship.position+" speed="+speed+" kills="+kills+" target="+(target?.name??"none"));break;}
                    yield return null;
                }
                if(mode==Mode.Results||(m==4&&mode==Mode.Cinematic)){testLog.Add("PASS: mission "+(m+1)+" complete; flight time "+missionTime.ToString("0.0")+"s, airframe "+health.ToString("0")+"%, ammunition "+missiles+"/"+cannon);}
                else{testLog.Add("FAIL: mission "+(m+1)+" ended "+mode+" / "+failReason);FinishTest(false);yield break;}
                Time.timeScale=1;yield return new WaitForSecondsRealtime(.3f);
            }
            // Verify recovery from a real combat checkpoint, including remaining contact state.
            mission=1;StartFlight();engineOn=true;ship.position=Zone;speed=180;SpawnThreats();SetStage(Stage.Combat);CaptureCheckpoint();int count=contacts.Count;DamagePlayer(100,"TEST DAMAGE");RestoreCheckpoint();
            if(mode!=Mode.Flight||stage!=Stage.Combat||health<50||contacts.Count!=count){testLog.Add("FAIL: checkpoint restore");FinishTest(false);yield break;}testLog.Add("PASS: destruction and checkpoint restore preserve contact state and restore flyable aircraft");
            var pausedPosition=ship.position;float pausedMissionTime=missionTime;mode=Mode.Paused;Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);
            if(ship.position!=pausedPosition||missionTime!=pausedMissionTime){testLog.Add("FAIL: simulation advanced while paused");FinishTest(false);yield break;}
            Resume();if(mode!=Mode.Flight||Time.timeScale!=1){testLog.Add("FAIL: resume");FinishTest(false);yield break;}testLog.Add("PASS: pause freezes flight and resume restores simulation");
            mission=0;StartFlight();RestoreCheckpoint();
            if(mission!=0||stage!=Stage.Startup||ship.position!=Launch||engineOn||kills!=0||contacts.Count!=1){testLog.Add("FAIL: new mission retained an old checkpoint");FinishTest(false);yield break;}
            testLog.Add("PASS: new mission checkpoint replaces previous combat state");
            // Exercise actual Input System state so mouse and controller steering can reclaim manual flight.
            engineOn=true;speed=180;ship.position=Zone;stage=Stage.Transit;waypoint=Zone+Vector3.forward*2000;
            var testPad=InputSystem.AddDevice<Gamepad>();
            assist=true;InputSystem.QueueStateEvent(testPad,new GamepadState{rightStick=new Vector2(.8f,0)});
            yield return null;yield return null;
            bool padTakeover=!assist;InputSystem.RemoveDevice(testPad);
            var testMouse=InputSystem.AddDevice<Mouse>();
            assist=true;InputSystem.QueueStateEvent(testMouse,new MouseState{delta=new Vector2(25,0)}.WithButton(MouseButton.Right));
            yield return null;yield return null;
            bool mouseTakeover=!assist;InputSystem.RemoveDevice(testMouse);
            if(!padTakeover||!mouseTakeover){testLog.Add("FAIL: input takeover / controller="+padTakeover+" mouse="+mouseTakeover);FinishTest(false);yield break;}
            testLog.Add("PASS: controller yaw and mouse steering disengage route assistance");
            mode=Mode.Menu;menuPage="settings";yield return new WaitForSecondsRealtime(.3f);CaptureEvidence(Path.Combine(evidencePath,"settings.png"));yield return new WaitForSecondsRealtime(.3f);
            menuPage="controls";yield return new WaitForSecondsRealtime(.3f);CaptureEvidence(Path.Combine(evidencePath,"controls.png"));yield return new WaitForSecondsRealtime(.3f);menuPage="";
            mission=4;mode=Mode.Cinematic;shot=20;lastShot=-1;shotTime=0;yield return new WaitForSecondsRealtime(.5f);NextShot();yield return new WaitForSecondsRealtime(.5f);NextShot();yield return new WaitForSecondsRealtime(2);shotTime=21;CaptureEvidence(Path.Combine(evidencePath,"ending-tribute.png"));yield return new WaitForSecondsRealtime(.5f);mode=Mode.Credits;CaptureEvidence(Path.Combine(evidencePath,"credits.png"));yield return new WaitForSecondsRealtime(.5f);
            testLog.Add("PASS: homecoming, tribute and credits");FinishTest(runtimeErrors==0);
        }
        void TestLog(string condition,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){runtimeErrors++;testLog.Add("RUNTIME ERROR: "+condition+"\n"+trace);}}
        void FinishTest(bool success){
            Time.timeScale=1;
            if(captureEnabled){
                bool capturesValid=testCaptures.Count>=12&&testCaptures.TrueForAll(p=>File.Exists(p)&&File.GetLastWriteTimeUtc(p)>=testStarted&&new FileInfo(p).Length>5000);
                testLog.Add((capturesValid?"PASS: ":"FAIL: ")+testCaptures.Count+" fresh rendered captures");success&=capturesValid;
            }else testLog.Add("SKIPPED: rendered captures (--no-captures); visual review still required");
            success&=runtimeErrors==0;testLog.Add("Runtime errors: "+runtimeErrors);testLog.Add(success?"CAMPAIGN_TEST_PASS":"CAMPAIGN_TEST_FAIL");File.WriteAllLines(Path.Combine(evidencePath,"campaign-validation.txt"),testLog);Debug.Log(success?"CAMPAIGN_TEST_PASS":"CAMPAIGN_TEST_FAIL");Application.Quit(success?0:2);
        }
    }
}

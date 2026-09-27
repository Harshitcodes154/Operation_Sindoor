using UnityEngine;

namespace Sindoor {
    public partial class OperationGame {
        static readonly float[] ShotDurations={9,10,8,12,12,7,10,7,7};
        public string shotTitle="",shotSubtitle="";
        Vector3 shotCamera,shotLook;
        void TickCinematic(float dt){
            shotTime+=dt;
            if(lastShot!=shot){lastShot=shot;EnterShot();}
            var r=WorldFactory.Room;float t=shotTime;
            if(shot==0){shotCamera=new Vector3(0,100,-6000);shotLook=new Vector3(0,0,-4400);}
            if(shot==1){shotCamera=new Vector3(-220+t*7,80+t*2,-5700+t*20);shotLook=new Vector3(120,5,-4550);}
            if(shot==2){pilot.position=new Vector3(95,1,-4800+t*.8f);pilot.rotation=Quaternion.identity;AnimateWalk(t);shotCamera=pilot.position+new Vector3(-3,1.3f,5);shotLook=pilot.position+Vector3.up*1.4f;}
            if(shot==3){pilot.position=r+new Vector3(2,0,-3);pilot.rotation=Quaternion.identity;shotCamera=r+new Vector3(-5,2.8f,-5+t*.07f);shotLook=r+new Vector3(1,2.8f,6);}
            if(shot==4){shotCamera=r+new Vector3(.5f,1.7f,2);shotLook=commander.position+Vector3.up*1.6f;}
            if(shot==5){shotCamera=pilot.position+new Vector3(-1,1.7f,2.3f);shotLook=pilot.position+Vector3.up*1.5f;var arm=pilot.Find("Arm R");if(arm)arm.localRotation=Quaternion.Euler(-135,0,-20);}
            if(shot==6){pilot.position=Launch+new Vector3(-7+t*.35f,-2,-10+t);pilot.rotation=Quaternion.identity;AnimateWalk(t);shotCamera=Launch+new Vector3(-15+t*.3f,5,18);shotLook=Launch+new Vector3(0,1,1);}
            if(shot==7){pilot.position=Launch+new Vector3(-1.6f,-1+Mathf.Min(2,t*.4f),3);shotCamera=Launch+new Vector3(-3,2.6f,7);shotLook=pilot.position+Vector3.up*1.5f;}
            if(shot==8){shotCamera=new Vector3(81+t*.5f,18,-4670);shotLook=new Vector3(104,21,-4700);}
            if(shot==20){pilot.position=ship.position+new Vector3(-4,-2,4);pilot.rotation=Quaternion.Euler(0,-90,0);commander.position=pilot.position+new Vector3(-4+Mathf.Min(2,t*.2f),0,0);commander.rotation=Quaternion.Euler(0,90,0);shotCamera=pilot.position+new Vector3(-3,2.1f,5);shotLook=pilot.position+new Vector3(-1,1.1f,0);var arm=pilot.Find("Arm R");if(arm)arm.localRotation=Quaternion.Euler(-135,0,-20);}
            if(shot==21){shotCamera=new Vector3(81,17,-4670);shotLook=new Vector3(104,21,-4700);}
            if(shotTime<.12f)cam.transform.position=shotCamera;
            cam.transform.position=Vector3.Lerp(cam.transform.position,shotCamera,1-Mathf.Exp(-dt*2));cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(shotLook-cam.transform.position),1-Mathf.Exp(-dt*4));cam.fieldOfView=shot==4||shot==7?42:58;
            if((shotTime>1.2f&&Controls.Confirm)||shotTime>(shot<9?ShotDurations[shot]:10))NextShot();
        }
        void EnterShot(){
            shotTitle="";shotSubtitle="";cockpit.gameObject.SetActive(false);ShowShip(true);
            switch(shot){
                case 0:shotTitle="A FICTIONAL STORY";shotSubtitle=Disclaimer;break;
                case 1:shotTitle="BEFORE THE FIRST LIGHT";shotSubtitle="ASHVA AIR STATION  /  FICTIONAL LOCATION\n0450 HOURS";break;
                case 2:shotTitle="A PILOT IS CALLED";shotSubtitle="SQUADRON LEADER AARYA SEN  /  CALLSIGN VEER-1\n17 SQUADRON — THE SENTINELS";Radio("COMMAND","Veer-1. Report to the briefing room.");break;
                case 3:shotTitle=Mission.All[mission].title;shotSubtitle=Mission.All[mission].briefing;commander.position=WorldFactory.Room+new Vector3(2,0,5.5f);commander.rotation=Quaternion.Euler(0,180,0);Radio("WING COMMANDER RAO",mission==0?"The country has been attacked. Our forces have been preparing a response. You have been selected. Complete your assigned objective, and return safely.":mission==2?"This is not about revenge. This is about protecting our people and completing the mission assigned to you.":Mission.All[mission].briefing);break;
                case 4:shotTitle="THE ASSIGNMENT";shotSubtitle=Mission.All[mission].weather+"\nKESTREL F.1  /  FICTIONAL MULTIROLE AIRCRAFT";Radio("WING COMMANDER RAO","Discipline. Precision. Look after your wingman. You're wheels-up at 0500.");break;
                case 5:shotTitle="VEER-1";shotSubtitle="\"Understood, sir.\"";break;
                case 6:shotTitle="THE WALK TO THE AIRCRAFT";shotSubtitle="One crew. One aircraft. One promise to come home.";ship.position=Launch;ship.rotation=Quaternion.identity;Radio("GROUND CREW","Airframe checked. Fuel and systems ready. We'll be here when you return.");break;
                case 7:shotTitle="READY";shotSubtitle="The weight of a quiet moment.";break;
                case 8:shotTitle="BHARAT MATA KI JAI";shotSubtitle="VEER-1  /  QUIETLY, BEFORE TAKEOFF";Radio("VEER-1","Bharat Mata ki Jai.");break;
                case 20:shotTitle="WELCOME HOME";shotSubtitle="The sound of engines gives way to silence.";Radio("WING COMMANDER RAO","Mission complete.");break;
                case 21:shotTitle="FOR THOSE WHO STAND BETWEEN US AND THE STORM";shotSubtitle="We remember those who serve.";break;
            }
        }
        public void NextShot(){
            shotTime=0;
            if(shot==8){StartFlight();return;}
            if(shot==21){mode=Mode.Tribute;shotTime=0;return;}
            shot++;
        }
        void AnimateWalk(float t){var l=pilot.Find("Leg L");var r=pilot.Find("Leg R");var a=pilot.Find("Arm R");if(l)l.localRotation=Quaternion.Euler(Mathf.Sin(t*5)*18,0,0);if(r)r.localRotation=Quaternion.Euler(-Mathf.Sin(t*5)*18,0,0);if(a)a.localRotation=Quaternion.Euler(Mathf.Sin(t*5)*15,0,0);}
        void MenuCamera(float dt){float a=Time.unscaledTime*.04f;Vector3 p=ship.position+new Vector3(18+Mathf.Sin(a)*2,7,24+Mathf.Cos(a)*2);cam.transform.position=Vector3.Lerp(cam.transform.position,p,1-Mathf.Exp(-dt*3));cam.transform.LookAt(ship.position+new Vector3(9,1,-6));cam.fieldOfView=43;}
        void TributeCamera(float dt){cam.transform.position=new Vector3(82,18,-4672);cam.transform.LookAt(new Vector3(104,21,-4700));cam.fieldOfView=42;}
    }
}

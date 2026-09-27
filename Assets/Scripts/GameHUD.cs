using UnityEngine;

namespace Sindoor {
    public partial class OperationGame {
        string ActionKey(string action)=>(string)typeof(Preferences).GetField(action).GetValue(save.settings);
        void HudUI(){
            if(mission==3&&save.settings.effects>0)for(int i=0;i<28;i++){
                float x=(i*137.3f+Time.time*50)%1600,y=(i*83.1f+Time.time*430)%800;
                Line(new Vector2(x,y),new Vector2(x-8,y+35),new Color(.65f,.8f,.85f,.13f));
            }
            Fill(30,25,1540,75,new Color(.017f,.032f,.041f,.88f));Fill(30,25,3,75,saffron);
            Tracking("VEER-1",49,39,14,2,ivory);Text(Mission.All[mission].subtitle,49,68,420,21,10,muted);
            Text(objective,418,34,1119,35,19,ivory,TextAnchor.MiddleRight);
            Text((assist?"ASSIST ACTIVE":"MANUAL FLIGHT")+"    ["+ActionKey("assist").ToUpperInvariant()+"]",1050,72,487,19,10,assist?saffron:muted,TextAnchor.MiddleRight);
            // The flight symbology leaves the central forward view open.
            Text(Mathf.RoundToInt(speed*3.6f).ToString("000"),242,338,188,53,37,mint,TextAnchor.MiddleCenter);
            Text("AIRSPEED  KM/H",242,397,188,20,10,mint,TextAnchor.MiddleCenter);Fill(261,390,150,1,mint);
            Text(Mathf.RoundToInt(ship.position.y).ToString("0000"),1170,338,188,53,37,mint,TextAnchor.MiddleCenter);
            Text("ALT  M  /  AGL "+Mathf.RoundToInt(AGL),1140,397,250,20,10,mint,TextAnchor.MiddleCenter);Fill(1189,390,150,1,mint);
            Text("HDG "+Mathf.RoundToInt(heading).ToString("000"),650,126,300,31,18,mint,TextAnchor.MiddleCenter);
            for(int i=-3;i<=3;i++){
                float x=800+i*55;Fill(x,163,1,i==0?14:7,mint);
                Text(((Mathf.RoundToInt(heading/10)*10+i*10+360)%360).ToString("000"),x-27,182,54,20,10,mint,TextAnchor.MiddleCenter);
            }
            var faint=new Color(mint.r,mint.g,mint.b,.55f);Circle(new Vector2(800,425),30,faint);
            Line(new Vector2(737,425),new Vector2(784,425),mint);Line(new Vector2(816,425),new Vector2(863,425),mint);
            Line(new Vector2(800,440),new Vector2(800,453),mint);
            float pitchAngle=Mathf.DeltaAngle(0,ship.eulerAngles.x),bank=Mathf.DeltaAngle(0,ship.eulerAngles.z);
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(bank,new Vector2(800,425));
            for(int i=-2;i<=2;i++){
                float y=425+(pitchAngle+i*10)*3;
                if(y<230||y>590)continue;
                Line(new Vector2(689,y),new Vector2(735,y),faint);Line(new Vector2(865,y),new Vector2(911,y),faint);
            }
            GUI.matrix=matrix;
            if(stage!=Stage.Combat&&stage!=Stage.Identify&&stage!=Stage.Strike&&stage!=Stage.Startup&&stage!=Stage.Taxi)WaypointMarker();
            foreach(var c in contacts)if(!c.dead)DrawContact(c);
            if(target!=null&&!target.dead){
                Fill(1240,541,330,166,new Color(.017f,.036f,.044f,.84f));Fill(1240,541,2,166,saffron);
                Text("TRACK "+(target.side==Allegiance.Unknown?"UNKNOWN":"CONFIRMED"),1260,558,285,24,10,muted);
                Text(target.name,1260,592,285,29,20,ivory);
                Text((Vector3.Distance(ship.position,target.body.position)/1000).ToString("0.0")+" KM    BRG "+Bearing(target.body.position).ToString("000"),1260,630,285,23,12,muted);
                Text(lockProgress>=1?"LOCK ACQUIRED  ["+ActionKey("missile").ToUpperInvariant()+"]":"TRACKING  "+Mathf.RoundToInt(lockProgress*100)+"%",1260,668,285,24,12,lockProgress>=1?saffron:muted);
            }
            if(alert!=""){
                Fill(480,229,640,40,new Color(.24f,.045f,.023f,.9f));Border(480,229,640,40,new Color(.92f,.36f,.18f,.55f));
                Text(alert.Replace("X COUNTERMEASURES",ActionKey("counter").ToUpperInvariant()+" COUNTERMEASURES"),495,230,610,38,15,saffron,TextAnchor.MiddleCenter,true);
            }
            if(Time.time<toastUntil)Text(toast,400,610,800,34,14,saffron,TextAnchor.MiddleCenter);
            Fill(30,797,1540,77,new Color(.015f,.031f,.041f,.93f));Fill(30,797,1540,1,hairline);
            Telemetry("AIRFRAME",Mathf.RoundToInt(health)+"%",51,health<35?saffron:ivory);
            Fill(178,841,100,4,new Color(.18f,.26f,.27f));Fill(178,841,health,4,health<35?saffron:mint);
            Telemetry("THROTTLE",Mathf.RoundToInt(throttle*100)+"%",331,ivory);Telemetry("FUEL",Mathf.RoundToInt(fuel)+"%",520,ivory);
            Telemetry("MISSILES ["+ActionKey("missile").ToUpperInvariant()+"]",missiles.ToString("00"),710,ivory);
            Telemetry("CANNON ["+ActionKey("fire").ToUpperInvariant()+"]",cannon.ToString("000"),920,ivory);
            Telemetry("DECOYS ["+ActionKey("counter").ToUpperInvariant()+"]",flares.ToString("00"),1130,ivory);
            Telemetry("WINGMAN",Mathf.RoundToInt(friendlyHealth)+"%",1370,mint);
            if(radarOpen)RadarUI();
            if(Time.time<radioUntil){
                Fill(425,684,774,88,new Color(.015f,.03f,.038f,.92f));Fill(425,684,3,88,saffron);
                Text(radioSpeaker,445,698,727,21,10,saffron);Text(radioText,445,727,727,39,15,ivory);
            }
            if(stage==Stage.Startup){
                Fill(503,486,594,111,new Color(.015f,.031f,.04f,.94f));Border(503,486,594,111,hairline);
                Tracking("PRE-FLIGHT CHECK COMPLETE",591,505,11,1.5f,muted);
                Text("ENTER / A   START ENGINE",517,547,566,34,22,saffron,TextAnchor.MiddleCenter);
            }
            if(stage==Stage.Taxi)Text("S  BRAKE     ENTER / A  SHUT DOWN",425,549,750,41,21,saffron,TextAnchor.MiddleCenter);
            Text("C  CAMERA     TAB  RADAR     ESC  PAUSE",40,880,950,18,9,muted);
            Text(System.TimeSpan.FromSeconds(missionTime).ToString(@"mm\:ss"),1390,879,170,20,11,muted,TextAnchor.MiddleRight);
        }
        void Telemetry(string label,string value,float x,Color color){Text(label,x,810,207,21,10,muted);Text(value,x,838,175,29,23,color);}
        int Bearing(Vector3 pos){var d=pos-ship.position;return Mathf.RoundToInt((Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg+360)%360);}
        void WaypointMarker(){
            var p=cam.WorldToViewportPoint(waypoint);float x=Mathf.Clamp(p.x*1600,448,1152),y=Mathf.Clamp((1-p.y)*900,287,597);
            if(p.z<0){x=448;y=450;}
            var c=new Vector2(x,y);Line(c+Vector2.up*10,c+Vector2.right*10,mint,2);Line(c+Vector2.right*10,c-Vector2.up*10,mint,2);Line(c-Vector2.up*10,c-Vector2.right*10,mint,2);Line(c-Vector2.right*10,c+Vector2.up*10,mint,2);
            Text((p.z<0?"TURN BACK / ":"")+(Vector3.Distance(ship.position,waypoint)/1000).ToString("0.0")+" KM",x-135,y+20,270,28,12,mint,TextAnchor.MiddleCenter);
        }
        void DrawContact(Contact c){
            var v=cam.WorldToViewportPoint(c.body.position);float d=Vector3.Distance(ship.position,c.body.position);
            if(v.z<0||v.x<.04||v.x>.96||v.y<.17||v.y>.86||d>9000)return;
            Color color=c.side==Allegiance.Friendly?mint:c.side==Allegiance.Unknown?ivory:saffron;float x=v.x*1600,y=(1-v.y)*900,s=c==target?23:11;
            // Corner brackets keep aircraft silhouettes readable.
            foreach(int side in new[]{-1,1})foreach(int up in new[]{-1,1}){
                var corner=new Vector2(x+side*s,y+up*s);Line(corner,corner-new Vector2(side*7,0),color);Line(corner,corner-new Vector2(0,up*7),color);
            }
            if(c==target&&lockProgress>=1)Circle(new Vector2(x,y),30,color);
            Text(c.name,x-110,y+s+6,220,23,10,color,TextAnchor.MiddleCenter);
        }
        void RadarUI(){
            var center=new Vector2(144,675);Fill(30,560,230,212,new Color(.012f,.03f,.035f,.92f));Border(30,560,230,212,hairline);
            Circle(center,81,new Color(.24f,.46f,.4f));Circle(center,41,new Color(.13f,.26f,.23f));
            Line(center+Vector2.left*81,center+Vector2.right*81,new Color(.15f,.3f,.27f));Line(center+Vector2.up*81,center+Vector2.down*81,new Color(.15f,.3f,.27f));
            float a=Time.time*.8f;Line(center,center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*81,new Color(.2f,.5f,.38f));
            Line(center-Vector2.up*5,center+new Vector2(-4,4),ivory,2);Line(center-Vector2.up*5,center+new Vector2(4,4),ivory,2);
            foreach(var c in contacts){
                if(c.dead)continue;Vector3 rel=Quaternion.Inverse(Quaternion.Euler(0,heading,0))*(c.body.position-ship.position);if(rel.magnitude>9000)continue;
                Vector2 dot=center+new Vector2(rel.x,-rel.z)/9000*79;Color col=c.side==Allegiance.Friendly?mint:c.side==Allegiance.Unknown?ivory:saffron;
                Fill(dot.x-2,dot.y-2,4,4,col);if(c==target)Circle(dot,7,col);
            }
            Text("RADAR / 9 KM",45,575,201,20,10,mint);Text("N "+Mathf.RoundToInt(heading).ToString("000"),45,747,199,18,9,muted,TextAnchor.MiddleRight);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sindoor {
    public partial class OperationGame {
        readonly Color ink=new Color(.022f,.031f,.042f), ivory=new Color(.94f,.93f,.88f), muted=new Color(.64f,.68f,.7f);
        readonly Color saffron=new Color(.92f,.66f,.36f), mint=new Color(.55f,.92f,.8f), hairline=new Color(1,1,1,.14f);
        GUIStyle labelStyle,buttonStyle;
        Texture2D gradient,titleArt,radialShade;
        Font bodyFont,titleFont;
        string binding="",bindingMessage="",uiRoute="";
        int uiButtonCount,uiLastButtonCount,uiSelected,uiActivate=-1,selectedMission;
        bool keyboardNavigation;
        float pageOpened,hoverSoundAt;
        int hoverButton=-1;

        void InitUI(){
            if(labelStyle!=null)return;
            bodyFont=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},20);
            titleFont=Font.CreateDynamicFontFromOSFont(new[]{"Bahnschrift","Arial"},100);
            labelStyle=new GUIStyle(GUI.skin.label){font=bodyFont,richText=false,wordWrap=true,padding=new RectOffset(0,0,0,0)};
            buttonStyle=new GUIStyle(GUIStyle.none);
            titleArt=Resources.Load<Texture2D>("UI/TitleKeyArt");
            gradient=new Texture2D(256,1,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int i=0;i<256;i++)gradient.SetPixel(i,0,new Color(ink.r,ink.g,ink.b,Mathf.Lerp(.94f,0,Mathf.Pow(i/255f,1.4f))));
            gradient.Apply();
            radialShade=new Texture2D(128,72,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<72;y++)for(int x=0;x<128;x++){
                float dx=(x-64)/64f,dy=(y-36)/36f;
                radialShade.SetPixel(x,y,new Color(0,0,0,Mathf.Clamp01((dx*dx+dy*dy)*.25f)));
            }
            radialShade.Apply();
        }

        bool MenuActive=>mode==Mode.Menu||mode==Mode.Paused||mode==Mode.Results||mode==Mode.Failed||mode==Mode.Credits;
        void TickInterface(){
            string route=mode+"/"+menuPage;
            if(uiRoute!=route){uiRoute=route;uiSelected=0;uiActivate=-1;uiLastButtonCount=0;pageOpened=Time.unscaledTime;hoverButton=-1;}
            if(binding!=""){
                if(Keyboard.current!=null)foreach(var k in Keyboard.current.allKeys)if(k.wasPressedThisFrame){
                    if(k.keyCode==Key.Escape){binding="";bindingMessage="";return;}
                    if(IsReservedKey(k.keyCode)){bindingMessage="That key is reserved for flight. Choose another key.";return;}
                    string next=k.keyCode.ToString();var field=typeof(Preferences).GetField(binding);string old=(string)field.GetValue(save.settings);
                    foreach(var name in new[]{"fire","missile","lockTarget","counter","assist"}){
                        var other=typeof(Preferences).GetField(name);if(name!=binding&&(string)other.GetValue(save.settings)==next)other.SetValue(save.settings,old);
                    }
                    field.SetValue(save.settings,next);binding="";bindingMessage="Binding updated. Duplicate action keys are swapped.";return;
                }
                return;
            }
            if(!MenuActive)return;
            var pad=Controls.Pad;
            int direction=Controls.Down(Key.DownArrow)||Controls.Down(Key.Tab)||(pad!=null&&pad.dpad.down.wasPressedThisFrame)?1:Controls.Down(Key.UpArrow)||(pad!=null&&pad.dpad.up.wasPressedThisFrame)?-1:0;
            if(direction!=0&&uiLastButtonCount>0){keyboardNavigation=true;uiSelected=(uiSelected+direction+uiLastButtonCount)%uiLastButtonCount;audioDirector.Cue("hover");}
            if(Controls.Confirm&&uiLastButtonCount>0){keyboardNavigation=true;uiActivate=uiSelected;}
            if(Mouse.current!=null&&Mouse.current.delta.ReadValue().sqrMagnitude>2)keyboardNavigation=false;
            if(pad!=null&&pad.buttonEast.wasPressedThisFrame){if(menuPage!="")CloseMenuPage();else if(mode==Mode.Paused)Resume();else if(mode==Mode.Credits)ToMenu();}
        }
        static bool IsReservedKey(Key key)=>key==Key.None||key==Key.W||key==Key.S||key==Key.A||key==Key.D||key==Key.Q||key==Key.E||key==Key.UpArrow||key==Key.DownArrow||key==Key.LeftArrow||key==Key.RightArrow||key==Key.Enter||key==Key.Tab||key==Key.C||key==Key.LeftShift||key==Key.RightShift;
        void CloseMenuPage(){ApplySettings();if(!testing)TrySave();binding="";bindingMessage="";menuPage="";}

        void OnGUI(){
            InitUI();uiButtonCount=0;
            GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            var offset=new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0);
            if(offset.x>0||offset.y>0){GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
            GUI.matrix=Matrix4x4.TRS(offset,Quaternion.identity,new Vector3(scale,scale,1));
            try{
                if(mode==Mode.Menu||mode==Mode.Credits)MenuUI();
                else if(mode==Mode.Cinematic)CinematicUI();
                else if(mode==Mode.Tribute)TributeUI();
                else if(mode==Mode.Flight)HudUI();
                else if(mode==Mode.Paused){HudUI();PauseUI();}
                else if(mode==Mode.Results||mode==Mode.Failed)ResultUI();
                if(MenuActive){float fade=1-Mathf.Clamp01((Time.unscaledTime-pageOpened)/.24f);if(fade>0)Fill(0,0,1600,900,new Color(ink.r,ink.g,ink.b,fade*.55f));}
            }finally{
                if(Event.current.type==EventType.Repaint){uiLastButtonCount=uiButtonCount;uiSelected=Mathf.Clamp(uiSelected,0,Mathf.Max(0,uiLastButtonCount-1));}
                GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
            }
        }
        void Fill(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
        void Border(float x,float y,float w,float h,Color c){Fill(x,y,w,1,c);Fill(x,y+h-1,w,1,c);Fill(x,y,1,h,c);Fill(x+w-1,y,1,h,c);}
        void Text(string s,float x,float y,float w,float h,int size,Color c,TextAnchor align=TextAnchor.UpperLeft,bool bold=false){
            labelStyle.font=size>=38?titleFont:bodyFont;labelStyle.fontSize=size;labelStyle.normal.textColor=c;labelStyle.alignment=align;labelStyle.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            GUI.Label(new Rect(x,y,w,h),s,labelStyle);
        }
        void Tracking(string s,float x,float y,int size,float spacing,Color color){
            labelStyle.font=bodyFont;labelStyle.fontSize=size;labelStyle.fontStyle=FontStyle.Normal;
            foreach(char c in s){string t=c.ToString();float width=labelStyle.CalcSize(new GUIContent(t)).x;Text(t,x,y,width+4,size+8,size,color);x+=width+spacing;}
        }
        bool Button(string s,float x,float y,float w,float h,bool primary=false){
            int index=uiButtonCount++;var rect=new Rect(x,y,w,h);
            bool over=rect.Contains(Event.current.mousePosition),focused=keyboardNavigation&&uiSelected==index;
            bool highlight=over||focused;
            Fill(x,y,w,h,primary?new Color(.85f,.61f,.34f,highlight?.98f:.88f):highlight?new Color(.16f,.2f,.23f,.94f):new Color(.045f,.06f,.075f,.78f));
            Border(x,y,w,h,primary?new Color(1,.78f,.48f,.55f):highlight?saffron:hairline);
            if(!primary&&highlight)Fill(x,y,3,h,saffron);
            bool clicked=GUI.Button(rect,GUIContent.none,buttonStyle);
            if(uiActivate==index&&Event.current.type==EventType.Repaint){clicked=true;uiActivate=-1;}
            Text(s,x+20,y,w-50,h,15,primary?ink:ivory,TextAnchor.MiddleLeft,primary);
            Text(primary?">":"",x+w-32,y,18,h,20,ink,TextAnchor.MiddleCenter);
            if(over&&Event.current.type==EventType.Repaint&&hoverButton!=index&&Time.unscaledTime>hoverSoundAt){hoverButton=index;hoverSoundAt=Time.unscaledTime+.08f;audioDirector.Cue("hover");}
            if(clicked){uiActivate=-1;audioDirector.Cue("ui");}
            return clicked;
        }
        void Line(Vector2 a,Vector2 b,Color c,float width=1){var matrix=GUI.matrix;float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;GUIUtility.RotateAroundPivot(angle,a);Fill(a.x,a.y,(b-a).magnitude,width,c);GUI.matrix=matrix;}
        void Circle(Vector2 centre,float radius,Color c){for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;Line(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,centre+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,c);}}
        void Backdrop(bool darken=false){
            Fill(0,0,1600,900,ink);
            if(titleArt){float zoom=1.015f+Mathf.Sin(Time.unscaledTime*.075f)*.012f;GUI.DrawTexture(new Rect(800-800*zoom,450-450*zoom,1600*zoom,900*zoom),titleArt,ScaleMode.ScaleAndCrop);}
            GUI.DrawTexture(new Rect(0,0,1100,900),gradient);GUI.DrawTexture(new Rect(0,0,1600,900),radialShade);
            Fill(0,0,1600,100,new Color(.015f,.024f,.032f,.36f));Fill(0,818,1600,82,new Color(.015f,.024f,.032f,.72f));
            if(darken)Fill(0,0,1600,900,new Color(.015f,.025f,.038f,.78f));
            // Sparse drifting dust adds motion without obscuring navigation.
            for(int i=0;i<18;i++){float x=(i*113.7f+Time.unscaledTime*(3+i%3))%1600,y=125+(i*71.4f-Time.unscaledTime*5+10000)%650;Fill(x,y,1.4f,1.4f,new Color(1,.78f,.43f,.12f));}
        }
        void ShellHeader(string section){
            Fill(68,43,3,28,saffron);Tracking("S E N T I N E L",84,45,14,1,ivory);
            Tracking(section,1170,48,11,2,muted);Fill(68,96,1464,1,hairline);
        }
        void ShellFooter(string hint="ARROWS / D-PAD  NAVIGATE      ENTER / A  SELECT"){
            Fill(68,818,1464,1,hairline);Text(hint,68,841,1110,25,11,muted);
            Text("OPERATION SINDOOR  /  1.1",1230,841,302,25,11,muted,TextAnchor.MiddleRight);
        }
        void MenuUI(){
            bool sub=menuPage!=""||mode==Mode.Credits;Backdrop(sub);
            ShellHeader(sub?"SQUADRON OPERATIONS":"A FICTIONAL AVIATION STORY");
            if(mode==Mode.Credits||menuPage=="credits"){CreditsUI();ShellFooter();return;}
            if(menuPage=="settings"||menuPage=="controls"){SettingsUI();ShellFooter();return;}
            if(menuPage=="campaign"||menuPage=="missions"){CampaignUI();ShellFooter();return;}
            Tracking("A STORY OF DUTY AND RETURN",72,169,12,2.5f,saffron);
            Tracking("OPERATION",70,222,31,8,ivory);
            Text("SINDOOR",64,256,760,150,120,ivory,TextAnchor.UpperLeft,true);
            Fill(72,414,46,3,new Color(.77f,.2f,.12f));Fill(123,414,12,3,saffron);
            Text("BETWEEN HOME\nAND THE STORM.",72,449,610,76,26,ivory);
            Text("A country waits. A pilot answers.",73,544,620,30,15,muted);
            if(Button(save.completed==0?"BEGIN THE CAMPAIGN":"CONTINUE CAMPAIGN",72,599,452,62,true))BeginMission(save.currentMission,save.currentMission==0);
            if(Button("CAMPAIGN",72,677,216,48)){selectedMission=save.currentMission;menuPage="campaign";}
            if(Button("SETTINGS",302,677,222,48))menuPage="settings";
            if(Button("CONTROLS",72,738,140,42))menuPage="controls";
            if(Button("CREDITS",226,738,141,42))menuPage="credits";
            if(Button("EXIT",381,738,143,42)){TrySave();Application.Quit();}
            Fill(1178,679,354,106,new Color(.02f,.035f,.048f,.76f));Fill(1178,679,2,106,saffron);
            Tracking("17 SQUADRON",1199,699,11,2,saffron);
            Text("THE SENTINELS",1199,724,307,31,24,ivory);Text("ASHVA AIR STATION  /  0450 HOURS",1200,761,300,19,10,muted);
            ShellFooter("SINGLE PLAYER CAMPAIGN     /     KEYBOARD, MOUSE & CONTROLLER");
        }
        void CampaignUI(){
            Text("CAMPAIGN",70,126,1200,72,54,ivory);Tracking("FIVE MISSIONS. ONE WAY HOME.",74,207,12,2,saffron);
            for(int i=0;i<5;i++){
                float y=274+i*87;bool unlocked=i<save.unlocked,chosen=i==selectedMission;
                if(Button((i+1).ToString("00")+"     "+Mission.All[i].title,72,y,486,73,chosen))selectedMission=i;
                Text(!unlocked?"LOCKED":(save.completed&(1<<i))!=0?"COMPLETE":"AVAILABLE",380,y+49,150,16,9,chosen?ink:muted,TextAnchor.MiddleRight);
            }
            var m=Mission.All[selectedMission];Fill(596,272,936,430,new Color(.028f,.043f,.055f,.85f));Border(596,272,936,430,hairline);
            DrawTheatreMap(new Rect(610,285,500,400),selectedMission);
            Tracking("SORTIE "+(selectedMission+1).ToString("00"),1140,298,12,2,saffron);
            Text(m.title,1140,332,362,83,32,ivory);Text(m.weather,1140,428,360,39,12,muted);
            Text(m.briefing,1140,486,358,149,17,ivory);
            Text("BEST SCORE  "+save.bestScores[selectedMission].ToString("N0"),1140,662,360,24,12,saffron);
            if(Button("BACK",72,738,220,48))menuPage="";
            if(selectedMission<save.unlocked){if(Button("DEPLOY  /  "+(selectedMission+1).ToString("00"),1180,738,352,54,true))BeginMission(selectedMission,selectedMission==0);}
            else Text("Complete the preceding mission to unlock this sortie.",820,741,700,45,17,muted,TextAnchor.MiddleRight);
        }
        void DrawTheatreMap(Rect area,int index){
            GUI.BeginGroup(area);Fill(0,0,area.width,area.height,new Color(.035f,.06f,.07f));
            for(int x=0;x<area.width;x+=40)Fill(x,0,1,area.height,new Color(.5f,.8f,.8f,.055f));
            for(int y=0;y<area.height;y+=40)Fill(0,y,area.width,1,new Color(.5f,.8f,.8f,.055f));
            for(int n=0;n<10;n++)for(int x=0;x<480;x+=10){float y=65+n*27+Mathf.Sin(x*.016f+n*.63f)*31+Mathf.Cos(x*.031f+n)*14;float yy=65+n*27+Mathf.Sin((x+10)*.016f+n*.63f)*31+Mathf.Cos((x+10)*.031f+n)*14;Line(new Vector2(x,y),new Vector2(x+10,yy),new Color(.35f,.6f,.59f,.11f));}
            Vector2[] points={new Vector2(110,320),new Vector2(145,220),new Vector2(260,135),new Vector2(365,90),new Vector2(320,245)};
            for(int i=0;i<points.Length-1;i++)Line(points[i],points[i+1],new Color(saffron.r,saffron.g,saffron.b,.55f),2);
            for(int i=0;i<points.Length;i++){Circle(points[i],i==index?10:5,i==index?saffron:mint);if(i==index)Circle(points[i],16+Mathf.Sin(Time.unscaledTime*2)*2,new Color(saffron.r,saffron.g,saffron.b,.3f));}
            Text("N",447,20,28,28,13,muted);Line(new Vector2(459,54),new Vector2(459,35),muted);
            Text("ASHVA",80,341,170,20,11,ivory);Text("KESAR CORRIDOR",255,47,235,23,11,muted);Text("FICTIONAL THEATRE / SCHEMATIC",20,372,455,20,10,muted);
            GUI.EndGroup();
        }
        void SettingsUI(){
            bool controls=menuPage=="controls";
            Text(controls?"FLIGHT CONTROLS":"SETTINGS",72,126,1100,72,54,ivory);
            Tracking(controls?"YOUR AIRCRAFT. YOUR CONTROLS.":"MAKE THE COCKPIT YOUR OWN.",76,207,12,2,saffron);
            Fill(72,258,694,452,new Color(.03f,.045f,.057f,.9f));Fill(796,258,736,452,new Color(.03f,.045f,.057f,.9f));
            if(controls){
                Tracking("FLIGHT & CAMERA",96,277,12,1.5f,saffron);
                string[] keys={"W / S","UP / DOWN","A / D","Q / E","RIGHT MOUSE","LEFT SHIFT","C / TAB","ENTER / ESC"};
                string[] values={"Throttle up / down","Nose down / up","Roll left / right","Yaw left / right","Hold and move to steer","Afterburner","Camera / radar","Engine / pause"};
                for(int i=0;i<keys.Length;i++){float y=324+i*42;Text(keys[i],96,y,236,30,14,muted);Text(values[i],338,y,404,30,16,ivory);Fill(96,y+32,646,1,new Color(1,1,1,.05f));}
                Tracking("PRIMARY ACTIONS",822,277,12,1.5f,saffron);
                BindRow("CANNON","fire",save.settings.fire,316);BindRow("GUIDED MISSILE","missile",save.settings.missile,365);BindRow("TARGET / IDENTIFY","lockTarget",save.settings.lockTarget,414);BindRow("COUNTERMEASURES","counter",save.settings.counter,463);BindRow("ROUTE ASSIST","assist",save.settings.assist,512);
                Text("CONTROLLER",822,581,650,23,12,saffron);
                Text("Left stick: pitch / roll  ·  Right stick: yaw  ·  D-pad: throttle\nRT: cannon  ·  A: missile / confirm  ·  X: target  ·  B: flares\nLB: route assist  ·  R-stick click: camera  ·  Start: pause",822,614,683,77,14,muted);
                if(binding!="")Text("PRESS A KEY  /  ESC TO CANCEL",822,721,680,27,14,saffron);
                else if(bindingMessage!="")Text(bindingMessage,822,717,680,38,13,muted);
            }else{
                var p=save.settings;Tracking("DISPLAY & FLIGHT",96,277,12,1.5f,saffron);Tracking("AUDIO & INPUT",822,277,12,1.5f,saffron);
                p.quality=Cycle("QUALITY",new[]{"PERFORMANCE","BALANCED","HIGH"},p.quality,96,316);
                p.resolution=Cycle("RESOLUTION",new[]{"1280 × 720","1600 × 900","1920 × 1080"},p.resolution,96,358);
                p.fullscreen=Cycle("DISPLAY",new[]{"WINDOWED","FULLSCREEN"},p.fullscreen?1:0,96,400)==1;
                p.vsync=Cycle("VSYNC",new[]{"OFF","ON"},p.vsync?1:0,96,442)==1;
                p.difficulty=Cycle("DIFFICULTY",new[]{"EASY","NORMAL","HARD"},p.difficulty,96,484);
                p.shadows=Cycle("SHADOWS",new[]{"OFF","MEDIUM","HIGH"},p.shadows,96,526);
                p.effects=Cycle("EFFECTS",new[]{"LOW","FULL"},p.effects,96,568);
                p.antialiasing=Cycle("ANTI-ALIASING",new[]{"FXAA","FXAA + 2× MSAA","FXAA + 4× MSAA"},p.antialiasing,96,610);
                p.textures=Cycle("TEXTURES",new[]{"FULL","HALF","QUARTER"},p.textures,96,652);
                p.master=Slider("MASTER",p.master,822,321);p.music=Slider("MUSIC",p.music,822,365);p.sfx=Slider("SOUND EFFECTS",p.sfx,822,409);p.voice=Slider("RADIO",p.voice,822,453);
                p.sensitivity=Mathf.Max(.1f,Slider("MOUSE SENSITIVITY",p.sensitivity/1.5f,822,497)*1.5f);
                p.invertMouse=Cycle("INVERT MOUSE",new[]{"OFF","ON"},p.invertMouse?1:0,822,541)==1;
                p.mouseFlight=Cycle("MOUSE FLIGHT",new[]{"OFF","HOLD RIGHT BUTTON"},p.mouseFlight?1:0,822,583)==1;
                p.viewDistance=9000+Slider("VIEW DISTANCE",(p.viewDistance-9000)/17000,822,640)*17000;
            }
            if(Button("SAVE & BACK",72,751,270,48,true))CloseMenuPage();
            if(Button(controls?"SETTINGS":"CONTROLS",362,751,250,48)){binding="";menuPage=controls?"settings":"controls";}
        }
        int Cycle(string name,string[] options,int value,float x,float y){value=Mathf.Clamp(value,0,options.Length-1);Text(name,x,y,238,35,12,muted,TextAnchor.MiddleLeft);if(Button(options[value],x+244,y,390,35))value=(value+1)%options.Length;return value;}
        float Slider(string name,float value,float x,float y){
            value=Mathf.Clamp01(value);Text(name,x,y,238,30,12,muted,TextAnchor.MiddleLeft);
            Rect track=new Rect(x+254,y+11,296,8);Fill(track.x,track.y,track.width,4,new Color(.2f,.25f,.28f));Fill(track.x,track.y,track.width*value,4,saffron);Fill(track.x+track.width*value-3,track.y-5,6,14,ivory);
            GUI.color=new Color(1,1,1,0);value=GUI.HorizontalSlider(new Rect(x+250,y,305,30),value,0,1);GUI.color=Color.white;
            Text(Mathf.RoundToInt(value*100)+"%",x+562,y,70,30,13,ivory,TextAnchor.MiddleRight);return value;
        }
        void BindRow(string title,string field,string value,int y){Text(title,822,y,339,39,12,muted,TextAnchor.MiddleLeft);if(Button(binding==field?"PRESS KEY...":value.ToUpperInvariant(),1190,y,316,39)){binding=field;bindingMessage="";}}
        void PauseUI(){
            Fill(0,0,1600,900,new Color(.014f,.024f,.033f,.88f));ShellHeader("FLIGHT SUSPENDED");
            if(menuPage=="settings"||menuPage=="controls"){SettingsUI();ShellFooter();return;}
            Tracking("VEER-1 / "+Mission.All[mission].title,75,154,12,2,saffron);Text("TAKE A BREATH.",70,204,1300,95,66,ivory);
            if(Button("RESUME FLIGHT",74,337,443,58,true))Resume();
            if(Button("RESTORE CHECKPOINT",74,413,443,49))RestoreCheckpoint();
            if(Button("RESTART MISSION",74,477,443,49))StartFlight();
            if(Button("SETTINGS",74,541,443,49))menuPage="settings";
            if(Button("RETURN TO TITLE",74,605,443,49))ToMenu();
            Fill(678,338,854,313,new Color(.04f,.063f,.075f,.82f));Tracking("CURRENT OBJECTIVE",709,369,12,2,saffron);Text(objective,709,416,789,75,27,ivory);
            Text("AIRFRAME",709,542,225,22,11,muted);Text(Mathf.RoundToInt(health)+"%",709,575,240,49,34,ivory);Text("FLIGHT TIME",1011,542,300,22,11,muted);Text(TimeSpan.FromSeconds(missionTime).ToString(@"mm\:ss"),1011,575,300,49,34,ivory);
            ShellFooter("ESC / START  RESUME       YOUR FLIGHT IS PAUSED");
        }
        void CinematicUI(){
            Fill(0,0,1600,82,Color.black);Fill(0,778,1600,122,Color.black);
            if(shot==0){
                Backdrop(true);Tracking("BEFORE YOU TAKE FLIGHT",555,218,13,3,saffron);Text("A FICTIONAL STORY",230,279,1140,86,56,ivory,TextAnchor.MiddleCenter);
                Fill(752,389,96,2,saffron);Text(Disclaimer,357,446,886,158,19,muted,TextAnchor.MiddleCenter);Text("Duty. Discipline. The promise to return.",300,652,1000,40,19,ivory,TextAnchor.MiddleCenter);
            }else{
                Tracking("OPERATION SINDOOR",68,30,12,3,muted);Text(shotTitle,670,23,862,40,20,ivory,TextAnchor.MiddleRight);
                Fill(58,613,1200,137,new Color(.014f,.022f,.027f,.72f));Fill(58,613,3,137,saffron);
                Text(shotSubtitle,82,635,1140,94,22,ivory);
                if(Time.time<radioUntil){Text(radioSpeaker,120,801,1360,23,11,saffron,TextAnchor.MiddleCenter);Text(radioText,140,834,1320,54,18,ivory,TextAnchor.UpperCenter);}
            }
            Text("ENTER / A  CONTINUE",1280,745,250,24,11,ivory,TextAnchor.MiddleRight);
            float duration=shot<9?ShotDurations[shot]:10;Fill(0,898,1600*Mathf.Clamp01(shotTime/duration),2,saffron);
            if(shotTime<.6f)Fill(0,82,1600,696,new Color(0,0,0,(1-shotTime/.6f)*.65f));
        }
        void ResultUI(){
            Fill(0,0,1600,900,new Color(.016f,.025f,.035f,.91f));bool success=mode==Mode.Results;ShellHeader("SORTIE DEBRIEF");
            Tracking("VEER-1 / "+Mission.All[mission].title,74,158,12,2,saffron);
            Text(success?"MISSION COMPLETE":"REGROUP. RETURN.",68,221,1430,101,73,ivory);Fill(74,346,98,3,success?mint:saffron);
            Text(success?"Objective confirmed. Your squadron is home.":failReason,74,377,1430,59,24,success?muted:saffron);
            if(success){
                string[] names={"FLIGHT TIME","OBJECTIVES","ACCURACY","AIRFRAME","SCORE"};string[] values={TimeSpan.FromSeconds(missionTime).ToString(@"mm\:ss"),"COMPLETE",shotsFired==0?"—":Mathf.RoundToInt(hits*100f/shotsFired)+"%",Mathf.RoundToInt(health)+"%",score.ToString("N0")};
                for(int i=0;i<5;i++){float x=74+i*296;Fill(x,485,274,136,new Color(.045f,.068f,.082f,.9f));Text(names[i],x+22,509,238,22,11,muted);Text(values[i],x+22,552,238,49,33,ivory);}
                if(Button("CONTINUE  /  "+Mission.All[Mathf.Min(4,mission+1)].title,74,712,627,60,true))BeginMission(Mathf.Min(4,mission+1));
            }else{
                Text("Your last major objective is checkpointed.\nReview your flight, regroup, and return to the sky.",74,493,1250,100,25,muted);
                if(Button("RESTORE CHECKPOINT",74,648,450,58,true))RestoreCheckpoint();
                if(Button("RESTART MISSION",74,725,450,48))StartFlight();
            }
            if(Button("RETURN TO TITLE",success?725:548,success?712:725,350,success?60:48))ToMenu();ShellFooter();
        }
        void CreditsUI(){
            Tracking("FOR EVERY PROMISE TO RETURN",75,153,12,2,saffron);Text("THE PEOPLE.\nTHE PROMISE.",69,207,1050,176,67,ivory);
            Text("OPERATION SINDOOR",77,426,820,37,22,saffron);
            Text("An independent fictional aviation campaign.\nOriginal procedural aircraft, environments, interface and synthesized score.\nTitle key art created with OpenAI Imagegen.\nBuilt with Unity, Universal Render Pipeline and Input System.\n\nAll characters, places, missions and dialogue are fictional.",77,488,1170,178,19,muted);
            Text("FOR THOSE WHO STAND BETWEEN US AND THE STORM.",77,687,1400,30,17,ivory);
            if(Button("RETURN TO TITLE",74,752,366,48,true))ToMenu();
        }
        void TributeUI(){
            Fill(0,0,1600,900,new Color(.008f,.013f,.018f,shotTime<5?1-shotTime/8:.67f));
            Tracking("WE REMEMBER THOSE WHO SERVE",516,263,12,2.4f,saffron);
            string words=shotTime<10?"FOR THOSE WHO STAND BETWEEN US\nAND THE STORM.":shotTime<15?"WE REMEMBER.":shotTime<20?"WE SALUTE.":shotTime<25?"JAI HIND.":"Bharat Mata ki Jai.";
            Text(words,170,370,1260,155,48,ivory,TextAnchor.MiddleCenter);Fill(760,585,80,2,saffron);
            Text("ENTER / A  CREDITS",600,824,400,35,11,muted,TextAnchor.MiddleCenter);
        }
    }
}

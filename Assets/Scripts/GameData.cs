using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sindoor {
    public enum Mode { Menu, Cinematic, Flight, Paused, Results, Failed, Tribute, Credits }
    public enum Stage { Startup, Takeoff, Transit, Identify, Combat, Strike, Egress, Return, Land, Taxi }
    public enum Allegiance { Friendly, Unknown, Hostile, Ground }
    [Serializable] public class Preferences {
        public int quality = 1, difficulty = 1, resolution = 1;
        public bool fullscreen = false, vsync = true, invertMouse, mouseFlight = true;
        public float master = .75f, music = .5f, sfx = .75f, voice = .8f, sensitivity = .65f, viewDistance = 18000;
        public int shadows = 1, effects = 1, textures = 0, antialiasing = 2;
        public string fire = "Space", missile = "R", lockTarget = "F", counter = "X", assist = "H";
    }
    [Serializable] public class SaveData {
        public int version = 1, unlocked = 1, currentMission, completed;
        public int[] bestScores = new int[5];
        public Preferences settings = new Preferences();
    }
    public static class Saves {
        public static string PathName => Path.Combine(Application.persistentDataPath, "campaign.json");
        public static SaveData Read(string path = null) {
            path = path ?? PathName;
            foreach (var candidate in new[] { path, path + ".bak" }) {
                try {
                    if (!File.Exists(candidate)) continue;
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(candidate));
                    if (data == null || data.version != 1 || data.bestScores == null || data.bestScores.Length != 5 || data.settings == null) continue;
                    data.unlocked = Mathf.Clamp(data.unlocked, 1, 5); data.currentMission = Mathf.Clamp(data.currentMission, 0, 4);
                    data.settings.difficulty = Mathf.Clamp(data.settings.difficulty, 0, 2);
                    data.settings.master = Mathf.Clamp01(data.settings.master);
                    data.settings.sensitivity = Mathf.Clamp(data.settings.sensitivity, .1f, 1.5f);
                    return data;
                } catch (Exception) { }
            }
            return new SaveData();
        }
        public static void Write(SaveData data, string path = null) {
            path = path ?? PathName; Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp = path + ".tmp"; File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
        }
    }
    public sealed class Mission {
        public string title, subtitle, briefing, weather;
        public Color sky;
        public int hostiles, installations;
        public Mission(string title, string subtitle, string briefing, string weather, Color sky, int hostiles, int installations = 0) {
            this.title = title; this.subtitle = subtitle; this.briefing = briefing; this.weather = weather;
            this.sky = sky; this.hostiles = hostiles; this.installations = installations;
        }
        public static readonly Mission[] All = {
            new Mission("SCRAMBLE", "01 / THE FIRST LIGHT", "An unidentified flight has entered the Ashva training sector. Launch, establish visual identification, and bring our survey aircraft home.", "DAWN / VISIBILITY 18 KM", new Color(.34f,.43f,.52f), 1),
            new Mission("AIR DEFENCE", "02 / HOLD THE LINE", "Hostile aircraft are approaching the fictional Kesar corridor. Protect the friendly formation and prevent a breakthrough.", "CLEAR / LIGHT CROSSWIND", new Color(.36f,.55f,.68f), 4),
            new Mission("OPERATION SINDOOR", "03 / THROUGH THE VALLEY", "Follow the designated valley corridor. Disable the two marked military relay installations at Vana Ridge. Civilian settlements are protected: fire only on designated targets.", "SUNSET / BROKEN CLOUD", new Color(.52f,.39f,.35f), 2, 2),
            new Mission("THE LONG RETURN", "04 / NO ONE LEFT BEHIND", "Your formation has been intercepted on the way home. Weapons are limited. Defend your wingman, break the interception, and reach safe airspace.", "STORM / VISIBILITY 9 KM", new Color(.22f,.3f,.37f), 5),
            new Mission("HOMECOMING", "05 / A QUIET SKY", "Ashva is in sight. Follow the approach lights, land, and taxi home. Your ground crew is waiting.", "MORNING / CALM", new Color(.5f,.63f,.7f), 0)
        };
    }
    public static class Controls {
        public static bool Down(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public static bool Held(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        public static bool Bind(string name, bool held = false) => Enum.TryParse<Key>(name, out var k) && (held ? Held(k) : Down(k));
        public static float Axis(Key positive, Key negative) => (Held(positive) ? 1 : 0) - (Held(negative) ? 1 : 0);
        public static Gamepad Pad => Gamepad.current;
        public static bool Confirm => Down(Key.Enter) || (Pad != null && Pad.buttonSouth.wasPressedThisFrame);
    }
    public sealed class AudioDirector : MonoBehaviour {
        AudioSource engine, music, effects, wind,afterburner;
        AudioClip tone, boom, click, gun, uiClick,launch;
        public Preferences settings;
        float alarmAt;
        public void Initialize(Preferences p) {
            settings = p;
            engine = Source(true); music = Source(true); effects = Source(false); wind = Source(true);
            engine.clip = AudioAssetSynthesis.Create("Turbine",4,true);
            wind.clip = AudioAssetSynthesis.Create("Wind",4,true);
            afterburner=Source(true);afterburner.clip=AudioAssetSynthesis.Create("Afterburner",4,true);afterburner.volume=0;afterburner.Play();
            music.clip = Synth("Original ambient score", 8, (t) => {
                float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI*t/8), 2);
                return envelope*(Mathf.Sin(t*2*Mathf.PI*110)*.1f + Mathf.Sin(t*2*Mathf.PI*164.81f)*.06f + Mathf.Sin(t*2*Mathf.PI*220)*.04f + Mathf.Sin(t*2*Mathf.PI*261.63f)*.035f);
            });
            tone = Synth("Alert", .4f, t => Mathf.Sin(t*2*Mathf.PI*880)*.22f*Mathf.Sin(t/.4f*Mathf.PI));
            boom = AudioAssetSynthesis.Create("Explosion",2.2f);
            click = AudioAssetSynthesis.Create("Radio",.16f);
            gun = AudioAssetSynthesis.Create("Cannon",.19f);
            launch=AudioAssetSynthesis.Create("Launch",1.25f);
            uiClick = Synth("Interface touch", .07f, t => Mathf.Sin(t*2*Mathf.PI*520)*.18f*Mathf.Exp(-t*65));
            engine.Play(); wind.Play(); music.Play();
        }
        static float Noise(float t) => Mathf.Sin(t*18731.7f)*Mathf.Sin(t*37243.1f);
        AudioSource Source(bool loop) { var a = gameObject.AddComponent<AudioSource>(); a.loop = loop; a.playOnAwake = false; a.spatialBlend = 0; return a; }
        static AudioClip Synth(string name, float seconds, Func<float,float> f) {
            int rate = 22050; var samples = new float[(int)(seconds*rate)];
            for(int i=0;i<samples.Length;i++) samples[i] = f(i/(float)rate);
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples,0); return clip;
        }
        public void Mix(float throttle, bool flying, bool combat) {
            AudioListener.volume = settings.master;
            engine.volume = (flying ? .11f+throttle*.16f : .018f)*settings.sfx; engine.pitch = .6f+throttle*.9f;
            wind.volume = (flying ? .16f : .04f)*settings.sfx;
            afterburner.volume=Mathf.MoveTowards(afterburner.volume,flying?Mathf.InverseLerp(.78f,1,throttle)*settings.sfx*.26f:0,Time.unscaledDeltaTime*.4f);
            music.volume = settings.music*(combat ? .6f : .45f); music.pitch = combat ? 1.08f : 1;
            effects.volume = settings.sfx;
        }
        public void Cue(string type) {
            if(type=="hover"||type=="ui"){effects.PlayOneShot(uiClick,type=="hover"?.22f:.7f);return;}
            if (type == "alarm" && Time.unscaledTime < alarmAt) return;
            if (type == "alarm") alarmAt = Time.unscaledTime + 1;
            effects.PlayOneShot(type == "boom" ? boom : type == "gun" ? gun : type == "radio" ? click : type=="launch"?launch:tone, type == "radio" ? settings.voice : 1);
        }
    }
}

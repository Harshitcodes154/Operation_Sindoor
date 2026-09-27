using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sindoor {
    public sealed class AtmospherePresentation:MonoBehaviour {
        public bool nightLighting;
        bool nightApplied;Color daySun,dayFog;float dayIntensity,dayExposure;Quaternion dayRotation;SphericalHarmonicsL2 dayAmbient;
        DepthOfField focus;ColorAdjustments grade;int lastMission=-1;Material water;
        public void RefreshAmbient(){lastMission=-1;}
        void Start(){
            var game=OperationGame.Instance;if(!game)return;
            var volume=new GameObject("Asset presentation grade").AddComponent<Volume>();volume.transform.SetParent(transform);volume.isGlobal=true;volume.priority=2;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            focus=volume.profile.Add<DepthOfField>();focus.mode.Override(DepthOfFieldMode.Off);focus.gaussianStart.Override(5);focus.gaussianEnd.Override(28);focus.gaussianMaxRadius.Override(.7f);
            grade=volume.profile.Add<ColorAdjustments>();grade.contrast.Override(8);grade.saturation.Override(-7);
            var ocean=transform.Find("Distant water");if(ocean){water=new Material(Shader.Find("Sindoor/Water"));ocean.GetComponent<Renderer>().sharedMaterial=water;}
            var reflection=new GameObject("Atmospheric reflection").AddComponent<ReflectionProbe>();reflection.transform.SetParent(transform);reflection.transform.position=new Vector3(0,35,-4700);reflection.mode=ReflectionProbeMode.Realtime;reflection.refreshMode=ReflectionProbeRefreshMode.OnAwake;reflection.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;reflection.resolution=128;reflection.size=new Vector3(2000,600,4000);reflection.cullingMask=0;reflection.clearFlags=ReflectionProbeClearFlags.Skybox;reflection.intensity=.75f;
        }
        void LateUpdate(){
            var g=OperationGame.Instance;if(!g||focus==null)return;
            bool close=g.mode==Mode.Cinematic&&(g.shot==4||g.shot==5||g.shot==7||g.shot==20)&&g.save.settings.effects>0;
            focus.mode.Override(close?DepthOfFieldMode.Gaussian:DepthOfFieldMode.Off);
            if(lastMission!=g.mission){lastMission=g.mission;grade.colorFilter.Override(lastMission==3?new Color(.84f,.91f,1):lastMission==2?new Color(1,.93f,.84f):new Color(1,.99f,.94f));
                // Runtime-generated worlds have no baked ambient probe. Supply sky fill explicitly.
                RenderSettings.ambientMode=AmbientMode.Custom;
                var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.36f,.42f,.5f)*(lastMission==3?.72f:1));ambient.AddDirectionalLight(Vector3.up,new Color(.4f,.48f,.59f),.25f);RenderSettings.ambientProbe=ambient;
            }
            if(water)water.SetVector("_SunDirection",-g.sun.transform.forward);
            if(nightLighting!=nightApplied){
                if(nightLighting){daySun=g.sun.color;dayFog=RenderSettings.fogColor;dayIntensity=g.sun.intensity;dayRotation=g.sun.transform.rotation;dayAmbient=RenderSettings.ambientProbe;dayExposure=RenderSettings.skybox.GetFloat("_Exposure");
                    g.sun.color=new Color(.48f,.63f,1);g.sun.intensity=.18f;g.sun.transform.rotation=Quaternion.Euler(32,145,0);RenderSettings.fogColor=new Color(.024f,.04f,.07f);RenderSettings.skybox.SetFloat("_Exposure",.045f);var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.033f,.046f,.075f));RenderSettings.ambientProbe=ambient;
                }else{g.sun.color=daySun;g.sun.intensity=dayIntensity;g.sun.transform.rotation=dayRotation;RenderSettings.fogColor=dayFog;RenderSettings.ambientProbe=dayAmbient;RenderSettings.skybox.SetFloat("_Exposure",dayExposure);}
                nightApplied=nightLighting;
            }
        }
    }
}

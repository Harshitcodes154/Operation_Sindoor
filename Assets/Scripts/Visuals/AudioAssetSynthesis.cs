using System;
using UnityEngine;

namespace Sindoor {
    // Original, deterministic layered sound design. No recordings or external samples.
    public static class AudioAssetSynthesis {
        public static AudioClip Create(string type,float duration,bool loop=false){
            const int rate=44100;int count=(int)(rate*duration);var samples=new float[count];var random=new System.Random(7103);float low=0,mid=0;
            for(int i=0;i<count;i++){
                float t=i/(float)rate,n=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,n,.009f);mid=Mathf.Lerp(mid,n,.13f);float value;
                switch(type){
                    case "Turbine":value=low*1.8f+mid*.3f+Mathf.Sin(t*2*Mathf.PI*186)*.042f+Mathf.Sin(t*2*Mathf.PI*372)*.026f+Mathf.Sin(t*2*Mathf.PI*1116)*.018f;break;
                    case "Afterburner":value=low*2.2f+mid*.55f+Mathf.Sin(t*2*Mathf.PI*44)*.06f;break;
                    case "Wind":value=low*.9f+mid*.2f;break;
                    case "Launch":value=(mid*.85f+low*2+Mathf.Sin(t*2*Mathf.PI*(110-25*t))*.1f)*Mathf.Exp(-t*3)*Mathf.Clamp01(t*80);break;
                    case "Explosion":value=(low*3.4f+mid*.8f*Mathf.Exp(-t*6)+Mathf.Sin(t*2*Mathf.PI*(52-5*t))*.35f)*Mathf.Exp(-t*2.4f)*Mathf.Clamp01(t*400);break;
                    case "Cannon":value=(mid*.9f+Mathf.Sin(t*2*Mathf.PI*96)*.3f)*Mathf.Exp(-t*34)*Mathf.Clamp01(t*1800);break;
                    default:value=(n-mid)*.14f*Mathf.Sin(Mathf.PI*t/duration);break;
                }
                samples[i]=Mathf.Clamp(value,-.9f,.9f);
            }
            if(loop){int fade=Mathf.Min(512,count/8);for(int i=0;i<fade;i++){float mix=i/(float)fade;samples[i]*=mix;samples[count-i-1]*=mix;}}
            var clip=AudioClip.Create("Original / "+type,count,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}

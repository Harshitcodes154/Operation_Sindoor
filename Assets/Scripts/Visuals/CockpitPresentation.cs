using UnityEngine;

namespace Sindoor {
    public sealed class CockpitPresentation : MonoBehaviour {
        TextMesh left,centre,right;float nextUpdate;
        void Start(){transform.localPosition=new Vector3(0,.06f,.4f);left=Display(-.66f);centre=Display(0);right=Display(.66f);}
        TextMesh Display(float x){var t=WorldFactory.Text(transform,"",new Vector3(x,-.43f,.693f),.038f,new Color(.35f,.95f,.65f));t.fontSize=64;t.lineSpacing=1;return t;}
        void Update(){
            if(Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+.15f;var g=OperationGame.Instance;if(!g)return;
            left.text="ENGINE\nRPM "+Mathf.RoundToInt(g.throttle*100)+"%\nFUEL "+g.fuel.ToString("0")+"%\nSYS  "+(g.health>35?"NOMINAL":"CAUTION");
            centre.text="NAVIGATION\nHDG "+g.heading.ToString("000")+"\nALT "+g.ship.position.y.ToString("0000")+"\n"+(g.assist?"ASSIST ON":"MANUAL");
            right.text="STORES\nAAM "+g.missiles.ToString("00")+"\nGUN "+g.cannon.ToString("000")+"\nCM  "+g.flares.ToString("00");
        }
    }
}

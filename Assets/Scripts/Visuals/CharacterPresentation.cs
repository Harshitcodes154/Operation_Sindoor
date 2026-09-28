using UnityEngine;

namespace Sindoor {
    // Consumes actor motion and shot state; never moves the gameplay root or changes shot timing.
    [DefaultExecutionOrder(100)]
    public sealed class CharacterPresentation:MonoBehaviour {
        Transform[] joints;Vector3[] positions;Transform[] legs,knees,ankles,arms,elbows;
        Quaternion[] pose;Vector3 previousPosition;float gait,speed,salute,climb,groundCorrection,animationTime;Transform torso;
        bool initialized;int previousShot=-1;
        public float WalkBlend { get; private set; }
        public float SaluteBlend=>salute;
        void Start(){
            joints=new Transform[transform.childCount];positions=new Vector3[joints.Length];
            for(int i=0;i<joints.Length;i++){joints[i]=transform.GetChild(i);positions[i]=joints[i].localPosition;}
            legs=new[]{transform.Find("Leg L"),transform.Find("Leg R")};arms=new[]{transform.Find("Arm L"),transform.Find("Arm R")};
            knees=new Transform[2];ankles=new Transform[2];elbows=new Transform[2];pose=new Quaternion[10];
            for(int i=0;i<2;i++){knees[i]=legs[i].Find("Knee");ankles[i]=knees[i]?knees[i].Find("Ankle"):null;elbows[i]=arms[i].Find("Elbow");}
            for(int i=0;i<pose.Length;i++)pose[i]=Quaternion.identity;
            previousPosition=transform.position;initialized=true;
            torso=transform.Find("Visual Model");
        }
        void OnEnable(){previousPosition=transform.position;speed=0;}
        public static float Damp(float value,float target,float response,float dt)=>Mathf.Lerp(value,target,1-Mathf.Exp(-response*dt));
        // Planted stance followed by an eased, raised return.
        public static Vector2 FootCycle(float cycle){
            float t=Mathf.Repeat(cycle,1);
            if(t<.6f)return new Vector2(Mathf.Lerp(.18f,-.18f,t/.6f),0);
            t=(t-.6f)/.4f;float eased=t*t*t*(t*(t*6-15)+10);
            return new Vector2(Mathf.Lerp(-.18f,.18f,eased),Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*Mathf.PI)*.11f);
        }
        public static Vector3 SolveLeg(float forward,float ankleHeight){
            const float length=.42f;float down=.98f-ankleHeight,distance=Mathf.Clamp(Mathf.Sqrt(down*down+forward*forward),.1f,2*length-.001f);
            float bend=Mathf.Acos(distance/(2*length))*Mathf.Rad2Deg;
            float hip=-Mathf.Atan2(forward,down)*Mathf.Rad2Deg-bend;
            return new Vector3(hip,bend*2,-hip-bend*2);
        }
        public static void SalutePose(out Quaternion upper,out Quaternion lower){
            Vector3 shoulder=new Vector3(.246f,1.448f,0),target=new Vector3(.105f,1.8f,.115f),upperRest=new Vector3(.035f,-.276f,0),handRest=new Vector3(0,-.305f,.028f);
            Vector3 toHand=target-shoulder,axis=toHand.normalized;float distance=toHand.magnitude,a=upperRest.magnitude,b=handRest.magnitude;
            float along=(a*a-b*b+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector3 pole=Vector3.ProjectOnPlane(Vector3.right,axis).normalized,elbow=shoulder+axis*along+pole*height;
            upper=Quaternion.FromToRotation(upperRest,elbow-shoulder);
            lower=Quaternion.Inverse(upper)*Quaternion.FromToRotation(handRest,target-elbow);
        }
        void Rotate(Transform joint,int index,Vector3 angles,float dt)=>Rotate(joint,index,Quaternion.Euler(angles),dt);
        void Rotate(Transform joint,int index,Quaternion rotation,float dt){if(!joint)return;pose[index]=Quaternion.Slerp(pose[index],rotation,1-Mathf.Exp(-16*dt));joint.localRotation=pose[index];}
        void LateUpdate(){
            if(!initialized)return;var g=OperationGame.Instance;
            float dt=g&&g.enabled&&g.mode==Mode.Paused?0:Mathf.Min(Time.deltaTime,.05f);
            animationTime+=dt;
            Vector3 delta=transform.position-previousPosition;previousPosition=transform.position;
            int shot=g&&g.mode==Mode.Cinematic?g.shot:-1;bool cut=shot!=previousShot||delta.sqrMagnitude>4;previousShot=shot;
            float measured=!cut&&dt>0?new Vector2(delta.x,delta.z).magnitude/dt:0;
            bool pilot=g&&g.pilot==transform;
            speed=Damp(speed,Mathf.Clamp(measured,0,1.8f),8,dt);WalkBlend=Damp(WalkBlend,Mathf.Clamp01(speed/.65f),9,dt);
            gait+=speed*dt/.6f;
            salute=Damp(salute,pilot&&(shot==5||shot==20)?1:0,6,dt);
            climb=Damp(climb,pilot&&shot==7?1:0,5,dt);
            float surface=VisualEnvironment.SurfaceHeight(transform.position),height=transform.position.y-surface;
            float target=height<.85f&&height>-.45f?(surface-transform.position.y)/transform.lossyScale.y-.012f:0;
            groundCorrection=cut?target:Damp(groundCorrection,target,14,dt);
            float bounce=(1-Mathf.Cos(gait*4*Mathf.PI))*.006f*WalkBlend;
            for(int i=0;i<joints.Length;i++)joints[i].localPosition=positions[i]+Vector3.up*(groundCorrection+bounce);
            if(torso)torso.localScale=new Vector3(1,1+Mathf.Sin(animationTime*1.7f)*.0015f,1+Mathf.Sin(animationTime*1.7f)*.004f);
            for(int i=0;i<2;i++){
                float phase=gait+i*.5f;Vector2 foot=FootCycle(phase);Vector3 leg=SolveLeg(foot.x*WalkBlend,.16f+foot.y*WalkBlend);
                float ladder=Mathf.Sin(animationTime*3.2f+i*Mathf.PI);
                leg=Vector3.Lerp(leg,new Vector3(-45-ladder*18,65+ladder*20,-20),climb);
                Rotate(legs[i],i*3,new Vector3(leg.x,0,0),dt);Rotate(knees[i],i*3+1,new Vector3(leg.y,0,0),dt);Rotate(ankles[i],i*3+2,new Vector3(leg.z,0,0),dt);
                float swing=Mathf.Sin(phase*2*Mathf.PI)*19*WalkBlend;
                Vector3 arm=new Vector3(swing,0,i==0?-4:4),elbow=new Vector3(-12-Mathf.Abs(swing)*.35f,0,0);
                arm=Vector3.Lerp(arm,new Vector3(-112-ladder*10,0,i==0?-12:12),climb);elbow=Vector3.Lerp(elbow,new Vector3(-32,0,0),climb);
                Quaternion upper=Quaternion.Euler(arm),lower=Quaternion.Euler(elbow);
                if(i==1){SalutePose(out var raised,out var bent);upper=Quaternion.Slerp(upper,raised,salute);lower=Quaternion.Slerp(lower,bent,salute);}
                Rotate(arms[i],6+i*2,upper,dt);Rotate(elbows[i],7+i*2,lower,dt);
            }
        }
    }
}

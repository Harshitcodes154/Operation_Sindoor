using UnityEngine;

namespace Sindoor {
    // Ground-contact correction for the visual rig; cinematic root motion and joint rotations stay intact.
    public sealed class CharacterPresentation:MonoBehaviour {
        Transform[] joints;Vector3[] positions;
        void Start(){joints=new Transform[transform.childCount];positions=new Vector3[joints.Length];for(int i=0;i<joints.Length;i++){joints[i]=transform.GetChild(i);positions[i]=joints[i].localPosition;}}
        void LateUpdate(){
            if(joints==null)return;float surface=VisualEnvironment.SurfaceHeight(transform.position),height=transform.position.y-surface;
            float correction=height<.85f&&height>-.45f?(surface-transform.position.y)/transform.lossyScale.y-.032f:0;
            for(int i=0;i<joints.Length;i++)joints[i].localPosition=positions[i]+Vector3.up*correction;
        }
    }
}

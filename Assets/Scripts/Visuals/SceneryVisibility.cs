using System.Collections.Generic;
using UnityEngine;

namespace Sindoor {
    // Conservative terrain occlusion for static props only. Combat targets are never culled here.
    public sealed class SceneryVisibility:MonoBehaviour {
        sealed class Entry {public LODGroup lod;public Renderer[] renderers;public Bounds bounds;public bool visible=true;}
        readonly List<Entry> entries=new List<Entry>();int cursor;
        public void Register(Transform root){var lod=root.GetComponent<LODGroup>();if(!lod)return;var rs=root.GetComponentsInChildren<Renderer>();if(rs.Length==0)return;var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);entries.Add(new Entry{lod=lod,renderers=rs,bounds=bounds});}
        void Update(){
            var game=OperationGame.Instance;if(!game||!game.cam||entries.Count==0)return;Vector3 from=game.cam.transform.position;
            for(int n=0;n<4;n++){
                var e=entries[cursor++%entries.Count];if(!e.lod)continue;float distance=Vector3.Distance(from,e.bounds.center);
                bool visible=distance<280||distance-e.bounds.extents.magnitude<game.cam.farClipPlane;
                if(visible&&distance>600){
                    // Only hide when three separated top-bound samples are all behind a ridge.
                    Vector3 top=e.bounds.center+Vector3.up*e.bounds.extents.y;
                    visible=Clear(from,top)||Clear(from,top+Vector3.right*e.bounds.extents.x)||Clear(from,top-Vector3.right*e.bounds.extents.x);
                }
                if(visible==e.visible)continue;e.visible=visible;e.lod.enabled=visible;foreach(var r in e.renderers)if(r)r.enabled=visible;
            }
        }
        static bool Clear(Vector3 from,Vector3 to){for(int s=1;s<12;s++){var p=Vector3.Lerp(from,to,s/12f);if(WorldFactory.Height(p.x,p.z)-70>p.y)return false;}return true;}
        void OnDisable(){foreach(var e in entries){if(e.lod)e.lod.enabled=true;foreach(var r in e.renderers)if(r)r.enabled=true;}}
    }
}

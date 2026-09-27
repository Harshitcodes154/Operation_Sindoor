using UnityEngine;

namespace Sindoor {
    // Pure presentation catalog: no aircraft, combat, objective or save state lives here.
    [CreateAssetMenu(menuName="Operation Sindoor/Visual asset library")]
    public sealed class VisualAssetLibrary : ScriptableObject {
        public GameObject kestrel,adversary,pilot,officer,cockpit,missile,truck,hangar,tree,rock,controlTower,barracks,building,relay;
        public Material airframe,enemyPaint,alloy,rubber,canopy,fabric,concrete,asphalt,cladding,soil,foliage;
        public Texture2D smoke;
        static VisualAssetLibrary loaded;
        public static VisualAssetLibrary Current {get {if(!loaded)loaded=Resources.Load<VisualAssetLibrary>("Visuals/VisualAssetLibrary");return loaded;}}
        public static Transform InstantiateVisual(GameObject prefab,Transform parent,string name){
            if(!prefab)throw new System.InvalidOperationException("Missing visual asset: "+name+". Run Setup and build.");
            var instance=Object.Instantiate(prefab,parent,false);instance.name=name;return instance.transform;
        }
    }
}

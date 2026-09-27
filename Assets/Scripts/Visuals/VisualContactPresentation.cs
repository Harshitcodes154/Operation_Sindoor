using UnityEngine;

namespace Sindoor {
    // Adapts ground-contact presentation without touching spawning, health, targeting or saves.
    public sealed class VisualContactPresentation:MonoBehaviour {
        float next;
        void LateUpdate(){
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;var game=OperationGame.Instance;if(!game)return;
            foreach(var contact in game.contacts){
                if(contact.side!=Allegiance.Ground||!contact.body||contact.dead||contact.body.Find("Visual Model"))continue;
                foreach(var r in contact.body.GetComponentsInChildren<Renderer>())r.enabled=false;
                VisualAssetLibrary.InstantiateVisual(VisualAssetLibrary.Current.relay,contact.body,"Visual Model");
            }
        }
    }
}

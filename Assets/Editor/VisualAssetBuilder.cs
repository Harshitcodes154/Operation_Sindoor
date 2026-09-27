using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sindoor.Editor {
    public static class VisualAssetBuilder {
        const string Folder="Assets/GeneratedVisuals";
        [MenuItem("Operation Sindoor/Rebuild upgraded visual assets")]
        public static void Build(){
            Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Prefabs");Directory.CreateDirectory("Assets/Resources/Visuals");AssetDatabase.Refresh();WorldFactory.Initialize();
            var a=AssetDatabase.LoadAssetAtPath<VisualAssetLibrary>("Assets/Resources/Visuals/VisualAssetLibrary.asset");if(!a){a=ScriptableObject.CreateInstance<VisualAssetLibrary>();AssetDatabase.CreateAsset(a,"Assets/Resources/Visuals/VisualAssetLibrary.asset");}
            a.airframe=VisualMaterialAuthoring.Make("Kestrel painted alloy",new Color(.37f,.405f,.405f),0,.38f,.43f,1024);
            a.enemyPaint=VisualMaterialAuthoring.Make("Raven disruptive paint",new Color(.31f,.325f,.28f),1,.23f,.34f,1024);
            a.alloy=VisualMaterialAuthoring.Make("Brushed titanium",new Color(.42f,.44f,.45f),0,.88f,.6f);
            a.rubber=VisualMaterialAuthoring.Make("Rubber and carbon",new Color(.028f,.033f,.038f),3,.04f,.22f,256);
            a.fabric=VisualMaterialAuthoring.Make("Flight suit twill",new Color(.225f,.267f,.185f),5,0,.21f);
            a.concrete=VisualMaterialAuthoring.Make("Weathered apron concrete",new Color(.39f,.395f,.365f),2,.02f,.18f,1024);
            a.asphalt=VisualMaterialAuthoring.Make("Runway aggregate",new Color(.19f,.2f,.21f),3,.02f,.16f,1024);
            a.cladding=VisualMaterialAuthoring.Make("Corrugated hangar steel",new Color(.3f,.33f,.32f),4,.45f,.37f);
            a.soil=VisualMaterialAuthoring.Make("Eroded rock and soil",new Color(.42f,.355f,.265f),6,0,.13f,1024);
            var soilImporter=AssetImporter.GetAtPath("Assets/Art/DryFoothillAlbedo.png") as TextureImporter;
            if(soilImporter){soilImporter.maxTextureSize=1024;soilImporter.mipmapEnabled=true;soilImporter.wrapMode=TextureWrapMode.Repeat;soilImporter.anisoLevel=4;soilImporter.textureCompression=TextureImporterCompression.CompressedHQ;soilImporter.SaveAndReimport();a.soil.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(soilImporter.assetPath));}
            a.foliage=VisualMaterialAuthoring.Make("Dryland leaves",new Color(.19f,.255f,.12f),6,0,.13f);
            a.canopy=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Optical canopy.mat");
            if(!a.canopy){a.canopy=new Material(Shader.Find("Sindoor/Canopy")){name="Optical canopy"};AssetDatabase.CreateAsset(a.canopy,Folder+"/Materials/Optical canopy.mat");}
            a.canopy.shader=Shader.Find("Sindoor/Canopy");a.canopy.shaderKeywords=new string[0];a.canopy.SetColor("_BaseColor",new Color(.12f,.22f,.24f,.4f));EditorUtility.SetDirty(a.canopy);
            a.smoke=SmokeTexture();
            a.kestrel=Save(OriginalAircraft.Build(null,false,a),"Kestrel");a.adversary=Save(OriginalAircraft.Build(null,true,a),"Raven");
            a.pilot=Save(OriginalCharacters.Build(null,false,a),"Pilot");a.officer=Save(OriginalCharacters.Build(null,true,a),"Officer");a.cockpit=Save(OriginalProps.Cockpit(null,a),"Cockpit");a.missile=Save(OriginalAircraft.Missile(null,a),"Missile");
            a.hangar=Save(OriginalProps.Build(null,"Hangar",a),"Hangar");a.truck=Save(OriginalProps.Build(null,"Utility truck",a),"Truck");a.tree=Save(OriginalProps.Build(null,"Tree",a),"Tree");a.rock=Save(OriginalProps.Build(null,"Rock",a),"Rock");
            a.controlTower=Save(OriginalStructures.Build("Control tower",a),"ControlTower");a.barracks=Save(OriginalStructures.Build("Barracks",a),"Barracks");a.building=Save(OriginalStructures.Build("Building",a),"Building");a.relay=Save(OriginalStructures.Build("Relay",a),"Relay");
            EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Validate(a);Debug.Log("SINDOOR_VISUAL_ASSETS_OK");
        }
        static GameObject Save(Transform root,string name){
            int index=0;
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true)){
                if(AssetDatabase.Contains(mf.sharedMesh))continue;
                string path=Folder+"/Meshes/"+name+"_"+(index++).ToString("000")+".asset";mf.sharedMesh.name=Path.GetFileNameWithoutExtension(path);var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing){EditorUtility.CopySerialized(mf.sharedMesh,existing);UnityEngine.Object.DestroyImmediate(mf.sharedMesh);mf.sharedMesh=existing;EditorUtility.SetDirty(existing);}else AssetDatabase.CreateAsset(mf.sharedMesh,path);
            }
            foreach(var r in root.GetComponentsInChildren<Renderer>(true)){
                var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++){
                    if(!materials[i]||AssetDatabase.Contains(materials[i]))continue;string safe=string.Concat(materials[i].name.Select(c=>char.IsLetterOrDigit(c)?c:'_'));string path=Folder+"/Materials/"+safe+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(existing){EditorUtility.CopySerialized(materials[i],existing);materials[i]=existing;}else AssetDatabase.CreateAsset(materials[i],path);
                }r.sharedMaterials=materials;
            }
            string prefabPath=Folder+"/Prefabs/"+name+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,prefabPath);UnityEngine.Object.DestroyImmediate(root.gameObject);return prefab;
        }
        static Texture2D SmokeTexture(){
            const int n=128;var t=new Texture2D(n,n,TextureFormat.RGBA32,true);var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=(x-n/2f)/(n/2f),v=(y-n/2f)/(n/2f),r=Mathf.Sqrt(u*u+v*v),noise=Mathf.PerlinNoise(u*5+20,v*5+30)*.6f+Mathf.PerlinNoise(u*13+50,v*13+70)*.4f;
                float alpha=Mathf.Pow(Mathf.Clamp01(1-r),1.1f)*(.3f+noise*.7f);pixels[y*n+x]=new Color(.7f+noise*.3f,.7f+noise*.3f,.7f+noise*.3f,alpha);
            }
            t.SetPixels(pixels);t.Apply();string path=Folder+"/Materials/SmokeDensity.png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static void Validate(VisualAssetLibrary a){
            Directory.CreateDirectory("Artifacts");var lines=new List<string>();
            foreach(var prefab in new[]{a.kestrel,a.adversary,a.pilot,a.officer,a.cockpit,a.missile,a.hangar,a.truck,a.tree,a.rock,a.controlTower,a.barracks,a.building,a.relay}){
                if(!prefab)throw new Exception("Visual catalog has a missing prefab");
                if(prefab.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Visual prefab unexpectedly adds gameplay colliders: "+prefab.name);
                foreach(var t in prefab.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Broken visual script: "+prefab.name);
                foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)throw new Exception("Missing or unsupported material: "+prefab.name);
                var lod=prefab.GetComponentInChildren<LODGroup>();if(lod){int prior=int.MaxValue;foreach(var l in lod.GetLODs()){int triangles=l.renderers.Sum(r=>r.GetComponent<MeshFilter>()?.sharedMesh.triangles.Length/3??0);if(triangles>=prior)throw new Exception("LOD does not reduce geometry: "+prefab.name);prior=triangles;lines.Add(prefab.name+" LOD @ "+l.screenRelativeTransitionHeight+": "+triangles+" triangles / "+l.renderers.Length+" renderers");}}
            }
            foreach(var prefab in new[]{a.kestrel,a.adversary})foreach(var name in new[]{"CockpitCamera","CannonMuzzle","MissileLaunch","ExhaustL","ExhaustR"})if(!prefab.transform.Find("Attachments/"+name))throw new Exception("Missing airframe attachment "+name);
            var attachmentPositions=new Dictionary<string,Vector3>{{"CockpitCamera",new Vector3(0,1.5f,4.5f)},{"CannonMuzzle",new Vector3(0,-.5f,9)},{"MissileLaunch",new Vector3(0,-1,8)},{"ExhaustL",new Vector3(-.69f,-.1f,-8.2f)},{"ExhaustR",new Vector3(.69f,-.1f,-8.2f)}};
            foreach(var prefab in new[]{a.kestrel,a.adversary})foreach(var item in attachmentPositions)if(Vector3.Distance(prefab.transform.Find("Attachments/"+item.Key).localPosition,item.Value)>.001f)throw new Exception("Misaligned attachment: "+item.Key);
            foreach(var prefab in new[]{a.pilot,a.officer})foreach(var bone in new[]{"Leg L","Leg R","Arm L","Arm R"})if(!prefab.transform.Find(bone))throw new Exception("Incompatible cinematic rig: "+bone);
            lines.Add("PASS: catalog, references, materials, decreasing LOD counts, aligned aircraft attachments, separate collision model and cinematic rig compatibility.");File.WriteAllLines("Artifacts/visual-asset-validation.txt",lines);
        }
    }
}

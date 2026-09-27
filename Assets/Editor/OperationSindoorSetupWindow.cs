using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sindoor.Editor {
    public class OperationSindoorSetupWindow : EditorWindow {
        static readonly string[] Scenes={"Boot","MainMenu","Intro","Briefing","AircraftSelection","Mission01","Mission02","Mission03","Mission04","Mission05","Ending","Credits"};
        [MenuItem("Operation Sindoor/Setup and build")]
        public static void Open(){GetWindow<OperationSindoorSetupWindow>("Operation Sindoor");}
        void OnGUI(){GUILayout.Label("OPERATION SINDOOR",EditorStyles.boldLabel);GUILayout.Label("Procedural campaign authoring and Windows build",EditorStyles.wordWrappedLabel);
            if(GUILayout.Button("SETUP PROJECT"))Setup();if(GUILayout.Button("BUILD CONTENT"))Content();if(GUILayout.Button("VALIDATE PROJECT"))Validate();if(GUILayout.Button("GENERATE MISSIONS"))GenerateScenes();if(GUILayout.Button("CHECK ASSETS"))Validate();if(GUILayout.Button("FIX COMMON ISSUES"))Setup();if(GUILayout.Button("BUILD WINDOWS GAME"))BuildWindows();}
        public static void Setup(){
            foreach(var folder in new[]{"Art","Audio","Materials","Models","Prefabs","Scenes","Settings","Fonts","Resources","Addressables","Scripts/Aircraft","Scripts/AI","Scripts/Combat","Scripts/Missions","Scripts/UI","Scripts/Systems","Scripts/Save","Scripts/Audio","Scripts/Cinematics","Scripts/VFX"})Directory.CreateDirectory("Assets/"+folder);
            AssetDatabase.Refresh();
            PlayerSettings.companyName="Sentinel Studio";PlayerSettings.productName="Operation Sindoor";PlayerSettings.bundleVersion="1.1.0";PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"studio.sentinel.operationsindoor");
            var keyArt=AssetImporter.GetAtPath("Assets/Resources/UI/TitleKeyArt.png") as TextureImporter;
            if(keyArt!=null){keyArt.textureType=TextureImporterType.Default;keyArt.sRGBTexture=true;keyArt.mipmapEnabled=false;keyArt.maxTextureSize=2048;keyArt.textureCompression=TextureImporterCompression.CompressedHQ;keyArt.SaveAndReimport();}
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            var ps=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=ps.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;ps.ApplyModifiedPropertiesWithoutUndo();}
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/SindoorRenderer.asset");
            if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/Settings/SindoorRenderer.asset");}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/SindoorURP.asset");
            if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,"Assets/Settings/SindoorURP.asset");}
            pipeline.msaaSampleCount=4;pipeline.renderScale=1;pipeline.shadowDistance=180;pipeline.supportsHDR=true;pipeline.mainLightShadowmapResolution=2048;pipeline.shadowCascadeCount=2;pipeline.cascade2Split=.25f;
            var pipelineSettings=new SerializedObject(pipeline);pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue=true;pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;QualitySettings.vSyncCount=1;QualitySettings.shadowDistance=180;EditorUtility.SetDirty(pipeline);
            // Explicit shader references prevent stripping of runtime-created procedural materials.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            foreach(var name in new[]{"Universal Render Pipeline/Lit","Universal Render Pipeline/Unlit","Skybox/Procedural","Sindoor/TheatreTerrain","Sindoor/SoftCloud","Sindoor/Particle","Sindoor/Water","Sindoor/Canopy"}){var shader=Shader.Find(name);if(shader==null)throw new Exception("Required shader missing: "+name);bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int index=shaders.arraySize;shaders.InsertArrayElementAtIndex(index);shaders.GetArrayElementAtIndex(index).objectReferenceValue=shader;}}
            settings.ApplyModifiedPropertiesWithoutUndo();Content();GenerateScenes();AssetDatabase.SaveAssets();Debug.Log("SINDOOR_SETUP_OK");
        }
        public static void Content(){
            VisualAssetBuilder.Build();var library=AssetDatabase.LoadAssetAtPath<VisualAssetLibrary>("Assets/Resources/Visuals/VisualAssetLibrary.asset");var model=UnityEngine.Object.Instantiate(library.kestrel);PrefabUtility.SaveAsPrefabAsset(model,"Assets/Prefabs/KestrelF1.prefab");UnityEngine.Object.DestroyImmediate(model);
            File.WriteAllText("Assets/Resources/CampaignManifest.json",JsonUtility.ToJson(new Manifest(),true));AssetDatabase.Refresh();
        }
        [Serializable] class Manifest {public string title="Operation Sindoor";public string airframe="Kestrel F.1";public string[] missions=Mission.All.Select(m=>m.title).ToArray();public string disclaimer=OperationGame.Disclaimer;}
        static string Sanitize(string s)=>string.Concat(s.Select(c=>char.IsLetterOrDigit(c)?c:'_'));
        public static void GenerateScenes(){
            var build=new EditorBuildSettingsScene[Scenes.Length];
            for(int i=0;i<Scenes.Length;i++){string path="Assets/Scenes/"+Scenes[i]+".unity";if(!File.Exists(path)){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var g=new GameObject("Scene gateway / "+Scenes[i]);var entry=g.AddComponent<SceneGateway>();entry.destination=Scenes[i];EditorSceneManager.SaveScene(scene,path);}build[i]=new EditorBuildSettingsScene(path,true);}
            EditorBuildSettings.scenes=build;EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
        }
        public static void Validate(){
            if(EditorUtility.scriptCompilationFailed)throw new Exception("Compilation failed");if(Mission.All.Length!=5)throw new Exception("Campaign mission count");
            if(EditorBuildSettings.scenes.Length!=Scenes.Length)throw new Exception("Expected all "+Scenes.Length+" scene gateways");
            foreach(var scene in EditorBuildSettings.scenes){if(!scene.enabled||!File.Exists(scene.path))throw new Exception("Missing scene "+scene.path);var s=EditorSceneManager.OpenScene(scene.path);foreach(var root in s.GetRootGameObjects())foreach(var child in root.GetComponentsInChildren<Transform>(true)){if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject)>0)throw new Exception("Missing script "+scene.path);}}
            if(!GraphicsSettings.defaultRenderPipeline)throw new Exception("URP not configured");if(!AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KestrelF1.prefab"))throw new Exception("Missing aircraft prefab");
            for(int x=-16000;x<=16000;x+=250)for(int z=-16000;z<=16000;z+=250){float height=WorldFactory.Height(x,z);if(float.IsNaN(height)||height<0||height>900)throw new Exception("Terrain outside playable height range");}
            string folder=Path.GetFullPath("Artifacts");Directory.CreateDirectory(folder);string save=Path.Combine(folder,"save-test-"+Guid.NewGuid().ToString("N")+".json");var test=new SaveData{unlocked=4,completed=7};Saves.Write(test,save);test.unlocked=5;Saves.Write(test,save);if(Saves.Read(save).unlocked!=5)throw new Exception("Save round trip failed");File.WriteAllText(save,"broken json");if(Saves.Read(save).unlocked!=4)throw new Exception("Save backup recovery failed");
            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");File.WriteAllText(Path.Combine(folder,"editor-validation.txt"),"PASS: compilation, 12 build scene gateways, missing-script check, URP, aircraft prefab, five missions, save round-trip, corrupt-save backup recovery.\n");Debug.Log("SINDOOR_VALIDATION_OK");
        }
        public static void BuildWindows(){Setup();Validate();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray(),locationPathName="Builds/Windows/OperationSindoor.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            File.WriteAllText("Artifacts/build-result.txt",report.summary.result+"\nSize: "+report.summary.totalSize+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nDuration: "+report.summary.totalTime);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);Debug.Log("SINDOOR_BUILD_OK");
        }
    }
}

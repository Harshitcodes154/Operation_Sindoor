using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Sindoor.Editor {
    // Deterministic original PBR maps. No downloaded art or third-party texture licenses.
    static class VisualMaterialAuthoring {
        // Avoid truncating unchanged PNGs that an importer or sync client may still have mapped.
        internal static void WritePngIfChanged(string path,byte[] bytes){if(File.Exists(path)&&File.ReadAllBytes(path).SequenceEqual(bytes))return;File.WriteAllBytes(path,bytes);}
        public static Material Make(string name,Color tint,int kind,float metal,float smooth,int resolution=512){
            string folder="Assets/GeneratedVisuals/Materials";Directory.CreateDirectory(folder);
            string path=folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(material,path);}
            Texture2D albedo=new Texture2D(resolution,resolution,TextureFormat.RGBA32,true),normal=new Texture2D(resolution,resolution,TextureFormat.RGBA32,true,true),mask=new Texture2D(resolution,resolution,TextureFormat.RGBA32,true,true);
            var colors=new Color[resolution*resolution];var normals=new Color[colors.Length];var masks=new Color[colors.Length];var heights=new float[colors.Length];
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++){
                float u=x/(float)resolution,v=y/(float)resolution,n=Noise(u,v,7)+Noise(u,v,29)*.35f+Noise(u,v,123)*.16f;
                float seam=0,rivet=0,variation=.88f+n*.12f;
                if(kind==0||kind==1){
                    float gx=u*2,gy=v*2;seam=Mathf.Min(Edge(gx),Edge(gy))<.003f?1:0;
                    float rx=Mathf.Abs(Repeat(gx*6)-.5f),ry=Mathf.Abs(Repeat(gy)-.07f);rivet=rx<.055f&&ry<.008f?1:0;
                    float panel=Mathf.PerlinNoise(Mathf.Floor(gx)*3+4,Mathf.Floor(gy)*7+5);variation*=.88f+panel*.17f;
                    if(kind==1)variation*=Noise(u,v,3)>.5f?.74f:1.09f;
                    variation*=1-seam*.1f-rivet*.12f;
                }else if(kind==2){seam=Edge(u*2)<.004f||Edge(v*2)<.004f?1:0;variation*=1-seam*.16f;variation*=.91f+Noise(u,v,3)*.14f;}
                else if(kind==3){variation*=.85f+Noise(u,v,90)*.22f;}
                else if(kind==4){seam=Edge(u*40)<.3f?.7f:0;variation*=1-seam*.16f;}
                else if(kind==5){float weave=Mathf.Sin(u*resolution*Mathf.PI)*Mathf.Sin(v*resolution*Mathf.PI);n+=weave*.12f;variation=.91f+n*.07f;}
                else if(kind==6){variation*=.68f+Noise(u,v,13)*.52f;}
                float grain=Noise(u,v,resolution*.65f);float h=n*.1f+grain*.035f-seam*.14f-rivet*.09f;
                int i=y*resolution+x;heights[i]=h;colors[i]=new Color(tint.r*variation,tint.g*variation,tint.b*variation,1);
                float rough=Mathf.Clamp01(1-smooth+(grain-.5f)*.15f+seam*.16f);float ao=Mathf.Clamp01(1-seam*.33f-rivet*.25f);
                masks[i]=new Color(metal,ao,0,1-rough);
            }
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++){
                float dx=heights[y*resolution+(x+1)%resolution]-heights[y*resolution+(x-1+resolution)%resolution];
                float dy=heights[((y+1)%resolution)*resolution+x]-heights[((y-1+resolution)%resolution)*resolution+x];
                Vector3 n=new Vector3(-dx*3,-dy*3,1).normalized;normals[y*resolution+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            albedo.SetPixels(colors);albedo.Apply();normal.SetPixels(normals);normal.Apply();mask.SetPixels(masks);mask.Apply();
            Texture2D SaveTexture(Texture2D texture,string suffix,bool normalMap){
                string png=folder+"/"+name+suffix+".png";WritePngIfChanged(png,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(png);
                var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.textureType=normalMap?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=suffix=="_BaseColor";importer.maxTextureSize=resolution;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            }
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",SaveTexture(albedo,"_BaseColor",false));material.SetTexture("_BumpMap",SaveTexture(normal,"_Normal",true));var packed=SaveTexture(mask,"_MetalAOGloss",false);material.SetTexture("_MetallicGlossMap",packed);material.SetTexture("_OcclusionMap",packed);material.SetFloat("_Smoothness",1);material.SetFloat("_BumpScale",.7f);material.SetFloat("_OcclusionStrength",.8f);material.SetFloat("_Cull",0);material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_METALLICSPECGLOSSMAP");material.EnableKeyword("_OCCLUSIONMAP");material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
        }
        static float Repeat(float a)=>a-Mathf.Floor(a);
        static float Edge(float a){float f=Repeat(a);return Mathf.Min(f,1-f);}
        static float Noise(float u,float v,float frequency){
            // Blend wrapped samples for seamless tile borders.
            float a=Mathf.PerlinNoise(u*frequency+19,v*frequency+31),b=Mathf.PerlinNoise((u-1)*frequency+19,v*frequency+31),c=Mathf.PerlinNoise(u*frequency+19,(v-1)*frequency+31),d=Mathf.PerlinNoise((u-1)*frequency+19,(v-1)*frequency+31);
            return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);
        }
    }
}

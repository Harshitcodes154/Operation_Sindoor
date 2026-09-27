using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sindoor {
    // Original mesh authoring. Batches each LOD by material and keeps moving parts independent.
    public sealed class VisualMesh {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<int> triangles=new List<int>();
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector2 ta,Vector2 tb,Vector2 tc){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);uv.Add(ta);uv.Add(tb);uv.Add(tc);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);}
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 scale){Triangle(a,b,c,Vector2.zero,new Vector2(scale.x,0),scale);Triangle(a,c,d,Vector2.zero,scale,new Vector2(0,scale.y));}
        public void Box(Vector3 p,Vector3 size,Quaternion rot){
            var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=p+rot*Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size);
            Quad(v[4],v[5],v[7],v[6],new Vector2(size.x,size.y));Quad(v[1],v[0],v[2],v[3],new Vector2(size.x,size.y));Quad(v[0],v[4],v[6],v[2],new Vector2(size.z,size.y));Quad(v[5],v[1],v[3],v[7],new Vector2(size.z,size.y));Quad(v[2],v[6],v[7],v[3],new Vector2(size.x,size.z));Quad(v[0],v[1],v[5],v[4],new Vector2(size.x,size.z));
        }
        public void Box(Vector3 p,Vector3 size)=>Box(p,size,Quaternion.identity);
        public void Ellipsoid(Vector3 centre,Vector3 scale,int segments=20,int rings=12,Quaternion rotation=default){
            if(rotation==default)rotation=Quaternion.identity;
            int start=vertices.Count;
            for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++){
                float u=x/(float)segments,v=y/(float)rings,a=u*Mathf.PI*2,b=v*Mathf.PI;
                vertices.Add(centre+rotation*Vector3.Scale(new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a)),scale));uv.Add(new Vector2(u,v));
            }
            for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int a=start+y*(segments+1)+x,b=a+segments+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
        }
        public void Tube(Vector3 from,Vector3 to,float r0,float r1,int segments=20,bool cap=true){
            var rotation=Quaternion.FromToRotation(Vector3.forward,(to-from).normalized);int start=vertices.Count;
            for(int z=0;z<=1;z++)for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add((z==0?from:to)+rotation*new Vector3(Mathf.Cos(a)*(z==0?r0:r1),Mathf.Sin(a)*(z==0?r0:r1),0));uv.Add(new Vector2(i/(float)segments,z*(to-from).magnitude));}
            for(int i=0;i<segments;i++){int a=start+i,b=a+segments+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            if(cap)for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;Vector3 p=rotation*new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),q=rotation*new Vector3(Mathf.Cos(b),Mathf.Sin(b),0);Triangle(from,from+q*r0,from+p*r0,Vector2.zero,Vector2.right,Vector2.up);Triangle(to,to+p*r1,to+q*r1,Vector2.zero,Vector2.right,Vector2.up);}
        }
        public void Loft(float[] z,float[] width,float[] height,float[] offset,int radial,int subdivisions=3){
            int start=vertices.Count,rows=(z.Length-1)*subdivisions+1;
            for(int row=0;row<rows;row++){
                float index=row/(float)subdivisions;int a=Mathf.Min((int)index,z.Length-2),b=a+1;float t=index-a;
                float zz=Mathf.Lerp(z[a],z[b],t),w=Mathf.SmoothStep(width[a],width[b],t),h=Mathf.SmoothStep(height[a],height[b],t),cy=Mathf.Lerp(offset[a],offset[b],t);
                for(int r=0;r<=radial;r++){float angle=r*Mathf.PI*2/radial;vertices.Add(new Vector3(Mathf.Cos(angle)*w,cy+Mathf.Sin(angle)*h,zz));uv.Add(new Vector2(r/(float)radial*3,(zz-z[0])/(z[z.Length-1]-z[0])*5));}
            }
            for(int s=0;s<rows-1;s++)for(int r=0;r<radial;r++){int a=start+s*(radial+1)+r,b=a+radial+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
        }
        public void Airfoil(Vector3 rootLeading,Vector3 rootTrailing,Vector3 tipLeading,Vector3 tipTrailing,float thickness,int spanSteps=10,int chordSteps=16){
            int start=vertices.Count;int sheet=(spanSteps+1)*(chordSteps+1);
            for(int side=0;side<2;side++)for(int s=0;s<=spanSteps;s++)for(int c=0;c<=chordSteps;c++){
                float span=s/(float)spanSteps,chord=c/(float)chordSteps;var leading=Vector3.Lerp(rootLeading,tipLeading,span);var trailing=Vector3.Lerp(rootTrailing,tipTrailing,span);
                var normal=Vector3.Cross(tipLeading-rootLeading,rootTrailing-rootLeading).normalized;if(normal.y<0)normal=-normal;
                float profile=5*thickness*(.2969f*Mathf.Sqrt(chord)-.126f*chord-.3516f*chord*chord+.2843f*chord*chord*chord-.1036f*Mathf.Pow(chord,4));
                vertices.Add(Vector3.Lerp(leading,trailing,chord)+normal*profile*(1-span*.72f)*(side==0?1:-1));uv.Add(new Vector2(span*3,chord*2));
            }
            bool reverse=Vector3.Cross(tipLeading-rootLeading,rootTrailing-rootLeading).y<0;
            for(int side=0;side<2;side++)for(int s=0;s<spanSteps;s++)for(int c=0;c<chordSteps;c++){
                int a=start+side*sheet+s*(chordSteps+1)+c,b=a+chordSteps+1;
                if((side==0)!=reverse)triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});else triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
            }
        }
        public Mesh ToMesh(string name){var m=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
        public Renderer Attach(Transform parent,string name,Material material){var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=ToMesh(name);var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;return r;}
    }
    public sealed class VisualBatch {
        readonly Dictionary<Material,VisualMesh> parts=new Dictionary<Material,VisualMesh>();
        public VisualMesh this[Material material]{get{if(!parts.TryGetValue(material,out var m)){m=new VisualMesh();parts.Add(material,m);}return m;}}
        public Renderer[] Attach(Transform parent,string prefix){var r=new List<Renderer>();foreach(var item in parts)r.Add(item.Value.Attach(parent,prefix+" / "+item.Key.name,item.Key));return r.ToArray();}
    }
}

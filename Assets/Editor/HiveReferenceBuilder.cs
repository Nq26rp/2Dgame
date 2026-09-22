using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Editor-only authoring: all coordinates use the reference's native 470 x 270 pixel layout.
// The saved scene contains ordinary renderers/colliders and has no runtime builder dependency.
public static class HiveReferenceBuilder
{
    const string Art = "Assets/Art Assets/Legacy-Fantasy - High Forest 2.3/";
    const string Output = "Assets/Art Assets/SceneLayouts/HiveReference";
    const string ScenePath = "Assets/Scenes/Hive.unity";
    const int W = 470, H = 270, Margin = 20;
    static Transform root;
    static Material hiveMaterial;
    static Texture2D atlas;
    static Dictionary<string, Sprite> sprites;
    static readonly List<Vector2[]> solids = new List<Vector2[]>();
    static readonly List<Vector2[]> holes = new List<Vector2[]>();
    static readonly List<string> solidNames = new List<string>();
    static readonly List<Vector3Int> offsets = new List<Vector3Int>();
    static bool[,] mask;

    static Vector2[] P(params float[] xy)
    {
        var result = new Vector2[xy.Length / 2];
        for (int i = 0; i < result.Length; i++) result[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
        return result;
    }
    static Vector3 World(float x, float y) => new Vector3((x - W / 2f) / 16f, (H / 2f - y) / 16f, 0);
    static int Mod(int n, int d) => (n % d + d) % d;
    static bool Inside(Vector2[] polygon, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            if ((polygon[i].y > y) != (polygon[j].y > y) &&
                x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                inside = !inside;
        return inside;
    }
    static bool Solid(int x, int y)
    {
        x += Margin; y += Margin;
        return x >= 0 && y >= 0 && x < mask.GetLength(0) && y < mask.GetLength(1) && mask[x, y];
    }
    static Transform Group(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent == null ? root : parent, false);
        return go.transform;
    }
    static void AddSolid(string name, Vector2[] polygon)
    { solidNames.Add(name); solids.Add(polygon); }

    [MenuItem("Tools/Level Layout/Rebuild Hive From Reference")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save or discard unsaved Hive edits before rebuilding.");
        if (scene.GetRootGameObjects().Any(g => g.GetComponent<Grid>() != null))
            throw new InvalidOperationException("Hive uses editable Tilemaps now. Edit Grid/Platform with the Hive palette; the legacy mesh rebuild is disabled to protect painted work.");
        SceneManager.SetActiveScene(scene);
        // Only this scene is the target of the requested rebuild.
        foreach (var go in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(go);
        root = new GameObject("HIVE_REFERENCE_LAYOUT").transform;
        EnsureFolder(Output);
        atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Assets/Hive.png");
        sprites = AssetDatabase.LoadAllAssetsAtPath(Art + "Assets/Hive.png").OfType<Sprite>().ToDictionary(s => s.name);
        hiveMaterial = MaterialFor("HiveAtlas", atlas, Color.white);
        solids.Clear(); holes.Clear(); solidNames.Clear(); offsets.Clear();

        // Continuous roof, including the descending right-hand exterior wall and ledge.
        AddSolid("01_Ceiling_And_Right_Outer_Wall", P(-20,-20,490,-20,490,224,467,216,453,210,447,193,
            405,193,399,186,399,148,405,142,443,142,448,138,448,87,443,78,433,76,
            428,66,416,63,411,49,404,45,259,45,251,42,247,32,239,29,238,21,229,13,
            222,10,152,10,143,15,141,22,141,41,137,46,75,46,67,43,63,33,57,29,18,29,-20,25));
        AddSolid("02_Left_Entrance_Column_And_Ledge", P(-20,27,19,27,23,34,22,78,27,93,66,95,
            66,100,62,109,19,111,11,107,-20,107));
        AddSolid("03_Continuous_Main_Floor_And_Chest_Dais", P(-20,157,70,157,77,162,79,174,
            83,177,136,177,143,172,145,135,150,126,229,126,235,130,237,149,242,157,
            281,157,287,150,289,143,294,141,334,142,343,149,347,158,347,200,
            342,206,-20,206));
        AddSolid("04_Chest_Alcove_Left_Column", P(147,12,161,12,166,21,166,112,171,127,
            148,133,145,124,148,108,148,33,144,23));
        AddSolid("05_Chest_Alcove_Right_Column", P(216,11,229,14,229,25,226,44,227,113,
            234,128,213,129,213,119,217,105,217,33,214,24));
        AddSolid("06_Right_Chamber_Column", P(309,45,324,45,326,53,324,69,323,126,
            328,144,307,147,305,136,309,119,309,62,306,54));
        AddSolid("07_Lower_Left_Floor", P(-20,252,84,252,91,259,95,272,95,290,-20,290));
        AddSolid("08_Lower_Right_Floor", P(159,261,167,253,177,252,393,252,399,260,399,290,159,290));
        AddSolid("09_Lower_Left_Support", P(68,204,79,204,81,215,80,237,85,247,91,255,
            61,255,67,243,69,231));
        AddSolid("10_Lower_Center_Support", P(165,204,181,204,183,212,179,227,180,243,
            186,254,162,256,160,248,166,234,166,218));

        // Openings are real holes in the rear honeycomb wall, exposing the separate forest layer.
        var windows = new[] {
            new Vector3(70,72,1f), new Vector3(110,95,.60f), new Vector3(197,39,1f),
            new Vector3(31,94,.62f), new Vector3(191,94,.62f), new Vector3(118,152,1f),
            new Vector3(39,230,1f), new Vector3(214,231,1f), new Vector3(309,231,1f) };
        foreach (var w in windows) holes.Add(Hex(w.x,w.y,17.5f*w.z,18f*w.z));
        // Irregular forest opening between the large central floor and right outer ledge.
        holes.Add(P(450,80,470,78,491,87,491,290,396,290,395,252,386,247,380,245,
            370,230,360,229,351,221,350,205,348,194,349,179,346,163,350,160,350,156,
            369,158,373,151,379,149,380,135,391,132,391,119,397,119,397,112,408,109,
            414,112,416,108,426,111,433,108,441,117,446,111));

        var forest = Group("00_Forest_Behind_Openings");
        var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Background/Background.png");
        var skyMat = MaterialFor("ForestSky", skyTex, new Color(.73f,.87f,.74f,1));
        Quad("Distant_Sky", forest, skyMat, new Rect(-20,-20,510,310), new Rect(0,0,480,272), skyTex, -100);
        var darkTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Trees/Dark-Tree.png");
        var greenTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Trees/Green-Tree.png");
        var farMat = MaterialFor("ForestFar", darkTex, Color.white);
        var nearMat = MaterialFor("ForestNear", greenTex, Color.white);
        Quad("Deep_Canopy_Base",forest,farMat,new Rect(-20,-20,510,310),new Rect(54,96,1,1),darkTex,-98);
        for (int i = 0; i < 8; i++)
        {
            Quad("Dark_Conifer_"+i, forest, farMat, new Rect(-50+i*75,-76+(i%3)*23,112,368),
                new Rect((i%2)*112,384,112,320), darkTex,-90+i);
            Quad("Near_Conifer_"+i, forest, nearMat, new Rect(-27+i*72,-110+(i%2)*45,112,368),
                new Rect((i%2)*112,384,112,320), greenTex,-75+i);
            Quad("Lower_Conifer_"+i, forest, nearMat, new Rect(-48+i*75,90+(i%3)*19,112,368),
                new Rect((i%2)*112,384,112,320), greenTex,-65+i);
        }

        var back = Group("10_Honeycomb_Back_Wall_And_Windows");
        Raster("Honeycomb_Wall_With_Openings", back, hiveMaterial, -50, -20,-20,490,290,
            (x,y)=> !holes.Any(p=>Inside(p,x+.5f,y+.5f)),
            (x,y)=>new Vector2Int(Mod(x,16),64+Mod(y,32)));
        int wi = 0;
        foreach (var w in windows)
        {
            float size = 42*w.z;
            var outer = Hex(w.x,w.y,21*w.z,23*w.z);
            var inner = Hex(w.x,w.y,17.5f*w.z,18*w.z);
            Raster("Hex_Window_Frame_"+(++wi),back,hiveMaterial,-35,
                Mathf.FloorToInt(w.x-size/2),Mathf.FloorToInt(w.y-24*w.z),Mathf.CeilToInt(w.x+size/2),Mathf.CeilToInt(w.y+24*w.z),
                (x,y)=>Inside(outer,x+.5f,y+.5f)&&!Inside(inner,x+.5f,y+.5f),
                (x,y)=>new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((x+.5f-w.x)/w.z+24),3,44),
                    Mathf.Clamp(Mathf.FloorToInt((y+.5f-w.y)/w.z+232),211,252)));
        }

        mask = new bool[W+Margin*2,H+Margin*2];
        for (int y=-Margin;y<H+Margin;y++) for(int x=-Margin;x<W+Margin;x++)
            mask[x+Margin,y+Margin]=solids.Any(p=>Inside(p,x+.5f,y+.5f));
        for(int dy=-16;dy<=16;dy++) for(int dx=-16;dx<=16;dx++)
            if(dx*dx+dy*dy>0 && dx*dx+dy*dy<=256) offsets.Add(new Vector3Int(dx,dy,dx*dx+dy*dy));
        offsets.Sort((a,b)=>a.z.CompareTo(b.z));
        var ground = Group("20_Connected_Honey_Terrain");
        Raster("Continuous_Honey_Surface",ground,hiveMaterial,0,-19,-19,489,289,Solid,TerrainUV);
        var collisionRoot=Group("21_Terrain_Collisions");
        for(int i=0;i<solids.Count;i++)
        {
            var tr=Group(solidNames[i],collisionRoot);
            tr.gameObject.layer=LayerMask.NameToLayer("Ground");
            var collider=tr.gameObject.AddComponent<PolygonCollider2D>();
            collider.points=solids[i].Select(p=>(Vector2)World(p.x,p.y)).ToArray();
        }

        var props = Group("30_Props_Chest_Door_And_Torches");
        // Full 48 x 64 door assembly from the source atlas, preserving its rounded lower opening.
        var doorPoly=P(242,109,262,98,283,109,283,157,242,157);
        Raster("Central_Door_Visual",props,hiveMaterial,-10,239,96,285,159,
            (x,y)=>Inside(doorPoly,x+.5f,y+.5f), (x,y)=>new Vector2Int(x-239+64,y-96+160));
        Sprite("Chest_Upper_Closed",29,190,115.5f,props,12);
        Sprite("Chest_Lower_Open",5,46,243,props,12);
        Sprite("Blue_Gem_Above_Lower_Chest",53,46,222,props,12);
        Sprite("Upper_Alcove_Gold",62,190,90,props,-15);
        foreach(var p in new[]{new Vector2(22,133),new Vector2(246,86),new Vector2(278,86),
            new Vector2(103,245),new Vector2(150,245)}) Sprite("Wall_Torch",69,p.x,p.y,props,10);

        var trim=Group("40_Wax_Drips_And_Static_Spikes");
        foreach(var drip in new[]{new Vector3(54,30,41),new Vector3(101,47,44),new Vector3(197,17,39),
            new Vector3(294,47,24),new Vector3(341,47,25),new Vector3(405,49,24),
            new Vector3(7,207,24),new Vector3(119,207,12),new Vector3(150,207,24),
            new Vector3(246,207,12),new Vector3(277,207,24),new Vector3(405,194,39)})
            Drip(drip.x,drip.y,drip.z,trim);
        foreach(var p in new[]{new Vector2(38,38),new Vector2(84,99),new Vector2(251,51),new Vector2(315,210)})
            Sprite("Short_Honey_Drop",15,p.x,p.y,trim,8);
        for(int i=0;i<4;i++) Sprite("Floor_Pit_Spikes_"+i,78+i%3,87+i*16,169,trim,9);
        for(int i=0;i<2;i++) Sprite("Ceiling_Spikes_"+i,81,120+i*16,51,trim,9);
        for(int i=0;i<2;i++) Sprite("Alcove_Wall_Spikes_"+i,89,148,23+i*16,trim,9);
        foreach(var p in new[]{new Vector2(52,159),new Vector2(148,128),new Vector2(289,146),
            new Vector2(309,142),new Vector2(18,255),new Vector2(69,255),new Vector2(167,255),
            new Vector2(193,253),new Vector2(401,146)}) Sprite("Wax_Stone_Edge",59,p.x,p.y,trim,9);
        foreach(var p in new[]{new Vector2(7,151),new Vector2(246,247),new Vector2(278,247),
            new Vector2(373,247),new Vector2(324,136)}) Sprite("Honey_Mound",54,p.x,p.y,trim,9);
        foreach(var p in new[]{new Vector2(23,153),new Vector2(262,249)}) Sprite("Honey_Rock",61,p.x,p.y,trim,9);

        var cameraGo=new GameObject("Main Camera");
        cameraGo.tag="MainCamera";
        cameraGo.transform.position=new Vector3(0,0,-10);
        var camera=cameraGo.AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=H/32f;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.16f,.20f,.075f);camera.nearClipPlane=.1f;camera.farClipPlane=100;
        cameraGo.AddComponent<AudioListener>();
        var light=new GameObject("Scene Light").AddComponent<Light>();light.type=LightType.Directional;
        light.transform.rotation=Quaternion.Euler(50,-30,0);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if(SceneView.lastActiveSceneView!=null)
        { SceneView.lastActiveSceneView.in2DMode=true;SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero,new Vector3(W/16f,H/16f,1)),false); }
        Debug.Log("Hive reference layout saved: continuous honey terrain, 9 forest windows, 10 polygon colliders; props are static.");
    }

    static Vector2[] Hex(float x,float y,float rx,float ry) => P(x,y-ry,x+rx,y-ry*.5f,x+rx,y+ry*.5f,x,y+ry,x-rx,y+ry*.5f,x-rx,y-ry*.5f);
    static Vector2Int TerrainUV(int x,int y)
    {
        foreach(var o in offsets)
        {
            if(Solid(x+o.x,y+o.y))continue;
            int d=Mathf.Clamp(Mathf.FloorToInt(Mathf.Sqrt(o.z)-.5f),0,15);
            if(Mathf.Abs(o.y)>=Mathf.Abs(o.x))
                return new Vector2Int(16+Mod(x,48),o.y<0?16+d:63-d);
            int left=0,right=0;
            while(left<30&&Solid(x-left-1,y))left++;
            while(right<30&&Solid(x+right+1,y))right++;
            // Narrow wax pillars are golden through their centre, not hollow brown wall tiles.
            if(left+right<29&&d>1)return new Vector2Int(5+Mod(x+(y/8%2)*3,8),13+Mod(y,8));
            return new Vector2Int(o.x<0?d:79-d,32+Mod(y,16));
        }
        return new Vector2Int(Mod(x,48),144+Mod(y,16));
    }
    static void Sprite(string name,int index,float x,float y,Transform parent,int order)
    {
        var tr=Group(name,parent);tr.position=World(x,y);
        var sr=tr.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprites["Hive_"+index];sr.sortingOrder=order;
    }
    static void Drip(float x,float y,float length,Transform parent)
    {
        var tr=Group("Hanging_Honey_"+x+"_"+y,parent);
        // The top anchor, repeatable narrow stem and rounded end are distinct atlas pieces.
        Quad("Anchor",tr,hiveMaterial,new Rect(x-6,y,13,5),new Rect(114,128,13,5),atlas,8);
        Quad("Stem",tr,hiveMaterial,new Rect(x-1,y+3,3,length-10),new Rect(119,144,3,10),atlas,8);
        Quad("Droplet",tr,hiveMaterial,new Rect(x-3,y+length-9,7,10),new Rect(117,159,7,10),atlas,8);
    }
    static Material MaterialFor(string name,Texture2D texture,Color color)
    {
        var path=Output+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,path);}
        material.mainTexture=texture;material.color=color;EditorUtility.SetDirty(material);return material;
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        var split=path.LastIndexOf('/');EnsureFolder(path.Substring(0,split));
        AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1));
    }
    sealed class Geometry
    {
        public readonly List<Vector3> v=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<int> triangles=new List<int>();
        public void Add(Rect dest,Rect src,Texture2D tex)
        {
            int n=v.Count;
            v.Add(World(dest.xMin,dest.yMax));v.Add(World(dest.xMax,dest.yMax));
            v.Add(World(dest.xMax,dest.yMin));v.Add(World(dest.xMin,dest.yMin));
            uv.Add(new Vector2(src.xMin/tex.width,1-src.yMax/tex.height));
            uv.Add(new Vector2(src.xMax/tex.width,1-src.yMax/tex.height));
            uv.Add(new Vector2(src.xMax/tex.width,1-src.yMin/tex.height));
            uv.Add(new Vector2(src.xMin/tex.width,1-src.yMin/tex.height));
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
        }
    }
    static void SaveMesh(string name,Transform parent,Material material,int order,Geometry geo)
    {
        string path=Output+"/"+parent.name+"_"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.name=name;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(geo.v);mesh.SetUVs(0,geo.uv);
        mesh.SetColors(Enumerable.Repeat(Color.white,geo.v.Count).ToList());
        mesh.SetTriangles(geo.triangles,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var tr=Group(name,parent);tr.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=tr.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=material;mr.sortingOrder=order;
        mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;
    }
    static void Quad(string name,Transform parent,Material material,Rect dest,Rect source,Texture2D texture,int order)
    { var geo=new Geometry();geo.Add(dest,source,texture);SaveMesh(name,parent,material,order,geo); }
    static void Raster(string name,Transform parent,Material material,int order,int x0,int y0,int x1,int y1,
        Func<int,int,bool> include,Func<int,int,Vector2Int> source)
    {
        // Merge adjacent source pixels into horizontal UV strips; no screenshot/flattened bitmap is used.
        var geo=new Geometry();
        for(int y=y0;y<y1;y++)
        {
            int x=x0;
            while(x<x1)
            {
                if(!include(x,y)){x++;continue;}
                int start=x;var uv=source(x,y);x++;
                while(x<x1&&include(x,y))
                {
                    var next=source(x,y);
                    if(next.y!=uv.y||next.x!=uv.x+x-start)break;
                    x++;
                }
                geo.Add(new Rect(start,y,x-start,1),new Rect(uv.x,uv.y,x-start,1),atlas);
            }
        }
        SaveMesh(name,parent,material,order,geo);
    }

    public static string CapturePreview()
    {
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid()||!scene.isLoaded)throw new InvalidOperationException("Open Hive first.");
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
        var previousTarget=camera.targetTexture;
        var other=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene)
            .SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))
            .ToDictionary(r=>r,r=>r.forceRenderingOff);
        var previousActive=RenderTexture.active;
        float previousAspect=camera.aspect;
        var target=new RenderTexture(1504,864,24,RenderTextureFormat.ARGB32);
        var pixels=new Texture2D(1504,864,TextureFormat.RGB24,false);
        const string path="Assets/Screenshots/LevelPreviews/Hive_Reference_Final.png";
        try
        {
            foreach(var r in other.Keys)r.forceRenderingOff=true;
            camera.targetTexture=target;camera.aspect=W/(float)H;
            camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1504,864),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally
        {
            foreach(var kv in other)if(kv.Key!=null)kv.Key.forceRenderingOff=kv.Value;
            camera.targetTexture=previousTarget;camera.aspect=previousAspect;
            RenderTexture.active=previousActive;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
        }
        AssetDatabase.ImportAsset(path);
        return path;
    }
}

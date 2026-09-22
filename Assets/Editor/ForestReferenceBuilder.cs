using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Editor-only layout authoring. Pixel coordinates follow the four original 480 x 270 references.
// Generated meshes reference the project's original atlases; no reference screenshot is used in-game.
public static class ForestReferenceBuilder
{
    const string Art="Assets/Art Assets/Legacy-Fantasy - High Forest 2.3/";
    const string Output="Assets/Art Assets/SceneLayouts/ForestReference";
    const int W=480,H=270,M=24;
    static int level,serial;
    static Transform root,background,decor,foreground;
    static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static readonly Dictionary<string,Color32[]> rawPixels=new Dictionary<string,Color32[]>();
    static readonly List<Vector3Int> offsets=new List<Vector3Int>();
    static readonly List<Vector2[]> terrain=new List<Vector2[]>();
    static readonly List<string> terrainNames=new List<string>();

    static Vector2[] P(params float[] xy)
    {var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
    static Vector3 World(float x,float y)=>new Vector3((x-240)/16f,(135-y)/16f,0);
    static int Mod(int a,int b)=>(a%b+b)%b;
    static bool In(Vector2[] p,float x,float y)
    {
        bool hit=false;
        for(int i=0,j=p.Length-1;i<p.Length;j=i++)
            if((p[i].y>y)!=(p[j].y>y)&&x<(p[j].x-p[i].x)*(y-p[i].y)/(p[j].y-p[i].y)+p[i].x)hit=!hit;
        return hit;
    }
    static void Folder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;int s=path.LastIndexOf('/');Folder(path.Substring(0,s));AssetDatabase.CreateFolder(path.Substring(0,s),path.Substring(s+1));}
    static Transform Group(string name,Transform parent=null)
    {var g=new GameObject(name);g.transform.SetParent(parent==null?root:parent,false);return g.transform;}
    static Texture2D Tex(string path)
    {if(!textures.ContainsKey(path))textures[path]=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+path);return textures[path];}
    static Color32[] Raw(string path)
    {
        if(!rawPixels.ContainsKey(path))
        {
            var t=new Texture2D(2,2);ImageConversion.LoadImage(t,System.IO.File.ReadAllBytes(Art+path));
            rawPixels[path]=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);
        }
        return rawPixels[path];
    }
    static Material Mat(string key,string texture,Color? tint=null)
    {
        if(materials.ContainsKey(key))return materials[key];
        string path=Output+"/"+key+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(mat,path);}
        mat.mainTexture=Tex(texture);mat.color=tint??Color.white;EditorUtility.SetDirty(mat);materials[key]=mat;return mat;
    }
    sealed class Geometry
    {
        public readonly List<Vector3> v=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<int> t=new List<int>();
        public void Add(Rect dst,Rect src,Texture2D tex,bool flip=false)
        {
            int n=v.Count;
            v.Add(World(dst.xMin,dst.yMax));v.Add(World(dst.xMax,dst.yMax));
            v.Add(World(dst.xMax,dst.yMin));v.Add(World(dst.xMin,dst.yMin));
            float a=(flip?src.xMax:src.xMin)/tex.width,b=(flip?src.xMin:src.xMax)/tex.width;
            uv.Add(new Vector2(a,1-src.yMax/tex.height));uv.Add(new Vector2(b,1-src.yMax/tex.height));
            uv.Add(new Vector2(b,1-src.yMin/tex.height));uv.Add(new Vector2(a,1-src.yMin/tex.height));
            t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3);
        }
    }
    static GameObject Save(string name,Transform parent,Material mat,int order,Geometry geo)
    {
        string path=Output+"/Forest"+level+"/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.name=name;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(geo.v);mesh.SetUVs(0,geo.uv);
        mesh.SetColors(Enumerable.Repeat(Color.white,geo.v.Count).ToList());mesh.SetTriangles(geo.t,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var tr=Group(name,parent);tr.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=tr.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.sortingOrder=order;
        mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;return tr.gameObject;
    }
    static GameObject Quad(string name,string texture,Rect source,Rect destination,Transform parent,int order,string material=null,Color? color=null,bool flip=false)
    {
        var geo=new Geometry();geo.Add(destination,source,Tex(texture),flip);
        return Save(name,parent,Mat(material??texture.Replace('/','_').Replace(".png",""),texture,color),order,geo);
    }
    static void Patch(string name,string texture,Rect source,float x,float y,Transform parent,int order,float scale=1,bool flip=false)
    {Quad(name+"_"+(serial++),texture,source,new Rect(x,y,source.width*scale,source.height*scale),parent,order,null,null,flip);}
    static GameObject Raster(string name,string texture,Transform parent,int order,RectInt rect,Func<int,int,bool> include,Func<int,int,Vector2Int> source,string material=null,Color? color=null)
    {
        var geo=new Geometry();var tex=Tex(texture);
        for(int y=rect.yMin;y<rect.yMax;y++)
        {
            int x=rect.xMin;
            while(x<rect.xMax)
            {
                if(!include(x,y)){x++;continue;}
                int start=x;var p=source(x,y);x++;
                while(x<rect.xMax&&include(x,y)){var q=source(x,y);if(q.y!=p.y||q.x!=p.x+x-start)break;x++;}
                geo.Add(new Rect(start,y,x-start,1),new Rect(p.x,p.y,x-start,1),tex);
            }
        }
        return Save(name,parent,Mat(material??texture.Replace('/','_').Replace(".png",""),texture,color),order,geo);
    }
    static void PolygonCollision(string name,Vector2[] p,Transform parent)
    {
        var g=Group(name,parent).gameObject;g.layer=LayerMask.NameToLayer("Ground");
        g.AddComponent<PolygonCollider2D>().points=p.Select(v=>(Vector2)World(v.x,v.y)).ToArray();
    }
    static void BoxCollision(GameObject g,Rect box)
    {
        g.layer=LayerMask.NameToLayer("Ground");var c=g.AddComponent<BoxCollider2D>();
        c.offset=World(box.center.x,box.center.y);c.size=new Vector2(box.width/16f,box.height/16f);
    }
    static void Solid(string name,Vector2[] p){terrainNames.Add(name);terrain.Add(p);}
    static void Surface(string name,List<Vector2[]> polygons,string kind,Transform parent,int order,bool collision)
    {
        var mask=new bool[W+M*2,H+M*2];
        for(int y=-M;y<H+M;y++)for(int x=-M;x<W+M;x++)mask[x+M,y+M]=polygons.Any(p=>In(p,x+.5f,y+.5f));
        Func<int,int,bool> inside=(x,y)=>x>=-M&&y>=-M&&x<W+M&&y<H+M&&mask[x+M,y+M];
        string texture=kind=="wood"?"Assets/Tree-Assets.png":kind=="honey"?"Assets/Hive.png":"Assets/Tiles.png";
        Vector2Int Sample(int x,int y)
        {
            foreach(var o in offsets)
            {
                if(inside(x+o.x,y+o.y))continue;
                int d=Mathf.Clamp(Mathf.FloorToInt(Mathf.Sqrt(o.z)-.5f),0,23);
                if(kind=="honey")
                {
                    if(d>=16)break;
                    if(Mathf.Abs(o.y)>=Mathf.Abs(o.x))return new Vector2Int(16+Mod(x,48),o.y<0?16+d:63-d);
                    int left=0,right=0;
                    while(left<30&&inside(x-left-1,y))left++;
                    while(right<30&&inside(x+right+1,y))right++;
                    if(left+right<29&&d>1)return new Vector2Int(5+Mod(x+(y/8%2)*3,8),13+Mod(y,8));
                    return new Vector2Int(o.x<0?d:79-d,32+Mod(y,16));
                }
                if(kind=="wood")
                {
                    if(d>=16)break;
                    if(Mathf.Abs(o.y)>=Mathf.Abs(o.x))return new Vector2Int(16+Mod(x,48),o.y<0?12+d:79-d);
                    return new Vector2Int(o.x<0?d:79-d,32+Mod(y,32));
                }
                bool up=o.y<0&&Mathf.Abs(o.y)>=Mathf.Abs(o.x)*.55f;
                if(kind=="grass"&&up)return new Vector2Int(16+Mod(x,48),8+Mathf.Min(d,23));
                if(d>=16)break;
                if(Mathf.Abs(o.y)>=Mathf.Abs(o.x))return new Vector2Int(16+Mod(x,48),o.y<0?88+Mathf.Min(d,23):159-Mathf.Min(d,15));
                return new Vector2Int(o.x<0?Mathf.Min(d,15):79-Mathf.Min(d,15),112+Mod(y,32));
            }
            if(kind=="honey")return new Vector2Int(Mod(x,48),144+Mod(y,16));
            if(kind=="wood")return new Vector2Int(16+Mod(x,48),32+Mod(y,32));
            return new Vector2Int(16+Mod(x,48),112+Mod(y,32));
        }
        var pixels=Raw(texture);var sourceTexture=Tex(texture);
        Vector2Int UV(int x,int y)
        {
            var p=Sample(x,y);
            if(pixels[(sourceTexture.height-1-p.y)*sourceTexture.width+p.x].a<240)
                return kind=="wood"?new Vector2Int(30,45):kind=="honey"?new Vector2Int(25,151):new Vector2Int(25,125);
            return p;
        }
        Raster(name,texture,parent,order,new RectInt(-23,-23,526,316),inside,UV);
        if(collision)
        {
            var colliders=Group(name+"_Collisions",parent);
            for(int i=0;i<polygons.Count;i++)PolygonCollision(i<terrainNames.Count?terrainNames[i]:name+"_"+i,polygons[i],colliders);
        }
    }
    static void Cliff(string name,Vector2[] p,int order=-45)
    {Surface(name,new List<Vector2[]>{p},"rock",background,order,false);}
    static void Bush(float x,float y,int type=0,int order=7,float scale=1)
    {Patch("Leaf_Cluster","Assets/Tree-Assets.png",new Rect(208,type*96,128,96),x,y,order<0?background:decor,order,scale);}
    static void Tree(float x,float y,int variant=0,bool dark=false,int order=-60,float scale=1)
    {Quad("Forest_Tree_"+(serial++),"Trees/"+(dark?"Dark":"Green")+"-Tree.png",new Rect(variant%2*112,384,112,320),new Rect(x,y,112*scale,368*scale),background,order);}
    static void Trunk(float x,float y,int order)
    {
        // Repeat bark at native pixel density, then place the original hollow over it.
        var geo=new Geometry();var tex=Tex("Assets/Tiles.png");
        float top=Mathf.Max(-24,y),bottom=y+369;
        for(float yy=top;yy<bottom;yy+=32)
            geo.Add(new Rect(x,yy,32,Mathf.Min(32,bottom-yy)),new Rect(160,96,32,Mathf.Min(32,bottom-yy)),tex);
        Save("Ancient_Trunk_"+(serial++),background,Mat("Tiles","Assets/Tiles.png"),order+2,geo);
        Patch("Trunk_Hollow","Assets/Tiles.png",new Rect(144,64,20,48),x,level==1?133:level==2?35:98,background,order+3);
    }
    static void DistantTree(float center,float top,bool far,int order,float width=128)
    {
        // Three authored tiles, excluding the transparent 16-pixel gutters in the atlas.
        float sx=width/112f,sy=300f/256f;
        Quad("Distant_Left_"+(serial++),"Trees/Background.png",new Rect(far?352:0,16,96,240),new Rect(center-96*sx,top+16*sy,96*sx,240*sy),background,order);
        Quad("Distant_Center_"+(serial++),"Trees/Background.png",new Rect(far?464:112,0,96,256),new Rect(center,top,96*sx,300),background,order);
        Quad("Distant_Right_"+(serial++),"Trees/Background.png",new Rect(far?576:224,0,128,256),new Rect(center+96*sx,top,128*sx,300),background,order);
    }
    static void ForestBackground(bool enclosed)
    {
        Quad("Sky","Background/Background.png",new Rect(0,0,480,270),new Rect(0,0,W,H),background,-200);
        if(enclosed)Quad("Deep_Canopy_Color","Trees/Green-Tree.png",new Rect(54,96,1,1),new Rect(-24,-24,528,318),background,-190);
        DistantTree(160,0,true,-160);DistantTree(355,62,true,-159,115);
        DistantTree(116,38,false,-150,103);DistantTree(236,82,false,-149,110);
        DistantTree(436,111,false,-148,100);
        if(enclosed)
        {
            for(int i=0;i<7;i++)Tree(-70+i*90,-95+(i%3)*42,i%3,true,-120+i,1.15f);
            for(int i=0;i<6;i++)Tree(-25+i*100,105+(i%2)*22,i%3,false,-100+i);
        }
    }
    static void Mushroom(float x,float floor,int style=0,float scale=1)
    {
        var r=style==0?new Rect(256,240,32,32):style==1?new Rect(288,240,32,32):new Rect(240,256,16,16);
        Patch("Mushroom","Assets/Tiles.png",r,x,floor-r.height*scale,decor,15,scale);
    }
    static void Cluster(float x,float floor,bool cone=false)
    {Mushroom(x,floor,cone?1:0);Mushroom(x-10,floor-2,2,.9f);Mushroom(x+26,floor+1,2);}
    static void Plant(float x,float floor,bool flower=false,float scale=1)
    {Patch(flower?"Blue_Flower":"Fern","Assets/Tiles.png",flower?new Rect(240,272,16,12):new Rect(320,360,32,40),x,floor-(flower?12:40)*scale,decor,12,scale);}
    static void Reed(float x,float floor)
    {Patch("River_Reed","Assets/Tiles.png",new Rect(256,288,16,32),x,floor-32,decor,12);}
    static void Lavender(float x,float floor,int stems=3)
    {
        Patch("Lavender","Assets/Tiles.png",new Rect(272,272,16,32),x,floor-32,decor,13);
    }
    static void Rock(float x,float floor,int style=0,float scale=1)
    {
        Rect r=style==0?new Rect(6,1,55,79):style==1?new Rect(6,82,55,78):style==2?new Rect(128,34,48,46):new Rect(66,1,30,31);
        Patch("Rock","Assets/Props-Rocks.png",r,x,floor-r.height*scale,decor,3,scale);
    }
    static void Bridge(string name,float x,float y,float length,bool rope)
    {
        var g=Group(name,decor);var geo=new Geometry();var tex=Tex("Assets/Tiles.png");
        if(rope)
        {
            geo.Add(new Rect(x-5,y-27,16,40),new Rect(80,112,16,40),tex);
            geo.Add(new Rect(x+length-10,y-27,16,40),new Rect(144,112,16,40),tex);
        }
        for(float dx=0;dx<length;dx+=16)
            geo.Add(new Rect(x+dx,y,Mathf.Min(16,length-dx),10),new Rect(96,96,Mathf.Min(16,length-dx),10),tex);
        var mesh=Save("Deck_"+name,g,Mat("Tiles","Assets/Tiles.png"),17,geo);
        BoxCollision(mesh,new Rect(x,y,length,5));
    }
    static void BranchHive(string name,float x,float y,float length,bool big,bool hive=true,bool flip=false)
    {
        var g=Group(name,decor);
        var b=Quad(name+"_Branch","Assets/Tiles.png",new Rect(160,168,80,24),new Rect(x,y-7,length,24),g,10,null,null,flip);
        BoxCollision(b,new Rect(x+4,y+1,length-8,5));
        if(hive)
        {
            Rect source=big?new Rect(192,104,32,40):new Rect(192,40,16,24);
            Patch("Hive_Visual","Assets/Tiles.png",source,x+length*.54f-source.width*.5f,y+1,g,12);
        }
    }
    static void Water(string name,Rect rect,bool fall,int order)
    {
        Raster(name,"Assets/Tiles.png",decor,order,new RectInt((int)rect.x,(int)rect.y,(int)rect.width,(int)rect.height),
            (x,y)=>true,(x,y)=>fall?new Vector2Int(64+Mod(x-(int)rect.x,16),288+Mod(y,16)):
                y-rect.y<8?new Vector2Int(96+Mod(x,32),300+(int)(y-rect.y)):
                new Vector2Int(96+Mod(x,48),308+Mod(y,16)));
    }
    static void ThornRow(float x,float y,int count,bool vertical=false)
    {for(int i=0;i<count;i++)Patch("Static_Thorns","Assets/Tree-Assets.png",vertical?new Rect(128,72,16,24):new Rect(88,64,24,16),x+(vertical?0:i*16),y+(vertical?i*16:0),decor,20);}

    static void Forest1()
    {
        ForestBackground(false);
        Trunk(52,-145,-66);Trunk(226,-145,-61);Trunk(383,-107,-56);
        Tree(10,-146,0,false,-65);Tree(183,-145,0,false,-60);Tree(340,-107,2,false,-55);
        Tree(53,86,0,true,-62);Tree(271,131,2,true,-57);
        Cliff("Left_Vertical_Cliff",P(-24,-24,27,-24,25,3,21,18,23,32,15,49,14,66,18,85,13,126,8,140,13,217,-24,233));
        Solid("Left_Upper_Ledge",P(-24,79,5,80,11,75,29,76,44,80,57,80,62,89,62,96,84,104,107,107,123,113,121,125,111,132,86,136,68,145,40,150,19,146,-24,148));
        Solid("Lower_Valley_Left",P(-24,219,9,225,17,237,26,243,41,240,54,233,76,235,91,241,111,243,130,238,151,225,175,217,200,219,211,225,222,232,239,234,261,242,272,257,272,295,-24,295));
        Solid("Upper_Right_Shelf",P(355,11,370,8,386,9,398,5,422,6,440,-3,504,-9,504,40,475,43,454,43,438,37,417,36,400,44,382,47,365,43,357,37));
        Solid("Right_Cliff_And_Lower_Slope",P(481,27,504,27,504,295,322,295,326,268,340,254,352,246,356,235,369,228,380,222,390,218,407,226,418,225,430,207,435,192,438,174,432,161,421,163,413,182,402,197,396,203,387,196,387,179,393,167,398,145,405,129,415,117,422,99,430,84,438,79,442,61,456,53,473,49));
        Surface("Grass_Valley",terrain,"grass",foreground,0,true);
        Rock(64,112,2,1f);
        Patch("Mossy_Upper_Stump","Assets/Tiles.png",new Rect(80,0,64,64),58,63,decor,4,.75f);
        Rock(30,229,0,.64f);Rock(69,235,0,.42f);Rock(148,227,1,.86f);
        Bush(7,196,0,5,.7f);Bush(200,204,0,5,.55f);Bush(266,242,0,4,.48f);
        Bridge("Upper_Left_Bridge",123,110,66,false);
        Patch("Left_Bridge_Anchor","Assets/Tiles.png",new Rect(80,112,16,40),118,83,decor,16);
        BranchHive("Suspended_Hive",260,43,61,true);
        Mushroom(22,79,2);Mushroom(31,72,2,.8f);Cluster(371,13);Cluster(392,223);
        Mushroom(425,159,1,.65f);Mushroom(416,157,2,.85f);Mushroom(444,159,2,.8f);
        Lavender(65,102,3);Lavender(96,109,2);Lavender(36,235,3);Lavender(126,237,3);Lavender(356,236,3);
        Plant(95,236,false,.72f);Plant(191,218,true);Plant(207,202,true);Plant(448,154,true);
        Reed(256,248);Reed(292,273);Reed(324,270);Reed(372,226);
        // Single luminous rune from the same rock atlas, used only as a visual mark.
        Patch("Blue_Rune","Assets/Props-Rocks.png",new Rect(148,256,12,16),167,176,decor,8);
        ThornRow(-1,139,5,true);
        Water("Lower_Stream",new Rect(270,269,67,28),false,25);
    }
    static void Forest2()
    {
        ForestBackground(false);Trunk(-3,-90,-36);Trunk(170,-95,-35);Trunk(456,-95,-35);
        Tree(-47,-75,0,false,-34);Tree(123,-96,0,false,-33);Tree(408,-96,2,false,-32);
        Tree(91,105,0,false,-54);Tree(289,82,2,false,-53);
        Cliff("Left_Canyon_Back_Wall",P(-24,-24,98,-24,95,239,72,247,17,224,-24,201));
        Cliff("Central_Canyon_Back_Wall",P(203,-24,354,-24,354,89,343,96,330,91,320,98,315,113,319,124,316,136,320,164,307,178,302,211,293,236,238,248,177,228,165,196,164,142,173,126,189,128,205,117));
        Water("Waterfall_Left",new Rect(97,-10,64,252),true,-21);
        Water("Waterfall_Right",new Rect(354,-10,48,252),true,-21);
        Solid("Left_Water_Bank",P(-24,192,4,195,24,202,49,207,64,220,73,231,75,241,66,251,48,255,23,257,14,282,-24,294));
        Solid("Central_Island",P(163,130,171,124,190,124,202,129,220,127,238,131,252,135,268,139,286,141,297,143,317,139,321,152,317,166,305,176,285,178,268,180,248,175,230,171,215,165,202,167,184,161,169,158,165,150));
        Solid("Right_Upper_Ledge",P(394,10,422,9,439,9,454,1,505,-10,505,53,479,56,461,46,443,49,427,45,415,40,401,41,392,32));
        Solid("Right_Bridge_Anchorage",P(443,139,453,135,468,137,480,127,505,123,505,175,485,175,469,171,456,167,444,159));
        Surface("Canyon_Grass_Platforms",terrain,"grass",foreground,0,true);
        Rock(234,126,0,1.05f);Rock(285,148,1,.65f);Rock(27,200,2,1f);Rock(104,239,0,.34f);Rock(418,243,0,.40f);
        Bush(212,108,0,4,.57f);Bush(65,219,0,3,.48f);Bush(244,222,0,3,.62f);
        Cluster(177,126);Cluster(270,142);Cluster(436,14,true);
        Plant(211,106,true);Plant(59,203,false,.55f);Plant(272,239,false,.55f);
        Bridge("Waterfall_Crossing",318,142,124,true);
        BranchHive("Left_Hive_Branch",38,90,75,true);BranchHive("Upper_Left_Branch",100,44,81,false,false);
        foreach(float x in new[]{99,148,245,343,388})Reed(x,243);
        Water("Lower_Pool",new Rect(47,238,457,57),false,25);
        foreach(float x in new[]{181,293,454})Patch("White_Water_Lily","Assets/Tiles.png",new Rect(304,304,32,20),x,229,decor,30);
        Bush(-41,247,2,35,.8f);
    }
    static void Forest3()
    {
        ForestBackground(true);
        Tree(57,-38,0,true,-80);Tree(187,-65,0,false,-65);Tree(303,-90,2,true,-60);Tree(401,-130,0,false,-50);
        Solid("Upper_Hollow_Ceiling",P(-24,-24,319,-24,319,24,331,26,341,33,345,42,346,65,353,69,353,144,349,155,340,159,329,158,318,151,313,142,305,139,302,133,300,121,294,116,291,100,287,94,117,94,111,99,110,121,106,126,36,126,30,132,29,235,22,241,10,239,-24,248));
        Solid("Lower_Root_Path",P(-24,237,18,237,26,234,118,237,125,242,127,253,135,257,201,257,211,254,219,247,224,240,226,218,230,209,237,207,342,207,348,211,352,224,353,294,-24,294));
        Solid("Right_Hollow_Wall",P(421,-24,504,-24,504,294,418,294,418,230,422,223,430,220,453,220,460,216,463,208,462,132,453,121,450,58,447,51,431,49,422,43,420,30));
        Surface("Ancient_Tree_Hollow",terrain,"wood",foreground,0,true);
        Quad("Lower_Left_Hollow","Assets/Tiles.png",new Rect(144,64,20,48),new Rect(2,196,24,45),decor,2);
        Bush(-34,-19,2,5,1.3f);Bush(88,-28,2,6,1.2f);Bush(19,196,0,-8,.8f);Bush(177,203,0,-8,.70f);Bush(366,260,1,18,.7f);
        ThornRow(107,96,12);ThornRow(345,66,7);ThornRow(322,152,4,true);
        ShelfMushroom("Upper_Wall_Mushroom",290,132,28);
        ShelfMushroom("Lower_Step_Mushroom",210,245,19);
        ShelfMushroom("Middle_Step_Mushroom",217,230,16);
        Cluster(430,222,true);
        Patch("Fallen_Acorns_Left","Assets/Tree-Assets.png",new Rect(96,48,32,16),48,225,decor,13);
        Patch("Fallen_Acorns_Center","Assets/Tree-Assets.png",new Rect(96,48,32,16),255,193,decor,13);
        Quad("Green_Acorn","Assets/Tree-Assets.png",new Rect(96,48,16,16),new Rect(174,244,16,16),decor,13,"Green_Acorn_Tint",new Color(.65f,1.3f,.18f,1));
        Quad("Green_Acorn_Right","Assets/Tree-Assets.png",new Rect(112,48,16,16),new Rect(283,193,16,16),decor,13,"Green_Acorn_Tint",new Color(.65f,1.3f,.18f,1));
        Bush(-41,251,2,30,.9f);Bush(213,258,2,30,1f);
    }
    static void ShelfMushroom(string name,float x,float y,float width)
    {
        var g=Quad(name,"Assets/Tree-Assets.png",new Rect(128,0,24,16),new Rect(x,y-5,width,16),decor,20);
        BoxCollision(g,new Rect(x+2,y,width-4,3));
    }
    static void Forest4()
    {
        ForestBackground(true);
        Trunk(117,-80,-56);Trunk(264,-82,-53);
        Tree(-25,-80,0,true,-70);Tree(74,-80,0,false,-55);Tree(221,-82,0,false,-52);Tree(368,-32,2,true,-51);
        Bush(-40,-34,2,-49,1.1f);Bush(139,-49,1,-48,1.0f);Bush(297,-68,1,-48,1.05f);
          var opening=P(306,19,314,17,320,21,329,19,333,24,345,23,353,27,366,26,380,29,380,36,368,45,360,60,347,65,346,73,337,72,333,81,325,83,321,92,313,89,310,94,303,91,305,84,300,81,303,72,300,68,305,61,301,54,304,47,301,40,306,33);
        Raster("Canopy_Sky_Opening","Background/Background.png",background,-47,new RectInt(296,-8,90,108),
            (x,y)=>In(opening,x+.5f,y+.5f),(x,y)=>new Vector2Int(Mod(x,480),Mod(y,270)));
        // Right-hand entrance transitions from tree canopy into the Hive's gold-wax walls.
        var honeyBack=P(368,-24,504,-24,504,163,456,173,411,170,387,153,399,126,403,73,395,59,381,42,372,31);
        Raster("Hive_Entrance_Back_Wall","Assets/Hive.png",background,-35,new RectInt(350,-23,154,220),
            (x,y)=>In(honeyBack,x+.5f,y+.5f),(x,y)=>new Vector2Int(Mod(x,16),64+Mod(y,32)));
        Solid("Hive_Roof",P(371,-24,504,-24,504,66,478,62,458,60,446,63,430,62,418,57,413,51,397,49,389,44,383,36,374,31,369,25));
        Solid("Hive_Entrance_Pillar",P(424,59,442,61,444,70,437,83,439,112,447,127,419,130,420,119,424,104,424,77,419,68));
        Solid("Hive_Entrance_Ledge",P(390,126,410,127,417,122,435,128,451,127,463,129,474,128,486,115,504,110,504,160,492,167,478,168,469,174,453,178,440,174,427,170,415,172,399,169,390,161,386,150,386,136));
        Surface("Hive_Entrance_Honey_Terrain",terrain,"honey",foreground,0,true);
        BranchHive("High_Left_Hive",83,12,48,false);BranchHive("Upper_Central_Hive",166,43,59,true);
        BranchHive("Left_Middle_Hive",19,109,102,false);BranchHive("Central_Small_Hive",170,141,54,false);
        BranchHive("Lower_Left_Hive",68,175,60,false);BranchHive("Lower_Right_Large_Hive",313,206,78,true);
        BranchHive("Lower_Right_Small_Hive",370,206,49,false);
        Bridge("Right_Trunk_Platform",258,109,63,false);Bridge("Left_Trunk_Platform",113,223,63,false);
        Ladder(47,-6,99);
        foreach(var p in new[]{new Vector2(400,122),new Vector2(422,125)})
            Patch("Wax_Stones","Assets/Hive.png",new Rect(48,112,32,16),p.x,p.y,decor,15);
        Patch("Honey_Mound","Assets/Hive.png",new Rect(112,96,16,16),401,109,decor,14);
        HiveDrip(376,30,12);HiveDrip(408,57,39);HiveDrip(470,63,18);
    }
    static void Ladder(float x,float y,float height)
    {
        var geo=new Geometry();var tex=Tex("Assets/Tiles.png");
        for(int i=0;i<3;i++)geo.Add(new Rect(x+i*14,y+(i==0?30:0),2,height-(i==0?30:0)),new Rect(86,96,2,10),tex);
        for(float r=0;r<height;r+=6){if(r>=30)geo.Add(new Rect(x+1,y+r,12,3),new Rect(81,94,12,3),tex);geo.Add(new Rect(x+15,y+r,12,3),new Rect(81,94,12,3),tex);}
        Save("Wooden_Ladder_Visual",decor,Mat("Tiles","Assets/Tiles.png"),14,geo);
    }
    static void HiveDrip(float x,float y,float length)
    {
        var geo=new Geometry();var t=Tex("Assets/Hive.png");
        geo.Add(new Rect(x-6,y,13,5),new Rect(114,128,13,5),t);
        geo.Add(new Rect(x-1,y+3,3,length-10),new Rect(119,144,3,10),t);
        geo.Add(new Rect(x-3,y+length-9,7,10),new Rect(117,159,7,10),t);
        Save("Hive_Drip_"+(serial++),decor,Mat("Hive","Assets/Hive.png"),16,geo);
    }
    public static string Build(int number)
    {
        if(number<1||number>4)throw new ArgumentOutOfRangeException(nameof(number));
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        level=number;serial=0;materials.Clear();textures.Clear();terrain.Clear();terrainNames.Clear();
        Folder(Output);Folder(Output+"/Forest"+level);
        if(offsets.Count==0){for(int y=-24;y<=24;y++)for(int x=-24;x<=24;x++)if(x*x+y*y>0&&x*x+y*y<=576)offsets.Add(new Vector3Int(x,y,x*x+y*y));offsets.Sort((a,b)=>a.z.CompareTo(b.z));}
        string path="Assets/Scenes/Forest "+level+".unity";var scene=SceneManager.GetSceneByPath(path);
        if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        if(scene.isDirty)throw new InvalidOperationException("Save or discard unsaved scene edits before rebuilding: "+path);
        if(scene.GetRootGameObjects().Any(g=>g.GetComponent<Grid>()!=null))
            throw new InvalidOperationException("This forest uses editable Tilemaps. Edit Grid/Platform with the existing palettes; the legacy mesh rebuild is disabled to protect painted work.");
        SceneManager.SetActiveScene(scene);
        foreach(var old in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(old);
        root=new GameObject("FOREST_"+level+"_REFERENCE_LAYOUT").transform;
        background=Group("00_Background_Sky_And_Forest");foreground=Group("20_Terrain_And_Collisions");decor=Group("30_Plants_Bridges_And_Static_Props");
        switch(level){case 1:Forest1();break;case 2:Forest2();break;case 3:Forest3();break;case 4:Forest4();break;}
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);
        camera.orthographic=true;camera.orthographicSize=H/32f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.055f);
        camera.gameObject.AddComponent<AudioListener>();var light=new GameObject("Scene Light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-30,0);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return path;
    }
    public static string Capture(int number)
    {
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Forest "+number+".unity");
        if(!scene.IsValid()||!scene.isLoaded)throw new InvalidOperationException("Open target scene first.");
        var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();
        var other=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene)
            .SelectMany(s=>s.GetRootGameObjects()).SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).ToDictionary(r=>r,r=>r.forceRenderingOff);
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;float previousAspect=camera.aspect;
        var target=new RenderTexture(1536,864,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1536,864,TextureFormat.RGB24,false);
        string path="Assets/Screenshots/LevelPreviews/Forest_"+number+"_Reference_Final.png";
        try
        {
            foreach(var r in other.Keys)r.forceRenderingOff=true;
            camera.targetTexture=target;camera.aspect=W/(float)H;camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1536,864),0,0);pixels.Apply();System.IO.File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally
        {
            foreach(var kv in other)if(kv.Key!=null)kv.Key.forceRenderingOff=kv.Value;
            camera.targetTexture=previousTarget;camera.aspect=previousAspect;RenderTexture.active=previousActive;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
        }
        AssetDatabase.ImportAsset(path);return path;
    }
}

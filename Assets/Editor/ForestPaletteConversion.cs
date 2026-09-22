using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Editor-only migration. Terrain is stored as ordinary, directly paintable Tilemaps.
public static class ForestPaletteConversion
{
    const string PaletteFolder = "Assets/Art Assets/TilesMap/Palettes/";
    static string Path(int n) => "Assets/Scenes/Forest " + n + ".unity";
    static Vector2 World(float x, float y) => new Vector2((x - 240) / 16, (135 - y) / 16);

    sealed class Palette
    {
        public readonly TileBase[] tiles;
        public readonly Dictionary<Vector2Int, Tile> atlas = new Dictionary<Vector2Int, Tile>();
        public Palette(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaletteFolder + name + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing existing palette: " + name);
            var used = new List<TileBase>();
            foreach (var map in prefab.GetComponentsInChildren<Tilemap>())
            {
                var buffer = new TileBase[map.GetUsedTilesCount()];
                map.GetUsedTilesNonAlloc(buffer); used.AddRange(buffer);
            }
            tiles = used.Distinct().ToArray();
            foreach (var t in tiles.OfType<Tile>())
                if (t.sprite != null && t.sprite.rect.width == 16 && t.sprite.rect.height == 16)
                    atlas[new Vector2Int(Mathf.RoundToInt(t.sprite.rect.x), Mathf.RoundToInt(t.sprite.texture.height - t.sprite.rect.yMax))] = t;
        }
        public Tile At(int x, int y) => atlas[new Vector2Int(x, y)];
    }

    static Tilemap MakeMap(Transform grid, Tilemap template, string name, int order)
    {
        var go = UnityEngine.Object.Instantiate(template.gameObject, grid);
        go.name = name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var map = go.GetComponent<Tilemap>();
        map.ClearAllTiles();
        map.GetComponent<TilemapRenderer>().sortingOrder = order;
        return map;
    }

    static void Flush(Tilemap map)
    {
        // Process removals before changing bounds. Compressing an edited Tilemap first
        // can leave old composite shapes cached outside its new bounds in Unity 2023.
        map.RefreshAllTiles();
        var collider = map.GetComponent<TilemapCollider2D>();
        if (collider == null) return;
        collider.ProcessTilemapChanges();
        map.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }

    static HashSet<Vector3Int> Cells(Tilemap map)
    {
        var cells = new HashSet<Vector3Int>();
        foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) cells.Add(p);
        return cells;
    }

    static void PaintSurface(Tilemap map, HashSet<Vector3Int> cells, Palette palette, string kind)
    {
        map.ClearAllTiles();
        var grass = kind == "grass" ? palette.tiles.Single(t => t.name == "Ground_1") : null;
        foreach (var p in cells)
        {
            if (grass != null) { map.SetTile(p, grass); continue; }
            bool left = !cells.Contains(p + Vector3Int.left), right = !cells.Contains(p + Vector3Int.right);
            bool top = !cells.Contains(p + Vector3Int.up), bottom = !cells.Contains(p + Vector3Int.down);
            int variant = ((p.x * 7 + p.y * 11) % 3 + 3) % 3;
            int x = 16 + variant * 16, y;
            float rotation = 0;
            if (kind == "honey" && (left && right || top && bottom))
            { x = variant * 16; y = 176; if (left && right) rotation = 90; }
            else if (top) { x = left ? 0 : right ? 64 : x; y = 16; }
            else if (bottom) { x = left ? 0 : right ? 64 : x; y = kind == "wood" ? 64 : 48; }
            else if (left || right) { x = left ? 0 : 64; y = 32; }
            else if (kind == "wood") y = 32 + ((p.y & 1) * 16);
            else { x = variant * 16; y = 144; }
            map.SetTile(p, palette.At(x, y));
            if (rotation != 0)
            {
                map.SetTileFlags(p, TileFlags.None);
                map.SetTransformMatrix(p, Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation)));
            }
        }
        Flush(map);
    }

    static float TileTop(Tile tile)
    {
        var points = new List<Vector2>();
        float top = -1;
        for (int i = 0; i < tile.sprite.GetPhysicsShapeCount(); i++)
        {
            tile.sprite.GetPhysicsShape(i, points);
            foreach (var point in points) top = Mathf.Max(top, point.y + .5f);
        }
        return top;
    }

    static Vector2 PaintBranch(Tilemap map, Palette palette, Bounds visual, float top)
    {
        int count = Mathf.Max(2, Mathf.RoundToInt(visual.size.x));
        int x = Mathf.RoundToInt(visual.min.x);
        int y = Mathf.RoundToInt(top - TileTop(palette.At(192, 160)));
        for (int i = 0; i < count; i++)
        {
            int source = Mathf.RoundToInt(i * 4f / (count - 1));
            map.SetTile(new Vector3Int(x + i, y, 0), palette.At(160 + source * 16, 160));
            map.SetTile(new Vector3Int(x + i, y - 1, 0), palette.At(160 + source * 16, 176));
        }
        return new Vector2(x + count * .5f, y + TileTop(palette.At(192, 160)) + .25f);
    }

    static Vector2 PaintDeck(Tilemap map, Tilemap trim, Palette palette, Bounds visual, float top, bool rope)
    {
        int count = Mathf.Max(2, Mathf.RoundToInt(visual.size.x));
        int x = Mathf.RoundToInt(visual.min.x);
        var tile = palette.At(96, 96);
        int y = Mathf.RoundToInt(top - TileTop(tile));
        for (int i = 0; i < count; i++) map.SetTile(new Vector3Int(x + i, y, 0), tile);
        if (rope)
            for (int row = 0; row < 3; row++)
            {
                trim.SetTile(new Vector3Int(x, y + 2 - row, 0), palette.At(80, 112 + row * 16));
                trim.SetTile(new Vector3Int(x + count - 1, y + 2 - row, 0), palette.At(144, 112 + row * 16));
            }
        return new Vector2(x + count * .5f, y + TileTop(tile) + .25f);
    }

    static Vector2 PaintMushroom(Tilemap map, Palette palette, Bounds visual, float top)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(visual.size.x), 1, 2);
        int x = Mathf.RoundToInt(visual.min.x);
        int y = Mathf.RoundToInt(top - TileTop(palette.At(128, 0)));
        for (int i = 0; i < count; i++) map.SetTile(new Vector3Int(x + i, y, 0), palette.At(128 + i * 16, 0));
        return new Vector2(x + .5f, y + TileTop(palette.At(128, 0)) + .25f);
    }

    static void AlignDecor(GameObject root, Tilemap ground)
    {
        Physics2D.SyncTransforms();
        var surface = ground.GetComponent<CompositeCollider2D>();
        var renderers = root.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (!(r.name.StartsWith("Mushroom_") || r.name.StartsWith("Rock_") || r.name.StartsWith("Lavender_") ||
                  r.name.StartsWith("Fern_") || r.name.StartsWith("Blue_Flower_") || r.name.StartsWith("Fallen_Acorns_") ||
                  r.name.StartsWith("Green_Acorn") || r.name.StartsWith("Honey_Mound"))) continue;
            var old = r.bounds;
            for (float y = old.min.y + .75f; y >= old.min.y - 1.25f; y -= 1f / 32)
                if (surface.OverlapPoint(new Vector2(old.center.x, y)))
                {
                    var delta = Vector3.up * (y + 1f / 32 - old.min.y);
                    r.transform.position += delta;
                    if (r.name.StartsWith("Rock_"))
                        foreach (var rune in renderers.Where(q => q.name.StartsWith("Blue_Rune") && old.Contains(q.bounds.center)))
                            rune.transform.position += delta;
                    break;
                }
        }
    }

    public static object Convert(int n)
    {
        if (n < 1 || n > 4) throw new ArgumentOutOfRangeException(nameof(n));
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetSceneByPath(Path(n));
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(Path(n), OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save unsaved edits first: " + Path(n));
        var roots = scene.GetRootGameObjects();
        var oldGrid = roots.SingleOrDefault(g => g.GetComponent<Grid>() != null);
        if (n != 1 && oldGrid != null) throw new InvalidOperationException("Scene already has editable Tilemaps: " + Path(n));
        var root = roots.Single(g => g.name == "FOREST_" + n + (n == 1 ? "_BACKGROUND_AND_DECOR" : "_REFERENCE_LAYOUT"));
        var forest = new Palette("Forest_1");
        var tree = new Palette("Forest_2");
        var hive = new Palette("Hive");
        var polygons = root.GetComponentsInChildren<PolygonCollider2D>();
        if (n > 1 && polygons.Length != (n == 2 ? 4 : 3)) throw new InvalidOperationException("Unknown terrain regions: " + Path(n));
        var boxes = root.GetComponentsInChildren<BoxCollider2D>();
        if (n > 1 && boxes.Length != (n == 4 ? 9 : 3)) throw new InvalidOperationException("Unknown platforms: " + Path(n));
        if (n == 1 && (oldGrid == null || oldGrid.transform.Find("Platform") == null)) throw new InvalidOperationException("Expected existing Forest 1 Tilemaps.");

        Physics2D.SyncTransforms();
        var cells = n == 1 ? Cells(oldGrid.transform.Find("Platform").GetComponent<Tilemap>()) : new HashSet<Vector3Int>();
        if (n > 1)
            for (int y = -11; y <= 10; y++)
                for (int x = -17; x <= 17; x++)
                    if (polygons.Any(p => p.OverlapPoint(new Vector2(x + .5f, y + .5f)))) cells.Add(new Vector3Int(x, y, 0));
        if (cells.Count < 40) throw new InvalidOperationException("Terrain rasterization failed.");

        var sample = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        GameObject grid;
        Tilemap ground, branches, decks, mushrooms, trim;
        try
        {
            var source = sample.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Grid>()).Single();
            var template = source.GetComponentsInChildren<Tilemap>().Single(t => t.name == "Platform");
            SceneManager.SetActiveScene(scene);
            grid = new GameObject("Grid", typeof(Grid));
            EditorUtility.CopySerialized(source, grid.GetComponent<Grid>());
            MakeMap(grid.transform, source.GetComponentsInChildren<Tilemap>().Single(t => t.name == "back_0"), "back_0", 0);
            ground = MakeMap(grid.transform, template, "Platform", 0);
            branches = n == 3 ? null : MakeMap(grid.transform, template, "BranchPlatforms", 2);
            decks = n == 3 ? null : MakeMap(grid.transform, template, "WoodenPlatforms", 3);
            mushrooms = n == 3 ? MakeMap(grid.transform, template, "MushroomPlatforms", 4) : null;
            trim = MakeMap(grid.transform, source.GetComponentsInChildren<Tilemap>().Single(t => t.name == "front_0"), "front_0", 0);
        }
        finally { EditorSceneManager.ClosePreviewScene(sample); }

        PaintSurface(ground, cells, n <= 2 ? forest : n == 3 ? tree : hive, n <= 2 ? "grass" : n == 3 ? "wood" : "honey");
        var probes = new List<Vector2>();
        if (n == 1)
        {
            probes.Add(PaintDeck(decks, trim, forest, new Bounds(World(156,110), new Vector3(66f/16, .625f, 0)), World(156,110).y, false));
            probes.Add(PaintBranch(branches, forest, new Bounds(World(290.5f,43), new Vector3(61f/16, 1.5f, 0)), World(291,44).y));
        }
        else foreach (var box in boxes)
        {
            var visual = box.GetComponent<Renderer>().bounds;
            if (box.name.StartsWith("Deck_"))
            {
                // Rope anchors were part of the old mesh; transfer them to non-colliding trim tiles.
                var deckBounds = box.bounds;
                probes.Add(PaintDeck(decks, trim, forest, deckBounds, deckBounds.max.y, n == 2));
            }
            else if (box.name.EndsWith("_Branch"))
            {
                var point = PaintBranch(branches, forest, visual, box.bounds.max.y);
                probes.Add(point);
                // Keep each static hive suspended from the newly aligned branch.
                float delta = point.y - .25f - box.bounds.max.y;
                foreach (var r in box.transform.parent.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Hive_Visual")))
                    r.transform.position += Vector3.up * delta;
            }
            else probes.Add(PaintMushroom(mushrooms, tree, visual, box.bounds.max.y));
        }
        foreach (var map in grid.GetComponentsInChildren<Tilemap>()) { Flush(map); map.CompressBounds(); }
        if (ground.GetComponent<CompositeCollider2D>().pathCount == 0) throw new InvalidOperationException("No terrain collider generated.");

        if (oldGrid != null) UnityEngine.Object.DestroyImmediate(oldGrid);
        if (n > 1)
        {
            foreach (var box in boxes) UnityEngine.Object.DestroyImmediate(box.gameObject);
            string surface = n == 2 ? "Canyon_Grass_Platforms" : n == 3 ? "Ancient_Tree_Hollow" : "Hive_Entrance_Honey_Terrain";
            var terrain = root.transform.Find("20_Terrain_And_Collisions");
            UnityEngine.Object.DestroyImmediate(terrain.Find(surface).gameObject);
            UnityEngine.Object.DestroyImmediate(terrain.Find(surface + "_Collisions").gameObject);
        }
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            renderer.sortingLayerName = renderer.sortingOrder < 0 ? "back" : "front";
        root.name = "FOREST_" + n + "_BACKGROUND_AND_DECOR";
        AlignDecor(root, ground);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Select(n);
        return new { scene = Path(n), terrainCells = cells.Count, platformGroups = probes.Count, maps = grid.GetComponentsInChildren<Tilemap>().Select(m => m.name).ToArray() };
    }

    public static void Select(int n)
    {
        var scene = SceneManager.GetSceneByPath(Path(n));
        SceneManager.SetActiveScene(scene);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Grid").transform.Find("Platform").gameObject;
        Selection.activeGameObject = map;
        GridPaintingState.scenePaintTarget = map;
        GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(PaletteFolder + (n <= 2 ? "Forest_1" : n == 3 ? "Forest_2" : "Hive") + ".prefab");
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, new Vector3(30, 16.875f, 1)), false);
        }
    }

    public static void Refine(int n)
    {
        if (n != 3 && n != 4) throw new ArgumentOutOfRangeException(nameof(n));
        var scene = SceneManager.GetSceneByPath(Path(n));
        if (scene.isDirty) throw new InvalidOperationException("Save scene before refining.");
        var grid = scene.GetRootGameObjects().Single(g => g.name == "Grid");
        var ground = grid.transform.Find("Platform").GetComponent<Tilemap>();
        var root = scene.GetRootGameObjects().Single(g => g.name == "FOREST_" + n + "_BACKGROUND_AND_DECOR");
        if (n == 3)
        {
            var map = grid.transform.Find("MushroomPlatforms").GetComponent<Tilemap>();
            var from = new[] { new Vector3Int(-2,-8,0), new Vector3Int(-1,-7,0) };
            var to = new[] { new Vector3Int(-3,-7,0), new Vector3Int(-2,-6,0) };
            for (int i = 0; i < 2; i++)
            {
                if (!map.HasTile(from[i]) || map.HasTile(to[i])) throw new InvalidOperationException("Mushroom layout already changed.");
                map.SetTile(to[i], map.GetTile(from[i])); map.SetTile(from[i], null);
            }
            // The upper cap needs one cell of headroom away from the overhanging root.
            var upperTip = map.GetTile(new Vector3Int(3,-1,0));
            var upperStem = map.GetTile(new Vector3Int(4,-1,0));
            map.SetTile(new Vector3Int(4,-1,0), null);
            map.SetTile(new Vector3Int(3,-1,0), upperStem);
            map.SetTile(new Vector3Int(2,-1,0), upperTip);
            Flush(map); map.CompressBounds();
        }
        Physics2D.SyncTransforms();
        var collider = ground.GetComponent<CompositeCollider2D>();
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            bool drip = r.name.StartsWith("Hive_Drip_");
            bool ceilingThorn = n == 3 && r.name.StartsWith("Static_Thorns_") && r.bounds.size.x > r.bounds.size.y;
            if (!drip && !ceilingThorn) continue;
            var anchor = new Vector2(r.bounds.center.x, r.bounds.max.y);
            for (float y = anchor.y - .5f; y < anchor.y + 1.25f; y += 1f / 32)
                if (collider.OverlapPoint(new Vector2(anchor.x, y)))
                {
                    r.transform.position += Vector3.up * (y - anchor.y + 1f / 32);
                    break;
                }
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    static IEnumerable<Vector2> TerrainProbes(int n)
    {
        switch (n)
        {
            case 1: return new[] { World(30,65), World(185,190), World(404,210), World(425,-12) };
            case 2: return new[] { World(35,181), World(225,99), World(456,-12), World(465,117) };
            case 3: return new[] { World(80,208), World(168,234), World(268,180), World(449,195) };
            default: return new[] { World(405,101), World(463,110) };
        }
    }

    static IEnumerable<Vector2> PlatformProbes(Tilemap map)
    {
        var remaining = Cells(map);
        while (remaining.Count > 0)
        {
            var cluster = new List<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            var first = remaining.First(); remaining.Remove(first); queue.Enqueue(first);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); cluster.Add(p);
                foreach (var offset in new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down })
                    if (remaining.Remove(p + offset)) queue.Enqueue(p + offset);
            }
            int top = cluster.Max(p => p.y);
            var row = cluster.Where(p => p.y == top).OrderBy(p => p.x).ToArray();
            var cell = map.name == "MushroomPlatforms" ? row[0] : row[row.Length / 2];
            var tile = (Tile)map.GetTile(cell);
            yield return new Vector2(cell.x + .5f, cell.y + TileTop(tile) + .25f);
        }
    }

    static void SimulatePlayer(PhysicsScene2D physics, Rigidbody2D body, CapsuleCollider2D capsule, PlayerController player)
    {
        // Match PlayerController.Move() with no horizontal input, and CheckState().
        body.velocity = new Vector2(0, body.velocity.y);
        body.sharedMaterial = capsule.IsTouchingLayers(player.groundCheck.groundLayer) ? player.normalMaterial : player.wallMaterial;
        physics.Simulate(1f / 60);
    }

    public static object Test(int n)
    {
        var source = SceneManager.GetSceneByPath(Path(n));
        var sample = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        var test = EditorSceneManager.NewPreviewScene();
        var previousMode = Physics2D.simulationMode;
        try
        {
            var player = sample.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>()).Single();
            var grid = UnityEngine.Object.Instantiate(source.GetRootGameObjects().Single(g => g.name == "Grid"));
            SceneManager.MoveGameObjectToScene(grid, test);
            var maps = grid.GetComponentsInChildren<Tilemap>().Where(m => m.GetComponent<TilemapCollider2D>() != null).ToArray();
            foreach (var map in maps) Flush(map);
            var actor = new GameObject("Player_Collision_Probe");
            SceneManager.MoveGameObjectToScene(actor, test);
            actor.layer = player.gameObject.layer;
            actor.transform.localScale = player.transform.lossyScale;
            var body = actor.AddComponent<Rigidbody2D>(); EditorUtility.CopySerialized(player.GetComponent<Rigidbody2D>(), body);
            var capsule = actor.AddComponent<CapsuleCollider2D>(); EditorUtility.CopySerialized(player.GetComponent<CapsuleCollider2D>(), capsule);
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            var physics = test.GetPhysicsScene2D();
            Physics2D.simulationMode = SimulationMode2D.Script;
            var results = new List<object>();
            var origins = TerrainProbes(n).Select(p => (point: p, expected: "Platform"))
                .Concat(maps.Where(m => m.name != "Platform").SelectMany(m => PlatformProbes(m).Select(p => (point: p, expected: m.name))));
            bool passed = true;
            foreach (var probe in origins)
            {
                var origin = probe.point;
                body.simulated = false; Physics2D.SyncTransforms();
                var hit = physics.Raycast(origin, Vector2.down, 4, player.groundCheck.groundLayer);
                if (hit.collider == null) { results.Add(new { origin = origin.ToString(), failure = "No platform below probe" }); passed = false; continue; }
                float foot = (capsule.size.y * .5f - capsule.offset.y) * actor.transform.localScale.y;
                var spawn = hit.point + Vector2.up * (foot + .15f);
                actor.transform.position = spawn;
                body.position = spawn; body.velocity = Vector2.zero; body.simulated = true;
                Physics2D.SyncTransforms();
                for (int i = 0; i < 180; i++) SimulatePlayer(physics, body, capsule, player);
                bool landed = capsule.IsTouching(hit.collider) && Mathf.Abs(body.position.y - foot - hit.point.y) < .55f;
                float restingY = body.position.y, peak = restingY;
                body.velocity = Vector2.up * player.JumpForce;
                for (int i = 0; i < 360; i++) { SimulatePlayer(physics, body, capsule, player); peak = Mathf.Max(peak, body.position.y); }
                bool rose = peak > restingY + .1f;
                bool landedAgain = capsule.IsTouchingLayers(player.groundCheck.groundLayer);
                bool correctPlatform = hit.collider.name == probe.expected;
                passed &= landed && rose && landedAgain && correctPlatform;
                results.Add(new { origin = origin.ToString(), platform = hit.collider.name, correctPlatform, landed, rose, landedAgain, restingY, endY = body.position.y, actualX = body.position.x });
            }
            body.simulated = false;
            var paintTests = new List<object>();
            int paintIndex = 0;
            foreach (var map in maps)
            {
                var tile = map.GetTile(Cells(map).First());
                var cell = new Vector3Int(30 + paintIndex++ * 4, 0, 0);
                var rayStart = new Vector2(cell.x + .5f, 2);
                map.SetTile(cell, tile); Flush(map); Physics2D.SyncTransforms(); physics.Simulate(1f / 60);
                var paintHit = physics.Raycast(rayStart, Vector2.down, 4, player.groundCheck.groundLayer);
                bool paint = paintHit.collider == map.GetComponent<CompositeCollider2D>();
                map.SetTile(cell, null); Flush(map); Physics2D.SyncTransforms(); physics.Simulate(1f / 60);
                var eraseHit = physics.Raycast(rayStart, Vector2.down, 4, player.groundCheck.groundLayer);
                bool erase = eraseHit.collider == null;
                passed &= paint && erase;
                paintTests.Add(new { map = map.name, paintAddsCollision = paint, eraseRemovesCollision = erase, remainingCollider = erase ? "none" : eraseHit.collider.name });
            }
            return new { scene = Path(n), passed, groundMask = player.groundCheck.groundLayer.value, results, paintTests };
        }
        finally
        {
            Physics2D.simulationMode = previousMode;
            EditorSceneManager.ClosePreviewScene(test); EditorSceneManager.ClosePreviewScene(sample);
            SceneManager.SetActiveScene(source);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// One-time editor migration. The saved level has no runtime builder dependency.
public static class HiveTilemapConversion
{
    const string ScenePath = "Assets/Scenes/Hive.unity";
    const string PalettePath = "Assets/Art Assets/TilesMap/Palettes/Hive.prefab";

    static void Flush(Tilemap map)
    {
        map.RefreshAllTiles();
        map.CompressBounds();
        map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        map.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }

    static Dictionary<int, Tile> PaletteTiles()
    {
        var palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        if (palette == null) throw new InvalidOperationException("The existing Hive palette is missing.");
        var map = palette.GetComponentInChildren<Tilemap>();
        var used = new TileBase[map.GetUsedTilesCount()];
        map.GetUsedTilesNonAlloc(used);
        return used.OfType<Tile>().ToDictionary(t => int.Parse(t.name.Substring("Hive_".Length)));
    }

    static int Variant(Vector3Int p) => ((p.x * 7 + p.y * 11) % 3 + 3) % 3;

    static int SurfaceTile(HashSet<Vector3Int> cells, Vector3Int p, out float rotation)
    {
        bool left = !cells.Contains(p + Vector3Int.left), right = !cells.Contains(p + Vector3Int.right);
        bool top = !cells.Contains(p + Vector3Int.up), bottom = !cells.Contains(p + Vector3Int.down);
        rotation = 0;
        // The full-width wax shelf also supplies a solid narrow column when rotated.
        if (left && right) { rotation = 90; return 86 + Variant(p); }
        if (top && bottom) return 86 + Variant(p);
        if (top) return left ? 8 : right ? 12 : 9 + Variant(p);
        if (bottom) return left ? 24 : right ? 28 : 25 + Variant(p);
        if (left) return 16;
        if (right) return 20;
        return 71 + Variant(p);
    }

    static void PreserveThinConnections(HashSet<Vector3Int> cells)
    {
        // These reference columns are narrower than one cell and can miss its centre.
        for (int y = 0; y <= 7; y++) cells.Add(new Vector3Int(-1, y, 0));
        for (int y = -7; y <= -4; y++) cells.Add(new Vector3Int(-10, y, 0));
        cells.Add(new Vector3Int(2, 8, 0));
        cells.Remove(new Vector3Int(-14, 1, 0));
    }

    static void Paint(Tilemap map, HashSet<Vector3Int> cells, Dictionary<int, Tile> tiles)
    {
        map.ClearAllTiles();
        foreach (var cell in cells)
        {
            int id = SurfaceTile(cells, cell, out float rotation);
            map.SetTile(cell, tiles[id]);
            if (rotation != 0)
            {
                map.SetTileFlags(cell, TileFlags.None);
                map.SetTransformMatrix(cell, Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation)));
            }
        }
        Flush(map);
    }

    static void AlignStaticDecor(GameObject root, Tilemap ground)
    {
        var surface = ground.GetComponent<CompositeCollider2D>();
        Physics2D.SyncTransforms();
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!(sr.name.StartsWith("Chest_") || sr.name == "Honey_Mound" || sr.name == "Honey_Rock" || sr.name.StartsWith("Floor_Pit_Spikes_"))) continue;
            float oldFoot = sr.bounds.min.y;
            for (float y = oldFoot + .75f; y >= oldFoot - .75f; y -= 1f / 32)
                if (surface.OverlapPoint(new Vector2(sr.bounds.center.x, y)))
                {
                    sr.transform.position += Vector3.up * (y + 1f / 32 - oldFoot);
                    break;
                }
        }
        foreach (var tr in root.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Hanging_Honey_")))
        {
            var parts = tr.name.Split('_');
            Vector2 anchor = World(float.Parse(parts[2]), float.Parse(parts[3]));
            for (float y = anchor.y - .75f; y <= anchor.y + 1.25f; y += 1f / 32)
                if (surface.OverlapPoint(new Vector2(anchor.x, y)))
                {
                    tr.position = new Vector3(0, y - anchor.y + 1f / 16, 0);
                    break;
                }
        }
    }

    // Restricted to the exact initial conversion, before any user-painted changes.
    public static void RefineInitialLayout()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (scene.isDirty) throw new InvalidOperationException("Save Hive before refining.");
        var map = scene.GetRootGameObjects().Single(g => g.name == "Grid").transform.Find("Platform").GetComponent<Tilemap>();
        var cells = new HashSet<Vector3Int>();
        foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) cells.Add(p);
        if (cells.Count != 333) throw new InvalidOperationException("The initial layout has already changed; refinement cancelled.");
        PreserveThinConnections(cells);
        Paint(map, cells, PaletteTiles());
        AlignStaticDecor(scene.GetRootGameObjects().Single(g => g.name == "HIVE_BACKGROUND_AND_DECOR"), map);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Level Layout/Convert Hive To Editable Tilemap")]
    public static string Convert()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save your Hive changes before conversion.");
        if (scene.GetRootGameObjects().Any(g => g.GetComponent<Grid>() != null))
            throw new InvalidOperationException("Hive already has a Grid. Edit Grid/Platform directly; this command does not overwrite painted work.");
        var root = scene.GetRootGameObjects().Single(g => g.name == "HIVE_REFERENCE_LAYOUT");
        var terrain = root.transform.Find("20_Connected_Honey_Terrain");
        var collisionRoot = root.transform.Find("21_Terrain_Collisions");
        if (terrain == null || collisionRoot == null) throw new InvalidOperationException("Unknown Hive layout; conversion cancelled.");
        var polygons = collisionRoot.GetComponentsInChildren<PolygonCollider2D>();
        if (polygons.Length != 10) throw new InvalidOperationException("Expected the original ten Hive terrain regions.");
        var tiles = PaletteTiles();
        foreach (int id in new[] { 8,9,10,11,12,16,20,24,25,26,27,28,71,72,73,86,87,88 })
            if (!tiles.ContainsKey(id)) throw new InvalidOperationException("Missing existing palette tile Hive_" + id);

        var cells = new HashSet<Vector3Int>();
        Physics2D.SyncTransforms();
        for (int y = -10; y <= 9; y++)
            for (int x = -16; x <= 15; x++)
                if (polygons.Any(p => p.OverlapPoint(new Vector2(x + .5f, y + .5f))))
                    cells.Add(new Vector3Int(x, y, 0));
        if (cells.Count < 150) throw new InvalidOperationException("Unexpected terrain coverage; original layout has not been changed.");
        PreserveThinConnections(cells);

        var sample = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        Tilemap ground;
        try
        {
            var sourceGrid = sample.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Grid>()).Single();
            SceneManager.SetActiveScene(scene);
            var grid = new GameObject("Grid", typeof(Grid));
            EditorUtility.CopySerialized(sourceGrid, grid.GetComponent<Grid>());
            ground = null;
            foreach (string name in new[] { "back_0", "Platform", "front_0" })
            {
                var template = sourceGrid.GetComponentsInChildren<Tilemap>().Single(t => t.name == name);
                var go = UnityEngine.Object.Instantiate(template.gameObject, grid.transform);
                go.name = name;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                var map = go.GetComponent<Tilemap>();
                map.ClearAllTiles();
                if (name == "Platform") ground = map;
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(sample); }

        Paint(ground, cells, tiles);
        if (ground.GetComponent<CompositeCollider2D>().pathCount == 0)
            throw new InvalidOperationException("Tilemap did not generate collision geometry; original surfaces have not been removed.");

        // Remove only the physical mesh and its proxies. Keep windows, forest and static props.
        UnityEngine.Object.DestroyImmediate(terrain.gameObject);
        UnityEngine.Object.DestroyImmediate(collisionRoot.gameObject);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            renderer.sortingLayerName = renderer.sortingOrder < 0 ? "back" : "front";
        root.name = "HIVE_BACKGROUND_AND_DECOR";
        AlignStaticDecor(root, ground);
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SelectForPainting();
        return "Hive: " + cells.Count + " palette tiles; Grid/Platform with SampleScene collision components.";
    }

    public static void SelectForPainting()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Grid").transform.Find("Platform").gameObject;
        Selection.activeGameObject = map;
        GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        GridPaintingState.scenePaintTarget = map;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, new Vector3(29.375f, 16.875f, 1)), false);
        }
    }

    static Vector2 World(float x, float y) => new Vector2((x - 235) / 16, (135 - y) / 16);

    public static object TestPlayerCollision()
    {
        var source = SceneManager.GetSceneByPath(ScenePath);
        var sample = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        var test = EditorSceneManager.NewPreviewScene();
        var previousMode = Physics2D.simulationMode;
        try
        {
            var player = sample.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>()).Single();
            var grid = UnityEngine.Object.Instantiate(source.GetRootGameObjects().Single(g => g.name == "Grid"));
            SceneManager.MoveGameObjectToScene(grid, test);
            var map = grid.transform.Find("Platform").GetComponent<Tilemap>();
            Flush(map);
            var actor = new GameObject("Player_Collision_Probe");
            SceneManager.MoveGameObjectToScene(actor, test);
            actor.layer = player.gameObject.layer;
            actor.transform.localScale = player.transform.lossyScale;
            var body = actor.AddComponent<Rigidbody2D>();
            EditorUtility.CopySerialized(player.GetComponent<Rigidbody2D>(), body);
            var capsule = actor.AddComponent<CapsuleCollider2D>();
            EditorUtility.CopySerialized(player.GetComponent<CapsuleCollider2D>(), capsule);
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            var physics = test.GetPhysicsScene2D();
            Physics2D.simulationMode = SimulationMode2D.Script;
            var results = new List<object>();
            var origins = new[] { World(40,135), World(190,95), World(261,132), World(422,120), World(38,225), World(255,229) };
            foreach (var origin in origins)
            {
                body.simulated = false;
                Physics2D.SyncTransforms();
                var hit = physics.Raycast(origin, Vector2.down, 5, player.groundCheck.groundLayer);
                if (hit.collider == null) throw new InvalidOperationException("No ground below " + origin);
                float footOffset = (capsule.size.y * .5f - capsule.offset.y) * actor.transform.localScale.y;
                body.position = hit.point + Vector2.up * (footOffset + .15f);
                body.velocity = Vector2.zero;
                body.simulated = true;
                for (int i = 0; i < 180; i++) physics.Simulate(1f / 60);
                bool landed = capsule.IsTouchingLayers(player.groundCheck.groundLayer);
                float restingY = body.position.y;
                body.velocity = new Vector2(0, player.JumpForce);
                float peak = restingY;
                for (int i = 0; i < 300; i++) { physics.Simulate(1f / 60); peak = Mathf.Max(peak, body.position.y); }
                bool rose = peak > restingY + .1f;
                bool landedAgain = capsule.IsTouchingLayers(player.groundCheck.groundLayer);
                results.Add(new { origin = origin.ToString(), landed, rose, landedAgain });
            }

            // A real palette paint/erase must add/remove collision without a separate proxy.
            body.simulated = false;
            var probeCell = new Vector3Int(25, 0, 0);
            var probePoint = new Vector2(25.5f, 2);
            map.SetTile(probeCell, PaletteTiles()[9]);
            Flush(map); Physics2D.SyncTransforms(); physics.Simulate(1f / 60);
            bool painted = physics.Raycast(probePoint, Vector2.down, 3, player.groundCheck.groundLayer).collider != null;
            map.SetTile(probeCell, null);
            Flush(map); Physics2D.SyncTransforms(); physics.Simulate(1f / 60);
            bool erased = physics.Raycast(probePoint, Vector2.down, 3, player.groundCheck.groundLayer).collider == null;
            return new { playerLayer = actor.layer, groundMask = player.groundCheck.groundLayer.value, results, paintAddsCollision = painted, eraseRemovesCollision = erased };
        }
        finally
        {
            Physics2D.simulationMode = previousMode;
            EditorSceneManager.ClosePreviewScene(test);
            EditorSceneManager.ClosePreviewScene(sample);
            SceneManager.SetActiveScene(source);
        }
    }
}

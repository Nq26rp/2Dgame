using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// One-time migration. Afterwards the scene is authored directly with Tile Palette.
public static class Forest1TilemapConversion
{
    const string ScenePath = "Assets/Scenes/Forest 1.unity";
    const string PalettePath = "Assets/Art Assets/TilesMap/Palettes/Forest1_Platforms.prefab";
    const string TilesFolder = "Assets/Art Assets/TilesMap/Palettes/Forest_1/";

    static TileBase Tile(int index) => AssetDatabase.LoadAssetAtPath<TileBase>(TilesFolder + "Tiles_" + index + ".asset");

    static Tilemap PhysicalMap(Transform grid, string name, Tilemap template, int order)
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
        map.RefreshAllTiles();
        map.CompressBounds();
        map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        map.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }

    [MenuItem("Tools/Level Layout/Convert Forest 1 To Editable Tilemaps")]
    public static string Convert()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save your Forest 1 changes before conversion.");
        if (scene.GetRootGameObjects().Any(g => g.GetComponent<Grid>() != null))
            throw new InvalidOperationException("Forest 1 already has a Grid. Edit its Tilemaps directly; conversion is not a rebuild command.");
        var root = scene.GetRootGameObjects().Single(g => g.name == "FOREST_1_REFERENCE_LAYOUT");
        var polygons = root.GetComponentsInChildren<PolygonCollider2D>();
        if (polygons.Length != 4) throw new InvalidOperationException("Expected the four original terrain regions; refusing to replace an unknown layout.");
        var rule = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art Assets/TilesMap/Palettes/RuleTiles/Ground_1.asset");
        if (rule == null || Tile(129) == null || Tile(206) == null) throw new InvalidOperationException("Original palette tiles are missing.");

        var sample = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        GameObject gridGo;
        Tilemap ground, bridge, branches;
        try
        {
            var sourceGrid = sample.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Grid>()).Single();
            var template = sourceGrid.GetComponentsInChildren<Tilemap>().Single(t => t.name == "Platform");
            SceneManager.SetActiveScene(scene);
            gridGo = new GameObject("Grid", typeof(Grid));
            EditorUtility.CopySerialized(sourceGrid, gridGo.GetComponent<Grid>());
            ground = PhysicalMap(gridGo.transform, "Platform", template, 0);
            bridge = PhysicalMap(gridGo.transform, "WoodenPlatforms", template, 1);
            branches = PhysicalMap(gridGo.transform, "BranchPlatforms", template, 2);
            foreach (string name in new[] { "back_0", "front_0" })
            {
                var source = sourceGrid.GetComponentsInChildren<Tilemap>().Single(t => t.name == name);
                var copy = UnityEngine.Object.Instantiate(source.gameObject, gridGo.transform);
                copy.name = name;
                copy.GetComponent<Tilemap>().ClearAllTiles();
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(sample); }

        // One 16-pixel tile per 1-unit cell, matching SampleScene; no hidden proxy collisions.
        Physics2D.SyncTransforms();
        for (int y = -11; y <= 10; y++)
            for (int x = -17; x <= 17; x++)
                if (polygons.Any(p => p.OverlapPoint(new Vector2(x + .5f, y + .5f))))
                    ground.SetTile(new Vector3Int(x, y, 0), rule);

        // Bridge uses the original plank sprites and their sprite physics outlines.
        for (int x = -8; x <= -4; x++) bridge.SetTile(new Vector3Int(x, 1, 0), Tile(129));
        for (int i = 0; i < 5; i++) branches.SetTile(new Vector3Int(1 + i, 5, 0), Tile(206 + i));
        Flush(ground); Flush(bridge); Flush(branches);

        // Remove only the old physical surfaces that have just been replaced.
        var terrain = root.transform.Find("20_Terrain_And_Collisions");
        UnityEngine.Object.DestroyImmediate(terrain.Find("Grass_Valley").gameObject);
        UnityEngine.Object.DestroyImmediate(terrain.Find("Grass_Valley_Collisions").gameObject);
        foreach (string name in new[] { "Deck_Upper_Left_Bridge", "Suspended_Hive_Branch" })
        {
            var go = root.GetComponentsInChildren<Transform>().Single(t => t.name == name).gameObject;
            UnityEngine.Object.DestroyImmediate(go);
        }
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            renderer.sortingLayerName = renderer.sortingOrder < 0 ? "back" : "front";
        root.name = "FOREST_1_BACKGROUND_AND_DECOR";
        Physics2D.SyncTransforms();
        CreatePalette(rule);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = ground.gameObject;
        GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        GridPaintingState.scenePaintTarget = ground.gameObject;
        return ScenePath;
    }

    static void CreatePalette(TileBase rule)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) != null) { EnsurePaletteSettings(); return; }
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var grid = new GameObject("Forest1_Platforms", typeof(Grid));
            SceneManager.MoveGameObjectToScene(grid, preview);
            var layer = new GameObject("Platform Tiles", typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(grid.transform, false);
            var map = layer.GetComponent<Tilemap>();
            for (int y = 0; y < 3; y++) for (int x = 0; x < 4; x++) map.SetTile(new Vector3Int(x, y, 0), rule);
            for (int x = 0; x < 4; x++) map.SetTile(new Vector3Int(x, -2, 0), Tile(129));
            for (int x = 0; x < 5; x++) map.SetTile(new Vector3Int(x, -4, 0), Tile(206 + x));
            PrefabUtility.SaveAsPrefabAsset(grid, PalettePath);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        EnsurePaletteSettings();
    }

    public static void EnsurePaletteSettings()
    {
        if (!AssetDatabase.LoadAllAssetsAtPath(PalettePath).Any(a => a.GetType().Name == "GridPalette"))
        {
            var original = AssetDatabase.LoadAllAssetsAtPath("Assets/Art Assets/TilesMap/Palettes/Forest_1.prefab")
                .Single(a => a.GetType().Name == "GridPalette");
            var settings = ScriptableObject.CreateInstance(original.GetType());
            EditorUtility.CopySerialized(original, settings);
            settings.name = "Palette Settings";
            settings.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(settings, PalettePath);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.ImportAsset(PalettePath, ImportAssetOptions.ForceUpdate);
    }

    public static object TestPlayerCollision()
    {
        var source = SceneManager.GetSceneByPath(ScenePath);
        var sample = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        var test = EditorSceneManager.NewPreviewScene();
        var previousMode = Physics2D.simulationMode;
        try
        {
            var originalPlayer = sample.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>()).Single();
            var sourceCapsule = originalPlayer.GetComponent<CapsuleCollider2D>();
            var sourceBody = originalPlayer.GetComponent<Rigidbody2D>();
            var grid = UnityEngine.Object.Instantiate(source.GetRootGameObjects().Single(g => g.name == "Grid"));
            SceneManager.MoveGameObjectToScene(grid, test);
            foreach (var map in grid.GetComponentsInChildren<Tilemap>().Where(t => t.GetComponent<TilemapCollider2D>() != null)) Flush(map);
            var actor = new GameObject("Player_Collision_Probe");
            SceneManager.MoveGameObjectToScene(actor, test);
            actor.layer = originalPlayer.gameObject.layer;
            actor.transform.localScale = originalPlayer.transform.lossyScale;
            var body = actor.AddComponent<Rigidbody2D>();
            EditorUtility.CopySerialized(sourceBody, body);
            var capsule = actor.AddComponent<CapsuleCollider2D>();
            EditorUtility.CopySerialized(sourceCapsule, capsule);
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            var physics = test.GetPhysicsScene2D();
            Physics2D.simulationMode = SimulationMode2D.Script;
            var results = new List<object>();
            foreach (var origin in new[] { new Vector2(-12,6), new Vector2(-4,0), new Vector2(12,0), new Vector2(-5,4), new Vector2(3,8) })
            {
                body.simulated = false;
                Physics2D.SyncTransforms();
                var hit = physics.Raycast(origin, Vector2.down, 30, originalPlayer.groundCheck.groundLayer);
                if (hit.collider == null) throw new InvalidOperationException("No platform at " + origin);
                body.position = hit.point + Vector2.up * 2;
                body.velocity = Vector2.zero;
                body.simulated = true;
                for (int i = 0; i < 240; i++) physics.Simulate(1f/60);
                bool landed = capsule.IsTouchingLayers(originalPlayer.groundCheck.groundLayer);
                float restingY = body.position.y;
                body.velocity = new Vector2(0, originalPlayer.JumpForce);
                for (int i = 0; i < 10; i++) physics.Simulate(1f/60);
                bool rose = body.position.y > restingY + .1f;
                for (int i = 0; i < 300; i++) physics.Simulate(1f/60);
                bool landedAgain = capsule.IsTouchingLayers(originalPlayer.groundCheck.groundLayer);
                results.Add(new { platform = hit.collider.name, landed, rose, landedAgain });
                if (!landed || !rose || !landedAgain) throw new InvalidOperationException("Player capsule failed fall/jump/land on " + hit.collider.name);
            }
            return new { playerLayer = actor.layer, groundMask = originalPlayer.groundCheck.groundLayer.value, capsuleSize = capsule.size.ToString(), results };
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

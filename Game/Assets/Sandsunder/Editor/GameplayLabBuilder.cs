using System.IO;
using System.Linq;
using Sandsunder.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sandsunder.Editor
{

public static class GameplayLabBuilder
{
    public const string ScenePath = "Assets/Scenes/GameplayLab.unity";
    public const string ProfilePath = "Assets/Sandsunder/Gameplay/Settings/Milestone1MovementProfile.asset";
    public const string GreyboxTexturePath = "Assets/Sandsunder/Gameplay/Settings/GreyboxSquareTexture.asset";
    public const string GreyboxSpritePath = "Assets/Sandsunder/Gameplay/Settings/GreyboxSquareSprite.asset";

    [MenuItem("Sandsunder/Gameplay/Build Gameplay Lab")]
    public static void BuildFromMenu()
    {
        BuildScene();
        Debug.Log($"Gameplay Lab rebuilt at {ScenePath}");
    }

    public static void BuildFromCommandLine()
    {
        BuildScene();
        Debug.Log($"Gameplay Lab rebuilt at {ScenePath}");
    }

    private static void BuildScene()
    {
        EnsureAssetFolder("Assets/Scenes");
        EnsureAssetFolder("Assets/Sandsunder/Gameplay/Settings");

        TopDownMovementProfile profile = LoadOrCreateProfile();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Sprite squareSprite = LoadOrCreateGreyboxSprite();
        SandboxArtSet art = SandboxArtAssetFactory.LoadOrCreate();
        CreateArena(squareSprite, art);
        GameObject player = CreatePlayer(squareSprite, art);
        PrototypePlayerCombat playerCombat = player.GetComponent<PrototypePlayerCombat>();
        CreateCombatActors(squareSprite, art, playerCombat);
        CreateDigNodes(squareSprite, art);
        CreateWorldDressing(art);
        CreateInteractiveObjects(art);
        TopDownPlayerController playerController = player.GetComponent<TopDownPlayerController>();
        Camera gameplayCamera = CreateCamera(player.transform, playerController, profile);

        GameObject hudManagers = new("Gameplay HUD Managers");
        hudManagers.AddComponent<SandboxMinimap>();
        hudManagers.AddComponent<SandboxInventoryWindow>();
        hudManagers.AddComponent<SandboxReloadBar>();
        playerController.Configure(profile, gameplayCamera);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new IOException($"Unable to save Gameplay Lab scene at {ScenePath}.");
        }

        EnsureSceneInBuildSettings();
        AssetDatabase.SaveAssets();
    }

    private static TopDownMovementProfile LoadOrCreateProfile()
    {
        TopDownMovementProfile profile = AssetDatabase.LoadAssetAtPath<TopDownMovementProfile>(ProfilePath);
        if (profile != null)
        {
            return profile;
        }

        profile = ScriptableObject.CreateInstance<TopDownMovementProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);
        return profile;
    }

    private static Sprite LoadOrCreateGreyboxSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GreyboxSpritePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GreyboxTexturePath);
        if (texture == null)
        {
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Sandsunder Greybox Square Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            AssetDatabase.CreateAsset(texture, GreyboxTexturePath);
        }

        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 1f,
            extrude: 0,
            meshType: SpriteMeshType.FullRect);
        sprite.name = "Sandsunder Greybox Square Sprite";
        AssetDatabase.CreateAsset(sprite, GreyboxSpritePath);
        return sprite;
    }

    private static void CreateArena(Sprite squareSprite, SandboxArtSet art)
    {
        GameObject arena = new("Arena");

        // Vast Desert Floor 48x32m
        GameObject floor = CreateTiledSprite(
            "Floor",
            art.SandTile,
            new Vector2(48f, 32f),
            new Color(0.93f, 0.82f, 0.67f),
            arena.transform,
            sortingOrder: -1000);

        // Central Temple Ruins Floor
        GameObject inset = CreateTiledSprite(
            "Central Ruin Sanctuary Floor",
            art.RuinTile,
            new Vector2(16f, 12f),
            new Color(0.70f, 0.53f, 0.39f),
            arena.transform,
            sortingOrder: -930);
        inset.transform.position = new Vector3(0f, 0f, 0f);

        // Ancient Cyan Rune Energy Core
        GameObject rune = CreateSpriteObject(
            "Buried Cyan Rune Core",
            art.CyanRune,
            new Color(0.20f, 0.95f, 0.90f, 0.85f),
            arena.transform,
            sortingOrder: -900);
        rune.transform.position = new Vector3(0f, 0f, 0f);
        rune.transform.localScale = Vector3.one * 4.2f;

        // Expanded Outer Arena Boundary Ramparts (48x32m)
        CreateRuinWall("North Rampart", new Vector2(0f, 15.6f), new Vector2(48f, 1.2f), squareSprite, art.RuinTile, arena.transform);
        CreateRuinWall("South Rampart", new Vector2(0f, -15.6f), new Vector2(48f, 1.2f), squareSprite, art.RuinTile, arena.transform);
        CreateRuinWall("East Rampart", new Vector2(23.6f, 0f), new Vector2(1.2f, 32f), squareSprite, art.RuinTile, arena.transform);
        CreateRuinWall("West Rampart", new Vector2(-23.6f, 0f), new Vector2(1.2f, 32f), squareSprite, art.RuinTile, arena.transform);

        // Internal Ruin Sanctuary Walls & Columns
        GameObject cover = new("Ruin Structures");
        cover.transform.SetParent(arena.transform, false);

        // Sanctuary Columns & Archways
        CreateRuinWall("North-West Courtyard Wall", new Vector2(-12f, 8f), new Vector2(8f, 1.1f), squareSprite, art.RuinTile, cover.transform);
        CreateRuinWall("North-East Courtyard Wall", new Vector2(12f, 8f), new Vector2(8f, 1.1f), squareSprite, art.RuinTile, cover.transform);
        CreateRuinWall("South-West Courtyard Wall", new Vector2(-12f, -8f), new Vector2(8f, 1.1f), squareSprite, art.RuinTile, cover.transform);
        CreateRuinWall("South-East Courtyard Wall", new Vector2(12f, -8f), new Vector2(8f, 1.1f), squareSprite, art.RuinTile, cover.transform);

        CreateRuinWall("Temple Pillar NW", new Vector2(-7.5f, 5.5f), new Vector2(1.8f, 2.2f), squareSprite, art.RuinTile, cover.transform);
        CreateRuinWall("Temple Pillar NE", new Vector2(7.5f, 5.5f), new Vector2(1.8f, 2.2f), squareSprite, art.RuinTile, cover.transform);
        CreateRuinWall("Temple Pillar SW", new Vector2(-7.5f, -5.5f), new Vector2(1.8f, 2.2f), squareSprite, art.RuinTile, cover.transform);
        CreateRuinWall("Temple Pillar SE", new Vector2(7.5f, -5.5f), new Vector2(1.8f, 2.2f), squareSprite, art.RuinTile, cover.transform);

        _ = floor;
    }

    private static GameObject CreatePlayer(Sprite squareSprite, SandboxArtSet art)
    {
        GameObject player = CreateGreyboxSprite("Player", squareSprite, new Color(0.82f, 0.68f, 0.34f), null);
        player.transform.position = Vector3.zero;
        SetWorldSize(player, new Vector2(0.72f, 0.72f));
        player.GetComponent<SpriteRenderer>().color = Color.clear;

        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CircleCollider2D>();
        player.AddComponent<TopDownPlayerController>();
        PrototypeHealth health = player.AddComponent<PrototypeHealth>();
        health.Configure(configuredEntityId: 1, configuredTeam: 0, configuredMaximumHealth: 100, shouldRespawn: false);
        PrototypePlayerCombat combat = player.AddComponent<PrototypePlayerCombat>();
        combat.Configure(entityId: 1);

        SandboxActorVisual actorVisual = player.AddComponent<SandboxActorVisual>();
        actorVisual.Configure(
            art.Nomad,
            art.Shadow,
            art.Pistol,
            player.GetComponent<TopDownPlayerController>(),
            combat,
            isHostile: false);

        // Feature 2 — the player's subterranean stealth (translucent cyan silhouette + overflight
        // immunity) reads the depth from DigDepthSystem.
        if (player.GetComponent<SubterraneanStealth>() == null)
        {
            player.AddComponent<SubterraneanStealth>();
        }
        return player;
    }

    private static void CreateCombatActors(Sprite squareSprite, SandboxArtSet art, PrototypePlayerCombat target)
    {
        Vector2[] positions =
        {
            new(-5.9f, 3.75f),
            new(5.9f, 3.55f),
            new(0f, -4.45f),
        };

        GameObject enemies = new("Dune Spitters");
        for (int index = 0; index < positions.Length; index++)
        {
            GameObject spitter = CreateGreyboxSprite(
                $"Dune Spitter {index + 1}",
                squareSprite,
                new Color(0.94f, 0.36f, 0.25f),
                enemies.transform);
            spitter.transform.position = positions[index];
            SetWorldSize(spitter, new Vector2(0.85f, 0.72f));
            spitter.GetComponent<SpriteRenderer>().color = Color.clear;
            Rigidbody2D body = spitter.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            CircleCollider2D collider = spitter.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            PrototypeHealth health = spitter.AddComponent<PrototypeHealth>();
            health.Configure(100 + index, configuredTeam: 1, configuredMaximumHealth: 55, shouldRespawn: true);
            PrototypeDuneSpitter behaviour = spitter.AddComponent<PrototypeDuneSpitter>();
            behaviour.Configure(target, 100 + index);

            SandboxActorVisual actorVisual = spitter.AddComponent<SandboxActorVisual>();
            actorVisual.Configure(art.Spitter, art.Shadow, null, null, null, isHostile: true);
        }
    }

    private static void CreateDigNodes(Sprite squareSprite, SandboxArtSet art)
    {
        GameObject digRoot = new("Dig Nodes");
        PrototypeDigGridAuthority authority = digRoot.AddComponent<PrototypeDigGridAuthority>();
        Vector2[] positions =
        {
            new(-7.25f, -4.75f),
            new(-4.55f, -4.2f),
            new(-2.15f, 2.65f),
            new(2.25f, -3.25f),
            new(4.35f, 2.65f),
            new(7.25f, -4.55f),
        };

        for (int index = 0; index < positions.Length; index++)
        {
            GameObject node = CreateGreyboxSprite(
                $"Dig Node {index + 1}",
                squareSprite,
                new Color(0.85f, 0.64f, 0.25f),
                digRoot.transform);
            node.transform.position = positions[index];
            SetWorldSize(node, new Vector2(0.9f, 0.9f));
            node.GetComponent<SpriteRenderer>().color = Color.clear;
            BoxCollider2D collider = node.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.9f);
            PrototypeDigNode digNode = node.AddComponent<PrototypeDigNode>();
            digNode.Configure(authority, index % 3, index / 3);
            SpriteRenderer nodeRenderer = node.GetComponent<SpriteRenderer>();
            nodeRenderer.sprite = art.DigIntact;
            nodeRenderer.color = Color.white;
            nodeRenderer.sortingOrder = 5;
            SandboxDigVisual digVisual = node.AddComponent<SandboxDigVisual>();
            digVisual.Configure(art.DigIntact, art.DigCracked, art.DigOpened);
        }
    }

    private static void CreateWorldDressing(SandboxArtSet art)
    {
        GameObject dressing = new("Desert Dressing");
        Vector2[] tufts =
        {
            new(-7.7f, 4.8f), new(-7.9f, 0.9f), new(-7.55f, -2.3f),
            new(-3.4f, -5.05f), new(2.0f, 4.95f), new(5.1f, 4.75f),
            new(7.65f, 1.3f), new(6.5f, -4.95f), new(1.1f, -5.15f),
        };
        for (int index = 0; index < tufts.Length; index++)
        {
            GameObject tuft = CreateSpriteObject(
                $"Dry Grass {index + 1}",
                art.SandTuft,
                index % 2 == 0 ? new Color(0.72f, 0.48f, 0.2f) : new Color(0.58f, 0.39f, 0.2f),
                dressing.transform,
                sortingOrder: -750);
            tuft.transform.position = tufts[index];
            tuft.transform.localScale = Vector3.one * (0.85f + ((index % 3) * 0.12f));
        }

        Vector2[] bones = { new(-7.55f, 3.1f), new(-1.8f, -5.15f), new(7.45f, 4.8f), new(6.9f, -3.7f) };
        for (int index = 0; index < bones.Length; index++)
        {
            GameObject bone = CreateSpriteObject(
                $"Sun-bleached Bone {index + 1}",
                art.Bone,
                new Color(0.88f, 0.79f, 0.63f),
                dressing.transform,
                sortingOrder: -740);
            bone.transform.position = bones[index];
            bone.transform.rotation = Quaternion.Euler(0f, 0f, index * 37f);
        }
    }

    private static void CreateInteractiveObjects(SandboxArtSet art)
    {
        GameObject interactiveRoot = new("Interactive Objects");

        // Destructible Vases
        GameObject vasesRoot = new("Destructible Vases");
        vasesRoot.transform.SetParent(interactiveRoot.transform, false);
        Vector2[] vasePositions =
        {
            new(-6.2f, 4.8f), new(-5.4f, 4.8f), new(6.2f, -4.8f),
            new(6.8f, -4.8f), new(-14.2f, -8.5f), new(14.2f, 8.5f)
        };
        for (int i = 0; i < vasePositions.Length; i++)
        {
            GameObject vaseObj = new($"DestructibleVase {i + 1}");
            vaseObj.transform.SetParent(vasesRoot.transform, false);
            vaseObj.transform.position = vasePositions[i];
            vaseObj.AddComponent<PrototypeDestructibleVase>();
        }

        // Ancient Obelisks
        GameObject obelisksRoot = new("Ancient Obelisks");
        obelisksRoot.transform.SetParent(interactiveRoot.transform, false);
        GameObject ob1 = new("Rune Obelisk West");
        ob1.transform.SetParent(obelisksRoot.transform, false);
        ob1.transform.position = new Vector3(-9.5f, 5.2f, 0f);
        ob1.AddComponent<PrototypeAncientRuneObelisk>();

        GameObject ob2 = new("Rune Obelisk East");
        ob2.transform.SetParent(obelisksRoot.transform, false);
        ob2.transform.position = new Vector3(9.5f, 5.2f, 0f);
        ob2.AddComponent<PrototypeAncientRuneObelisk>();

        // Ruin Doors
        GameObject doorsRoot = new("Ruin Doors");
        doorsRoot.transform.SetParent(interactiveRoot.transform, false);
        GameObject door1 = new("Ruin Door Sanctuary");
        door1.transform.SetParent(doorsRoot.transform, false);
        door1.transform.position = new Vector3(0f, 3.5f, 0f);
        door1.AddComponent<PrototypeDesertRuinDoor>();
    }

    private static void CreateRuinWall(
        string name,
        Vector2 position,
        Vector2 size,
        Sprite squareSprite,
        Sprite ruinTile,
        Transform parent)
    {
        GameObject wall = CreateGreyboxSprite(name, squareSprite, Color.clear, parent);
        wall.transform.position = position;
        SetWorldSize(wall, size);
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        int depthOrder = 220 - Mathf.RoundToInt(position.y * 18f);
        GameObject shadow = CreateTiledSprite(
            "Depth Shadow",
            ruinTile,
            new Vector2(size.x + 0.28f, size.y + 0.32f),
            new Color(0.18f, 0.13f, 0.12f, 0.72f),
            wall.transform,
            depthOrder - 2);
        shadow.transform.localPosition = new Vector3(0.16f, -0.28f, 0f);

        GameObject face = CreateTiledSprite(
            "Clay Block Face",
            ruinTile,
            size,
            new Color(0.72f, 0.51f, 0.34f),
            wall.transform,
            depthOrder);
        face.transform.localPosition = Vector3.zero;

        float capHeight = Mathf.Min(0.28f, size.y * 0.32f);
        GameObject cap = CreateTiledSprite(
            "Sunlit Wall Cap",
            ruinTile,
            new Vector2(size.x, capHeight),
            new Color(1f, 0.79f, 0.46f),
            wall.transform,
            depthOrder + 1);
        cap.transform.localPosition = new Vector3(0f, (size.y * 0.5f) - (capHeight * 0.5f), 0f);
    }

    private static GameObject CreateTiledSprite(
        string name,
        Sprite sprite,
        Vector2 size,
        Color color,
        Transform parent,
        int sortingOrder)
    {
        GameObject gameObject = CreateSpriteObject(name, sprite, color, parent, sortingOrder);
        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;
        return gameObject;
    }

    private static GameObject CreateSpriteObject(
        string name,
        Sprite sprite,
        Color color,
        Transform parent,
        int sortingOrder)
    {
        GameObject gameObject = new(name);
        gameObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return gameObject;
    }

    private static Camera CreateCamera(
        Transform target,
        TopDownPlayerController controller,
        TopDownMovementProfile profile)
    {
        GameObject cameraObject = new("Gameplay Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera gameplayCamera = cameraObject.AddComponent<Camera>();
        gameplayCamera.orthographic = true;
        gameplayCamera.orthographicSize = 5.4f;
        gameplayCamera.clearFlags = CameraClearFlags.SolidColor;
        gameplayCamera.backgroundColor = new Color(0.16f, 0.105f, 0.09f);

        cameraObject.AddComponent<AudioListener>();
        OrthographicCameraFollow follow = cameraObject.AddComponent<OrthographicCameraFollow>();
        follow.Configure(
            target,
            controller,
            profile.CameraFollowSharpness,
            minBounds: new Vector2(-23f, -15f),
            maxBounds: new Vector2(23f, 15f),
            configuredAimLookAhead: 1.15f);
        follow.SetPixelDensity(32);
        follow.SnapToTarget();
        return gameplayCamera;
    }

    private static void CreateWall(
        string name,
        Vector2 position,
        Vector2 size,
        Sprite squareSprite,
        Transform parent)
    {
        GameObject wall = CreateGreyboxSprite(name, squareSprite, new Color(0.42f, 0.44f, 0.46f), parent);
        wall.transform.position = position;
        SetWorldSize(wall, size);
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static GameObject CreateGreyboxSprite(string name, Sprite sprite, Color color, Transform parent)
    {
        GameObject gameObject = new(name);
        gameObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        return gameObject;
    }

    private static void SetWorldSize(GameObject gameObject, Vector2 size)
    {
        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        gameObject.transform.localScale = Vector3.one;
    }

    private static void EnsureAssetFolder(string assetPath)
    {
        string[] segments = assetPath.Split('/');
        string current = segments[0];

        for (int index = 1; index < segments.Length; index++)
        {
            string next = $"{current}/{segments[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }

            current = next;
        }
    }

    private static void EnsureSceneInBuildSettings()
    {
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        int index = System.Array.FindIndex(existing, entry => entry.path == ScenePath);
        if (index >= 0)
        {
            if (!existing[index].enabled)
            {
                existing[index] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = existing;
            }

            return;
        }

        EditorBuildSettings.scenes = existing
            .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
            .ToArray();
    }
}
}

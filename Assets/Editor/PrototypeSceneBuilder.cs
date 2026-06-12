using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBuilder
{
    private const string SpriteFolder = "Assets/Art/Player";
    private const string BackgroundPath = "Assets/Art/Backgrounds/gothic_stage_background.png";
    private const string AnimationFolder = "Assets/Animations/Player";
    private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
    private const string ControllerPath = AnimationFolder + "/PlayerAnimator.controller";

    [MenuItem("Tools/LILIE/Build Animation Prototype")]
    public static void Build()
    {
        EnsureFolders();
        int groundLayer = EnsureLayer("Ground");
        Sprite squareSprite = CreateSquareSprite();
        List<Sprite> idleSprites = LoadSprites("idle_*.png");
        List<Sprite> runSprites = LoadSprites("run_*.png");

        AnimationClip idleClip = CreateStateClip("Idle", true, 8f);
        AnimationClip runClip = CreateStateClip("Run", true, 10f);
        AnimationClip jumpClip = CreateStateClip("Jump", false, 1f);
        AnimatorController animatorController = CreateAnimatorController(idleClip, runClip, jumpClip);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndLight(out CameraFollow cameraFollow);
        CreateBackground();
        CreateStage(squareSprite, groundLayer);
        PlayerController player = CreatePlayer(idleSprites, runSprites, animatorController, groundLayer);
        cameraFollow.Target = player.transform;
        CreateDebugOverlay(player);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        Selection.activeGameObject = player.gameObject;
        EditorGUIUtility.PingObject(player.gameObject);
        Debug.Log("PrototypeScene build complete: " + ScenePath);
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(SpriteFolder);
        Directory.CreateDirectory(AnimationFolder);
        Directory.CreateDirectory("Assets/Scenes");
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
        {
            return existing;
        }

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning("No free layer slot was available for Ground. Falling back to Default.");
        return 0;
    }

    private static Sprite CreateSquareSprite()
    {
        string texturePath = "Assets/Art/square_stage.png";
        if (!File.Exists(texturePath))
        {
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = Enumerable.Repeat(Color.white, 32 * 32).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        }

        AssetDatabase.ImportAsset(texturePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32f;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static List<Sprite> LoadSprites(string searchPattern)
    {
        foreach (string path in Directory.GetFiles(SpriteFolder, searchPattern))
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace("\\", "/"));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 220f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        List<Sprite> sprites = Directory.GetFiles(SpriteFolder, searchPattern)
            .OrderBy(path => path)
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace("\\", "/")))
            .Where(sprite => sprite != null)
            .ToList();

        if (sprites.Count == 0)
        {
            throw new FileNotFoundException("No sprites found for " + searchPattern);
        }

        return sprites;
    }

    private static AnimationClip CreateStateClip(string clipName, bool loop, float frameRate)
    {
        string clipPath = $"{AnimationFolder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.ClearCurves();
        clip.frameRate = frameRate;
        AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = loop });
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static AnimatorController CreateAnimatorController(
        AnimationClip idleClip,
        AnimationClip runClip,
        AnimationClip jumpClip)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle", new Vector3(250f, 80f, 0f));
        AnimatorState run = stateMachine.AddState("Run", new Vector3(520f, 80f, 0f));
        AnimatorState jump = stateMachine.AddState("Jump", new Vector3(385f, 240f, 0f));

        idle.motion = idleClip;
        run.motion = runClip;
        jump.motion = jumpClip;
        stateMachine.defaultState = idle;

        AddTransition(idle, run, AnimatorConditionMode.Greater, 0.1f, "Speed", true);
        AddTransition(run, idle, AnimatorConditionMode.Less, 0.1f, "Speed", true);
        AddTransition(idle, jump, AnimatorConditionMode.IfNot, 0f, "IsGrounded", false);
        AddTransition(run, jump, AnimatorConditionMode.IfNot, 0f, "IsGrounded", false);
        AddTransition(jump, idle, AnimatorConditionMode.Less, 0.1f, "Speed", true);
        AddTransition(jump, run, AnimatorConditionMode.Greater, 0.1f, "Speed", true);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void AddTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode speedMode,
        float speedThreshold,
        string speedParameter,
        bool requireGrounded)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(speedMode, speedThreshold, speedParameter);

        if (requireGrounded)
        {
            transition.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        }
    }

    private static void CreateCameraAndLight(out CameraFollow cameraFollow)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();
        cameraFollow = cameraObject.AddComponent<CameraFollow>();

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateBackground()
    {
        if (!File.Exists(BackgroundPath))
        {
            Debug.LogWarning("Background image not found: " + BackgroundPath);
            return;
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 180f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        GameObject background = new GameObject("Background");
        background.transform.position = new Vector3(0f, 0f, 5f);
        background.transform.localScale = new Vector3(1.55f, 1.55f, 1f);

        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.sortingOrder = -20;
    }

    private static void CreateStage(Sprite squareSprite, int groundLayer)
    {
        CreatePlatform("Ground", squareSprite, groundLayer, new Vector3(0f, -3f, 0f), new Vector3(20f, 1f, 1f));
        CreatePlatform("Platform_Left", squareSprite, groundLayer, new Vector3(-5f, -1.2f, 0f), new Vector3(3f, 0.35f, 1f));
        CreatePlatform("Platform_Mid", squareSprite, groundLayer, new Vector3(1.5f, 0.1f, 0f), new Vector3(3.2f, 0.35f, 1f));
        CreatePlatform("Platform_Right", squareSprite, groundLayer, new Vector3(6f, 1.3f, 0f), new Vector3(2.6f, 0.35f, 1f));
    }

    private static void CreatePlatform(string name, Sprite sprite, int layer, Vector3 position, Vector3 scale)
    {
        GameObject platform = new GameObject(name);
        platform.layer = layer;
        platform.transform.position = position;
        platform.transform.localScale = scale;

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = name == "Ground" ? new Color(0.35f, 0.40f, 0.37f) : new Color(0.42f, 0.49f, 0.45f);
        renderer.sortingOrder = -2;

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static PlayerController CreatePlayer(
        List<Sprite> idleSprites,
        List<Sprite> runSprites,
        AnimatorController controller,
        int groundLayer)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, -1.65f, 0f);

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = idleSprites.First();
        renderer.sortingOrder = 5;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.8f, 1.7f);
        collider.offset = new Vector2(0f, 0.05f);

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.92f, 0f);

        PlayerController controllerComponent = player.AddComponent<PlayerController>();
        SerializedObject serializedController = new SerializedObject(controllerComponent);
        serializedController.FindProperty("moveSpeed").floatValue = 6f;
        serializedController.FindProperty("jumpForce").floatValue = 12f;
        serializedController.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        serializedController.FindProperty("groundCheckRadius").floatValue = 0.22f;
        serializedController.FindProperty("groundLayer").intValue = 1 << groundLayer;
        AssignSpriteArray(serializedController.FindProperty("idleFrames"), idleSprites);
        AssignSpriteArray(serializedController.FindProperty("runFrames"), runSprites);
        serializedController.FindProperty("jumpSprite").objectReferenceValue = idleSprites.Last();
        serializedController.FindProperty("animationFrameRate").floatValue = 8f;
        serializedController.ApplyModifiedProperties();

        return controllerComponent;
    }

    private static void AssignSpriteArray(SerializedProperty property, List<Sprite> sprites)
    {
        property.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    private static void CreateDebugOverlay(PlayerController player)
    {
        GameObject debugObject = new GameObject("DebugOverlay");
        PlayerDebugOverlay overlay = debugObject.AddComponent<PlayerDebugOverlay>();
        overlay.Player = player;
        EditorUtility.SetDirty(overlay);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(scene => scene.path != scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

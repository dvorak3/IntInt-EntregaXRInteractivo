using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Management;

public static class XRPracticeSceneBuilder
{
    private const string SampleRoot = "Assets/Samples";
    private const string SettingsRoot = "Assets/Settings";
    private const string DirectGrabModelPath = "Assets/Art/KenneyFoodKit/Banana/banana.obj";
    private static readonly Color CubeColor = new Color(0.1f, 0.62f, 0.95f);
    private static readonly Color SphereColor = new Color(0.24f, 0.82f, 0.45f);

    [MenuItem("XR Practice/Import Samples, Configure OpenXR and Build Scene")]
    public static void RunCompleteSetup()
    {
        ImportSample("Starter Assets");
        ImportSample("XR Interaction Simulator");
        ConfigureOpenXRForProject(false);
        BuildPracticeSceneForProject(false);
        AssetDatabase.SaveAssets();
        Debug.Log("XR practice setup complete: samples imported, OpenXR configured, and scene saved.");
    }

    [MenuItem("XR Practice/Configure OpenXR for Windows and Quest")]
    private static void ConfigureOpenXR()
    {
        ConfigureOpenXRForProject(true);
    }

    [MenuItem("XR Practice/Open OpenXR Settings")]
    public static void OpenOpenXRSettings()
    {
        EditorApplication.delayCall +=
            () => SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");
    }

    internal static void ConfigureOpenXRForProject(bool showDialog)
    {
        var settingsByTarget = FindOrCreateXRSettingsStore();
        var targets = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android };

        foreach (var target in targets)
        {
            if (!settingsByTarget.HasSettingsForBuildTarget(target))
                settingsByTarget.CreateDefaultSettingsForBuildTarget(target);
            if (!settingsByTarget.HasManagerSettingsForBuildTarget(target))
                settingsByTarget.CreateDefaultManagerSettingsForBuildTarget(target);

            var manager = settingsByTarget.ManagerSettingsForBuildTarget(target);
            if (manager == null ||
                !XRPackageMetadataStore.AssignLoader(
                    manager,
                    "UnityEngine.XR.OpenXR.OpenXRLoader",
                    target))
            {
                throw new InvalidOperationException(
                    $"No se pudo asignar OpenXR a {target}. Comprueba que OpenXR Plugin terminó de instalarse.");
            }

            FeatureHelpers.RefreshFeatures(target);
            var openXrSettings = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (openXrSettings == null)
            {
                throw new InvalidOperationException(
                    $"Unity todavía no creó los ajustes OpenXR para {target}. Abre Project Settings > XR Plug-in Management > OpenXR y vuelve a intentarlo.");
            }

            var enabledProfiles = new Dictionary<string, string>
            {
                ["com.unity.openxr.feature.input.oculustouch"] = "Oculus Touch Controller Profile",
                ["com.unity.openxr.feature.input.metaquestplus"] = "Meta Quest Touch Plus Controller Profile",
                ["com.unity.openxr.feature.input.metaquestpro"] = "Meta Quest Touch Pro Controller Profile",
            };
            foreach (var profile in enabledProfiles)
            {
                var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(target, profile.Key);
                if (feature == null)
                {
                    Debug.LogWarning($"El perfil OpenXR {profile.Value} no está disponible para {target}.");
                    continue;
                }

                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }

            EditorUtility.SetDirty(openXrSettings);
            EditorUtility.SetDirty(manager);
        }

        AssetDatabase.SaveAssets();
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "XR Practice",
                "OpenXR está asignado a Standalone y Android. Se activaron los perfiles Oculus Touch, Meta Quest Touch Plus y Touch Pro si están incluidos por la versión instalada de OpenXR. Revisa las validaciones de Project Settings > XR Plug-in Management.",
                "Aceptar");
        }
    }

    private static XRGeneralSettingsPerBuildTarget FindOrCreateXRSettingsStore()
    {
        var assetGuids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
        if (assetGuids.Length > 0)
        {
            var existing = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                AssetDatabase.GUIDToAssetPath(assetGuids[0]));
            if (existing != null)
                return existing;
        }

        var settingsStore = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
        AssetDatabase.CreateAsset(settingsStore, SettingsRoot + "/XRGeneralSettingsPerBuildTarget.asset");
        AssetDatabase.SaveAssets();
        return settingsStore;
    }

    [MenuItem("XR Practice/Build Practice Scene")]
    private static void BuildPracticeScene()
    {
        BuildPracticeSceneForProject(true);
    }

    internal static void BuildPracticeSceneForProject(bool showDialog)
    {
        var rigPrefab = FindSamplePrefab("XR Origin (XR Rig)");
        if (rigPrefab == null)
        {
            throw new InvalidOperationException(
                "No se encontró el prefab XR Origin (XR Rig). La muestra Starter Assets no se importó correctamente.");
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena abierta para generar la práctica.");
        if (string.IsNullOrEmpty(scene.path))
        {
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path) == null)
                    continue;

                scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                break;
            }

            if (string.IsNullOrEmpty(scene.path))
            {
                const string defaultScenePath = "Assets/Scenes/SampleScene.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(defaultScenePath) == null)
                {
                    throw new InvalidOperationException(
                        "No hay una escena guardada ni una escena habilitada en Build Settings para generar la práctica.");
                }

                scene = EditorSceneManager.OpenScene(defaultScenePath, OpenSceneMode.Single);
            }
        }

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build XR practice scene");

        RemoveGeneratedObjects(scene);

        var rig = PrefabUtility.InstantiatePrefab(rigPrefab, scene) as GameObject;
        if (rig == null)
            throw new InvalidOperationException("No se pudo crear el prefab XR Origin (XR Rig).");
        Undo.RegisterCreatedObjectUndo(rig, "Create XR Origin");
        rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (UnityEngine.Object.FindAnyObjectByType<XRInteractionManager>() == null)
        {
            var interactionManager = new GameObject("XR Interaction Manager");
            Undo.RegisterCreatedObjectUndo(interactionManager, "Create XR Interaction Manager");
            interactionManager.AddComponent<XRInteractionManager>();
        }

        CreateEnvironment(scene);
        var statusText = CreateStatusPanel();
        CreatePracticeObjects(statusText);

        var simulatorPrefab = FindSamplePrefab("XR Interaction Simulator");
        if (simulatorPrefab != null)
        {
            var simulator = PrefabUtility.InstantiatePrefab(simulatorPrefab, scene) as GameObject;
            if (simulator != null)
            {
                Undo.RegisterCreatedObjectUndo(simulator, "Create XR Interaction Simulator");
                simulator.name = "XR Interaction Simulator (solo Play Mode)";
            }
        }
        else
        {
            Debug.LogWarning(
                "No se encontró la muestra XR Interaction Simulator. La escena está lista para visor; importa esa muestra para simular en Play Mode.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"No se pudo guardar la escena XR: {scene.path}");
        Undo.CollapseUndoOperations(undoGroup);
        var banana = GameObject.Find("Kenney Banana (Direct Interactor)");
        if (banana == null)
            throw new InvalidOperationException("No se encontró la banana tras crear la escena.");

        Selection.objects = new UnityEngine.Object[]
        {
            GameObject.Find("Practice Cube (Ray)"),
            GameObject.Find("Practice Sphere (Ray)"),
            banana,
        };
        Selection.activeGameObject = banana;
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.pivot = new Vector3(0f, 1.1f, 2.4f);
            sceneView.rotation = Quaternion.identity;
            sceneView.size = 3.5f;
            sceneView.Repaint();
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "XR Practice",
                "Escena XR creada y guardada. La banana gratuita de Kenney reemplaza al objeto provisional y conserva XR Grab Interactable, Rigidbody y Collider.",
                "Aceptar");
        }
    }

    private static GameObject FindSamplePrefab(string prefabName)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { SampleRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.name == prefabName)
                return prefab;
        }

        return null;
    }

    private static void ImportSample(string displayName)
    {
        var samples = Sample.FindByPackage("com.unity.xr.interaction.toolkit", string.Empty);
        if (samples != null)
        {
            foreach (var sample in samples)
            {
                if (sample.displayName != displayName)
                    continue;

                sample.Import(Sample.ImportOptions.OverridePreviousImports);
                Debug.Log($"Imported XR Interaction Toolkit sample: {displayName}.");
                return;
            }
        }

        throw new InvalidOperationException(
            $"Unity Package Manager could not find the {displayName} XR sample.");
    }

    private static void RemoveGeneratedObjects(Scene scene)
    {
        var generatedNames = new HashSet<string>
        {
            "Main Camera",
            "XR Origin (XR Rig)",
            "XR Interaction Manager",
            "XR Interaction Simulator (solo Play Mode)",
            "Practice Floor",
            "Practice Cube (Ray)",
            "Practice Sphere (Ray)",
            "Asset Store Object (replace with your prefab)",
            "Kenney Banana (Direct Interactor)",
            "XR Practice Status Canvas",
        };

        foreach (var root in scene.GetRootGameObjects())
        {
            if (generatedNames.Contains(root.name))
                Undo.DestroyObjectImmediate(root);
        }
    }

    private static void CreateEnvironment(Scene scene)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(floor, "Create practice floor");
        floor.name = "Practice Floor";
        floor.transform.SetPositionAndRotation(new Vector3(0f, -0.08f, 2.2f), Quaternion.identity);
        floor.transform.localScale = new Vector3(1.3f, 1f, 0.9f);
        SetMaterial(floor.GetComponent<Renderer>(), "XRPractice_Floor.mat", new Color(0.28f, 0.31f, 0.34f));
        SceneManager.MoveGameObjectToScene(floor, scene);
    }

    private static void CreatePracticeObjects(Text statusText)
    {
        CreateRayGrabObject(
            PrimitiveType.Cube,
            "Practice Cube (Ray)",
            new Vector3(-1.05f, 0.5f, 3f),
            CubeColor,
            new Vector3(1f, 1f, 1f));

        CreateRayGrabObject(
            PrimitiveType.Sphere,
            "Practice Sphere (Ray)",
            new Vector3(1.1f, 0.5f, 4f),
            SphereColor,
            Vector3.one);

        CreateDirectGrabModel(statusText);
    }

    private static void CreateRayGrabObject(
        PrimitiveType primitive,
        string objectName,
        Vector3 position,
        Color color,
        Vector3 scale)
    {
        var target = GameObject.CreatePrimitive(primitive);
        Undo.RegisterCreatedObjectUndo(target, "Create " + objectName);
        target.name = objectName;
        target.transform.SetPositionAndRotation(position, Quaternion.identity);
        target.transform.localScale = scale;
        SetMaterial(target.GetComponent<Renderer>(), objectName.Replace(' ', '_') + ".mat", color);
        AddGrabComponents(target);
        target.AddComponent<XRSelectionColorFeedback>();
    }

    private static void CreateDirectGrabModel(Text statusText)
    {
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DirectGrabModelPath);
        if (modelPrefab == null)
            throw new InvalidOperationException($"No se encontró el modelo descargado: {DirectGrabModelPath}");

        var visual = PrefabUtility.InstantiatePrefab(modelPrefab, SceneManager.GetActiveScene()) as GameObject;
        if (visual == null)
            throw new InvalidOperationException($"No se pudo instanciar el modelo descargado: {DirectGrabModelPath}");

        Undo.RegisterCreatedObjectUndo(visual, "Create banana visual");
        var artifact = new GameObject("Kenney Banana (Direct Interactor)");
        Undo.RegisterCreatedObjectUndo(artifact, "Create Kenney Banana");
        visual.name = "banana";
        visual.transform.SetParent(artifact.transform, false);
        visual.transform.localScale = Vector3.one * 1.5f;
        artifact.transform.SetPositionAndRotation(
            new Vector3(0.55f, -0.08f, 2.8f),
            Quaternion.Euler(0f, 45f, 0f));
        var collider = artifact.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.15f, 0f);
        collider.size = new Vector3(0.24f, 0.34f, 1f);
        AddGrabComponents(artifact);
        artifact.GetComponent<Rigidbody>().mass = 0.35f;
        var status = artifact.AddComponent<GrabStatusUI>();
        status.SetStatusText(statusText);
    }

    private static Text CreateStatusPanel()
    {
        var canvasObject = new GameObject(
            "XR Practice Status Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create XR status UI");

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObject.transform.SetPositionAndRotation(
            new Vector3(0f, 1.85f, 1.55f),
            Quaternion.identity);
        canvasObject.transform.localScale = Vector3.one * 0.0025f;

        var rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700f, 150f);

        var background = canvasObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);

        var textObject = new GameObject("Grab Status", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 34;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "Acércate y agarra el objeto.";

        return text;
    }

    private static void AddGrabComponents(GameObject target)
    {
        var body = target.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        target.AddComponent<XRGrabInteractable>();
    }

    private static void SetMaterial(Renderer renderer, string fileName, Color color)
    {
        var path = SettingsRoot + "/" + fileName;
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("No se encontró un shader Lit para los objetos de práctica.");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        renderer.sharedMaterial = material;
    }
}

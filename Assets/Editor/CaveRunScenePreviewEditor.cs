using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class CaveRunScenePreviewEditor
{
    private const string GameplayScenePath = "Assets/_Project/Scenes/20_Gameplay.unity";

    static CaveRunScenePreviewEditor()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += EnsureGameplayPreview;
    }

    [MenuItem("Tools/Cave Run/Generate or Refresh Scene Level")]
    private static void GenerateOrRefreshSceneLevel()
    {
        GeneratePreview(true, false);
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == GameplayScenePath)
        {
            EditorApplication.delayCall += EnsureGameplayPreview;
        }
    }

    private static void EnsureGameplayPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameplayScenePath || GameObject.Find("Crystal Cavern - Gauntlet") != null)
        {
            return;
        }

        GeneratePreview(false, !scene.isDirty);
    }

    private static void GeneratePreview(bool refresh, bool saveScene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Generate the cave scene preview after exiting Play Mode.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameplayScenePath)
        {
            Debug.LogError("Open 20_Gameplay.unity before generating the cave scene preview.");
            return;
        }

        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("The gameplay scene does not contain a GameManager.");
            return;
        }

        CaveRunController controller = gameManager.GetComponent<CaveRunController>();
        if (controller == null)
        {
            Debug.LogError("The GameManager is missing its CaveRunController.");
            return;
        }

        SerializedObject serializedManager = new(gameManager);
        SerializedProperty playerProperty = serializedManager.FindProperty("_playerInScene");
        GameObject player = playerProperty?.objectReferenceValue as GameObject;
        if (player == null)
        {
            Debug.LogError("The GameManager has no player assigned for the cave scene preview.", gameManager);
            return;
        }

        GameObject existingPreview = GameObject.Find("Crystal Cavern - Gauntlet");
        if (existingPreview != null)
        {
            if (!refresh)
            {
                return;
            }

            Undo.DestroyObjectImmediate(existingPreview);
        }

        GameObject oldLevel = GameObject.Find("Level");
        if (oldLevel != null && oldLevel.activeSelf)
        {
            Undo.RecordObject(oldLevel, "Hide legacy gameplay level");
            oldLevel.SetActive(false);
            EditorUtility.SetDirty(oldLevel);
        }

        Transform preview = controller.GenerateScenePreview(player);
        if (preview == null)
        {
            return;
        }
        Undo.RegisterCreatedObjectUndo(preview.gameObject, "Generate cave scene preview");
        EditorSceneManager.MarkSceneDirty(scene);

        if (saveScene)
        {
            EditorSceneManager.SaveScene(scene);
        }

        Selection.activeGameObject = preview.gameObject;
        FramePreview(preview);
        Debug.Log("Generated the cave level in the gameplay Scene. The same layout is rebuilt with active gameplay logic in Play Mode.", preview);
    }

    private static void FramePreview(Transform preview)
    {
        SpriteRenderer[] renderers = preview.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0 || SceneView.lastActiveSceneView == null)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        bounds.Expand(new Vector3(4f, 4f, 0f));
        SceneView.lastActiveSceneView.Frame(bounds, false);
    }
}

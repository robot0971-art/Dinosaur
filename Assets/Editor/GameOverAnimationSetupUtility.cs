using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameOverAnimationSetupUtility
{
    private const string GameOverPanelName = "Game over Panel";
    private const string GameOverImageName = "Game over Image";
    private const string RestartButtonName = "Restart Button";
    private const string GameOverControllerPath = "Assets/Image/Game over Image.controller";
    private const string GameOverClipPath = "Assets/Image/Game over.anim";
    private const string RestartControllerPath = "Assets/Image/Restart Button.controller";
    private const string RestartClipPath = "Assets/Image/Restart Button.anim";
    private const string GameOverSpritePath = "Assets/Image/Game over.png";

    [MenuItem("Tools/Dino Game/UI/Apply Game Over Animations")]
    public static void Apply()
    {
        var gameOverPanel = GameObject.Find(GameOverPanelName);
        if (gameOverPanel == null)
        {
            Debug.LogError($"{GameOverPanelName} was not found in the active scene.");
            return;
        }

        var imageController = EnsureControllerState(GameOverControllerPath, GameOverClipPath, "Game over");
        var restartController = EnsureControllerState(RestartControllerPath, RestartClipPath, RestartButtonName);
        var imageObject = EnsureGameOverImage(gameOverPanel, imageController);
        ApplyRestartButtonAnimator(restartController);

        EditorUtility.SetDirty(gameOverPanel);
        EditorUtility.SetDirty(imageObject);
        EditorSceneManager.MarkSceneDirty(gameOverPanel.scene);
        AssetDatabase.SaveAssets();
    }

    private static AnimatorController EnsureControllerState(string controllerPath, string clipPath, string stateName)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (controller == null || clip == null)
        {
            Debug.LogError($"Missing animation asset: {controllerPath} / {clipPath}");
            return controller;
        }

        if (controller.layers == null || controller.layers.Length == 0)
        {
            var stateMachine = new AnimatorStateMachine { name = "Base Layer" };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);
            controller.AddLayer(new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = stateMachine,
                defaultWeight = 1f
            });
        }

        var root = controller.layers[0].stateMachine;
        var state = FindState(root, stateName) ?? root.AddState(stateName);
        state.motion = clip;
        root.defaultState = state;
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(root);
        return controller;
    }

    private static AnimatorState FindState(AnimatorStateMachine root, string stateName)
    {
        foreach (var childState in root.states)
        {
            if (childState.state.name == stateName)
            {
                return childState.state;
            }
        }

        return null;
    }

    private static GameObject EnsureGameOverImage(GameObject gameOverPanel, RuntimeAnimatorController controller)
    {
        var imageTransform = gameOverPanel.transform.Find(GameOverImageName);
        var imageObject = imageTransform != null
            ? imageTransform.gameObject
            : new GameObject(GameOverImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        imageObject.transform.SetParent(gameOverPanel.transform, false);
        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 95f);
        rect.sizeDelta = new Vector2(620f, 210f);
        rect.localScale = Vector3.one;

        var image = imageObject.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverSpritePath);
        image.preserveAspect = true;
        image.raycastTarget = false;

        var animator = imageObject.GetComponent<Animator>() ?? imageObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        return imageObject;
    }

    private static void ApplyRestartButtonAnimator(RuntimeAnimatorController controller)
    {
        var restartButton = GameObject.Find(RestartButtonName);
        if (restartButton == null)
        {
            Debug.LogWarning($"{RestartButtonName} was not found in the active scene.");
            return;
        }

        var animator = restartButton.GetComponent<Animator>() ?? restartButton.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        EditorUtility.SetDirty(restartButton);
    }
}

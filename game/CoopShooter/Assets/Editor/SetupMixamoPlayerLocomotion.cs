using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public static class SetupMixamoPlayerLocomotion
{
    private const string ControllerDirectory = "Assets/Animations/Player";
    private const string ControllerPath = ControllerDirectory + "/PlayerLocomotion.controller";
    private const string UpperBodyMaskPath = ControllerDirectory + "/UpperBodyFire.mask";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string MixamoModelPath = "Assets/Rifle 8-Way Locomotion Pack/Ch15_nonPBR.fbx";
    private const string LocomotionPackPath = "Assets/Rifle 8-Way Locomotion Pack/";
    private const string ShooterPackPath = "Assets/Animations/MixamoShooting/Shooter Pack/";
    private const float LocomotionOrientationOffsetY = -45f;
    private const float AimOrientationOffsetY = -45f;

    [MenuItem("Tools/Project Z/Setup Mixamo Player Locomotion")]
    public static void RunSetup()
    {
        EnsureDirectoryExists(ControllerDirectory);
        EnsureLocomotionClipImportSettings();
        EnsureAimingClipImportSettings();
        CreateOrReplaceController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Mixamo locomotion controller updated. Prefab hierarchy was left untouched.");
    }

    [MenuItem("Tools/Project Z/Rebuild Player Visual From Mixamo")]
    public static void RebuildPlayerVisual()
    {
        EnsureDirectoryExists(ControllerDirectory);
        EnsureLocomotionClipImportSettings();
        EnsureAimingClipImportSettings();

        AnimatorController controller = CreateOrReplaceController();
        GameObject playerPrefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        try
        {
            Transform visualAnchor = EnsureVisualAnchor(playerPrefabRoot.transform);
            GameObject visualInstance = EnsureCharacterVisual(visualAnchor);
            Animator animator = EnsureAnimator(visualInstance, controller);
            EnsureRigBuilder(visualInstance);
            PlayerAnimator playerAnimator = EnsurePlayerAnimator(playerPrefabRoot, animator);
            UpdatePlayerRefs(playerPrefabRoot, playerAnimator);

            PrefabUtility.SaveAsPrefabAsset(playerPrefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerPrefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Player visual rebuilt from Mixamo and controller wired to Player.prefab.");
    }

    private static AnimatorController CreateOrReplaceController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsAiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDowned", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = new ChildAnimatorState[0];
        stateMachine.anyStateTransitions = new AnimatorStateTransition[0];

        BlendTree locomotionBlendTree;
        AnimatorState locomotion = stateMachine.AddState("Locomotion");
        locomotion.motion = CreateLocomotionBlendTree(controller, out locomotionBlendTree);
        locomotion.writeDefaultValues = true;
        stateMachine.defaultState = locomotion;

        BlendTree aimBlendTree;
        AnimatorState aimLocomotion = stateMachine.AddState("AimLocomotion");
        aimLocomotion.motion = CreateAimBlendTree(controller, out aimBlendTree);
        aimLocomotion.writeDefaultValues = true;

        AddPlaceholderState(controller, stateMachine, "Downed", "idle aiming.fbx", out AnimatorState downedState);
        AddPlaceholderState(controller, stateMachine, "Dead", "death from front headshot.fbx", out AnimatorState deadState);

        AddBoolTransition(locomotion, aimLocomotion, "IsAiming", true);
        AddBoolTransition(aimLocomotion, locomotion, "IsAiming", false);
        AddBoolTransition(locomotion, downedState, "IsDowned", true);
        AddBoolTransition(aimLocomotion, downedState, "IsDowned", true);
        AddBoolTransition(downedState, locomotion, "IsDowned", false);
        AddBoolTransition(locomotion, deadState, "IsDead", true);
        AddBoolTransition(aimLocomotion, deadState, "IsDead", true);
        AddBoolTransition(downedState, deadState, "IsDead", true);

        AddUpperBodyFireLayer(controller);

        return controller;
    }

    private static void AddUpperBodyFireLayer(AnimatorController controller)
    {
        AnimatorControllerLayer fireLayer = new AnimatorControllerLayer
        {
            name = "UpperBodyFire",
            defaultWeight = 1f,
            blendingMode = AnimatorLayerBlendingMode.Override,
            avatarMask = GetOrCreateUpperBodyMask(),
            stateMachine = new AnimatorStateMachine { name = "UpperBodyFire" }
        };

        AssetDatabase.AddObjectToAsset(fireLayer.stateMachine, controller);
        controller.AddLayer(fireLayer);

        AnimatorStateMachine stateMachine = controller.layers[controller.layers.Length - 1].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle");
        idle.writeDefaultValues = true;
        stateMachine.defaultState = idle;

        AnimatorState adsHold = stateMachine.AddState("AdsHold");
        adsHold.motion = LoadClip(ShooterPackPath, "firing rifle.fbx");
        adsHold.writeDefaultValues = true;

        AnimatorState hipFireShot = stateMachine.AddState("HipFireShot");
        hipFireShot.motion = LoadClip(ShooterPackPath, "firing rifle.fbx");
        hipFireShot.writeDefaultValues = true;

        AddBoolTransition(idle, adsHold, "IsAiming", true);
        AddBoolTransition(adsHold, idle, "IsAiming", false);
        AddFireTransition(idle, hipFireShot, false);
        AddReturnTransition(hipFireShot, idle);
    }

    private static Motion CreateLocomotionBlendTree(AnimatorController controller, out BlendTree blendTree)
    {
        blendTree = new BlendTree
        {
            name = "LocomotionBlendTree",
            blendType = BlendTreeType.FreeformCartesian2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);

        AddBlendMotion(blendTree, LocomotionPackPath, "idle.fbx", Vector2.zero);
        AddBlendMotion(blendTree, LocomotionPackPath, "walk forward.fbx", new Vector2(0f, 0.5f));
        AddBlendMotion(blendTree, LocomotionPackPath, "walk backward.fbx", new Vector2(0f, -0.5f));
        AddBlendMotion(blendTree, LocomotionPackPath, "walk left.fbx", new Vector2(-0.5f, 0f));
        AddBlendMotion(blendTree, LocomotionPackPath, "walk right.fbx", new Vector2(0.5f, 0f));
        AddBlendMotion(blendTree, LocomotionPackPath, "walk forward left.fbx", new Vector2(-0.5f, 0.5f));
        AddBlendMotion(blendTree, LocomotionPackPath, "walk forward right.fbx", new Vector2(0.5f, 0.5f));
        AddBlendMotion(blendTree, LocomotionPackPath, "walk backward left.fbx", new Vector2(-0.5f, -0.5f));
        AddBlendMotion(blendTree, LocomotionPackPath, "walk backward right.fbx", new Vector2(0.5f, -0.5f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run forward.fbx", new Vector2(0f, 1f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run backward.fbx", new Vector2(0f, -1f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run left.fbx", new Vector2(-1f, 0f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run right.fbx", new Vector2(1f, 0f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run forward left.fbx", new Vector2(-1f, 1f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run forward right.fbx", new Vector2(1f, 1f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run backward left.fbx", new Vector2(-1f, -1f));
        AddBlendMotion(blendTree, LocomotionPackPath, "run backward right.fbx", new Vector2(1f, -1f));

        return blendTree;
    }

    private static Motion CreateAimBlendTree(AnimatorController controller, out BlendTree blendTree)
    {
        blendTree = new BlendTree
        {
            name = "AimLocomotionBlendTree",
            blendType = BlendTreeType.FreeformCartesian2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);

        AddBlendMotion(blendTree, ShooterPackPath, "rifle aiming idle.fbx", Vector2.zero);
        AddBlendMotion(blendTree, ShooterPackPath, "walking.fbx", new Vector2(0f, 1f));
        AddBlendMotion(blendTree, ShooterPackPath, "walking backwards.fbx", new Vector2(0f, -1f));
        AddBlendMotion(blendTree, ShooterPackPath, "strafe (2).fbx", new Vector2(-1f, 0f));
        AddBlendMotion(blendTree, ShooterPackPath, "strafe.fbx", new Vector2(1f, 0f));

        return blendTree;
    }

    private static void AddPlaceholderState(AnimatorController controller, AnimatorStateMachine stateMachine, string stateName, string clipFileName, out AnimatorState state)
    {
        state = stateMachine.AddState(stateName);
        state.motion = LoadClip(LocomotionPackPath, clipFileName);
        state.writeDefaultValues = true;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddFireTransition(AnimatorState from, AnimatorState to, bool isAiming)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Fire");
        transition.AddCondition(isAiming ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsAiming");
    }

    private static void AddReturnTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.9f;
        transition.duration = 0.08f;
    }

    private static AvatarMask GetOrCreateUpperBodyMask()
    {
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
        }

        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

        EditorUtility.SetDirty(mask);
        return mask;
    }

    private static void AddBlendMotion(BlendTree blendTree, string folderPath, string clipFileName, Vector2 position)
    {
        AnimationClip clip = LoadClip(folderPath, clipFileName);
        if (clip == null)
            return;

        blendTree.AddChild(clip, position);
    }

    private static AnimationClip LoadClip(string folderPath, string clipFileName)
    {
        string path = folderPath + clipFileName;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        return assets.OfType<AnimationClip>().FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }

    private static void EnsureAimingClipImportSettings()
    {
        string[] aimClipPaths =
        {
            ShooterPackPath + "rifle aiming idle.fbx",
            ShooterPackPath + "walking.fbx",
            ShooterPackPath + "walking backwards.fbx",
            ShooterPackPath + "strafe.fbx",
            ShooterPackPath + "strafe (2).fbx",
            ShooterPackPath + "rifle run.fbx",
            ShooterPackPath + "run backwards.fbx",
            ShooterPackPath + "firing rifle.fbx"
        };

        for (int i = 0; i < aimClipPaths.Length; i++)
            EnsureClipImportSettings(aimClipPaths[i], AimOrientationOffsetY, !aimClipPaths[i].EndsWith("firing rifle.fbx"));
    }

    private static void EnsureLocomotionClipImportSettings()
    {
        string[] locomotionClipPaths =
        {
            LocomotionPackPath + "idle.fbx",
            LocomotionPackPath + "idle aiming.fbx",
            LocomotionPackPath + "walk forward.fbx",
            LocomotionPackPath + "walk backward.fbx",
            LocomotionPackPath + "walk left.fbx",
            LocomotionPackPath + "walk right.fbx",
            LocomotionPackPath + "walk forward left.fbx",
            LocomotionPackPath + "walk forward right.fbx",
            LocomotionPackPath + "walk backward left.fbx",
            LocomotionPackPath + "walk backward right.fbx",
            LocomotionPackPath + "run forward.fbx",
            LocomotionPackPath + "run backward.fbx",
            LocomotionPackPath + "run left.fbx",
            LocomotionPackPath + "run right.fbx",
            LocomotionPackPath + "run forward left.fbx",
            LocomotionPackPath + "run forward right.fbx",
            LocomotionPackPath + "run backward left.fbx",
            LocomotionPackPath + "run backward right.fbx",
            LocomotionPackPath + "death from front headshot.fbx"
        };

        for (int i = 0; i < locomotionClipPaths.Length; i++)
            EnsureClipImportSettings(locomotionClipPaths[i], LocomotionOrientationOffsetY, true);
    }

    private static void EnsureClipImportSettings(string assetPath, float orientationOffsetY, bool loopTime)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0)
            return;

        bool changed = false;
        for (int i = 0; i < clips.Length; i++)
        {
            ModelImporterClipAnimation clip = clips[i];

            if (!Mathf.Approximately(clip.rotationOffset, orientationOffsetY))
            {
                clip.rotationOffset = orientationOffsetY;
                changed = true;
            }

            if (clip.loopTime != loopTime)
            {
                clip.loopTime = loopTime;
                changed = true;
            }

            if (!clip.keepOriginalPositionY)
            {
                clip.keepOriginalPositionY = true;
                changed = true;
            }

            clips[i] = clip;
        }

        if (!changed)
            return;

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static Transform EnsureVisualAnchor(Transform playerRoot)
    {
        Transform existing = playerRoot.Find("CharacterVisualAnchor");
        if (existing != null)
            return existing;

        GameObject anchor = new GameObject("CharacterVisualAnchor");
        Transform anchorTransform = anchor.transform;
        anchorTransform.SetParent(playerRoot, false);
        anchorTransform.localPosition = Vector3.zero;
        anchorTransform.localRotation = Quaternion.identity;
        anchorTransform.localScale = Vector3.one;
        return anchorTransform;
    }

    private static GameObject EnsureCharacterVisual(Transform visualAnchor)
    {
        for (int i = visualAnchor.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(visualAnchor.GetChild(i).gameObject);

        GameObject mixamoModel = AssetDatabase.LoadAssetAtPath<GameObject>(MixamoModelPath);
        GameObject visualRoot = new GameObject("CharacterVisual");
        Transform visualRootTransform = visualRoot.transform;
        visualRootTransform.SetParent(visualAnchor, false);
        visualRootTransform.localPosition = Vector3.zero;
        visualRootTransform.localRotation = Quaternion.identity;
        visualRootTransform.localScale = Vector3.one;

        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(mixamoModel);
        modelInstance.transform.SetParent(visualRootTransform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        return visualRoot;
    }

    private static Animator EnsureAnimator(GameObject visualInstance, RuntimeAnimatorController controller)
    {
        Animator animator = visualInstance.GetComponent<Animator>();
        if (animator == null)
            animator = visualInstance.AddComponent<Animator>();

        Animator sourceAnimator = visualInstance.GetComponentInChildren<Animator>(true);
        if (sourceAnimator != null && sourceAnimator != animator)
        {
            animator.avatar = sourceAnimator.avatar;
            sourceAnimator.runtimeAnimatorController = null;
            sourceAnimator.enabled = false;
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        return animator;
    }

    private static RigBuilder EnsureRigBuilder(GameObject visualInstance)
    {
        RigBuilder rigBuilder = visualInstance.GetComponent<RigBuilder>();
        if (rigBuilder == null)
            rigBuilder = visualInstance.AddComponent<RigBuilder>();

        return rigBuilder;
    }

    private static PlayerAnimator EnsurePlayerAnimator(GameObject playerPrefabRoot, Animator animator)
    {
        PlayerAnimator playerAnimator = playerPrefabRoot.GetComponent<PlayerAnimator>();
        if (playerAnimator == null)
            playerAnimator = playerPrefabRoot.AddComponent<PlayerAnimator>();

        SerializedObject so = new SerializedObject(playerAnimator);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("playerController").objectReferenceValue = playerPrefabRoot.GetComponent<PlayerController>();
        so.FindProperty("playerMovement").objectReferenceValue = playerPrefabRoot.GetComponent<PlayerMovement>();
        so.FindProperty("playerState").objectReferenceValue = playerPrefabRoot.GetComponent<PlayerState>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return playerAnimator;
    }

    private static void UpdatePlayerRefs(GameObject playerPrefabRoot, PlayerAnimator playerAnimator)
    {
        PlayerRefs refs = playerPrefabRoot.GetComponent<PlayerRefs>();
        if (refs == null)
            return;

        SerializedObject so = new SerializedObject(refs);
        SerializedProperty playerAnimatorProp = so.FindProperty("playerAnimator");
        if (playerAnimatorProp != null)
        {
            playerAnimatorProp.objectReferenceValue = playerAnimator;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void EnsureDirectoryExists(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}

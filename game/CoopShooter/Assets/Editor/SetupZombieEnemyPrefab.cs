using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode.Components;

public static class SetupZombieEnemyPrefab
{
    private const string ControllerDirectory = "Assets/Animations/Enemy";
    private const string ControllerPath = ControllerDirectory + "/ZombieEnemy.controller";
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
    private const string ZombieVisualPrefabPath = "Assets/Zombie/Prefabs/Zombie1.prefab";
    private const string ZombieIdlePath = "Assets/Zombie/Animations/Z_Idle.anim";
    private const string ZombieWalkPath = "Assets/Zombie/Animations/Z_Walk_InPlace.anim";
    private const string ZombieRunPath = "Assets/Zombie/Animations/Z_Run_InPlace.anim";
    private const string ZombieAttackPath = "Assets/Zombie/Animations/Z_Attack.anim";
    private const string ZombieDeathPath = "Assets/Zombie/Animations/Z_FallingForward.anim";

    [MenuItem("Tools/Project Z/Setup Base Zombie Enemy Prefab")]
    public static void RunSetup()
    {
        EnsureDirectoryExists(ControllerDirectory);
        AnimatorController controller = CreateOrReplaceController();
        ConfigureEnemyPrefab(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Base zombie enemy prefab configured.");
    }

    private static AnimatorController CreateOrReplaceController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        sm.states = new ChildAnimatorState[0];
        sm.anyStateTransitions = new AnimatorStateTransition[0];

        BlendTree locomotionBlend;
        AnimatorState locomotion = sm.AddState("Locomotion");
        locomotion.motion = CreateLocomotionBlendTree(controller, out locomotionBlend);
        locomotion.writeDefaultValues = true;
        sm.defaultState = locomotion;

        AnimatorState attack = sm.AddState("Attack");
        attack.motion = LoadClip(ZombieAttackPath);
        attack.writeDefaultValues = true;

        AnimatorState dead = sm.AddState("Dead");
        dead.motion = LoadClip(ZombieDeathPath);
        dead.writeDefaultValues = true;

        AnimatorStateTransition attackTransition = locomotion.AddTransition(attack);
        attackTransition.hasExitTime = false;
        attackTransition.duration = 0.06f;
        attackTransition.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition returnFromAttack = attack.AddTransition(locomotion);
        returnFromAttack.hasExitTime = true;
        returnFromAttack.exitTime = 0.9f;
        returnFromAttack.duration = 0.08f;

        AddDeadTransition(sm, locomotion, dead);
        AddDeadTransition(sm, attack, dead);

        return controller;
    }

    private static Motion CreateLocomotionBlendTree(AnimatorController controller, out BlendTree blendTree)
    {
        blendTree = new BlendTree
        {
            name = "ZombieLocomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "MoveSpeed",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        blendTree.AddChild(LoadClip(ZombieIdlePath), 0f);
        blendTree.AddChild(LoadClip(ZombieWalkPath), 0.45f);
        blendTree.AddChild(LoadClip(ZombieRunPath), 1f);
        return blendTree;
    }

    private static void AddDeadTransition(AnimatorStateMachine sm, AnimatorState from, AnimatorState dead)
    {
        AnimatorStateTransition transition = from.AddTransition(dead);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
    }

    private static void ConfigureEnemyPrefab(AnimatorController controller)
    {
        GameObject enemyRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

        try
        {
            Transform root = enemyRoot.transform;
            EnsureHiddenDebugCapsule(root);

            Transform visual = root.Find("EnemyVisual");
            if (visual != null)
                Object.DestroyImmediate(visual.gameObject);

            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombieVisualPrefabPath);
            GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualPrefab) as GameObject;
            visualInstance.name = "EnemyVisual";
            visualInstance.transform.SetParent(root, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;

            Animator animator = visualInstance.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            NetworkAnimator networkAnimator = enemyRoot.GetComponent<NetworkAnimator>();
            if (networkAnimator == null)
                networkAnimator = enemyRoot.AddComponent<NetworkAnimator>();

            SerializedObject networkAnimatorSo = new SerializedObject(networkAnimator);
            networkAnimatorSo.FindProperty("m_Animator").objectReferenceValue = animator;
            networkAnimatorSo.ApplyModifiedPropertiesWithoutUndo();

            EnemyAnimatorDriver driver = enemyRoot.GetComponent<EnemyAnimatorDriver>();
            if (driver == null)
                driver = enemyRoot.AddComponent<EnemyAnimatorDriver>();

            EnemyAI enemyAI = enemyRoot.GetComponent<EnemyAI>();
            NavMeshAgent agent = enemyRoot.GetComponent<NavMeshAgent>();
            Health health = enemyRoot.GetComponent<Health>();

            AssignObjectReference(driver, "animator", animator);
            AssignObjectReference(driver, "networkAnimator", networkAnimator);
            AssignObjectReference(driver, "agent", agent);
            AssignObjectReference(driver, "health", health);

            if (enemyAI != null)
                AssignObjectReference(enemyAI, "enemyAnimator", driver);

            PrefabUtility.SaveAsPrefabAsset(enemyRoot, EnemyPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(enemyRoot);
        }
    }

    private static void EnsureHiddenDebugCapsule(Transform root)
    {
        Transform capsule = root.Find("Capsule");
        if (capsule == null)
            return;

        MeshRenderer renderer = capsule.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private static void AssignObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    }

    private static void EnsureDirectoryExists(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = parts[i];
            string combined = current + "/" + next;
            if (!AssetDatabase.IsValidFolder(combined))
                AssetDatabase.CreateFolder(current, next);
            current = combined;
        }
    }
}

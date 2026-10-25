using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CanyonClimber.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/CanyonPrototype.unity";
        const string PipelinePath = "Assets/Settings/CanyonURP.asset";
        const string Marker = "Library/CanyonClimberSetup-v1";
        static ProjectSetup() { EditorApplication.delayCall += FirstImport; }
        static void FirstImport()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += FirstImport; return; }
            if (File.Exists(Marker)) return;
            try
            {
                RepairMeshes();
                Configure();
                if (!Application.isBatchMode && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                    EditorSceneManager.OpenScene(ScenePath);
                File.WriteAllText(Marker,"Original assets checked; project settings applied.");
                Debug.Log("Canyon Climber is ready. Open CanyonPrototype and press Play. Use a 9:16 Game view.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }
        [MenuItem("Canyon Climber/Open prototype scene")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Canyon Climber/Apply mobile and URP settings")]
        public static void Configure()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer,"Assets/Settings/RecoveredRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline,"Assets/Settings/RecoveredURP.asset");
            }
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.useSRPBatcher = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            PlayerSettings.companyName = "JavedRehman";
            PlayerSettings.productName = "Canyon Climber Prototype";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.javedrehman.canyonclimber.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[] { GraphicsDeviceType.OpenGLES3 });
            // This prototype intentionally uses the built-in touch/mouse Input API.
            var playerSettings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            var handler = playerSettings.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != 0) { handler.intValue = 0; playerSettings.ApplyModifiedPropertiesWithoutUndo(); }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
        }
        [Serializable] sealed class Catalog { public MeshEntry[] meshes; }
        [Serializable] sealed class MeshEntry { public string name; public float[] positions; public float[] normalData; public int[] triangles; }
        [MenuItem("Canyon Climber/Check or repair baked meshes")]
        public static void RepairMeshes()
        {
            var source = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Meshes/MeshCatalog.json");
            if (!source) throw new InvalidOperationException("MeshCatalog.json is missing.");
            var catalog = JsonUtility.FromJson<Catalog>(source.text);
            foreach (var entry in catalog.meshes)
            {
                string path = "Assets/Art/Meshes/" + entry.name + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                int count = entry.positions.Length / 3;
                if (asset != null && asset.vertexCount == count && asset.normals.Length == count && asset.triangles.Length == entry.triangles.Length) continue;
                var mesh = new Mesh { name = entry.name };
                var vertices = new Vector3[count]; var normals = new Vector3[count];
                for (int i=0; i<count; i++)
                {
                    vertices[i] = new Vector3(entry.positions[i*3],entry.positions[i*3+1],entry.positions[i*3+2]);
                    normals[i] = new Vector3(entry.normalData[i*3],entry.normalData[i*3+1],entry.normalData[i*3+2]);
                }
                mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = entry.triangles; mesh.RecalculateBounds();
                if (asset == null)
                {
                    // Retain the shipped .meta GUID if a native mesh needs recovery.
                    if (File.Exists(path)) File.Delete(path);
                    AssetDatabase.CreateAsset(mesh,path);
                }
                else { EditorUtility.CopySerialized(mesh,asset); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(asset); }
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Canyon Climber/Validate scene and assets")]
        public static void ValidateProject()
        {
            RepairMeshes();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool temporary = scene.path != ScenePath;
            if (temporary) scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var motor = roots.SelectMany(r=>r.GetComponentsInChildren<ClimbMotor>(true)).Single();
                if (motor.settings == null || motor.startHold == null || motor.holds.Length != 40) throw new Exception("Incomplete route references.");
                foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script on " + t.name);
                    var mf=t.GetComponent<MeshFilter>();
                    if (mf != null && (mf.sharedMesh == null || mf.sharedMesh.vertexCount==0)) throw new Exception("Missing mesh on " + t.name);
                    var renderer=t.GetComponent<MeshRenderer>();
                    if (renderer != null && (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null || !renderer.sharedMaterial.shader.isSupported)) throw new Exception("Material/shader problem on " + t.name);
                }
                if (GraphicsSettings.defaultRenderPipeline == null) throw new Exception("URP pipeline is unassigned.");
                Debug.Log("Canyon Climber validation passed: 40 holds, meshes, scripts, materials, shader support and URP checked.");
            }
            finally { if (temporary) EditorSceneManager.CloseScene(scene,true); }
        }
        // Usage: Unity -batchmode -projectPath <folder> -executeMethod CanyonClimber.Editor.ProjectSetup.ValidateBatch -logFile validate.log
        public static void ValidateBatch()
        {
            try { Configure(); ValidateProject(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        [MenuItem("Canyon Climber/Build Android APK")]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
            { EditorUtility.DisplayDialog("Android module required","Install Android Build Support, SDK/NDK and OpenJDK for Unity 6000.3.8f1 in Unity Hub.","OK"); return; }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android);
                Debug.Log("Switching to Android. After Unity finishes importing, run Build Android APK again.");
                return;
            }
            string path=EditorUtility.SaveFilePanel("Build Android prototype","","CanyonClimber-Prototype.apk","apk");
            if (string.IsNullOrEmpty(path)) return;
            Configure(); ValidateProject();
            EditorUserBuildSettings.buildAppBundle=false;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName=path,target=BuildTarget.Android,options=BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed. Check Console/build report.");
            Debug.Log("Android APK built: " + path);
        }
    }
}

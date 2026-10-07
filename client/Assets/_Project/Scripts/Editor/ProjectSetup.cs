using System.IO;
using System.Linq;
using BattleHunter.Client;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleHunter.Client.Editor
{
    /// <summary>
    /// Configuração reproduzível do projeto: modo 2D, retrato, nomes e as 5 cenas do GDD no Build Settings.
    /// Roda pelo menu ou em batchmode: Unity -executeMethod BattleHunter.Client.Editor.ProjectSetup.Configure
    /// </summary>
    public static class ProjectSetup
    {
        public static readonly string[] SceneNames = { "Boot", "Guilda", "Lobby", "Partida", "Resultado" };
        private const string ScenesFolder = "Assets/_Project/Scenes";

        [MenuItem("Battle Hunter/Configurar projeto")]
        public static void Configure()
        {
            PlayerSettings.companyName = "Battle Hunter";
            PlayerSettings.productName = "Battle Hunter";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "br.com.battlehunter.game");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "br.com.battlehunter.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.runInBackground = true;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            CreateScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("ProjectSetup: projeto configurado (2D, retrato, 5 cenas).");
        }

        [MenuItem("Battle Hunter/Criar cenas")]
        public static void CreateScenes()
        {
            Directory.CreateDirectory(ScenesFolder);

            foreach (var name in SceneNames)
            {
                var path = $"{ScenesFolder}/{name}.unity";
                if (File.Exists(path))
                    continue;

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera";
                var cam = camera.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.08f, 0.07f, 0.10f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                var root = new GameObject(name + "Root");
                var bootstrap = root.AddComponent<SceneBootstrap>();
                bootstrap.sceneName = name;

                EditorSceneManager.SaveScene(scene, path);
            }

            EditorBuildSettings.scenes = SceneNames
                .Select(n => new EditorBuildSettingsScene($"{ScenesFolder}/{n}.unity", true))
                .ToArray();

            AssetDatabase.Refresh();
        }
    }
}

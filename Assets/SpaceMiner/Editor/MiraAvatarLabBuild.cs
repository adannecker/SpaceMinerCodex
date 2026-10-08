using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMiner.Editor
{
    public static class MiraAvatarLabBuild
    {
        [MenuItem("Space Miner/Mira/Avatar-Testszene öffnen")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/SpaceMiner/AvatarLab/MiraAvatarLab.unity");
        }

        public static void Build()
        {
            const string texturePath = "Assets/SpaceMiner/AvatarLab/MiraExpressions.png";
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.SaveAndReimport();
            const string portraitPath = "Assets/SpaceMiner/AvatarLab/MiraDialoguePortraits.png";
            var portraits = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
            portraits.textureType = TextureImporterType.Default;
            portraits.alphaIsTransparency = true; portraits.mipmapEnabled = false;
            portraits.textureCompression = TextureImporterCompression.Uncompressed;
            portraits.maxTextureSize = 2048; portraits.SaveAndReimport();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Avatar preview camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f,.045f,.075f);
            var actor = new GameObject("Mira avatar").AddComponent<MiraAvatar>();
            actor.Expressions = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            actor.DialoguePortraits = AssetDatabase.LoadAssetAtPath<Texture2D>(portraitPath);
            actor.Motion = false;
            var speech = actor.gameObject.AddComponent<AudioSource>(); speech.playOnAwake = false;
            var preview = actor.gameObject.AddComponent<MiraDialoguePreview>();
            preview.Avatar = actor; preview.Speech = speech;
            const string scenePath = "Assets/SpaceMiner/AvatarLab/MiraAvatarLab.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scenePath }, locationPathName = "Builds/MiraAvatarLab/MiraAvatarLab.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (result.summary.result != BuildResult.Succeeded) throw new System.Exception("Mira preview build failed");
        }
    }
}

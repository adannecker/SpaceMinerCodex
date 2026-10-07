using UnityEditor;
using UnityEngine;
namespace SpaceMiner.Editor
{
    public static class MemoryCinematicBuild
    {
        public static void Run()
        {
            for(int i=1;i<=MemoryCinematic.SceneCount;i++)
            {
                string path="Assets/SpaceMiner/Resources/MemoryCinematic/scene"+i+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;
                importer.textureShape=TextureImporterShape.Texture2D;
                importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                var audio=(AudioImporter)AssetImporter.GetAtPath("Assets/SpaceMiner/Resources/MemoryCinematic/voice"+i+".wav");
                var settings=audio.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.preloadAudioData=true;
                audio.defaultSampleSettings=settings;audio.loadInBackground=false;
                audio.SaveAndReimport();
                if(Resources.Load<Texture2D>("MemoryCinematic/scene"+i)==null || Resources.Load<AudioClip>("MemoryCinematic/voice"+i)==null)throw new System.Exception("Cinematic import failed "+i);
            }
            foreach(string name in new[]{"narration","atmosphere"})
            {
                var audio=(AudioImporter)AssetImporter.GetAtPath("Assets/SpaceMiner/Resources/MemoryCinematic/"+name+".wav");
                var settings=audio.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;settings.preloadAudioData=true;
                audio.defaultSampleSettings=settings;audio.loadInBackground=false;audio.SaveAndReimport();
            }
            StartMenuBuild.Run();
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 아트 팀 UI 그림(Assets/Resources/UI): 스프라이트로, 압축·밉맵 없이. 늘려 쓰는 틀은 9분할 테두리 지정.
/// </summary>
public class UiArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith("Assets/Resources/UI/")) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.filterMode = FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.maxTextureSize = 2048;
        imp.spritePixelsPerUnit = 100f;
        var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteBorder = Border(Path.GetFileNameWithoutExtension(assetPath));
        imp.SetTextureSettings(s);
    }

    static Vector4 Border(string n)
    {
        switch (n)
        {
            case "button_gray": case "button_red": return new Vector4(60, 60, 60, 60);
            case "item": case "item_2": case "lock": return new Vector4(40, 40, 40, 40);
            case "back_failed": return new Vector4(80, 80, 80, 80);
            case "line_scroll": case "back_scroll": return new Vector4(12, 12, 12, 12);
            case "scroll_handle": return new Vector4(20, 20, 20, 20);
            case "class": case "class_2": case "language": case "language_2": return new Vector4(24, 12, 24, 12);
            default: return Vector4.zero;
        }
    }
}

/// <summary>브금(Assets/Resources/Bgm): 긴 곡이라 스트리밍 재생 (메모리에 통째로 풀지 않음)</summary>
public class BgmImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').StartsWith("Assets/Resources/Bgm/")) return;
        var imp = (AudioImporter)assetImporter;
        var s = imp.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming;
        imp.defaultSampleSettings = s;
    }
}

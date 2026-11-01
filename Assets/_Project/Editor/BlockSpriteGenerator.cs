
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BlockSpriteGenerator
{
    private const string OutputFolder =
        "Assets/_Project/Art/Blocks";

    [MenuItem("Tools/Block Puzzle/Generate Rounded Block Sprite")]
    public static void Generate()
    {
        Directory.CreateDirectory(OutputFolder);

        const int size = 64;
        const float radius = 11f;

        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - size * 0.5f;
                float py = y + 0.5f - size * 0.5f;

                Vector2 halfSize =
                    new Vector2(size * 0.5f, size * 0.5f);

                Vector2 q = new Vector2(
                    Mathf.Abs(px),
                    Mathf.Abs(py)
                ) - (halfSize - Vector2.one * radius);

                float distance =
                    Vector2.Max(q, Vector2.zero).magnitude +
                    Mathf.Min(Mathf.Max(q.x, q.y), 0f) -
                    radius;

                float alpha = Mathf.Clamp01(
                    0.5f - distance
                );

                float highlight =
                    Mathf.Lerp(
                        0.88f,
                        1f,
                        (float)y / (size - 1)
                    );

                Color color = new Color(
                    highlight,
                    highlight,
                    highlight,
                    alpha
                );

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        string path =
            OutputFolder + "/RoundedBlock.png";

        File.WriteAllBytes(path, texture.EncodeToPNG());

        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);

        TextureImporter importer =
            AssetImporter.GetAtPath(path)
            as TextureImporter;

        if (importer == null)
            return;

        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Single;

        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;

        importer.spriteBorder =
            new Vector4(12f, 12f, 12f, 12f);

        importer.SaveAndReimport();

        Debug.Log(
            "[BlockSpriteGenerator] RoundedBlock generated."
        );
    }
}
#endif

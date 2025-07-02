using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
// Copyright (c) 2025 Dongwon Lee. All rights reserved.

public class SpriteSheetJsonExporter
{
    [System.Serializable]
    public class SpriteRect
    {
        public string name;
        public float x;
        public float y;
        public float width;
        public float height;
        public float pivotX;
        public float pivotY;
    }

    [System.Serializable]
    public class SpriteList
    {
        public string texture;
        public int textureWidth;
        public int textureHeight;
        public List<SpriteRect> sprites = new List<SpriteRect>();
    }

    private const string SavePathKey = "SpriteJson_LastSavePath";

    // �� ��Ŭ�� �޴� ���: Project �信�� ���� ����
    [MenuItem("Assets/SpriteTool/Export Sprite Sheet to JSON", false, 2000)]
    public static void Export()
    {
        Object selected = Selection.activeObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("����", "Sprite ������ �����ϼ���.", "Ȯ��");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("����", "Assets ���� ������ ���¸� �����˴ϴ�.", "Ȯ��");
            return;
        }

        if (!(selected is Sprite) && !(selected is Texture2D))
        {
            EditorUtility.DisplayDialog("����", "���õ� ������ Sprite �Ǵ� Sprite Sheet �ؽ�ó�� �ƴմϴ�.", "Ȯ��");
            return;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<Sprite> spriteList = new List<Sprite>();

        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                spriteList.Add(sprite);
            }
        }

        if (spriteList.Count == 0)
        {
            EditorUtility.DisplayDialog("���", "Sprite�� ���ԵǾ� ���� �ʽ��ϴ�.\nSprite Mode�� Multiple�̰� Slice�� ����Ǿ����� Ȯ���ϼ���.", "Ȯ��");
            return;
        }

        // �ؽ�ó �̸� �� ũ��
        string textureName = Path.GetFileName(assetPath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        int texWidth = texture != null ? texture.width : 0;
        int texHeight = texture != null ? texture.height : 0;

        SpriteList spritelist = new SpriteList
        {
            texture = textureName,
            textureWidth = texWidth,
            textureHeight = texHeight
        };

        foreach (var sprite in spriteList)
        {
            Rect rect = sprite.rect;
            spritelist.sprites.Add(new SpriteRect
            {
                name = sprite.name,
                x = rect.x,
                y = rect.y,
                width = rect.width,
                height = rect.height,
                pivotX = sprite.pivot.x / rect.width,
                pivotY = sprite.pivot.y / rect.height
            });
        }

        string defaultFileName = Path.GetFileNameWithoutExtension(assetPath);
        string defaultSavePath = EditorPrefs.GetString(SavePathKey, Application.dataPath);
        string savePath = EditorUtility.SaveFilePanel("JSON ���Ϸ� ����", defaultSavePath, defaultFileName + "_sprites", "json");

        if (string.IsNullOrEmpty(savePath))
        {
            EditorUtility.DisplayDialog("��ҵ�", "������ ��ҵǾ����ϴ�.", "Ȯ��");
            return;
        }

        EditorPrefs.SetString(SavePathKey, Path.GetDirectoryName(savePath));

        string json = JsonUtility.ToJson(spritelist, true);
        File.WriteAllText(savePath, json);

        EditorUtility.DisplayDialog("����", $"JSON ���� �Ϸ�:\n{savePath}", "Ȯ��");
    }

    // �� ���Ǻ� �޴� ǥ��: Texture2D �Ǵ� Sprite�� ����
    [MenuItem("Assets/SpriteTool/Export Sprite Sheet to JSON", true)]
    public static bool Validate()
    {
        Object selected = Selection.activeObject;
        return selected is Texture2D || selected is Sprite;
    }
}

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

    // ▶ 우클릭 메뉴 등록: Project 뷰에서 실행 가능
    [MenuItem("Assets/SpriteTool/Export Sprite Sheet to JSON", false, 2000)]
    public static void Export()
    {
        Object selected = Selection.activeObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("오류", "Sprite 에셋을 선택하세요.", "확인");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("오류", "Assets 폴더 내부의 에셋만 지원됩니다.", "확인");
            return;
        }

        if (!(selected is Sprite) && !(selected is Texture2D))
        {
            EditorUtility.DisplayDialog("오류", "선택된 에셋은 Sprite 또는 Sprite Sheet 텍스처가 아닙니다.", "확인");
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
            EditorUtility.DisplayDialog("경고", "Sprite가 포함되어 있지 않습니다.\nSprite Mode가 Multiple이고 Slice가 적용되었는지 확인하세요.", "확인");
            return;
        }

        // 텍스처 이름 및 크기
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
        string savePath = EditorUtility.SaveFilePanel("JSON 파일로 저장", defaultSavePath, defaultFileName + "_sprites", "json");

        if (string.IsNullOrEmpty(savePath))
        {
            EditorUtility.DisplayDialog("취소됨", "저장이 취소되었습니다.", "확인");
            return;
        }

        EditorPrefs.SetString(SavePathKey, Path.GetDirectoryName(savePath));

        string json = JsonUtility.ToJson(spritelist, true);
        File.WriteAllText(savePath, json);

        EditorUtility.DisplayDialog("성공", $"JSON 저장 완료:\n{savePath}", "확인");
    }

    // ▶ 조건부 메뉴 표시: Texture2D 또는 Sprite일 때만
    [MenuItem("Assets/SpriteTool/Export Sprite Sheet to JSON", true)]
    public static bool Validate()
    {
        Object selected = Selection.activeObject;
        return selected is Texture2D || selected is Sprite;
    }
}

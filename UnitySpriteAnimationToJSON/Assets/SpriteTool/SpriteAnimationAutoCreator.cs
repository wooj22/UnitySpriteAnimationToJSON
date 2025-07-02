// Copyright (c) 2025 Dongwon Lee. All rights reserved.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SpriteAnimationAutoCreator
{
    private const string LastUsedFolderKey = "SpriteAnim_LastFolder";

    [MenuItem("Assets/SpriteTool/Create AnimationClips from Sprites", false, 2001)]
    public static void CreateClips()
    {
        EditorUtility.DisplayDialog("프레임 이름 규칙 안내",
            "스프라이트 이름은 '이름_번호' 형식을 따라야 하며, 그룹을 형성하려면 언더바(_)가 최소 2개 이상 포함되어야 합니다.\n예: Walk_Left_0, Walk_Left_1, Jump_Up_0 ...\n\n같은 접두사를 가진 스프라이트끼리 하나의 애니메이션 클립으로 묶입니다.",
            "확인");

        Object obj = Selection.activeObject;

        if (!(obj is Texture2D))
        {
            EditorUtility.DisplayDialog("오류", "선택한 객체가 Texture2D가 아닙니다.", "확인");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("오류", "Assets 폴더 내부의 파일만 지원됩니다.", "확인");
            return;
        }

        string defaultFolder = EditorPrefs.GetString(LastUsedFolderKey, Application.dataPath);
        string absoluteFolder = EditorUtility.OpenFolderPanel("애니메이션 저장 폴더 선택", defaultFolder, "");
        if (string.IsNullOrEmpty(absoluteFolder))
            return;

        EditorPrefs.SetString(LastUsedFolderKey, absoluteFolder);

        string projectPath = Application.dataPath;
        if (!absoluteFolder.StartsWith(projectPath))
        {
            EditorUtility.DisplayDialog("오류", "Assets 폴더 내부만 선택할 수 있습니다.", "확인");
            return;
        }
        string relativeFolder = "Assets" + absoluteFolder.Substring(projectPath.Length);

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sprites = allAssets.OfType<Sprite>().ToList();

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("경고", "해당 텍스처에 Sprite가 없습니다. Sprite Mode: Multiple 설정 및 Slice 필요.", "확인");
            return;
        }

        var grouped = sprites.GroupBy(s =>
        {
            string name = s.name;
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore < 0) return name;

            string prefix = name.Substring(0, lastUnderscore);
            int underscoreCount = prefix.Count(c => c == '_');
            return underscoreCount >= 1 ? prefix : "";
        }).Where(g => !string.IsNullOrEmpty(g.Key));

        int createdCount = 0;
        int skippedCount = 0;
        List<string> createdNames = new List<string>();
        List<string> skippedNames = new List<string>();

        foreach (var group in grouped)
        {
            var orderedSprites = group.OrderBy(s =>
            {
                string[] parts = s.name.Split('_');
                return int.TryParse(parts.Last(), out int index) ? index : -1;
            }).ToList();

            bool hasInvalid = orderedSprites.Any(s =>
            {
                string[] parts = s.name.Split('_');
                return !int.TryParse(parts.Last(), out _);
            });

            if (hasInvalid)
            {
                skippedCount++;
                skippedNames.Add(group.Key);
                EditorUtility.DisplayDialog("경고", $"'{group.Key}' 그룹의 일부 스프라이트 이름에 숫자가 없어 애니메이션 생성을 건너뜁니다.", "확인");
                continue;
            }

            float frameRate = 12f;
            float timePerFrame = 1f / frameRate;

            var keyframes = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < orderedSprites.Count; i++)
            {
                keyframes.Add(new ObjectReferenceKeyframe
                {
                    time = i * timePerFrame,
                    value = orderedSprites[i]
                });
            }

            string clipPath = Path.Combine(relativeFolder, $"{group.Key}.anim");
            if (File.Exists(clipPath))
            {
                skippedCount++;
                skippedNames.Add(group.Key);
                continue;
            }

            var clip = new AnimationClip();
            clip.frameRate = frameRate;

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());

            AssetDatabase.CreateAsset(clip, clipPath);
            createdCount++;
            createdNames.Add(group.Key);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string createdList = createdNames.Count > 0 ? string.Join("\n", createdNames) : "(없음)";
        string skippedList = skippedNames.Count > 0 ? string.Join("\n", skippedNames) : "(없음)";

        EditorUtility.DisplayDialog("완료",
            $"애니메이션 생성 완료\n\n생성됨: {createdCount}개\n건너뜀: {skippedCount}개\n\n[생성된 그룹]\n{createdList}\n\n[건너뛴 그룹]\n{skippedList}",
            "확인");
    }

    // 우클릭 메뉴 활성화 조건 (Texture2D만 허용)
    [MenuItem("Assets/SpriteTool/Create AnimationClips from Sprites", true)]
    public static bool ValidateCreateClips()
    {
        return Selection.activeObject is Texture2D;
    }
}

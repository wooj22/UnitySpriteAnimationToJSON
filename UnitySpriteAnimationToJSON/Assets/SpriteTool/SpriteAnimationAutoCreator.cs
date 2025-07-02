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
        EditorUtility.DisplayDialog("������ �̸� ��Ģ �ȳ�",
            "��������Ʈ �̸��� '�̸�_��ȣ' ������ ����� �ϸ�, �׷��� �����Ϸ��� �����(_)�� �ּ� 2�� �̻� ���ԵǾ�� �մϴ�.\n��: Walk_Left_0, Walk_Left_1, Jump_Up_0 ...\n\n���� ���λ縦 ���� ��������Ʈ���� �ϳ��� �ִϸ��̼� Ŭ������ ���Դϴ�.",
            "Ȯ��");

        Object obj = Selection.activeObject;

        if (!(obj is Texture2D))
        {
            EditorUtility.DisplayDialog("����", "������ ��ü�� Texture2D�� �ƴմϴ�.", "Ȯ��");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("����", "Assets ���� ������ ���ϸ� �����˴ϴ�.", "Ȯ��");
            return;
        }

        string defaultFolder = EditorPrefs.GetString(LastUsedFolderKey, Application.dataPath);
        string absoluteFolder = EditorUtility.OpenFolderPanel("�ִϸ��̼� ���� ���� ����", defaultFolder, "");
        if (string.IsNullOrEmpty(absoluteFolder))
            return;

        EditorPrefs.SetString(LastUsedFolderKey, absoluteFolder);

        string projectPath = Application.dataPath;
        if (!absoluteFolder.StartsWith(projectPath))
        {
            EditorUtility.DisplayDialog("����", "Assets ���� ���θ� ������ �� �ֽ��ϴ�.", "Ȯ��");
            return;
        }
        string relativeFolder = "Assets" + absoluteFolder.Substring(projectPath.Length);

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sprites = allAssets.OfType<Sprite>().ToList();

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("���", "�ش� �ؽ�ó�� Sprite�� �����ϴ�. Sprite Mode: Multiple ���� �� Slice �ʿ�.", "Ȯ��");
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
                EditorUtility.DisplayDialog("���", $"'{group.Key}' �׷��� �Ϻ� ��������Ʈ �̸��� ���ڰ� ���� �ִϸ��̼� ������ �ǳʶݴϴ�.", "Ȯ��");
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

        string createdList = createdNames.Count > 0 ? string.Join("\n", createdNames) : "(����)";
        string skippedList = skippedNames.Count > 0 ? string.Join("\n", skippedNames) : "(����)";

        EditorUtility.DisplayDialog("�Ϸ�",
            $"�ִϸ��̼� ���� �Ϸ�\n\n������: {createdCount}��\n�ǳʶ�: {skippedCount}��\n\n[������ �׷�]\n{createdList}\n\n[�ǳʶ� �׷�]\n{skippedList}",
            "Ȯ��");
    }

    // ��Ŭ�� �޴� Ȱ��ȭ ���� (Texture2D�� ���)
    [MenuItem("Assets/SpriteTool/Create AnimationClips from Sprites", true)]
    public static bool ValidateCreateClips()
    {
        return Selection.activeObject is Texture2D;
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
// Copyright (c) 2025 Dongwon Lee. All rights reserved.

using System.IO;

public class AnimationClipToJson
{
    [System.Serializable]
    public class FrameInfo
    {
        public string sprite;
        public float time;
    }

    [System.Serializable]
    public class EventInfo
    {
        public string function;
        public string parameter;
        public float time;
    }

    [System.Serializable]
    public class ClipData
    {
        public string clipName;
        public bool loop;
        public string texturePath;
        public float duration;
        public List<FrameInfo> frames = new List<FrameInfo>();
        public List<EventInfo> events = new List<EventInfo>();
    }

    private const string EditorPrefsKey = "LastJsonExportPath";

    // ▶ 우클릭 메뉴 등록
    [MenuItem("Assets/SpriteTool/Export AnimationClip to JSON", false, 2000)]
    static void Export()
    {
        var clip = Selection.activeObject as AnimationClip;
        if (clip == null)
        {
            EditorUtility.DisplayDialog("경고", "AnimationClip을 선택하세요.", "확인");
            return;
        }

        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (binding.propertyName != "m_Sprite" && binding.propertyName != "sprite")
                continue;

            var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keyframes.Length == 0)
            {
                EditorUtility.DisplayDialog("경고", "Sprite 키프레임이 없습니다.", "확인");
                return;
            }

            string defaultFolder = EditorPrefs.GetString(EditorPrefsKey, Application.dataPath);
            string defaultFileName = clip.name + "_AniClip.json";
            string savePath = EditorUtility.SaveFilePanel("JSON 파일로 저장", defaultFolder, defaultFileName, "json");

            if (string.IsNullOrEmpty(savePath))
            {
                EditorUtility.DisplayDialog("취소됨", "저장이 취소되었습니다.", "확인");
                return;
            }

            EditorPrefs.SetString(EditorPrefsKey, Path.GetDirectoryName(savePath));

            ClipData data = new ClipData
            {
                clipName = clip.name,
                duration = clip.length
            };

            Sprite firstSprite = keyframes[0].value as Sprite;
            if (firstSprite != null)
            {
                Texture2D texture = firstSprite.texture;
                data.texturePath = AssetDatabase.GetAssetPath(texture);
            }

            foreach (var kf in keyframes)
            {
                Sprite sprite = kf.value as Sprite;
                if (sprite == null) continue;

                data.frames.Add(new FrameInfo
                {
                    sprite = sprite.name,
                    time = kf.time
                });
            }

            // 이벤트 정보 추가
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            foreach (var evt in events)
            {
                data.events.Add(new EventInfo
                {
                    function = evt.functionName,
                    parameter = evt.stringParameter,
                    time = evt.time
                });
            }

            var so = new SerializedObject(clip);
            var settings = so.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                data.loop = settings.FindPropertyRelative("m_LoopTime").boolValue;
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("성공", $"JSON 저장 완료:\n{savePath}", "확인");
            return;
        }

        EditorUtility.DisplayDialog("경고", "Sprite 애니메이션이 아닙니다.", "확인");
    }

    // ▶ 메뉴 표시 조건
    [MenuItem("Assets/SpriteTool/Export AnimationClip to JSON", true)]
    static bool ValidateExport()
    {
        return Selection.activeObject is AnimationClip;
    }
}

// Copyright (c) 2025 Dongwon Lee. All rights reserved.

using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Linq;
using System;

public class AnimationClipPreviewWindow : EditorWindow
{
    public AnimationClip clip;
    public AnimationClip selectedClipFromField;
    public float currentTime;
    public float playbackSpeed = 1.0f;
    public PlayState playState = PlayState.Playing;
    public int scaleFactor = 1;
    public Color pivotColor = Color.red;
    public bool overrideLoop = false;

    private float lastTime;
    private UnityEngine.Object lastSelection;

    private int lastFrameIndex = -1;
    private float lastShownTime = -1f;
    private double nextUpdateTime = 0.0;
    private const double frameInterval = 1.0 / 60.0;

    private float displayedTime = 0f;
    private Sprite currentSprite = null;
    private bool forceRefresh = false;

    public enum PlayState { Playing, Paused, Stopped }

    [MenuItem("Assets/SpriteTool/AnimationClip Previewer", false, 2002)]
    private static void OpenFromContextMenu()
    {
        var clip = Selection.activeObject as AnimationClip;
        if (clip == null)
        {
            EditorUtility.DisplayDialog("오류", "AnimationClip을 선택하세요.", "확인");
            return;
        }

        var window = GetWindow<AnimationClipPreviewWindow>("AnimationClip Previewer");
        window.clip = clip;
        window.selectedClipFromField = clip;
        window.currentTime = 0f;
        window.playState = PlayState.Playing;
        window.lastTime = (float)EditorApplication.timeSinceStartup;
        window.lastFrameIndex = -1;
        window.lastShownTime = -1f;
        window.nextUpdateTime = EditorApplication.timeSinceStartup;
        window.forceRefresh = true;
    }

    [MenuItem("Assets/SpriteTool/AnimationClip Previewer", true)]
    private static bool ValidateContextMenu()
    {
        return Selection.activeObject is AnimationClip;
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        lastFrameIndex = -1;
        lastShownTime = -1f;
        nextUpdateTime = EditorApplication.timeSinceStartup;
        forceRefresh = true;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now < nextUpdateTime)
            return;
        nextUpdateTime = now + frameInterval;

        if (Selection.activeObject != lastSelection)
        {
            lastSelection = Selection.activeObject;
            if (lastSelection is AnimationClip newClip && newClip != clip)
            {
                clip = newClip;
                selectedClipFromField = newClip;
                currentTime = 0f;
                playState = PlayState.Playing;
                lastTime = (float)now;
                lastFrameIndex = -1;
                lastShownTime = -1f;
                forceRefresh = true;
            }
        }

        if (clip == null || playState != PlayState.Playing)
            return;

        float deltaTime = (float)(now - lastTime);
        currentTime += deltaTime * playbackSpeed;

        bool shouldLoop = overrideLoop || clip.isLooping();

        if (shouldLoop)
        {
            if (currentTime > clip.length)
                currentTime = 0f;
            displayedTime = currentTime;
        }
        else
        {
            if (currentTime >= clip.length)
            {
                currentTime = clip.length;
                displayedTime = clip.length;
                playState = PlayState.Stopped;
                forceRefresh = true;
                Repaint();
                return;
            }
            else
            {
                displayedTime = currentTime;
            }
        }

        lastTime = (float)now;

        UpdateCurrentSprite();
    }

    private void UpdateCurrentSprite()
    {
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        var spriteBinding = bindings.FirstOrDefault(b => b.propertyName == "m_Sprite" || b.propertyName == "sprite");
        if (spriteBinding.propertyName == null)
            return;

        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
        if (keyframes.Length == 0)
            return;

        int frameIndex = 0;
        Sprite newSprite = null;
        for (int i = 0; i < keyframes.Length; ++i)
        {
            if (currentTime < keyframes[i].time)
                break;
            newSprite = keyframes[i].value as Sprite;
            frameIndex = i;
        }

        bool frameChanged = (frameIndex != lastFrameIndex);
        bool timeChanged = Mathf.Abs(currentTime - lastShownTime) >= 0.05f;

        if (frameChanged || timeChanged || forceRefresh)
        {
            currentSprite = newSprite;
            Repaint();
            lastFrameIndex = frameIndex;
            lastShownTime = currentTime;
            forceRefresh = false;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Drag-Drop or Select", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        selectedClipFromField = EditorGUILayout.ObjectField("Animation Clip", selectedClipFromField, typeof(AnimationClip), false) as AnimationClip;
        if (EditorGUI.EndChangeCheck())
        {
            if (selectedClipFromField != null && selectedClipFromField != clip)
            {
                clip = selectedClipFromField;
                currentTime = 0f;
                playState = PlayState.Playing;
                lastTime = (float)EditorApplication.timeSinceStartup;
                lastFrameIndex = -1;
                lastShownTime = -1f;
                forceRefresh = true;
            }
        }

        if (clip == null)
        {
            EditorGUILayout.HelpBox("AnimationClip을 선택하세요.", MessageType.Info);
            return;
        }

        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        var spriteBinding = bindings.FirstOrDefault(b => b.propertyName == "m_Sprite" || b.propertyName == "sprite");

        if (spriteBinding.propertyName == null)
        {
            EditorGUILayout.HelpBox("Sprite 애니메이션이 아닙니다.", MessageType.Warning);
            return;
        }

        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
        if (keyframes.Length == 0)
        {
            EditorGUILayout.HelpBox("Sprite 키프레임이 없습니다.", MessageType.Warning);
            return;
        }

        if (currentSprite != null)
        {
            EditorGUILayout.ObjectField("Texture2D", currentSprite.texture, typeof(Texture2D), false);
        }

        playbackSpeed = EditorGUILayout.Slider("Playback Speed", playbackSpeed, 0.1f, 3.0f);
        scaleFactor = EditorGUILayout.IntSlider("Scale Factor", scaleFactor, 1, 10);
        pivotColor = EditorGUILayout.ColorField("Pivot Color", pivotColor);

        EditorGUI.BeginChangeCheck();
        bool newOverrideLoop = EditorGUILayout.Toggle("Loop Preview", overrideLoop);
        if (EditorGUI.EndChangeCheck())
        {
            if (newOverrideLoop != overrideLoop)
            {
                overrideLoop = newOverrideLoop;
                currentTime = 0f;
                playState = PlayState.Playing;
                lastTime = (float)EditorApplication.timeSinceStartup;
                lastFrameIndex = -1;
                lastShownTime = -1f;
                forceRefresh = true;
            }
        }

        EditorGUI.BeginChangeCheck();
        float newTime = EditorGUILayout.Slider("Timeline", currentTime, 0f, clip.length);
        if (EditorGUI.EndChangeCheck())
        {
            currentTime = newTime;
            playState = PlayState.Paused;
            lastFrameIndex = -1;
            lastShownTime = -1f;
            forceRefresh = true;
            UpdateCurrentSprite();
            Repaint();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play"))
        {
            if (playState == PlayState.Stopped)
                currentTime = 0f;
            playState = PlayState.Playing;
            lastTime = (float)EditorApplication.timeSinceStartup;
        }
        if (GUILayout.Button("Pause"))
            playState = PlayState.Paused;
        if (GUILayout.Button("Stop"))
        {
            playState = PlayState.Stopped;
            currentTime = 0f;
            lastFrameIndex = -1;
            lastShownTime = -1f;
            forceRefresh = true;
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("State:", playState.ToString());
        EditorGUILayout.LabelField("Time:", $"{displayedTime:F2} / {clip.length:F2}");
        EditorGUILayout.LabelField("Index:", $"{lastFrameIndex} / {keyframes.Length - 1}");
        EditorGUILayout.LabelField("Sprite Name:", currentSprite != null ? currentSprite.name : "(None)");

        if (currentSprite != null)
        {
            Vector2 pivot = currentSprite.pivot;
            Vector2 size = currentSprite.rect.size;

            float previewWidth = size.x * scaleFactor;
            float previewHeight = size.y * scaleFactor;
            float uiHeight = GUILayoutUtility.GetLastRect().yMax + 20f;
            uiHeight = Mathf.Max(uiHeight, EditorGUIUtility.singleLineHeight * 20);

            Vector2 pivotOffset = new Vector2(
                (pivot.x / size.x - 0.5f) * previewWidth,
                -(pivot.y / size.y - 0.5f) * previewHeight
            );

            Rect drawRect = new Rect(
                (position.width - previewWidth) / 2f - pivotOffset.x,
                uiHeight + (position.height - uiHeight - previewHeight) / 2f - pivotOffset.y,
                previewWidth,
                previewHeight
            );

            Rect texCoords = new Rect(
                currentSprite.rect.x / currentSprite.texture.width,
                currentSprite.rect.y / currentSprite.texture.height,
                currentSprite.rect.width / currentSprite.texture.width,
                currentSprite.rect.height / currentSprite.texture.height
            );

            GUI.DrawTextureWithTexCoords(drawRect, currentSprite.texture, texCoords, true);

            Vector2 origin = new Vector2(drawRect.center.x + pivotOffset.x, drawRect.center.y + pivotOffset.y);
            Handles.color = pivotColor;
            Handles.DrawLine(origin + new Vector2(-5, 0), origin + new Vector2(5, 0));
            Handles.DrawLine(origin + new Vector2(0, -5), origin + new Vector2(0, 5));
        }
    }
}

public static class AnimationClipExtensions
{
    public static bool isLooping(this AnimationClip clip)
    {
        SerializedObject so = new SerializedObject(clip);
        SerializedProperty settings = so.FindProperty("m_AnimationClipSettings");
        return settings?.FindPropertyRelative("m_LoopTime")?.boolValue ?? false;
    }
}

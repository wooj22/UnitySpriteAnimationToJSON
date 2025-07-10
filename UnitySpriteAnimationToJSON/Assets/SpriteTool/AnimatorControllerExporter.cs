using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public class AnimatorControllerExporter
{
    public static readonly string[] ConditionModes = new string[]
    {
        "If", "IfNot", "Greater", "Less", "Equals", "NotEqual", "Always"
    };

    [System.Serializable]
    public class ParameterInfo
    {
        public string name;
        public string type;
        public float defaultFloat;
        public int defaultInt;
        public bool defaultBool;
    }

    [System.Serializable]
    public class ConditionInfo
    {
        public string parameter;
        public string mode;
        public float threshold;
    }

    [System.Serializable]
    public class TransitionInfo
    {
        public string fromState;
        public string toState;
        public float exitTime;
        public bool hasExitTime;
        public List<ConditionInfo> conditions = new List<ConditionInfo>();
    }

    [System.Serializable]
    public class StateInfo
    {
        public string name;
        public string motionName;
        public float clipLength;
        public bool loop;
        public List<TransitionInfo> transitions = new List<TransitionInfo>();
    }

    [System.Serializable]
    public class SpecialTransitionInfo
    {
        public string toState;
        public List<ConditionInfo> conditions = new List<ConditionInfo>();
    }

    [System.Serializable]
    public class AnimatorControllerExport
    {
        public string controllerName;
        public List<ParameterInfo> parameters = new List<ParameterInfo>();
        public string defaultState;
        public List<StateInfo> states = new List<StateInfo>();
        public List<SpecialTransitionInfo> anyStateTransitions = new List<SpecialTransitionInfo>();
    }

    [MenuItem("Assets/SpriteTool/Export AnimController to JSON", false, 2002)]
    public static void ExportFirstLayerStateMachine()
    {
        var controller = Selection.activeObject as AnimatorController;
        if (controller == null || controller.layers.Length == 0)
        {
            EditorUtility.DisplayDialog("에러", "AnimatorController의 첫 번째 Layer를 찾을 수 없습니다.", "확인");
            return;
        }

        AnimatorControllerExport export = new AnimatorControllerExport();
        export.controllerName = controller.name;

        foreach (var param in controller.parameters)
        {
            export.parameters.Add(new ParameterInfo
            {
                name = param.name,
                type = param.type.ToString(),
                defaultFloat = param.defaultFloat,
                defaultInt = param.defaultInt,
                defaultBool = param.defaultBool
            });
        }

        var stateMachine = controller.layers[0].stateMachine;
        export.defaultState = stateMachine.defaultState?.name ?? "";

        foreach (var childState in stateMachine.states)
        {
            var state = childState.state;
            var clip = state.motion as AnimationClip;
            var stateInfo = new StateInfo
            {
                name = state.name,
                motionName = clip != null ? clip.name : "",
                clipLength = clip != null ? clip.length : 0f,
                loop = clip != null && clip.isLooping
            };

            foreach (var transition in state.transitions)
            {
                var t = new TransitionInfo
                {
                    fromState = state.name,
                    toState = transition.destinationState?.name ?? "Exit",
                    exitTime = transition.hasExitTime ? transition.exitTime : -1f,
                    hasExitTime = transition.hasExitTime
                };

                if (transition.conditions.Length > 0)
                {
                    foreach (var cond in transition.conditions)
                    {
                        t.conditions.Add(new ConditionInfo
                        {
                            parameter = cond.parameter,
                            mode = cond.mode.ToString(),
                            threshold = cond.threshold
                        });
                    }
                }
                else
                {
                    t.conditions.Add(new ConditionInfo
                    {
                        parameter = "",
                        mode = "Always",
                        threshold = 0f
                    });
                }
                stateInfo.transitions.Add(t);
            }

            export.states.Add(stateInfo);
        }

        foreach (var anyTrans in stateMachine.anyStateTransitions)
        {
            if (anyTrans.destinationState == null) continue;

            var t = new SpecialTransitionInfo
            {
                toState = anyTrans.destinationState.name
            };

            if (anyTrans.conditions.Length > 0)
            {
                foreach (var cond in anyTrans.conditions)
                {
                    t.conditions.Add(new ConditionInfo
                    {
                        parameter = cond.parameter,
                        mode = cond.mode.ToString(),
                        threshold = cond.threshold
                    });
                }
            }
            else
            {
                t.conditions.Add(new ConditionInfo
                {
                    parameter = "",
                    mode = "Always",
                    threshold = 0f
                });
            }

            export.anyStateTransitions.Add(t);
        }

        string json = JsonUtility.ToJson(export, true);
        string path = EditorUtility.SaveFilePanel("AnimController JSON 저장", "", controller.name + "_AnimController.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log("저장 완료: " + path);
        }
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public class AnimatorControllerExporter
{
    // UnityEngine.AnimatorConditionMode 의 문자열 매핑
    public static readonly string[] ConditionModes = new string[]
    {
        "If",        // bool 파라미터가 true일 때 전이됨
        "IfNot",     // bool 파라미터가 false일 때 전이됨
        "Greater",   // float/int 파라미터가 기준값보다 클 때 전이됨
        "Less",      // float/int 파라미터가 기준값보다 작을 때 전이됨
        "Equals",    // int 파라미터가 기준값과 같을 때 전이됨
        "NotEqual",  // int 파라미터가 기준값과 다를 때 전이됨
        "Always"     // 조건 없이 항상 전이됨 (조건이 없는 경우 명시적으로 사용됨)
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
    public class TransitionInfo
    {
        public string fromState;
        public string toState;
        public string conditionParameter;
        public string conditionMode;
        public float threshold;
        public float exitTime;
        public float duration;
        public bool hasExitTime;
    }

    [System.Serializable]
    public class StateInfo
    {
        public string name;
        public string motionName;
        public List<TransitionInfo> transitions = new List<TransitionInfo>();
    }

    [System.Serializable]
    public class SpecialTransitionInfo
    {
        public string toState;
        public string conditionParameter;
        public string conditionMode;
        public float threshold;
        public float duration;
    }

    [System.Serializable]
    public class LayerInfo
    {
        public string layerName;
        public string defaultState;
        public List<StateInfo> states = new List<StateInfo>();
        public List<SpecialTransitionInfo> anyStateTransitions = new List<SpecialTransitionInfo>();
    }

    [System.Serializable]
    public class AnimatorControllerInfo
    {
        public string controllerName;
        public List<ParameterInfo> parameters = new List<ParameterInfo>();
        public List<LayerInfo> layers = new List<LayerInfo>();
    }

    [MenuItem("Assets/SpriteTool/Export AnimatorController to JSON", false, 2001)]
    public static void ExportSelectedAnimatorController()
    {
        var controller = Selection.activeObject as AnimatorController;
        if (controller == null)
        {
            EditorUtility.DisplayDialog("에러", "AnimatorController를 선택하세요.", "확인");
            return;
        }

        AnimatorControllerInfo info = new AnimatorControllerInfo();
        info.controllerName = controller.name;

        foreach (var param in controller.parameters)
        {
            info.parameters.Add(new ParameterInfo
            {
                name = param.name,
                type = param.type.ToString(),
                defaultFloat = param.defaultFloat,
                defaultInt = param.defaultInt,
                defaultBool = param.defaultBool
            });
        }

        foreach (var layer in controller.layers)
        {
            var stateMachine = layer.stateMachine;
            var layerInfo = new LayerInfo
            {
                layerName = layer.name,
                defaultState = stateMachine.defaultState?.name ?? ""
            };

            foreach (var childState in stateMachine.states)
            {
                var state = childState.state;
                var stateInfo = new StateInfo
                {
                    name = state.name,
                    motionName = state.motion != null ? state.motion.name : ""
                };

                foreach (var transition in state.transitions)
                {
                    if (transition.conditions.Length > 0)
                    {
                        foreach (var cond in transition.conditions)
                        {
                            var t = new TransitionInfo
                            {
                                fromState = state.name,
                                toState = transition.destinationState?.name ?? "Exit",
                                exitTime = transition.hasExitTime ? transition.exitTime : -1f,
                                duration = transition.duration,
                                hasExitTime = transition.hasExitTime,
                                conditionParameter = cond.parameter,
                                conditionMode = cond.mode.ToString(),
                                threshold = cond.threshold
                            };
                            stateInfo.transitions.Add(t);
                        }
                    }
                    else
                    {
                        var t = new TransitionInfo
                        {
                            fromState = state.name,
                            toState = transition.destinationState?.name ?? "Exit",
                            exitTime = transition.hasExitTime ? transition.exitTime : -1f,
                            duration = transition.duration,
                            hasExitTime = transition.hasExitTime,
                            conditionParameter = "",
                            conditionMode = "Always",
                            threshold = 0f
                        };
                        stateInfo.transitions.Add(t);
                    }
                }

                layerInfo.states.Add(stateInfo);
            }

            foreach (var anyTrans in stateMachine.anyStateTransitions)
            {
                if (anyTrans.destinationState == null) continue;

                if (anyTrans.conditions.Length > 0)
                {
                    foreach (var cond in anyTrans.conditions)
                    {
                        var t = new SpecialTransitionInfo
                        {
                            toState = anyTrans.destinationState.name,
                            conditionParameter = cond.parameter,
                            conditionMode = cond.mode.ToString(),
                            threshold = cond.threshold,
                            duration = anyTrans.duration
                        };
                        layerInfo.anyStateTransitions.Add(t);
                    }
                }
                else
                {
                    var t = new SpecialTransitionInfo
                    {
                        toState = anyTrans.destinationState.name,
                        conditionParameter = "",
                        conditionMode = "Always",
                        threshold = 0f,
                        duration = anyTrans.duration
                    };
                    layerInfo.anyStateTransitions.Add(t);
                }
            }

            info.layers.Add(layerInfo);
        }

        string json = JsonUtility.ToJson(info, true);
        string path = EditorUtility.SaveFilePanel("AnimatorController JSON 저장", "", controller.name + "_AnimController.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log("저장 완료: " + path);
        }
    }
}

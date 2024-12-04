using DG.Tweening;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameUtil.Editor
{
    [CustomEditor(typeof(AnimationSequence)), CanEditMultipleObjects]
    public class AnimationSequenceEditor : UnityEditor.Editor
    {
        private const float ELEMENT_OFFSET_X = 7;

        private const float ELEMENT_OFFSET_Y =
#if UNITY_2020_1_OR_NEWER
                0;
#elif UNITY_2019_4_OR_NEWER
                1;
#else
                2;
#endif
            
        private ReorderableList mStartSingleTweenReorderableList;
        private AnimationSequence sequence;
        private float _previewTime;
        private bool _isPlaying;

        private void OnEnable()
        {
            sequence = (AnimationSequence)target;
             
            mStartSingleTweenReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(AnimationSequence.AnimationSteps)));
            mStartSingleTweenReorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, mStartSingleTweenReorderableList.serializedProperty.displayName);
            };
            mStartSingleTweenReorderableList.drawElementCallback = (rect, index, active, focused) =>
            {
                rect.x += ELEMENT_OFFSET_X;
                rect.width -= ELEMENT_OFFSET_X;
                rect.y += ELEMENT_OFFSET_Y;
                EditorGUI.PropertyField(rect, mStartSingleTweenReorderableList.serializedProperty.GetArrayElementAtIndex(index));
            };
            mStartSingleTweenReorderableList.elementHeightCallback = index =>
                EditorGUI.GetPropertyHeight(mStartSingleTweenReorderableList.serializedProperty.GetArrayElementAtIndex(index)) + ELEMENT_OFFSET_Y;
            mStartSingleTweenReorderableList.onAddCallback = OnAddCallback;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            //DrawPreviewControls();

            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                switch (iterator.propertyPath)
                {
                    case "m_Script":
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.PropertyField(iterator, true);
                        break;
                    case nameof(AnimationSequence.AnimationSteps):
                        mStartSingleTweenReorderableList.DoLayoutList();
                        break;
                    default:
                        EditorGUILayout.PropertyField(iterator, true);
                        break;
                }
            }
            
            
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
        
         private void DrawPreviewControls()
        {
            GUILayout.Space(10);
            GUILayout.Label("-------------------------------------------------------------------------------------");
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (sequence.gameObject.activeInHierarchy && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                GUILayout.BeginHorizontal();
                var stBtnPlay = new GUIStyle(EditorStyles.miniButtonLeft)
                {
                    fontSize = 15,
                    fixedWidth = 35,
                    fixedHeight = 20,
                    normal = new GUIStyleState()
                    {
                        //background = MakeColorTexture(new Color32(46, 139, 87, 255))
                    }
                };

                if (GUILayout.Button("▶", stBtnPlay))
                {
                    if (_previewTime != 0)
                        ResumePreview();
                    else
                        StartPreview();
                }

                GUILayout.Space(5);

                var stBtnStop = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    fontSize = 20,
                    fixedWidth = 35,
                    fixedHeight = 20,
                    contentOffset = new Vector2(0, -1.5f),
                    normal = new GUIStyleState()
                    {
                        //background = MakeColorTexture(new Color32(178, 34, 34, 255))
                    }
                };


                GUI.enabled = _previewTime != 0;
                if (GUILayout.Button("■", stBtnStop))
                {
                    StopPreview();
                }

                GUI.enabled = true;


                GUILayout.Space(5);
                PreviewAll();

                GUILayout.Space(10);
                EditorGUI.BeginChangeCheck();

                float newPreviewTime = EditorGUILayout.Slider(_previewTime, 0, sequence.GetDuration() - 0.001f);
                if (EditorGUI.EndChangeCheck())
                {
                    if (_previewTime == 0)
                        ResumePreview();
                    _previewTime = newPreviewTime;
                    sequence.Preview(_previewTime);
                    _isPlaying = false;
                    EditorApplication.update -= UpdatePreview;
                }

                //GUILayout.Label($"{_previewTime:F2}s", GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }
            else
            {
                StopPreview();
            }
        }

        private void PreviewAll()
        {
            
        }
        
        private void StartPreview()
        {
            _previewTime = 0;
            sequence.Preview(_previewTime);
            _isPlaying = true;
            EditorApplication.update += UpdatePreview;
        }
        
        private void ResumePreview()
        {
            _isPlaying = true;
            EditorApplication.update += UpdatePreview;
        }
        
        private void StopPreview()
        {
            _previewTime = 0;
            sequence.Preview(_previewTime);
            _isPlaying = false;
            EditorApplication.update -= UpdatePreview;
        }
        
        private void UpdatePreview()
        {
            _previewTime += Time.deltaTime;
            if (_previewTime >= sequence.GetDuration())
            {
                _previewTime = 0;
                _isPlaying = false;
                EditorApplication.update -= UpdatePreview;
            }
            sequence.Preview(_previewTime);
        }

        private static void OnAddCallback(ReorderableList list)
        {
            //Add at last
            list.serializedProperty.InsertArrayElementAtIndex(list.count);
            //Is first element, change EaseType to Ease.OutQuad(default)
            if (list.count == 1)
                list.serializedProperty.GetArrayElementAtIndex(0)
                    .FindPropertyRelative(nameof(SingleTween.EaseType)).intValue = (int) Ease.OutQuad;
        }
    }
}
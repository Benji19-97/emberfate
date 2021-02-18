using System;
using System.Linq;
using Runtime.NewStuff;
using Runtime.WorkInProgress;
using UnityEditor;
using UnityEngine;
using ValueType = Runtime.NewStuff.ValueType;

namespace Ayaya
{
    public class TraitCollectionEditor : EditorWindow
    {
        private const int SidebarWidth = 220;
        private const int TagListWidth = 200;
        private const int TagListHeight = 300;

        private TraitCollection _target;
        private TraitCollection _newTarget;
        private bool _arrayChanged;

        private string _searchFieldText;

        private int _selectedTraitIdx;
        private string[] _traitStringList = Array.Empty<string>();

        private Vector2 _traitListScrollPosition;

        private Vector2 _leftTagListPosition;
        private Vector2 _rightTagListPosition;

        private Texture2D _verticalLineTex;
        private Texture2D _lightTex;
        private Texture2D _darkTex;

        [MenuItem("Traits/Trait Collection Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<TraitCollectionEditor>();
            window.titleContent = new GUIContent("Trait Collection Editor");
            window.Show();
        }

        private void OnEnable()
        {
            _verticalLineTex = TextureHelper.MakeTex(2, TagListHeight, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            _lightTex = TextureHelper.MakeTex(2, TagListHeight, new Color(0f, 0f, 0f, 0.05f));
            _darkTex = TextureHelper.MakeTex(2, TagListHeight, new Color(0f, 0f, 0f, 0.1f));
        }

        private void Update()
        {
            if (_target != null && _target.Traits != null && _traitStringList.Length != _target.Traits.Length)
            {
                _arrayChanged = true;
            }

            if (_newTarget != _target)
            {
                _target = _newTarget;

                if (_target != null)
                {
                    EditorUtility.SetDirty(_target);
                }
                
                _arrayChanged = true;
            }

            if (_arrayChanged)
            {
                if (_newTarget != null)
                {
                    _traitStringList = new string[_newTarget.Traits.Count()];
                    for (int i = 0; i < _newTarget.Traits.Count(); i++)
                    {
                        _traitStringList[i] = $"[{i:D3}] " + _newTarget.Traits[i].Name;
                    }
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _newTarget = (TraitCollection) EditorGUILayout.ObjectField(_target, typeof(TraitCollection), false);

            if (_target && EditorUtility.IsDirty(_target))
            {
                if (GUILayout.Button("Save", GUILayout.Width(150)))
                {
                    EditorUtility.SetDirty(_target);
                    AssetDatabase.SaveAssets();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_target)
            {
                OnGUITraits();
            }
        }

        private void OnGUITraits()
        {

            
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth));
            OnGUITraitsSidebar();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            OnGUITraitInspector();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void OnGUITraitsSidebar()
        {

            _searchFieldText = EditorGUILayout.DelayedTextField(_searchFieldText);
            OnGUITraitList();
        }

        private void OnGUITraitList()
        {
            _traitListScrollPosition = GUILayout.BeginScrollView(_traitListScrollPosition);
            var fontStyle = new GUIStyle(GUI.skin.button);
            fontStyle.clipping = TextClipping.Clip;
            fontStyle.alignment = TextAnchor.MiddleLeft;
            _selectedTraitIdx = GUILayout.SelectionGrid(_selectedTraitIdx, _traitStringList, 1, fontStyle, GUILayout.Width(SidebarWidth - 20));
            GUILayout.EndScrollView();

            if (GUILayout.Button("Change Maximum ..."))
            {
                ChangeMaximumModalUtility.Show(ref _target);
                GUIUtility.ExitGUI();
            }
        }

        private void OnGUITraitInspector()
        {
            if (_selectedTraitIdx >= 0 && _selectedTraitIdx <= _target.Traits.Length)
            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Name:");
                _target.Traits[_selectedTraitIdx].Name = GUILayout.TextField(_target.Traits[_selectedTraitIdx].Name, 32, GUILayout.MaxWidth(250));
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Operator:");
                _target.Traits[_selectedTraitIdx].@operator =
                    (TraitOperator) EditorGUILayout.EnumPopup(_target.Traits[_selectedTraitIdx].@operator, GUILayout.MaxWidth(150));
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Category:");
                _target.Traits[_selectedTraitIdx].Category =
                    (TraitCategory) EditorGUILayout.EnumPopup(_target.Traits[_selectedTraitIdx].Category, GUILayout.MaxWidth(150));
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.Label("Value:");
                EditorGUILayout.BeginHorizontal();
                _target.Traits[_selectedTraitIdx].ValueType =
                    (ValueType) EditorGUILayout.EnumPopup(_target.Traits[_selectedTraitIdx].ValueType, GUILayout.MaxWidth(150));
                _target.Traits[_selectedTraitIdx].IsPercentage = GUILayout.Toggle(_target.Traits[_selectedTraitIdx].IsPercentage, "%");
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);


                GUILayout.Label("Tags:");
                //Start Lists
                var lightStyle = new GUIStyle(GUIStyle.none);
                lightStyle.normal.background = _lightTex;
                var darkStyle = new GUIStyle(GUIStyle.none);
                darkStyle.normal.background = _darkTex;

                EditorGUILayout.BeginHorizontal(darkStyle, GUILayout.Width(TagListWidth * 2 + 10));
                //Left List Start
                EditorGUILayout.BeginVertical(GUILayout.Width(TagListWidth), GUILayout.Height(TagListHeight));
                _leftTagListPosition = GUILayout.BeginScrollView(_leftTagListPosition);
                int idx = 1;
                foreach (var tag in _target.Traits[_selectedTraitIdx].Tags)
                {
                    var t = tag;
                    EditorGUILayout.BeginHorizontal(idx % 2 == 0 ? lightStyle : darkStyle);
                    idx++;
                    GUILayout.Label(tag.ToString());
                    if (GUILayout.Button(">", GUILayout.Width(20)))
                    {
                        _target.Traits[_selectedTraitIdx].Tags = _target.Traits[_selectedTraitIdx].Tags.Where(traitTag => traitTag != t).ToArray();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                //Left List End

                //Vertical Line Start
                var boxStyle = new GUIStyle(GUIStyle.none);
                boxStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Box(_verticalLineTex, boxStyle, GUILayout.Width(10));
                //Vertical Line End

                //Right List Start
                EditorGUILayout.BeginVertical(GUILayout.Width(TagListWidth), GUILayout.Height(TagListHeight));
                _rightTagListPosition = GUILayout.BeginScrollView(_rightTagListPosition);
                idx = 1;
                foreach (var tag in (TraitTag[]) Enum.GetValues(typeof(TraitTag)))
                {
                    if (!_target.Traits[_selectedTraitIdx].Tags.Contains(tag))
                    {
                        var t = tag;
                        EditorGUILayout.BeginHorizontal(idx % 2 == 0 ? lightStyle : darkStyle);
                        idx++;
                        if (GUILayout.Button("<", GUILayout.Width(20)))
                        {
                            Array.Resize(ref _target.Traits[_selectedTraitIdx].Tags, _target.Traits[_selectedTraitIdx].Tags.Length + 1);
                            _target.Traits[_selectedTraitIdx].Tags[_target.Traits[_selectedTraitIdx].Tags.Length - 1] = t;
                        }

                        GUILayout.Label(tag.ToString());
                        EditorGUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                //Right List End
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                //End Lists
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Effect:");
                _target.Traits[_selectedTraitIdx].Effect = (Effect) EditorGUILayout.ObjectField(_target.Traits[_selectedTraitIdx].Effect, typeof(Effect), false, GUILayout.Width(200));
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();

                GUILayout.Label("Modifier:");
                _target.Traits[_selectedTraitIdx].Modifier =
                    (Modifier) EditorGUILayout.ObjectField(_target.Traits[_selectedTraitIdx].Modifier, typeof(Modifier), false, GUILayout.Width(200));
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                
                EditorGUILayout.EndHorizontal();


                GUILayout.Space(5);

                GUILayout.Label("Notes:");
                _target.Traits[_selectedTraitIdx].Notes =
                    GUILayout.TextArea(_target.Traits[_selectedTraitIdx].Notes, GUILayout.Height(60), GUILayout.Width(250));

                EditorGUILayout.EndVertical();
            }
        }
    }
}
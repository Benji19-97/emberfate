using System;
using System.Collections.Generic;
using Runtime.NewStuff;
using Runtime.Traits.Enums;
using Runtime.Traits.Serializable;
using Runtime.WorkInProgress;
using UnityEditor;
using UnityEngine;

namespace Editor.Windows.TraitCollectionEditor
{
    public class TraitCollectionEditor : EditorWindow
    {
        private const int SidebarWidth = 300;
        private const int TagListWidth = 200;
        private const int TagListHeight = 300;

        private TraitCollection _target;
        private TraitCollection _newTarget;

        private string _searchFieldText = "";

        private int _selectedTraitIdx;

        private Vector2 _traitListScrollPosition;

        private Vector2 _leftTagListPosition;
        private Vector2 _rightTagListPosition;

        private Texture2D _verticalLineTex;
        private Texture2D _lightTex;
        private Texture2D _darkTex;
        private Texture2D _blueTex;

        private Font _consoleFont;

        [MenuItem("Game Data/Trait Collection Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<TraitCollectionEditor>();
            window.titleContent = new GUIContent("Trait Collection Editor");
            window.Show();
        }

        public static void ShowWindow(TraitCollection collection)
        {
            var window = GetWindow<TraitCollectionEditor>();
            window.titleContent = new GUIContent("Trait Collection Editor");
            window._newTarget = collection;
            window.Show();
        }

        private void OnEnable()
        {
            _verticalLineTex = TextureHelper.MakeTex(2, TagListHeight, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            _lightTex = TextureHelper.MakeTex(2, TagListHeight, new Color(0f, 0f, 0f, 0.05f));
            _darkTex = TextureHelper.MakeTex(2, TagListHeight, new Color(0f, 0f, 0f, 0.1f));
            _blueTex = TextureHelper.MakeTex(2, 2, new Color(44f / 255f, 93 / 255f, 135f / 255f, 1f));

            _consoleFont = Font.CreateDynamicFontFromOSFont("Consolas", 11);
        }

        private void Update()
        {
            if (_newTarget != _target)
            {
                _target = _newTarget;

                if (_target != null)
                {
                    EditorUtility.SetDirty(_target);
                }
            }
        }

        private void OnGUI()
        {
            var e = Event.current;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
            {
                _selectedTraitIdx++;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow)
            {
                _selectedTraitIdx--;
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            _newTarget = (TraitCollection) EditorGUILayout.ObjectField(_target, typeof(TraitCollection), false);
            GUILayout.Space(5);

            if (_target)
            {
                if (GUILayout.Button("Save", GUILayout.Width(150)))
                {
                    EditorUtility.SetDirty(_target);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);


            if (_target)
            {
                GUILayout.Space(10);
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
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Search:");
            _searchFieldText = EditorGUILayout.DelayedTextField(_searchFieldText, GUILayout.Width(SidebarWidth - 20));
            GUILayout.Space(5);

            OnGUITraitList();
            GUILayout.EndVertical();
        }

        private void OnGUITraitList()
        {
            _traitListScrollPosition = GUILayout.BeginScrollView(_traitListScrollPosition);

            var baseStyle = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                margin = new RectOffset(0, 0, 0, 0),
                font = _consoleFont, fontSize = 12,
                padding = new RectOffset(0, 0, 2, 2),
                fixedHeight = 20
            };

            var lightStyle = new GUIStyle(baseStyle);
            lightStyle.normal.background = _lightTex;

            var darkStyle = new GUIStyle(baseStyle);
            darkStyle.normal.background = _darkTex;

            var selectedStyle = new GUIStyle(baseStyle);
            selectedStyle.normal.background = _blueTex;

            for (int i = 0; i < _target.traits.Length; i++)
            {
                var name = $"[{i:D3}] " + _target.traits[i].name;

                if (!name.ToLower().Contains(_searchFieldText.ToLower()) && _searchFieldText.Length > 0) continue;

                var idx = i;

                var style = idx % 2 == 0 ? lightStyle : darkStyle;
                var actualStyle = idx == _selectedTraitIdx ? selectedStyle : style;

                if (GUILayout.Button(name, actualStyle, GUILayout.MinWidth(SidebarWidth - 20), GUILayout.ExpandWidth(true)))
                {
                    _selectedTraitIdx = idx;
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            if (GUILayout.Button("Change Maximum ...", GUILayout.Width(SidebarWidth - 20)))
            {
                ChangeTraitCollectionMaximumModal.Show(ref _target);
                GUIUtility.ExitGUI();
            }
        }

        private void OnGUITraitInspector()
        {
            if (_selectedTraitIdx >= 0 && _selectedTraitIdx <= _target.traits.Length && _target.traits.Length > 0)
            {
                _target.traits[_selectedTraitIdx].isLocked = EditorGUILayout.ToggleLeft("Lock", _target.traits[_selectedTraitIdx].isLocked);
                
                if (_target.traits[_selectedTraitIdx].isLocked)
                {
                    OnGuiTraitIsLocked();
                    return;
                }

                EditorGUILayout.BeginVertical();

                GUILayout.Label("Name:");

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{_selectedTraitIdx:D3}]", new GUIStyle(GUI.skin.label)
                {
                    font = _consoleFont,
                    fontSize = 14
                });

                if (_target.traits[_selectedTraitIdx] == null)
                {
                    _target.traits[_selectedTraitIdx] = new Trait();
                }

                _target.traits[_selectedTraitIdx].name = GUILayout.TextField(_target.traits[_selectedTraitIdx].name, 32, GUILayout.MaxWidth(250));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);


                GUILayout.Label("Category:");
                _target.traits[_selectedTraitIdx].category =
                    (TraitCategory) EditorGUILayout.EnumPopup(_target.traits[_selectedTraitIdx].category, GUILayout.MaxWidth(150));
                GUILayout.Space(5);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Is Local Modifier?");
                _target.traits[_selectedTraitIdx].isLocalModifier =
                    GUILayout.Toggle(_target.traits[_selectedTraitIdx].isLocalModifier, "", GUILayout.Width(20));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.Label("Versions:");
                var enums = Enum.GetNames(typeof(TraitValueType));
                _target.traits[_selectedTraitIdx].valueTypes = EditorGUILayout.MaskField(_target.traits[_selectedTraitIdx].valueTypes, enums, GUILayout.Width(200));

                var style = new GUIStyle();
                style.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(240));
                foreach (var traitVersion in Trait.ReturnSelectedElements(_target.traits[_selectedTraitIdx].valueTypes))
                {
                    GUILayout.Label("> " + traitVersion.ToString(), style);
                    GUILayout.Space(3);
                }
                GUILayout.EndVertical();
                
                GUILayout.Space(5);


                GUILayout.Label("Tags:");
                //Start Lists
                var lightStyle = new GUIStyle(GUIStyle.none);
                lightStyle.normal.background = _lightTex;
                var darkStyle = new GUIStyle(GUIStyle.none);
                darkStyle.normal.background = _darkTex;

                if (_target.traits[_selectedTraitIdx].tags == null)
                {
                    _target.traits[_selectedTraitIdx].tags = new List<TraitTag>();
                }

                EditorGUILayout.BeginHorizontal(darkStyle, GUILayout.Width(TagListWidth * 2 + 10));
                //Left List Start
                EditorGUILayout.BeginVertical(GUILayout.Width(TagListWidth), GUILayout.Height(TagListHeight));
                _leftTagListPosition = GUILayout.BeginScrollView(_leftTagListPosition);
                int idx = 1;
                foreach (var tag in _target.traits[_selectedTraitIdx].tags)
                {
                    var t = tag;
                    EditorGUILayout.BeginHorizontal(idx % 2 == 0 ? lightStyle : darkStyle);
                    idx++;
                    GUILayout.Label(tag.ToString());
                    if (GUILayout.Button(">", GUILayout.Width(20)))
                    {
                        _target.traits[_selectedTraitIdx].tags.Remove(t);
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
                    if (!_target.traits[_selectedTraitIdx].tags.Contains(tag))
                    {
                        var t = tag;
                        EditorGUILayout.BeginHorizontal(idx % 2 == 0 ? lightStyle : darkStyle);
                        idx++;
                        if (GUILayout.Button("<", GUILayout.Width(20)))
                        {
                            // Array.Resize(ref _target.Traits[_selectedTraitIdx].Tags, _target.Traits[_selectedTraitIdx].Tags.Length + 1);
                            _target.traits[_selectedTraitIdx].tags.Add(t);
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
                _target.traits[_selectedTraitIdx].effect =
                    (Effect) EditorGUILayout.ObjectField(_target.traits[_selectedTraitIdx].effect, typeof(Effect), false, GUILayout.Width(200));
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();

                GUILayout.Label("Modifier:");
                _target.traits[_selectedTraitIdx].modifier =
                    (Modifier) EditorGUILayout.ObjectField(_target.traits[_selectedTraitIdx].modifier, typeof(Modifier), false, GUILayout.Width(200));
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();


                GUILayout.Space(5);

                GUILayout.Label("Notes:");
                _target.traits[_selectedTraitIdx].notes =
                    GUILayout.TextArea(_target.traits[_selectedTraitIdx].notes, GUILayout.Height(60), GUILayout.Width(250));

                EditorGUILayout.EndVertical();
            }
        }

        private void OnGuiTraitIsLocked()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.grey;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:" );
            GUILayout.Label(_target.traits[_selectedTraitIdx].name, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Category:" );
            GUILayout.Label(_target.traits[_selectedTraitIdx].category.ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Is Local?");
            GUILayout.Label(_target.traits[_selectedTraitIdx].isLocalModifier.ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.Label("Versions: ");
            foreach (var traitVersion in Trait.ReturnSelectedElements(_target.traits[_selectedTraitIdx].valueTypes))
            {
                GUILayout.Label("> " + traitVersion, style);
            }
            GUILayout.Space(6);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tags: ");
            foreach (var tag in _target.traits[_selectedTraitIdx].tags)
            {
                GUILayout.Label(tag + ", ", style);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Effect: ");
            var effectString = _target.traits[_selectedTraitIdx].effect != null ? _target.traits[_selectedTraitIdx].effect.ToString() : "null";
            GUILayout.Label(effectString, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Modifier: ");
            var modifierString = _target.traits[_selectedTraitIdx].modifier != null ? _target.traits[_selectedTraitIdx].modifier.ToString() : "null";
            GUILayout.Label(modifierString, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Notes: ");
            GUILayout.Label(_target.traits[_selectedTraitIdx].notes, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
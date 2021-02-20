using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.NewStuff;
using UnityEditor;
using UnityEngine;

namespace Ayaya
{
    public class AffixCollectionEditor : EditorWindow
    {
        private AffixCollection _target;
        private AffixCollection _newTarget;

        private string _searchFieldText = "";
        private Vector2 _affixListScrollPosition;
        private Font _consoleFont;
        private const int SidebarWidth = 300;
        private Texture2D _verticalLineTex;
        private Texture2D _lightTex;
        private Texture2D _darkTex;
        private Texture2D _blueTex;
        private int _selectedAffixIdx;

        private string[] traitVersionTypes;
        private string[] traitCollectionsStrings;
        private string[][] availableTraitsStrings;

        [MenuItem("Data/Affix Collection Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<AffixCollectionEditor>();
            window.titleContent = new GUIContent("Affix Collection Editor");
            window.Show();
        }

        private void OnEnable()
        {
            _verticalLineTex = TextureHelper.MakeTex(2, 2, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            _lightTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.05f));
            _darkTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.1f));
            _blueTex = TextureHelper.MakeTex(2, 2, new Color(44f / 255f, 93 / 255f, 135f / 255f, 1f));
            _consoleFont = Font.CreateDynamicFontFromOSFont("Consolas", 11);

            traitVersionTypes = Enum.GetNames(typeof(TraitVersion));
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

        private void RefreshTraitCollectionStrings()
        {
            traitCollectionsStrings = new string[_target.traitCollectionDictionary.traitCollections.Length];

            for (int i = 0; i < _target.traitCollectionDictionary.traitCollections.Length; i++)
            {
                if (_target.traitCollectionDictionary.traitCollections[i] == null ||
                    _target.traitCollectionDictionary.traitCollections?[i].traits == null ||
                    _target.traitCollectionDictionary.traitCollections[i].traits.Length <= 0)
                {
                    continue;
                }

                var collectionName = _target.traitCollectionDictionary.traitCollections[i].name;
                traitCollectionsStrings[i] = collectionName;
            }
        }

        private void RefreshAvailableTraitsStrings()
        {
            availableTraitsStrings = new string[_target.traitCollectionDictionary.traitCollections.Length][];
            for (int i = 0; i < _target.traitCollectionDictionary.traitCollections.Length; i++)
            {
                if (_target.traitCollectionDictionary.traitCollections[i] == null ||
                    _target.traitCollectionDictionary.traitCollections?[i].traits == null ||
                    _target.traitCollectionDictionary.traitCollections[i].traits.Length <= 0)
                {
                    continue;
                }

                availableTraitsStrings[i] = new string[_target.traitCollectionDictionary.traitCollections[i].traits.Length];

                for (int y = 0; y < _target.traitCollectionDictionary.traitCollections[i].traits.Length; y++)
                {
                    var traitName = _target.traitCollectionDictionary.traitCollections[i].traits[y]?.name;
                    availableTraitsStrings[i][y] = traitName ?? "null";
                }
            }
        }

        private void OnGUI()
        {
            _newTarget = EditorGUILayout.ObjectField(_newTarget, typeof(AffixCollection), false) as AffixCollection;

            if (_target)
            {
                _target.traitCollectionDictionary =
                    EditorGUILayout.ObjectField(_target.traitCollectionDictionary, typeof(TraitCollectionDictionary), false) as TraitCollectionDictionary;

                if (GUILayout.Button("Refresh"))
                {
                    RefreshTraitCollectionStrings();
                }


                if (_target.traitCollectionDictionary)
                {
                    OnAffixTraits();
                }
            }
        }

        private void OnAffixTraits()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth));
            OnSidebar();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            OnGUIInspector();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        #region Sidebar

        private void OnSidebar()
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
            _affixListScrollPosition = GUILayout.BeginScrollView(_affixListScrollPosition);

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

            for (int i = 0; i < _target.affixes.Length; i++)
            {
                if (_target.affixes[i] == null)
                {
                    _target.affixes[i] = new Affix();
                }

                var name = $"[{i:D3}] " + _target.affixes[i].name;

                if (!name.ToLower().Contains(_searchFieldText.ToLower()) && _searchFieldText?.Length > 0) continue;

                var idx = i;

                var style = idx % 2 == 0 ? lightStyle : darkStyle;
                var actualStyle = idx == _selectedAffixIdx ? selectedStyle : style;

                if (GUILayout.Button(name, actualStyle, GUILayout.MinWidth(SidebarWidth - 20), GUILayout.ExpandWidth(true)))
                {
                    _selectedAffixIdx = idx;
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            if (GUILayout.Button("Change Maximum ...", GUILayout.Width(SidebarWidth - 20)))
            {
                ChangeAffixCollectionMaximumModal.Show(ref _target);
                GUIUtility.ExitGUI();
            }
        }

        #endregion

        private void OnGUIInspector()
        {
            if (_selectedAffixIdx >= 0 && _selectedAffixIdx <= _target.affixes.Length && _target.affixes.Length > 0)
            {
                OnGuiName();
                OnGuiAffixType();

                if (_target.traitCollectionDictionary.traitCollections != null && _target.traitCollectionDictionary.traitCollections.Length > 0)
                {
                    if (traitCollectionsStrings == null || traitCollectionsStrings.Length != _target.traitCollectionDictionary.traitCollections.Length)
                    {
                        RefreshTraitCollectionStrings();
                    }

                    var collectionIdx = _target.affixes[_selectedAffixIdx].traitCollectionIdx;
                    var collectionsArr = _target.traitCollectionDictionary.traitCollections;

                    if (collectionIdx < 0 || collectionIdx >= collectionsArr.Length)
                    {
                        _target.affixes[_selectedAffixIdx].traitCollectionIdx = 0;
                        collectionIdx = 0;
                    }

                    OnGuiSelectCollectionIndex();

                    if (_target.traitCollectionDictionary.traitCollections[collectionIdx]?.traits != null &&
                        _target.traitCollectionDictionary.traitCollections[collectionIdx].traits.Length > 0)
                    {
                        if (availableTraitsStrings == null ||
                            availableTraitsStrings[collectionIdx].Length != _target.traitCollectionDictionary.traitCollections.Length)
                        {
                            RefreshAvailableTraitsStrings();
                        }

                        if (availableTraitsStrings[collectionIdx] == null ||
                            availableTraitsStrings[collectionIdx].Length != _target.traitCollectionDictionary.traitCollections[collectionIdx].traits.Length)
                        {
                            RefreshAvailableTraitsStrings();
                        }

                        var traitIdx = _target.affixes[_selectedAffixIdx].traitIdx;

                        if (traitIdx < 0 || traitIdx >= _target.traitCollectionDictionary.traitCollections[collectionIdx].traits.Length)
                        {
                            _target.affixes[_selectedAffixIdx].traitIdx = 0;
                        }

                        OnGuiSelectTraitIndex();

                        OnGuiSelectVersion();
                    }
                }
            }
        }

        private void OnGuiName()
        {
            _target.affixes[_selectedAffixIdx].name = GUILayout.TextField(_target.affixes[_selectedAffixIdx].name, 32);
        }

        private void OnGuiAffixType()
        {
            _target.affixes[_selectedAffixIdx].affixType = (AffixType) EditorGUILayout.EnumPopup(_target.affixes[_selectedAffixIdx].affixType);
        }

        private void OnGuiSelectCollectionIndex()
        {
            _target.affixes[_selectedAffixIdx].traitCollectionIdx =
                EditorGUILayout.Popup(_target.affixes[_selectedAffixIdx].traitCollectionIdx, traitCollectionsStrings);
        }

        private void OnGuiSelectTraitIndex()
        {
            _target.affixes[_selectedAffixIdx].traitIdx =
                EditorGUILayout.Popup(_target.affixes[_selectedAffixIdx].traitIdx,
                    availableTraitsStrings[_target.affixes[_selectedAffixIdx].traitCollectionIdx]);
        }

        private void OnGuiSelectVersion()
        {
            var versions = _target.traitCollectionDictionary.traitCollections[_target.affixes[_selectedAffixIdx].traitCollectionIdx]
                .traits[_target.affixes[_selectedAffixIdx].traitIdx].versions;
            var enums = Trait.ReturnSelectedElements(versions);

            List<string> enumStrings = new List<string>();

            for (int i = 0; i < enums.Count; i++)
            {
                enumStrings.Add(enums[i].ToString());
            }

            int idx = enums.FindIndex(e => e == _target.affixes[_selectedAffixIdx].traitVersion);
            if (idx < 0 || idx >= enums.Count)
            {
                idx = 0;
                _target.affixes[_selectedAffixIdx].traitVersion = enums[0];
            }

            idx = EditorGUILayout.Popup(idx, enumStrings.ToArray());

            var traitVersion = (TraitVersion) Enum.Parse(typeof(TraitVersion) ,enumStrings[idx]);

            if (_target.affixes[_selectedAffixIdx].traitVersion != traitVersion)
            {
                _target.affixes[_selectedAffixIdx].traitVersion = traitVersion;
            }
            
            GUILayout.Label(_target.affixes[_selectedAffixIdx].traitVersion.ToString());

       }
    }
}
using System;
using System.Collections.Generic;
using Runtime.Affixes.Enums;
using Runtime.Affixes.Serializable;
using Runtime.NewStuff;
using Runtime.Traits.Enums;
using Runtime.Traits.Serializable;
using UnityEditor;
using UnityEngine;

namespace Editor.Windows.AffixCollectionEditor
{
    public class AffixCollectionEditor : EditorWindow
    {
        private const int SidebarWidth = 300;
        private Font _consoleFont;
        private Texture2D _lightTex;
        private Texture2D _darkTex;
        private Texture2D _blueTex;
        
        private AffixCollection _target;
        private AffixCollection _newTarget;
        private string _searchFieldText = "";
        private Vector2 _affixListScrollPosition;
        private int _selectedAffixIdx;
        private int _previewPowerLevel;
        private int _previewVarianceRoll;
        private string[] _traitCollectionsStrings;
        private string[][] _availableTraitsStrings;
        
        #region Show Window Functions
        [MenuItem("Game Data/Affix Collection Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<AffixCollectionEditor>();
            window.titleContent = new GUIContent("Affix Collection Editor");
            window.Show();
        }

        public static void ShowWindow(AffixCollection collection)
        {
            var window = GetWindow<AffixCollectionEditor>();
            window.titleContent = new GUIContent("Affix Collection Editor");
            window._newTarget = collection;
            window.Show();
        }
        #endregion

        private void OnEnable()
        {
            _lightTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.05f));
            _darkTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.1f));
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

        private void RefreshTraitCollectionStrings()
        {
            _traitCollectionsStrings = new string[_target.traitCollectionLibrary.collections.Length];

            for (int i = 0; i < _target.traitCollectionLibrary.collections.Length; i++)
            {
                if (_target.traitCollectionLibrary.collections[i] == null ||
                    _target.traitCollectionLibrary.collections?[i].traits == null ||
                    _target.traitCollectionLibrary.collections[i].traits.Length <= 0)
                {
                    continue;
                }

                var collectionName = _target.traitCollectionLibrary.collections[i].name;
                _traitCollectionsStrings[i] = collectionName;
            }
        }

        private void RefreshAvailableTraitsStrings()
        {
            _availableTraitsStrings = new string[_target.traitCollectionLibrary.collections.Length][];
            for (int i = 0; i < _target.traitCollectionLibrary.collections.Length; i++)
            {
                if (_target.traitCollectionLibrary.collections[i] == null ||
                    _target.traitCollectionLibrary.collections?[i].traits == null ||
                    _target.traitCollectionLibrary.collections[i].traits.Length <= 0)
                {
                    continue;
                }

                _availableTraitsStrings[i] = new string[_target.traitCollectionLibrary.collections[i].traits.Length];

                for (int y = 0; y < _target.traitCollectionLibrary.collections[i].traits.Length; y++)
                {
                    var traitName = _target.traitCollectionLibrary.collections[i].traits[y]?.name;
                    _availableTraitsStrings[i][y] = traitName ?? "null";
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            _newTarget = EditorGUILayout.ObjectField(_newTarget, typeof(AffixCollection), false, GUILayout.Width(350)) as AffixCollection;

            if (_target)
            {
                if (GUILayout.Button("Save", GUILayout.Width(100)))
                {
                    EditorUtility.SetDirty(_target);
                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.EndHorizontal();

            if (_target)
            {
                GUILayout.BeginHorizontal();
                _target.traitCollectionLibrary =
                    EditorGUILayout.ObjectField(_target.traitCollectionLibrary, typeof(TraitCollectionLibrary), false, GUILayout.Width(350)) as
                        TraitCollectionLibrary;

                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                {
                    RefreshTraitCollectionStrings();
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (_target.traitCollectionLibrary)
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
                _target.affixes[_selectedAffixIdx].isLocked = EditorGUILayout.ToggleLeft("Lock", _target.affixes[_selectedAffixIdx].isLocked);

                if (_target.affixes[_selectedAffixIdx].isLocked)
                {
                    OnGuiAffixIsLocked();
                    return;
                }


                OnGuiName();
                OnGuiAffixType();

                if (_target.traitCollectionLibrary.collections != null && _target.traitCollectionLibrary.collections.Length > 0)
                {
                    if (_traitCollectionsStrings == null || _traitCollectionsStrings.Length != _target.traitCollectionLibrary.collections.Length)
                    {
                        RefreshTraitCollectionStrings();
                    }

                    var collectionIdx = _target.affixes[_selectedAffixIdx].collectionIdx;
                    var collectionsArr = _target.traitCollectionLibrary.collections;

                    if (collectionIdx < 0 || collectionIdx >= collectionsArr.Length)
                    {
                        _target.affixes[_selectedAffixIdx].collectionIdx = 0;
                        collectionIdx = 0;
                    }

                    OnGuiSelectCollectionIndex();

                    if (_target.traitCollectionLibrary.collections[collectionIdx]?.traits != null &&
                        _target.traitCollectionLibrary.collections[collectionIdx].traits.Length > 0)
                    {
                        if (_availableTraitsStrings == null ||
                            _availableTraitsStrings[collectionIdx].Length != _target.traitCollectionLibrary.collections.Length)
                        {
                            RefreshAvailableTraitsStrings();
                        }

                        if (_availableTraitsStrings[collectionIdx] == null ||
                            _availableTraitsStrings[collectionIdx].Length != _target.traitCollectionLibrary.collections[collectionIdx].traits.Length)
                        {
                            RefreshAvailableTraitsStrings();
                        }

                        var traitIdx = _target.affixes[_selectedAffixIdx].traitIdx;

                        if (traitIdx < 0 || traitIdx >= _target.traitCollectionLibrary.collections[collectionIdx].traits.Length)
                        {
                            _target.affixes[_selectedAffixIdx].traitIdx = 0;
                        }

                        OnGuiSelectTraitIndex();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Variance:");
                        _target.affixes[_selectedAffixIdx].variance =
                            EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].variance, GUILayout.Width(35));
                        GUILayout.Label((_target.affixes[_selectedAffixIdx].variance * 100f) + "%");
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);

                        OnGuiSelectVersion();

                        OnGuiValueInput();
                        OnGuiInputReadableFormat();
                        OnGuiReadablePreview();
                    }
                }
            }
        }

        private void OnGuiName()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            _target.affixes[_selectedAffixIdx].name = GUILayout.TextField(_target.affixes[_selectedAffixIdx].name, 32, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiAffixType()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Affix Type:");
            _target.affixes[_selectedAffixIdx].affixType =
                (AffixType) EditorGUILayout.EnumPopup(_target.affixes[_selectedAffixIdx].affixType, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiSelectCollectionIndex()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Trait Collection:");
            _target.affixes[_selectedAffixIdx].collectionIdx =
                EditorGUILayout.Popup(_target.affixes[_selectedAffixIdx].collectionIdx, _traitCollectionsStrings, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiSelectTraitIndex()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Trait:");
            _target.affixes[_selectedAffixIdx].traitIdx =
                EditorGUILayout.Popup(_target.affixes[_selectedAffixIdx].traitIdx,
                    _availableTraitsStrings[_target.affixes[_selectedAffixIdx].collectionIdx], GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiSelectVersion()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Value Type:");
            var versions = _target.traitCollectionLibrary.collections[_target.affixes[_selectedAffixIdx].collectionIdx]
                .traits[_target.affixes[_selectedAffixIdx].traitIdx].valueTypes;
            var enums = Trait.ReturnSelectedElements(versions);

            List<string> enumStrings = new List<string>();

            for (int i = 0; i < enums.Count; i++)
            {
                enumStrings.Add(enums[i].ToString());
            }

            int idx = enums.FindIndex(e => e == _target.affixes[_selectedAffixIdx].valueType);
            if (idx < 0 || idx >= enums.Count)
            {
                idx = 0;
                _target.affixes[_selectedAffixIdx].valueType = enums[0];
            }

            idx = EditorGUILayout.Popup(idx, enumStrings.ToArray());

            var traitVersion = (TraitValueType) Enum.Parse(typeof(TraitValueType), enumStrings[idx]);

            if (_target.affixes[_selectedAffixIdx].valueType != traitVersion)
            {
                _target.affixes[_selectedAffixIdx].valueType = traitVersion;
            }

            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.gray;
            GUILayout.Label(_target.affixes[_selectedAffixIdx].valueType.ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiValueInput()
        {
            switch (_target.affixes[_selectedAffixIdx].valueType)
            {
                case TraitValueType.AddsRemovesFixedFlat:
                    OnGuiAddsRemoveFixedValues();
                    break;
                case TraitValueType.AddsRemovesFixedPercentage:
                    OnGuiAddsRemoveFixedValues();
                    break;
                case TraitValueType.AddsRemovesRangeFlat:
                    OnGuiAddsRemovesRangeFlatValues();
                    break;
                case TraitValueType.IncreasesReducesFixedPercentage:
                    OnGuiAddsRemoveFixedValues();
                    break;
                case TraitValueType.MoreLessFixedPercentage:
                    OnGuiAddsRemoveFixedValues();
                    break;
            }
        }

        private void OnGuiAddsRemoveFixedValues()
        {
            if (_target.affixes[_selectedAffixIdx].valuesPowerMin == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMin.Length != 1)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMin = new float[1];
            }

            GUILayout.BeginHorizontal();


            GUILayout.Label("Power (0):");
            _target.affixes[_selectedAffixIdx].valuesPowerMin[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMin[0], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_target.affixes[_selectedAffixIdx].valuesPowerMax == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMax.Length != 1)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMax = new float[1];
            }

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power (60):");
            _target.affixes[_selectedAffixIdx].valuesPowerMax[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMax[0], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void OnGuiAddsRemovesRangeFlatValues()
        {
            if (_target.affixes[_selectedAffixIdx].valuesPowerMin == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMin.Length != 2)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMin = new float[2];
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power (0):");
            _target.affixes[_selectedAffixIdx].valuesPowerMin[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMin[0], GUILayout.Width(40));
            GUILayout.Label("to");
            _target.affixes[_selectedAffixIdx].valuesPowerMin[1] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMin[1], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_target.affixes[_selectedAffixIdx].valuesPowerMax == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMax.Length != 2)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMax = new float[2];
            }

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power (60):");
            _target.affixes[_selectedAffixIdx].valuesPowerMax[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMax[0], GUILayout.Width(40));
            GUILayout.Label("to");
            _target.affixes[_selectedAffixIdx].valuesPowerMax[1] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMax[1], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void OnGuiInputReadableFormat()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Readable Format:");
            _target.affixes[_selectedAffixIdx].readableFormat = GUILayout.TextField(_target.affixes[_selectedAffixIdx].readableFormat, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void OnGuiReadablePreview()
        {


            var readablePreview = _target.affixes[_selectedAffixIdx].readableFormat;

            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.yellow;
            GUILayout.Label("Preview:");

            GUILayout.BeginHorizontal();
            _previewPowerLevel = EditorGUILayout.IntSlider(_previewPowerLevel, 0, 60, GUILayout.Width(150));
            GUILayout.Label("Power");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _previewVarianceRoll = EditorGUILayout.IntSlider(_previewVarianceRoll, 0, 100, GUILayout.Width(150));
            GUILayout.Label("Variance Roll");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            var value = _target.affixes[_selectedAffixIdx].GetValueRolledAndScaled(_previewVarianceRoll, _previewPowerLevel);

            try
            {
                if (_target.affixes[_selectedAffixIdx].valuesPowerMin.Length == 1 &&
                    readablePreview.Contains("{0}"))
                {
                    string valueString = value[0].ToString();

                    if (_target.affixes[_selectedAffixIdx].valueType == TraitValueType.AddsRemovesFixedPercentage ||
                        _target.affixes[_selectedAffixIdx].valueType == TraitValueType.IncreasesReducesFixedPercentage ||
                        _target.affixes[_selectedAffixIdx].valueType == TraitValueType.MoreLessFixedPercentage)
                    {
                        valueString = (value[0] * 100f) + "%";
                    }

                    GUILayout.Label(String.Format(readablePreview, valueString), style);
                }

                if (_target.affixes[_selectedAffixIdx].valuesPowerMin.Length == 2 &&
                    readablePreview.Contains("{0}") &&
                    readablePreview.Contains("{1}"))
                {
                    GUILayout.Label(String.Format(readablePreview, value[0],
                        value[1]), style);
                }
            }
            catch (FormatException e)
            {
                return;
            }
        }

        private void OnGuiAffixIsLocked()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.grey;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].name, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);


            GUILayout.BeginHorizontal();
            GUILayout.Label("Affix Type:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].affixType.ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Collection:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].collectionIdx.ToString(), style);
            GUILayout.Label("=> " + _target.traitCollectionLibrary.collections[_target.affixes[_selectedAffixIdx].collectionIdx].ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Trait:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].traitIdx.ToString(), style);
            GUILayout.Label(
                "=> " + _target.traitCollectionLibrary.collections[_target.affixes[_selectedAffixIdx].collectionIdx]
                    .traits[_target.affixes[_selectedAffixIdx].traitIdx].name, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Variance:");
            GUILayout.Label((_target.affixes[_selectedAffixIdx].variance * 100f) + "%", style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Readable Format:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].readableFormat, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            
            OnGuiReadablePreview();
        }
    }
}
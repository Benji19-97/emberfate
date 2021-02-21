using System;
using System.Linq;
using Runtime.Affixes.Serializable;
using Runtime.NewStuff;
using UnityEditor;
using UnityEngine;

namespace Editor.Windows.AffixPoolEditor
{
   
    public class AffixPoolEditor : EditorWindow
    {
        private AffixPool _target;
        private AffixPool _newTarget;
        private Vector2 _affixListScrollPosition;
        private Vector2 _affixCollectionPosition;

        private const int SidebarWidth = 400;
        private Texture2D _lightTex;
        private Texture2D _darkTex;
        private Texture2D _verticalLineTex;
        
        [MenuItem("Game Data/Affix Pool Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<AffixPoolEditor>();
            window.titleContent = new GUIContent("Affix Pool Editor");
            window.Show();
        }
        
        public static void ShowWindow(AffixPool pool)
        {
            var window = GetWindow<AffixPoolEditor>();
            window.titleContent = new GUIContent("Affix Pool Editor");
            window._newTarget = pool;
            window.Show();
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
        
        private void OnEnable()
        {
            _lightTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.05f));
            _darkTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.1f));
            _verticalLineTex = TextureHelper.MakeTex(2, 2, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            _newTarget = EditorGUILayout.ObjectField(_newTarget, typeof(AffixPool), false, GUILayout.Width(350)) as AffixPool;

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
                GUILayout.Space(2);
                _target.collection = EditorGUILayout.ObjectField(_target.collection, typeof(AffixCollection), false, GUILayout.Width(350)) as AffixCollection;
                GUILayout.Space(8);
                
                if (_target.collection)
                {
                    GUILayout.BeginHorizontal();
                    OnGUIAffixList();

                    //Vertical Line Start
                    var boxStyle = new GUIStyle(GUIStyle.none);
                    boxStyle.alignment = TextAnchor.MiddleCenter;
                    boxStyle.normal.background = _verticalLineTex;
                    boxStyle.margin.bottom = 4;
                    GUILayout.Label("", boxStyle, GUILayout.Width(2), GUILayout.ExpandHeight(true));
                    //Vertical Line End
                    
                    OnGUIAffixCollectionList();
                    GUILayout.EndHorizontal();
                }
            }
        }
        
                #region Sidebar



        private void OnGUIAffixList()
        {
            _affixListScrollPosition = GUILayout.BeginScrollView(_affixListScrollPosition);

            var baseStyle = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 2, 2),
                fixedHeight = 20
            };

            var lightStyle = new GUIStyle(baseStyle);
            lightStyle.normal.background = _lightTex;

            var darkStyle = new GUIStyle(baseStyle);
            darkStyle.normal.background = _darkTex;

            int totalWeight = 0;
            foreach (var affixReference in _target.affixes)
            {
                totalWeight += affixReference.weighting;
            }
            
            for (int i = 0; i < _target.affixes.Count; i++)
            {
                var idx = i;

                var name = $"[{_target.affixes[idx].affixIndex:D3}] " + _target.collection.affixes[_target.affixes[idx].affixIndex].name;

                var style = idx % 2 == 0 ? lightStyle : darkStyle;

                GUILayout.BeginHorizontal(style);
                
                GUILayout.Label(name, GUILayout.Width(200));
                _target.affixes[idx].weighting = EditorGUILayout.IntField(_target.affixes[idx].weighting, GUILayout.Width(40));

                var weightPercent = (float) _target.affixes[idx].weighting / totalWeight * 100f;
                weightPercent = (float) Math.Round(weightPercent, 2);
                GUILayout.Label(weightPercent + "%", GUILayout.Width(60));

                if (GUILayout.Button(">", GUILayout.Width(20)))
                {
                    _target.affixes.RemoveAt(idx);
                }
                
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void OnGUIAffixCollectionList()
        {
            _affixCollectionPosition = GUILayout.BeginScrollView(_affixCollectionPosition);

            var baseStyle = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 2, 2),
                fixedHeight = 20
            };

            var lightStyle = new GUIStyle(baseStyle);
            lightStyle.normal.background = _lightTex;

            var darkStyle = new GUIStyle(baseStyle);
            darkStyle.normal.background = _darkTex;

            int colorIdx = 0;
            for (int i = 0; i < _target.collection.affixes.Length; i++)
            {
                var idx = i;

                if (_target.affixes.Any(affix => affix.affixIndex == idx))
                {
                    continue;
                }

                colorIdx++;

                var name = _target.collection.affixes[idx].name;

                var style = colorIdx % 2 == 0 ? lightStyle : darkStyle;

                GUILayout.BeginHorizontal(style);
                if (GUILayout.Button("<", GUILayout.Width(20)))
                {
                    _target.affixes.Add(new AffixReference()
                    {
                        affixIndex = idx
                    });
                }
                GUILayout.Label(name);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        #endregion
    }
}
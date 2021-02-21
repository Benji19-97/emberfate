// using System;
// using Runtime.NewStuff;
// using UnityEditor;
// using UnityEngine;
//
// namespace Ayaya
// {
//     public class ChangeAffixPoolMaximumModal : EditorWindow
//     {
//         public AffixPool pool;
//         public int size;
//         
//         public static void Show(ref AffixPool affixPool)
//         {
//             var window = CreateInstance(typeof(ChangeAffixPoolMaximumModal)) as ChangeAffixPoolMaximumModal;
//             window.titleContent = new GUIContent("Change Maximum");
//             window.pool = affixPool;
//             window.size = affixPool.affixes.Length;
//             window.minSize = new Vector2(200, 75);
//             window.maxSize = new Vector2(200, 75);
//             window.ShowModalUtility();
//         }
//
//         private void OnGUI()
//         {
//             EditorGUILayout.LabelField("Maximum:");
//             size = EditorGUILayout.IntField(size);
//
//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("OK"))
//             {
//                 Array.Resize(ref pool.affixes, Mathf.Clamp(size, 0, 9999));
//                 Close();
//             }
//
//             if (GUILayout.Button("Cancel"))
//             {
//                 Close();
//             }
//             EditorGUILayout.EndHorizontal();
//         }
//     }
// }
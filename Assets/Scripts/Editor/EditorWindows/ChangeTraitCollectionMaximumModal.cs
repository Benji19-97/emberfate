﻿using System;
using Runtime.NewStuff;
using UnityEditor;
using UnityEngine;

namespace Ayaya
{
    public class ChangeTraitCollectionMaximumModal : EditorWindow
    {
        public TraitCollection collection;
        public int size;
        
        public static void Show(ref TraitCollection traitCollection)
        {
            var window = CreateInstance(typeof(ChangeTraitCollectionMaximumModal)) as ChangeTraitCollectionMaximumModal;
            window.titleContent = new GUIContent("Change Maximum");
            window.collection = traitCollection;
            window.size = traitCollection.traits.Length;
            window.minSize = new Vector2(200, 75);
            window.maxSize = new Vector2(200, 75);
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Maximum:");
            size = EditorGUILayout.IntField(size);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                Array.Resize(ref collection.traits, Mathf.Clamp(size, 0, 9999));
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
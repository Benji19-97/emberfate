using Ayaya;
using Runtime.NewStuff;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
{
    public class CustomAssetHandler
    {
        [OnOpenAsset(1)]
            public static bool step1(int instanceID, int line)
            {
                if (Selection.activeObject as TraitCollection != null) {
                    TraitCollectionEditor.ShowWindow(Selection.activeObject as TraitCollection);
                    return true; 
                }
                
                if (Selection.activeObject as AffixCollection != null) {
                    AffixCollectionEditor.ShowWindow(Selection.activeObject as AffixCollection);
                    return true; 
                }
                
                if (Selection.activeObject as AffixPool != null) {
                    AffixPoolEditor.ShowWindow(Selection.activeObject as AffixPool);
                    return true; 
                }

                return false;
            }
    }
}
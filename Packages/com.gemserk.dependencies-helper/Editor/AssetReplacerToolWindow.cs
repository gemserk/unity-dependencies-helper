using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Gemserk.DependenciesHelper
{
    public class AssetReplacerToolWindow : EditorWindow
    {

        [MenuItem("Window/Gemserk/DependenciesHelper/AssetReplacerTool")]
        public static AssetReplacerToolWindow ShowWindow()
        {
            AssetReplacerToolWindow window = EditorWindow.GetWindow<AssetReplacerToolWindow>("AssetReplacerToolWindow", true);
            return window;
        }

        public UnityEngine.Object originalDependency;
        public UnityEngine.Object replacementDependency;
        private List<AssetReplacerTool.UsageInfo> users = new List<AssetReplacerTool.UsageInfo>();

        public Vector2 scrollPos;

        void OnGUI()
        {
            originalDependency = EditorGUILayout.ObjectField("Dependency", originalDependency, typeof(UnityEngine.Object), false);
            replacementDependency = EditorGUILayout.ObjectField("Replacement", replacementDependency, typeof(UnityEngine.Object), false);

            if (GUILayout.Button("Load"))
            {
                LoadUsers();
            }


            EditorGUILayout.BeginVertical();
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                foreach (var user in users)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.LabelField(user.path);
                    EditorGUILayout.ObjectField("", user.asset, typeof(UnityEngine.Object), false);
                    if (replacementDependency != null && GUILayout.Button("REPLACE"))
                    {
                        AssetReplacerTool.ReplaceDependency(user.path, originalDependency, replacementDependency);
                        AssetDatabase.Refresh();
                    }
                }

                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void LoadUsers()
        {
            users = AssetReplacerTool.FindDependencyUsages(originalDependency);
        }
    }
}
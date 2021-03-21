using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gemserk.DependenciesHelper
{
    public class AssetReplacerTool
    {
        public interface IDependencyInfoProvider
        {
            List<string> GuidsOfUsers(string dependencyGuid);
        }
        
        public class UsageInfo
        {
            public string path;
            public UnityEngine.Object asset;
        }

        static List<string> GuidOfUsersFallback(string dependencyGuid)
        {
            var dependencyPath = AssetDatabase.GUIDToAssetPath(dependencyGuid);
            List<string> users = new List<string>();
            
            var allFiles = Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories);
            var disabledExtensions = new List<string>() {".meta", ".cs", ".txt", ".png", ".ogg"};
            var assets = allFiles.Where(s => !disabledExtensions.Contains(Path.GetExtension(s)) ).ToList();

            for (var index = 0; index < assets.Count; index++)
            {
                var asset = assets[index];
                if (EditorUtility.DisplayCancelableProgressBar("Dependencies", asset, index / (float) assets.Count))
                {
                    break;
                }

                var dependencies = AssetDatabase.GetDependencies(asset, false);
                if (dependencies.Contains(dependencyPath))
                {
                    users.Add(AssetDatabase.GUIDFromAssetPath(asset).ToString());
                }
            }

            EditorUtility.ClearProgressBar();
            
            return users;
        }

        static List<string> GuidsOfUsers(string dependencyGuid)
        {
            var types = TypeCache.GetTypesDerivedFrom<IDependencyInfoProvider>();
            var type = types.FirstOrDefault();
            if (type != null)
            {
                var infoProvider = (IDependencyInfoProvider) Activator.CreateInstance(type);
                return infoProvider.GuidsOfUsers(dependencyGuid);
            }
            else
            {
                return GuidOfUsersFallback(dependencyGuid);
            }
        }

        public static List<UsageInfo> FindDependencyUsages(UnityEngine.Object dependency)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(dependency, out string guid, out long localId);
            Debug.Log($"DEPENDENCY: {guid} - {localId} - {AssetDatabase.GUIDToAssetPath(guid)}");
            //var paths = FR2_Cache.Api.Get(guid).UsedByMap.Keys.Select(AssetDatabase.GUIDToAssetPath).ToList();

            var paths = GuidsOfUsers(guid).Select(AssetDatabase.GUIDToAssetPath).ToList();
            //Regex referenceRegexp = new Regex("{fileID: (-?[0-9]+), guid: ([0-9abcdef]+), type: ([0-9])}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            var referenceRegexp = CreateRegexp(localId, guid);

            Debug.Log($"Regexp: {referenceRegexp}");
            var usages = new List<AssetReplacerTool.UsageInfo>();
            for (var index = 0; index < paths.Count; index++)
            {
                var path = paths[index];
                EditorUtility.DisplayProgressBar("Parsing", path, index / (float) paths.Count);
                var text = File.ReadAllText(path);
                var matches = referenceRegexp.Matches(text);

                if (matches.Count > 0)
                {
                    usages.Add(new AssetReplacerTool.UsageInfo()
                    {
                        path = path,
                        asset = AssetDatabase.LoadMainAssetAtPath(path),
                    });
                    // Debug.Log($"{path}\n{matchingLines.ToStringArray("\n")}");
                }
            }

            EditorUtility.ClearProgressBar();
            return usages;
        }

        private static Regex CreateRegexp(long localId, string guid)
        {
            Regex referenceRegexp = new Regex($"{{fileID:\\s+({localId}),\\s+guid:\\s+({guid}),", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            return referenceRegexp;
        }

        public static void ReplaceDependency(string userPath, Object originalDependency, Object replacementDependency)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalDependency, out string originalGuid, out long originalLocalId);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(replacementDependency, out string replacementGuid, out long replacementLocalId);
            Debug.Log($"DEPENDENCY: {originalGuid} - {originalLocalId} - {AssetDatabase.GUIDToAssetPath(originalGuid)}");
            Debug.Log($"REPLACEMENT: {replacementGuid} - {replacementLocalId} - {AssetDatabase.GUIDToAssetPath(replacementGuid)}");
            Debug.Log($"USAGE: {userPath}");

            var regexp = CreateRegexp(originalLocalId, originalGuid);
            var text = File.ReadAllText(userPath);
            // var newText = regexp.Replace(text, match =>
            // {
            //    // match.
            //
            // });

        }
    }
}
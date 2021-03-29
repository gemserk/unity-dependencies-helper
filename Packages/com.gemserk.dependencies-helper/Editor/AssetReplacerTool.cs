using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gemserk.DependenciesHelper
{
    public class AssetRef
    {
        public long fileId;
        public string guid;

        public static AssetRef New(long fileId, string guid)
        {
            return new AssetRef {fileId = fileId, guid = guid};
        }

        protected bool Equals(AssetRef other)
        {
            return fileId == other.fileId && string.Equals(guid, other.guid, StringComparison.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetRef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (fileId.GetHashCode() * 397) ^ (guid != null ? StringComparer.InvariantCulture.GetHashCode(guid) : 0);
            }
        }
    }
    
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

        public static Regex CreateRegexp(long localId, string guid)
        {
            Regex referenceRegexp = new Regex($"{{fileID:\\s+({localId}),\\s+guid:\\s+({guid}),", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            return referenceRegexp;
        }
        
        public static Regex CreateRegexp()
        {
            Regex referenceRegexp = new Regex($"({{fileID:\\s+)(-?[0-9]+)(,\\s+guid:\\s+)([0-9abcdef]+)(,)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            return referenceRegexp;
        }

        public static void ReplaceDependency(string userPath, Object originalDependency, Object replacementDependency)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalDependency, out string originalGuid, out long originalLocalId);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(replacementDependency, out string replacementGuid, out long replacementLocalId);
            Debug.Log($"DEPENDENCY: {originalGuid} - {originalLocalId} - {AssetDatabase.GUIDToAssetPath(originalGuid)}");
            Debug.Log($"REPLACEMENT: {replacementGuid} - {replacementLocalId} - {AssetDatabase.GUIDToAssetPath(replacementGuid)}");
            Debug.Log($"USAGE: {userPath}");

            var originalAssetRef = AssetRef.New(originalLocalId, originalGuid);
            var replacementAssetRef = AssetRef.New(replacementLocalId, replacementGuid);

            var sourceFile = File.ReadAllText(userPath);
            var targetFile = ReplaceDependencyInPlace(sourceFile, originalAssetRef, replacementAssetRef);
            File.WriteAllText(userPath, targetFile);
        }

        public static string ReplaceDependencyInPlace(string fileContent, AssetRef original, AssetRef replacement)
        {
            var regexp = CreateRegexp();
            return regexp.Replace(fileContent, match =>
            {
                var tuple = ExtractFromMatch(match);
                if (!original.Equals(tuple))
                {
                    return match.Value;
                }
                else
                {
                    return $"{match.Groups[1]}{replacement.fileId}{match.Groups[3]}{replacement.guid}{match.Groups[5]}";
                }
            });
        }

        public static AssetRef ExtractFromMatch(Match match)
        {
            var fileIdString = match.Groups[2].Value;
            long fileId = long.Parse(fileIdString);
            var guid = match.Groups[4].Value;
            return AssetRef.New(fileId, guid);
        }
    }
}
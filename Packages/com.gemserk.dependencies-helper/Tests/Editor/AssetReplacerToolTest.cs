using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gemserk.DependenciesHelper.Tests
{
    public class AssetReplacerToolTest
    {
        private const string referenceAsset = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 33c43f4fd43a4fbaa997441062753e05, type: 3}
  m_Name: Testito
  m_EditorClassIdentifier: 
  stuff: {fileID: 2941741086030567527, guid: 845e14e3d06d247fb9c79560f3a335e9, type: 3}
  stuffjoijoijfiodjfsdojfsdodfjd: {fileID: 1757496506939017585, guid: 845e14e3d06d247fb9c79560f3a335e9,
    type: 3}
  stuffjoijdoijafoidjfsdoaijfdsofjsdofjdsofjdsaofjsdofjosdjfsdojfsadjojojojoofj: {fileID: -3957278670179174112,
    guid: 845e14e3d06d247fb9c79560f3a335e9, type: 3}
";

        public static string GeneratedFile(AssetRef scriptRef,
            AssetRef ref1,
            AssetRef ref2,
            AssetRef ref3)
        {
            return $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: {scriptRef.fileId}, guid: {scriptRef.guid}, type: 3}}
  m_Name: Testito
  m_EditorClassIdentifier: 
  stuff: {{fileID: {ref1.fileId}, guid: {ref1.guid}, type: 3}}
  stuffjoijoijfiodjfsdojfsdodfjd: {{fileID: {ref2.fileId}, guid: {ref2.guid},
    type: 3}}
  stuffjoijdoijafoidjfsdoaijfdsofjsdofjdsofjdsaofjsdofjosdjfsdojfsadjojojojoofj: {{fileID: {ref3.fileId},
    guid: {ref3.guid}, type: 3}}
";
        }

        public static string GeneratedFileDefault() => GeneratedFile(scriptRef, ref1, ref2, ref3);
        public static string GeneratedFileWithScript(AssetRef assetRef) => GeneratedFile(assetRef, ref1, ref2, ref3);
        public static string GeneratedFileWithRef1(AssetRef assetRef) => GeneratedFile(scriptRef, assetRef, ref2, ref3);
        public static string GeneratedFileWithRef2(AssetRef assetRef) => GeneratedFile(scriptRef, ref1, assetRef, ref3);
        public static string GeneratedFileWithRef3(AssetRef assetRef) => GeneratedFile(scriptRef, ref1, ref2, assetRef);


        private static AssetRef scriptRef = AssetRef.New(11500000, "33c43f4fd43a4fbaa997441062753e05");
        private static AssetRef ref1 = AssetRef.New(2941741086030567527, "845e14e3d06d247fb9c79560f3a335e9");
        private static AssetRef ref2 = AssetRef.New(1757496506939017585, "845e14e3d06d247fb9c79560f3a335e9");
        private static AssetRef ref3 = AssetRef.New(-3957278670179174112, "845e14e3d06d247fb9c79560f3a335e9");


        // A Test behaves as an ordinary method
       
        [TestCase(referenceAsset, 2941741086030567527, "845e14e3d06d247fb9c79560f3a335e9", true)]
        [TestCase(referenceAsset, 1757496506939017585, "845e14e3d06d247fb9c79560f3a335e9", true)]
        [TestCase(referenceAsset, -3957278670179174112, "845e14e3d06d247fb9c79560f3a335e9", true)]
        [TestCase(referenceAsset, 29417418, "845e14e3d06d247fb9c79560f3a335e9", false)]
        [TestCase(referenceAsset, 2941741086030567527, "888814e3d06d247fb9c79560f3a335e9", false)]
        public void AssetReplacerToolTestSimplePasses(string fileContent, long fileId, string guid, bool shouldMatch)
        {
            var regexp = AssetReplacerTool.CreateRegexp(fileId, guid);
            bool matches = regexp.IsMatch(fileContent);
            Assert.That(matches, Is.EqualTo(shouldMatch));
        }
        
        [Test]
        public void AssetReplacerToolTestSimplePassesGenericRegex()
        {
            var regexp = AssetReplacerTool.CreateRegexp();
            var matches = regexp.Matches(referenceAsset);

            Assert.That(matches.Count, Is.EqualTo(4));
            
            CheckMatch(matches[1], ref1);
            CheckMatch(matches[2], ref2);
            CheckMatch(matches[3], ref3);

            void CheckMatch(Match match, AssetRef assetRef)
            {
                var fileIdString = match.Groups[2].Value;
                Assert.That(long.Parse(fileIdString), Is.EqualTo(assetRef.fileId));
                var guidString = match.Groups[4].Value;
                Assert.That(guidString, Is.EqualTo(assetRef.guid));
            }
        }

        [Test]
        public void AssetReplacerToolReplace([ValueSource(nameof(AssetReplacerToolReplaceValues))] Tuple<string, AssetRef, AssetRef, string> testData)
        {
            var sourceFile = testData.Item1;
            var targetFile = testData.Item4;
            var targetFileCalculated = AssetReplacerTool.ReplaceDependencyInPlace(sourceFile, testData.Item2, testData.Item3);
            Assert.That(targetFileCalculated, Is.EqualTo(targetFile));
        }

        public static IEnumerable AssetReplacerToolReplaceValues()
        {
            return new List<Tuple<string, AssetRef, AssetRef, string>>()
            {
                new Tuple<string, AssetRef, AssetRef, string>(GeneratedFileDefault(),AssetRef.New(1, "pipote"), AssetRef.New(2, "popote"), GeneratedFileDefault()),
                new Tuple<string, AssetRef, AssetRef, string>(GeneratedFileDefault(),ref1, AssetRef.New(2, "popote"), GeneratedFileWithRef1(AssetRef.New(2, "popote"))),
                new Tuple<string, AssetRef, AssetRef, string>(GeneratedFileDefault(),ref2, AssetRef.New(2, "popote"), GeneratedFileWithRef2(AssetRef.New(2, "popote"))),
                new Tuple<string, AssetRef, AssetRef, string>(GeneratedFileDefault(),ref3, AssetRef.New(2, "popote"), GeneratedFileWithRef3(AssetRef.New(2, "popote"))),
            };
        }

    }
}

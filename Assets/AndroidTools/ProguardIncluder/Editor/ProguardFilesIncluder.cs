using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Wonderplanet.AndroidTools.ProguardIncluder
{
    public class ProguardFilesIncluder : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        private const string ProguardFileListPath = "Assets/Editor/AndroidTools/ProguardFileList.asset";
        private const string ProguardFileListDir = "Assets/Editor/AndroidTools";

        [MenuItem("Tools/Wonderplanet/Android Tools/Prepare Proguard Include File List")]
        static void PrepareFileList()
        {
            ProguardFileList fileList = AssetDatabase.LoadAssetAtPath<ProguardFileList>(ProguardFileListPath);
            if (fileList == null)
            {
                fileList = ProguardFileList.CreateInstance<ProguardFileList>();
                if (!Directory.Exists(ProguardFileListDir))
                {
                    Directory.CreateDirectory(ProguardFileListDir);
                    AssetDatabase.ImportAsset(ProguardFileListDir);
                }
                AssetDatabase.CreateAsset(fileList, ProguardFileListPath);
                AssetDatabase.SaveAssets();
            }
            EditorUtility.UnloadUnusedAssetsImmediate();
        }
        
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            ProguardFileList fileList = AssetDatabase.LoadAssetAtPath<ProguardFileList>(ProguardFileListPath);
            if (fileList != null)
            {
                List<string> filesToAdd = new List<string>();
                foreach (var text in fileList.ProguardFilesToInclude)
                {
                    if (text != null)
                    {
                        filesToAdd.Add(ProcessProguardFile(path, text));
                    }
                }

                if (filesToAdd.Count > 0)
                {
                    string gradlePath = Path.Combine(path, "build.gradle");
                    IncludeFilesInGradle(gradlePath,filesToAdd);
                }
            }
            
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        string ProcessProguardFile(string targetGradleProj, TextAsset proguardText)
        {
            string assetPath = AssetDatabase.GetAssetPath(proguardText);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            string outputTextFileName = $"proguard_{guid}.txt";
            string outputTextPath = Path.Combine(targetGradleProj, outputTextFileName);
            using (TextWriter writer = File.CreateText(outputTextPath))
            {
                writer.Write(proguardText.text);
            }

            return outputTextFileName;
        }

        void IncludeFilesInGradle(string gradleScript, List<string> proguardFiles)
        {
            List<string> gradleLines = new List<string>();
            gradleLines.AddRange(File.ReadLines(gradleScript));
            List<string> proguardFilesToAppend = proguardFiles.Distinct().ToList();
            
            int lineCount = gradleLines.Count;
            for (int i = 0; i < lineCount; i++)
            {
                var line = gradleLines[i];
                int lineLen = line.Length;
                bool check = lineLen > 0; 
                if (check)
                {
                    int startIndex = line.IndexOf("consumerProguardFiles");
                    if (startIndex > -1)
                    {
                        StringBuilder stringBuilder = new StringBuilder(line);
                        foreach (var appendFiles in proguardFilesToAppend)
                        {
                            stringBuilder.Append($", \"{appendFiles}\"");
                        }

                        gradleLines[i] = stringBuilder.ToString();
                    }
                }
            }
            
            using (var sw = File.CreateText(gradleScript))
            {
                foreach (var line in gradleLines)
                {
                    sw.WriteLine(line);
                }
            }
        }
    }
}

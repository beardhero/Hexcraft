// using UnityEditor;
// using UnityEditor.Build;
// using UnityEditor.Build.Reporting;
// using UnityEngine;
// using System.IO;

// class BuildPreprocessor : IPreprocessBuildWithReport
// {
//     public int callbackOrder { get { return 0; } }
//     public void OnPreprocessBuild(BuildReport report)
//     {
//         FileUtil.DeleteFileOrDirectory(Path.Combine(Application.streamingAssetsPath, "Server", "Node", "node_modules", "npm", "test"));
//         Debug.Log("MyCustomBuildProcessor.OnPreprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
//     }
// }

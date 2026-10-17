using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ProteinWeb.Editor
{
    // Keep the generated player usable in both a desktop window and a narrow preview panel.
    public class WebPageLayout : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1000;
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            Apply(report.summary.outputPath);
        }
        public static void Apply(string outputPath)
        {
            var path = Path.Combine(outputPath, "index.html");
            if (!File.Exists(path)) return;
            var html = File.ReadAllText(path)
                .Replace("Unity Web Player | EZPZ_Interaction_Toolkit", "High-Protein Shopping")
                .Replace("canvas.style.width = \"960px\";", "canvas.style.width = \"100vw\";")
                .Replace("canvas.style.height = \"600px\";", "canvas.style.height = \"100vh\";");
            if (!html.Contains("protein-responsive"))
                html = html.Replace("</head>", "<style id=\"protein-responsive\">html,body{width:100%;height:100%;overflow:hidden;background:#14181c}#unity-container.unity-desktop{position:fixed;inset:0;transform:none}#unity-canvas{display:block;width:100vw!important;height:100vh!important}#unity-footer{display:none}</style></head>");
            File.WriteAllText(path, html);
        }
    }
}

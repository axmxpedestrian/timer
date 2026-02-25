// Assets/Editor/CombineCharFiles.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CombineCharFiles : MonoBehaviour
{
    [MenuItem("Tools/Combine GB2312 + Big5 + Full Multilingual Chars")]
    static void Combine()
    {
        string[] fileNames = {
            "GB2312-1n2.txt",
            "big5_common_chars.txt",
            "full_multilingual_chars.txt"
        };

        var allChars = new HashSet<char>();

        foreach (string fileName in fileNames)
        {
            string path = Path.Combine(Application.dataPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"⚠️ File not found: {path}");
                continue;
            }

            string content = File.ReadAllText(path, Encoding.UTF8);
            foreach (char c in content)
            {
                // Skip control characters (optional)
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                    continue;
                allChars.Add(c);
            }

            Debug.Log($"✅ Loaded {allChars.Count} unique chars so far from {fileName}");
        }

        if (allChars.Count == 0)
        {
            Debug.LogError("❌ No valid characters loaded. Check file paths and encoding.");
            return;
        }

        // Optional: sort by Unicode code point
        var sortedChars = allChars.OrderBy(c => c).ToArray();

        // Save combined result
        string outputPath = Path.Combine(Application.dataPath, "combined_multilingual_chars.txt");
        File.WriteAllText(outputPath, new string(sortedChars), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log($"🎉 Combined and deduplicated {allChars.Count} characters.\nSaved to: {outputPath}");
    }
}
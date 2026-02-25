// Assets/Editor/Big5CommonCharExtractor.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class Big5CommonCharExtractor : MonoBehaviour
{
    [MenuItem("Tools/Extract Big5 Common Characters")]
    static void Extract()
    {
        string big5MapPath = "Assets/big5_map.txt"; // 先手动下载 BIG5.TXT 并重命名放这里

        if (!File.Exists(big5MapPath))
        {
            Debug.LogError("请先下载 https://www.unicode.org/Public/MAPPINGS/OBSOLETE/EASTASIA/OTHER/BIG5.TXT 并保存为 Assets/big5_map.txt");
            return;
        }

        var chars = new HashSet<char>();
        string[] lines = File.ReadAllLines(big5MapPath, Encoding.UTF8);

        int count = 0;
        foreach (string line in lines)
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

            // 格式: 0xA440  0x4E2D
            string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            if (int.TryParse(parts[1].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out int unicode))
            {
                if (unicode >= 0x4E00 && unicode <= 0x9FA5) // 只取 CJK 汉字
                {
                    chars.Add((char)unicode);
                    count++;
                    // Big5 常用字约 5401 个，可在此处限制数量
                    if (count >= 5601) break; // 近似截断（实际顺序非严格按频度）
                }
            }
        }

        // 排序（可选）
        var sortedChars = new List<char>(chars);
        sortedChars.Sort();

        string outputPath = "Assets/big5_common_chars.txt";
        File.WriteAllText(outputPath, new string(sortedChars.ToArray()), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"✅ 已生成 Big5 常用字符 ({sortedChars.Count} 字)，保存至: {outputPath}");
    }
}
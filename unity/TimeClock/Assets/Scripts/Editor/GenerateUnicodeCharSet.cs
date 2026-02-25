// Assets/Editor/GenerateUnicodeCharSet.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class GenerateUnicodeCharSet : MonoBehaviour
{
    [MenuItem("Tools/Generate Full Multilingual Character Set")]
    static void Generate()
    {
        var ranges = new (int start, int end)[]
        {
            // A) Latin + Symbols
            (0x0020, 0x00FF), // Basic Latin + Latin-1
            (0x0100, 0x017F), // Latin Extended-A
            (0x0180, 0x024F), // Latin Extended-B
            (0x1E00, 0x1EFF), // Latin Extended Additional
            (0x2000, 0x206F), // General Punctuation
            (0x20A0, 0x20CF), // Currency Symbols
            (0x2100, 0x214F), // Letterlike Symbols
            (0x2190, 0x21FF), // Arrows
            (0x2500, 0x257F), // Box Drawing

            // B) Cyrillic (Russian)
            (0x0400, 0x04FF), // Cyrillic
            (0x0500, 0x052F), // Cyrillic Supplement

            // C) Japanese
            (0x3040, 0x309F), // Hiragana
            (0x30A0, 0x30FF), // Katakana
            (0x31F0, 0x31FF), // Katakana Phonetic Extensions
            (0x3000, 0x303F), // CJK Symbols and Punctuation
            (0xFF00, 0xFFEF), // Halfwidth and Fullwidth Forms
            (0x3099, 0x309C), // Combining Kana Marks

            // D) Korean
            //(0xAC00, 0xD7AF), // Hangul Syllables
            //(0x1100, 0x11FF), // Hangul Jamo
            //(0x3130, 0x318F), // Hangul Compatibility Jamo
            //(0xA960, 0xA97F), // Hangul Jamo Extended-A
            //(0xD7B0, 0xD7FF)  // Hangul Jamo Extended-B
        };

        var charSet = new HashSet<char>();

        foreach (var (start, end) in ranges)
        {
            for (int code = start; code <= end; code++)
            {
                // Skip invalid or non-characters
                if (code < 0 || code > 0x10FFFF) continue;
                if (char.IsSurrogate((char)code)) continue; // Skip surrogates (use full surrogate pairs if needed)

                // For characters beyond U+FFFF, we'd need surrogate pairs,
                // but all your ranges are within BMP (U+0000–U+FFFF), so safe.
                charSet.Add((char)code);
            }
        }

        // Convert to sorted list (optional, for readability)
        var sortedList = new List<char>(charSet);
        sortedList.Sort();

        // Save to file
        string outputPath = Path.Combine(Application.dataPath, "full_multilingual_chars.txt");
        File.WriteAllText(outputPath, new string(sortedList.ToArray()), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log($"✅ Generated multilingual character set with {sortedList.Count} unique characters.\nSaved to: {outputPath}");
    }
}
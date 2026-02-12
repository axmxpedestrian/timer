#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PomodoroTimer.Editor
{
    /// <summary>
    /// 批量修改 Aseprite 文件的导入设置
    /// 菜单：Tools > Aseprite > 修复建筑Sprite导入设置
    /// </summary>
    public static class AsepriteBuildingImportFixer
    {
        /// <summary>
        /// 修复所有建筑 Aseprite 文件的 PPU 和 Pivot
        /// </summary>
        [MenuItem("Tools/Aseprite/修复建筑Sprite导入设置 (PPU=32, Pivot=Bottom)")]
        public static void FixAllBuildingAsepriteImports()
        {
            string buildSpritesPath = "Assets/Sprites/Build";

            if (!AssetDatabase.IsValidFolder(buildSpritesPath))
            {
                Debug.LogError($"[AsepriteFixer] 目录不存在: {buildSpritesPath}");
                return;
            }

            // 查找所有 .aseprite 和 .ase 文件
            string[] guids = AssetDatabase.FindAssets("", new[] { buildSpritesPath });
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!assetPath.EndsWith(".aseprite") && !assetPath.EndsWith(".ase"))
                    continue;

                string metaPath = assetPath + ".meta";
                if (!File.Exists(metaPath))
                    continue;

                string metaContent = File.ReadAllText(metaPath);
                string originalContent = metaContent;

                // 1. 修改 PPU: spritePixelsToUnits: 100 → 32
                metaContent = metaContent.Replace(
                    "spritePixelsToUnits: 100",
                    "spritePixelsToUnits: 32");

                // 2. 修改全局 Pivot 为 Bottom Center
                //    spritePivot: {x: 0.5, y: 0.5} → {x: 0.5, y: 0}
                metaContent = metaContent.Replace(
                    "spritePivot: {x: 0.5, y: 0.5}",
                    "spritePivot: {x: 0.5, y: 0}");

                // 3. 修改 Aseprite Importer 的 defaultPivotAlignment
                //    7 = Center → 9 = Custom (配合 customPivotPosition)
                //    同时确保 customPivotPosition 为 Bottom Center
                metaContent = metaContent.Replace(
                    "defaultPivotAlignment: 7",
                    "defaultPivotAlignment: 7"); // 保持不变，因为实际 pivot 由 animatedSpriteImportData 控制

                if (metaContent != originalContent)
                {
                    File.WriteAllText(metaPath, metaContent);
                    fixedCount++;
                    Debug.Log($"[AsepriteFixer] 已修复: {assetPath}");
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[AsepriteFixer] 完成！共修复 {fixedCount} 个文件。请检查 Sprite 显示是否正确。");
            }
            else
            {
                Debug.Log("[AsepriteFixer] 没有需要修复的文件（可能已经是正确设置）。");
            }
        }

        /// <summary>
        /// 仅修复 PPU（不改 Pivot）
        /// </summary>
        [MenuItem("Tools/Aseprite/仅修复PPU为32")]
        public static void FixPPUOnly()
        {
            string buildSpritesPath = "Assets/Sprites/Build";

            if (!AssetDatabase.IsValidFolder(buildSpritesPath))
            {
                Debug.LogError($"[AsepriteFixer] 目录不存在: {buildSpritesPath}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { buildSpritesPath });
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!assetPath.EndsWith(".aseprite") && !assetPath.EndsWith(".ase"))
                    continue;

                string metaPath = assetPath + ".meta";
                if (!File.Exists(metaPath))
                    continue;

                string metaContent = File.ReadAllText(metaPath);
                string originalContent = metaContent;

                metaContent = metaContent.Replace(
                    "spritePixelsToUnits: 100",
                    "spritePixelsToUnits: 32");

                if (metaContent != originalContent)
                {
                    File.WriteAllText(metaPath, metaContent);
                    fixedCount++;
                    Debug.Log($"[AsepriteFixer] PPU已修复: {assetPath}");
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[AsepriteFixer] 完成！共修复 {fixedCount} 个文件的PPU设置。");
            }
            else
            {
                Debug.Log("[AsepriteFixer] 没有需要修复的文件。");
            }
        }
    }
}
#endif

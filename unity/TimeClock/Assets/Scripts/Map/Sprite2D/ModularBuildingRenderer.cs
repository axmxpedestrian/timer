using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 模块化建筑渲染器
    /// 负责分层渲染、部件管理、染色和多楼层渲染
    /// </summary>
    public class ModularBuildingRenderer : MonoBehaviour
    {
        private ModularBuildingInstance buildingInstance;
        private Material sharedMaterial;
        private Material tintableMaterial;

        // 部件渲染器列表
        private List<PartRenderer> partRenderers = new List<PartRenderer>();
        private Transform partsContainer;

        // 动画状态
        private float animationTime;
        private bool isAnimating;

        // 建造进度
        private float constructionProgress = 1f;

        // 缓存的Shader属性ID
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int EnableTintId = Shader.PropertyToID("_EnableTint");

        /// <summary>
        /// 部件渲染器内部类
        /// </summary>
        private class PartRenderer
        {
            public string slotId;
            public int floorIndex;
            public SpriteRenderer spriteRenderer;
            public PartVariant variant;
            public BuildingPartConfig config;
            public MaterialPropertyBlock propertyBlock;
            public int currentFrame;

            public PartRenderer()
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void Awake()
        {
            CreatePartsContainer();
        }

        private void Update()
        {
            if (isAnimating && buildingInstance?.Blueprint != null)
            {
                UpdateAnimation();
            }
        }

        private void CreatePartsContainer()
        {
            if (partsContainer == null)
            {
                var containerObj = new GameObject("Parts");
                containerObj.transform.SetParent(transform);
                containerObj.transform.localPosition = Vector3.zero;
                partsContainer = containerObj.transform;
            }
        }

        /// <summary>
        /// 初始化渲染器
        /// </summary>
        public void Initialize(ModularBuildingInstance instance, Material material)
        {
            buildingInstance = instance;
            sharedMaterial = material;
            CreatePartsContainer();

            // 创建可染色材质
            if (tintableMaterial == null)
            {
                var tintableShader = Shader.Find("Custom/TintableSprite");
                if (tintableShader != null)
                {
                    tintableMaterial = new Material(tintableShader);
                }
                else
                {
                    tintableMaterial = sharedMaterial;
                }
            }
        }

        /// <summary>
        /// 重建所有部件渲染
        /// </summary>
        public void Rebuild()
        {
            Clear();

            if (buildingInstance?.Blueprint == null) return;

            var blueprint = buildingInstance.Blueprint;

            // 渲染单层建筑部件
            if (blueprint.partSlots != null && blueprint.partSlots.Length > 0)
            {
                RenderSingleFloorParts(blueprint);
            }

            // 渲染多层建筑
            if (blueprint.floors != null && blueprint.floors.Length > 0)
            {
                RenderMultiFloorParts(blueprint);
            }

            UpdateSorting();
            StartAnimationIfNeeded();
        }

        private void RenderSingleFloorParts(BuildingBlueprint blueprint)
        {
            foreach (var slot in blueprint.partSlots)
            {
                var config = buildingInstance.GetPartConfig(slot.slotId);
                if (config == null && slot.isRequired)
                {
                    // 使用默认变体
                    var defaultVariant = slot.GetDefaultVariant();
                    if (defaultVariant != null)
                    {
                        config = new BuildingPartConfig(slot.slotId, defaultVariant.variantId);
                    }
                }

                if (config == null) continue;

                var variant = slot.GetVariant(config.variantId);
                if (variant == null)
                    variant = slot.GetDefaultVariant();

                if (variant != null)
                {
                    CreatePartRenderer(slot, variant, config, -1, 0);
                }
            }
        }

        private void RenderMultiFloorParts(BuildingBlueprint blueprint)
        {
            int floorCount = buildingInstance.FloorCount;

            for (int i = 0; i < floorCount && i < blueprint.floors.Length; i++)
            {
                var floor = blueprint.floors[i];
                if (floor.partSlots == null) continue;

                foreach (var slot in floor.partSlots)
                {
                    var config = buildingInstance.GetFloorPartConfig(floor.floorIndex, slot.slotId);
                    if (config == null && slot.isRequired)
                    {
                        var defaultVariant = slot.GetDefaultVariant();
                        if (defaultVariant != null)
                        {
                            config = new BuildingPartConfig(slot.slotId, defaultVariant.variantId);
                        }
                    }

                    if (config == null) continue;

                    var variant = slot.GetVariant(config.variantId);
                    if (variant == null)
                        variant = slot.GetDefaultVariant();

                    if (variant != null)
                    {
                        CreatePartRenderer(slot, variant, config, floor.floorIndex, floor.sortingOrderBase);
                    }
                }
            }
        }

        private PartRenderer CreatePartRenderer(BuildingPartSlot slot, PartVariant variant,
            BuildingPartConfig config, int floorIndex, int baseSortingOrder)
        {
            var go = new GameObject($"Part_{slot.slotId}_{floorIndex}");
            go.transform.SetParent(partsContainer);

            // 计算本地位置
            Vector3 localPos = new Vector3(variant.localOffset.x, variant.localOffset.y, 0);
            if (floorIndex >= 0 && buildingInstance.Blueprint.floors != null)
            {
                var floor = buildingInstance.Blueprint.GetFloor(floorIndex);
                if (floor != null)
                {
                    var mapManager = IsometricSpriteMapManager.Instance;
                    float ppu = mapManager?.GetPixelsPerUnit() ?? 32f;
                    localPos.y += floor.heightOffset / ppu;
                }
            }
            go.transform.localPosition = localPos;

            // 创建SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = variant.GetFrame(0);

            // 选择材质
            if (variant.isTintable && tintableMaterial != null)
            {
                sr.material = tintableMaterial;
            }
            else if (sharedMaterial != null)
            {
                sr.sharedMaterial = sharedMaterial;
            }

            // 创建PartRenderer
            var partRenderer = new PartRenderer
            {
                slotId = slot.slotId,
                floorIndex = floorIndex,
                spriteRenderer = sr,
                variant = variant,
                config = config,
                currentFrame = 0
            };

            // 应用染色
            ApplyTint(partRenderer);

            // 设置排序
            sr.sortingLayerName = IsometricSortingHelper.LAYER_BUILDINGS;
            sr.sortingOrder = baseSortingOrder + variant.sortingOrderOffset;

            partRenderers.Add(partRenderer);
            return partRenderer;
        }

        private void ApplyTint(PartRenderer partRenderer)
        {
            if (partRenderer.spriteRenderer == null) return;

            var sr = partRenderer.spriteRenderer;
            sr.GetPropertyBlock(partRenderer.propertyBlock);

            if (partRenderer.variant.isTintable)
            {
                Color tintColor = partRenderer.config.useCustomTint
                    ? partRenderer.config.tintColor
                    : partRenderer.variant.defaultTint;

                partRenderer.propertyBlock.SetColor(TintColorId, tintColor);
                partRenderer.propertyBlock.SetFloat(EnableTintId, 1f);
            }
            else
            {
                partRenderer.propertyBlock.SetFloat(EnableTintId, 0f);
            }

            partRenderer.propertyBlock.SetFloat(BrightnessId, 1f);
            sr.SetPropertyBlock(partRenderer.propertyBlock);
        }

        /// <summary>
        /// 更新排序
        /// </summary>
        public void UpdateSorting()
        {
            if (buildingInstance == null) return;

            var gridPos = buildingInstance.GridPosition;
            int heightLevel = buildingInstance.Blueprint?.heightLevel ?? 0;
            int baseOrder = IsometricSortingHelper.CalculateBuildingSortingOrder(gridPos, heightLevel);

            foreach (var part in partRenderers)
            {
                if (part.spriteRenderer != null)
                {
                    int floorOffset = part.floorIndex >= 0 ? part.floorIndex * 100 : 0;
                    part.spriteRenderer.sortingOrder = baseOrder + floorOffset + part.variant.sortingOrderOffset;
                }
            }
        }

        /// <summary>
        /// 状态变化处理
        /// </summary>
        public void OnStateChanged(ModularBuildingState newState)
        {
            switch (newState)
            {
                case ModularBuildingState.Selected:
                    SetHighlight(true);
                    break;
                case ModularBuildingState.Normal:
                    SetHighlight(false);
                    SetBrightness(1f);
                    break;
                case ModularBuildingState.Damaged:
                    SetBrightness(0.7f);
                    break;
                case ModularBuildingState.Destroyed:
                    SetBrightness(0.4f);
                    break;
                case ModularBuildingState.Constructing:
                    SetConstructionProgress(constructionProgress);
                    break;
            }
        }

        /// <summary>
        /// 设置建造进度
        /// </summary>
        public void SetConstructionProgress(float progress)
        {
            constructionProgress = Mathf.Clamp01(progress);

            // 从下往上渐显效果
            foreach (var part in partRenderers)
            {
                if (part.spriteRenderer != null)
                {
                    // 简单实现：调整透明度
                    var color = part.spriteRenderer.color;
                    color.a = constructionProgress;
                    part.spriteRenderer.color = color;
                }
            }
        }

        /// <summary>
        /// 设置高亮
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            float brightness = highlighted ? 1.3f : 1f;
            SetBrightness(brightness);
        }

        /// <summary>
        /// 设置亮度
        /// </summary>
        public void SetBrightness(float brightness)
        {
            foreach (var part in partRenderers)
            {
                if (part.spriteRenderer != null)
                {
                    part.spriteRenderer.GetPropertyBlock(part.propertyBlock);
                    part.propertyBlock.SetFloat(BrightnessId, brightness);
                    part.spriteRenderer.SetPropertyBlock(part.propertyBlock);
                }
            }
        }

        /// <summary>
        /// 设置部件染色
        /// </summary>
        public void SetPartTint(string slotId, Color color, int floorIndex = -1)
        {
            foreach (var part in partRenderers)
            {
                if (part.slotId == slotId && part.floorIndex == floorIndex)
                {
                    part.config.tintColor = color;
                    part.config.useCustomTint = true;
                    ApplyTint(part);
                    break;
                }
            }
        }

        private void StartAnimationIfNeeded()
        {
            isAnimating = false;
            if (buildingInstance?.Blueprint == null) return;

            // 检查是否有多帧动画
            foreach (var part in partRenderers)
            {
                if (part.variant.FrameCount > 1)
                {
                    isAnimating = true;
                    break;
                }
            }

            animationTime = 0f;
        }

        private void UpdateAnimation()
        {
            if (buildingInstance?.Blueprint == null) return;

            float frameRate = buildingInstance.Blueprint.frameRate;
            if (frameRate <= 0) frameRate = 8f;

            animationTime += Time.deltaTime;
            float frameDuration = 1f / frameRate;

            foreach (var part in partRenderers)
            {
                if (part.variant.FrameCount <= 1) continue;

                int newFrame = Mathf.FloorToInt(animationTime / frameDuration) % part.variant.FrameCount;
                if (newFrame != part.currentFrame)
                {
                    part.currentFrame = newFrame;
                    part.spriteRenderer.sprite = part.variant.GetFrame(newFrame);
                }
            }
        }

        /// <summary>
        /// 清除所有部件
        /// </summary>
        public void Clear()
        {
            foreach (var part in partRenderers)
            {
                if (part.spriteRenderer != null && part.spriteRenderer.gameObject != null)
                {
                    Destroy(part.spriteRenderer.gameObject);
                }
            }
            partRenderers.Clear();
            isAnimating = false;
        }

        private void OnDestroy()
        {
            Clear();
            if (tintableMaterial != null && tintableMaterial != sharedMaterial)
            {
                Destroy(tintableMaterial);
            }
        }
    }
}

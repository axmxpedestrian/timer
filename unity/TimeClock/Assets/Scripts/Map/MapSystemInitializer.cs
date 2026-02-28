using UnityEngine;
using System.Reflection;
using PomodoroTimer.Map.Sprite2D;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map
{
    /// <summary>
    /// 地图系统初始化器（2D Sprite版本）
    /// 在MainPanel中创建和初始化等距视角2D Sprite地图
    /// </summary>
    public class MapSystemInitializer : MonoBehaviour
    {
        [Header("初始化设置")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private Transform mapParent;

        [Header("地图设置")]
        [SerializeField] private int mapWidth = 32;
        [SerializeField] private int mapHeight = 32;
        [SerializeField] private int chunkSize = 8;

        [Header("Sprite设置")]
        [SerializeField] private float tileWidth = 64f;
        [SerializeField] private float tileHeight = 32f;
        [SerializeField] private float pixelsPerUnit = 32f;
        [SerializeField] private Sprite defaultTileSprite;
        [SerializeField] private Material spriteMaterial;

        [Header("相机设置")]
        [SerializeField] private float initialZoom = 10f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 20f;

        [Header("建筑设置")]
        [SerializeField] private Material buildingMaterial;

        [Header("模块化建筑设置")]
        [SerializeField] private bool enableModularBuildings = true;
        [SerializeField] private BuildingBlueprint[] buildingBlueprints;
        [Tooltip("放置预览材质（使用 Custom/BuildingPreview Shader）")]
        [SerializeField] private Material buildingPreviewMaterial;

        [Header("全局系统")]
        [SerializeField] private bool enableTimeSystem = true;
        [SerializeField] private bool enableAudioSystem = true;

        private GameObject mapSystemRoot;

        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeMapSystem();
            }
        }

        public void InitializeMapSystem()
        {
            if (mapSystemRoot != null)
            {
                Debug.LogWarning("[MapSystemInitializer] 地图系统已初始化");
                return;
            }

            // 创建根对象
            mapSystemRoot = new GameObject("MapSystem");
            if (mapParent != null)
            {
                mapSystemRoot.transform.SetParent(mapParent);
            }
            mapSystemRoot.transform.localPosition = Vector3.zero;

            // 检查必要资源
            if (defaultTileSprite == null)
            {
                Debug.LogError("[MapSystemInitializer] 请在 Inspector 中指定 defaultTileSprite！");
                return;
            }

            // 创建材质（如果未指定）
            if (spriteMaterial == null)
            {
                spriteMaterial = CreateDefaultSpriteMaterial();
            }
            if (buildingMaterial == null)
            {
                buildingMaterial = spriteMaterial;
            }

            // 添加2D Sprite地图管理器
            var mapManager = mapSystemRoot.AddComponent<IsometricSpriteMapManager>();
            SetupSpriteMapManager(mapManager);

            // 添加输入控制器
            mapSystemRoot.AddComponent<MapInputController>();

            // 添加建筑管理器
            var buildingManager = mapSystemRoot.AddComponent<SpriteBuildingManager>();
            SetupBuildingManager(buildingManager);

            // 添加模块化建筑系统
            if (enableModularBuildings)
            {
                InitializeModularBuildingSystem();
            }

            // 添加全局时间系统
            if (enableTimeSystem)
            {
                mapSystemRoot.AddComponent<GlobalTimeManager>();
            }

            // 添加音效系统
            if (enableAudioSystem)
            {
                mapSystemRoot.AddComponent<BuildingAudioManager>();
            }

            Debug.Log("[MapSystemInitializer] 2D Sprite地图系统初始化完成");
        }

        private void InitializeModularBuildingSystem()
        {
            // 添加模块化建筑管理器
            var modularManager = mapSystemRoot.AddComponent<ModularBuildingManager>();
            SetupModularBuildingManager(modularManager);

            // 添加放置控制器，并注入预览材质
            var placementController = mapSystemRoot.AddComponent<BuildingPlacementController>();
            SetupPlacementController(placementController);
        }

        private void SetupPlacementController(BuildingPlacementController controller)
        {
            if (buildingPreviewMaterial == null) return;

            var type = typeof(BuildingPlacementController);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            SetFieldValue(type, controller, "previewMaterial", buildingPreviewMaterial, flags);
        }

        private void SetupModularBuildingManager(ModularBuildingManager manager)
        {
            var type = typeof(ModularBuildingManager);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            SetFieldValue(type, manager, "buildingMaterial", buildingMaterial, flags);
            if (buildingBlueprints != null && buildingBlueprints.Length > 0)
            {
                SetFieldValue(type, manager, "blueprintList", buildingBlueprints, flags);
            }
        }

        private void SetupSpriteMapManager(IsometricSpriteMapManager mapManager)
        {
            var type = typeof(IsometricSpriteMapManager);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            SetFieldValue(type, mapManager, "mapWidth", mapWidth, flags);
            SetFieldValue(type, mapManager, "mapHeight", mapHeight, flags);
            SetFieldValue(type, mapManager, "chunkSize", chunkSize, flags);
            SetFieldValue(type, mapManager, "tileWidth", tileWidth, flags);
            SetFieldValue(type, mapManager, "tileHeight", tileHeight, flags);
            SetFieldValue(type, mapManager, "pixelsPerUnit", pixelsPerUnit, flags);
            SetFieldValue(type, mapManager, "defaultTileSprite", defaultTileSprite, flags);
            SetFieldValue(type, mapManager, "spriteMaterial", spriteMaterial, flags);
            SetFieldValue(type, mapManager, "initialZoom", initialZoom, flags);
            SetFieldValue(type, mapManager, "minZoom", minZoom, flags);
            SetFieldValue(type, mapManager, "maxZoom", maxZoom, flags);
        }

        private void SetupBuildingManager(SpriteBuildingManager buildingManager)
        {
            var type = typeof(SpriteBuildingManager);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            SetFieldValue(type, buildingManager, "buildingMaterial", buildingMaterial, flags);
        }

        private void SetFieldValue(System.Type type, object obj, string fieldName, object value, BindingFlags flags)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[MapSystemInitializer] 未找到字段: {fieldName}");
            }
        }

        private Material CreateDefaultSpriteMaterial()
        {
            // 使用 URP 2D Sprite Unlit Shader
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                Debug.LogError("[MapSystemInitializer] 无法找到 Sprite Shader！");
                return null;
            }

            Material mat = new Material(shader);
            mat.name = "DefaultSpriteMaterial";
            return mat;
        }

        public void DestroyMapSystem()
        {
            if (mapSystemRoot != null)
            {
                Destroy(mapSystemRoot);
                mapSystemRoot = null;
            }
        }

        public GameObject GetMapSystemRoot()
        {
            return mapSystemRoot;
        }
    }
}

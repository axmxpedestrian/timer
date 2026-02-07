using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map.Sprite2D
{
    public class IsometricSpriteMapManager : MonoBehaviour
    {
        public static IsometricSpriteMapManager Instance { get; private set; }

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
        [SerializeField] private Camera mapCamera;
        [SerializeField] private float initialZoom = 10f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 20f;

        [Header("性能设置")]
        [SerializeField] private int tilePoolInitialSize = 256;
        [SerializeField] private int tilePoolMaxSize = 2048;
        [SerializeField] private bool enableChunkCulling = true;

        private Dictionary<Vector2Int, SpriteChunk> chunks = new Dictionary<Vector2Int, SpriteChunk>();
        private Transform chunksParent;
        private Transform buildingsParent;
        private SpriteTilePool tilePool;
        private SpriteChunkPool chunkPool;
        private Vector3 cameraTargetPosition;
        private float currentZoom;
        private Bounds mapBounds;

        private void Awake()
        {
            if (Instance == null) { Instance = this; }
            else { Destroy(gameObject); return; }
            InitializeContainers();
            InitializePools();
        }

        private void Start()
        {
            SetupCamera();
            CalculateMapBounds();
            GenerateMap();
        }

        private void Update()
        {
            if (enableChunkCulling) UpdateChunkCulling();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            CleanupPools();
        }

        private void InitializeContainers()
        {
            chunksParent = new GameObject("Chunks").transform;
            chunksParent.SetParent(transform);
            chunksParent.localPosition = Vector3.zero;
            buildingsParent = new GameObject("Buildings").transform;
            buildingsParent.SetParent(transform);
            buildingsParent.localPosition = Vector3.zero;
        }

        private void InitializePools()
        {
            tilePool = new SpriteTilePool(chunksParent, spriteMaterial, tilePoolInitialSize, tilePoolMaxSize);
            chunkPool = new SpriteChunkPool(chunksParent, tilePool);
        }

        private void CleanupPools()
        {
            chunkPool?.Clear();
            tilePool?.Clear();
        }

        private void SetupCamera()
        {
            if (mapCamera == null)
            {
                var cameraObj = new GameObject("MapCamera");
                cameraObj.transform.SetParent(transform);
                mapCamera = cameraObj.AddComponent<Camera>();
            }
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = initialZoom;
            currentZoom = initialZoom;
            mapCamera.transform.rotation = Quaternion.identity;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
            Vector3 mapCenter = GridToWorld(new Vector2Int(mapWidth / 2, mapHeight / 2));
            cameraTargetPosition = mapCenter;
            mapCamera.transform.position = new Vector3(mapCenter.x, mapCenter.y, -10f);
        }

        private void CalculateMapBounds()
        {
            Vector3 c00 = GridToWorld(new Vector2Int(0, 0));
            Vector3 c10 = GridToWorld(new Vector2Int(mapWidth, 0));
            Vector3 c01 = GridToWorld(new Vector2Int(0, mapHeight));
            Vector3 c11 = GridToWorld(new Vector2Int(mapWidth, mapHeight));
            float minX = Mathf.Min(c00.x, c10.x, c01.x, c11.x);
            float maxX = Mathf.Max(c00.x, c10.x, c01.x, c11.x);
            float minY = Mathf.Min(c00.y, c10.y, c01.y, c11.y);
            float maxY = Mathf.Max(c00.y, c10.y, c01.y, c11.y);
            mapBounds = new Bounds(new Vector3((minX+maxX)/2f, (minY+maxY)/2f, 0), new Vector3(maxX-minX, maxY-minY, 1f));
        }

        private void GenerateMap()
        {
            int chunksX = Mathf.CeilToInt((float)mapWidth / chunkSize);
            int chunksY = Mathf.CeilToInt((float)mapHeight / chunkSize);
            for (int cx = 0; cx < chunksX; cx++)
                for (int cy = 0; cy < chunksY; cy++)
                    CreateChunk(new Vector2Int(cx, cy));
            Debug.Log($"[IsometricSpriteMapManager] 地图生成完成: {mapWidth}x{mapHeight} 格子, {chunksX}x{chunksY} 分块");
        }

        private void CreateChunk(Vector2Int chunkCoord)
        {
            SpriteChunk chunk = chunkPool.Get(chunkCoord);
            int startX = chunkCoord.x * chunkSize;
            int startY = chunkCoord.y * chunkSize;
            int endX = Mathf.Min(startX + chunkSize, mapWidth);
            int endY = Mathf.Min(startY + chunkSize, mapHeight);
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    SpriteRenderer renderer = tilePool.Get();
                    if (renderer != null)
                    {
                        renderer.transform.position = GridToWorld(gridPos);
                        renderer.sprite = defaultTileSprite;
                        IsometricSortingHelper.ApplyTileSorting(renderer, gridPos);
                        chunk.AddTile(renderer);
                    }
                }
            }
            Vector3 center = GridToWorld(new Vector2Int((startX+endX)/2, (startY+endY)/2));
            chunk.SetBounds(new Bounds(center, new Vector3(chunkSize*(tileWidth/pixelsPerUnit), chunkSize*(tileHeight/pixelsPerUnit), 1f)));
            chunks[chunkCoord] = chunk;
        }

        private void UpdateChunkCulling()
        {
            if (mapCamera == null) return;
            Rect camRect = GetCameraWorldRect();
            foreach (var kvp in chunks)
                kvp.Value.SetVisible(camRect.Overlaps(BoundsToRect(kvp.Value.Bounds)));
        }

        private Rect GetCameraWorldRect()
        {
            float h = mapCamera.orthographicSize * 2f;
            float w = h * mapCamera.aspect;
            Vector3 p = mapCamera.transform.position;
            return new Rect(p.x - w/2f, p.y - h/2f, w, h);
        }

        private Rect BoundsToRect(Bounds b) => new Rect(b.center.x - b.extents.x, b.center.y - b.extents.y, b.size.x, b.size.y);

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float worldX = (gridPos.x - gridPos.y) * (tileWidth / 2f) / pixelsPerUnit;
            float worldY = (gridPos.x + gridPos.y) * (tileHeight / 2f) / pixelsPerUnit;
            return new Vector3(worldX, worldY, 0);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float isoX = worldPos.x * pixelsPerUnit / (tileWidth / 2f);
            float isoY = worldPos.y * pixelsPerUnit / (tileHeight / 2f);
            return new Vector2Int(Mathf.RoundToInt((isoX + isoY) / 2f), Mathf.RoundToInt((isoY - isoX) / 2f));
        }

        public Vector2Int ScreenToGrid(Vector3 screenPos)
        {
            if (mapCamera == null) return new Vector2Int(-1, -1);
            Vector3 worldPos = mapCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            return WorldToGrid(worldPos);
        }

        public bool IsValidGridPosition(Vector2Int gridPos) => gridPos.x >= 0 && gridPos.x < mapWidth && gridPos.y >= 0 && gridPos.y < mapHeight;

        public Transform GetBuildingsParent() => buildingsParent;
        public Vector2Int GetMapSize() => new Vector2Int(mapWidth, mapHeight);
        public Camera GetCamera() => mapCamera;
        public Bounds GetMapBounds() => mapBounds;
        public float GetTileWidth() => tileWidth;
        public float GetTileHeight() => tileHeight;
        public float GetPixelsPerUnit() => pixelsPerUnit;

        public void SetCameraPosition(Vector3 pos)
        {
            cameraTargetPosition = pos;
            if (mapCamera != null) mapCamera.transform.position = new Vector3(pos.x, pos.y, -10f);
        }

        public void SetCameraZoom(float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            if (mapCamera != null) mapCamera.orthographicSize = currentZoom;
        }
    }
}
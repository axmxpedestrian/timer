using UnityEngine;
using UnityEngine.EventSystems;
using PomodoroTimer.Map.Sprite2D;
using PomodoroTimer.Core;

namespace PomodoroTimer.Map
{
    /// <summary>
    /// 地图输入控制器（2D Sprite版本）
    /// 支持：WASD/方向键移动、鼠标拖动、滚轮缩放
    /// </summary>
    public class MapInputController : MonoBehaviour
    {
        public static MapInputController Instance { get; private set; }

        [Header("移动设置")]
        [SerializeField] private float keyboardMoveSpeed = 10f;
        [SerializeField] private float edgeScrollSpeed = 5f;
        [SerializeField] private float edgeScrollThreshold = 20f;
        [SerializeField] private bool enableEdgeScroll = false;

        [Header("拖动设置")]
        [SerializeField] private bool enableLeftClickDrag = true;
        [SerializeField] private bool enableRightClickDrag = true;
        [SerializeField] private bool enableMiddleClickDrag = true;

        [Header("缩放设置")]
        [SerializeField] private float zoomSpeed = 2f;  // 默认值，会被设置覆盖
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float zoomSmoothTime = 0.1f;

        /// <summary>
        /// 获取当前缩放速度（优先从设置读取）
        /// </summary>
        private float CurrentZoomSpeed
        {
            get
            {
                if (DataManager.Instance != null && DataManager.Instance.Settings != null)
                {
                    return DataManager.Instance.Settings.mapZoomSpeed;
                }
                return zoomSpeed;
            }
        }

        private Camera mapCamera;
        private Vector3 targetPosition;
        private float targetZoom;
        private float currentZoom;
        private float zoomVelocity;

        private bool isDragging;
        private Vector3 dragStartWorldPos;
        private Vector3 dragStartCameraPos;

        private Bounds mapBounds;
        private bool isInitialized;

        public event System.Action<Vector2Int> OnTileClicked;
        public event System.Action<Vector2Int> OnTileHovered;

        private Vector2Int lastHoveredTile = new Vector2Int(-1, -1);

        private void Awake()
        {
            if (Instance == null) { Instance = this; }
            else { Destroy(gameObject); }
        }

        private void Start()
        {
            StartCoroutine(DelayedInitialize());
        }

        private System.Collections.IEnumerator DelayedInitialize()
        {
            // 等待地图管理器初始化
            while (IsometricSpriteMapManager.Instance == null)
            {
                yield return null;
            }
            // 额外等待一帧确保相机已设置
            yield return null;

            mapCamera = IsometricSpriteMapManager.Instance.GetCamera();
            if (mapCamera != null)
            {
                currentZoom = mapCamera.orthographicSize;
                targetZoom = currentZoom;
                targetPosition = new Vector3(mapCamera.transform.position.x, mapCamera.transform.position.y, 0);
            }

            mapBounds = IsometricSpriteMapManager.Instance.GetMapBounds();
            isInitialized = true;
            Debug.Log($"[MapInputController] 初始化完成, 地图边界: {mapBounds}");
        }

        private void Update()
        {
            if (!isInitialized || mapCamera == null) return;

            // 检查是否在UI上
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            HandleKeyboardInput();
            HandleKeyboardZoom();
            HandleMouseDrag(isOverUI);
            HandleMouseScroll(isOverUI);
            if (enableEdgeScroll) HandleEdgeScroll();
            if (!isOverUI) HandleTileInteraction();

            UpdateCamera();
        }

        #region 键盘输入

        private void HandleKeyboardInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                Vector3 moveDir = new Vector3(horizontal, vertical, 0).normalized;
                float speedMultiplier = currentZoom / 10f;
                targetPosition += moveDir * keyboardMoveSpeed * speedMultiplier * Time.deltaTime;
                ClampTargetPosition();
            }
        }

        #endregion

        #region 鼠标拖动

        private Vector3 dragStartScreenPos;  // 使用屏幕坐标

        private void HandleMouseDrag(bool isOverUI)
        {
            // 检测拖动开始
            bool startDrag = false;
            if (enableLeftClickDrag && Input.GetMouseButtonDown(0) && !isOverUI) startDrag = true;
            if (enableRightClickDrag && Input.GetMouseButtonDown(1)) startDrag = true;
            if (enableMiddleClickDrag && Input.GetMouseButtonDown(2)) startDrag = true;

            if (startDrag && !isDragging)
            {
                isDragging = true;
                dragStartScreenPos = Input.mousePosition;
                dragStartCameraPos = targetPosition;
            }

            // 拖动中
            if (isDragging)
            {
                bool stillDragging = false;
                if (enableLeftClickDrag && Input.GetMouseButton(0)) stillDragging = true;
                if (enableRightClickDrag && Input.GetMouseButton(1)) stillDragging = true;
                if (enableMiddleClickDrag && Input.GetMouseButton(2)) stillDragging = true;

                if (stillDragging)
                {
                    // 计算屏幕坐标差值，转换为世界坐标偏移
                    Vector3 currentScreenPos = Input.mousePosition;
                    Vector3 screenDelta = dragStartScreenPos - currentScreenPos;

                    // 将屏幕像素差值转换为世界单位
                    // 正交相机：世界单位 = 屏幕像素 * (orthographicSize * 2 / 屏幕高度)
                    float pixelToWorld = (currentZoom * 2f) / Screen.height;
                    Vector3 worldDelta = new Vector3(
                        screenDelta.x * pixelToWorld,
                        screenDelta.y * pixelToWorld,
                        0
                    );

                    targetPosition = dragStartCameraPos + worldDelta;
                    ClampTargetPosition();
                }
                else
                {
                    isDragging = false;
                }
            }
        }

        #endregion

        #region 缩放控制

        private void HandleMouseScroll(bool isOverUI)
        {
            if (mapCamera == null) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // 计算鼠标在屏幕上的归一化位置（用于缩放中心补偿）
                Vector3 mouseScreenPos = Input.mousePosition;
                Vector3 mouseWorldBefore = mapCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));

                // 计算新缩放值并立即应用（不使用平滑）
                float newZoom = currentZoom - scroll * CurrentZoomSpeed * currentZoom;
                newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);

                // 立即更新缩放
                currentZoom = newZoom;
                targetZoom = newZoom;
                zoomVelocity = 0f; // 重置平滑速度
                mapCamera.orthographicSize = newZoom;

                // 缩放后计算鼠标新的世界位置
                Vector3 mouseWorldAfter = mapCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));

                // 补偿相机位置，使鼠标指向的世界位置保持不变
                targetPosition += (mouseWorldBefore - mouseWorldAfter);
                ClampTargetPosition();
            }
        }

        private void HandleKeyboardZoom()
        {
            if (mapCamera == null) return;

            float zoomDelta = 0f;

            // PageUp 放大（缩小 orthographicSize）
            if (Input.GetKey(KeyCode.PageUp))
            {
                zoomDelta = -CurrentZoomSpeed * currentZoom * Time.deltaTime;
            }
            // PageDown 缩小（增大 orthographicSize）
            else if (Input.GetKey(KeyCode.PageDown))
            {
                zoomDelta = CurrentZoomSpeed * currentZoom * Time.deltaTime;
            }

            if (Mathf.Abs(zoomDelta) > 0.001f)
            {
                // 以屏幕中心为缩放中心
                float newZoom = currentZoom + zoomDelta;
                newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);

                currentZoom = newZoom;
                targetZoom = newZoom;
                zoomVelocity = 0f;
                mapCamera.orthographicSize = newZoom;

                ClampTargetPosition();
            }
        }

        #endregion

        #region 边缘滚动

        private void HandleEdgeScroll()
        {
            // 防止与 UI 冲突
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mousePos = Input.mousePosition;
            Vector2 moveDir = Vector2.zero;

            if (mousePos.x < edgeScrollThreshold)
                moveDir.x = -1;
            else if (mousePos.x > Screen.width - edgeScrollThreshold)
                moveDir.x = 1;

            if (mousePos.y < edgeScrollThreshold)
                moveDir.y = -1;
            else if (mousePos.y > Screen.height - edgeScrollThreshold)
                moveDir.y = 1;

            if (moveDir != Vector2.zero)
            {
                // 可选：移除 zoom 补偿，或改为反比
                // float speedMultiplier = 10f / Mathf.Max(currentZoom, 1f);
                float speed = edgeScrollSpeed; // 先用固定速度测试

                Vector3 delta = new Vector3(moveDir.x, moveDir.y, 0) * speed * Time.deltaTime;
                targetPosition += delta;

                ClampTargetPosition();
            }
        }

        #endregion

        #region 地砖交互

        private void HandleTileInteraction()
        {
            if (IsometricSpriteMapManager.Instance == null) return;

            Vector2Int hoveredTile = GetTileUnderMouse();

            if (hoveredTile != lastHoveredTile)
            {
                lastHoveredTile = hoveredTile;
                if (IsometricSpriteMapManager.Instance.IsValidGridPosition(hoveredTile))
                {
                    OnTileHovered?.Invoke(hoveredTile);
                }
            }

            // 只有非拖动状态下的点击才触发
            if (Input.GetMouseButtonUp(0) && !isDragging)
            {
                if (IsometricSpriteMapManager.Instance.IsValidGridPosition(hoveredTile))
                {
                    OnTileClicked?.Invoke(hoveredTile);
                }
            }
        }

        #endregion

        #region 相机更新

        private void UpdateCamera()
        {
            // 平滑缩放
            currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, zoomSmoothTime);
            mapCamera.orthographicSize = currentZoom;

            // 缩放后重新限制位置
            ClampTargetPosition();

            // 更新相机位置
            mapCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, -10f);
        }

        private void ClampTargetPosition()
        {
            if (mapCamera == null) return;

            var mapManager = IsometricSpriteMapManager.Instance;
            if (mapManager == null) return;

            // 获取地图四个角的世界坐标（菱形）
            Vector2Int mapSize = mapManager.GetMapSize();
            Vector3 cornerTop = mapManager.GridToWorld(new Vector2Int(0, 0));           // 上角
            Vector3 cornerRight = mapManager.GridToWorld(new Vector2Int(mapSize.x, 0)); // 右角
            Vector3 cornerBottom = mapManager.GridToWorld(new Vector2Int(mapSize.x, mapSize.y)); // 下角
            Vector3 cornerLeft = mapManager.GridToWorld(new Vector2Int(0, mapSize.y));  // 左角

            // 计算可视区域的一半尺寸
            float halfVisibleWidth = currentZoom * mapCamera.aspect;
            float halfVisibleHeight = currentZoom;

            // 计算一个网格的世界尺寸作为边界预留
            float tileWidth = mapManager.GetTileWidth() / mapManager.GetPixelsPerUnit();
            float tileHeight = mapManager.GetTileHeight() / mapManager.GetPixelsPerUnit();
            float boundaryPadding = Mathf.Max(tileWidth, tileHeight); // 预留1个网格边长

            // 简化处理：使用菱形的中心和半对角线长度
            Vector3 mapCenter = (cornerTop + cornerBottom) / 2f;
            float diamondHalfWidth = Mathf.Abs(cornerRight.x - cornerLeft.x) / 2f + boundaryPadding;  // X方向半宽 + 预留
            float diamondHalfHeight = Mathf.Abs(cornerTop.y - cornerBottom.y) / 2f + boundaryPadding; // Y方向半高 + 预留

            // 计算相机可移动的菱形范围（内缩可视区域大小）
            float allowedHalfWidth = Mathf.Max(0, diamondHalfWidth - halfVisibleWidth);
            float allowedHalfHeight = Mathf.Max(0, diamondHalfHeight - halfVisibleHeight);

            // 将目标位置转换为相对于地图中心的坐标
            Vector3 relativePos = targetPosition - mapCenter;

            // 菱形边界检测：|x/a| + |y/b| <= 1
            // 其中 a = allowedHalfWidth, b = allowedHalfHeight
            if (allowedHalfWidth > 0.01f && allowedHalfHeight > 0.01f)
            {
                float normalizedX = Mathf.Abs(relativePos.x) / allowedHalfWidth;
                float normalizedY = Mathf.Abs(relativePos.y) / allowedHalfHeight;
                float sum = normalizedX + normalizedY;

                if (sum > 1f)
                {
                    // 超出菱形边界，投影到边界上
                    // 保持方向，缩放到边界
                    float scale = 1f / sum;
                    relativePos.x *= scale;
                    relativePos.y *= scale;
                }
            }
            else
            {
                // 地图太小，锁定在中心
                relativePos = Vector3.zero;
            }

            targetPosition = mapCenter + relativePos;
            targetPosition.z = 0;
        }

        #endregion

        #region 辅助方法

        private Vector3 GetMouseWorldPosition()
        {
            if (mapCamera == null) return Vector3.zero;
            Vector3 mousePos = Input.mousePosition;
            // 正交相机下，z 值表示从相机到目标平面的距离
            // 相机在 z=-10，地图在 z=0，所以距离是 10
            mousePos.z = 10f;
            return mapCamera.ScreenToWorldPoint(mousePos);
        }

        private Vector2Int GetTileUnderMouse()
        {
            if (IsometricSpriteMapManager.Instance == null) return new Vector2Int(-1, -1);
            return IsometricSpriteMapManager.Instance.ScreenToGrid(Input.mousePosition);
        }

        #endregion

        #region 公共接口

        public void MoveCameraTo(Vector2Int gridPos)
        {
            if (IsometricSpriteMapManager.Instance == null) return;
            targetPosition = IsometricSpriteMapManager.Instance.GridToWorld(gridPos);
            ClampTargetPosition();
        }

        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        public Camera GetCamera() => mapCamera;

        /// <summary>
        /// 启用/禁用鼠标左键拖动视角
        /// </summary>
        public void SetLeftClickDragEnabled(bool enabled)
        {
            enableLeftClickDrag = enabled;
            // 如果正在左键拖动中，立即中断
            if (!enabled && isDragging && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
            {
                isDragging = false;
            }
        }

        #endregion
    }
}
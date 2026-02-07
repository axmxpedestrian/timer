using UnityEngine;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map.Sprite2D
{
    public class BuildingSpriteInstance : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteAnimationController animController;

        private BuildingSpriteData buildingData;
        private Vector2Int gridPosition;
        private int buildingId;
        private BuildingAnimationState currentState = BuildingAnimationState.Idle;

        public int BuildingId => buildingId;
        public Vector2Int GridPosition => gridPosition;
        public BuildingSpriteData BuildingData => buildingData;
        public BuildingAnimationState CurrentState => currentState;
        public SpriteRenderer Renderer => spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (animController == null)
                animController = GetComponent<SpriteAnimationController>();
            if (animController == null)
                animController = gameObject.AddComponent<SpriteAnimationController>();
        }

        public void Initialize(int id, BuildingSpriteData data, Vector2Int gridPos)
        {
            buildingId = id;
            buildingData = data;
            gridPosition = gridPos;

            if (data != null)
            {
                gameObject.name = $"Building_{data.buildingName}_{id}";
                Vector3 worldPos = IsometricSpriteMapManager.Instance.GridToWorld(gridPos);
                worldPos.y += data.yOffset;
                transform.position = worldPos;
                IsometricSortingHelper.ApplyBuildingSorting(spriteRenderer, gridPos, data.heightLevel);
                SetState(BuildingAnimationState.Idle);
            }
        }

        public void SetState(BuildingAnimationState state)
        {
            if (buildingData == null) return;
            currentState = state;

            Sprite[] frames = GetFramesForState(state);
            if (frames != null && frames.Length > 0)
            {
                bool shouldLoop = state != BuildingAnimationState.Destroyed;
                animController.PlayState(state, frames, shouldLoop);
            }
            else if (buildingData.idleFrames != null && buildingData.idleFrames.Length > 0)
            {
                animController.SetSprite(buildingData.idleFrames[0]);
            }
        }

        private Sprite[] GetFramesForState(BuildingAnimationState state)
        {
            if (buildingData == null) return null;
            return state switch
            {
                BuildingAnimationState.Idle => buildingData.idleFrames,
                BuildingAnimationState.Active => buildingData.activeFrames,
                BuildingAnimationState.Building => buildingData.buildingFrames,
                BuildingAnimationState.Destroyed => buildingData.destroyedFrames,
                _ => buildingData.idleFrames
            };
        }

        public void SetGridPosition(Vector2Int newPos)
        {
            gridPosition = newPos;
            Vector3 worldPos = IsometricSpriteMapManager.Instance.GridToWorld(newPos);
            if (buildingData != null) worldPos.y += buildingData.yOffset;
            transform.position = worldPos;
            IsometricSortingHelper.ApplyBuildingSorting(spriteRenderer, newPos, buildingData?.heightLevel ?? 0);
        }

        public void Reset()
        {
            buildingData = null;
            buildingId = -1;
            gridPosition = Vector2Int.zero;
            currentState = BuildingAnimationState.Idle;
            animController?.Reset();
            if (spriteRenderer != null) spriteRenderer.sprite = null;
            gameObject.name = "Building_Pooled";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
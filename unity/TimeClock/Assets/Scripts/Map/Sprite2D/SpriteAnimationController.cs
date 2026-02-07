using UnityEngine;

namespace PomodoroTimer.Map.Sprite2D
{
    public enum BuildingAnimationState
    {
        Idle,
        Active,
        Building,
        Destroyed
    }

    public class SpriteAnimationController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float frameRate = 8f;

        private Sprite[] currentFrames;
        private int currentFrameIndex;
        private float frameTimer;
        private bool isPlaying;
        private bool loop = true;
        private BuildingAnimationState currentState = BuildingAnimationState.Idle;

        public BuildingAnimationState CurrentState => currentState;
        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!isPlaying || currentFrames == null || currentFrames.Length == 0)
                return;

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / frameRate;

            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex++;

                if (currentFrameIndex >= currentFrames.Length)
                {
                    if (loop)
                        currentFrameIndex = 0;
                    else
                    {
                        currentFrameIndex = currentFrames.Length - 1;
                        isPlaying = false;
                    }
                }

                spriteRenderer.sprite = currentFrames[currentFrameIndex];
            }
        }

        public void Play(Sprite[] frames, bool shouldLoop = true)
        {
            if (frames == null || frames.Length == 0) return;

            currentFrames = frames;
            currentFrameIndex = 0;
            frameTimer = 0f;
            loop = shouldLoop;
            isPlaying = true;

            if (spriteRenderer != null && frames.Length > 0)
                spriteRenderer.sprite = frames[0];
        }

        public void PlayState(BuildingAnimationState state, Sprite[] frames, bool shouldLoop = true)
        {
            currentState = state;
            Play(frames, shouldLoop);
        }

        public void Stop()
        {
            isPlaying = false;
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void Resume()
        {
            if (currentFrames != null && currentFrames.Length > 0)
                isPlaying = true;
        }

        public void SetFrame(int index)
        {
            if (currentFrames == null || currentFrames.Length == 0) return;
            currentFrameIndex = Mathf.Clamp(index, 0, currentFrames.Length - 1);
            if (spriteRenderer != null)
                spriteRenderer.sprite = currentFrames[currentFrameIndex];
        }

        public void SetFrameRate(float fps)
        {
            frameRate = Mathf.Max(0.1f, fps);
        }

        public void SetSprite(Sprite sprite)
        {
            Stop();
            if (spriteRenderer != null)
                spriteRenderer.sprite = sprite;
        }

        public void Reset()
        {
            currentFrames = null;
            currentFrameIndex = 0;
            frameTimer = 0f;
            isPlaying = false;
            currentState = BuildingAnimationState.Idle;
        }
    }
}
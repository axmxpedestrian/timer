using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

namespace PomodoroTimer.Core
{
    /// <summary>
    /// 全局输入管理器
    /// 统一管理游戏键盘输入的屏蔽：当 UI 面板（如 TaskEditPanel、SettingsPanel、ConfirmDialog）
    /// 激活时，阻止键盘事件传递到地图移动、建筑放置等游戏逻辑。
    ///
    /// 两种屏蔽机制：
    /// 1. 自动检测：EventSystem 当前焦点是否为 InputField（处理文字输入时自动屏蔽）
    /// 2. 手动计数：UI 面板通过 PushInputBlock / PopInputBlock 显式屏蔽
    /// </summary>
    public class GlobalInputManager : MonoBehaviour
    {
        public static GlobalInputManager Instance { get; private set; }

        // 手动屏蔽计数器（支持多面板同时打开）
        private int blockCount = 0;

        /// <summary>
        /// 游戏键盘输入是否被屏蔽
        /// </summary>
        public bool IsGameInputBlocked
        {
            get
            {
                // 机制1：手动屏蔽
                if (blockCount > 0) return true;

                // 机制2：自动检测当前焦点是否为输入框
                return IsInputFieldFocused();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 压入一层输入屏蔽（UI 面板打开时调用）
        /// </summary>
        public void PushInputBlock()
        {
            blockCount++;
        }

        /// <summary>
        /// 弹出一层输入屏蔽（UI 面板关闭时调用）
        /// </summary>
        public void PopInputBlock()
        {
            blockCount = Mathf.Max(0, blockCount - 1);
        }

        /// <summary>
        /// 重置屏蔽计数器（安全兜底，防止异常情况导致永久屏蔽）
        /// </summary>
        public void ResetBlockCount()
        {
            blockCount = 0;
        }

        /// <summary>
        /// 检测 EventSystem 当前焦点是否为输入框组件
        /// </summary>
        private bool IsInputFieldFocused()
        {
            if (EventSystem.current == null) return false;

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return false;

            // 检查 TMP_InputField
            if (selected.GetComponent<TMP_InputField>() != null)
                return true;

            // 检查 Unity 原生 InputField
            if (selected.GetComponent<InputField>() != null)
                return true;

            return false;
        }
    }
}

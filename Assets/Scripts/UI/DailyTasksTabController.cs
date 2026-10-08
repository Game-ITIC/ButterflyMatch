using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Switches DailyRewardsPanel and Tasks Content Panel from the existing tab buttons.
    /// </summary>
    public sealed class DailyTasksTabController : MonoBehaviour
    {
        [SerializeField] private GameObject dailyPanel;
        [SerializeField] private GameObject tasksPanel;
        [SerializeField] private Button dailyTabButton;
        [SerializeField] private Button tasksTabButton;

        private void OnEnable()
        {
            Wire(dailyTabButton, ShowDaily);
            Wire(tasksTabButton, ShowTasks);
            ShowTasks();
        }

        private void OnDisable()
        {
            Unwire(dailyTabButton, ShowDaily);
            Unwire(tasksTabButton, ShowTasks);
        }

        public void ShowDaily()
        {
            SetActive(tasksPanel, false);
            SetActive(dailyPanel, true);
        }

        public void ShowTasks()
        {
            SetActive(dailyPanel, false);
            SetActive(tasksPanel, true);
        }

        private static void Wire(Button button, UnityAction action)
        {
            if(button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void Unwire(Button button, UnityAction action)
        {
            if(button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if(target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}

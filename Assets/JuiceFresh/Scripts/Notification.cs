using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
using TMPro;
using System;

public class Notification : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    private bool startTimer;
    private float TotalTimeForRestLife = 15f * 60; // 15 minutes for restore life

    void Awake()
    {
        // Запрашиваем разрешение на отправку уведомлений
        RequestAuthorization();

        // Создаем канал для уведомлений
        AndroidNotificationChannel channel = new AndroidNotificationChannel
        {
            Id = "Push",
            Name = "Push Notifications",
            Description = "Notification of news",
            Importance = Importance.Default,
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        // Запускаем отправку уведомлений через заданные интервалы времени
        ScheduleDailyNotifications();
    }

    void Start()
    {
        // text = GetComponent<Text>();
        // TotalTimeForRestLife = InitScript.Instance.TotalTimeForRestLifeHours * 60 * 60 +
        //                        InitScript.Instance.TotalTimeForRestLifeMin * 60 +
        //                        InitScript.Instance.TotalTimeForRestLifeSec;

        // if (InitScript.lifes < InitScript.Instance.CapOfLife)
        // {
        //     startTimer = true;
        // }
    }

    void Update()
    {
        // if (startTimer)
        // {
        //     TimeCount(Time.deltaTime);
        // }
        //
        // if (gameObject.activeSelf)
        // {
        //     if (InitScript.lifes < InitScript.Instance.CapOfLife)
        //     {
        //         int minutes = Mathf.FloorToInt(InitScript.RestLifeTimer / 60F);
        //         int seconds = Mathf.FloorToInt(InitScript.RestLifeTimer - minutes * 60);
        //
        //         text.enabled = true;
        //         text.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        //
        //         InitScript.timeForReps = text.text;
        //     }
        //     else
        //     {
        //         text.text = "   Full";
        //     }
        // }
    }

    private void TimeCount(float tick)
    {
        // if (InitScript.RestLifeTimer <= 0)
        //     ResetTimer();
        //
        // InitScript.RestLifeTimer -= tick;
        // if (InitScript.RestLifeTimer <= 1 && InitScript.lifes < InitScript.Instance.CapOfLife)
        // {
        //     InitScript.Instance.AddLife(1);
        //     if (InitScript.lifes == InitScript.Instance.CapOfLife)
        //     {
        //         ScheduleFullLivesNotification(); // Планируем уведомление после восстановления всех жизней
        //     }
        //
        //     ResetTimer();
        // }
    }

    private void ResetTimer()
    {
        InitScript.RestLifeTimer = TotalTimeForRestLife;
    }

    private void ScheduleDailyNotifications()
    {
        // Удаляем все предыдущие уведомления, чтобы избежать дублирования
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        // Устанавливаем уведомление на первую половину дня
        SendNotificationAtTime(10, 0); // 10:00 AM

        // Устанавливаем уведомление на вторую половину дня
        SendNotificationAtTime(18, 0); // 6:00 PM
    }

    private void SendNotificationAtTime(int hour, int minute)
    {
        System.DateTime now = System.DateTime.Now;
        System.DateTime fireTime = new System.DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

        // Если текущее время уже прошло, устанавливаем уведомление на следующий день
        if (fireTime < now)
        {
            fireTime = fireTime.AddDays(1);
        }

        AndroidNotification notification = new AndroidNotification()
        {
            Title = "Butterflies are waiting for you",
            Text = "Discover new parts of the park and improve the old ones, make your park even better!",
            FireTime = fireTime,
            SmallIcon = "small_icon",
        };

        AndroidNotificationCenter.SendNotification(notification, "Push");
    }

    private void ScheduleFullLivesNotification()
    {
        var notification = new AndroidNotification
        {
            Title = "All Lives Restored!",
            Text = "Your lives are fully restored! Come back and play!",
            FireTime = DateTime.Now.AddSeconds(1), // Отправка уведомления через 1 секунду
            SmallIcon = "small_icon",
        };

        AndroidNotificationCenter.SendNotification(notification, "Push");
    }

    public void RequestAuthorization()
    {
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            InitScript.DateOfExit = DateTime.Now.ToString();
        }
        else
        {
            startTimer = false;
        }
    }

    void OnApplicationQuit()
    {
        InitScript.DateOfExit = DateTime.Now.ToString();
    }
}
#endif

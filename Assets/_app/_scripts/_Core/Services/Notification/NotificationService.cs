﻿using Antura.Database;
using Antura.Modules.Notifications;
using System;
using UnityEngine;

namespace Antura.Core.Services.Notification
{
    public class NotificationService
    {
        public const string ChannelId = "game_channel0";
        private bool inizialized = false;

        public NotificationService()
        {
        }

        public void Init()
        {
            if (!inizialized) {
                Debug.Log("NotificationService Init");
                var channel = new GameNotificationChannel(ChannelId, "Default Game Channel", "Generic notifications");
                GameNotificationsManager.I.Initialize(channel);
                inizialized = true;
            }
        }

        #region main
        /// <summary>
        /// automatically call everything to setup Notifications at AppSuspended
        /// </summary>
        public void AppSuspended()
        {
            PrepareNextLocalNotification();
            GameNotificationsManager.I.ChangeApplicationFocus(false);
        }

        /// <summary>
        /// automatically restore all Notifications at AppResumed
        /// </summary>
        public void AppResumed()
        {
            if (!GameNotificationsManager.I.Initialized) {
                Init();
            }
            GameNotificationsManager.I.CancelAllNotifications();
            GameNotificationsManager.I.ChangeApplicationFocus(true);
        }
        #endregion

        private void PrepareNextLocalNotification()
        {
            //DeleteAllLocalNotifications();
            //Debug.Log("Next Local Notifications prepared");
            var arabicString = LocalizationManager.GetLocalizationData(LocalizationDataId.UI_Notification_24h);
            ScheduleSimple(
                GetTomorrow(),
                "Antura and the Letters",
                arabicString.Arabic
            );

            //NotificationManager.ScheduleSimpleWithAppIcon(
            //    TimeSpan.FromSeconds(60),
            //    "Antura and the Letters",
            //    "Test notification after closing the app [60 seconds]",
            //    Color.blue
            //);
        }

        #region direct plugins methods

        /// <summary>
        /// Schedule notification with app icon.
        /// </summary>
        /// <param name="smallIcon">List of build-in small icons: notification_icon_bell (default), notification_icon_clock, notification_icon_heart, notification_icon_message, notification_icon_nut, notification_icon_star, notification_icon_warning.</param>
        public void ScheduleSimple(DateTime deliveryTime, string title, string message)
        {
            IGameNotification notification = GameNotificationsManager.I.CreateNotification();
            notification.Title = title;
            notification.Body = message;
            notification.DeliveryTime = deliveryTime;
            notification.LargeIcon = "icon_antura";
            GameNotificationsManager.I.ScheduleNotification(notification);
        }

        public void DeleteAllLocalNotifications()
        {

        }
        #endregion

        #region time utilities
        private DateTime GetTomorrow()
        {
            return DateTime.Now.AddHours(20);
        }

        private DateTime GetDateTimeInMinues(int minutes)
        {
            return DateTime.Now.ToLocalTime() + TimeSpan.FromMinutes(minutes);
        }

        private int CalculateSecondsToTomorrowMidnight()
        {
            TimeSpan ts = DateTime.Today.AddDays(2).Subtract(DateTime.Now);
            return (int)ts.TotalSeconds;
        }
        #endregion

        #region tests
        public void TestLocalNotification()
        {
            Debug.Log("TestLocalNotification");
            //Debug.Log("Tomorrows midnight is in " + CalculateSecondsToTomorrowMidnight() + " seconds");
            var arabicString = LocalizationManager.GetLocalizationData(LocalizationDataId.UI_Notification_24h);
            ScheduleSimple(
                GetDateTimeInMinues(1),
                "Antura and the Letters",
                arabicString.Arabic
            );
        }
        #endregion
    }
}
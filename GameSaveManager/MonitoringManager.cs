using System;
using System.Timers;
using System.Text.Json.Serialization;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata.Ecma335;

namespace GameSaveManager
{
    // ********************************************************************************************************************
    // ENUM: Monitoring Mode
    // ********************************************************************************************************************
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MonitoringMode
    {
        Off,        // Monitoring is off 
        Passive,     // Monitoring is active, but backups are manually triggered by the user
        Active        // Monitoring is active, and backups are automatically triggered by the system
    }

    // ********************************************************************************************************************
    // ENUM: MonitoringStatus
    // ********************************************************************************************************************
    public enum MonitoringStatus
    {
        Playing,     // Game progressed since last backup
        Restored,    // Game has been restored, can be reverted to the previous state
        BackedUp     // Game has not progressed since last backup taken
    }

    // ********************************************************************************************************************
    // STRUCT: MonitoringPoint
    // ********************************************************************************************************************
    public struct MonitoringPoint
    {
        public DateTime GameSaveTime;
        public DateTime LastBackupTime;
        public bool hasRevertDirectory;
        public int AutoBackupFileCounter;
        public DateTime LastAutoBackupTime;
    }
    
    // ********************************************************************************************************************
    // CLASS: Monitoring Manager
    // * This class is used to manage the monitoring of the game saves. It can trigger the backup of the game.
    // ********************************************************************************************************************
    public class MonitoringManager : INotifyPropertyChanged
    {
        private readonly GameConfig gameConfig;
        private System.Timers.Timer monitoringTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        public MonitoringManager(GameConfig gameConfig)
        {
            this.gameConfig = gameConfig;
        }
        public MonitoringPoint point;
        public MonitoringPoint prevPoint;

        public MonitoringMode monitoringMode
        {
            get => _monitoringMode;
            set
            {
                // Switch off monitoring if it is already on and going off               
                if (isMonitoring && value == MonitoringMode.Off)
                {
                    Stop(null);
                }
                // Switch on monitoring if it is off and going on
                else if (!isMonitoring && value != MonitoringMode.Off)
                {
                    Start(null);
                }
                _monitoringMode = value;

                // Trigger the OnPropertyChanged event
                OnPropertyChanged(nameof(monitoringMode));
            }
        }

        private MonitoringMode _monitoringMode = MonitoringMode.Off;
        public bool isMonitoring => _monitoringMode != MonitoringMode.Off;
        public bool isMonitoringAuto => _monitoringMode == MonitoringMode.Active;

        private MonitoringStatus _monitoringStatus;
        public MonitoringStatus monitoringStatus
        {
            get => _monitoringStatus;
            set
            {
                if (_monitoringStatus != value)
                {
                    _monitoringStatus = value;
                    OnPropertyChanged(nameof(monitoringStatus));
                }
            }
        }

        private int _monitorFileCounter;
        public int MonitorFileCounter
        {
            get => _monitorFileCounter;
            set
            {
                _monitorFileCounter = value;
                OnPropertyChanged(nameof(MonitorFileCounter));
            }
        }

        public void Start(PropertyChangedEventHandler propertyChangedHandler)
        {
            if (monitoringTimer == null)
            {
                monitoringTimer = new System.Timers.Timer(1000); // 1 second interval
                monitoringTimer.Elapsed += MonitoringTimer_Elapsed;
            }
            monitoringTimer.Start();
            if (propertyChangedHandler != null)
            {
                PropertyChanged += propertyChangedHandler;
            }
        }

        public void Stop(PropertyChangedEventHandler propertyChangedHandler)
        {
            if (monitoringTimer != null)
            {
                monitoringTimer.Stop();
            }
            if (propertyChangedHandler != null)
            {
                PropertyChanged -= propertyChangedHandler;
            }
        }

        public void CycleMonitoringMode()
        {
            switch (monitoringMode)
            {
                case MonitoringMode.Off:
                    monitoringMode = MonitoringMode.Passive;
                    break;
                case MonitoringMode.Passive:
                    monitoringMode = MonitoringMode.Active;
                    break;
                case MonitoringMode.Active:
                    monitoringMode = MonitoringMode.Off;
                    break;
            }
        }

        private void MonitoringTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // get latest monitoring info
            refreshMonitoringPoint(null);

            // Debounce no change in latest check of game save time
            if (point.GameSaveTime == prevPoint.GameSaveTime)
            {
                return;
            }

            // Trigger property change if Monitor Point is differnet
            if (point.GameSaveTime != prevPoint.GameSaveTime || point.LastBackupTime != prevPoint.LastBackupTime)
            {
                OnPropertyChanged(nameof(point));
            }
            
            // Debounce multiple file change events within 10 seconds
            if ((point.GameSaveTime - prevPoint.GameSaveTime).TotalMilliseconds < 10000)
            {
                return;
            }

            // Marshal the call to the UI thread
            SynchronizationContextManager.Context.Post(_ =>
            {
                // If Automatic monitoring is on, then backup the game
                if (isMonitoringAuto)
                {
                    MonitorFileCounter = (MonitorFileCounter + 1) % 1000;
                    string backupName = $"{MonitorFileCounter:D3}";
                    gameConfig.Strategy.HandleMonitorBackup(backupName, null);
                }
            }, null);

            // Store the previous MonitorPoint
            prevPoint = point;

        }

        public void refreshMonitoringPoint(Label lblError)
        {
            point = gameConfig.Strategy.getMonitorPointInfo(lblError);

            if (point.GameSaveTime > point.LastBackupTime)
            {
                monitoringStatus = MonitoringStatus.Playing;
            }
            else if (point.GameSaveTime == point.LastBackupTime)
            {
                monitoringStatus = point.hasRevertDirectory ? MonitoringStatus.Restored : MonitoringStatus.BackedUp;
            }
            else
            {
                 MessageUtils.SetInfoMessage(lblError, "Game Save Time is before latest backup.");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            SynchronizationContextManager.Context.Post(_ =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }, null);
        }
    }
}

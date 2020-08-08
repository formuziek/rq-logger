namespace RQLogger
{
    using System.Linq;
    using Android.App;
    using Android.Content;
    using Android.Hardware;
    using Android.Locations;
    using Android.OS;
    using Android.Runtime;

    /// <summary>
    /// Location & acceleration logger service.
    /// </summary>
    [Service]
    public class LoggerService : Service, ILocationListener, ISensorEventListener
    {
        private const string LOGGER_NOTIFICATION_CHANNEL = "RQ_LOGGER";
        private const string LOGGER_NOTIFICATION_TITLE = "RQ Logger";
        private const string LOGGER_NOTIFICATION_TEXT = "Logging location & acceleration data";
        private const string LOG_FILE = "rq.log";
        private const int ACCEL_VALUE = 0;
        private const int ROTAT_VALUE = 1;

        private LocationManager _locationManager;
        private SensorManager _sensorManager;
        private Sensor _accelerationSensor;
        private Sensor _rotationSensor;
        private DataEntry _liveDataEntry;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override bool OnUnbind(Intent intent)
        {
            StopForeground(true);
            return base.OnUnbind(intent);
        }

        public override void OnDestroy()
        {
            this.UnregisterListeners();
            base.OnDestroy();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // Create notification channel & notification to properly start a foreground service.
            var notificationChannel = new NotificationChannel(LOGGER_NOTIFICATION_CHANNEL, LOGGER_NOTIFICATION_TITLE, NotificationImportance.High);
            NotificationManager notificationManager = GetSystemService(Android.Content.Context.NotificationService) as NotificationManager;
            notificationManager.CreateNotificationChannel(notificationChannel);
            var notification = new Notification.Builder(this, LOGGER_NOTIFICATION_CHANNEL)
                .SetContentTitle(LOGGER_NOTIFICATION_TITLE)
                .SetContentText(LOGGER_NOTIFICATION_TEXT)
                .SetOngoing(true)
                .Build();

            this.StartForeground(1261, notification);

            this.RegisterListeners();

            return StartCommandResult.Sticky;
        }

        /// <summary>
        /// Loads sensor & location services and starts listening for updates.
        /// </summary>
        private void RegisterListeners()
        {
            if (_sensorManager == null)
            {
                _sensorManager = GetSystemService(Android.Content.Context.SensorService) as SensorManager;
                _accelerationSensor = _sensorManager.GetDefaultSensor(SensorType.Accelerometer);
                _rotationSensor = _sensorManager.GetDefaultSensor(SensorType.RotationVector);
            }

            if (_locationManager == null)
            {
                _locationManager = GetSystemService(Android.Content.Context.LocationService) as LocationManager;
            }

            if (_liveDataEntry == null)
            {
                _liveDataEntry = new DataEntry();
            }

            _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 250, 1, this);
            _sensorManager.RegisterListener(this, _accelerationSensor, SensorDelay.Normal);
            _sensorManager.RegisterListener(this, _rotationSensor, SensorDelay.Normal);
        }

        /// <summary>
        /// Unregisters sensor & location update listeners.
        /// </summary>
        private void UnregisterListeners()
        {
            _locationManager.RemoveUpdates(this);
            _sensorManager.UnregisterListener(this);
        }

        /// <summary>
        /// Receives updates from location service.
        /// Flushes the current data entry to log & creates a new one.
        /// </summary>
        /// <remarks>
        /// TODO: Check if there's not a memory leak here.
        /// </remarks>
        /// <param name="location">Location update.</param>
        public void OnLocationChanged(Android.Locations.Location location)
        {
            LoggingProvider.Log("Received location update");

            _liveDataEntry.Latitude = location?.Latitude;
            _liveDataEntry.Longitude = location?.Longitude;

            this.AppendToLog();

            LoggingProvider.Log($"Flushed data entry, Lon: {_liveDataEntry.Longitude}, Lat: {_liveDataEntry.Latitude}, Values: {_liveDataEntry.SensorValues.Count}");

            _liveDataEntry = new DataEntry();
        }

        /// <summary>
        /// Appends current data set to a rolling log file.
        /// </summary>
        private void AppendToLog()
        {
            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendLine($"C:{_liveDataEntry.Latitude};{_liveDataEntry.Longitude}");
            foreach (var sensorValue in _liveDataEntry.SensorValues)
            {
                if (sensorValue.Type == ACCEL_VALUE)
                {
                    stringBuilder.AppendLine($"A:{string.Join(";", sensorValue.Values)}");
                }

                if (sensorValue.Type == ROTAT_VALUE)
                {
                    stringBuilder.AppendLine($"R:{string.Join(";", sensorValue.Values)}");
                }
            }

            var externalStorageLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            string path = System.IO.Path.Combine(externalStorageLocation, LOG_FILE);
            System.IO.File.AppendAllText(path, stringBuilder.ToString());
        }

        public void OnProviderDisabled(string provider)
        {
            // throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            // throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            // throw new NotImplementedException();
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            // throw new NotImplementedException();
        }

        /// <summary>
        /// Receives updates from acceleration sensor.
        /// </summary>
        /// <param name="e">Sensor event.</param>
        public void OnSensorChanged(SensorEvent e)
        {
            if (e.Sensor.Equals(_accelerationSensor))
            {
                LoggingProvider.Log("Received accelerometer update");
                LoggingProvider.Log($"x: {e.Values[0]}, y: {e.Values[1]}, z: {e.Values[2]}");
                _liveDataEntry.SensorValues.Add(new SensorEntry { Type = ACCEL_VALUE, Values = e.Values.Select(r => r).ToList() });
            }
            
            if (e.Sensor.Equals(_rotationSensor))
            {
                LoggingProvider.Log("Received orientation sensor update");
                _liveDataEntry.SensorValues.Add(new SensorEntry { Type = ROTAT_VALUE, Values = e.Values.Select(r => r).ToList() });
            }
        }
    }
}
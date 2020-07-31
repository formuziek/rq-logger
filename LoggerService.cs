namespace RQLogger
{
    using System;
    using System.Linq;
    using Android.App;
    using Android.Content;
    using Android.Hardware;
    using Android.Locations;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Java.Lang;

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

        private LocationManager _locationManager;
        private SensorManager _sensorManager;
        private Sensor _accelerationSensor;
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

        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
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
                _accelerationSensor = _sensorManager.GetDefaultSensor(SensorType.LinearAcceleration);
            }

            if (_locationManager == null)
            {
                _locationManager = GetSystemService(Android.Content.Context.LocationService) as LocationManager;
            }

            if (_liveDataEntry == null)
            {
                _liveDataEntry = new DataEntry();
            }

            _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 5, 0, this);
            _sensorManager.RegisterListener(this, this._accelerationSensor, SensorDelay.Normal);
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
            System.Diagnostics.Debug.WriteLine("Received location update");
            _liveDataEntry.Latitude = location?.Latitude;
            _liveDataEntry.Longitude = location?.Longitude;

            this.AppendToLog();

            System.Diagnostics.Debug.WriteLine($"Flushed data entry, Lon: {_liveDataEntry.Longitude}, Lat: {_liveDataEntry.Latitude}, Readings: {_liveDataEntry.AccelerometerReadings.Count}");

            _liveDataEntry = new DataEntry();
        }

        /// <summary>
        /// Appends current data set to a rolling log file.
        /// </summary>
        private void AppendToLog()
        {
            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendLine($"LATLON:{_liveDataEntry.Latitude};{_liveDataEntry.Longitude}");
            foreach (var accelerationEntry in _liveDataEntry.AccelerometerReadings)
            {
                stringBuilder.AppendLine($"ACCELE:{string.Join(";", accelerationEntry)}");
            }

            var externalStorageLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            string path = System.IO.Path.Combine(externalStorageLocation, LOG_FILE);
            System.IO.File.AppendAllText(path, stringBuilder.ToString());
            System.Diagnostics.Debug.Write(stringBuilder.ToString());
        }

        public void OnProviderDisabled(string provider)
        {
            // throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            // throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            // throw new NotImplementedException();
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            // throw new NotImplementedException();
        }

        /// <summary>
        /// Receives updates from acceleration sensor.
        /// </summary>
        /// <param name="e">Sensor event.</param>
        public void OnSensorChanged(SensorEvent e)
        {
            System.Diagnostics.Debug.WriteLine("Received accelerometer update");
            _liveDataEntry.AccelerometerReadings.Add(e.Values.Select(r => r).ToList());
        }
    }
}
namespace RQLogger
{
    using System;
    using Android;
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Support.V4.App;
    using Android.Support.V4.Content;
    using Android.Support.V7.App;
    using Android.Widget;

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const int LOCATION_PERMISSIONS_REQUEST = 9121;
        private const int FOREGROUND_SERVICE_PERMISSIONS_REQUEST = 9122;
        private const int EXTERNAL_STORAGE_PERMISSIONS_REQUEST = 9123;

        private bool _locationPermitted = false;
        private bool _foregroundServicePermitted = false;
        private bool _externalStoragePermitted = false;
        private bool _isLoggingActive = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.main);

            Button toggleButton = FindViewById<Button>(Resource.Id.togglebutton);
            toggleButton.Click += OnToggleClick;
        }

        private void OnToggleClick(object sender, EventArgs eventArgs)
        {
            // Deal with location permissions.
            _locationPermitted = ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Android.Content.PM.Permission.Granted;
            if (!_locationPermitted)
            {
                var requiredPermissions = new string[] { Manifest.Permission.AccessFineLocation };
                ActivityCompat.RequestPermissions(this, requiredPermissions, LOCATION_PERMISSIONS_REQUEST);
            }

            // Deal with foreground service permissions.
            _foregroundServicePermitted = ContextCompat.CheckSelfPermission(this, Manifest.Permission.ForegroundService) == Android.Content.PM.Permission.Granted;
            if (!_foregroundServicePermitted)
            {
                var requiredPermissions = new string[] { Manifest.Permission.ForegroundService };
                ActivityCompat.RequestPermissions(this, requiredPermissions, FOREGROUND_SERVICE_PERMISSIONS_REQUEST);
            }

            _externalStoragePermitted = ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) == Android.Content.PM.Permission.Granted
                && ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Granted;
            if (!_externalStoragePermitted)
            {
                var requiredPermissions = new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage };
                ActivityCompat.RequestPermissions(this, requiredPermissions, EXTERNAL_STORAGE_PERMISSIONS_REQUEST);
            }



            if (_locationPermitted && _foregroundServicePermitted)
            {
                this.ToggleLogging();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            if (grantResults.Length == 0)
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                return;
            }

            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == LOCATION_PERMISSIONS_REQUEST && grantResults[0] == Android.Content.PM.Permission.Granted)
            {
                _locationPermitted = true;
            }

            if (requestCode == FOREGROUND_SERVICE_PERMISSIONS_REQUEST && grantResults[0] == Android.Content.PM.Permission.Granted)
            {
                _foregroundServicePermitted = true;
            }

            if (requestCode == EXTERNAL_STORAGE_PERMISSIONS_REQUEST && grantResults[0] == Android.Content.PM.Permission.Granted && grantResults[1] == Android.Content.PM.Permission.Granted)
            {
                _externalStoragePermitted = true;
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void ToggleLogging()
        {
            var intent = new Intent(Android.App.Application.Context, typeof(LoggerService));
            string loggingStatus = string.Empty;

            _isLoggingActive = !_isLoggingActive;
            if (_isLoggingActive)
            {
                StartForegroundService(intent);
                loggingStatus = "Logging active";

                System.Diagnostics.Debug.WriteLine("Started logger service");
            }
            else
            {
                StopService(intent);

                System.Diagnostics.Debug.WriteLine("Stopped logger service");
            }

            TextView loggingStatusTextView = FindViewById<TextView>(Resource.Id.loggingstatus);
            loggingStatusTextView.Text = loggingStatus;
        }
    }
}

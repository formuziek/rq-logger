namespace RQLogger
{
    using System.Collections.Generic;

    /// <summary>
    /// Data entry for a given location.
    /// </summary>
    public class DataEntry
    {
        /// <summary>
        /// Longitude coordinate.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Latitude coordinate.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Accelerometer vectors read between the previous location and the current location.
        /// </summary>
        public List<SensorEntry> SensorValues { get; set; } = new List<SensorEntry>();
    }

    /// <summary>
    /// Sensor entry.
    /// </summary>
    public class SensorEntry
    {
        /// <summary>
        /// Type of sensor - 0 for accelerometer, 1 for rotation vector.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Sensor values.
        /// </summary>
        public IList<float> Values { get; set; }
    }
}
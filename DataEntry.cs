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
        public List<IList<float>> AccelerometerReadings { get; set; } = new List<IList<float>>();
    }
}
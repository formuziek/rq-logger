namespace RQLogger
{
    using System.Collections.Generic;

    public class DataEntry
    {
        public double? Longitude { get; set; }

        public double? Latitude { get; set; }

        public List<IList<float>> AccelerometerReadings { get; set; } = new List<IList<float>>();
    }
}
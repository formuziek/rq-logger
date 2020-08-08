namespace RQFormatter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    class Program
    {
        private const double gravity = 9.81;

        static void Main(string[] args)
        {
            var inputLines = File.ReadAllLines("rq.log");

            var entries = new List<Entry>();

            // Keeping track of current and previous coordinates to approximate acceleration values to points in-between.
            double? previousLat = null;
            double? previousLon = null;
            double? currentLat = null;
            double? currentLon = null;
            double[] rotationMatrix = new double[9];
            double[] rotationValues = new double[4];
            for (int fileLoop = 0; fileLoop < inputLines.Length; fileLoop++)
            {
                // Start processing when a coordinate is found.
                if (inputLines[fileLoop].StartsWith("C:"))
                {
                    var coordinateSubEntries = new List<Entry>();

                    // Get the coordinate values, store them for later calculations.
                    var coordinateLine = inputLines[fileLoop].Substring(2);
                    var coordinateData = coordinateLine.Split(';');
                    previousLat = currentLat;
                    previousLon = currentLon;
                    currentLat = double.Parse(coordinateData[0], CultureInfo.InvariantCulture);
                    currentLon = double.Parse(coordinateData[1], CultureInfo.InvariantCulture);

                    // Loop until the next coordinate or EOF and gather all acceleration and rotation values.
                    int coordinateSubIndex = fileLoop;
                    while (++coordinateSubIndex < inputLines.Length)
                    {
                        if (inputLines[coordinateSubIndex].StartsWith("C:"))
                        {
                            //Stop on hitting the next coordinate.
                            break;
                        }

                        if (inputLines[coordinateSubIndex].StartsWith("A:"))
                        {
                            var accelerationLine = inputLines[coordinateSubIndex].Substring(2);
                            var accelerationData = accelerationLine.Split(';');
                            var ax = double.Parse(accelerationData[0], CultureInfo.InvariantCulture);
                            var ay = double.Parse(accelerationData[1], CultureInfo.InvariantCulture);
                            var az = double.Parse(accelerationData[2], CultureInfo.InvariantCulture);
                            var accelerationVector = new double[3] { ax, ay, az };

                            var rotationAdjustedAccelerationVector = RotateVector3(accelerationVector, rotationMatrix);

                            coordinateSubEntries.Add(new Entry
                            {
                                DeltaZ = Math.Abs(gravity - rotationAdjustedAccelerationVector[2]),
                            });
                        }

                        if (inputLines[coordinateSubIndex].StartsWith("R:"))
                        {
                            var rotationLine = inputLines[coordinateSubIndex].Substring(2);
                            var rotationEntries = rotationLine.Split(';');
                            var rx = double.Parse(rotationEntries[0], CultureInfo.InvariantCulture);
                            var ry = double.Parse(rotationEntries[1], CultureInfo.InvariantCulture);
                            var rz = double.Parse(rotationEntries[2], CultureInfo.InvariantCulture);
                            var sc = double.Parse(rotationEntries[3], CultureInfo.InvariantCulture);
                            UpdateRotationMatrix(ref rotationMatrix, new double[4] { rx, ry, rz, sc });
                        }
                    }

                    // When acceleration & rotation loop completes, update file loop iterator to current index.
                    fileLoop = coordinateSubIndex;

                    // Guarding against division by 0.
                    int accelEntries = coordinateSubEntries.Count;
                    if (accelEntries == 0)
                    {
                        continue;
                    }

                    // Calculating the approximate coordinates for acceleration entries between the coordinates.
                    double? latDelta = currentLat - previousLat;
                    double? lonDelta = currentLon - previousLon;
                    double? latStep = latDelta / accelEntries;
                    double? lonStep = lonDelta / accelEntries;
                    for (int x = 0; x < accelEntries; x++)
                    {
                        coordinateSubEntries[x].Latitude = previousLat + (latStep * (x + 1));
                        coordinateSubEntries[x].Longitude = previousLon + (lonStep * (x + 1));
                    }

                    entries.AddRange(coordinateSubEntries);
                }
            }

            // Writing all coordinate-delta triplets to a file in csv format.
            File.WriteAllLines(
                $"goodres.log", 
                new List<string> 
                { 
                    "LATITUDE;LONGITUDE;Z_DELTA" 
                }
                .Concat(entries
                    .Select(e => $"{e.Latitude};{e.Longitude};{e.DeltaZ}")));
        }

        private static void UpdateRotationMatrix(ref double[] R, double[] rotationVector)
        {
            double q0;
            double q1 = rotationVector[0];
            double q2 = rotationVector[1];
            double q3 = rotationVector[2];

            if (rotationVector.Length >= 4)
            {
                q0 = rotationVector[3];
            }
            else
            {
                q0 = 1 - q1 * q1 - q2 * q2 - q3 * q3;
                q0 = (q0 > 0) ? (float)Math.Sqrt(q0) : 0;
            }

            double sq_q1 = 2 * q1 * q1;
            double sq_q2 = 2 * q2 * q2;
            double sq_q3 = 2 * q3 * q3;
            double q1_q2 = 2 * q1 * q2;
            double q3_q0 = 2 * q3 * q0;
            double q1_q3 = 2 * q1 * q3;
            double q2_q0 = 2 * q2 * q0;
            double q2_q3 = 2 * q2 * q3;
            double q1_q0 = 2 * q1 * q0;

            if (R.Length == 9)
            {
                R[0] = 1 - sq_q2 - sq_q3;
                R[1] = q1_q2 - q3_q0;
                R[2] = q1_q3 + q2_q0;

                R[3] = q1_q2 + q3_q0;
                R[4] = 1 - sq_q1 - sq_q3;
                R[5] = q2_q3 - q1_q0;

                R[6] = q1_q3 - q2_q0;
                R[7] = q2_q3 + q1_q0;
                R[8] = 1 - sq_q1 - sq_q2;
            }
            else if (R.Length == 16)
            {
                R[0] = 1 - sq_q2 - sq_q3;
                R[1] = q1_q2 - q3_q0;
                R[2] = q1_q3 + q2_q0;
                R[3] = 0.0f;

                R[4] = q1_q2 + q3_q0;
                R[5] = 1 - sq_q1 - sq_q3;
                R[6] = q2_q3 - q1_q0;
                R[7] = 0.0f;

                R[8] = q1_q3 - q2_q0;
                R[9] = q2_q3 + q1_q0;
                R[10] = 1 - sq_q1 - sq_q2;
                R[11] = 0.0f;

                R[12] = R[13] = R[14] = 0.0f;
                R[15] = 1.0f;
            }
        }

        private static double[] RotateVector3(double[] vector, double[] rotationMatrix)
        {
            var res = new double[3];
            for (int i = 0; i < 3; i++)
            {
                double s = 0;
                for (int j = 0; j < 3; j++)
                {
                    s += vector[j] * rotationMatrix[j + i * 3];
                }
                res[i] = s;
            }

            return res;
        }
    }

    public class Entry
    {
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? DeltaZ { get; set; }
    }
}

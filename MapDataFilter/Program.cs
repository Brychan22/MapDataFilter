using System;
using System.Collections.Generic;
using System.IO;

namespace MapDataFilter
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            // Wait for the profiler to attach properly
            Console.WriteLine($"Reading {args[0]}; Press enter to begin...");
            Console.ReadLine();
#endif
            List<RoadData> parsedRoadData = new();
            // The character which delimits the 
            string delimiter = ",";
            // StreamReader provides a simple method for reading text from a file
            // It provides no addtional features (such as file sharing), so should only be used as a quick-and-dirty solution
            using (StreamReader sr = File.OpenText(args[0]))
            {
                string line = sr.ReadLine();
                // Skip the first line, as it contains the header
                line = sr.ReadLine();
                // This loop will go through every line of the file (the file path is passed as a command-line argument `args`
                while (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        // Split according to the delimiter char
                        string[] parts = line.Split(delimiter);
                        // First column is coordinate path data; decode
                        List<Coordinate> sectionCoords = ParseCoordinates(parts[0]);
                        // Get further parameters
                        int geo_sec_geo_id = int.Parse(parts[1]);
                        int roadSectionId = int.Parse(parts[2]);
                        int RoadID = int.Parse(parts[5]);
                        // Parse potentially combined fields
                        List<string> roadNames = new(3) { parts[6] };
                        if (!string.IsNullOrEmpty(parts[7]) || !string.IsNullOrEmpty(parts[8]))
                        {
                            roadNames.Add(parts[7]);
                            if (!string.IsNullOrEmpty(parts[8]))
                            {
                                roadNames.Add(parts[8]);
                            }
                        }
                        List<string> suburbs = new(2) { parts[9] };
                        if (!string.IsNullOrEmpty(parts[10]))
                        {
                            suburbs.Add(parts[10]);
                        }
                        List<string> cities = new(2) { parts[11] };
                        if (!string.IsNullOrEmpty(parts[12]))
                        {
                            cities.Add(parts[12]);
                        }
                        List<string> municipalities = new(2) { parts[13] };
                        if (!string.IsNullOrEmpty(parts[14]))
                        {
                            municipalities.Add(parts[14]);
                        }
                        // Finally, create the holder and resize
                        RoadData roadData = new()
                        {
                            RoadPath = sectionCoords,
                            GeomtryID = geo_sec_geo_id,
                            RoadSectionID = roadSectionId,
                            Suburbs = suburbs,
                            Cities = cities,
                            Regions = municipalities,
                            RoadID = RoadID,
                            RoadNames = roadNames
                        };
                        // Add this row to the collection
                        parsedRoadData.Add(roadData);
                    }
                    catch
                    {

                    }
                    line = sr.ReadLine();
                }
            }

            Dictionary<Coordinate, List<RoadData>> coordinateRoadMap = new();
            Dictionary<Coordinate, List<RoadData>> coordinateRoadMapShort = new();

            foreach (RoadData roadData in parsedRoadData)
            {
                if (coordinateRoadMap.ContainsKey(roadData.StartPoint))
                {
                    coordinateRoadMap[roadData.StartPoint].Add(roadData);
                }
                else
                {
                    coordinateRoadMap.Add(roadData.StartPoint, new List<RoadData>() { roadData });
                }

                if (coordinateRoadMap.ContainsKey(roadData.EndPoint))
                {
                    coordinateRoadMap[roadData.EndPoint].Add(roadData);
                }
                else
                {
                    coordinateRoadMap.Add(roadData.EndPoint, new List<RoadData>() { roadData });
                }


                if (coordinateRoadMapShort.ContainsKey(roadData.StartPoint))
                {
                    coordinateRoadMapShort[roadData.StartPoint].Add(roadData);
                }
                else if (coordinateRoadMapShort.Count < 10)
                {
                    coordinateRoadMapShort.Add(roadData.StartPoint, new List<RoadData>() { roadData });
                }

                if (coordinateRoadMapShort.ContainsKey(roadData.EndPoint))
                {
                    coordinateRoadMapShort[roadData.EndPoint].Add(roadData);
                }
                else if (coordinateRoadMapShort.Count < 10)
                {
                    coordinateRoadMapShort.Add(roadData.EndPoint, new List<RoadData>() { roadData });
                }
            }



            int k = 0;
        }

        /// <summary>
        /// Parses the coordinates from the provided field
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        static public List<Coordinate> ParseCoordinates(string field)
        {
            // Data is delimted by the pipe character '|'
            // 174.5954606167 -36.0807369167|174.594731957296 -36.0796673302854

            // We need to parse these into the Coordinate class, and build a list
            List<Coordinate> coordinates = new();
            string[] coordinatePairs = field.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (int i = 0; i < coordinatePairs.Length; i++)
            {
                // Each coordinate half is delimited by space, so split by space
                string[] parts = coordinatePairs[i].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); // "StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries" sets both Flags to true, so they are performed
                                                                                                                                        // If we can parse both numbers, we should set a value
                if (parts.Length == 2 && // Evaluate the length of parts first! Fail-fast, ensures an exception isn't thrown
                    double.TryParse(parts[0], out double longitude) && double.TryParse(parts[1], out double latitude))
                {
                    Coordinate coordinate = new()
                    {
                        Latitude = latitude,
                        Longitude = longitude
                    };
                    coordinates.Add(coordinate);
                }
                else
                {
                    // Failed to parse coordinate
                    int g = 0;
                }
            }
            // Not explicitly needed in such a short-lived program, but a good practice for keeping memory space free
            // At this point, the list of coordinates is resolved, so we need no further space to be allocated
            coordinates.TrimExcess();
            return coordinates;
        }
    }

    struct Node // Aka, intersection
    {
        public string NodeName;

        public Dictionary<Node, int> AdjacentNodes; // Intersections that can be directly reached from this Intersection 
    }

    struct RoadGeoMap : IComparable
    {
        public int RoadID { get; set; }
        public int GeometryID { get; set; }

        public int CompareTo(object obj)
        {
            RoadGeoMap other = (RoadGeoMap)obj;
            return RoadID.CompareTo(other.RoadID);
        }

        public override string ToString()
        {
            return $"{RoadID}: {GeometryID}";
        }

        public override int GetHashCode()
        {
            return GeometryID;
        }
    }
    /// <summary>
    /// Represents a full road segment, captures all the data in the table
    /// </summary>
    struct RoadData
    {
        public List<Coordinate> RoadPath;
        // We need two additional fields, at the cost of 16 bytes:
        // These are where the intersections may be
        public Coordinate StartPoint
        {
            get
            {
                if (RoadPath != null && RoadPath.Count > 0)
                {
                    return RoadPath[0];
                }
                else return new Coordinate();
            }
        }
        public Coordinate EndPoint
        {
            get
            {
                if (RoadPath != null && RoadPath.Count > 0)
                {
                    return RoadPath[^1];
                }
                else return new Coordinate();
            }
        }
        // Other fields from the source data
        public int GeomtryID;
        public int RoadSectionID;
        public List<string> Suburbs;
        public List<string> Cities;
        public List<string> Regions;
        public int RoadID;
        public List<string> RoadNames; // A road name can be made of 3 parts, Primary (street) name, a secondary name, and the route name


        /// <summary>
        /// Overriding ToString() allows the debugger to display this by logical value, rather than memory allocation
        /// Helps significantly with debugging
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{RoadNames[0]}, {Suburbs[0]}, {Cities[0]}, {Regions[0]}";
        }
    }


    /// <summary>
    /// Represents a Latitude-Longitude coordinate pair.
    /// Implements IEquatable, within an accepted range specified by LatPrecision & LongPrecision,
    /// such that two positions within an accpetable range are considered identical
    /// </summary>
    struct Coordinate : IEquatable<Coordinate>
    {
        public double Latitude;
        public double Longitude;

        // NB: 174.442916 is the eastern-most point of the South Island
        //   & -40.261682 More north than this is North Island
        // Values less than both are SI-based

        public static double LatPrecision = 1e-4; // Approximately 10 m 
        public static double LongPrecision = 1e-4; // Approximately 8 m

        /// <summary>
        /// Compare the two coordinates to see if they're equal
        /// Accounts for the range of values in 
        /// </summary>
        public bool Equals(Coordinate other)
        {
            bool approxEqualLat = (other.Latitude >= Latitude - LatPrecision) && (other.Latitude <= Latitude + LatPrecision);
            bool approxEqualLong = (other.Longitude >= Longitude - LatPrecision) && (other.Latitude <= Longitude + LatPrecision);
            // Maximal distances that a measure can be is 2 m vertically, 1.6 m horizontally (therefore 2.56 m diagonally)
            // We can double these distances by using 2e[value] for the precision
            return approxEqualLat && approxEqualLong;
        }

        public override bool Equals(object obj) => obj is Coordinate coord && Equals(coord);

        public override int GetHashCode()
        {
            return (Math.Round(Latitude, 4) * 31 + Math.Round(Longitude, 4) * 59).GetHashCode();
        }
    }
}

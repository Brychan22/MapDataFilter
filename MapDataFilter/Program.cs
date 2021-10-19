using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            }
            // This is contained in a graph; quickly searchable set of nodes
            HashSet<Node> nodes = new();
            HashSet<RoadData> expandedRoads = new();

            Node previous = new Node()
            {
                NodeName = "Root",
                AdjacentNodes = new Dictionary<Node, int>()
            };
            // Nodes that need expanding
            Queue<Tuple<RoadData, Node, Coordinate>> queue = new();

            queue.Enqueue(new Tuple<RoadData, Node, Coordinate>(parsedRoadData[1], previous, new Coordinate { Latitude = 0, Longitude = 0 }));
            while (queue.Count > 0)
            {
                if (queue.Count == 100)
                {
                    int t = 0;
                }
                // Pop an item off the queue
                Tuple<RoadData, Node, Coordinate> calcData = queue.Dequeue();
                // Ensure the coord we eval is not the one we just did
                Coordinate evalCoord = calcData.Item1.StartPoint.Equals(calcData.Item3) ? calcData.Item1.EndPoint : calcData.Item1.StartPoint;
                List<RoadData> adjoiningRoadData = coordinateRoadMap[evalCoord];
                // Evaluate its name
                List<string> roadNames = new();
                adjoiningRoadData.ForEach(roadData => roadNames.Add(roadData.RoadNames.First()));
                // Sort alphabetically
                roadNames.Sort();
                expandedRoads.Add(calcData.Item1);
                // Accumulate the name
                string name = string.Join(",", roadNames);
                if (name == calcData.Item2.NodeName)
                {
                    // Node name already expanded (no exit road), or the road data is broken
                    nodes.Add(new Node { NodeName = name, AdjacentNodes = new()});
                    // Find a new node to expand:
                    int index = 0;
                    while (index < parsedRoadData.Count && expandedRoads.Contains(parsedRoadData[index++])) ;
                    if (index < parsedRoadData.Count)
                    {
                        queue.Enqueue(new Tuple<RoadData, Node, Coordinate>(parsedRoadData[index], new Node { NodeName = $"Root{index}", AdjacentNodes = new Dictionary<Node, int>() }, new Coordinate { Latitude = 0, Longitude = 0 }));
                    }
                    continue;
                }
                // Create the node that this item will be represented by
                Node n = new()
                {
                    // Set the name
                    NodeName = name,
                    // Init the adjacent nodes list
                    AdjacentNodes = new()
                };
                calcData.Item2.AdjacentNodes.TryAdd(n, 0);
                // Each connected road must be added to the queue
                foreach (RoadData item in adjoiningRoadData)
                {
                    if (!expandedRoads.Contains(item))
                    {
                        // These nodes have not been visited
                        queue.Enqueue(new Tuple<RoadData, Node, Coordinate>(item, n, evalCoord));
                    }
                    // Need the else to link other roads we've been to
                    
                }
                nodes.Add(n);
            }
            

            int k = 0;
        }

        /// <summary>
        /// This will, of course fail as it will overflow the stack
        /// But the logic is what needs to be implemented
        /// </summary>
        /// <param name="source"></param>
        /// <param name="coordinateRoadMap"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        static public Node CalcAllNodes(RoadData source, Dictionary<Coordinate, List<RoadData>> coordinateRoadMap, HashSet<Node> nodes)
        {
            List<RoadData> adjoiningRoadData = coordinateRoadMap[source.StartPoint];
            List<string> roadNames = new();
            adjoiningRoadData.ForEach(roadData => roadNames.Add(roadData.RoadNames.First()));
            // Sort alphabetically
            roadNames.Sort();
            // Accumulate the name
            string name = string.Join(",", roadNames);
            // Beginning node, arbitrary. Begins at the first node for now
            Node start = new()
            {
                NodeName = name,
                AdjacentNodes = new()
            };
            foreach (RoadData item in adjoiningRoadData)
            {
                start.AdjacentNodes.Add(CalcAllNodes(item, coordinateRoadMap, nodes), 0);
            }
            nodes.Add(start);

            return start;
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
        /// <summary>
        /// NodeName is a unique name that represents this intersection.
        /// The simplest implementation is the name of each connected road, alphabeticised.
        /// 
        /// The primary constraint is that it must be consistent, such that the same intersection is always found correctly
        /// </summary>
        public string NodeName;

        public Dictionary<Node, int> AdjacentNodes; // Intersections that can be directly reached from this Intersection


        /// <summary>
        /// Hashcode must be consistent, ignoring the memory addresses, and instead focusing on the contained parameters
        /// </summary>
        public override int GetHashCode()
        {
            return NodeName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is Node other && NodeName.Equals(other.NodeName);
        }

        public override string ToString()
        {
            return NodeName;
        }
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

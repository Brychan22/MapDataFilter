using System;
using System.Collections.Generic;
using System.IO;

namespace MapDataFilter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.ReadLine();
            //List<RoadGeoMap> geoMap = new List<RoadGeoMap>();
            //HashSet<RoadGeoMap> geoHashSet = new HashSet<RoadGeoMap>();
            List<RoadData> rows = new List<RoadData>();
            string delimiter = ",";
            using (StreamReader sr = File.OpenText(args[0]))
            {
                string line = sr.ReadLine();
                // Skip the first line, as it contains the header
                line = sr.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        string[] parts = line.Split(delimiter);
                        // This loop will go through every line of the file (the file path is passed as a command-line argument `args`
                        // At the moment, it just generates a new sorted list of RoadID: GeometryID, but we can adapt this to better sort our
                        // data

                        // TODO: Filter out coordinates from part[0]


                        // We should use TryParse instead, here, as we don't do anything with the exceptional case
                        // int geoID = int.Parse(parts[1]);
                        // int roadID = int.Parse(parts[8]);

                        // Add a breakpoint, to verify the value of rowCoord
                        // RoadGeoMap roadGeoMap = new RoadGeoMap()
                        // {
                        //    RoadID = roadID,
                        //    GeometryID = geoID
                        // };
                        // geoMap.Add(roadGeoMap);
                        // geoHashSet.Add(roadGeoMap);

                        List<Coordinate> rowCoord = ParseCoordinates(parts[0]);
                        int geo_sec_geo_id = int.Parse(parts[1]);
                        int roadSectionId = int.Parse(parts[2]);
                        string Suburb = parts[3];
                        string City = parts[4];
                        string Region = parts[5];
                        int AssociationID = int.Parse(parts[6]);
                        int RoadNameID = int.Parse(parts[7]); // Probably don't need this column
                        int RoadID = int.Parse(parts[8]);
                        string RoadName = parts[9];


                        RoadData roadData = new RoadData()
                        {
                            RoadPath = rowCoord,
                            GeomtryID = geo_sec_geo_id,
                            RoadSectionID = roadSectionId,
                            Suburb = Suburb,
                            City = City,
                            Region = Region,
                            AssociationID = AssociationID,
                            RoadNameID = RoadNameID,
                            RoadID = RoadID,
                            RoadName = RoadName
                        };
                        // Add this row to the collection
                        rows.Add(roadData);
                    }
                    catch
                    {
                        
                    }
                    line = sr.ReadLine();
                }
            }
            // geoMap.Sort();

            // using StreamWriter sw = File.CreateText("Test.txt");
            // foreach (var item in geoMap)
            // {
            //    sw.WriteLine(item);
            // }

            Dictionary<Coordinate, List<RoadData>> coordinateRoadMap = new();
            Dictionary<Coordinate, List<RoadData>> coordinateRoadMapShort = new();

            foreach (RoadData roadData in rows)
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
                else if(coordinateRoadMapShort.Count < 10)
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
            // The format of field might look like so:
            // MULTILINESTRING ((174.5954606167 -36.0807369167|
            // 174.594731957296 -36.0796673302854|174.59441926705 -36.0793780918086|
            // 174.593926779914 -36.0792061121738|174.593473379059 -36.0791982949177|
            // 174.593090333509 -36.0792217466861|174.592762008751 -36.0791826604055|
            // 174.592535308324 -36.0789872290022|174.592011552163 -36.0791982949177))
            // (New-lines manually added)

            // We need to parse these into the Coordinate class, and build a list
            List<Coordinate> coordinates = new List<Coordinate>();
            // Before we break the field into coordinate strings, we should get rid of (trim) the unneccessary characters;
            // there are a few ways to do this, but the easiest is to simply perform a replace for 'MULTILINESTRING', and then trim space and brackets
            field = field.Replace("MULTILINESTRING", "").Trim(new char[] { '(', ')', ' ' });
            // This results in field being like so:
            // 174.594731957296 -36.0796673302854|174.59441926705 -36.0793780918086
            // We know that each coordinate-pair is delimited by a specific char (I chose the pipe '|' character)
            string[] coordinatePairs = field.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < coordinatePairs.Length; i++)
            {
                string coordinatePair = coordinatePairs[i];
                // Each coordinate half is delimited by space, so split by space
                string[] parts = coordinatePair.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); // "StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries" sets both Flags to true, so they are performed
                                                                                                                                    // If we can parse both numbers, we should set a value
                if (parts.Length == 2 && // Evaluate the length of parts first! Fail-fast, ensures an exception isn't thrown
                    double.TryParse(parts[0], out double longitude) && double.TryParse(parts[1], out double latitude))
                {
                    Coordinate coordinate = new Coordinate()
                    {
                        Latitude = latitude,
                        Longitude = longitude
                    };
                    coordinates.Add(coordinate);
                }
                else
                {
                    // Set a breakpoint, as this is a failiure to parse
                    int g = 0;
                }
            }
            return coordinates;
        }
    }
    //hELLO!!!
    
   struct Node
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
        public string Suburb;
        public string City;
        public string Region;
        public int AssociationID;
        public int RoadNameID; // Probably don't need this column
        public int RoadID;
        public string RoadName;
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

        public static double LatPrecision = 1e-5; // Approximately 10 m 
        public static double LongPrecision = 1e-5; // Approximately 8 m

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
            return (Math.Round(Latitude, 4)*31 + Math.Round(Longitude, 4) *59).GetHashCode();
        }
    }
}

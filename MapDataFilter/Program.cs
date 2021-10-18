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
            List<RoadGeoMap> geoMap = new List<RoadGeoMap>();
            HashSet<RoadGeoMap> geoHashSet = new HashSet<RoadGeoMap>();
            string delimiter = ",";
            using (StreamReader sr = File.OpenText(args[0]))
            {
                string line = sr.ReadLine();
                line = sr.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        string[] parts = line.Split(delimiter);
                        int geoID = int.Parse(parts[1]);
                        int roadID = int.Parse(parts[8]);
                        RoadGeoMap roadGeoMap = new RoadGeoMap()
                        {
                            RoadID = roadID,
                            GeometryID = geoID
                        };
                        geoMap.Add(roadGeoMap);
                        geoHashSet.Add(roadGeoMap);
                    }
                    catch
                    {

                    }
                    line = sr.ReadLine();
                }
            }
            geoMap.Sort();

            using StreamWriter sw = File.CreateText("Test.txt");
            foreach (var item in geoMap)
            {
                sw.WriteLine(item);
            }
            int k = 0;
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

    struct RoadData
    {
        List<Coordinate> RoadPath;
        int RoadSectionID;
        string Suburb;
        string City;
        string Region;
        int AssociationID;
        int RoadNameID; // Probably don't need this column
        int RoadID;
        string RoadName;
    }

    struct Coordinate : IEquatable<Coordinate>
    {
        public double Latitude;
        public double Longitude;

        public static double LatPrecision = 1e-5; // Approximately 1 m 
        public static double LongPrecision = 1e-5; // Approximately 0.8 m

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
            return (Latitude*31 + Longitude*59).GetHashCode();
        }
    }
}

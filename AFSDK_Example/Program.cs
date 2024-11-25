using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OSIsoft.AF.PI;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Search; 
using AFSDK_Example;

namespace AFSDK_Example {
    class Program {
        static void Main(string[] args) {
            TagLists tagLists;
            String listsFile="";
            String efFile = "";
            if ( (args.Length >= 2) && (args[0] == "-c" ) ) {
                listsFile = args[1];
                if (args.Length == 3) {
                    efFile = args[2]; 
                }
            } else {
                Console.WriteLine("Prints help:");
                Console.WriteLine("\tAFSDK_Example -h");
                Console.WriteLine("To run the application: ");
                Console.WriteLine("\tAFSDK_Example -c path_to_json_file path_to_json_EF_file");
                System.Environment.Exit(1);
            }
            EventFrameExamples efExample = new EventFrameExamples(efFile);
            efExample.Test_event_frames();
            Console.WriteLine($"Attempting to read {listsFile}");
            try {
                String data = File.ReadAllText(listsFile);
                tagLists = JsonConvert.DeserializeObject<TagLists>(data);
                foreach (TagList tagList in tagLists.lists) {
                    lookup_tags(tagList);
                }
            } catch(Exception ex) {
                Console.WriteLine($"Error opening or reading {listsFile} {ex.Message}");
                System.Environment.Exit(1); 
            }
        }
        static void lookup_tags(TagList tagList) {
            PIServers servers = new PIServers();
            PIServer server;
            System.Console.WriteLine($"processing tags for {tagList.server}");
            
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try {
                server = servers[tagList.server];
                if (server != null) {
                    server.Connect();
                    System.Console.WriteLine($"Connected: {server.ConnectionInfo.IsConnected} PIServer: {server.ConnectionInfo.PIServer.Name} Port: {server.ConnectionInfo.Port} Point count: {server.GetPointCount()}");
                    IEnumerable<PIPoint> test = get_points_by_ptsource(server, "R");
                    PIPointList points = get_pointids(server, tagList.tags);
                    System.Console.WriteLine($"{points.Count} out of {tagList.tags.Count} PIPoints found");
                    Stopwatch sw1 = new Stopwatch();
                    sw1.Start();
                    (int ptCount, int sampleCount) retVal = get_archive_data(server, points, tagList.start, tagList.end, tagList.pageSize, tagList.showSamples);
                    sw1.Stop();
                    System.Console.WriteLine($"Total tags: {retVal.ptCount}, Total samples: {retVal.sampleCount}, Total samples/sec {retVal.sampleCount/sw1.Elapsed.TotalSeconds}, Total time {sw1.Elapsed.TotalSeconds}");
                } else {
                    System.Console.WriteLine($"Server {tagList.server} is not in the Known Servers Table (KST)");
                }
            }
            catch( Exception ex) {
                System.Console.WriteLine($"{ex.Message}");
            }
            sw.Stop();
            System.Console.WriteLine($"Total time: {sw.Elapsed.TotalSeconds} seconds");
        }
        static IEnumerable<PIPoint> get_points_by_ptsource ( PIServer server, String ptSource) {
            //https://github.com/bzshang/PI-AF-SDK-Basic-Samples/blob/master/ExamplesLibrary/PIPointExamples/FindPIPointsExample.cs
            IEnumerable<PIPoint> points;
            PIPointQuery ptSourceQuery = new PIPointQuery {
                AttributeName = PICommonPointAttributes.PointSource,
                AttributeValue = ptSource,
                Operator = AFSearchOperator.Equal
            };
            IEnumerable<PIPointQuery> query = new[] { ptSourceQuery, };
            
            IEnumerable<string> attributesToLoad = new[] {
                PICommonPointAttributes.Archiving,
                PICommonPointAttributes.Compressing,
                PICommonPointAttributes.CompressionDeviation,
                PICommonPointAttributes.CompressionMaximum,
                PICommonPointAttributes.CompressionMinimum,
                PICommonPointAttributes.CompressionPercentage,
                PICommonPointAttributes.Descriptor,
                PICommonPointAttributes.EngineeringUnits,
                PICommonPointAttributes.ExceptionDeviation,
                PICommonPointAttributes.ExceptionMaximum,
                PICommonPointAttributes.ExceptionMinimum,
                PICommonPointAttributes.ExceptionPercentage,
                PICommonPointAttributes.ExtendedDescriptor,
                PICommonPointAttributes.InstrumentTag,
                PICommonPointAttributes.Location1,
                PICommonPointAttributes.Location4,
                PICommonPointAttributes.PointID,
                PICommonPointAttributes.PointSource,
                PICommonPointAttributes.PointType,
                PICommonPointAttributes.SourcePointID,
                PICommonPointAttributes.Span,
                PICommonPointAttributes.Step,
                PICommonPointAttributes.Tag,
                PICommonPointAttributes.Zero,
            };
            points = PIPoint.FindPIPoints(server, query, attributesToLoad);
            foreach (PIPoint pt in points) {
                Console.WriteLine($"{pt.ID}:");
                foreach (String attr in attributesToLoad) {
                    Console.WriteLine($"\t{attr}:\t {pt.GetAttribute(attr)}");
                }
            }

                return points;
        }
        static PIPointList get_pointids (PIServer server, List<String> tags) {
            PIPointList points = new PIPointList();
            Console.WriteLine($"Looking up {tags.Count} tags from {server.Name}");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach ( String tag in tags) {
                try {
                    PIPoint pt = PIPoint.FindPIPoint(server, tag);
                    points.Add(pt);

                } catch (Exception ex) {
                    System.Console.WriteLine($"Error finding point: \"{tag}\" {ex.Message}");
                }
            }
            sw.Stop();
            Console.WriteLine($"{points.Count} Tags Found in {sw.Elapsed.TotalSeconds} seconds");
            return points; 
        }
        static (int ptCount, int sampleCount) get_archive_data(PIServer server, PIPointList points, String start, String end, int pageSize, bool showSamples) {
            int ptCount = 0;
            int sampleCount = 0;
            if (points.Count > 0) {
                OSIsoft.AF.Time.AFTimeRange timeRange = new OSIsoft.AF.Time.AFTimeRange(start, end);
                OSIsoft.AF.Data.AFBoundaryType boundaryType = 0;
                String filterExpression = "";
                bool includeFilteredValues = false;
                Console.WriteLine($"Getting data for {points.Count} tags, time range: {timeRange.ToString()}");
                PIPagingConfiguration pagingConfig = new PIPagingConfiguration(OSIsoft.AF.PI.PIPageType.TagCount, pageSize);
                int maxCount = 0; // zero returns all values
                Stopwatch sw = new Stopwatch();
                Double prevElapsed = 0;
                sw.Start();
                foreach (AFValues afVals in points.RecordedValues(timeRange, boundaryType, filterExpression, includeFilteredValues, pagingConfig, maxCount)) {
                    sw.Stop();
                    ptCount++;
                    Console.WriteLine($"{afVals.PIPoint.Name}: Samples: {afVals.Count}, Elapsed Time {sw.Elapsed.TotalSeconds - prevElapsed} seconds");
                    prevElapsed = sw.Elapsed.TotalSeconds;
                    sampleCount += afVals.Count; 
                    if ( showSamples ) {
                        /*Console.WriteLine($"{afVals.Count}");*/
                        foreach (AFValue afVal in afVals) {
                            Console.WriteLine($"{afVal.Timestamp}, {afVal.Value}");
                        }
                        Console.WriteLine("-------------------------");
                    }
                    sw.Start();
                }
            }
            return (ptCount, sampleCount);
        }     
    }
    class TagList {
        public String comment { get; set; }
        public String server { get; set; }
        public int port { get; set; }     // usually not needed since; but we may want to add the sever to the KST if it does not exist
        public String start { get; set; }
        public String end { get; set; }
        public int pageSize { get; set; }
        public bool showSamples { get; set; }
        public List<String> tags { get; set; }
    }
    class TagLists {
        public List<TagList> lists { get; set; }
    }

}

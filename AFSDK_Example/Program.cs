using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OSIsoft.AF.PI;
using OSIsoft.AF.Asset;

namespace AFSDK_Example {
    class Program {
        static void Main(string[] args) {
            TagLists tagLists;
            String listsFile = "tag_lists.json";
            String data = File.ReadAllText(listsFile);
            tagLists = JsonConvert.DeserializeObject<TagLists>(data);
            foreach (TagList tagList in tagLists.lists) {
                lookup_tags(tagList);
            }
        }
        static void lookup_tags(TagList tagList) {
            PIServers servers = new PIServers();
            PIServer server;
            System.Console.WriteLine($"processing tags for {tagList.server}");
            try {
                server = servers[tagList.server];
                if (server != null) {
                    server.Connect();
                    System.Console.WriteLine($"Connected: {server.ConnectionInfo.IsConnected} PIServer: {server.ConnectionInfo.PIServer.Name} Port: {server.ConnectionInfo.Port} Point count: {server.GetPointCount()}");
                    PIPointList points = get_pointids(server, tagList.tags);
                    System.Console.WriteLine($"{points.Count} out of {tagList.tags.Count} PIPoints found");
                    get_archive_data(server, points);
                } else {
                    System.Console.WriteLine($"Server {tagList.server} is not in the Known Servers Table (KST)");
                }
            }
            catch( Exception ex) {
                System.Console.WriteLine($"{ex.Message}");
            }
        }
        static PIPointList get_pointids (PIServer server, List<String> tags) {
            PIPointList points = new PIPointList();
            foreach ( String tag in tags) {
                try {
                    PIPoint pt = PIPoint.FindPIPoint(server, tag);
                    points.Add(pt);

                } catch (Exception ex) {
                    System.Console.WriteLine($"Error finding point {tag} {ex.Message}");
                }
            }
            return points; 
        }
        static void get_archive_data(PIServer server, PIPointList points) {
            if (points.Count > 0) {
                OSIsoft.AF.Time.AFTimeRange timeRange = new OSIsoft.AF.Time.AFTimeRange("*-1h", "*");
                OSIsoft.AF.Data.AFBoundaryType boundaryType = 0;
                String filterExpression = "";
                bool includeFilteredValues = false;
                PIPagingConfiguration pagingConfig = new PIPagingConfiguration(OSIsoft.AF.PI.PIPageType.EventCount, 1000000);
                //int maxCount = 1000;
                foreach (AFValues afVals in points.RecordedValues(timeRange, boundaryType, filterExpression, includeFilteredValues, pagingConfig)) {
                    foreach(AFValue afVal in afVals) {
                        Console.WriteLine($"{afVal.PIPoint.ID} {afVal.Timestamp}, {afVal.Value}");
                    }
                }
            }
        }
    }
    class TagList {
        public String server { get; set; }
        public int port { get; set; }     // usually not needed since; but we may want to add the sever to the KST if it does not exist
        public List<String> tags { get; set; }
    }
    class TagLists {
        public List<TagList> lists { get; set; }
    }

}

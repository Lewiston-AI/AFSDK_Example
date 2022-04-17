using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OSIsoft.AF.PI;

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
                    List<int> pointids = get_pointids(server, tagList.tags);
                    System.Console.WriteLine($"{pointids.Count} out of {tagList.tags.Count} PIPoints found");
                } else {
                    System.Console.WriteLine($"Server {tagList.server} is not in the Known Servers Table (KST)");
                }
            }
            catch( Exception ex) {
                System.Console.WriteLine($"{ex.Message}");
            }
        }
        static List<int> get_pointids (PIServer server, List<String> tags) {
            List<int> pointids = new List<int>();
            foreach ( String tag in tags) {
                try {
                    PIPoint pt = PIPoint.FindPIPoint(server, tag);
                    pointids.Add(pt.ID);

                } catch (Exception ex) {
                    System.Console.WriteLine($"Error finding point {tag} {ex.Message}");
                }
            }
            return pointids; 
        }
        static void get_archive_data(PIServer server, List<int> pointids) {
            if (pointids.Count > 0) {

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

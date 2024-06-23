using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Search; 

namespace AFSDK_Example {
    public class EventFrameExamples {
        public EventFrameExamples(String jsonFile) {
            this.jsonFile = jsonFile;
        }
        public void Test_event_frames() {
            System.Console.WriteLine($"Test: {jsonFile}");
            Connect();
            Get_test_element();
        }
        public int Connect() {
            piSystem = new PISystems().DefaultPISystem;
            credential = new System.Net.NetworkCredential();
            credential.UserName = "piadm";
            credential.Domain = "seeqpiaf";
            credential.Password = "SeeQ2013!@#$";
            try {
                piSystem.Connect(credential);
                Console.WriteLine($"Successful connection to {piSystem.Name}");
                return 0;
            }
            catch(Exception ex) {
                Console.WriteLine($"Error connecting: {ex.ToString()}");
                return -1; 
            }

        }
        public int Get_test_element() {
            try {
                OSIsoft.AF.AFDatabase defDatabase = piSystem.Databases.DefaultDatabase;
                Console.WriteLine($"Default DB: {defDatabase.ToString()}");
                String elementNameFilter = "EFTest2A";
                AFElement element = new AFElement();
                string querystring = string.Format("{0}", elementNameFilter);
                using (AFElementSearch elementquery = new AFElementSearch(defDatabase, "ElementSearch", querystring)) {
                    elementquery.CacheTimeout = TimeSpan.FromMinutes(10);
                    foreach (AFElement el in elementquery.FindObjects()) {
                        Console.WriteLine($"Element: {el.Name}");
                        element = el; 
                    }
                }

                int ct = defDatabase.Elements.Count;

                Guid id = element.ID; 
                AFElement elByID = OSIsoft.AF.Asset.AFElement.FindElement(piSystem, id);
                return 0;
            }
            catch(Exception ex) {
                Console.WriteLine($"get test element {ex.ToString()}");
                return -1; 
            }

        }
        private PISystem piSystem; 
        private System.Net.NetworkCredential credential;
        private String jsonFile;
    }
}

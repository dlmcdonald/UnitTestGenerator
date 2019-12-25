using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnitTestGenerator.Models
{
    public class Configuration
    {
        [JsonProperty("remove_first_from_namespace")]
        public bool RemoveFirst { get; set; }
        [JsonProperty("unittest_project_name")]
        public string UnitTestProjectName { get; set; }
        [JsonProperty("unittest_suffix")]
        public string UnitTestSuffix { get; set; }
        [JsonProperty("unittest_class_suffix")]
        public string UnitTestClassSuffix { get; set; }
        [JsonProperty("custom_setup_method_lines")]
        public List<string> CustomSetupMethodLines { get; set; }


        public static Configuration DefaultConfiguration()
        {
            return new Configuration
            {
                RemoveFirst = true,
                UnitTestProjectName = "",
                UnitTestSuffix = "Should",
                UnitTestClassSuffix = "Tests",
                CustomSetupMethodLines = new List<string>()
            };
        }
    }
}

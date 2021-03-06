﻿using System.Collections.Generic;
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
        [JsonProperty("default_using_statements")]
        public List<string> DefaultUsings { get; set; }
        [JsonProperty("test_framework")]
        public string TestFramework { get; set; }


        public static Configuration DefaultConfiguration()
        {
            return new Configuration
            {
                RemoveFirst = true,
                UnitTestProjectName = "",
                TestFramework = "nunit",
                UnitTestSuffix = "Should",
                UnitTestClassSuffix = "Tests",
                DefaultUsings = new List<string>
                {
                    "Moq",
                }
            };
        }
    }
}

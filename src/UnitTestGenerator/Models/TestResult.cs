using System;
namespace UnitTestGenerator.Models
{
    public class TestResult
    {
        public TestResult()
        {
        }

        public string TestName { get; set; }
        public string FullTestName { get; set; }
        public bool Passed { get; set; }
    }
}

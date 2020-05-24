using System;
using System.Collections.Generic;
using UnitTestGenerator.Models;

namespace UnitTestGenerator.TestStorage
{
    public class TestResults
    {
        static TestResults _current;

        public static TestResults Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new TestResults();
                }
                return _current;
            }
        }

        public TestResults()
        {
            Tests = new List<TestResult>();
        }

        public List<TestResult> Tests { get; set; }
    }
}

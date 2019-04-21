using System;
using UnitTestGenerator.Models;

namespace UnitTestGenerator.Services.Interfaces
{
    public interface IConfigurationService
    {
        Configuration GetConfiguration();
        void Save(Configuration configuration);
    }
}

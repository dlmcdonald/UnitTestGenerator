using System;
using System.Threading.Tasks;
using UnitTestGenerator.Models;

namespace UnitTestGenerator.Services
{
    public interface IConfigurationService
    {
        Task<Configuration> GetConfiguration();
        void Save(Configuration configuration);
    }
}

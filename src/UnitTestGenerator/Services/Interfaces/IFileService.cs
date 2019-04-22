using System;
using System.Threading.Tasks;
namespace UnitTestGenerator.Services.Interfaces
{
    public interface IFileService
    {
        Task GenerateFile(string namespaceId, string classId, string filePath);
    }
}

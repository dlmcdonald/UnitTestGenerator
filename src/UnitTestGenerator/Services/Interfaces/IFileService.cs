using System;
namespace UnitTestGenerator.Services.Interfaces
{
    public interface IFileService
    {
        void GenerateFile(string namespaceId, string classId, string filePath);
    }
}

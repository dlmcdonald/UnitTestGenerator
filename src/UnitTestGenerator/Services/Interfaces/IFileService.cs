using System;
namespace UnitTestGenerator.Services.Interfaces
{
    public interface IFileService
    {
        void GenerateFile(string namespaceId, string classId, string filePath, string templateName);
        void GenerateXFUnitTestNUnitFile(string namespaceId, string classId, string filePath);
    }
}

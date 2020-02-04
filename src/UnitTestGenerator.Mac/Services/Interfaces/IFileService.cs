using System.Threading.Tasks;
namespace UnitTestGenerator.Mac.Services.Interfaces
{
    public interface IFileService
    {
        Task GenerateFile(string namespaceId, string classId, string filePath);
    }
}

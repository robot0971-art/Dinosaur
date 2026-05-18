using System.Collections.Generic;

namespace Dino.Infrastructure.Data
{
    public interface IDataService
    {
        T LoadData<T>(string key) where T : class;
        void SaveData<T>(string key, T data) where T : class;
        bool HasData(string key);
        void ClearData(string key);
        void ClearAll();
    }

    public interface IExcelConverter
    {
        List<T> ReadExcel<T>(string filePath, string sheetName = null) where T : class, new();
        void WriteExcel<T>(string filePath, string sheetName, List<T> data) where T : class;
        void CreateTemplate<T>(string filePath, string sheetName) where T : class;
    }
}
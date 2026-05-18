using System.Collections.Generic;
using DinoGrow.Core.Data;

namespace DinoGrow.Infrastructure.Data
{
    public interface IDataService
    {
        IReadOnlyList<DinoDataRecord> LoadDinoRows(string xlsxPath);
        void CreateDinoTemplate(string xlsxPath);
    }
}

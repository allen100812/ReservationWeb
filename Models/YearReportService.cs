using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;


namespace Web0524.Models
{
    public interface IYearReportService
    {
        IEnumerable<YearReport> GetYearReport(string Year);
        IEnumerable<YearReport> GetYearReport_MonthSaleSumForm(string year);
    }

    public class YearReportService: IYearReportService
    {
        private readonly IDbConnection _dbConnection;
        public YearReportService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public IEnumerable<YearReport> GetYearReport_MonthSaleSumForm(string year)
        {
            var sql = "SELECT MONTH(Date) AS Month, COUNT(*) AS Num FROM OrderTB WHERE YEAR(Date) = @Year GROUP BY MONTH(Date) ORDER BY MONTH(Date)";


            return _dbConnection.Query<YearReport>(sql, new { Year = year });
        }
        public IEnumerable<YearReport> GetYearReport(string year)
        {
            var sql = @"
SELECT 
    MONTH(a.Date) AS Month,
    b.Placetitle AS Place,
    c.Name AS Product,
    COUNT(*) AS Num
FROM OrderTB a
LEFT JOIN PlaceTB b ON a.Placeid = b.Placeid
LEFT JOIN ProductTB c ON a.Pid = c.Pid
WHERE YEAR(a.Date) = @Year
GROUP BY MONTH(a.Date), b.Placetitle, c.Name
ORDER BY MONTH(a.Date), b.Placetitle, c.Name";

            return _dbConnection.Query<YearReport>(sql, new {Year = year });
        }
    }
}

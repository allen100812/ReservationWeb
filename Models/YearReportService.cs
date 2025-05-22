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
            var sql = "select month(Date) as 'Month',count(*) as 'Num' from OrderTB Where year(Date) = @Year Group by month(Date) order by month(Date)";

            return _dbConnection.Query<YearReport>(sql, new { Year = year });
        }
        public IEnumerable<YearReport> GetYearReport(string year)
        {
            var sql = "select month(Date) as 'Month',b.Placetitle as 'Place',c.Name as 'Product',count(*) as 'Num' from OrderTB a ";
            sql = sql + " left join PlaceTB b on a.Placeid=b.Placeid left join ProductTB c on a.Pid=c.Pid";
            sql = sql + " Where year(Date) = @Year";
            sql = sql + " Group by b.Placetitle,month(Date),c.Name ";
            sql = sql + " order by month(Date),b.Placetitle,c.Name";
            return _dbConnection.Query<YearReport>(sql, new {Year = year });
        }
    }
}

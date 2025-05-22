using Dapper;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IPgroupService
    {
        bool AddPgroup(Pgroup Pgroup);
        bool UpdatePgroup(Pgroup Pgroup);
        bool DeletePgroup(string Pid);
        Pgroup GetPgroupById(string Pid);
        IEnumerable<Pgroup> GetPgroupTB();
    }
    public class PgroupService : IPgroupService
    {
        private readonly IDbConnection _dbConnection;
        public PgroupService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public Pgroup GetPgroupById(string Pgid)
        {
            var sql = "SELECT * FROM PgroupTB WHERE Pgid = @Pgid";
            return _dbConnection.QueryFirstOrDefault<Pgroup>(sql, new { Pgid = Pgid });
        }
        public IEnumerable<Pgroup> GetPgroupTB()
        {
            var sql = "SELECT * FROM PgroupTB Order by Pgorder";
            return _dbConnection.Query<Pgroup>(sql);
        }
        public bool AddPgroup(Pgroup Pgroup)
        {
            var sql = "INSERT INTO PgroupTB (Pgname,Pgcontent,Pgorder) VALUES (@Pgname,@Pgcontent,@Pgorder)";
            var affectedRows = _dbConnection.Execute(sql, Pgroup);
            return affectedRows > 0;
        }
        public bool UpdatePgroup(Pgroup Pgroup)
        {
            var sql = "UPDATE PgroupTB SET Pgname = @Pgname, Pgcontent = @Pgcontent,Pgorder=Pgorder WHERE Pgid = @Pgid";
            var affectedRows = _dbConnection.Execute(sql, Pgroup);
            return affectedRows > 0;
        }

        public bool DeletePgroup(string Pgid)
        {
            int pgid = int.Parse(Pgid);

            var sql = "DELETE FROM PgroupTB WHERE Pgid= @Pgid";
            var affectedRows = _dbConnection.Execute(sql, new { Pgid = pgid });
            return affectedRows > 0;
        }
    }
}

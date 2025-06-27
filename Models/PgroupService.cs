using Dapper;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IPgroupService
    {
        int CreatePgroup(Pgroup group);
        bool UpdatePgroup(Pgroup group);
        IEnumerable<Pgroup> GetAllPgroups();
        IEnumerable<Pgroup> GetAllPgroupsIncludeDeleted();
        Pgroup? GetPgroupById(int pgid);
        bool DeletePgroup(int pgid);
        bool RestorePgroup(int pgid);
        bool IsPgroupNameDuplicate(string pgname, int? excludePgid = null);
        bool UpdatePgroupOrder(int pgid, string newOrder);
        int CountPgroups();
        int BulkDeletePgroups(List<int> pgids);
    }

    public class PgroupService : IPgroupService
    {
        private readonly IDbConnection _dbConnection;

        public PgroupService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public int CreatePgroup(Pgroup group)
        {
            var sql = @"
                INSERT INTO PgroupTB (PGname, PGcontent, PGorder, IsDeleted)
                VALUES (@PGname, @PGcontent, @PGorder, 0);
                SELECT LAST_INSERT_ID();";
            return _dbConnection.ExecuteScalar<int>(sql, group);
        }

        public bool UpdatePgroup(Pgroup group)
        {
            var sql = @"
                UPDATE PgroupTB
                SET PGname = @PGname,
                    PGcontent = @PGcontent,
                    PGorder = @PGorder
                WHERE PGid = @PGid AND IsDeleted = 0";
            return _dbConnection.Execute(sql, group) > 0;
        }

        public IEnumerable<Pgroup> GetAllPgroups()
        {
            var sql = "SELECT * FROM PgroupTB WHERE IsDeleted = 0 ORDER BY PGorder ASC";
            return _dbConnection.Query<Pgroup>(sql);
        }

        public IEnumerable<Pgroup> GetAllPgroupsIncludeDeleted()
        {
            var sql = "SELECT * FROM PgroupTB ORDER BY PGorder ASC";
            return _dbConnection.Query<Pgroup>(sql);
        }

        public Pgroup? GetPgroupById(int pgid)
        {
            var sql = "SELECT * FROM PgroupTB WHERE PGid = @PGid";
            return _dbConnection.QueryFirstOrDefault<Pgroup>(sql, new { PGid = pgid });
        }

        public bool DeletePgroup(int pgid)
        {
            var sql = "UPDATE PgroupTB SET IsDeleted = 1 WHERE PGid = @PGid";
            return _dbConnection.Execute(sql, new { PGid = pgid }) > 0;
        }

        public bool RestorePgroup(int pgid)
        {
            var sql = "UPDATE PgroupTB SET IsDeleted = 0 WHERE PGid = @PGid";
            return _dbConnection.Execute(sql, new { PGid = pgid }) > 0;
        }

        public bool IsPgroupNameDuplicate(string pgname, int? excludePgid = null)
        {
            var sql = "SELECT COUNT(*) FROM PgroupTB WHERE PGname = @PGname AND IsDeleted = 0";
            if (excludePgid.HasValue)
                sql += " AND PGid <> @ExcludePGid";

            var count = _dbConnection.ExecuteScalar<int>(sql, new { PGname = pgname, ExcludePGid = excludePgid });
            return count > 0;
        }

        public bool UpdatePgroupOrder(int pgid, string newOrder)
        {
            var sql = "UPDATE PgroupTB SET PGorder = @NewOrder WHERE PGid = @PGid AND IsDeleted = 0";
            return _dbConnection.Execute(sql, new { PGid = pgid, NewOrder = newOrder }) > 0;
        }

        public int CountPgroups()
        {
            var sql = "SELECT COUNT(*) FROM PgroupTB WHERE IsDeleted = 0";
            return _dbConnection.ExecuteScalar<int>(sql);
        }

        public int BulkDeletePgroups(List<int> pgids)
        {
            var sql = "UPDATE PgroupTB SET IsDeleted = 1 WHERE PGid IN @Pgids";
            return _dbConnection.Execute(sql, new { Pgids = pgids });
        }
    }
}

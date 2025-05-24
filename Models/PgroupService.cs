using Dapper;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IPgroupService
    {
        // 新增分類
        int CreatePgroup(Pgroup group);

        // 更新分類
        bool UpdatePgroup(Pgroup group);

        // 取得全部未刪除分類
        IEnumerable<Pgroup> GetAllPgroups();

        // 取得所有分類（包含已刪除）
        IEnumerable<Pgroup> GetAllPgroupsIncludeDeleted();

        // 依主鍵查詢分類
        Pgroup? GetPgroupById(int pgid);

        // 假刪除分類
        bool DeletePgroup(int pgid);

        // 還原分類（取消假刪除）
        bool RestorePgroup(int pgid);

        // 檢查分類名稱是否重複（新增、編輯時）
        bool IsPgroupNameDuplicate(string pgname, int? excludePgid = null);

        // 更新分類順序代碼
        bool UpdatePgroupOrder(int pgid, string newOrder);

        // 統計目前總分類數（不含已刪除）
        int CountPgroups();

        // 批次假刪除
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
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
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

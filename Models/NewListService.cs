using Dapper;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Web0524.Models
{
    public interface INewService
    {
        bool AddNewList(NewList NewList);
        bool UpdateNewList(NewList NewList);
        bool DeleteNewList(int NewId);
        bool StopNewList(NewList NewList);
        bool StartNewList(NewList NewList);
        bool PushNewList(NewList NewList);
        NewList GetNewListById(int NewId);
        IEnumerable<NewList> GetNewTB();
        IEnumerable<NewList> GetNewTB_Top2();
    }
    public class NewListService:INewService
    {
        private readonly IDbConnection _dbConnection;
        public NewListService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public NewList GetNewListById(int NewId)
        {
            var sql = "SELECT * FROM NewTB WHERE NewId = @NewId";
            return _dbConnection.QueryFirstOrDefault<NewList>(sql, new { NewId = NewId });
        }
        public IEnumerable<NewList> GetNewTB()
        {
            var sql = "SELECT Top 100 * FROM NewTB where Status <> 2 Order by Status ASC,PublishDate ";
            return _dbConnection.Query<NewList>(sql);
        }
        public IEnumerable<NewList> GetNewTB_Top2()
        {
            var sql = "SELECT Top 2 * FROM NewTB where Status <> 2 Order by Status ASC,PublishDate ";
            return _dbConnection.Query<NewList>(sql);
        }
        public bool AddNewList(NewList NewList)
        {
            
            var sql = "INSERT INTO NewTB (Title,Content,Author,PublishDate,Status,Category,Tags,Photo) VALUES (@Title,@Content,@Author,@PublishDate,@Status,@Category,@Tags,@Photo)";
            var affectedRows = _dbConnection.Execute(sql, NewList);
            return affectedRows > 0;
        }
        public bool UpdateNewList(NewList NewList)
        {
            var sql = "UPDATE NewTB SET Title = @Title, Content = @Content, Author = @Author, PublishDate = @PublishDate, Status = @Status, Category = @Category, Tags = @Tags,Photo = @Photo WHERE NewId = @NewId";
            var affectedRows = _dbConnection.Execute(sql, NewList);
            return affectedRows > 0;
        }
        public bool StopNewList(NewList NewList)
        {
            var sql = "UPDATE NewTB SET Status = 2 WHERE NewId = @NewId";
            var affectedRows = _dbConnection.Execute(sql, NewList);
            return affectedRows > 0;
        }
        public bool StartNewList(NewList NewList)
        {
            var sql = "UPDATE NewTB SET Status = 0 WHERE NewId = @NewId";
            var affectedRows = _dbConnection.Execute(sql, NewList);
            return affectedRows > 0;
        }
        public bool PushNewList(NewList NewList)
        {
            var sql = "UPDATE NewTB SET Status = 1 WHERE NewId = @NewId";
            var affectedRows = _dbConnection.Execute(sql, NewList);
            return affectedRows > 0;
        }
        public bool DeleteNewList(int NewId)
        {
            var sql = "DELETE FROM NewTB WHERE NewId= @NewId";
            var affectedRows = _dbConnection.Execute(sql, new { NewId = NewId });
            return affectedRows > 0;
        }
    }
}

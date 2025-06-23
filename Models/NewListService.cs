using Dapper;
using System.Data;
using System.Transactions;

namespace Web0524.Models
{

    public interface INewService
    {
        bool AddNewList(NewList newList);
        bool UpdateNewList(NewList newList);
        bool DeleteNewList(int newId);
        bool UpdateStatus(int newId, int status);
        NewList GetNewListById(int newId);
        IEnumerable<NewList> GetNewTB();
        IEnumerable<NewList> GetNewTB_Top2();
    }

    public class NewListService : INewService
    {
        private readonly IDbConnection _dbConnection;

        public NewListService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public NewList GetNewListById(int newId)
        {
            var news = _dbConnection.QueryFirstOrDefault<NewList>(
                "SELECT * FROM NewTB WHERE NewId = @NewId", new { NewId = newId });

            if (news != null)
            {
                var photos = _dbConnection.Query<byte[]>(
                    "SELECT Photo FROM NewPhotoTB WHERE NewId = @NewId", new { NewId = newId });
                news.PhotoList = photos.ToList();
            }

            return news;
        }

        public IEnumerable<NewList> GetNewTB()
        {
            string sql = "SELECT TOP 100 * FROM NewTB  ORDER BY Status ASC, PublishDate DESC";
            return _dbConnection.Query<NewList>(sql);
        }

        public IEnumerable<NewList> GetNewTB_Top2()
        {
            string sql = "SELECT TOP 2 * FROM NewTB WHERE Status <> 2 ORDER BY Status ASC, PublishDate DESC";
            return _dbConnection.Query<NewList>(sql);
        }

        public bool AddNewList(NewList newList)
        {
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            using (var scope = new TransactionScope())
            {
                try
                {
                    string sql = @"
INSERT INTO NewTB (Title, Content, Author, PublishDate, Status, Category, Tag, TopTime)
VALUES (@Title, @Content, @Author, @PublishDate, @Status, @Category, @Tag, @TopTime);
SELECT CAST(SCOPE_IDENTITY() AS INT)";


                    int newId = _dbConnection.QuerySingle<int>(sql, newList);

                    foreach (var photo in newList.PhotoList)
                    {
                        string photoSql = "INSERT INTO NewPhotoTB (NewId, Photo) VALUES (@NewId, @Photo)";
                        _dbConnection.Execute(photoSql, new { NewId = newId, Photo = photo });
                    }

                    scope.Complete();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("新增新聞失敗：" + ex.Message);
                    return false;
                }
            }
        }

        public bool UpdateNewList(NewList newList)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    string sql = @"
UPDATE NewTB SET
    Title = @Title,
    Content = @Content,
    Author = @Author,
    PublishDate = @PublishDate,
    Status = @Status,
    Category = @Category,
    Tag = @Tag,
    TopTime = @TopTime
WHERE NewId = @NewId";


                    _dbConnection.Execute(sql, newList);

                    _dbConnection.Execute("DELETE FROM NewPhotoTB WHERE NewId = @NewId", new { newList.NewId });

                    foreach (var photo in newList.PhotoList)
                    {
                        string photoSql = "INSERT INTO NewPhotoTB (NewId, Photo) VALUES (@NewId, @Photo)";
                        _dbConnection.Execute(photoSql, new { NewId = newList.NewId, Photo = photo });
                    }

                    scope.Complete();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("更新新聞失敗：" + ex.Message);
                    return false;
                }
            }
        }

        public bool DeleteNewList(int newId)
        {
            using (var scope = new TransactionScope())
            {
                try
                {
                    _dbConnection.Execute("DELETE FROM NewPhotoTB WHERE NewId = @NewId", new { NewId = newId });
                    _dbConnection.Execute("DELETE FROM NewTB WHERE NewId = @NewId", new { NewId = newId });

                    scope.Complete();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("刪除新聞失敗：" + ex.Message);
                    return false;
                }
            }
        }

        public bool UpdateStatus(int newId, int status)
        {
            string sql = "UPDATE NewTB SET Status = @Status WHERE NewId = @NewId";
            return _dbConnection.Execute(sql, new { Status = status, NewId = newId }) > 0;
        }
    }

}

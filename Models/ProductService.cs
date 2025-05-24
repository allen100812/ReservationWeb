using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IProductService
    {
        // 新增產品
        int CreateProduct(Product product);

        // 更新產品
        bool UpdateProduct(Product product);

        // 刪除產品（依編號）
        bool DeleteProduct(int productId);

        // 取得所有產品
        IEnumerable<Product> GetAllProducts();

        // 依產品編號取得單筆資料
        Product GetProductById(int productId);

        // 依產品名稱模糊搜尋
        IEnumerable<Product> SearchProductsByName(string keyword);

        // 依產品群組查詢
        IEnumerable<Product> GetProductsByGroup(int pgid);

        // 依產品狀態查詢（啟用中 / 停用）
        IEnumerable<Product> GetProductsByState(int productState);

        // 設定產品狀態（上架、下架）
        bool ChangeProductState(int productId, int newState);

        // 設定產品排序代碼（ProductOrder）
        bool UpdateProductOrder(int productId, string productOrder);

        // 驗證產品名稱是否重複（用於新增/編輯）
        bool IsProductNameDuplicate(string productName, int? excludeProductId = null);

        // 取得推薦產品（價格最高前 N 筆 / 狀態為啟用）
        IEnumerable<Product> GetTopProductsByPrice(int topN);

        // 統計：產品總數
        int CountAllProducts();

        // 統計：各群組產品數量
        Dictionary<int, int> GetProductCountByGroup();

        // 批次更新產品狀態
        int BulkUpdateProductState(List<int> productIds, int newState);

        // 批次刪除產品
        int BulkDeleteProducts(List<int> productIds);

        //還原產品
        bool RestoreProduct(int productId);
    }
    public class ProductService: IProductService
    {
        private readonly IDbConnection _dbConnection;
        public ProductService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public int CreateProduct(Product product)
        {
            var sql = @"
            INSERT INTO ProductTB (PGid, ProductState, Name, Price, Content, Photo, ProductOrder, IsDeleted)
            VALUES (@PGid, @ProductState, @Name, @Price, @Content, @Photo, @ProductOrder, 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _dbConnection.ExecuteScalar<int>(sql, product);
        }

        public bool UpdateProduct(Product product)
        {
            var sql = @"
            UPDATE ProductTB
            SET PGid = @PGid,
                ProductState = @ProductState,
                Name = @Name,
                Price = @Price,
                Content = @Content,
                Photo = @Photo,
                ProductOrder = @ProductOrder
            WHERE ProductId = @ProductId AND IsDeleted = 0";
            return _dbConnection.Execute(sql, product) > 0;
        }

        public bool DeleteProduct(int productId)
        {
            var sql = "UPDATE ProductTB SET IsDeleted = 1 WHERE ProductId = @ProductId";
            return _dbConnection.Execute(sql, new { ProductId = productId }) > 0;
        }

        public IEnumerable<Product> GetAllProducts()
        {
            var sql = "SELECT * FROM ProductTB WHERE IsDeleted = 0";
            return _dbConnection.Query<Product>(sql);
        }

        public Product GetProductById(int productId)
        {
            var sql = "SELECT * FROM ProductTB WHERE ProductId = @ProductId AND IsDeleted = 0";
            return _dbConnection.QueryFirstOrDefault<Product>(sql, new { ProductId = productId });
        }

        public IEnumerable<Product> SearchProductsByName(string keyword)
        {
            var sql = "SELECT * FROM ProductTB WHERE Name LIKE @Keyword AND IsDeleted = 0";
            return _dbConnection.Query<Product>(sql, new { Keyword = $"%{keyword}%" });
        }

        public IEnumerable<Product> GetProductsByGroup(int pgid)
        {
            var sql = "SELECT * FROM ProductTB WHERE PGid = @PGid AND IsDeleted = 0";
            return _dbConnection.Query<Product>(sql, new { PGid = pgid });
        }

        public IEnumerable<Product> GetProductsByState(int productState)
        {
            var sql = "SELECT * FROM ProductTB WHERE ProductState = @ProductState AND IsDeleted = 0";
            return _dbConnection.Query<Product>(sql, new { ProductState = productState });
        }

        public bool ChangeProductState(int productId, int newState)
        {
            var sql = "UPDATE ProductTB SET ProductState = @NewState WHERE ProductId = @ProductId AND IsDeleted = 0";
            return _dbConnection.Execute(sql, new { ProductId = productId, NewState = newState }) > 0;
        }

        public bool UpdateProductOrder(int productId, string productOrder)
        {
            var sql = "UPDATE ProductTB SET ProductOrder = @ProductOrder WHERE ProductId = @ProductId AND IsDeleted = 0";
            return _dbConnection.Execute(sql, new { ProductId = productId, ProductOrder = productOrder }) > 0;
        }

        public bool IsProductNameDuplicate(string productName, int? excludeProductId = null)
        {
            var sql = "SELECT COUNT(*) FROM ProductTB WHERE Name = @ProductName AND IsDeleted = 0";
            if (excludeProductId.HasValue)
                sql += " AND ProductId <> @ExcludeProductId";

            var count = _dbConnection.ExecuteScalar<int>(sql, new { ProductName = productName, ExcludeProductId = excludeProductId });
            return count > 0;
        }

        public IEnumerable<Product> GetTopProductsByPrice(int topN)
        {
            var sql = $"SELECT TOP (@TopN) * FROM ProductTB WHERE ProductState = 1 AND IsDeleted = 0 ORDER BY Price DESC";
            return _dbConnection.Query<Product>(sql, new { TopN = topN });
        }

        public int CountAllProducts()
        {
            var sql = "SELECT COUNT(*) FROM ProductTB WHERE IsDeleted = 0";
            return _dbConnection.ExecuteScalar<int>(sql);
        }

        public Dictionary<int, int> GetProductCountByGroup()
        {
            var sql = "SELECT PGid, COUNT(*) AS Count FROM ProductTB WHERE IsDeleted = 0 GROUP BY PGid";
            return _dbConnection.Query(sql).ToDictionary(r => (int)r.PGid, r => (int)r.Count);
        }

        public int BulkUpdateProductState(List<int> productIds, int newState)
        {
            var sql = "UPDATE ProductTB SET ProductState = @NewState WHERE ProductId IN @ProductIds AND IsDeleted = 0";
            return _dbConnection.Execute(sql, new { ProductIds = productIds, NewState = newState });
        }

        public int BulkDeleteProducts(List<int> productIds)
        {
            var sql = "UPDATE ProductTB SET IsDeleted = 1 WHERE ProductId IN @ProductIds";
            return _dbConnection.Execute(sql, new { ProductIds = productIds });
        }

        public bool RestoreProduct(int productId)
        {
            var sql = "UPDATE ProductTB SET IsDeleted = 0 WHERE ProductId = @ProductId";
            return _dbConnection.Execute(sql, new { ProductId = productId }) > 0;
        }

    }
}

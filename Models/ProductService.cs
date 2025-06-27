using Dapper;
using System;
using System.Collections.Generic;
using System.Data;

namespace Web0524.Models
{
    public interface IProductService
    {
        int CreateProduct(Product product);
        bool UpdateProduct(Product product);
        bool DeleteProduct(int productId);
        IEnumerable<Product> GetAllProducts();
        Product GetProductById(int productId);
        IEnumerable<Product> SearchProductsByName(string keyword);
        IEnumerable<Product> GetProductsByGroup(int pgid);
        IEnumerable<Product> GetProductsByState(int productState);
        bool ChangeProductState(int productId, int newState);
        bool UpdateProductOrder(int productId, string productOrder);
        bool IsProductNameDuplicate(string productName, int? excludeProductId = null);
        IEnumerable<Product> GetTopProductsByPrice(int topN);
        int CountAllProducts();
        Dictionary<int, int> GetProductCountByGroup();
        int BulkUpdateProductState(List<int> productIds, int newState);
        int BulkDeleteProducts(List<int> productIds);
        bool RestoreProduct(int productId);
    }

    public class ProductService : IProductService
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
                SELECT LAST_INSERT_ID();";
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
            var sql = @"SELECT * FROM ProductTB
                        WHERE ProductState = 1 AND IsDeleted = 0
                        ORDER BY Price DESC
                        LIMIT @TopN";
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
            return _dbConnection.Query(sql)
                .ToDictionary(row => (int)row.PGid, row => (int)row.Count);
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

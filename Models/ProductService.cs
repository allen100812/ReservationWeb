using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IProductService
    {
        bool AddProduct(Product product);
        bool UpdateProduct(Product product);
        bool UpdateProductOrderSw(int pid, int orderSw);
        bool UpdateProductOrderPlaceid(int pid, string placeid);

        bool UpdateProductGroup(int pid, int pgid);
        bool DeleteProduct(string Pid);
        
        Product GetProductById(string Pid);
        Product GetProductByName(string Pname);
        IEnumerable<Product> GetProductTB();

        IEnumerable<Product> GetProductTB_PlaceNotNull();
        IEnumerable<Product> GetProductByPgid(string Pgid);
    }
    public class ProductService: IProductService
    {
        private readonly IDbConnection _dbConnection;
        public ProductService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public Product GetProductById(string Pid)
        {
            var sql = "SELECT a.*,b.pgname,b.pgcontent,b.pgorder FROM ProductTB a left join PgroupTB b on a.Pgid=b.Pgid WHERE Pid = @Pid";
            return _dbConnection.QueryFirstOrDefault<Product>(sql, new { Pid = Pid });
        }
        public Product GetProductByName(string name)
        {
            var sql = "SELECT a.*,b.pgname,b.pgcontent,b.pgorder FROM ProductTB a left join PgroupTB b on a.Pgid=b.Pgid WHERE name = @name";
            return _dbConnection.QueryFirstOrDefault<Product>(sql, new { name = name });
        }
        public IEnumerable<Product> GetProductTB()
        {
            var sql = "SELECT a.*,b.pgname,b.pgcontent,b.pgorder FROM ProductTB a left join PgroupTB b on a.Pgid=b.Pgid Where Porder <> 'off' Order by Pgorder,Porder";
            return _dbConnection.Query<Product>(sql);
        }
        public IEnumerable<Product> GetProductTB_PlaceNotNull()
        {
            var sql = "SELECT a.*,b.pgname,b.pgcontent,b.pgorder FROM ProductTB a left join PgroupTB b on a.Pgid=b.Pgid Where Porder <> 'off' and EXISTS (SELECT 1 FROM PlaceTB AS Pl WHERE CHARINDEX(CONVERT(NVARCHAR, a.pid), Pl.Placepid) > 0 and Pl.Placeorder <> 'off') Order by a.Pgorder,a.Porder";
            return _dbConnection.Query<Product>(sql);
        }
        public IEnumerable<Product> GetProductByPgid(string Pgid)
        {
            var sql = "SELECT a.*,b.pgname,b.pgcontent,b.pgorder FROM ProductTB a left join PgroupTB b on a.Pgid=b.Pgid Where Porder <> 'off' and a.Pgid=@Pgid and EXISTS (SELECT 1 FROM PlaceTB AS Pl WHERE CHARINDEX(CONVERT(NVARCHAR, a.pid), Pl.Placepid) > 0) Order by Pgorder,Porder";
            return _dbConnection.Query<Product>(sql, new { Pgid = Pgid });
        }
        public bool AddProduct(Product product)
        {
            //檢測是否有重複帳號
            if (GetProductByName(product.Name) == null)
            {
                var sql = "INSERT INTO ProductTB (Name, Price,Unit,Content,Photo,OrderSw,Pgid,Event,Porder,SpendTime) VALUES (@Name, @Price,@Unit,@Content,@Photo,1,@Pgid,@Event,@Porder,@SpendTime)";
                var affectedRows = _dbConnection.Execute(sql, product);
                return affectedRows > 0;
            }
            else
            {
                return false;
            }
        }
        public bool UpdateProduct(Product Product)
        {
            //檢測是否有重複帳號
            Product Product_temp = GetProductByName(Product.Name);
            if (Product_temp == null || Product_temp.ProductId == Product.ProductId)
            {
                var sql = "UPDATE ProductTB SET Name = @Name, Price = @Price,Unit=@Unit,Content=@Content,Photo=@Photo,OrderSw=@OrderSw,Pgid=@Pgid,Event=@Event,Porder=@Porder,SpendTime=@SpendTime WHERE Pid = @Pid";
                var affectedRows = _dbConnection.Execute(sql, Product);
                return affectedRows > 0;
            }
            else
            {
                return false;
            }
        }
        public bool UpdateProductOrderSw(int pid, int orderSw)
        {
            var sql = "UPDATE ProductTB SET OrderSw = @OrderSw WHERE Pid = @Pid";
            var parameters = new { OrderSw = orderSw, Pid = pid };
            var affectedRows = _dbConnection.Execute(sql, parameters);
            return affectedRows > 0;
        }
        public bool UpdateProductOrderPlaceid(int pid, string placeid)
        {
            var sql = "UPDATE ProductTB SET Placeid = @Placeid WHERE Pid = @Pid";
            var parameters = new {Placeid = placeid, Pid = pid };
            var affectedRows = _dbConnection.Execute(sql, parameters);
            return affectedRows > 0;
        }
        public bool UpdateProductGroup(int pid, int pgid)
        {
            var sql = "UPDATE ProductTB SET Pgid = @pgid WHERE Pid = @Pid";
            var parameters = new { Pgid = pgid, Pid = pid };
            var affectedRows = _dbConnection.Execute(sql, parameters);
            return affectedRows > 0;
        }
        public bool DeleteProduct(string Pid)
        {
            int pid = int.Parse(Pid);
            var sql = "UPDATE ProductTB SET Name = '(Del)'+Name,Porder='off' WHERE Pid= @Pid";
            var affectedRows = _dbConnection.Execute(sql, new { Pid = pid });
            return affectedRows > 0;
        }
    }
}

using Dapper;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Web0524.Models
{

    public interface IPortfolioGroupService
    {
        Task<IEnumerable<PortfolioGroup>> GetAllAsync();
        Task<PortfolioGroup?> GetByIdAsync(int id);
        Task<int> CreateAsync(PortfolioGroup group);
        Task<bool> UpdateAsync(PortfolioGroup group);
        Task<bool> DeleteAsync(int id); // ⚠️ 若仍有作品集，禁止刪除
    }

    public interface IPortfolioService
    {
        Task<IEnumerable<Portfolio>> GetAllAsync();

        Task<IEnumerable<Portfolio>> GetPublishedAsync(); // ✅ 加這行

        Task<Portfolio?> GetByIdAsync(int id);
        Task<int> CreateAsync(Portfolio model, List<byte[]> photos);
        Task<bool> UpdateAsync(Portfolio model, List<byte[]> newPhotos, List<int> deletePhotoIds);
        Task<bool> DeleteAsync(int id);

        Task<bool> DeletePhotoAsync(int photoId);

    }
    public class PortfolioGroupService : IPortfolioGroupService
    {
        private readonly IDbConnection _db;

        public PortfolioGroupService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<PortfolioGroup>> GetAllAsync()
        {
            return await _db.QueryAsync<PortfolioGroup>("SELECT * FROM PortfolioGroupTB");
        }

        public async Task<PortfolioGroup?> GetByIdAsync(int id)
        {
            return await _db.QueryFirstOrDefaultAsync<PortfolioGroup>(
                "SELECT * FROM PortfolioGroupTB WHERE PortfolioGroup_Id = @id", new { id });
        }

        public async Task<int> CreateAsync(PortfolioGroup group)
        {
            var sql = @"INSERT INTO PortfolioGroupTB (PortfolioGroup_Name, PortfolioGroup_Content)
                    VALUES (@PortfolioGroup_Name, @PortfolioGroup_Content);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return await _db.ExecuteScalarAsync<int>(sql, group);
        }

        public async Task<bool> UpdateAsync(PortfolioGroup group)
        {
            var sql = @"UPDATE PortfolioGroupTB
                    SET PortfolioGroup_Name = @PortfolioGroup_Name,
                        PortfolioGroup_Content = @PortfolioGroup_Content
                    WHERE PortfolioGroup_Id = @PortfolioGroup_Id";
            return await _db.ExecuteAsync(sql, group) > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // ⚠️ 判斷是否還有作品集
            var count = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM PortfolioTB WHERE PortfolioGroup_Id = @id", new { id });

            if (count > 0)
                return false;

            return await _db.ExecuteAsync(
                "DELETE FROM PortfolioGroupTB WHERE PortfolioGroup_Id = @id", new { id }) > 0;
        }
    }

    public class PortfolioService : IPortfolioService
    {
        private readonly IDbConnection _db;

        public PortfolioService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Portfolio>> GetAllAsync()
        {
            var portfolios = (await _db.QueryAsync<Portfolio>("SELECT * FROM PortfolioTB")).ToList();

            foreach (var p in portfolios)
            {
                var photos = await _db.QueryAsync<PortfolioPhoto>(
                    "SELECT Photo_Id, Portfolio_Id, Photo FROM PortfolioPhotoTB WHERE Portfolio_Id = @id",
                    new { id = p.Portfolio_Id});

                p.PhotoList = photos.ToList();
            }

            return portfolios;
        }
        public async Task<IEnumerable<Portfolio>> GetPublishedAsync()
        {
            var portfolios = (await _db.QueryAsync<Portfolio>(
                "SELECT * FROM PortfolioTB WHERE IsPublished = 1"
            )).ToList();

            foreach (var p in portfolios)
            {
                var photos = await _db.QueryAsync<PortfolioPhoto>(
                    "SELECT Photo_Id, Portfolio_Id, Photo FROM PortfolioPhotoTB WHERE Portfolio_Id = @id",
                    new { id = p.Portfolio_Id });

                p.PhotoList = photos.ToList();
            }

            return portfolios;
        }

        public async Task<Portfolio?> GetByIdAsync(int id)
        {
            var portfolio = await _db.QueryFirstOrDefaultAsync<Portfolio>(
                "SELECT * FROM PortfolioTB WHERE Portfolio_Id = @id", new { id });

            if (portfolio != null)
            {
                var photos = await _db.QueryAsync<PortfolioPhoto>(
                    "SELECT Photo_Id, Portfolio_Id, Photo FROM PortfolioPhotoTB WHERE Portfolio_Id = @id", new { id });
                portfolio.PhotoList = photos.ToList();
            }

            return portfolio;
        }

        public async Task<int> CreateAsync(Portfolio model, List<byte[]> photos)
        {
            var sql = @"
                INSERT INTO PortfolioTB (Portfolio_Title, Portfolio_Content, Portfolio_Photo, Portfolio_URL, IsPublished)
                VALUES (@Portfolio_Title, @Portfolio_Content, NULL, @Portfolio_URL, @IsPublished);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            int newId = await _db.ExecuteScalarAsync<int>(sql, model);

            if (photos != null && photos.Count > 0)
            {
                foreach (var photo in photos)
                {
                    await _db.ExecuteAsync(
                        "INSERT INTO PortfolioPhotoTB (Portfolio_Id, Photo) VALUES (@pid, @photo)",
                        new { pid = newId, photo });
                }
            }

            return newId;
        }

        public async Task<bool> UpdateAsync(Portfolio model, List<byte[]> newPhotos, List<int> deletePhotoIds)
        {
            // 1. 更新主資料
            var sql = @"
                UPDATE PortfolioTB SET
PortfolioGroup_Id = @PortfolioGroup_Id,
                    Portfolio_Title = @Portfolio_Title,
                    Portfolio_Content = @Portfolio_Content,
                    Portfolio_URL = @Portfolio_URL,
                    IsPublished = @IsPublished
                WHERE Portfolio_Id = @Portfolio_Id";
            int rows = await _db.ExecuteAsync(sql, model);

            // 2. 刪除圖片
            if (deletePhotoIds?.Any() == true)
            {
                await _db.ExecuteAsync("DELETE FROM PortfolioPhotoTB WHERE Photo_Id IN @ids", new { ids = deletePhotoIds });
            }

            // 3. 新增圖片
            if (newPhotos?.Any() == true)
            {
                foreach (var photo in newPhotos)
                {
                    await _db.ExecuteAsync(
                        "INSERT INTO PortfolioPhotoTB (Portfolio_Id, Photo) VALUES (@pid, @photo)",
                        new { pid = model.Portfolio_Id, photo });
                }
            }

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await _db.ExecuteAsync("DELETE FROM PortfolioPhotoTB WHERE Portfolio_Id = @id", new { id });
            var rows = await _db.ExecuteAsync("DELETE FROM PortfolioTB WHERE Portfolio_Id = @id", new { id });
            return rows > 0;
        }

        public async Task<bool> DeletePhotoAsync(int photoId)
        {
            var rows = await _db.ExecuteAsync("DELETE FROM PortfolioPhotoTB WHERE Photo_Id = @id", new { id = photoId });
            return rows > 0;
        }

    }

}

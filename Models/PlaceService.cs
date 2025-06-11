using Dapper;
using MDP.DevKit.LineMessaging;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IPlaceService {
        bool AddPlace(Place Place);
        bool UpdatePlace(Place Place);
        bool DeletePlace(string Placeid);
        Place GetPlaceById(string Placeid);
        IEnumerable<Place> GetPlaceTB();

    }
    public class PlaceService : IPlaceService
    {
        private readonly IDbConnection _dbConnection;
        public PlaceService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public Place GetPlaceById(string Placeid)
        {
            var sql = "SELECT * FROM PlaceTB WHERE Placeid = @Placeid";
            return _dbConnection.QueryFirstOrDefault<Place>(sql, new { Placeid = Placeid });
        }
        public IEnumerable<Place> GetPlaceTB()
        {
            var sql = "SELECT * FROM PlaceTB Order by Placeorder";
                return _dbConnection.Query<Place>(sql);
        }
        public bool AddPlace(Place Place)
        {
            var sql = "INSERT INTO PlaceTB (Placetitle,Placeaddress,Placeorder,PlaceSw,Placepid,Placemapurl) VALUES (@Placetitle,@Placeaddress,@Placeorder,1,@Placepid,@Placemapurl)";
            var affectedRows = _dbConnection.Execute(sql, Place);
            return affectedRows > 0;
        }
        public bool UpdatePlace(Place Place)
        {
            var sql = "UPDATE PlaceTB SET Placetitle = @Placetitle, Placeaddress = @Placeaddress,Placeorder=@Placeorder,PlaceSw=@PlaceSw,Placepid=@Placepid,Placemapurl=@Placemapurl WHERE Placeid = @Placeid";
            var affectedRows = _dbConnection.Execute(sql, Place);
            return affectedRows > 0;
        }

        public bool DeletePlace(string Placeid)
        {
            int aid = int.Parse(Placeid);
            var sql = @"UPDATE PlaceTB 
                SET IsDeleted = 1 
                WHERE Placeid = @Placeid";
            var affectedRows = _dbConnection.Execute(sql, new { Placeid = aid });
            return affectedRows > 0;
        }

    }
}

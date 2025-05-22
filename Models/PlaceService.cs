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
        Place GetPlaceByTitle(string Placetitle);
        IEnumerable<Place> GetPlaceTB();
        IEnumerable<Place> GetPlaceByPid(string pid);

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
            var sql = "SELECT * FROM PlaceTB WHERE Placeid = @Placeid and Placeorder <> 'off'";
            return _dbConnection.QueryFirstOrDefault<Place>(sql, new { Placeid = Placeid });
        }
        public Place GetPlaceByTitle(string Placetitle)
        {
            var sql = "SELECT * FROM PlaceTB WHERE Placetitle = @Placetitle ";
            return _dbConnection.QueryFirstOrDefault<Place>(sql, new { Placetitle = Placetitle });
        }
        public IEnumerable<Place> GetPlaceByPid(string pid)
        {
            var sql = "SELECT * FROM PlaceTB WHERE Placesw = 1 AND CONCAT(',', Placepid, ',') LIKE CONCAT('%,', @pid, ',%') and Placeorder <> 'off' Order by Placeorder;";
            return _dbConnection.Query<Place>(sql, new { pid = pid });

        }
        public IEnumerable<Place> GetPlaceTB()
        {
            var sql = "SELECT * FROM PlaceTB Where Placeorder <> 'off' Order by Placeorder";
                return _dbConnection.Query<Place>(sql);
        }
        public bool AddPlace(Place Place)
        {
            //檢測是否有重複帳號
            if (GetPlaceByTitle(Place.Placetitle) == null)
            {
                var sql = "INSERT INTO PlaceTB (Placetitle,Placeaddress,Placeorder,PlaceSw,Placepid,Placemapurl) VALUES (@Placetitle,@Placeaddress,@Placeorder,1,@Placepid,@Placemapurl)";
                var affectedRows = _dbConnection.Execute(sql, Place);
                return affectedRows > 0;
            }
            else
            {
                return false;
            }
        }
        public bool UpdatePlace(Place Place)
        {
            //檢測是否有重複帳號
            Place Place_temp = GetPlaceByTitle(Place.Placetitle);
            if (Place_temp == null || Place_temp.Placeid == Place.Placeid)
            {
                var sql = "UPDATE PlaceTB SET Placetitle = @Placetitle, Placeaddress = @Placeaddress,Placeorder=@Placeorder,PlaceSw=@PlaceSw,Placepid=@Placepid,Placemapurl=@Placemapurl WHERE Placeid = @Placeid";
                var affectedRows = _dbConnection.Execute(sql, Place);
                return affectedRows > 0;
            }
            else
            {
                return false;
            }
        }

        public bool DeletePlace(string Placeid)
        {
            int aid = int.Parse(Placeid);
            var sql = "UPDATE PlaceTB SET Placetitle='(Del)'+Placetitle,Placeorder = 'off' WHERE Placeid= @Placeid";
            var affectedRows = _dbConnection.Execute(sql, new { Placeid = aid });
            return affectedRows > 0;
        }

    }
}

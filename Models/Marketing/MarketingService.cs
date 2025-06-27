using Dapper;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Web0524.Models.Marketing
{
    public interface IMarketingService
    {

        List<Coupon> GetAllCoupons();

        void AddSystemCoupon(Coupon coupon);

        // ✅ 查詢會員尚未使用的優惠券記錄
        List<CouponDispatchRecord> GetAvailableCouponRecords(string memberId);

        // ✅ 查詢會員所有優惠券記錄（包含已使用）
        List<CouponDispatchRecord> GetAllCouponRecords(string memberId);

        // ✅ 根據 CouponId 陣列取得優惠券內容
        List<Coupon> GetCouponsByIds(List<int> couponIds);

        // ✅ 將記錄轉為 Coupon 對照表（RecordId → Coupon）
        Dictionary<int, Coupon> GetCouponMapByRecordList(List<CouponDispatchRecord> records);

        // ✅ 查詢排程紀錄（一年前到一年後）
        List<CouponDispatchRecord> GetDispatchCalendar(DateTime center);

        // ✅ 自動派發排程處理
        Task RunAutoDispatchAsync();

        // ✅ 註冊立即發放優惠券
        void AssignRegisterCoupons(string memberId);

        // ✅ 標記優惠券為已使用（通用函式）
        bool MarkCouponAsUsed(int recordId);

        // ✅ 產生條碼憑證
        string GenerateCouponQRCode(int recordId);

        // ✅ 根據條碼查找優惠券記錄（條碼掃描用）
        CouponDispatchRecord? GetCouponDispatchRecordByQRCode(string qrCode);

        // ✅ 根據 RecordId 查找優惠券記錄
        CouponDispatchRecord? GetCouponDispatchRecord(int recordId);

        void AssignCouponToMember(string memberId, int couponId);
        void ToggleCouponStatus(int couponId);
        string ApplyCouponToOrder(string qrCode, Order order);

        string ApplyCouponByRecordId(int recordId, Order order);

        List<CouponDispatchRecord> SearchCouponRecords(string keyword);

        bool DeleteCouponRecord(int recordId);




        int GetMemberPoints(string memberId);
        void AddPoints(string memberId, int points, string action);
        bool DeductPoints(string memberId, int points, string action);
        string GetMemberLevel(string memberId);
        List<PointLog> GetPointHistory(string memberId);









        ////
    }


    public class MarketingService : IMarketingService
    {
        private readonly IDbConnection _dbConnection;

        public MarketingService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public List<Coupon> GetAllCoupons()
        {
            return _dbConnection.Query<Coupon>("SELECT * FROM CouponTB").ToList();
        }

        public void AddSystemCoupon(Coupon coupon)
        {
            if (!coupon.AutoAssign)
            {
                coupon.AutoAssignRule = AutoAssignRuleEnum.None;
                coupon.AutoDate = null;
            }
            else if (coupon.AutoAssignRule != AutoAssignRuleEnum.SpecificDate)
            {
                coupon.AutoDate = null;
            }

            string sql = @"
            INSERT INTO CouponTB 
            (Title, Code, DiscountType, DiscountAmount, FixedDiscountAmount, MinAmount, ValidFrom, ValidTo,
             ForFirstTimeUser, IsWelcome, IsActive, CouponSource, CategoryLimit, AutoAssign, AutoAssignRule, AutoDate, Remark)
            VALUES
            (@Title, @Code, @DiscountType, @DiscountAmount, @FixedDiscountAmount, @MinAmount, @ValidFrom, @ValidTo,
             @ForFirstTimeUser, @IsWelcome, @IsActive, @CouponSource, @CategoryLimit, @AutoAssign, @AutoAssignRule, @AutoDate, @Remark)";

            _dbConnection.Execute(sql, coupon);
        }

        public List<CouponDispatchRecord> GetAvailableCouponRecords(string memberId)
        {
            var sql = @"SELECT * FROM CouponDispatchRecordTB WHERE MemberId = @memberId AND IsDispatched = 0";
            return _dbConnection.Query<CouponDispatchRecord>(sql, new { memberId }).ToList();
        }

        public List<Coupon> GetCouponsByIds(List<int> couponIds)
        {
            if (couponIds == null || !couponIds.Any()) return new List<Coupon>();
            var sql = "SELECT * FROM CouponTB WHERE CouponId IN @ids";
            return _dbConnection.Query<Coupon>(sql, new { ids = couponIds }).ToList();
        }

        public List<CouponDispatchRecord> GetAllCouponRecords(string memberId)
        {
            var sql = @"SELECT * FROM CouponDispatchRecordTB 
                    WHERE MemberId = @memberId ORDER BY DispatchDate DESC";
            return _dbConnection.Query<CouponDispatchRecord>(sql, new { memberId }).ToList();
        }

        public Dictionary<int, Coupon> GetCouponMapByRecordList(List<CouponDispatchRecord> records)
        {
            var couponIds = records.Select(r => r.CouponId).Distinct().ToList();
            var couponList = GetCouponsByIds(couponIds);
            return couponList.ToDictionary(c => c.CouponId);
        }

        public List<CouponDispatchRecord> GetDispatchCalendar(DateTime center)
        {
            var start = center.AddYears(-1);
            var end = center.AddYears(1);
            var sql = "SELECT * FROM CouponDispatchRecordTB WHERE DispatchDate BETWEEN @start AND @end ORDER BY DispatchDate DESC";
            return _dbConnection.Query<CouponDispatchRecord>(sql, new { start, end }).ToList();
        }

        public async Task RunAutoDispatchAsync()
        {
            var today = DateTime.Today;
            var allUsers = _dbConnection.Query<User>("SELECT * FROM UserTB").ToList();
            var coupons = _dbConnection.Query<Coupon>("SELECT * FROM CouponTB WHERE AutoAssign = 1 AND IsActive = 1").ToList();

            foreach (var coupon in coupons)
            {
                foreach (var user in allUsers)
                {
                    if (string.IsNullOrWhiteSpace(user.Id)) continue;

                    bool match = false;
                    switch (coupon.AutoAssignRule)
                    {
                        case AutoAssignRuleEnum.RegisterNow:
                        case AutoAssignRuleEnum.SpecificDate:
                            match = coupon.AutoDate?.Date == today;
                            break;
                        case AutoAssignRuleEnum.BeforeBirthday20:
                            if (user.Birthday.HasValue)
                            {
                                var thisYearBirthday = new DateTime(today.Year, user.Birthday.Value.Month, user.Birthday.Value.Day);
                                match = thisYearBirthday.AddDays(-20).Date == today;
                            }
                            break;
                    }

                    if (!match) continue;

                    var exists = _dbConnection.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM CouponDispatchRecordTB WHERE MemberId = @memberId AND CouponId = @couponId",
                        new { memberId = user.Id, couponId = coupon.CouponId });

                    if (exists == 0)
                    {
                        _dbConnection.Execute(@"
                        INSERT INTO CouponDispatchRecordTB 
                        (MemberId, CouponId, DispatchDate, IsDispatched, Note) 
                        VALUES (@memberId, @couponId, @now, 0, '排程派發')",
                            new { memberId = user.Id, couponId = coupon.CouponId, now = DateTime.UtcNow });
                    }
                }
            }
        }

        public void AssignRegisterCoupons(string memberId)
        {
            var today = DateTime.Today;
            var registerCoupons = _dbConnection.Query<Coupon>(
                "SELECT * FROM CouponTB WHERE AutoAssign = 1 AND AutoAssignRule = @rule AND IsActive = 1",
                new { rule = (int)AutoAssignRuleEnum.RegisterNow }).ToList();

            foreach (var coupon in registerCoupons)
            {
                var exists = _dbConnection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM CouponDispatchRecordTB WHERE MemberId = @memberId AND CouponId = @couponId",
                    new { memberId, couponId = coupon.CouponId });

                if (exists == 0)
                {
                    _dbConnection.Execute(@"
                    INSERT INTO CouponDispatchRecordTB 
                    (CouponId, MemberId, DispatchDate, IsDispatched, Note)
                    VALUES (@couponId, @memberId, @now, 0, '新用戶註冊自動派發')",
                        new { couponId = coupon.CouponId, memberId, now = today });
                }
            }
        }

        public bool MarkCouponAsUsed(int recordId)
        {
            var rows = _dbConnection.Execute(
                "UPDATE CouponDispatchRecordTB SET IsDispatched = 1 WHERE RecordId = @Id AND IsDispatched = 0",
                new { Id = recordId });
            return rows > 0;
        }

        public string GenerateCouponQRCode(int recordId)
        {
            return $"COUPONQR-{recordId}-{Guid.NewGuid():N}";
        }

        public CouponDispatchRecord? GetCouponDispatchRecordByQRCode(string qrCode)
        {
            var parts = qrCode.Split('-');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int recordId)) return null;
            return _dbConnection.QueryFirstOrDefault<CouponDispatchRecord>(
                "SELECT * FROM CouponDispatchRecordTB WHERE RecordId = @recordId AND IsDispatched = 0",
                new { recordId });
        }

        public CouponDispatchRecord? GetCouponDispatchRecord(int recordId)
        {
            return _dbConnection.QueryFirstOrDefault<CouponDispatchRecord>(
                "SELECT * FROM CouponDispatchRecordTB WHERE RecordId = @recordId",
                new { recordId });
        }

        public void AssignCouponToMember(string memberId, int couponId)
        {
            _dbConnection.Execute(@"
            INSERT INTO CouponDispatchRecordTB 
            (CouponId, MemberId, DispatchDate, IsDispatched, Note)
            VALUES (@couponId, @memberId, @now, 0, '後台派發')",
                new { couponId, memberId, now = DateTime.UtcNow });
        }

        public void ToggleCouponStatus(int couponId)
        {
            _dbConnection.Execute("UPDATE CouponTB SET IsActive = 1 - IsActive WHERE CouponId = @id", new { id = couponId });
        }

        public List<CouponDispatchRecord> SearchCouponRecords(string keyword)
        {
            string sql = @"
            SELECT r.* 
            FROM CouponDispatchRecordTB r
            WHERE r.IsDispatched = 0 AND (
                EXISTS (
                    SELECT 1 FROM UserTB u 
                    WHERE u.Id = r.MemberId AND (u.Id LIKE @kw OR u.Name LIKE @kw)
                ) OR
                EXISTS (
                    SELECT 1 FROM CouponTB c 
                    WHERE c.CouponId = r.CouponId AND (c.Title LIKE @kw OR c.Code LIKE @kw)
                )
            )
            ORDER BY r.DispatchDate DESC";

            return _dbConnection.Query<CouponDispatchRecord>(sql, new { kw = $"%{keyword}%" }).ToList();
        }

        public bool DeleteCouponRecord(int recordId)
        {
            var sql = "UPDATE CouponDispatchRecordTB SET IsDispatched = 1 WHERE RecordId = @recordId";
            return _dbConnection.Execute(sql, new { recordId }) > 0;
        }

        // 點數系統（MySQL）
        public int GetMemberPoints(string memberId)
        {
            return _dbConnection.ExecuteScalar<int>(
                "SELECT IFNULL(SUM(Points), 0) FROM PointLogTB WHERE MemberId = @memberId",
                new { memberId });
        }

        public void AddPoints(string memberId, int points, string action)
        {
            var sql = @"
            INSERT INTO PointLogTB (MemberId, Points, Action, CreateTime, Remark)
            VALUES (@memberId, @points, @action, @now, '')";
            _dbConnection.Execute(sql, new { memberId, points, action, now = DateTime.UtcNow });
        }

        public bool DeductPoints(string memberId, int points, string action)
        {
            if (GetMemberPoints(memberId) < points) return false;
            AddPoints(memberId, -points, action);
            return true;
        }

        public string GetMemberLevel(string memberId)
        {
            int points = GetMemberPoints(memberId);
            if (points >= 300) return "VIP";
            if (points >= 150) return "金級";
            if (points >= 50) return "銀級";
            return "銅級";
        }

        public List<PointLog> GetPointHistory(string memberId)
        {
            return _dbConnection.Query<PointLog>(
                "SELECT * FROM PointLogTB WHERE MemberId = @memberId ORDER BY CreateTime DESC",
                new { memberId }).ToList();
        }


        public string ApplyCouponToOrder(string qrCode, Order order)
        {
            if (string.IsNullOrWhiteSpace(qrCode) || order == null)
                return "❌ 資料錯誤：缺少條碼或訂單資訊";

            var parts = qrCode.Split('-');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int recordId))
                return "❌ 條碼格式錯誤";

            var dispatch = _dbConnection.QueryFirstOrDefault<CouponDispatchRecord>(
                "SELECT * FROM CouponDispatchRecordTB WHERE RecordId = @recordId", new { recordId });

            if (dispatch == null)
                return "❌ 查無此優惠券紀錄";

            if (dispatch.IsDispatched)
                return "❌ 此優惠券已使用過";

            var coupon = _dbConnection.QueryFirstOrDefault<Coupon>(
                "SELECT * FROM CouponTB WHERE CouponId = @id", new { id = dispatch.CouponId });

            if (coupon == null)
                return "❌ 查無優惠券主檔";

            if (!coupon.IsActive)
                return "❌ 此優惠券已停用";

            var now = DateTime.Now;
            if (now < coupon.ValidFrom || now > coupon.ValidTo)
                return "❌ 優惠券已過期或尚未生效";

            if (order.Price < (double)coupon.MinAmount)
                return $"❌ 訂單金額未達優惠券門檻（最低需滿 {coupon.MinAmount} 元）";

            // ✅ 若有限定商品類別
            if (!string.IsNullOrEmpty(coupon.CategoryLimit))
            {
                var orderProduct = _dbConnection.QueryFirstOrDefault<dynamic>(
                    "SELECT PGid FROM ProductTB WHERE ProductId = @id", new { id = order.ProductId });

                if (orderProduct == null || !coupon.CategoryLimit.Contains(orderProduct.PGid.ToString()))
                    return "❌ 優惠券不適用於該產品類別";
            }

            // ✅ 計算折扣
            double discount = 0;
            if (coupon.DiscountType == DiscountTypeEnum.Percentage)
            {
                discount = Math.Round(order.Price * ((double)coupon.DiscountAmount / 100), 0, MidpointRounding.AwayFromZero);
            }
            else if (coupon.FixedDiscountAmount.HasValue)
            {
                discount = (double)coupon.FixedDiscountAmount.Value;
            }

            discount = Math.Min(discount, order.Price); // 折扣不得超過金額

            // ✅ 更新 OrderTB 與優惠券紀錄
            _dbConnection.Execute(@"
        UPDATE OrderTB 
        SET DiscountAmount = @discount, UsedCouponId = @recordId 
        WHERE OrderId = @orderId",
                new { discount, recordId = dispatch.RecordId, orderId = order.OrderId });

            _dbConnection.Execute(@"
        UPDATE CouponDispatchRecordTB 
        SET IsDispatched = 1, OrderId = @orderId 
        WHERE RecordId = @recordId",
                new { orderId = order.OrderId, recordId = dispatch.RecordId });

            return $"✅ 成功套用優惠券，折扣金額 {discount:N0} 元";
        }


        public string ApplyCouponByRecordId(int recordId, Order order)
        {
            if (order == null || order.OrderId <= 0)
                return "❌ 訂單資料錯誤";

            var dispatch = GetCouponDispatchRecord(recordId);
            if (dispatch == null)
                return "❌ 找不到優惠券派發記錄";

            if (dispatch.IsDispatched)
                return "❌ 此優惠券已使用過";

            var coupon = _dbConnection.QueryFirstOrDefault<Coupon>(
                "SELECT * FROM CouponTB WHERE CouponId = @id", new { id = dispatch.CouponId });

            if (coupon == null || !coupon.IsActive)
                return "❌ 優惠券無效或已停用";

            var now = DateTime.Now;
            if (now < coupon.ValidFrom || now > coupon.ValidTo)
                return "❌ 優惠券已過期或尚未生效";

            if (order.Price < (double)coupon.MinAmount)
                return $"❌ 訂單金額未達優惠門檻（最低需滿 {coupon.MinAmount} 元）";

            // ✅ 若有限制商品類別
            if (!string.IsNullOrEmpty(coupon.CategoryLimit))
            {
                var orderProductPGid = _dbConnection.QueryFirstOrDefault<int?>(
                    "SELECT PGid FROM ProductTB WHERE ProductId = @id", new { id = order.ProductId });

                if (!orderProductPGid.HasValue || !coupon.CategoryLimit.Contains(orderProductPGid.Value.ToString()))
                    return "❌ 優惠券不適用於此產品";
            }

            // ✅ 計算折扣
            double discount = 0;
            if (coupon.DiscountType == DiscountTypeEnum.Percentage)
            {
                discount = Math.Round(order.Price * ((double)coupon.DiscountAmount / 100), 0, MidpointRounding.AwayFromZero);
            }
            else if (coupon.FixedDiscountAmount.HasValue)
            {
                discount = (double)coupon.FixedDiscountAmount.Value;
            }

            discount = Math.Min(discount, order.Price);

            // ✅ 更新資料
            _dbConnection.Execute(@"
        UPDATE OrderTB 
        SET DiscountAmount = @discount, UsedCouponId = @couponId 
        WHERE OrderId = @orderId",
                new { discount, couponId = dispatch.CouponId, orderId = order.OrderId });

            _dbConnection.Execute(@"
        UPDATE CouponDispatchRecordTB 
        SET IsDispatched = 1, OrderId = @orderId 
        WHERE RecordId = @recordId",
                new { orderId = order.OrderId, recordId });

            return $"✅ 優惠券已套用，折抵金額：{discount:N0} 元";
        }

    }



}

using Dapper;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Web0524.Models
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
                // 沒有勾選自動派發，就不需要寫入 AutoAssignRule 與 AutoDate
                coupon.AutoAssignRule = AutoAssignRuleEnum.None;
                coupon.AutoDate = null;
            }
            else if (coupon.AutoAssignRule != AutoAssignRuleEnum.SpecificDate)
            {
                // 若不是指定日期，也不應寫入 AutoDate
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
            var sql = @"SELECT * FROM CouponDispatchRecordTB 
                    WHERE MemberId = @memberId AND IsDispatched = 0";
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
                    switch ((AutoAssignRuleEnum)coupon.AutoAssignRule)
                    {
                        case AutoAssignRuleEnum.RegisterNow:
                        case AutoAssignRuleEnum.SpecificDate:
                            match = coupon.AutoDate.Value.Date == today;
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
                        _dbConnection.Execute(@"INSERT INTO CouponDispatchRecordTB 
(MemberId, CouponId, DispatchDate, IsDispatched, Note) 
VALUES (@memberId, @couponId, GETDATE(), 0, '排程派發')",
                            new { memberId = user.Id, couponId = coupon.CouponId });
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
                    _dbConnection.Execute(@"INSERT INTO CouponDispatchRecordTB 
(CouponId, MemberId, DispatchDate, IsDispatched, Note)
VALUES (@couponId, @memberId, @dispatchDate, 0, @note)",
                        new
                        {
                            couponId = coupon.CouponId,
                            memberId,
                            dispatchDate = today,
                            note = "新用戶註冊自動派發"
                        });
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
            var exists = _dbConnection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM CouponDispatchRecordTB WHERE MemberId = @memberId AND CouponId = @couponId",
                new { memberId, couponId });

            if (exists == 0)
            {
                _dbConnection.Execute(@"INSERT INTO CouponDispatchRecordTB (CouponId, MemberId, DispatchDate, IsDispatched, Note)
                                VALUES (@couponId, @memberId, GETDATE(), 0, '後台派發')",
                    new { couponId, memberId });
            }
        }

        public void ToggleCouponStatus(int couponId)
        {
            _dbConnection.Execute("UPDATE CouponTB SET IsActive = 1 - IsActive WHERE CouponId = @id", new { id = couponId });
        }
        public string ApplyCouponToOrder(string qrCode, Order order)
        {
            if (string.IsNullOrWhiteSpace(qrCode) || order == null)
                return "❌ 資料錯誤：缺少條碼或訂單資訊";

            // 解析條碼格式：預期格式 COUPONQR-123456-xxxxxxxx
            var parts = qrCode.Split('-');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int recordId))
                return "❌ 條碼格式錯誤";

            // 取得該派發紀錄
            var dispatch = _dbConnection.QueryFirstOrDefault<CouponDispatchRecord>(
                "SELECT * FROM CouponDispatchRecordTB WHERE RecordId = @recordId",
                new { recordId });

            if (dispatch == null)
                return "❌ 查無此優惠券紀錄";

            if (dispatch.IsDispatched)
                return "❌ 此優惠券已使用過";

            // 取得對應的優惠券主檔
            var coupon = _dbConnection.QueryFirstOrDefault<Coupon>(
                "SELECT * FROM CouponTB WHERE CouponId = @id",
                new { id = dispatch.CouponId });

            if (coupon == null)
                return "❌ 查無優惠券主檔";

            if (!coupon.IsActive)
                return "❌ 此優惠券已停用";

            var now = DateTime.Now;
            if (now < coupon.ValidFrom || now > coupon.ValidTo)
                return "❌ 優惠券已過期或尚未生效";

            if (order.Price < (double)coupon.MinAmount)
                return $"❌ 訂單金額未達優惠券門檻（最低需滿 {coupon.MinAmount} 元）";

            // ✅ 若有設定商品類別限制，需檢查訂單商品是否符合
            if (!string.IsNullOrEmpty(coupon.CategoryLimit))
            {
                var orderProduct = _dbConnection.QueryFirstOrDefault<dynamic>(
                    "SELECT PGid FROM ProductTB WHERE ProductId = @id",
                    new { id = order.ProductId });

                if (orderProduct == null || !coupon.CategoryLimit.Contains(orderProduct.Category))
                    return "❌ 優惠券不適用於該產品類別";
            }

            // ✅ 計算折扣金額
            double discount = 0;
            if (coupon.DiscountType == DiscountTypeEnum.Percentage)
            {
                discount = Math.Round(order.Price * ((double)coupon.DiscountAmount / 100), 0, MidpointRounding.AwayFromZero);
            }
            else if (coupon.FixedDiscountAmount.HasValue)
            {
                discount = (double)coupon.FixedDiscountAmount.Value;
            }

            // ✅ 限制不可折抵超過訂單金額
            discount = Math.Min(discount, order.Price);

            // ✅ 更新訂單折扣金額與綁定優惠券
            _dbConnection.Execute(@"
        UPDATE OrderTB 
        SET DiscountAmount = @discount, UsedCouponId = @recordId
        WHERE OrderId = @orderId",
                new
                {
                    discount,
                    recordId = dispatch.RecordId,
                    orderId = order.OrderId
                });

            // ✅ 將優惠券標記為已使用，綁定訂單 ID
            _dbConnection.Execute(@"
        UPDATE CouponDispatchRecordTB 
        SET IsDispatched = 1, OrderId = @orderId
        WHERE RecordId = @recordId",
                new
                {
                    orderId = order.OrderId,
                    recordId = dispatch.RecordId
                });

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

            // 商品類別限制（若有）
            if (!string.IsNullOrEmpty(coupon.CategoryLimit))
            {
                var orderProduct = _dbConnection.QueryFirstOrDefault<dynamic>(
                    "SELECT PGid FROM ProductTB WHERE ProductId = @id", new { id = order.ProductId });

                if (orderProduct == null || !coupon.CategoryLimit.Contains(orderProduct.Category))
                    return "❌ 優惠券不適用於此產品";
            }

            // 計算折扣金額
            double discount = 0;
            if (coupon.DiscountType == DiscountTypeEnum.Percentage)
            {
                discount = Math.Round(order.Price * ((double)coupon.DiscountAmount / 100), 0, MidpointRounding.AwayFromZero);
            }
            else if (coupon.FixedDiscountAmount.HasValue)
            {
                discount = (double)coupon.FixedDiscountAmount.Value;
            }

            discount = Math.Min(discount, order.Price); // 不可超過總價

            // 更新 OrderTB
            _dbConnection.Execute(@"
        UPDATE OrderTB 
        SET DiscountAmount = @discount, UsedCouponId = @recordId 
        WHERE OrderId = @orderId",
                new { discount, recordId, orderId = order.OrderId });

            // 更新 CouponDispatchRecordTB
            _dbConnection.Execute(@"
        UPDATE CouponDispatchRecordTB 
        SET IsDispatched = 1, OrderId = @orderId 
        WHERE RecordId = @recordId",
                new { orderId = order.OrderId, recordId });

            return $"✅ 優惠券已套用，折抵金額：{discount:N0} 元";
        }







        // ✅ 查詢會員總點數
        public int GetMemberPoints(string memberId)
        {
            return _dbConnection.ExecuteScalar<int>(
                "SELECT ISNULL(SUM(Points), 0) FROM PointLogTB WHERE MemberId = @memberId",
                new { memberId });
        }

        // ✅ 增加會員點數
        public void AddPoints(string memberId, int points, string action)
        {
            var sql = @"
        INSERT INTO PointLogTB (MemberId, Points, Action, CreateTime, Remark)
        VALUES (@memberId, @points, @action, GETDATE(), '')";
            _dbConnection.Execute(sql, new { memberId, points, action });
        }

        // ✅ 扣除會員點數（前提是點數足夠）
        public bool DeductPoints(string memberId, int points, string action)
        {
            if (GetMemberPoints(memberId) < points) return false;
            AddPoints(memberId, -points, action);
            return true;
        }

        // ✅ 取得會員等級（根據點數累積）
        public string GetMemberLevel(string memberId)
        {
            int points = GetMemberPoints(memberId);
            if (points >= 300) return "VIP";
            if (points >= 150) return "金級";
            if (points >= 50) return "銀級";
            return "銅級";
        }

        // ✅ 查詢會員點數紀錄
        public List<PointLog> GetPointHistory(string memberId)
        {
            return _dbConnection.Query<PointLog>(
                "SELECT * FROM PointLogTB WHERE MemberId = @memberId ORDER BY CreateTime DESC",
                new { memberId }).ToList();
        }

    }


}

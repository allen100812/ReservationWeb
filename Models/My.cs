namespace Web0524.Models
{
    public class My
    {

        public const string Fb_Url = "";
        public const string Ig_Url = "";
        public const string Yt_Url = "https://futurelab.tw/pages/page-12";
        public const string Tk_Url = "";
        public const string Line_Url = "";

        public const string Name_short = "SuperiorSeed";
        public const string Name = "SuperiorSeed時尚美學";
        public const string Phone = "0936-064980";
        public const string Email = "allen100812@gmail.com";
        public const string Line = "@123123123";
        public const string WebURL = "http://localhost:5155/";
        public const string LineBotURL = "https://line.me/R/ti/p/%40771xwptp";






        public const bool CreateOrderSandLineMsgSw = true;
        public const bool CancelSandLineMsgSw = true;

        public const string Msg_BindOk = "親愛的用戶，感謝您加入工作室會員！🎉 您已成功完成帳號綁定。\r\n\r\n現在，您可以輕鬆地在線上預約我們提供的服務，並且我們將為您提供即時通知📢。\r\n\r\n我們的通知服務包含以下內容：\r\n\r\n1. 提前通知您即將到來的預約服務，確保您不會錯過重要時間⏰。\r\n\r\n2. 商家接受您的預約或希望與您討論改期時，立即通知您📝📩。\r\n\r\n3. 您還會收到商家的最新資訊、活動和優惠📰💰。\r\n\r\n如果您有任何疑問或需要協助，請隨時與我們聯繫🌟🙌。期待為您服務！";
        public const string Msg_OrderCancel_client = "尊敬的顧客，很抱歉，您的預約已被取消。\r\n\r\n訂單編號：{Sid}\r\n\r\n🌟 預約服務：{Pname}\r\n📅 預約時間：{Date}\r\n📍 預約地址：{Address}\r\n\r\n對於造成您的不便，我們深感抱歉。如果您需要重新預約或預約其他服務，請透過以下網址進行線上預約：\r\n\r\n我要預約：{Url}\r\n\r\n我們非常期待為您提供優質的服務。\r\n\r\n如果您有任何疑問或需要協助，請隨時與我們聯繫。\r\n\r\n謝謝您的理解與支持。再次深表歉意。";
        public const string Msg_OrderSend_client = "✨ 感謝您的預約，我們已經收到您的預約單並通知商家！✨\r\n\r\n訂單編號：{Sid}\r\n\r\n🌟 預約服務：{Pname}\r\n📅 預約時間：{Date}\r\n📍 預約地址：{Address}\r\n\r\n一旦商家確認接受此預約單，我們將另行通知您。您可以透過以下連結查看預約單是否已被接受：\r\n\r\n查看預約單：{Url}\r\n\r\n如果您有任何疑問或需要協助，請隨時與我們聯繫。";






        public const string Msg_OrderAccept_client = "✨ 您的訂單已被商家接受！✨\r\n\r\n訂單編號：{Sid}\r\n\r\n🌟預約服務：{Pname}\r\n📅預約時間：{Date}\r\n📍預約地址：{Address} \r\n\r\n請注意，預約為您保留 15 分鐘⏳，敬請準時抵達。\r\n\r\n若您需要更改預約，請透過以下網址進行預約單的取消或修改：\r\n\r\n修改預約：{Url}/:\r\n\r\n如果您有任何疑問或需要協助，請隨時與我們聯繫。";
       
        public const string Msg_OrderCome_client = "📢系統通知（即將到來）：\r\n提醒您 三日內，您有預約服務\r\n\r\n{OrderData}\r\n查看預約單：{Url}";
        public const string Msg_OrderToday_client = "📢系統通知（今日有約）：\r\n提醒您\r\n今日 {Date} 您有預約服務\r\n\r\n{OrderData}\r\n查看預約單：{Url}";

        public const string Msg_OrderNew_op = "📢系統通知（新的預約）：\r\n新的客戶預約等待接受。\r\n\r\n訂單編號：{Sid}\r\n預約店鋪：{Place}\r\n預約項目：{Pname}\r\n預約時間：{Date}\r\n預約會員：{Uname}\r\n會員Line暱稱：{LineId}\r\n\r\n接受/拒絕預約單：{Url}";
        public const string Msg_OrderCancel_op = "📢系統通知（預約取消）：\r\n老闆您好，有客戶取消了預約。\r\n\r\n訂單編號：{Sid}\r\n預約項目：{Pname}\r\n預約地址：{Address}\r\n預約時間：{Date}\r\n預約會員：{Uname}\r\n會員Line暱稱：{LineId}\r\n\r\n查看預約單：{Url}";
        public const string Msg_OrderWeekReport = "📢系統通知（預約下週報）：\r\n提醒您，下週 {DateStart}-{DateEnd} 共有\r\n\r\n {OrderCount} 筆預約單\r\n\r\n{Event0_Num} 筆待接受的預約，須請盡速確認！！\r\n\r\n{Event1_Num} 筆已接受的預約：\r\n {Event1List} \r\n\r\n查看預約單：{Url}";
        public const string Msg_OrderCome_op = "📢系統通知（即將到來）：\r\n提醒您 三日內，共有 {OrderCount} 筆預約單\r\n\r\n{Event0_Num} 筆待接受的預約，須請盡速確認！！\r\n\r\n{Event1_Num} 筆已接受的預約：\r\n {Event1List}　\r\n\r\n查看預約單：{Url}";
        public const string Msg_OrderToday_op = "📢系統通知（今日有約）：\r\n提醒您\r\n今日 {Date} 共有 {Event1_Num} 筆預約：\r\n\r\n {Event1List}\r\n\r\n查看預約單：{Url}";
        

        public const int Max_Order_Oneday = 5;
        public const int Max_Reg_Oneday = 5;

        public const int CancelLimitHours = 2; //取消訂單時限,設0則不可取消訂單

        public List<LocationInfo> Locations { get; set; } = new List<LocationInfo>
        {
            new LocationInfo
            {
                Name = "總店 - 竹北店",
                Address = "新竹縣竹北市自強五路327號",
                Phone = "03-6681222",
                MapUrl = "https://www.google.com/maps?q=新竹縣竹北市自強五路37號&output=embed"
            },
            new LocationInfo
            {
                Name = "分店 - 台北店",
                Address = "台北市信義區松高路",
                Phone = "02-12345678",
                MapUrl = "https://www.google.com/maps?q=台北市信義區松高路&output=embed"
            },
            new LocationInfo
            {
                Name = "分店 - 台中店",
                Address = "台中市西區公益路",
                Phone = "04-87654321",
                MapUrl = "https://www.google.com/maps?q=台中市西區公益路&output=embed"
            }
        };
        public AboutContent AboutPage { get; set; } = new AboutContent();

    }
    public class LocationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string MapUrl { get; set; } = string.Empty;
    }
    public class AboutContent
    {
        public string PageTitle { get; set; } = "關於我們 - SuperiorSeed";
        public string HeroTitle { get; set; } = "時尚尖端 自然美結合";

        public string Section1_Title { get; set; } = "自然之美，專業呵護。";
        public string Section1_Paragraph1 { get; set; } = "SuperiorSeed 是一家專注於自然美的美容業公司。我們致力於提供最高品質的美容服務，讓每位顧客都能散發自己的獨特之處。";
        public string Section1_Paragraph2 { get; set; } = "我們以專業手法和最新技術為您提供獨特的美容體驗，並幫助您實現美麗目標。";

        public string Section2_Title { get; set; } = "探索美的秘密，SuperiorSeed 將引導您。";
        public string Section2_Paragraph1 { get; set; } = "我們的使命是讓每位顧客都感受到自然之美，相信每個人都擁有獨特魅力，並協助展現這份自信。";
        public string Section2_Paragraph2 { get; set; } = "我們堅持使用最優質產品與技術，致力於提供一流服務體驗。";

        public string Section3_Title { get; set; } = "自然之道，美的旅程。";
        public List<string> Section3_Items { get; set; } = new()
    {
        "專業美容治療",
        "最新美容技術",
        "個性化美容方案",
        "友善環境與專業團隊"
    };
    }
}

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
    public static class MyMessageTemplates
    {
        public const string Msg_OrderCreated_Client =
            "🎉 您好，預約已成功！\r\n\r\n📌 訂單編號：{Sid}\r\n🌟 服務項目：{Pname}\r\n📅 預約時間：{Date}\r\n\r\n我們已收到您的預約並通知商家確認，請稍待片刻 ⏳\r\n如有異動會再通知您喔！😊";

        public const string Msg_OrderCancel_Client =
            "🙇‍♀️ 很遺憾，您已取消此次預約。\r\n\r\n📌 訂單編號：{Sid}\r\n🌟 服務項目：{Pname}\r\n📅 原預約時間：{Date}\r\n\r\n期待下次再為您服務 💖";

        public const string Msg_OrderCancel_Store =
            "⚠️ 很抱歉，您的預約已由商家取消。\r\n\r\n📌 訂單編號：{Sid}\r\n🌟 服務項目：{Pname}\r\n📅 原預約時間：{Date}\r\n\r\n若有任何疑問歡迎聯絡我們，我們將盡快協助您 🙏";

        public const string Msg_OrderDone =
            "✨ 您的預約已順利完成，感謝您的蒞臨！\r\n\r\n📌 訂單編號：{Sid}\r\n🌟 服務項目：{Pname}\r\n📅 預約時間：{Date}\r\n\r\n若您滿意這次服務，歡迎留下評價與回饋 💬";

        public const string Msg_OrderRemind_3days =
            "⏰ 貼心提醒：您有即將到來的預約唷！\r\n\r\n📌 訂單編號：{Sid}\r\n🌟 服務項目：{Pname}\r\n📅 預約時間：{Date}\r\n\r\n我們期待見到您 🥰";

        public const string Msg_OrderRemind_Today =
            "📢 今日預約提醒：千萬別忘記唷！\r\n\r\n📌 訂單編號：{Sid}\r\n🌟 服務項目：{Pname}\r\n📅 預約時間：{Date}\r\n\r\n請準時抵達，我們已準備好迎接您 🌈";

        // 可重用方法
        public static string Format(string template, string sid, string pname, string date)
        {
            return template.Replace("{Sid}", sid)
                           .Replace("{Pname}", pname)
                           .Replace("{Date}", date);
        }

        // 個別方法（若你偏好明確呼叫）
        public static string FormatOrderCreated(string sid, string pname, string date) =>
            Format(Msg_OrderCreated_Client, sid, pname, date);

        public static string FormatOrderCancelByClient(string sid, string pname, string date) =>
            Format(Msg_OrderCancel_Client, sid, pname, date);

        public static string FormatOrderCancelByStore(string sid, string pname, string date) =>
            Format(Msg_OrderCancel_Store, sid, pname, date);

        public static string FormatOrderDone(string sid, string pname, string date) =>
            Format(Msg_OrderDone, sid, pname, date);

        public static string FormatOrderRemind3Days(string sid, string pname, string date) =>
            Format(Msg_OrderRemind_3days, sid, pname, date);

        public static string FormatOrderRemindToday(string sid, string pname, string date) =>
            Format(Msg_OrderRemind_Today, sid, pname, date);
    }

}

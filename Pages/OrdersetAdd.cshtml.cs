using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using Web0524.Models;


namespace Web0524.Pages
{
    [Authorize]
    public class OrdersetAddModel : PageModel
    {
        private readonly IOrdersetService _OrdersetService;
        private readonly IProductService _ProductService;
        private readonly LineMessageContext _lineMessageContext;
        private readonly IUserService _userService;
        private readonly IPlaceService _placeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMyService _myService;

        DateTime localTime = DateTime.Now;
        DateTime taipeiTime;
        // ���o�x�_�ɰϪ�TimeZoneInfo
        TimeZoneInfo taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        private static readonly HashSet<string> SubmittedTokens = new HashSet<string>();

        public OrdersetAddModel(IOrdersetService OrdersetService, IProductService productService, LineMessageContext lineMessageContext, IUserService userService, IPlaceService placeService, IHttpContextAccessor httpContextAccessor, IMyService myService)
        {
            _OrdersetService = OrdersetService;
            _ProductService = productService;
            #region Contracts

            if (lineMessageContext == null) throw new ArgumentException($"{nameof(lineMessageContext)}=null");

            #endregion

            // Default
            _lineMessageContext = lineMessageContext;
            _userService = userService;
            _placeService = placeService;
            _httpContextAccessor = httpContextAccessor;
            _myService = myService;
        }
        [BindProperty]
        public Orderset Orderset { get; set; }
        public Product Product { get; set; }
        public List<Place> Place { get; set; }
        public Models.User user { get; set; }
        public Models.User User_me { get; set; }

        public List<Models.User> users { get; set; }
        public List<Models.User> admins { get; set; }

        public My basedata { get; set; }

        public IActionResult OnGet(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //�޲z���Ҧ�
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null)
            {
                return NotFound();
            }else if(int.Parse(User_me.UserType) <= 2)
            {
                users = _userService.GetUserTB().ToList();
            }
            Product = _ProductService.GetProductById(id);
            Place = _placeService.GetPlaceByPid(Product.Pid).ToList();
            Orderset =new Orderset();
            Orderset.Pid = id;
            Orderset.Photo = Product.Photo;
            Orderset.Pname = Product.Name;
            Orderset.Price = Product.Price;
            Orderset.Unit=Product.Unit;
            Orderset.Uname = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            // �N���a�ɶ��ഫ���x�_�ɰϪ��ɶ�
            taipeiTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, taipeiTimeZone);
            ViewData["taipeiTime"] = taipeiTime;
            return Page();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OnPost(Orderset orderset)
        {

            var token = HttpContext.Request.Form["__RequestVerificationToken"];
            LineMessageSender lineMessageSender = new LineMessageSender();
            if (!string.IsNullOrEmpty(token) && !SubmittedTokens.Contains(token))
            {
                SubmittedTokens.Add(token);

                // ������� Orderset ���󪺨�L�ݩʡ]���]�A Photo �ݩʡ^
                //�޲z���Ҧ�
                orderset = new Orderset
                {
                    Pid = Orderset.Pid,
                    Date = Orderset.Date,
                    Time = Orderset.Time,
                    Uid = Orderset.Uid,
                    Placeid = Orderset.Placeid
                };
                Product = _ProductService.GetProductById(Orderset.Pid);
                User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
                taipeiTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, taipeiTimeZone);
                basedata = _myService.GetBaseData();
                string WebUrl = "";
                if(basedata == null)
                {
                    WebUrl = "";
                }
                else
                {
                    WebUrl = basedata.WebURL;
                }

                if (User_me == null)//�ϥΪ̵n�J�P�_
                {
                    return NotFound();
                }
                else if (int.Parse(User_me.UserType) <= 2)//�ϥΪ��v���P�_
                {
                    //�޲z������
                    if (Request.Form["OrderUser"] == "")
                    {
                        TempData["OrdersetError"] = true;
                        return RedirectToPage("ProductList");
                    }
                    else
                    {
                        orderset.Uid = Request.Form["OrderUser"];
                        user = _userService.GetUserById(Request.Form["OrderUser"]);
                    }
                }
                else
                {
                    //�@�봣��
                    orderset.Uid = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
                    user = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
                    //Ip�w�����ƶW�L�P�_
                    var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    var ip_regnum = _userService.Iplog_CheckByIp(ipAddress, 2);
                    if (ip_regnum > My.Max_Order_Oneday)
                    {
                        TempData["OrdersetError_ip"] = true;
                        return RedirectToPage("/Index");
                    }
                    _userService.Iplog_save(ipAddress,user.Id, 2);
                }


                if (user == null)//�w���H�P�_
                {
                    TempData["OrdersetError_user"] = true;
                    return RedirectToPage("ProductList");
                }
                if (_OrdersetService.GetOrdersetByUidInMinute(user.Id, taipeiTime))//�@���������ƤU��P�_
                {
                    TempData["OrdersetError_minute"] = true;
                    return RedirectToPage("ProductList");
                }
                if (_OrdersetService.GetOrdersetByUidCountToday(user.Id, taipeiTime) > My.Max_Order_Oneday)//�@�餺���ƤU��P�_
                {
                    TempData["OrdersetError_maxorder"] = true;
                    return RedirectToPage("ProductList");
                }


                List<Place> Places = _placeService.GetPlaceTB().ToList();
                admins = _userService.GetUserByType(2).ToList();
                var ordersetValidationContext = new ValidationContext(orderset, serviceProvider: null, items: null);
                var ordersetValidationResults = new List<ValidationResult>();
                bool ordersetIsValid = Validator.TryValidateObject(orderset, ordersetValidationContext, ordersetValidationResults, validateAllProperties: true);
                // �i���L�ݩʪ�����
                if (!ordersetIsValid)
                {
                    // �B�z��L�ݩʪ����ҿ��~�T��
                    foreach (var validationResult in ordersetValidationResults)
                    {
                        // �B�z���~�T���A�Ҧp�O����x�γ]�wĵ�i�T��
                        // ...
                        TempData["OrdersetError"] = true;

                    }

                    // �N���~�T���ǻ�������
                    return RedirectToPage("ProductList");
                }

                taipeiTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, taipeiTimeZone);
                DateTime minDate = taipeiTime.AddDays(0).Date;
                DateTime maxDate = taipeiTime.AddMonths(2).Date;
                DateTime OTime = (DateTime)(orderset.Date + orderset.Time);
                if (OTime <= taipeiTime || orderset.Date > maxDate)
                {
                    ModelState.AddModelError("Orderset.Date", "�п�ܨ�Ӥ뤺�����");
                    TempData["DateError"] = true;
                    return RedirectToPage("ProductList");
                }

                // �ˬd Time �O�_�����Įɶ�
                if (!TimeSpan.TryParse(orderset.Time.ToString(), out TimeSpan timeSpan))
                {
                    ModelState.AddModelError("Orderset.Time", "�п�ܦ��Ī��ɶ�");
                    TempData["TimeError"] = true;
                    return RedirectToPage("ProductList");
                }


                // Orderset �������ҳq�L�A��������B�z
                int New_Sid = _OrdersetService.AddOrderset(orderset);
                if (New_Sid != 0)
                {
                    //�q��
                    //�q���|���w�����\
                    string msg;
                    var Place = Places.FirstOrDefault(place => place.Placeid == orderset.Placeid);
                    string OrderDate = orderset.Date != null ? orderset.Date.Value.ToString("yyyy-MM-dd") : "N/A";
                    string OrderTime = orderset.Time.HasValue ? orderset.Time.Value.ToString(@"hh\:mm") : "N/A";


                    if (user != null && user.LineUserId != null)
                    {
                        msg = My.Msg_OrderSend_client;
                        msg = msg.Replace("{Sid}", (New_Sid == null) ? "�L�k���o" : "S" + New_Sid.ToString().PadLeft(6, '0'));
                        msg = msg.Replace("{Pname}", (Product.Name == null) ? "�L�k���o" : Product.Name.ToString());
                        msg = msg.Replace("{Place}", (Place.Placetitle == null) ? "�L�k���o" : Place.Placetitle);
                        msg = msg.Replace("{Date}", (OrderDate == null) ? "�L�k���o" : OrderDate + " " + OrderTime);
                        msg = msg.Replace("{Address}", (Place.Placeaddress == null) ? "�L�k���o" : Place.Placetitle + "-" + Place.Placeaddress);
                        msg = msg.Replace("{Url}", WebUrl + "Order");
                        //LineUserAsync(msg, user.LineUserId);
                    }
                    //�q���޲z�����s���w��
                    if (admins != null)
                    {
                        foreach (Models.User admin in admins)
                        {
                            if (admin.LineUserId != null)
                            {
                                msg = My.Msg_OrderNew_op;
                                msg = msg.Replace("{Sid}", (New_Sid == null) ? "�L�k���o" : "S" + New_Sid.ToString().PadLeft(6, '0'));
                                msg = msg.Replace("{Place}", (Place.Placetitle == null) ? "�L�k���o" : Place.Placetitle);
                                msg = msg.Replace("{Pname}", (Product.Name == null) ? "�L�k���o" : Product.Name.ToString());
                                msg = msg.Replace("{Date}", (OrderDate == null) ? "�L�k���o" : OrderDate + " " + OrderTime);
                                //if (user.LineUserId == null || user.LineUserId == "")
                                //{
                                //    msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o-�Ȥ᥼�j�wLine�۰ʳq���A�Цۦ�q���P�����Ȥ�" : user.Line + ":�Ȥ᥼�j�wLine�q���A�Цۦ�q���P�����Ȥ�");
                                //}
                                //else
                                //{
                                //    msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o-�Ȥ�w�j�wLine�۰ʳq��" : user.Line + ":�Ȥ�w�j�wLine�۰ʳq��");
                                //}
                                msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o" : user.Line);
                                msg = msg.Replace("{Uname}", (user.Name == null) ? "�L�k���o" : user.Name);
                                msg = msg.Replace("{Url}", WebUrl + "OrdersetCalendar_m");
                                lineMessageSender.SendMessage_Notify(admin.LineUserId, msg);
                            }

                        }
                    }
                    TempData["add_success"] = true;
                    return RedirectToPage("Order");
                }
                else
                {
                    TempData["OrdersetError"] = true;
                    return RedirectToPage("ProductList");
                }
            }
            return NotFound();
        }
    }
}

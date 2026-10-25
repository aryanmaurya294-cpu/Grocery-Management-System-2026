using myproject.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Data;
namespace myproject.Controllers
{
    public class GeneralController : Controller
    {
        // GET: General --- Landing Page / Main Page
        DBManager db = new DBManager();
        public ActionResult Index()
        {
            //top 10 category
            DataTable cat = db.ExecuteSelect("select top 12 * from tbl_category");
            ViewBag.cat = cat;
            //select top 6 products
            DataTable p = db.ExecuteSelect($"select top 8 * from tbl_product left join tbl_cart on id=productid and email='{Session["emailid"]}' order by id desc");
            ViewBag.p = p;


            return View();
        }
        public ActionResult Pdetails()
        {
            return View();
        }
        public ActionResult Products(int? catid)
        {
            //select all category
            DataTable cat = db.ExecuteSelect("select * from tbl_category");
            ViewBag.cat = cat;
            //select all products
            DataTable p = db.ExecuteSelect($"select * from tbl_product left join tbl_cart on id=productid and email='{Session["emailid"]}' order by id desc");
            if (catid.HasValue)
            {
                p = db.ExecuteSelect($"select * from tbl_product left join tbl_cart on id=productid and email='{Session["emailid"]}' where catid={catid} order by id desc");
            }

            ViewBag.p = p;
            return View();
        }
        public ActionResult Cart(int? cartid)
        {
            //delete item from cart
            if (cartid.HasValue)
            {
                db.ExecuteSQL($"delete from tbl_cart where cartid={cartid}");
                //select total items of cart
                DataTable dt1 = db.ExecuteSelect($"select sum(qty) from tbl_cart where email='{Session["emailid"]}'");
                Session["cartitem"] = dt1.Rows[0][0];
            }
            if (Session["emailid"] == null)
            {
                TempData["msg"] = "Login first to access this page";
                return RedirectToAction("login");

            }
            string email = Session["emailid"].ToString();
            DataTable dt = db.ExecuteSelect($"select * from tbl_cart where email='{email}'");
            ViewBag.dt = dt;
            //select all address of user
            DataTable add = db.ExecuteSelect($"select * from tbl_address where email='{email}'");
            ViewBag.add = add;
       
            return View();
        }
        [HttpPost]
        public ActionResult cart(int? address,int? total)
        {
            //add data tbl_order ,tbl_orderitem,delete from tbl_cart
            Random r = new Random();
            int orderid = r.Next(100000, 999999);
            string email = Session["emailid"].ToString();
            if (address.HasValue && total.HasValue)
            {
                db.ExecuteSQL($"insert into tbl_order values({orderid},'{email}',{address},{total},'cash on delivery','success','{DateTime.Now.ToString("yyy-MM-dd hh:mm:ss ")}',null,null,null,'ordered','{DateTime.Now.AddMinutes(30).ToString("yyy-MM-dd hh:mm:ss ")}')");
                db.ExecuteSQL($"insert into tbl_orderitem select {orderid},* from tbl_cart where email='{email}'");
                db.ExecuteSQL($"delete from tbl_cart where email='{email}'");
                Session["cartitem"] = 0;

                return Content("<script>alert('Order Placed Successfully . You can check status from my orders section');location.href='/general/myorders'</script>");
            }
            else
            {
                return Content("<script>alert('Address Id or Total bill not found . Please Try Again.');location.href='/general/cart'</script>");
            }
        }
        public ActionResult signup()
        {
            return View();
        }
        [HttpPost]
        public ActionResult signup(string name,string email,string mobile,string gender)
        {
            int res = db.ExecuteSQL($"insert into tbl_user values ('{name}', {mobile},'{email}','{gender}','{DateTime.Now.ToString("yyy-MM-dd hh:mm:ss")}',1)");
            if (res > 0)
            {
                
                TempData["msg"] = "Product Saved Successfully . Now you can start ordering";
            }
            else
            {
                TempData["msg"] = "Data not saved due to Some Error.";

            }
            return RedirectToAction("signup");
        }
        

        public ActionResult login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult login(string email)
        {
            //check this email in tbl_user
            DataTable dt = db.ExecuteSelect($"select * from tbl_user where email='{email}'");
            if (dt.Rows.Count > 0)
            {
                Session["name"] = dt.Rows[0][0];
                Session["email"] = email;
                //generte OTP
                Random r = new Random();
                int otp = r.Next(100000, 999999);
                Session["otp"] = otp;
                //compose mail to send
                string mail = $"Your OTP to login on QuickBasket is {otp}";
                //send mail
                SendEmail(email, "Login OTP from QuickBasket", mail);
                return RedirectToAction("checkotp");
            }
            else
            {
                TempData["msg"] = "This Email is not registered with us.";
                return RedirectToAction("login");
            }

        }
        public ActionResult CheckOTP()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CheckOTP(int otp)
        {
            if (Session["otp"]!=null)
            {
                if (otp == Convert.ToInt32(Session["otp"]))
                {
                    TempData["msg"] = "OTP Matched. Login Successful.";
                    Session["emailid"] = Session["email"];
                    //select total items of cart
                    DataTable dt = db.ExecuteSelect($"select sum(qty) from tbl_cart where email='{Session["emailid"]}'");
                    Session["cartitem"] = dt.Rows[0][0];
                    return RedirectToAction("index");
                }
                else
                {
                    TempData["msg"] = "OTP not Matched. Try Again.";
                }

            }
            else
            {
                TempData["msg"] = "Kindly goto login and then check OTP";
            }
           
            return View();
        }
        public ActionResult Myorders()
        {
            if (Session["emailid"] == null)
            {
                TempData["msg"] = "Login first to access this page";
                return RedirectToAction("login");

            }
            string email = Session["emailid"].ToString();
            DataTable dt = db.ExecuteSelect($"select * from tbl_order where email='{email}' order by order_on desc");
            ViewBag.dt = dt;
            return View();
          
        }
        public ActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Contact(string FName, string Email, string Mobile, string Subject, string Message)
        {
            int res = db.ExecuteSQL($@"
    insert into tbl_enquiry
    values('{FName}',
           '{Mobile}',
           '{Email}',
           'General',
           '{Subject}',
           '{Message}',
           'Pending',
           '',
           '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')");

            if (res > 0)
                TempData["msg"] = "Enquiry Sent Successfully.";
            else
                TempData["msg"] = "Something went wrong.";

            return RedirectToAction("Contact");
        }

        public ActionResult Address()
        {
            if (Session["emailid"] == null)
            {
                TempData["msg"] = "Login first to access this page";
                return RedirectToAction("login");

            }
            string email = Session["emailid"].ToString();
            DataTable dt = db.ExecuteSelect($"select * from tbl_address where email='{email}'");
            ViewBag.add = dt;
            return View();

        }
        [HttpPost]
        public ActionResult Address(string Address,string City,string Pincode,string State,string Landmark,string Addresstype,string mobile)
        {
            if (Session["emailid"] == null)
            {
                TempData["msg"] = "login first to access this page";
                return RedirectToAction("login");
            }
            string email = Session["emailid"].ToString();
            int res = db.ExecuteSQL($@"insert into tbl_address values('{email}','{Address}','{City}','{Pincode}','{State}','{Landmark}','{Addresstype}','{DateTime.Now.ToString()}','{mobile}')");
            if (res > 0)
            {

                return Content("<script> alert('address saved');location.href='/General/address'</script>");
            }
            else
            {
                return Content("<script>alert('address not saved error');location.href='/General/address'</script>");
            }
        }
        public ActionResult deleteaddress(int id)
        {
            int res = db.ExecuteSQL($"delete from tbl_address where id={id}");
            if (res > 0)
            {

                return Content("<script> alert('address delete successfully');location.href='/General/address'</script>");
            }
            else
            {
                return Content("<script>alert('address not delete ');location.href='/General/address'</script>");
            }
        }
        public ActionResult faqs()
        {
            return View();
        }
        public ActionResult changepassword()
        {
            if (Session["emailid"] == null)
            {
                TempData["msg"] = "Login first to access this page";
                return RedirectToAction("login");

            }
            return View();

        }

        public ActionResult developer()
        {
            return View();
        }
        [NonAction]
        public void SendEmail(string receiver,string subject,string email)
        {
            MailMessage message = new MailMessage("aryanmaurya294@gmail.com",receiver);
            message.Subject = subject;
            message.Body = email;
            message.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient();
            smtp.Port = 587; 
            smtp.Host = "smtp.gmail.com";
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential("aryanmaurya294@gmail.com", "jhkr vnmd dimi asio");
            smtp.Send(message);
        }
        public ActionResult logout()
        {
            Session.RemoveAll(); //delete all active session
            TempData["msg"] = "logout successfully. You can login again here.";
            return RedirectToAction("login");
        }
        public ActionResult addtocart(int pid, int qty)
        {
            if (Session["emailid"] == null)
            {
                return Json(0, JsonRequestBehavior.AllowGet);
            }
            string email = Session["emailid"].ToString();
            DataTable dt = db.ExecuteSelect($"select * from tbl_cart where email='{email}' and productid={pid}");
            if (dt.Rows.Count > 0)
            {
                int res = db.ExecuteSQL($"update tbl_cart set qty={qty},totalprice=price*{qty} where  email='{email}' and productid={pid}");
            }

            else
            {
                int res = db.ExecuteSQL($"insert into tbl_cart select '{email}',{pid},name,image,mrp,salerate,discount,{qty},salerate*{qty},getdate() from tbl_product where id={pid}");

            }
            DataTable dt1 = db.ExecuteSelect($"select sum(qty) from tbl_cart where email='{Session["emailid"]}'");
            Session["cartitem"] = dt1.Rows[0][0];
            return Json(dt1.Rows[0][0],JsonRequestBehavior.AllowGet);
        }
        public ActionResult orderproducts(int? oid)
        {
            DataTable dt = new DataTable();
            if (oid.HasValue)
            {
                 dt = db.ExecuteSelect($"select * from tbl_orderitem where orderid={oid.Value}");
            }
            ViewBag.dt = dt;

            return View();
        }
    }
}
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            Console.WriteLine("🔥🔥🔥 TEST CONTROLLER ĐƯỢC GỌI 🔥🔥🔥");
            return Content("Test OK");
        }
    }
}
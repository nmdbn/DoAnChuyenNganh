using DoAnChuyenNganh.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AdminBaseController : Controller
    {
    }
}

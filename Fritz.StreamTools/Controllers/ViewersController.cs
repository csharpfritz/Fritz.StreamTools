using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fritz.StreamTools.Controllers
{
  public class ViewersController : Controller
  {

    public ViewersController(StreamService streamService)
    {
      this.StreamService = streamService;
    }

    public StreamService StreamService { get; }

    public IActionResult Current()
    {
      return View(StreamService.CurrentViewerCount);
    }
  }
}
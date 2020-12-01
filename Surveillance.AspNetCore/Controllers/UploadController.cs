using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Dynamic;
using System.IO;

namespace Surveillance.AspNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class UploadController : ControllerBase
    {
        readonly IHostEnvironment env;

        public UploadController(IHostEnvironment env)
        {
            this.env = env;
        }

        string UploadPath
        {
            get
            {
                var path = Path.Combine(env.ContentRootPath, "uploads");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

#if DEBUG
        [HttpGet]
        public ActionResult Get()
        {
            if (env.IsDevelopment())
            {
                dynamic result = new ExpandoObject();
                result.ApplicationName = env.ApplicationName;
                result.ContentRootPath = env.ContentRootPath;
                result.EnvironmentName = env.EnvironmentName;
                result.UploadPath = UploadPath;
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
#endif
    }
}
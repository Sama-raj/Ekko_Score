using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrawlerController : ControllerBase
    {

        [HttpOptions]
        public bool RunCrawler(string urlfilenames)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "D:\\rmit\\project\\AzureSearchCrawler\\AzureSearchCrawler\\bin\\Release\\AzureSearchCrawler.exe";

                process.StartInfo.Arguments = urlfilenames;
                process.Start();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
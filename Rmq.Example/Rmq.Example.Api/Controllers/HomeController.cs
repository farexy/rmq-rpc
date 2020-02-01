using Microsoft.AspNetCore.Mvc;

namespace Rmq.Example.Api.Controllers
{
    [Route("")]
    public class HomeController
    {
        [HttpGet]
        public string Index() => "Ok";
    }
}
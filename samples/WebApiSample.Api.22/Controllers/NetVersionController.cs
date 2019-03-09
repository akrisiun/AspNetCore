using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace WebApiSample.Api._22.Controllers
{
    [Route("api/[controller]")]
    public class NetVersionController : ControllerBase
    {
        public NetVersionController(IServiceProvider services)
        {
        }

 
        [HttpGet]
        // ActionResult<
        public async Task<IEnumerable<JObject>> GetAsync()
        {
            List<JObject> list = new List<JObject>();

            var asm = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var item in asm) {
                if (!item.IsDynamic) {

                    list.Add(JObject.FromObject(
                        new { FullName = item.FullName, Location = item.Location }
                    ));
                }
            }

            var list2 = list.OrderBy(i => i.Value<string>("Location"));

            return await Task.FromResult(list2);
        } 
    }
}

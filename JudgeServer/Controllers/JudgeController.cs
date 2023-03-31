using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JudgeServer.Controllers {
    [Route("[controller]")]
    [ApiController]
    public class JudgeController : ControllerBase {

        // POST api/<RunCode>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JudgeRequest request) {
            Console.WriteLine("#문제 번호 : " + request.TaskId);
            Console.WriteLine("#언어 : " + request.Language);
            Console.WriteLine("#코드 : " + request.Code);

            // 채점 수행
            // 채점 DB에서 추가적인 정보 받아와서 가공하고 사용하는 작업 필요
            return Ok(Judge.JudgeHandler[request.Language](request));
        }
    }
}

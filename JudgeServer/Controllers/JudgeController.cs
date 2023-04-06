using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JudgeServer.Controllers {
    [Route("[controller]")]
    [ApiController]
    public class JudgeController : ControllerBase {
        // POST <JudgeController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JudgeRequest request) {
            Console.WriteLine("채점 시작");
            Console.WriteLine("#문제 Id : " + request.Id);
            Console.WriteLine("#코드 : " + request.Code);

            // 코드를 채점한 결과를 받는다.
            JudgeResult result = await Judge.JudgeHandler[request.Language](request);

            return Ok(result);
        }
    }
}

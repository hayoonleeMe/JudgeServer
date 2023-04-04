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

            // 채점 작업을 수행하는 Task 등록
            var judgeTask = Task<JudgeResult>.Run(() => JudgeAsync(request));
            // Task가 끝날 때까지 대기하여 결과를 받음
            JudgeResult result = await judgeTask;

            return Ok(result);
        }

        // 비동기로 채점을 수행함
        private Task<JudgeResult> JudgeAsync(JudgeRequest request) {
            // 채점 DB에서 추가적인 정보 받아와서 가공하고 사용하는 작업 필요
            return Judge.TaskJudgeHandler[request.Language](request);
        }
    }
}

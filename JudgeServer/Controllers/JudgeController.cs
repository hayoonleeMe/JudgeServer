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
            Console.WriteLine("채점 시작");
            Console.WriteLine("#문제 Id : " + request.Id);
            Console.WriteLine("#코드 : " + request.Code);
            
            // 채점 작업을 수행하는 Task 등록
            var judgeTask = Task<JudgeResult>.Run(() => JudgeAsync(request));   
            // Task가 끝날 때까지 대기하여 결과를 받음
            JudgeResult result = await judgeTask;
            switch (result.Result) {
                case JudgeResult.JResult.Accepted:
                    Console.WriteLine("맞았습니다.");
                    Console.WriteLine("실행 시간(ms) : " + result.ExecutionTime);
                    Console.WriteLine("메모리 사용량(KB) : " + result.MemoryUsage);
                    break;
                case JudgeResult.JResult.WrongAnswer:
                    Console.WriteLine("틀렸습니다.");
                    break;
                case JudgeResult.JResult.CompileError:
                    Console.WriteLine("컴파일 에러");
                    break;
                case JudgeResult.JResult.RuntimeError:
                    Console.WriteLine("런타임 에러");
                    break;
                case JudgeResult.JResult.TimeLimitExceeded:
                    Console.WriteLine("시간 초과");
                    break;
                case JudgeResult.JResult.MemoryLimitExceeded:
                    Console.WriteLine("메모리 초과");
                    break;
                case JudgeResult.JResult.PresentationError:
                    Console.WriteLine("출력 형식 에러");
                    break;
                case JudgeResult.JResult.OutputLimitExceeded:
                    Console.WriteLine("출력 한도 초과");
                    break;
                case JudgeResult.JResult.JudgementFailed:
                    Console.WriteLine("채점 실패");
                    break;
                case JudgeResult.JResult.Pending:
                    Console.WriteLine("채점 미완료");
                    break;
            }

            return Ok(result);
        }

        // 비동기로 채점을 수행함
        private JudgeResult JudgeAsync(JudgeRequest request) {
            // 채점 수행
            // 채점 DB에서 추가적인 정보 받아와서 가공하고 사용하는 작업 필요
            // 임의로 c언어로 설정
            return Judge.JudgeHandler["c"](request);
        }
    }
}

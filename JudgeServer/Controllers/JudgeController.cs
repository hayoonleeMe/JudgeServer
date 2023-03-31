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
            //return Ok(Judge.JudgeHandler[request.Language](request));
            JudgeResult result = Judge.JudgeHandler[request.Language](request);

            // JudgeResult를 받은 곳에서 채점 결과를 판단할 때 사용할 로직
            // 입력한 코드가 모든 테스트 케이스를 통과하여 맞았을 때
            if (result.IsCorrect == true) {
                Console.WriteLine("결과: 맞았습니다!  " + "실행 시간(ms): " + result.ExecutionTime + "  메모리 사용량(byte): " + result.MemoryUsage);
            } 
            // 입력한 코드가 테스트 케이스를 통과하지 못한 경우
            else {
                if (result.CompileErrorMsg != null) {
                    Console.WriteLine("컴파일 에러: " + result.CompileErrorMsg);
                }
                else if (result.RuntimeErrorMsg != null) {
                    Console.WriteLine("런타임 에러: "  + result.RuntimeErrorMsg);
                }
                else if (result.IsTimeOut) {
                    Console.WriteLine("시간 초과");
                } 
                else if (result.IsExceedMemory) {
                    Console.WriteLine("메모리 초과");
                }
            }

            return Ok(result);
        }
    }
}

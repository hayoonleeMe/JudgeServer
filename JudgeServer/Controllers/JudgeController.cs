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

            // 채점 진행
            Console.WriteLine("c언어 컴파일 및 채점 시작");

            // C 컴파일러를 실행하여 C 코드를 컴파일합니다.
            ProcessStartInfo psi = new ProcessStartInfo("C:\\MinGW\\bin\\mingw-get.exe", "hello.c -o hello");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            Process process = Process.Start(psi);

            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();

            process.WaitForExit();

            //// 생성된 실행 파일을 실행합니다.
            //ProcessStartInfo psi2 = new ProcessStartInfo("./hello");
            //psi2.RedirectStandardOutput = true;
            //psi2.UseShellExecute = false;
            //Process process2 = Process.Start(psi2);

            //string output2 = process2.StandardOutput.ReadToEnd();

            //process2.WaitForExit();

            //Console.WriteLine(output2);

            Console.WriteLine("c언어 컴파일 및 채점 종료");

            JudgeResult judgeResult = new JudgeResult { IsCorrect = true, ExecutionTime = 800, MemoryUsage = 32 };

            Console.WriteLine("IsCorrect : " + (judgeResult.IsCorrect ? "true" : "false"));
            Console.WriteLine("ExecutionTime (ms) : " + judgeResult.ExecutionTime);
            Console.WriteLine("MemoryUsage (KB) : " + judgeResult.MemoryUsage);

            return Ok(judgeResult);
        }
    }
}

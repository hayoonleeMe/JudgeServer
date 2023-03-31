using System.Diagnostics;

namespace JudgeServer {
    public static class Judge {
        private const string C = "c";
        private const string CPP = "cpp";
        private const string CSHARP = "csharp";
        private const string JAVA = "java";
        private const string PYTHON = "python";

        public static Dictionary<string, Func<JudgeRequest, JudgeResult>> JudgeHandler;

        static Judge() {
            JudgeHandler = new Dictionary<string, Func<JudgeRequest, JudgeResult>>() {
                { C, JudgeC }, { CPP, JudgeCpp }, { CSHARP, JudgeCSharp }, { JAVA, JudgeJava }, { PYTHON, JudgePython },};
        }

        private static JudgeResult JudgeC(JudgeRequest request) {

            // 반환할 객체
            JudgeResult result = new JudgeResult();

            // TODO : 채점 DB에서 입출력 테스트 케이스, 실행 시간 제한, 메모리 사용량 제한 받아오기
            // TODO : 채점 DB에서 가져오는 값들이 교수가 과제를 등록할 때 정할 수 있다면, 정해진 값들만 사용하도록 코드 개선 필요
            // 입력 테스트 케이스
            List<string> inputCases = new List<string>() { "1 2", "5 9", "-3 3" };
            // 출력 테스트 케이스
            List<string> outputCases = new List<string>() { "3", "14", "0" };
            // 실행 시간(ms) 제한 - 500ms
            double executionTimeLimit = 500;
            // 메모리 사용량(B) 제한 - 512B
            long memoryUsageLimit = 512;

            // .c 파일이 저장될 경로
            string cFilePath = Environment.CurrentDirectory + "\\file.c";
            // .c 파일의 실행 파일인 .exe 파일이 저장될 경로
            string exeFilePath = Environment.CurrentDirectory + "\\file.exe";
            // 전달받은 코드로 .c 파일 생성하기
            File.WriteAllText(cFilePath, request.Code);

            // 컴파일러 경로
            string gccCompilerPath = "C:\\MinGW\\bin\\gcc";

            // C11 컴파일러를 실행하여 C 코드를 컴파일하여 실행 파일 생성
            ProcessStartInfo psi = new ProcessStartInfo(gccCompilerPath, cFilePath + " -o" + exeFilePath + " -O2 -Wall -lm -static -std=gnu11");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            Process? process = Process.Start(psi);

            // 예외처리 필요
            process.WaitForExit();

            // 컴파일 실패
            if (process.ExitCode != 0) {
                Console.WriteLine("Compile Failed");
                string errors = process.StandardError.ReadToEnd();
                Console.WriteLine($"{errors}");

                result.IsSuccess = false;
                result.CompileErrorMsg = errors;
                return result;
            } 
            // 컴파일 성공
            else {
                Console.WriteLine("Compile Successed");
                //string output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine($"{output}");
            }

            // 테스트 케이스들의 평균 실행 시간과 메모리 사용량
            double avgExecutionTime = 0;
            long avgMemoryUsage = 0;

            // 생성된 실행 파일을 테스트 케이스 만큼 실행
            for (int i = 0; i < inputCases.Count(); i++) {
                ProcessStartInfo psi2 = new ProcessStartInfo(exeFilePath);
                psi2.RedirectStandardOutput = true;
                psi2.RedirectStandardError = true;
                psi2.RedirectStandardInput = true;
                psi2.UseShellExecute = false;
                Process? process2 = Process.Start(psi2);

                // 예외처리 필요
                // input case 적용
                using (StreamWriter writer2 = process2.StandardInput) {
                    writer2.Write(inputCases[i]);
                }

                // 스톱워치 시작
                Stopwatch watch = new Stopwatch();
                watch.Start();

                // 프로세스 실행 전의 메모리 사용량 측정
                long memoryUsageBefore = process2.WorkingSet64;

                // 프로세스 실행
                process2.WaitForExit();

                // 스톱워치를 멈춰 걸린 시간으로 실행 시간 측정
                watch.Stop();
                double executionTime = watch.ElapsedMilliseconds;
                avgExecutionTime += executionTime;

                // 프로세스 실행 후의 메모리 사용량 측정
                long memoryUsageAfter = process2.WorkingSet64;

                // 메모리 사용량 계산
                long memoryUsage = memoryUsageAfter - memoryUsageBefore;
                avgMemoryUsage += memoryUsage;

                // 실행 실패 - 런타임 에러
                if (process2.ExitCode != 0) {
                    string errors = process2.StandardError.ReadToEnd();
                    Console.WriteLine("Runtime Error");
                    Console.WriteLine($"{errors}");

                    result.IsSuccess = false;
                    result.RuntimeErrorMsg = errors;
                    return result;
                }
                // 실행 성공
                else {
                    string output = process2.StandardOutput.ReadToEnd();
                    Console.WriteLine("실행 결과 : " + output);
                    Console.WriteLine("실행 시간(ms) : " + executionTime);
                    Console.WriteLine("메모리 사용량(byte) : " + memoryUsage);

                    // TODO : 추가적으로 채점 수행 필요
                    // 시간 초과
                    if (executionTime > executionTimeLimit) {
                        Console.WriteLine("시간 초과");

                        result.IsSuccess = false;
                        return result;
                    }

                    // 메모리 사용량 초과
                    if (memoryUsage > memoryUsageLimit) {
                        Console.WriteLine("메모리 사용량 초과");

                        result.IsSuccess = false;
                        return result;
                    }

                    Console.WriteLine("output : " + output);
                    Console.WriteLine("outputCase : " + outputCases[i]);

                    // 정답 맞춤
                    if (output == outputCases[i]) {
                        Console.WriteLine(i + "번째 테스트 케이스가 맞았습니다.");
                    }
                    // 정답 틀림
                    else {
                        Console.WriteLine("틀렸습니다.");

                        result.IsSuccess = true;
                        result.IsCorrect = false;
                        return result;
                    }
                }
            }

            // 생성했던 파일 삭제
            File.Delete(cFilePath);
            File.Delete(exeFilePath);

            avgExecutionTime /= inputCases.Count();
            avgMemoryUsage /= inputCases.Count();

            Console.WriteLine("avgExecutionTime : " + avgExecutionTime);
            Console.WriteLine("avgMemoryUsage : " + avgMemoryUsage);

            // 모든 테스트 케이스를 통과
            result.IsSuccess = true;
            result.IsCorrect = true;
            result.ExecutionTime = avgExecutionTime;
            result.MemoryUsage = avgMemoryUsage;
            return result;
        }

        private static JudgeResult JudgeCpp(JudgeRequest request) {
            Console.WriteLine("JudgeCpp is Called");
            return new JudgeResult();
        }

        private static JudgeResult JudgeCSharp(JudgeRequest request) {
            Console.WriteLine("JudgeCSharp is Called");
            return new JudgeResult();
        }

        private static JudgeResult JudgeJava(JudgeRequest request) {
            Console.WriteLine("JudgeJava is Called");
            return new JudgeResult();
        }

        private static JudgeResult JudgePython(JudgeRequest request) {
            Console.WriteLine("JudgePython is Called");
            return new JudgeResult();
        }
    }
}

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
            Console.WriteLine("JudgeC is Called");

            JudgeResult result = new JudgeResult();

            // 입력 값을 저장할 파일 이름
            string inputData = "0 10";

            // C11 컴파일러를 실행하여 C 코드를 컴파일합니다.
            ProcessStartInfo psi = new ProcessStartInfo("C:\\MinGW\\bin\\gcc", "hello.c -o hello -O2 -Wall -lm -static -std=gnu11");
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            Process? process = Process.Start(psi);

            if (process == null) {
                Console.WriteLine("Process is null");
                result.IsSuccess = false;
                return result;
            }

            process.WaitForExit();

            // 컴파일 실패
            if (process.ExitCode != 0) {
                string errors = process.StandardError.ReadToEnd();
                Console.WriteLine("Compile Failed");
                Console.WriteLine($"{errors}");
                result.IsSuccess = false;
                result.CompileErrorMsg = errors;
                return result;
            } else {
                string output = process.StandardOutput.ReadToEnd();
                Console.WriteLine("Compile Successed");
                Console.WriteLine($"{output}");
            }

            // 생성된 실행 파일을 실행합니다.
            ProcessStartInfo psi2 = new ProcessStartInfo("./hello");
            psi2.RedirectStandardOutput = true;
            psi2.RedirectStandardError = true;
            psi2.RedirectStandardInput = true;
            psi2.UseShellExecute = false;
            Process? process2 = Process.Start(psi2);

            if (process2 == null) {
                Console.WriteLine("Process is null");
                result.IsSuccess = false;
                return result;
            }

            using (StreamWriter writer2 = process2.StandardInput) {
                writer2.Write(inputData);
            }

            // 실행파일 실행
            try {
                // 실행 프로세스가 끝날 때까지 대기.
                process2.WaitForExit();

                // 실행 프로세스가 실패할 때
                if (process2.ExitCode != 0) {
                    string errors = process2.StandardError.ReadToEnd();
                    Console.WriteLine("Runtime Error");
                    Console.WriteLine($"{errors}");
                    result.IsSuccess = false;
                    result.RuntimeErrorMsg = errors;
                    return result;

                // 실행 프로세스가 성공적으로 끝날 때
                } else {
                    string output = process2.StandardOutput.ReadToEnd();
                    Console.WriteLine(output);

                    // 추가적으로 채점 수행
                    result.IsSuccess = true;
                    result.IsCorrect = true;
                    result.ExecutionTime = 800;
                    result.MemoryUsage = 32;
                    return result;
                }

                // 실행 중에 예외 발생 처리
            } catch (Exception ex) {
                Console.WriteLine("An exception occurred while running the program");
                Console.WriteLine($"Exception message: {ex.Message}");
                result.IsSuccess = false;
                return result;
            } finally {
                // 실행파일 제거
                File.Delete("hello.exe");
            }
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.ComponentModel;
using System.Text;
using System;

namespace JudgeServer {
    public class Judge {
        // 각 언어들의 심볼릭 상수
        // NOTE : 채점 DB에서 과제의 요구 언어를 string으로 저장할 것으로 예상하여 상수의 값을 string으로 설정함
        private const string C = "c";
        private const string CPP = "cpp";
        private const string CSHARP = "csharp";
        private const string JAVA = "java";
        private const string PYTHON = "python";

        // Judge 클래스에서 유일하게 접근할 수 있는 Judge 함수 Handler
        // 이 Dictionary 객체를 언어 문자열로 인덱싱하여 JudgeRequest 객체를 인자로 전달하여 사용
        // Ex) JudgeResult result = JudgeHandler["c"](request); == JudgeC(request)
        public static Dictionary<string, Func<JudgeRequest, Task<JudgeResult>>> JudgeHandler;

        // static class의 생성자
        static Judge() {
            // Judge 함수 Handler를 초기화
            JudgeHandler = new Dictionary<string, Func<JudgeRequest, Task<JudgeResult>>>() {
                { C, JudgeCAsync }, { CPP, JudgeCppAsync }, { CSHARP, JudgeCSharpAsync }, { JAVA, JudgeJavaAsync }, { PYTHON, JudgePythonAsync },};
        }

        // C 코드 채점
        private static JudgeResult JudgeCLocal(JudgeRequest request) {
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
            ProcessStartInfo compilePsi = new ProcessStartInfo(gccCompilerPath, cFilePath + " -o" + exeFilePath + " -O2 -Wall -lm -static -std=gnu11");
            compilePsi.RedirectStandardOutput = true;
            compilePsi.RedirectStandardError = true;
            compilePsi.UseShellExecute = false;
            Process? compileProcess = Process.Start(compilePsi);

            // 예외처리 필요
            compileProcess.WaitForExit();

            // 컴파일 에러
            if (compileProcess.ExitCode != 0) {
                string errors = compileProcess.StandardError.ReadToEnd();
                Console.WriteLine("Compile Failed");
                Console.WriteLine($"{errors}");

                result.Result = JudgeResult.JResult.CompileError;
                result.Message = errors;
                return result;
            }
            // 컴파일 성공
            else {
                Console.WriteLine("Compile Successed");
            }

            // 테스트 케이스들의 평균 실행 시간과 메모리 사용량
            double avgExecutionTime = 0;
            long avgMemoryUsage = 0;

            // 생성된 실행 파일을 테스트 케이스 만큼 실행
            for (int i = 0; i < inputCases.Count(); i++) {
                Console.WriteLine(i + "번째 테스트 케이스 시작");
                ProcessStartInfo executePsi = new ProcessStartInfo(exeFilePath);
                executePsi.RedirectStandardOutput = true;
                executePsi.RedirectStandardError = true;
                executePsi.RedirectStandardInput = true;
                executePsi.UseShellExecute = false;
                Process? executeProcess = Process.Start(executePsi);

                // 예외처리 필요
                // input case 적용
                using (StreamWriter writer2 = executeProcess.StandardInput) {
                    writer2.Write(inputCases[i]);
                }

                // 스톱워치 시작
                Stopwatch watch = new Stopwatch();
                watch.Start();

                // 프로세스 실행 전의 메모리 사용량 측정
                long memoryUsageBefore = executeProcess.WorkingSet64;

                // 프로세스 실행
                executeProcess.WaitForExit();

                // 스톱워치를 멈춰 걸린 시간으로 실행 시간 측정
                watch.Stop();
                double executionTime = watch.ElapsedMilliseconds;
                avgExecutionTime += executionTime;

                // 프로세스 실행 후의 메모리 사용량 측정
                long memoryUsageAfter = executeProcess.WorkingSet64;

                // 메모리 사용량 계산
                long memoryUsage = memoryUsageAfter - memoryUsageBefore;
                avgMemoryUsage += memoryUsage;

                // 실행 실패 - 런타임 에러
                if (executeProcess.ExitCode != 0) {
                    string errors = executeProcess.StandardError.ReadToEnd();
                    Console.WriteLine("Runtime Error");
                    Console.WriteLine(errors);

                    result.Result = JudgeResult.JResult.RuntimeError;
                    result.Message = errors;
                    break;
                }
                // 실행 성공
                else {
                    string output = executeProcess.StandardOutput.ReadToEnd();

                    // TODO : 추가적으로 채점 수행 필요
                    // 시간 초과
                    if (executionTime > executionTimeLimit) {
                        Console.WriteLine("시간 초과");

                        result.Result = JudgeResult.JResult.TimeLimitExceeded;
                        break;
                    }

                    // 메모리 사용량 초과
                    if (memoryUsage > memoryUsageLimit) {
                        Console.WriteLine("메모리 사용량 초과");

                        result.Result = JudgeResult.JResult.MemoryLimitExceeded;
                        break;
                    }

                    Console.WriteLine("output : " + output);
                    Console.WriteLine("outputCase : " + outputCases[i]);

                    // 정답 맞춤
                    if (output == outputCases[i]) {
                        Console.WriteLine(i + "번째 테스트 케이스가 맞았습니다.");

                        result.Result = JudgeResult.JResult.Accepted;
                    }
                    // 정답 틀림
                    else {
                        Console.WriteLine(i + "번째 테스트 케이스가 틀렸습니다.");

                        result.Result = JudgeResult.JResult.WrongAnswer;
                        break;
                    }
                }
            }

            // 생성했던 파일 삭제
            File.Delete(cFilePath);
            File.Delete(exeFilePath);

            // 테스트 케이스를 통과하지 못하여 break로 for문을 빠져나온 상태
            if (result.Result != JudgeResult.JResult.Accepted) {
                return result;
            }
            // 모든 테스트 케이스를 통과
            else {
                avgExecutionTime /= inputCases.Count();
                avgMemoryUsage /= inputCases.Count();

                Console.WriteLine("avgExecutionTime : " + avgExecutionTime);
                Console.WriteLine("avgMemoryUsage : " + avgMemoryUsage);

                result.ExecutionTime = avgExecutionTime;
                result.MemoryUsage = avgMemoryUsage;

                return result;
            }
        }

        private static async Task<JudgeResult> JudgeCAsync(JudgeRequest request) {
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

            // 채점 요청별로 랜덤 값 생성
            Random randomObj = new Random();
            string randomValue = randomObj.Next().ToString();

            // 유저 전용 폴더 생성
            string folderPath = @"C:\Users\LeeHaYoon\Desktop\docker\c\" + randomValue;

            // 폴더가 존재하지 않는 경우에만 폴더를 생성합니다.
            if (!Directory.Exists(folderPath)) {
                // 폴더를 생성합니다.
                Directory.CreateDirectory(folderPath);
                Console.WriteLine($"폴더가 생성되었습니다: {folderPath}");
            }

            // 코드를 파일로 저장
            string codeFilePath = Path.Combine(folderPath, "code.c");
            File.WriteAllText(codeFilePath, request.Code);

            // 테스트 케이스들의 평균 실행 시간과 메모리 사용량
            // TODO : 실행 시간과 메모리 사용량 계산 구현
            double avgExecutionTime = 0;
            long avgMemoryUsage = 0;

            // 입력 케이스가 저장되는 경로
            string inputFilePath = Path.Combine(folderPath, "input.txt");

            // 컴파일 에러 메시지의 경로
            string compileErrorFilePath = Path.Combine(folderPath, "compileError.txt");

            // 런타임 에러 메시지의 경로
            string runtimeErrorFilePath = Path.Combine(folderPath, "runtimeError.txt");

            // 결과가 저장되는 경로
            string resultFilePath = Path.Combine(folderPath, "result.txt");

            // 테스트 케이스 수행
            for (int i = 0; i < outputCases.Count(); i++) {
                // 입력 케이스를 파일로 저장
                File.WriteAllText(inputFilePath, inputCases[i]);

                // Docker Hub에서의 이미지 이름과 태그
                string imageName = "leehayoon/judge";
                string imageTag = "c";

                // Docker client 생성
                using var dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

                // 이미지 다운로드
                await dockerClient.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = imageName, Tag = imageTag }, new AuthConfig(), new Progress<JSONMessage>());

                // 볼륨 맵핑 - 로컬 유저 폴더 : 컨테이너 내부 유저 폴더
                var volumeMapping = new Dictionary<string, string>
                 {
                     { folderPath, $"/app/{randomValue}" }
                 };

                // 컨테이너 생성
                var createContainerResponse = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters {
                    Image = $"{imageName}:{imageTag}",
                    Tty = true,
                    // 환경 변수 설정
                    Env = new List<string> { "DIR_NAME=" + randomValue },
                    // 볼륨 설정
                    HostConfig = new HostConfig {
                        Binds = volumeMapping.Select(kv => $"{kv.Key}:{kv.Value}").ToList()
                    }
                });

                // 컨테이너 실행
                await dockerClient.Containers.StartContainerAsync(createContainerResponse.ID, new ContainerStartParameters());

                // 컨테이너 로그
                var logs = await dockerClient.Containers.GetContainerLogsAsync(createContainerResponse.ID, new ContainerLogsParameters { ShowStdout = true, ShowStderr = true });
                using (var reader = new StreamReader(logs)) {
                    string log = reader.ReadToEnd();
                    Console.WriteLine(log);
                }
                
                // 컨테이너 종료 및 삭제
                await dockerClient.Containers.StopContainerAsync(createContainerResponse.ID, new ContainerStopParameters());
                await dockerClient.Containers.RemoveContainerAsync(createContainerResponse.ID, new ContainerRemoveParameters());

                // 컴파일 에러 발생
                if (File.Exists(compileErrorFilePath)) {
                    string errorMsg = File.ReadAllText(compileErrorFilePath);
                    
                    if (errorMsg.Length != 0) {
                        Console.WriteLine("Compile Error Occured : ", errorMsg);

                        result.Result = JudgeResult.JResult.CompileError;
                        result.Message = errorMsg;
                        break;
                    }
                }

                // 런타임 에러가 발생했는지 체크
                if (File.Exists(runtimeErrorFilePath)) {
                    string errorMsg = File.ReadAllText(runtimeErrorFilePath);

                    if (errorMsg.Length != 0) {
                        Console.WriteLine("Runtime Error Occured : ", errorMsg);

                        result.Result = JudgeResult.JResult.RuntimeError;
                        result.Message = errorMsg;
                        break;
                    }
                }

                // 출력 케이스와 결과 비교
                string expectedOutput = outputCases[i];
                string actualOutput = File.Exists(resultFilePath) ? File.ReadAllText(resultFilePath) : "";

                // TODO : 시간 초과

                // TODO : 메모리 초과

                // 틀림
                if (expectedOutput != actualOutput) {
                    Console.WriteLine($"[WrongAnswer] expected : {expectedOutput} / actual : {actualOutput}");

                    result.Result = JudgeResult.JResult.WrongAnswer;
                    break;
                }

                // 맞음
                result.Result = JudgeResult.JResult.Accepted;

                Console.WriteLine($"{i + 1}번째 케이스 통과");

                // 입력 파일 초기화
                if (File.Exists(inputFilePath)) {
                    File.WriteAllText(inputFilePath, string.Empty);
                }

                // 결과 파일 초기화
                if (File.Exists(resultFilePath)) {
                    File.WriteAllText(resultFilePath, string.Empty); 
                }
            }

            // TODO : 폴더 삭제

            // 테스트 케이스를 통과하지 못함
            if (result.Result != JudgeResult.JResult.Accepted) {
                return result;
            }

            // 모든 테스트 케이스를 통과
            Console.WriteLine("모든 케이스 통과");

            // 평균 실행 시간, 메모리 사용량 계산
            avgExecutionTime /= outputCases.Count();
            avgMemoryUsage /= outputCases.Count();

            Console.WriteLine("avgExecutionTime : " + avgExecutionTime);
            Console.WriteLine("avgMemoryUsage : " + avgMemoryUsage);

            // 모든 테스트 케이스를 통과
            result.ExecutionTime = avgExecutionTime;
            result.MemoryUsage = avgMemoryUsage;

            return result;
        }

        // C++ 코드 채점
        private static async Task<JudgeResult> JudgeCppAsync(JudgeRequest request) {
            return new JudgeResult();
        }

        // C# 코드 채점
        private static async Task<JudgeResult> JudgeCSharpAsync(JudgeRequest request) {
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

            // 컴파일 및 실행을 위한 준비
            var syntaxTree = CSharpSyntaxTree.ParseText(request.Code);
            string assemblyName = Path.GetRandomFileName();
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
            };

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            // 컴파일
            using (var ms = new MemoryStream()) {
                var emitResult = compilation.Emit(ms);

                // 컴파일 실패
                if (!emitResult.Success) {
                    Console.WriteLine("컴파일 오류 발생");

                    // 런타임 에러 메시지 저장
                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    string errors = "";
                    foreach (Diagnostic diagnostic in failures) {
                        var lineSpan = diagnostic.Location.GetLineSpan();
                        int lineNumber = lineSpan.StartLinePosition.Line + 1; // 0-based index to 1-based index

                        errors += $"{diagnostic.Id}: {diagnostic.GetMessage()} at line {lineNumber}\n";
                        Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()} at line {lineNumber}");
                    }

                    result.Result = JudgeResult.JResult.CompileError;
                    result.Message = errors;
                    return result;
                }
                // 컴파일 성공
                else {
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());

                    // 프로그램 실행 준비
                    var type = assembly.GetType("Test");
                    var mainMethod = type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    // 테스트 케이스들의 평균 실행 시간과 메모리 사용량
                    double avgExecutionTime = 0;
                    long avgMemoryUsage = 0;

                    // 테스트 케이스 만큼 실행
                    for (int i = 0; i < inputCases.Count(); i++) {
                        Console.WriteLine($"{i} 번째 테스트 케이스 시작");

                        // inputCase를 표준 입력으로 설정
                        string input = inputCases[i];
                        TextReader originalIn = Console.In;
                        Console.SetIn(new StringReader(input));

                        string output;                  // 실행 결과
                        double executionTime = 0;       // 실행 시간
                        long memoryUsage = 0;           // 메모리 사용량

                        // 실행 결과를 표준 출력으로 설정
                        TextWriter originalOut = Console.Out;
                        using (var sw = new StringWriter()) {
                            Console.SetOut(sw);

                            // 스톱워치 시작
                            Stopwatch watch = new Stopwatch();
                            watch.Start();

                            // 실행 전의 메모리 사용량 측정
                            Process currentProcess = Process.GetCurrentProcess();
                            long memoryUsageBefore = currentProcess.WorkingSet64;

                            try {
                                // 실행
                                mainMethod.Invoke(null, new object[] { });

                                // 실행 결과를 변수에 저장
                                output = sw.ToString();
                            }
                            // 런타임 에러가 발생해 프로그램 실행 중지
                            catch (TargetInvocationException ex) {
                                Exception innerException = ex.InnerException; // 실제 발생한 런타임 에러 메시지
                                Console.Error.WriteLine($"Runtime Error: {innerException.Message}");

                                result.Result = JudgeResult.JResult.RuntimeError;
                                result.Message = "Runtime Error: " + innerException.Message;
                                break;
                            } finally {
                                // 표준 입력 및 출력 복원
                                Console.SetIn(originalIn);
                                Console.SetOut(originalOut);
                            }

                            // 스톱워치를 멈춰 걸린 시간으로 실행 시간 측정
                            watch.Stop();
                            TimeSpan elapsedTime = watch.Elapsed;
                            executionTime = watch.ElapsedMilliseconds;
                            avgExecutionTime += executionTime;

                            // 프로세스 실행 후의 메모리 사용량 측정
                            long memoryUsageAfter = currentProcess.WorkingSet64;

                            // 메모리 사용량 계산
                            memoryUsage = memoryUsageAfter - memoryUsageBefore;
                            avgMemoryUsage += memoryUsage;
                        }

                        Console.WriteLine($"Execution Time: {executionTime} ms Memory Used: {memoryUsage} byte");

                        // TODO : 추가적으로 채점 수행 필요
                        // 시간 초과
                        if (executionTime > executionTimeLimit) {
                            Console.WriteLine("시간 초과");

                            result.Result = JudgeResult.JResult.TimeLimitExceeded;
                            break;
                        }

                        // 메모리 사용량 초과
                        if (memoryUsage > memoryUsageLimit) {
                            Console.WriteLine("메모리 사용량 초과");

                            result.Result = JudgeResult.JResult.MemoryLimitExceeded;
                            break;
                        }

                        Console.WriteLine($"output : {output} outputCase : {outputCases[i]}");

                        // 테스트 케이스 통과
                        if (output == outputCases[i]) {
                            Console.WriteLine("맞았습니다.");

                            result.Result = JudgeResult.JResult.Accepted;
                        }
                        // 틀림
                        else {
                            Console.WriteLine("틀렸습니다.");

                            result.Result = JudgeResult.JResult.WrongAnswer;
                            break;
                        }
                    }

                    // 모든 테스트 케이스를 통과하지 못해 break로 빠져나오면 바로 반환
                    if (result.Result != JudgeResult.JResult.Accepted) {
                        return result;
                    }
                    // 모든 테스트 케이스를 통과
                    else {
                        avgExecutionTime /= inputCases.Count();
                        avgMemoryUsage /= inputCases.Count();

                        Console.WriteLine("avgExecutionTime : " + avgExecutionTime);
                        Console.WriteLine("avgMemoryUsage : " + avgMemoryUsage);

                        result.ExecutionTime = avgExecutionTime;
                        result.MemoryUsage = avgMemoryUsage;

                        return result;
                    }
                }
            }
        }

        // Java 코드 채점
        private static async Task<JudgeResult> JudgeJavaAsync(JudgeRequest request) {
            return new JudgeResult();
        }

        // Python 코드 채점
        private static async Task<JudgeResult> JudgePythonAsync(JudgeRequest request) {
            return new JudgeResult();
        }
    }
}
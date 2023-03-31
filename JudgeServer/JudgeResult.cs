namespace JudgeServer {
    public class JudgeResult {
        public bool IsSuccess { get; set; }
        public string? CompileErrorMsg { get; set; }   
        public string? RuntimeErrorMsg { get; set; }
        public bool IsCorrect { get; set; } = false;
        public double ExecutionTime { get; set; } = 0;
        public long MemoryUsage { get; set; } = 0;
    }
}

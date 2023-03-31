namespace JudgeServer {
    public class JudgeResult {
        public bool IsCorrect { get; set; }
        public double ExecutionTime { get; set; } = 0;
        public long MemoryUsage { get; set; } = 0;
        public string? CompileErrorMsg { get; set; } = null;  
        public string? RuntimeErrorMsg { get; set; } = null;
        public bool IsTimeOut { get; set; } = false;
        public bool IsExceedMemory { get; set; } = false;
    }
}

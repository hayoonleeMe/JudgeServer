namespace JudgeServer {
    public class JudgeRequest {
        // 문제를 식별할 수 있는 Id
        public string? Id { get; set; }
        // 에디터에 작성하여 제출한 코드
        public string? Code { get; set; }
    }
}

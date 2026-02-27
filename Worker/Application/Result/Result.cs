

namespace Application.Result
{
    public class Result<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }

        public static Result<T> Fail(string message)
            => new() { Succeeded = false, Message = message };

        public static Result<T> Success(T data, string message = "")
            => new() { Succeeded = true, Message = message, Data = data };
    }
}

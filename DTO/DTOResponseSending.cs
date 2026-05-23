namespace DTOResponseSending;

public class Result<T>
{
    public bool IsSuccess {get; set; }
    public string ErrorMessage {get; set;}
    public T Data {get; set;}
    public string Message {get; set;}

    public static Result<T> Success(T data)
    {
        return new Result<T> {IsSuccess = true, Data = data};
    }
    public static Result<T> Success(T data, string message)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }
    public static Result<T> Failure(string errorMessage)
    {
        return new Result<T>{ IsSuccess = false, ErrorMessage = errorMessage};
    }
    public static Result<T> Issuccess (string message)
    {
        return new Result<T> {IsSuccess = true, Message = message};
    }
}


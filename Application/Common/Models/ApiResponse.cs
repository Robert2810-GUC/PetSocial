using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models;

public class ApiResponse<T>
{
    public bool Status { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }

    public ApiResponse(bool status, int statusCode, string message, T data)
    {
        Status = status;
        StatusCode = statusCode;
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> Success(T data, string message = "Success", int statusCode = 200) =>
        new ApiResponse<T>(true, statusCode, message, data);

    public static ApiResponse<T> Fail(string message, int statusCode = 400, T data = default) =>
        new ApiResponse<T>(false, statusCode, message, data);
}


using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = [];

        public static ServiceResult Ok() => new() { Success = true };
        public static ServiceResult Fail(string error) => new() { Success = false, ErrorMessage = error, Errors = [error] };
        public static ServiceResult Fail(List<string> errors) => new() { Success = false, ErrorMessage = errors.Count > 0 ? errors[0] : null, Errors = errors };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static new ServiceResult<T> Fail(string error) => new() { Success = false, ErrorMessage = error, Errors = [error] };
        public static new ServiceResult<T> Fail(List<string> errors) => new() { Success = false, ErrorMessage = errors.Count > 0 ? errors[0] : null, Errors = errors };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Basic
{
    public class PaginationResult<T> where T : class
    {
        public List<T> Data { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPage { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public DateTime? NextCursor { get; set; } 
        public bool HasMore { get; set; }
        public object Meta { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPage;
        public bool Succeeded { get; set; }
        public List<string> Messages { get; set; } = new();

        public PaginationResult()
        {
            Data = new List<T>();
        }

        internal PaginationResult(
            bool succeeded,
            List<T> data = null,
            int count = 0,
            int page = 1,
            int pageSize = 10,
            object meta = null)
        {
            Data = data ?? new List<T>();
            CurrentPage = page;
            PageSize = pageSize;
            TotalCount = count;
            TotalPage = (int)Math.Ceiling(count / (double)pageSize);
            Succeeded = succeeded;
            Meta = meta;
        }

        public static PaginationResult<T> Success(List<T> data, int count, int page, int pageSize, object meta = null)
        {
            return new PaginationResult<T>(true, data, count, page, pageSize, meta);
        }
    }
}
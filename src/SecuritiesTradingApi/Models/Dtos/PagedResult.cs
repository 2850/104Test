namespace SecuritiesTradingApi.Models.Dtos;

/// <summary>
/// 分頁響應結果
/// </summary>
/// <typeparam name="T">數據項目類型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 當前頁碼
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每頁大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 總記錄數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 是否有上一頁
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// 是否有下一頁
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// 數據項目列表
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// 創建分頁結果
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}

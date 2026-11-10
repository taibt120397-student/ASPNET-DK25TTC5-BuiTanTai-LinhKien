using LinhKien.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LinhKien.Helpers;

/// <summary>Các hàm dựng cây danh mục nhiều cấp từ danh sách phẳng.</summary>
public static class DanhMucTree
{
    /// <summary>Duyệt cây theo thứ tự hiển thị; bỏ qua nhánh của skipSubtreeOf (dùng khi chọn danh mục cha).</summary>
    public static List<(DanhMuc Node, int Level)> Flatten(IEnumerable<DanhMuc> all, int? skipSubtreeOf = null)
    {
        var byParent = all.ToLookup(d => d.DanhMucChaId);
        var result = new List<(DanhMuc Node, int Level)>();

        void Walk(int? parentId, int level)
        {
            foreach (var d in byParent[parentId].OrderBy(x => x.ThuTu).ThenBy(x => x.TenDanhMuc))
            {
                if (skipSubtreeOf.HasValue && d.Id == skipSubtreeOf.Value) continue;
                result.Add((d, level));
                Walk(d.Id, level + 1);
            }
        }

        Walk(null, 0);
        return result;
    }

    /// <summary>Id của danh mục gốc và toàn bộ danh mục con cháu.</summary>
    public static List<int> SubtreeIds(IEnumerable<DanhMuc> all, int rootId)
    {
        var byParent = all.ToLookup(d => d.DanhMucChaId);
        var ids = new List<int> { rootId };
        var stack = new Stack<int>();
        stack.Push(rootId);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            foreach (var c in byParent[cur])
            {
                ids.Add(c.Id);
                stack.Push(c.Id);
            }
        }
        return ids;
    }

    /// <summary>Danh sách thụt lề cho thẻ select.</summary>
    public static List<SelectListItem> Options(IEnumerable<DanhMuc> all, int? skipSubtreeOf = null) =>
        Flatten(all, skipSubtreeOf)
            .Select(x => new SelectListItem(
                string.Concat(Enumerable.Repeat("— ", x.Level)) + x.Node.TenDanhMuc,
                x.Node.Id.ToString()))
            .ToList();
}

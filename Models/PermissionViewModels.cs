namespace HeThongThiDQ.Models
{
    public class GroupQuyenViewModel
    {
        public int IDQuyen { get; set; }
        public string? TenQuyen { get; set; }
        public List<ListControllerViewModel> ListController { get; set; } = new();
    }

    public class ListControllerViewModel
    {
        public int ID { get; set; }
        public string? Controller { get; set; }
        public string? Mota { get; set; }
        public int? IsActive { get; set; }
        public string? DsquyenCn { get; set; }
        public string? DsTenQuyen { get; set; }
        public List<ItemCheckViewModel> LSChecked { get; set; } = new();
        public bool IsCheck { get; set; }
    }

    public class ItemCheckViewModel
    {
        public string? Name { get; set; }
        public int IdCN { get; set; }
        public bool IsChecked { get; set; }
    }

    public class AddUserQuyenViewModel
    {
        public int? IDKNL { get; set; }
        public string? NVDG { get; set; }
        public string[]? Selected { get; set; }
    }
}

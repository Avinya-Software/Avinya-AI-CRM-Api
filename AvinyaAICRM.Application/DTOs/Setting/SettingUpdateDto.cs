
namespace AvinyaAICRM.Application.DTOs.Setting
{
    public class SettingUpdateDto
    {
        public Guid SettingID { get; set; }
        public string? Value { get; set; }
        public string? PreFix { get; set; }
        public int? Digits { get; set; }
    }
}

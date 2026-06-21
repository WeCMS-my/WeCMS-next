using SqlSugar;

namespace WeCms.Modules.Organization.SqlSugar.Entities;

[SugarTable("sys_user_position")]
public sealed class UserPositionEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "user_id")]
    public long UserId { get; set; }

    [SugarColumn(IsPrimaryKey = true, ColumnName = "position_id")]
    public long PositionId { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; }
}

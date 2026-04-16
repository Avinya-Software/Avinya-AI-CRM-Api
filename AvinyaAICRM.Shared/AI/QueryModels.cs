using System;
using System.Collections.Generic;

namespace AvinyaAICRM.Shared.AI
{
    public enum QueryType
    {
        LIST,
        SUMMARY,
        DETAIL
    }

    public class QueryRequest
    {
        public List<string> Entities { get; set; } = new();
        public QueryType Type { get; set; } = QueryType.LIST;
        public Dictionary<string, object> RawFilters { get; set; } = new();
        public NormalizedFilters NormalizedFilters { get; set; } = new();
    }

    public class NormalizedFilters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? LeadId { get; set; }
        public Guid? ClientId { get; set; }
        public Guid? StatusId { get; set; }
        public string? Search { get; set; }
        public string? TaskFilterType { get; set; } // "MY", "CREATED", "TEAM"
        public bool IsMyData { get; set; }
    }

    public class FilterMap
    {
        public string FilterKey { get; set; }
        public string Table { get; set; }
        public string Column { get; set; }
    }

    public class EntityPolicy
    {
        public string TableName { get; set; }
        public string Alias { get; set; }
        public string TenantColumn { get; set; } = "TenantId";
        public string? UserColumn { get; set; } // AssignedTo, CreatedBy, FollowUpBy
        public string DefaultDateColumn { get; set; } = "CreatedDate";
        public Dictionary<string, string> DateColumnAliases { get; set; } = new();
        public bool HasIsDeleted { get; set; } = true;
        public string ModuleKey { get; set; }
        public List<string> SelectColumns { get; set; } = new();
    }

    public class Relationship
    {
        public string FromTable { get; set; }
        public string ToTable { get; set; }
        public string FromColumn { get; set; }
        public string ToColumn { get; set; }
        public int Weight { get; set; } = 1; // 1 = direct, higher = less preferred
    }
}

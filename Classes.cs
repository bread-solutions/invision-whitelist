using System.Collections.Generic;

namespace invision_whitelist
{
    internal class ApiResponse
    {
        public int page { get; set; }
        public int perPage { get; set; }
        public int totalResults { get; set; }
        public int totalPages { get; set; }
        public ApiUser[] results { get; set; }
    }
    internal class ApiUser
    {
        public int id { get; set; }
        public string name { get; set; }
        public ApiGroup primaryGroup { get; set; }
        public ApiGroup[] secondaryGroups { get; set; }
        public Dictionary<string, ApiCustomField> customFields { get; set; }

    }
    internal class ApiGroup
    {
        public int id { get; set; }
        public string name { get; set; }
        public string formattedName { get; set; }
    }
    internal class ApiCustomField
    {
        public string name { get; set; }
        public Dictionary<string, ApiField> fields { get; set; }
    }
    internal class ApiField
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    internal class WhitelistConfig
    {
        
        public string apiBaseURL { get; set; }
        public string apiToken { get; set; }
        public string[] allowedGroupIds { get; set; }

        public string profileFieldName { get; set; }

        public string profileFieldSubNode { get; set; }
    }
}

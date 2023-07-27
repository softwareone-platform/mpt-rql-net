using Microsoft.AspNetCore.Mvc;

namespace Rql.Sample.Api.Model
{
    public class GetRequest
    {
        [FromQuery(Name = "query")]
        public string? Query { get; set; }

        [FromQuery(Name = "order")]
        public string? Order { get; set; }
        
        [FromQuery(Name = "select")]
        public string? Select { get; set; }

        [FromQuery(Name = "offset")]
        public int Offset { get; set; } = 0;

        [FromQuery(Name = "limit")]
        public int Limit { get; set; } = 10;
    }
}

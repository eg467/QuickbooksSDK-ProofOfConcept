using Microsoft.AspNetCore.Mvc;
using QBFC16Lib;
using QbSync.QbXml.Objects;

namespace QuickbooksApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuickbooksController : ControllerBase
    {

        private readonly QbXmlQueriesFactory _queryFactory;
        private readonly ILogger<QuickbooksController> _logger;

        public QuickbooksController(ILogger<QuickbooksController> logger, QbXmlQueriesFactory queryFactory)
        {
            _logger = logger;
            _queryFactory = queryFactory;
        }

        [HttpPost("Customers", Name = "GetCustomers")]
        public CustomerRet[] GetCustomers([FromBody]CustomerQueryParameters? queryParams)
        {
            var queries = _queryFactory.CreateQueries();
            return queries.GetCustomers(queryParams??new());
        }
    }

    public class CustomerQueryParameters
    {
        public DateTime? ModifiedSince { get; set; }
        public int? MaxResults { get; set; }
        public bool? Active { get; set; }
        public List<string>? FullName { get; set; }
        public List<string>? ReturnedFields { get; set; }
    }

}
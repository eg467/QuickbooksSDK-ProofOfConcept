using Microsoft.AspNetCore.Mvc;
using QBFC15Lib;

namespace QuickbooksApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuickbooksController : ControllerBase
    {

        private readonly QuickbooksQuery _query;
        private readonly ILogger<QuickbooksController> _logger;

        public QuickbooksController(ILogger<QuickbooksController> logger, QuickbooksQuery query)
        {
            _logger = logger;
            _query = query;
        }

        [HttpGet("Customers", Name = "GetCustomers")]
        public List<Customer> GetCustomers()
        {
            var customerQuery = new CustomerQuery();
            _query.AddRequest(customerQuery).Execute();
            return customerQuery.Result;
        }

        [HttpGet("Invoices", Name = "GetInvoices")]
        public List<Invoice> GetInvoices()
        {
            var invoiceQuery = new InvoiceQuery();
            _query.AddRequest(invoiceQuery).Execute();
            return invoiceQuery.Result;
        }
    }

    class CustomerQuery : RequestMessageWithResult<List<Customer>>
    {

        public int MaxResults { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxResults">The maximum number of customers to return, or 0 for no limit.</param>
        public CustomerQuery(int maxResults = 0)
        {
            MaxResults = maxResults;
        }

        public override string Label => "Customer Query";

        public override List<Customer> ProcessResponseHelper(IQBBase responseDetails)
        {
            // Get the customer list from the response Detail property
            // (see OSR) and cast to the expected type.
            ICustomerRetList custList = (ICustomerRetList)responseDetails;

            var customers = new List<Customer>();
            for (int i = 0; i < custList.Count; i++)
            {
                ICustomerRet cust = custList.GetAt(i);
                Customer c = new()
                {
                    Name = ComStr(cust.Name),
                    BillingAddress = ComAddress(cust.BillAddressBlock),
                    ShippingAddress = ComAddress(cust.ShipAddressBlock),
                    Notes = ComStr(cust.Notes),
                    Modified = cust.TimeModified.GetValue()


                };
                customers.Add(c);
            }
            return customers;
        }


        public override void TransformRequestMessageSet(IMsgSetRequest requestMessageSet)
        {
            // Create the customer query and add to the request message set. The
            // append method adds the request to the set, and returns the new
            // request.
            ICustomerQuery customerQuery = requestMessageSet.AppendCustomerQueryRq();

            // Add a filter to the request that limits the number of items in
            // the response to 50.

            if(MaxResults > 0)
            {
                customerQuery.ORCustomerListQuery
                    .CustomerListFilter
                    .MaxReturned
                    .SetValue(MaxResults);
            }
        }
    }

    class InvoiceQuery : RequestMessageWithResult<List<Invoice>>
    {

        public int MaxResults { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxResults">The maximum number of customers to return, or 0 for no limit.</param>
        public InvoiceQuery(int maxResults = 0)
        {
            MaxResults = maxResults;
        }

        public override string Label => nameof(InvoiceQuery);

        public override List<Invoice> ProcessResponseHelper(IQBBase responseDetails)
        {
            var invList = (IInvoiceRetList)responseDetails;

            var invoices = new List<Invoice>();
            for (int i = 0; i < invList.Count; i++)
            {
                IInvoiceRet inv = invList.GetAt(i);
                var invoice = Invoice.FromCom(inv);
                invoices.Add(invoice);
            }
            return invoices;
        }


        public override void TransformRequestMessageSet(IMsgSetRequest requestMessageSet)
        {
            // Create the customer query and add to the request message set. The
            // append method adds the request to the set, and returns the new
            // request.
            IInvoiceQuery invoiceQuery = requestMessageSet.AppendInvoiceQueryRq();


            // Add a filter to the request that limits the number of items in
            // the response to 50.
            if (MaxResults > 0)
            {
                invoiceQuery.ORInvoiceQuery.InvoiceFilter.MaxReturned.SetValue(MaxResults);
            }
            //invoiceQuery.ORInvoiceQuery.InvoiceFilter.ORDateRangeFilter.ModifiedDateRangeFilter.FromModifiedDate.SetValue(DateTime.Now.Subtract(TimeSpan.FromDays(14)), true);
            //invoiceQuery.ORInvoiceQuery.InvoiceFilter.ORDateRangeFilter.ModifiedDateRangeFilter.ToModifiedDate.SetValue(DateTime.Now, false);
        }
    }

    public class Customer
    {
        public string Name { get; set; } = "";
        public string BillingAddress { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime Modified { get; set; }
    }

    public class Amount
    {
        private readonly IQBAmountType _amount;
        private Amount(IQBAmountType amount)
        {
            _amount = amount;
        }

        public static Amount? FromCom(IQBAmountType? amount) =>
            amount is not null ? new Amount(amount) : null;

        public double? Value => _amount.GetValue();
        public string? Min => _amount.GetMinValue();
        public string? Max => _amount.GetMinValue();
        public string? Str => _amount.GetAsString();
    }


    public class Invoice
    {
        private readonly IInvoiceRet _invoice;
        private Invoice(IInvoiceRet invoice)
        {
            _invoice = invoice;
        }

        public static Invoice? FromCom(IInvoiceRet? invoice) =>
            invoice is not null ? new Invoice(invoice) : null;

        public string? Other => _invoice?.Other?.GetValue();
        public Amount? AppliedAmount => Amount.FromCom(_invoice.AppliedAmount);
       


    }
}
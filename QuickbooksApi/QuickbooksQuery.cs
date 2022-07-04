using QBFC15Lib;

namespace QuickbooksApi
{
    public class QuickbooksQuery
    {
        public string AppId { get; }
        public string AppName { get; }
        public Version QbXmlVersion { get; }

        public QuickbooksQuery(string appId = "", string appName = "Quickbooks Web Api", short qbXmlMajorVersion = 15, short qbXmlMinorVersion = 0)
        {
            AppId = appId;
            AppName = appName;
            QbXmlVersion = new(qbXmlMajorVersion, qbXmlMinorVersion);
        }

        private readonly List<IRequestMessage> _requestMessages = new();

        public QuickbooksQuery AddRequest(IRequestMessage message)
        {
            _requestMessages.Add(message);
            return this;
        }

        public void Execute()
        {
            if(!_requestMessages.Any())
            {
                throw new InvalidOperationException($"Please add requests using {nameof(AddRequest)} before calling query.");
            }

            // Create a QBSessionManager
            QBSessionManager sessMgr = new QBSessionManager();

            // Put your code in a try-catch, as the session manager will throw an
            // exception if an error occurs while sending the request or opening a
            // connection to QuickBooks.
            try
            {
                // Connect to QuickBooks and open a new session using the open mode
                // currently in use by your local QuickBooks installation.
                sessMgr.OpenConnection(AppId, AppName);
                sessMgr.BeginSession("", ENOpenMode.omDontCare);

                // Create a request message set, which will contain the customer
                // query. The arguments specify the country (should match your
                // QuickBooks version) and qbXML version.
                IMsgSetRequest requestMessageSet = sessMgr.CreateMsgSetRequest("US", (short)QbXmlVersion.Major, (short)QbXmlVersion.Minor);

                _requestMessages.ForEach(r => r.TransformRequestMessageSet(requestMessageSet));

                // Execute all requests in the session manager’s request message
                // set. The response list contains the responses for each request
                // sent to QuickBooks, in the order they were sent.
                IMsgSetResponse resp = sessMgr.DoRequests(requestMessageSet);
                IResponseList respList = resp.ResponseList;

                if(respList.Count != _requestMessages.Count)
                {
                    throw new Exception(@$"Expected {_requestMessages.Count} response sets but received {respList.Count}.");
                }

                for(var i = 0; i < respList.Count; i++)
                {
                    IResponse curResp = respList.GetAt(i);
                    
                    if (curResp.StatusCode >= 0)
                    {
                        _requestMessages[i].ProcessResponse(curResp.Detail);
                    }
                }
            }

            // Catch any exceptions that occur and report them in the response.
            catch (Exception ex)
            {
                throw;
                // < Handle the exception here >
            }

            // Finally close connection & session no matter what happens.
            finally
            {
                sessMgr.EndSession();
                sessMgr.CloseConnection();
            }
        }
    }

    public interface IRequestMessage
    {
        public string Label { get; }
        public void TransformRequestMessageSet(IMsgSetRequest requestMessageSet);
        public void ProcessResponse(IQBBase responseDetails);
    }

    public abstract class RequestMessageWithResult<T> : IRequestMessage
    {

        private T? _result;
        public bool HasResult => _result != null;

        public T Result { 
            get
            { 
                if(!HasResult)
                {
                    throw new InvalidOperationException("The result has not been set yet.");
                }
                return _result!;
            }
            set
            {
                // WARNING: Some results might be null, breaking things.
                _result = value;
            }
        }
        public string? Error { get; private set; }

        public abstract string Label { get; }
        public abstract void TransformRequestMessageSet(IMsgSetRequest requestMessageSet);

   

        public void ProcessResponse(IQBBase responseDetails)
        {
            try
            {
                Result = ProcessResponseHelper(responseDetails);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                throw;
            }
        }

        public abstract T ProcessResponseHelper(IQBBase responseDetails);

        protected string ComStr(IQBStringType value) => value?.GetValue() ?? "";
        protected string ComAddress(IAddressBlock value)
        {
            if(value is null)
            {
                return "";
            }
            var lines = new[] { value.Addr1, value.Addr2, value.Addr3, value.Addr4, value.Addr5 };
            return string.Join("\r\n", lines.Select(ComStr));
        } 
    }

}

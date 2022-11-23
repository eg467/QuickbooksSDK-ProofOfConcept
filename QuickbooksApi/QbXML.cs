using QbSync.QbXml.Objects;
using QbSync.QbXml;
using QBXMLRP2Lib;
using System.Net.Sockets;
using QBFileMode = QBXMLRP2Lib.QBFileMode;
using System.Runtime.CompilerServices;
using QBFC16Lib;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using QuickbooksApi.Controllers;

namespace QuickbooksApi
{


    /// <summary>
    /// Creates a session for QB queries only if necessary.
    /// </summary>
    public sealed class QbXmlQueriesFactory: IDisposable
    {
        private IQbXMLConnection _connection;
        private QbXmlSession? _session;

        public QbXmlQueriesFactory(IQbXMLConnection connection)
        {
            _connection = connection;
        }

        public QbXmlQueries CreateQueries()
        {
            _session = _connection.GetSession();
            System.Diagnostics.Debug.WriteLine($"{_session.Ticket} : query created");
            var x = new QbXmlQueries(_session);
            return x;
        }

        public void Dispose()
        {
            if (_session != null)
            {
                _connection.ReturnSession(_session);
                _session = null;
            }
        }
    }
    public class QbXmlQueries
    {
        private readonly QbXmlSession _session;
        public QbXmlQueries(QbXmlSession session)
        {
            _session = session;
        }

        private TResponse MakeRequest<TResponse>(string requestXml) where TResponse: class
        {
            string? responseXml = _session.ProcessRequest(requestXml);

            if (string.IsNullOrEmpty(responseXml))
            {
                throw new InvalidOperationException("No response from QB.");
            }

            var response = new QbXmlResponse();
            var finalResponse = response.GetSingleItemFromResponse<TResponse>(responseXml);

            if(finalResponse == null)
            {
                throw new InvalidOperationException("Could not parse QB response.");
            }

            return finalResponse;
            
        }

        public TResponseType Query<TResponseType>(IQbRequest request) where TResponseType : class
        {
            var qbRequest = new QbXmlRequest();
            qbRequest.AddToSingle(request);
            string requestXml = qbRequest.GetRequest();
            return MakeRequest<TResponseType>(requestXml);
        }

        public CustomerRet[] GetCustomers(CustomerQueryParameters queryParameters)
        {
            var innerRequest = new CustomerQueryRqType();

            if (queryParameters.Active.HasValue)
            {
                innerRequest.ActiveStatus = queryParameters.Active.Value ? ActiveStatus.ActiveOnly : ActiveStatus.InactiveOnly;
            }

            if(queryParameters.FullName != null && queryParameters.FullName.Any())
            {
                innerRequest.FullName = queryParameters.FullName;
            }

            if (queryParameters.MaxResults.HasValue)
            {
                innerRequest.MaxReturned = queryParameters.MaxResults.ToString();
            }

            if(queryParameters.ModifiedSince.HasValue)
            {
                innerRequest.FromModifiedDate = new DATETIMETYPE(queryParameters.ModifiedSince.Value);
            }

            var response = Query<CustomerQueryRsType>(innerRequest);
            return response.CustomerRet;
        }

    }

    public interface IQbXMLConnection : IDisposable
    {
        void Connect();
        void Disconnect();

        QbXmlSession GetSession();

        void ReturnSession(QbXmlSession session);
    }

    public class QbXmlSession
    {
  
        public RequestProcessor2Class Processor { get; }

        public string Ticket { get; }

        public QbXmlSession(RequestProcessor2Class processor, string ticket)
        {
            Processor = processor;
            Ticket = ticket;
        }

        public override int GetHashCode() => Ticket.GetHashCode();
        public bool Equals(QbXmlSession? other) => other != null && Ticket == other.Ticket;

        /// <summary>
        /// Processes a single use request, after which the session will automatically close if necessary
        /// </summary>
        /// <param name="requestXml"></param>
        /// <returns></returns>
        public string ProcessRequest(string requestXml) =>
            this.Processor.ProcessRequest(Ticket, requestXml);
    }

    public class QbXmlProcessorConnectionWrapper
    {
        public const string AppID = "";
        public const string AppName = "QB XML Test";

        public static RequestProcessor2Class CreateOpenQbConnection()
        {
            var requestProcessor = new RequestProcessor2Class();
            requestProcessor.OpenConnection(AppID, AppName);
            return requestProcessor;
        }

        public static void CloseQbConnection(RequestProcessor2Class? processor)
        {
            processor?.CloseConnection();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processor"></param>
        /// <returns>The token of the created session</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string CreateQbSession(RequestProcessor2Class processor)
        {
            string ticket = processor.BeginSession("", QBFileMode.qbFileOpenDoNotCare);
            if (string.IsNullOrEmpty(ticket))
            {
                throw new InvalidOperationException("A QB session ticket was not created.");
            }
            return ticket;
        }

        public static void CloseQbSession(RequestProcessor2? processor, string? ticket)
        {
            if (processor == null || string.IsNullOrEmpty(ticket)) return;
            processor.EndSession(ticket);
        }
            
    }

    public enum SessionCreationMode
    {
        OneConnectionIndividualSessions,
        OneConnectionAndSession,
        IndividualConnectionsAndSessions
    }

    public static class SessionConnectionFactory
    {
        public static IQbXMLConnection Create(SessionCreationMode mode) => mode switch
        {
            SessionCreationMode.OneConnectionAndSession => new QbXMLSingleSessionConnection(),
            SessionCreationMode.OneConnectionIndividualSessions => new QbXMLMultiSessionSingleConnection(),
            SessionCreationMode.IndividualConnectionsAndSessions => new QbXMLPerRequestConnection(),
            _ => new QbXMLPerRequestConnection(),
        };
    }

    public class QbXMLSingleSessionConnection : IQbXMLConnection, IDisposable
    {
        private RequestProcessor2Class? _processor;
        private QbXmlSession? _session;

        public void Connect()
        {
            _processor = QbXmlProcessorConnectionWrapper.CreateOpenQbConnection();
            string ticket = QbXmlProcessorConnectionWrapper.CreateQbSession(_processor);
            _session = new(_processor, ticket);
        }

        public void Disconnect()
        {
            QbXmlProcessorConnectionWrapper.CloseQbSession(_processor, _session?.Ticket);
            _session = null;
            QbXmlProcessorConnectionWrapper.CloseQbConnection(_processor);
            _processor = null;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public QbXmlSession GetSession()
        {
            if(_processor == null || _session == null)
            {
                Connect();
            }
            return _session!;
        }

        public void ReturnSession(QbXmlSession session)
        {
        }
    }

    public class QbXMLMultiSessionSingleConnection : IQbXMLConnection, IDisposable
    {
        private RequestProcessor2Class? _processor;

        public void Connect()
        {
            _processor = QbXmlProcessorConnectionWrapper.CreateOpenQbConnection();
        }

        public void Disconnect()
        {
            QbXmlProcessorConnectionWrapper.CloseQbConnection(_processor);
            _processor = null;
        }

        public void Dispose()
        {
            // TODO: Track and disconnect open sessions
            Debug.WriteLine("Disposing connection");
            Disconnect();
        }

        public QbXmlSession GetSession()
        {
            if (_processor == null)
            {
                Connect();
            }
            string ticket = QbXmlProcessorConnectionWrapper.CreateQbSession(_processor!);
            return new(_processor!, ticket);
        }

        public void ReturnSession(QbXmlSession session)
        {
            QbXmlProcessorConnectionWrapper.CloseQbSession(session.Processor, session.Ticket);
        }
    }

    public class QbXMLPerRequestConnection : IQbXMLConnection
    {
        public void Connect()         {        }

        public void Disconnect()        {        }

        public void Dispose()
        {
        }

        public QbXmlSession GetSession()
        {
            var processor = QbXmlProcessorConnectionWrapper.CreateOpenQbConnection();
            string ticket = QbXmlProcessorConnectionWrapper.CreateQbSession(processor!);
            return new(processor!, ticket);
        }

        public void ReturnSession(QbXmlSession session)
        {
            QbXmlProcessorConnectionWrapper.CloseQbSession(session.Processor, session.Ticket);
            QbXmlProcessorConnectionWrapper.CloseQbConnection(session.Processor);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;

namespace MoreService
{
    class RouteHandler : IRouteHandler
    {
        public System.Web.IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            var service = requestContext.RouteData.Values.ContainsKey("service")
                ? requestContext.RouteData.Values["service"].ToString()
                : string.Empty;
            var action = requestContext.RouteData.Values.ContainsKey("action")
                ? requestContext.RouteData.Values["action"].ToString()
                : string.Empty;

            return new Handler()
            {
                Service = service,
                Action = action
            };
        }
    }

    class Handler : IHttpHandler
    {
        public string Service { get; set; }
        public string Action { get; set; }

        public RequestContext RequestContext { get; set; }

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            Console.WriteLine("处理GetSuperior Mock 9D23EAA9-6145-4635-A7C2-D8AEEDF45C1E");
            context.Response.Write("9D23EAA9-6145-4635-A7C2-D8AEEDF45C1E");
        }

        #endregion
    }
}

/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Web;
using CodeSharp.Framework;
using CodeSharp.Framework.Castles;
using CodeSharp.ServiceFramework.Castles;

namespace Taobao.Workflow.Activities.AdminWeb
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            Castle.Windsor.IWindsorContainer c = null;

            //配置框架初始化
            SystemConfig.ConfigFilesAssemblyName = "Taobao.Workflow.Activities.AdminWeb";
            SystemConfig.Configure("TFlowEngineAdminWeb")
                .Castle()
                .Resolve(o =>
                {
                    c = o.Container;
                    o.Container.ControllerFactory();
                    o.Container.RegisterControllers(Assembly.GetExecutingAssembly());
                });

           //NSF初始化
            CodeSharp.ServiceFramework.Configuration
                .Configure()
                .Castle(c)
                .Log4Net(false)
                .Associate(new Uri("tcp://localhost:8000/remote.rem"))
                .Endpoint()
                .Run();
        }
    }
}
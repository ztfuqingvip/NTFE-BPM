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
using System.Web;
using System.Web.Mvc;
using Taobao.Workflow.Activities.AdminWeb.Models;
using Taobao.Workflow.Activities.Management;
using Taobao.Workflow.Activities.Converters;

namespace Taobao.Workflow.Activities.AdminWeb.Controllers
{
    public class HttpController : Controller
    {
        /// <summary>
        /// 流程服务请求
        /// </summary>
        /// <param name="method">方法名</param>
        /// <returns></returns>
        public ActionResult WorkFlow(string method)
        {
            var engine = CodeSharp.Core.Services.DependencyResolver.Resolve<ITFlowEngine>();
            var converterService = CodeSharp.Core.Services.DependencyResolver.Resolve<IConverterService>();
            
            switch (method)
            {
                case "GetProcessTypes": 
                    return this.Json(engine.GetProcessTypes());
                case "GetWorkflowDefinition":
                    var name = this.Request["name"];
                    return this.Json(engine.GetWorkflowDefinition(name));
                case "ParseWorkflowDefinition":
                    var workflowDefinition = HttpUtility.UrlDecode(this.Request["workflowDefinition"]);
                    var customSettingsDefinition = HttpUtility.UrlDecode(this.Request["customSettingsDefinition"]);
                    return this.Json(converterService.ParseWorkflowDefinition(workflowDefinition, customSettingsDefinition));
                case "CreateProcessType":
                    var name1 = this.Request["name"];
                    var workflowDefinition1 = HttpUtility.UrlDecode(this.Request["workflowDefinition"]);
                    var customSettingsDefinition1 = HttpUtility.UrlDecode(this.Request["customSettingsDefinition"]);
                    var description = HttpUtility.UrlDecode(this.Request["description"]);
                    var groupName = HttpUtility.UrlDecode(this.Request["group"]);
                    engine.CreateProcessType(name1
                        , workflowDefinition1
                        , customSettingsDefinition1
                        , description, groupName);
                    return Json("{}");
                case "GetProcessTypeByProcessId":
                    var processId = new Guid(this.Request["id"]);
                    return Json(converterService.GetProcessTypeByProcessId(processId));
                default: throw new InvalidOperationException("method找不到");
            }
        }
        /// <summary>
        /// 本地Http请求
        /// </summary>
        /// <param name="method">方法名</param>
        /// <returns></returns>
        public ActionResult Local(string method)
        {
            switch (method)
            {
                case "GetServiceConfig":
                    return this.Content(System.IO.File.ReadAllText(System.Web.HttpContext.Current.Server.MapPath("~/Configs/service.xml")));
                default: throw new InvalidOperationException("method找不到");
            }
        }
    }
}

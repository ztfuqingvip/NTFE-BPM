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
using System.Web;
using System.Web.Mvc;
using Taobao.Workflow.Activities.Management;

namespace Taobao.Workflow.Activities.AdminWeb.Controllers
{
    public class ProcessController : BaseController
    {
        private ITFlowEngine _managementApi;
        private Taobao.Workflow.Activities.Client.ITFlowEngine _clientApi;
        private string _MiddleWebWhiteList;

        public ProcessController(ITFlowEngine managementApi
            , Taobao.Workflow.Activities.Client.ITFlowEngine clientApi
            , string MiddleWebWhiteList)
        {
            this._managementApi = managementApi;
            this._clientApi = clientApi;
            this._MiddleWebWhiteList = MiddleWebWhiteList;
        }

        public ActionResult Index(string type, string processTitle, string originator, string status, string p)
        {
            ViewBag.ProcessTypes = this._managementApi.GetProcessTypes();
            if (string.IsNullOrEmpty(type)
                && string.IsNullOrEmpty(processTitle)
                && string.IsNullOrEmpty(originator)
                && string.IsNullOrEmpty(status)
                && string.IsNullOrEmpty(p))
                return View();

            var statuses =  string.IsNullOrEmpty(status) 
                ? new Client.ProcessStatus[]{} 
                    : new Client.ProcessStatus[] { (Client.ProcessStatus)Enum.Parse(typeof(Client.ProcessStatus), status) };
            var query = new ProcessQuery()
            {
                ProcessTypeName = type,
                Originator = originator,
                Title = processTitle,
                Status = statuses
            };
            var result = this._managementApi.SearchProcesses(query, Convert.ToInt32(p), 20);
            ViewBag.Total = result.Total;
            ViewBag.Processes = result.Processes;
  
            ViewBag.Type = type;
            ViewBag.ProcessTitle = processTitle;
            ViewBag.Originator = originator;
            ViewBag.Status = status;

            return View();
        }

        [HttpPost]
        public ActionResult Index(string type, string processTitle, string originator, string status)
        {
            return Redirect(Url.Content(string.Format("~/Process/?p=1&type={0}&processTitle={1}&originator={2}&status={3}"
                , type
                , processTitle
                , originator
                , status)));
        }

        /// <summary>
        /// 停止流程
        /// </summary>
        [HttpPost]
        public ActionResult StopProcess(Guid processId)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.StopProcess(processId);
            return Json("", JsonRequestBehavior.AllowGet);
        }
        
        /// <summary>
        /// 继续流程
        /// </summary>
        [HttpPost]
        public ActionResult RestartProcess(Guid processId)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.RestartProcess(processId);
            return Json("", JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 删除流程
        /// </summary>
        [HttpPost]
        public ActionResult DeleteProcess(Guid processId)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.DeleteProcess(processId);
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Goto(Guid processId)
        {
            var process = this._clientApi.GetProcess(processId);
            ViewBag.Process = process;
            return View();
        }

        /// <summary>
        /// 跳转节点
        /// </summary>
        [HttpPost]
        public ActionResult Goto(Guid processId, string activityName)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.RedirectProcess(processId, activityName);
            return this.RedirectToAction("Index");
        }

        public ActionResult Error(string p)
        {
            var query = new ProcessQuery();
            query.Status = new Client.ProcessStatus[] { Client.ProcessStatus.Error };
            var result = this._managementApi.SearchProcesses(query, Convert.ToInt32(p), 20);
            ViewBag.Total = result.Total;
            ViewBag.Processes = result.Processes;

            return View();
        }

        /// <summary>
        /// 重试
        /// </summary>
        [HttpPost]
        public ActionResult RetryProcess(Guid processId)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.RetryFaultProcess(processId);
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Change(Guid processId)
        {
            var process = this._clientApi.GetProcess(processId);
            var processTypes = this._managementApi.GetAllVersionsOfProcessType(process.ProcessType.Name);
            ViewBag.Process = process;
            ViewBag.ProcessTypes = processTypes;
            return View();
        }

        /// <summary>
        /// 切换版本
        /// </summary>
        [HttpPost]
        public ActionResult Change(Guid processId, string version)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.ChangeProcessType(processId, version);
            return this.RedirectToAction("Error");
        }
    }

    public static partial class EnumHelper
    {
        public static string ToProcessStatusName(string status)
        {
            Client.ProcessStatus processStatus = (Client.ProcessStatus)Enum.Parse(typeof(Client.ProcessStatus), status);
            switch (processStatus)
            {
                case Client.ProcessStatus.New: return "新建";
                case Client.ProcessStatus.Error: return "发生错误";
                case Client.ProcessStatus.Running: return "运行中";
                case Client.ProcessStatus.Active: return "活动";
                case Client.ProcessStatus.Completed: return "完成";
                case Client.ProcessStatus.Stopped: return "被停止";
                case Client.ProcessStatus.Deleted: return "被删除";
                default: return "";
            }
        }
    }
}

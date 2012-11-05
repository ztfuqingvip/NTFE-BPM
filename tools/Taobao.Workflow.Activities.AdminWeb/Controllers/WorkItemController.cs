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
    public class WorkItemController : BaseController
    {
        private ITFlowEngine _managementApi;
        private Taobao.Workflow.Activities.Client.ITFlowEngine _clientApi;
        private string _MiddleWebWhiteList;

        public WorkItemController(ITFlowEngine managementApi
            , Taobao.Workflow.Activities.Client.ITFlowEngine clientApi
            , string MiddleWebWhiteList)
        {
            this._managementApi = managementApi;
            this._clientApi = clientApi;
            this._MiddleWebWhiteList = MiddleWebWhiteList;
        }

        public ActionResult Index(string type, string processTitle, string actioner, string activityName, string status, string p)
        {
            ViewBag.ProcessTypes = this._managementApi.GetProcessTypes();
            if (string.IsNullOrEmpty(type)
                && string.IsNullOrEmpty(processTitle)
                && string.IsNullOrEmpty(actioner)
                && string.IsNullOrEmpty(activityName)
                && string.IsNullOrEmpty(status)
                && string.IsNullOrEmpty(p))
                return View();

            var statuses = string.IsNullOrEmpty(status)
                ? new Client.WorkItemStatus[] { }
                    : new Client.WorkItemStatus[] { (Client.WorkItemStatus)Enum.Parse(typeof(Client.WorkItemStatus), status) };
            var query = new WorkItemQuery()
            {
                ProcessTypeName = type,
                ProcessTitle = processTitle,
                Actioner = actioner,
                ActivityName = activityName,
                Status = statuses
            };
            var result = this._managementApi.SearchWorkItems(query, Convert.ToInt32(p), 20);
            ViewBag.Total = result.Total;
            ViewBag.WorkItems = result.WorkItems;

            ViewBag.Type = type;
            ViewBag.ProcessTitle = processTitle;
            ViewBag.Actioner = actioner;
            ViewBag.ActivityName = activityName;
            ViewBag.Status = status;

            return View();
        }

        [HttpPost]
        public ActionResult Index(string type, string processTitle, string actioner, string activityName, string status)
        {
            return Redirect(Url.Content(string.Format("~/WorkItem/?p=1&type={0}&processTitle={1}&actioner={2}&activityName={3}&status={4}"
                , type
                , processTitle
                , actioner
                , activityName
                , status)));
        }

        /// <summary>
        /// 释放工作任务
        /// </summary>
        [HttpPost]
        public ActionResult ReleaseWorkItem(long workItemId)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.ReleaseWorkItem(workItemId);
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Redirect(long workItemId, string fromUser)
        {
            ViewBag.WorkItemId = workItemId;
            ViewBag.FromUser = fromUser;
            return this.View();
        }

        /// <summary>
        /// 转交工作任务
        /// </summary>
        [HttpPost]
        public ActionResult Redirect(long workItemId, string fromUser, string toUser)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.RedirectWorkItem(workItemId, fromUser, toUser);
            return this.RedirectToAction("Index");
        }
    }

    public static partial class EnumHelper
    {
        public static string ToWorkItemStatusName(string status)
        {
            Client.WorkItemStatus workItemStatus = (Client.WorkItemStatus)Enum.Parse(typeof(Client.WorkItemStatus), status);
            switch (workItemStatus)
            {
                case Client.WorkItemStatus.New: return "新建";
                case Client.WorkItemStatus.Open: return "打开";
                default: return "";
            }
        }
    }
}

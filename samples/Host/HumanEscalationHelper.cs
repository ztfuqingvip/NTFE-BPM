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
using Taobao.Workflow.Activities.Hosting;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities;
using Castle.Services.Transaction;

namespace Host
{
    /// <summary>
    /// 用于实现超时升级规则与外围的集成部分，这是一个范例实现，可按需扩充
    /// </summary>
    [CodeSharp.Core.Component]
    [Transactional]
    public class HumanEscalationHelper : HumanEscalationWaitingResumption.DefaultHumanEscalationHelper, HumanEscalationWaitingResumption.IHumanEscalationHelper
    {
        //private IEngineIntegrationService _integrationService;
        public HumanEscalationHelper(ILoggerFactory factory
            , IWorkItemService workItemService
            , IUserService userService
            , ISchedulerService schedulerService
            , ProcessService processService)
            //, IEngineIntegrationService integrationService)
            : base(factory
            , workItemService
            , userService
            , schedulerService
            , processService)
        {
            //this._integrationService = integrationService;
        }

        public override void Notify(Process process, IEnumerable<WorkItem> workItems, string templateName)
        {
            //超时通知
            //foreach (var w in workItems)
            //    using (new CodeSharp.ServiceFramework.Async.ServiceAsync())
            //        this._integrationService.NotifyHumanEscalation(templateName
            //            , process.ID
            //            , w.ID.ToString()
            //            , w.Actioner.UserName);
        }
        [Transaction(TransactionMode.Requires)]
        public override void Redirect(Process process, string activityName, IEnumerable<WorkItem> workItems, string toUserName)
        {
            base.Redirect(process, activityName, workItems, toUserName);
            if (workItems.Count() == 0) return;
            //在流程服务层增加备注
            //this._integrationService.CreateCommentForHumanEscalationRedirect(process.ID, activityName);
        }
        [Transaction(TransactionMode.Requires)]
        public override void Goto(Process process, string from, string to)
        {
            base.Goto(process, from, to);
            //在流程服务层增加备注
            //this._integrationService.CreateCommentForHumanEscalationGoto(process.ID, from, to);
        }
    }
}
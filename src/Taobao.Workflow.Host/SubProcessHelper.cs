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
using Castle.Services.Transaction;
using Taobao.Workflow.Activities;
using Taobao.Workflow.Activities.Hosting;
using Taobao.Workflow.Client.Integrated;

namespace Taobao.Workflow.Host
{
    /// <summary>
    /// 用于实现子流程与外围的集成
    /// </summary>
    [CodeSharp.Core.Component]
    [Transactional]
    public class SubProcessHelper : SubProcessCreateWaitingResumption.DefaultSubProcessHelper, SubProcessCreateWaitingResumption.ISubProcessHelper
    {
        private IEngineIntegrationService _integrationService;
        public SubProcessHelper(IProcessService processService
            , IProcessTypeService processTypeService
            , IUserService userService
            , string ntfeSystemUserName
            , IEngineIntegrationService integrationService)
            : base(processService
            , processTypeService
            , userService
            , ntfeSystemUserName)
        {
            this._integrationService = integrationService;
        }

        [Transaction(TransactionMode.Requires)]
        public override Process Create(Process parent, string subProcessTypeName, Guid assignedSubProcessId)
        {
            var subProcess = base.Create(parent, subProcessTypeName, assignedSubProcessId);
            //HACK:若与外围集成异常会出现Unexpected row count: 0; expected: 1 问题，应注意避免
            //同时在流程服务中创建一条流程数据记录
            this._integrationService.JustCreateSubFlowData("ntfe"
                , subProcess.ID
                , subProcess.Originator.UserName
                , subProcess.ProcessType.Name
                , subProcess.Title
                , new Client.FlowDataFields(subProcess.GetDataFields())
                , parent.ID);
            return subProcess;
        }
    }
}
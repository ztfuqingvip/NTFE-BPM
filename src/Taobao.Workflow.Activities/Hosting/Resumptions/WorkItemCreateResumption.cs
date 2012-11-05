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
using System.Text;
using Taobao.Activities.Hosting;
using Castle.Services.Transaction;
using CodeSharp.Core.Services;
using CodeSharp.Core;
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 任务待创建请求记录
    /// <remarks>从HumanActivityInstance生成实际WorkItem</remarks>
    /// </summary>
    public class WorkItemCreateResumption : WaitingResumption
    {
        /// <summary>
        /// 获取人工任务创建信息
        /// </summary>
        public virtual HumanActivityInstance HumanActivityInstance { get; private set; }

        protected WorkItemCreateResumption() : base() { }
        /// <summary>
        /// 初始化任务待创建请求记录
        /// </summary>
        /// <param name="process"></param>
        /// <param name="humanActivityInstance"></param>
        public WorkItemCreateResumption(Process process, HumanActivityInstance humanActivityInstance)
            : base(process, WaitingResumption.MaxPriority)
        {
            this.Init(humanActivityInstance);
        }

        public override long? FlowNodeIndex
        {
            get
            {
                return this.HumanActivityInstance != null
                    ? this.HumanActivityInstance.FlowNodeIndex
                    : _emptyLong;
            }
        }
        public override bool EnableActiveAfterExecuted
        {
            get
            {
                return true;
            }
        }
        public override Type Handle
        {
            get { return typeof(WorkItemCreateWaitingResumption); }
        }

        private void Init(HumanActivityInstance humanActivityInstance)
        {
            this.HumanActivityInstance = humanActivityInstance;

            if (this.HumanActivityInstance == null)
                throw new InvalidOperationException("HumanActivityInstance不能为空");
        }
    }
    /// <summary>
    /// 任务待创建请求处理
    /// </summary>
    [CodeSharp.Core.Component]
    [Transactional]
    public class WorkItemCreateWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private IUserService _userService;
        private IProcessService _processService;
        private IWorkItemService _workItemService;
        private ISchedulerService _resumptionService;

        public WorkItemCreateWaitingResumption(ILoggerFactory factory
            , IUserService userService
            , IProcessService processService
            , IWorkItemService workItemService
            , ISchedulerService resumptionService)
        {
            this._log = factory.Create(typeof(WorkItemCreateWaitingResumption));
            this._userService = userService;
            this._processService = processService;
            this._workItemService = workItemService;
            this._resumptionService = resumptionService;
        }

        #region IWaitingResumption Members
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            var r = waitingResumption as WorkItemCreateResumption;
            var human = r.HumanActivityInstance;

            if (human == null)
            {
                this._log.Warn("不存在待创建任务的人工节点信息");
                return;
            }

            var process = r.Process;
            var setting = process.ProcessType.GetHumanSetting(human.ActivityName);

            if (setting == null)
                throw new InvalidOperationException("没有找到节点"
                    + human.ActivityName
                    + "对应的HumanSetting，可能需要修正流程");

            //OneAtOnce依次创建
            if (setting.SlotMode == HumanSetting.SlotDistributionMode.OneAtOnce)
                this._workItemService.Create(human.CreateNextWorkItem(process, this._userService));
            else
            {
                //AllAtOnce一次性创建
                var list = human.CreateAllWorkItem(process, this._userService);
                foreach (var w in list)
                    this._workItemService.Create(w);
            }
            //更新节点实例
            this._processService.UpdateActivityInstance(human);
        }
        #endregion
    }
}
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
using CodeSharp.Core;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 子流程运行完成调度请求
    /// </summary>
    public class SubProcessCompleteResumption : WaitingResumption
    {
        /// <summary>
        /// 获取子流程实例
        /// </summary>
        public virtual Process SubProcess { get; private set; }

        protected SubProcessCompleteResumption() : base() { }
        public SubProcessCompleteResumption(Process process, Process subProcess)
            : base(process, process.Priority)
        {
            this.SubProcess = subProcess;
            this.Validate();
        }
        public override Type Handle
        {
            get { return typeof(SubProcessCompleteWaitingResumption); }
        }

        private void Validate()
        {
            if (this.SubProcess == null)
                throw new InvalidOperationException("SubProcess不能为空");
            if (this.SubProcess.Status != ProcessStatus.Completed)
                throw new InvalidOperationException("SubProcess应为完成状态");
            if (this.SubProcess.ParentProcessId != this.Process.ID)
                throw new InvalidOperationException("SubProcess与Process不是父子流程关系");
        }
    }
    [CodeSharp.Core.Component]
    [Transactional]
    public class SubProcessCompleteWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private IWorkflowParser _parser;
        private IProcessService _processService;
        private ISchedulerService _resumption;
        public SubProcessCompleteWaitingResumption(ILoggerFactory factory
             , IWorkflowParser parser
             , IProcessService processService
             , ISchedulerService resumption)
        {
            this._log = factory.Create(typeof(SubProcessCompleteWaitingResumption));
            this._parser = parser;
            this._processService = processService;
            this._resumption = resumption;
        }

        #region IWaitingResumption Members
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            var r = waitingResumption as SubProcessCompleteResumption;
            var subProcess = r.SubProcess;
            var parentProcess = r.Process;

            //尝试从父流程中获取启动该子流程的节点实例信息
            SubProcessActivityInstance sub = this._processService.GetSubProcessActivityInstances(parentProcess, subProcess);
            if (sub == null)
                throw new InvalidOperationException(string.Format("没有在父流程“{0}”#{1}中找到启动子流程“{2}”#{3}的子流程节点实例"
                    , parentProcess.Title
                    , parentProcess.ID
                    , subProcess.Title
                    , subProcess.ID));
            //将子流程的流程变量传递给父流程
            var dict = subProcess.GetDataFields();
            foreach (string key in dict.Keys)
                parentProcess.UpdateDataField(key, dict[key]);
            //唤醒父流程
            var persisted = this._processService.GetWorkflowInstance(parentProcess).WorkflowInstance;
            var instance = WorkflowBuilder.CreateInstance(parentProcess, this._parser);
            instance.Load(persisted);
            instance.Update(parentProcess.GetInputs());
            instance.ResumeBookmark(sub.ReferredBookmarkName, null);
            //目前不允许在调度项中生成新的非延迟调度项，无法保证预期顺序，需要有类似核心的quack机制
            ////创建父流程唤醒调度项
            //this._resumption.Add(new BookmarkResumption(parentProcess
            //    , sub.ActivityName
            //    , sub.ReferredBookmarkName
            //    , string.Empty));

            //将子流程节点置为完成
            sub.SetAsComplete();
            this._processService.UpdateActivityInstance(sub);
        }
        #endregion
    }
}
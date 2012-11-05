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
using Castle.Services.Transaction;
using CodeSharp.Core;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 节点实例取消请求
    /// </summary>
    public class ActivityInstanceCancelResumption : WaitingResumption
    {
        /// <summary>
        /// 获取要取消的节点实例
        /// </summary>
        public virtual ActivityInstanceBase ActivityInstance { get; private set; }

        protected ActivityInstanceCancelResumption() : base() { }
        public ActivityInstanceCancelResumption(Process process, ActivityInstanceBase activityInstance)
            : base(process, process.Priority)
        {
            this.ActivityInstance = activityInstance;

            this.Validate();
        }

        public override Type Handle
        {
            get { return typeof(ActivityInstanceCancelWaitingResumption); }
        }
        //由于cancel时有可能受调度影响，需要允许自动retry
        public override bool EnableAutoRetry
        {
            get
            {
                return true;
            }
        }
        private void Validate()
        {
            if (this.ActivityInstance == null)
                throw new InvalidOperationException("ActivityInstance不能为空");
            if (this.ActivityInstance.ProcessId != this.Process.ID)
                throw new InvalidOperationException("ActivityInstance所在的流程实例与Process不一致");
        }
    }
    [CodeSharp.Core.Component]
    [Transactional]
    public class ActivityInstanceCancelWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private IProcessService _processService;
        public ActivityInstanceCancelWaitingResumption(ILoggerFactory factory, IProcessService processService)
        {
            this._log = factory.Create(typeof(ActivityInstanceCancelWaitingResumption));
            this._processService = processService;
        }

        #region IWaitingResumptionHandle Members
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            var r = waitingResumption as ActivityInstanceCancelResumption;

            try
            {
                //由于该调度项是允许自动重试的，应避免无关异常产生影响调度队列
                if (r != null
                    && r.Process != null
                    && r.ActivityInstance != null)
                    this._processService.CancelAllAbout(r.Process, r.ActivityInstance);
            }
            catch (Exception e)
            {
                this._log.Warn("取消流程节点实例时异常，将自动重试", e);
                throw e;
            }
        }
        #endregion
    }
}
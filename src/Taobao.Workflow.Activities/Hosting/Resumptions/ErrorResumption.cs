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

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 错误待恢复/重试记录
    /// </summary>
    public class ErrorResumption : WaitingResumption
    {
        /// <summary>
        /// 获取发生错误的节点索引
        /// </summary>
        public virtual int ErrorNodeIndex { get; private set; }

        protected ErrorResumption() : base() { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="process"></param>
        /// <param name="bookmarkName"></param>
        public ErrorResumption(Process process, int errorNodeIndex)
            : base(process, WaitingResumption.MaxPriority)
        {
            this.ErrorNodeIndex = errorNodeIndex;
        }

        public override Type Handle
        {
            get { return typeof(ErrorWaitingResumption); }
        }
    }

    [CodeSharp.Core.Component]
    [Transactional]
    public class ErrorWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private IWorkflowParser _parser;
        public ErrorWaitingResumption(ILoggerFactory factory, IWorkflowParser parser)
        {
            this._log = factory.Create(typeof(ErrorWaitingResumption));
            this._parser = parser;
        }

        #region IWaitingResumption Members
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            try
            {
                var r = waitingResumption as ErrorResumption;
                //错误恢复使用粗粒度实现，不延续原有工作流实例树
                var instance = WorkflowBuilder.CreateInstance(r.Process, this._parser);
                instance.Update(r.Process.GetInputs());
                instance.Run();

                this._log.InfoFormat("尝试对流程{0}#{1}在FlowNodeIndex={2}处进行了错误恢复"
                    , r.Process.Title
                    , r.Process.ID
                    , r.ErrorNodeIndex);
            }
            catch (Exception e)
            {
                //由于ErrorResumption本身就是错误恢复调度，即使异常也不应抛出而导致其本身被记录成error
                this._log.Error("错误重试时异常", e);
            }
        }
        #endregion
    }
}
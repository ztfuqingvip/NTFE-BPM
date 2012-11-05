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

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 用于记录流程运行中产生的子流程节点信息
    /// <remarks>同时用于描述子流程节点实例</remarks>
    /// </summary>
    public class SubProcessActivityInstance : ActivityInstanceBase
    {
        /// <summary>
        /// 获取关联的书签名
        /// </summary>
        public virtual string ReferredBookmarkName { get; private set; }
        /// <summary>
        /// 获取在该节点启动的子流程实例标识
        /// </summary>
        public virtual Guid? SubProcessId { get; private set; }

        protected SubProcessActivityInstance() : base() { }
        public SubProcessActivityInstance(Guid processId
            , int flowNodeIndex
            , long workflowActivityInstanceId
            , string activityName
            , string bookmarkName)
            : base(processId
            , flowNodeIndex
            , workflowActivityInstanceId
            , activityName)
        {
            this.ReferredBookmarkName = bookmarkName;
            this.Validate();
        }
        public SubProcessActivityInstance(Guid processId
            , int flowNodeIndex
            , long workflowActivityInstanceId
            , string activityName
            , string bookmarkName
            , Guid subProcessId)
            : this(processId
            , flowNodeIndex
            , workflowActivityInstanceId
            , activityName
            , bookmarkName)
        {
            this.SetSubProcessId(subProcessId);
        }

        protected internal virtual void SetSubProcessId(Guid subProcessId)
        {
            this.SubProcessId = subProcessId;

            if (this.SubProcessId == Guid.Empty)
                throw new InvalidOperationException("SubProcessId不能为空");
            if (this.SubProcessId == this.ProcessId)
                throw new InvalidOperationException("SubProcessId不能与ProcessId相同");
        }
        private void Validate()
        {
            if (string.IsNullOrEmpty(this.ReferredBookmarkName))
                throw new InvalidOperationException("ReferredBookmarkName不能为空");
        }
    }
}
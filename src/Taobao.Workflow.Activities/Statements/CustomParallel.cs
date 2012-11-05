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
using Taobao.Activities.Statements;
using Taobao.Activities;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 自定义并行节点/活动
    /// </summary>
    public class CustomParallel : Parallel, Flowchart.IMetadataKnowable
    {
        private static readonly int _defaultIndex = WorkflowBuilder.Default_FlowNodeIndex;
        /// <summary>
        /// 获取或设置FlowNodeIndex被设置时的回调
        /// </summary>
        public Action<int> OnFlowNodeIndex { get; set; }
        /// <summary>
        /// 获取所在flowchart中的索引
        /// </summary>
        protected int FlowNodeIndex { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public CustomParallel()
            : base()
        {
            this.FlowNodeIndex = _defaultIndex;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="flowNodeIndex">对应的索引</param>
        public CustomParallel(int flowNodeIndex)
            : base()
        {
            this.FlowNodeIndex = flowNodeIndex;
        }

        protected override void Execute(NativeActivityContext context)
        {
            //HACK:节点执行时首先设置当前节点索引
            context.GetExtension<DataFieldExtension>().SetCurrentNode(this.FlowNodeIndex);
            base.Execute(context);
        }
        protected override void OnHasCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            base.OnHasCompleted(context, completedInstance);

            //HACK:重写并行节点满足条件退出逻辑
            var e = context.GetExtension<ParallelExtension>();
            e.Cancelled(context.GetChildren()
                .Where(o => o.ID != completedInstance.ID)
                .Select(o => o.ID)
                .ToArray());
        }

        #region IMetadataKnowable Members

        void Flowchart.IMetadataKnowable.TellFlowNodeIndex(int index)
        {
            //未设置时
            if (this.FlowNodeIndex == _defaultIndex)
            {
                this.FlowNodeIndex = index;

                //内嵌节点需要单独触发设置
                this.Branches.ToList().ForEach(o =>
                {
                    if (o is Flowchart.IMetadataKnowable)
                        (o as Flowchart.IMetadataKnowable).TellFlowNodeIndex(this.FlowNodeIndex);
                });
                if (this.OnFlowNodeIndex != null)
                    this.OnFlowNodeIndex(this.FlowNodeIndex);
            }
            else if (this.FlowNodeIndex != index)
                throw new InvalidOperationException(string.Format(
                    "索引设置发生不一致，当前值={0}，实际运行时应={1}"
                    , this.FlowNodeIndex
                    , index));
        }

        #endregion
    }
}
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
using Taobao.Activities;
using System.Collections.ObjectModel;
using Taobao.Activities.Statements;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 用于描述NTFE-BPM流程活动树
    /// </summary>
    public class WorkflowActivity : NativeActivity
    {
        /// <summary>
        /// 当前节点
        /// <remarks>此属性名称决定了WorkflowBuilder.Variable_CurrentNode内置变量名</remarks>
        /// </summary>
        public Variable<int> CurrentNode { get; private set; }
        /// <summary>
        /// 流程图
        /// </summary>
        public Flowchart Body { get; private set; }
        /// <summary>
        /// 自定义节点的执行结果暂存变量
        /// </summary>
        public Variable<string> CustomActivityResult { get; private set; }
        /// <summary>
        /// 获取变量集合
        /// </summary>
        public Collection<Variable> Variables { get; private set; }

        /// <summary>
        /// 初始化用于描述NTFE-BPM流程活动树
        /// </summary>
        public WorkflowActivity()
        {
            this.CurrentNode = new Variable<int>();
            this.CustomActivityResult = new Variable<string>();
            this.Variables = new Collection<Variable>();

            this.Body = new Flowchart();
            //HACK:将父变量CurrentNode设置到flowchart
            this.Body.CurrentNode = this.CurrentNode;
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.ScheduleActivity(this.Body);
        }
    }
}

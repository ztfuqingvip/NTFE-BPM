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

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 数据字段扩展
    /// <remarks>主要用于在activity活动中与流程变量交互</remarks>
    /// </summary>
    public class DataFieldExtension
    {
        /// <summary>
        /// 获取发起人
        /// </summary>
        public string Originator { get; private set; }
        /// <summary>
        /// 获取当前节点索引
        /// </summary>
        public int CurrentNode { get; private set; }
        /// <summary>
        /// 获取数据字段集合
        /// </summary>
        public IDictionary<string, string> DataFields { get; private set; }
        /// <summary>
        /// 初始化数据字段扩展
        /// </summary>
        /// <param name="originator"></param>
        /// <param name="currentNode"></param>
        /// <param name="dataFields"></param>
        public DataFieldExtension(string originator, int currentNode, IDictionary<string, string> dataFields)
        {
            if (originator == null)
                throw new InvalidOperationException("originator不能为空");

            this.Originator = originator;
            this.CurrentNode = currentNode;
            this.DataFields = dataFields ?? new Dictionary<string, string>();
        }
        /// <summary>
        /// 设置变量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Set(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("name不能为空");
            if (!this.DataFields.ContainsKey(name))
                this.DataFields.Add(name, null);
            this.DataFields[name] = value;
        }
        /// <summary>
        /// 设置当前节点索引
        /// </summary>
        /// <param name="i"></param>
        public void SetCurrentNode(int i)
        {
            AssertHelper.ThrowIfInvalidFlowNodeIndex(i);
            this.CurrentNode = i;
            //this.Set(WorkflowBuilder.Variable_CurrentNode, i.ToString());
        }
    }
}
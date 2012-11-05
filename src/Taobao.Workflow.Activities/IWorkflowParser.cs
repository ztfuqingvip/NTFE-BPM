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
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 工作流转换器接口
    /// <remarks>提供工作流定义、节点设置（文本）、工作流活动对象之间的转换功能</remarks>
    /// </summary>
    public interface IWorkflowParser
    {
        /// <summary>
        /// 将工作流定义和节点设置合并转换为工作流活动对象
        /// </summary>
        /// <param name="workflowDefinition">工作流定义文本</param>
        /// <param name="activitySettings">节点设置列表</param>
        /// <returns></returns>
        WorkflowActivity Parse(string workflowDefinition, IEnumerable<ActivitySetting> activitySettings);
        /// <summary>
        /// 从工作流定义和节点设置转换为工作流活动对象并缓存该对象
        /// </summary>
        /// <param name="key">缓存的键</param>
        /// <param name="workflowDefinition">工作流定义</param>
        /// <param name="activitySettings">节点设置列表</param>
        /// <returns></returns>
        WorkflowActivity Parse(string key, string workflowDefinition, IEnumerable<ActivitySetting> activitySettings);

        /// <summary>
        /// 从工作流定义和节点设置文本转换出节点列表
        /// <remarks>主要用于流程类型创建</remarks>
        /// </summary>
        /// <param name="workflowDefinition">工作流定义</param>
        /// <param name="activitySettingsDefinition">节点设置文本</param>
        /// <returns></returns>
        ActivitySetting[] ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition);

        /// <summary>
        /// 将工作流活动对象转换成工作流定义
        /// <remarks>用于对工作流进行动态变更后新工作流定义的解析</remarks>
        /// </summary>
        /// <param name="workflowActivity">工作流活动对象</param>
        /// <param name="originalWorkflowDefinition">原始工作流定义</param>
        /// <returns>工作流图形描述文本</returns>
        string Parse(WorkflowActivity workflowActivity, string originalWorkflowDefinition);
        /// <summary>
        /// 从节点设置列表转换为节点设置描述文本
        /// <remarks>用于对节点设置进行动态变更后，新节点设置文本的解析</remarks>
        /// </summary>
        /// <param name="activitySettings">节点设置</param>
        /// <returns></returns>
        string Parse(IEnumerable<ActivitySetting> activitySettings);
    }
}
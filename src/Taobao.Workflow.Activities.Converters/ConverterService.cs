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

namespace Taobao.Workflow.Activities.Converters
{
    /// <summary>
    /// 对外服务解析器接口
    /// </summary>
    public interface IConverterService
    {
        /// <summary>
        /// 将XAML工作流描述定义和规则描述定义转换为新工作流描述定义
        /// </summary>
        /// <param name="workflowDefinition"></param>
        /// <param name="customSettingsDefinition"></param>
        /// <returns></returns>
        string[] ParseWorkflowDefinition(string workflowDefinition, string customSettingsDefinition);
        /// <summary>
        /// 根据指定的主键获取流程类型
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Converters.ProcessType GetProcessTypeByProcessId(Guid id);
    }
    /// <summary>
    /// 用于提供NTFE引擎客户端解析器接口实现
    /// </summary>
    public class ConverterService : IConverterService
    {
        private IWorkflowConverterFactory _factory;
        private IProcessService _processService;
        private IWorkflowParser _workflowParser;

        public ConverterService(IWorkflowConverterFactory factory
            , IProcessService processService
            , IWorkflowParser workflowParser)
        {
            this._factory = factory;
            this._processService = processService;
            this._workflowParser = workflowParser;
        }

        string[] IConverterService.ParseWorkflowDefinition(string workflowDefinition, string customSettingsDefinition)
        {
            var workflow = this._factory.Create(workflowDefinition).Parse(workflowDefinition, customSettingsDefinition);
            return new string[] { workflow };
        }
        Converters.ProcessType IConverterService.GetProcessTypeByProcessId(Guid processId)
        {
            return this.Parse(this._processService.GetProcess(processId).ProcessType);
        }

        private Converters.ProcessType Parse(Taobao.Workflow.Activities.ProcessType processType)
        {
            var activity = this._workflowParser.Parse(WorkflowBuilder.GetCacheKey(processType)
                , processType.Workflow.Serialized
                , processType.ActivitySettings);
            return new Converters.ProcessType()
            {
                CreateTime = processType.CreateTime,
                Description = processType.Description,
                Name = processType.Name,
                Version = processType.Version,
                IsCurrent = processType.IsCurrent,
                Group = processType.Group,
                ActivityNames = processType.ActivitySettings.Select(o => o.ActivityName).ToArray(),
                DataFields = activity.Variables.Select(o => o.Name).ToArray()
            };
        }
    }
}

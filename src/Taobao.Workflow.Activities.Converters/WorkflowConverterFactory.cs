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
using System.Xml.Linq;

namespace Taobao.Workflow.Activities.Converters
{
    /// <summary>
    /// 提供工作流转换器的创建
    /// </summary>
    public interface IWorkflowConverterFactory
    {
        /// <summary>
        /// 创建默认转换器
        /// </summary>
        /// <returns></returns>
        IWorkflowConverter Create();
        /// <summary>
        /// 通过指定的流程描述文本创建对应转换器
        /// </summary>
        /// <param name="workflowDefinition"></param>
        /// <returns></returns>
        IWorkflowConverter Create(string workflowDefinition);
    }
    [CodeSharp.Core.Component]
    public class WorkflowConverterFactory : IWorkflowConverterFactory
    {
        IWorkflowConverter IWorkflowConverterFactory.Create()
        {
            return new DefaultConverter();
        }
        IWorkflowConverter IWorkflowConverterFactory.Create(string workflowDefinition)
        {
            if (this.CheckIsXamlFormat(workflowDefinition))
                return new WF4Converter();
            else
                return new DefaultConverter();
        }

        //HACK:【重要】检测是否采用Xaml格式的脚本定义文本
        private bool CheckIsXamlFormat(string workflowDefinition)
        {
            var root = XElement.Parse(workflowDefinition);
            return root.Name.LocalName != "FlowChart";
        }
    }
}

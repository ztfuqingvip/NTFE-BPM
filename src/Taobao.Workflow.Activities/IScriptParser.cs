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
    /// 脚本解析器接口
    /// </summary>
    public interface IScriptParser
    {
        /// <summary>
        /// 执行完成规则
        /// </summary>
        /// <param name="workItem"></param>
        /// <param name="allOtherExecutions"></param>
        /// <param name="inputs"></param>
        /// <param name="action"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        bool EvaluateFinishRule(WorkItem workItem
            , List<DefaultHumanFinishRuleAsserter.Execution> allOtherExecutions
            , IDictionary<string, string> inputs
            , string action
            , string script);
        /// <summary>
        /// 执行脚本获取用户列表
        /// </summary>
        /// <param name="script"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        string[] EvaluateUsers(string script, Statements.DataFieldExtension extension);
        /// <summary>
        /// 执行完成规则脚本
        /// </summary>
        /// <param name="script"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        bool EvaluateRule(string script, Statements.DataFieldExtension extension);
        /// <summary>
        /// 执行脚本
        /// </summary>
        /// <param name="script"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        string Evaluate(string script, Statements.DataFieldExtension extension);
    }
}